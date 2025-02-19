using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class FieldValueConverter(IIndexFieldSettingSearchService searchService) : IIndexDocumentConverter
{
    public async Task ConvertAsync(string documentType, IList<IndexDocument> documents)
    {
        var criteria = AbstractTypeFactory<IndexFieldSettingSearchCriteria>.TryCreateInstance();
        criteria.DocumentType = documentType;

        var fieldSettings = await searchService.SearchAllNoCloneAsync(criteria);
        if (fieldSettings.Count == 0)
        {
            return;
        }

        foreach (var document in documents)
        {
            foreach (var fieldSetting in fieldSettings)
            {
                var documentField = document.Fields.FirstOrDefault(x => x.ValueType == IndexDocumentFieldValueType.String && x.Name.EqualsIgnoreCase(fieldSetting.FieldName));
                if (documentField != null)
                {
                    NormalizeValues(documentField, fieldSetting.Values);
                }
            }
        }
    }

    private static void NormalizeValues(IndexDocumentField documentField, IList<IndexFieldValueSetting> valueSettings)
    {
        List<string> newValues = null;

        for (var i = 0; i < documentField.Values.Count; i++)
        {
            var value = (string)documentField.Values[i];

            // Create a new list only if we replace at least one value.
            if (TryGetNewValue(value, valueSettings, out var newValue))
            {
                newValues ??= documentField.Values.Select(x => (string)x).Take(i).ToList();
                newValues.Add(newValue);
            }
            else if (newValues != null)
            {
                newValues.Add(value);
            }
        }

        if (newValues != null)
        {
            documentField.Values = newValues.Distinct().ToList<object>();
        }
    }

    private static bool TryGetNewValue(string value, IList<IndexFieldValueSetting> valueSettings, out string newValue)
    {
        newValue = null;

        var valueSetting = valueSettings.FirstOrDefault(x => x.Synonyms.ContainsIgnoreCase(value));
        if (valueSetting is null)
        {
            return false;
        }

        newValue = valueSetting.Value;

        return true;
    }
}
