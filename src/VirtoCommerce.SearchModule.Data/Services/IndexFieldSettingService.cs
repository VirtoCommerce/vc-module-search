using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class IndexFieldSettingService(ISettingsManager settingsManager) : IIndexFieldSettingSearchService, IIndexFieldSettingService
{
    private static readonly StringComparer _ignoreCase = StringComparer.OrdinalIgnoreCase;

    // The setting holds every field setting as one JSON document - megabytes once a value mapping carries thousands of
    // synonyms - and FieldValueConverter reads it for every indexed batch. Parse it once per distinct value: a save,
    // a change made on another instance and a configuration override all arrive as a different string.
    private static ParsedFieldSettings _parsedFieldSettings;

    public async Task<IndexFieldSettingSearchResult> SearchAsync(IndexFieldSettingSearchCriteria criteria, bool clone = true)
    {
        IEnumerable<IndexFieldSetting> query = await GetAllFieldSettings();

        if (criteria.DocumentType != null)
        {
            query = query.Where(x => x.DocumentType.EqualsIgnoreCase(criteria.DocumentType));
        }

        if (criteria.FieldName != null)
        {
            query = query.Where(x => x.FieldName.EqualsIgnoreCase(criteria.FieldName));
        }

        if (!string.IsNullOrEmpty(criteria.Keyword))
        {
            query = query.Where(x => x.FieldName.Contains(criteria.Keyword, StringComparison.OrdinalIgnoreCase));
        }

        var result = AbstractTypeFactory<IndexFieldSettingSearchResult>.TryCreateInstance();
        result.Results = ToList(query, clone);
        result.TotalCount = result.Results.Count;

        return result;
    }

    public async Task<IList<IndexFieldSetting>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true)
    {
        if (ids.IsNullOrEmpty())
        {
            return [];
        }

        var fieldSettings = await GetAllFieldSettings();

        return ToList(fieldSettings.Where(x => ids.Contains(x.Id, _ignoreCase)), clone);
    }

    public async Task SaveChangesAsync(IList<IndexFieldSetting> models)
    {
        if (models.IsNullOrEmpty())
        {
            return;
        }

        var fieldSettings = (await GetAllFieldSettings()).ToList();

        foreach (var model in models)
        {
            ValidateSetting(model);
            RemoveExistingSetting(model, fieldSettings);
            SortValues(model);
            GenerateIds(model);

            fieldSettings.Add(model);
        }

        CheckDuplicates(fieldSettings);
        await SaveAllFieldSettings(fieldSettings);
    }

    public async Task DeleteAsync(IList<string> ids, bool softDelete = false)
    {
        if (ids.IsNullOrEmpty())
        {
            return;
        }

        var fieldSettings = (await GetAllFieldSettings()).ToList();

        foreach (var id in ids)
        {
            var setting = fieldSettings.FirstOrDefault(x => x.Id.EqualsIgnoreCase(id));
            if (setting != null)
            {
                fieldSettings.Remove(setting);
            }
        }

        await SaveAllFieldSettings(fieldSettings);
    }


    private static void ValidateSetting(IndexFieldSetting fieldSetting)
    {
        if (string.IsNullOrEmpty(fieldSetting.DocumentType))
        {
            throw new InvalidOperationException("DocumentType is required.");
        }

        if (string.IsNullOrEmpty(fieldSetting.FieldName))
        {
            throw new InvalidOperationException("FieldName is required.");
        }

        if (fieldSetting.Values != null && fieldSetting.Values.Any(x => string.IsNullOrEmpty(x.Value)))
        {
            throw new InvalidOperationException("Value is required.");
        }
    }

    private static void RemoveExistingSetting(IndexFieldSetting model, IList<IndexFieldSetting> fieldSettings)
    {
        IndexFieldSetting existingSetting = null;

        if (!string.IsNullOrEmpty(model.Id))
        {
            existingSetting = fieldSettings.FirstOrDefault(x => x.Id.EqualsIgnoreCase(model.Id));
        }

        existingSetting ??= fieldSettings.FirstOrDefault(x => x.DocumentType.EqualsIgnoreCase(model.DocumentType) && x.FieldName.EqualsIgnoreCase(model.FieldName));

        if (existingSetting != null)
        {
            fieldSettings.Remove(existingSetting);
        }
    }

    private static void SortValues(IndexFieldSetting fieldSetting)
    {
        fieldSetting.Values = fieldSetting.Values?.OrderBy(x => x.Value, _ignoreCase).ToList() ?? [];

        foreach (var valueSetting in fieldSetting.Values)
        {
            valueSetting.Synonyms = valueSetting.Synonyms
                ?.Where(x => !string.IsNullOrEmpty(x))
                .OrderBy(x => x, _ignoreCase)
                .Distinct(_ignoreCase)
                .ToList() ?? [];
        }
    }

    private static void GenerateIds(IndexFieldSetting fieldSetting)
    {
        if (string.IsNullOrEmpty(fieldSetting.Id))
        {
            fieldSetting.Id = NewId();
        }

        foreach (var valueSetting in fieldSetting.Values.Where(x => string.IsNullOrEmpty(x.Id)))
        {
            valueSetting.Id = NewId();
        }
    }

    private static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static void CheckDuplicates(IList<IndexFieldSetting> fieldSettings)
    {
        var fieldKeys = new HashSet<string>(_ignoreCase);
        var synonymKeys = new HashSet<string>(_ignoreCase);

        foreach (var fieldSetting in fieldSettings)
        {
            var fieldKey = $"{fieldSetting.DocumentType}.{fieldSetting.FieldName}";
            if (!fieldKeys.Add(fieldKey))
            {
                throw new InvalidOperationException($"Index field setting '{fieldKey}' already exists.");
            }

            foreach (var synonym in fieldSetting.Values.SelectMany(x => x.Synonyms))
            {
                var synonymKey = $"{fieldKey}.{synonym}";
                if (!synonymKeys.Add(synonymKey))
                {
                    throw new InvalidOperationException($"Synonym '{synonymKey}' already exists.");
                }
            }
        }
    }

    // The returned list and its items are shared by every caller: copy before changing either
    private async Task<IList<IndexFieldSetting>> GetAllFieldSettings()
    {
        var json = await settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.IndexSettings);

        return ParseFieldSettings(json);
    }

    private static IList<IndexFieldSetting> ParseFieldSettings(string json)
    {
        var parsed = _parsedFieldSettings;
        if (parsed is null || !string.Equals(parsed.Json, json, StringComparison.Ordinal))
        {
            parsed = new ParsedFieldSettings(json, JsonConvert.DeserializeObject<IList<IndexFieldSetting>>(json));
            _parsedFieldSettings = parsed;
        }

        return parsed.FieldSettings;
    }

    private static List<IndexFieldSetting> ToList(IEnumerable<IndexFieldSetting> fieldSettings, bool clone)
    {
        return clone
            ? fieldSettings.Select(CloneSetting).ToList()
            : fieldSettings.ToList();
    }

    private static IndexFieldSetting CloneSetting(IndexFieldSetting fieldSetting)
    {
        return new IndexFieldSetting
        {
            Id = fieldSetting.Id,
            DocumentType = fieldSetting.DocumentType,
            FieldName = fieldSetting.FieldName,
            Values = fieldSetting.Values?
                .Select(x => new IndexFieldValueSetting
                {
                    Id = x.Id,
                    Value = x.Value,
                    Synonyms = x.Synonyms?.ToList(),
                })
                .ToList(),
        };
    }

    private sealed record ParsedFieldSettings(string Json, IList<IndexFieldSetting> FieldSettings);

    private Task SaveAllFieldSettings(IList<IndexFieldSetting> fieldSettings)
    {
        var orderedSettings = fieldSettings
            .OrderBy(x => x.DocumentType, _ignoreCase)
            .ThenBy(x => x.FieldName, _ignoreCase);

        var json = JsonConvert.SerializeObject(orderedSettings, Formatting.Indented);

        return settingsManager.SetValueAsync(ModuleConstants.Settings.General.IndexSettings.Name, json);
    }
}
