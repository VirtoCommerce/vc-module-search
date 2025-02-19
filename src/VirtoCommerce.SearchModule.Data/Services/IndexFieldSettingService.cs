using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class IndexFieldSettingService(ISettingsManager settingsManager) : IIndexFieldSettingSearchService, IIndexFieldSettingService
{
    private readonly StringComparer _ignoreCase = StringComparer.OrdinalIgnoreCase;

    private static readonly JsonSerializer _jsonSerializer = new()
    {
        DefaultValueHandling = DefaultValueHandling.Include,
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
    };

    public async Task<IndexFieldSettingSearchResult> SearchAsync(IndexFieldSettingSearchCriteria criteria, bool clone = true)
    {
        var fieldSettings = await GetAllFieldSettings();
        var query = fieldSettings.AsQueryable();

        if (criteria.DocumentType != null)
        {
            query = query.Where(x => x.DocumentType.EqualsIgnoreCase(criteria.DocumentType));
        }

        if (criteria.FieldName != null)
        {
            query = query.Where(x => x.FieldName.EqualsIgnoreCase(criteria.FieldName));
        }

        var result = AbstractTypeFactory<IndexFieldSettingSearchResult>.TryCreateInstance();
        result.Results = query.ToList();
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

        return fieldSettings
            .Where(x => ids.ContainsIgnoreCase(x.Id))
            .ToList();
    }

    public async Task SaveChangesAsync(IList<IndexFieldSetting> models)
    {
        if (models.IsNullOrEmpty())
        {
            return;
        }

        var fieldSettings = await GetAllFieldSettings();

        foreach (var model in models)
        {
            ValidateSetting(model);
            RemoveExistingSetting(model, fieldSettings);
            SortValues(model);
            GenerateIds(model);

            fieldSettings.Add(model);
        }

        CheckDuplicates(fieldSettings);
        SaveAllFieldSettings(fieldSettings);
    }

    public async Task DeleteAsync(IList<string> ids, bool softDelete = false)
    {
        if (ids.IsNullOrEmpty())
        {
            return;
        }

        var fieldSettings = await GetAllFieldSettings();

        foreach (var id in ids)
        {
            var setting = fieldSettings.FirstOrDefault(x => x.Id.EqualsIgnoreCase(id));
            if (setting != null)
            {
                fieldSettings.Remove(setting);
            }
        }

        SaveAllFieldSettings(fieldSettings);
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

    private void SortValues(IndexFieldSetting fieldSetting)
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

    private void CheckDuplicates(IList<IndexFieldSetting> fieldSettings)
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

    private async Task<IList<IndexFieldSetting>> GetAllFieldSettings()
    {
        var json = await settingsManager.GetValueAsync<string>(ModuleConstants.Settings.General.IndexSettings);

        using var stringReader = new StringReader(json);
        await using var jsonTextReader = new JsonTextReader(stringReader);
        var fieldSettings = _jsonSerializer.Deserialize<IList<IndexFieldSetting>>(jsonTextReader);

        return fieldSettings;
    }

    private void SaveAllFieldSettings(IList<IndexFieldSetting> fieldSettings)
    {
        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true);
        streamWriter.AutoFlush = true;
        using JsonWriter writer = new JsonTextWriter(streamWriter);
        _jsonSerializer.Serialize(writer, fieldSettings.OrderBy(x => x.DocumentType, _ignoreCase).ThenBy(x => x.FieldName, _ignoreCase));

        stream.Seek(0, SeekOrigin.Begin);
        var json = stream.ReadToString();

        settingsManager.SetValueAsync(ModuleConstants.Settings.General.IndexSettings.Name, json);
    }
}
