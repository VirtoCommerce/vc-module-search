using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.SearchModule.Core.Model;
using static VirtoCommerce.SearchModule.Core.Extensions.IndexDocumentExtensions;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class IndexDocumentDynamicPropertyExtensions
{
    public static async Task AddDynamicProperties(this IndexDocument schema, IDynamicPropertySearchService dynamicPropertySearchService, params string[] objectTypes)
    {
        var properties = await dynamicPropertySearchService.GetAllDynamicProperties(objectTypes);

        foreach (var property in properties)
        {
            var name = property.Name.ToLowerInvariant();
            var valueType = property.ValueType.ToIndexedDocumentFieldValueType();
            var isCollection = property.IsDictionary || property.IsArray;
            var value = GetSchemaValue(property.ValueType);

            schema.Add(new IndexDocumentField(name, value, valueType)
            {
                IsRetrievable = true,
                IsFilterable = true,
                IsCollection = isCollection,
            });
        }
    }

    public static async Task AddDynamicProperties(this IndexDocument document, IDynamicPropertySearchService dynamicPropertySearchService, IHasDynamicProperties @object)
    {
        var properties = await dynamicPropertySearchService.GetAllDynamicProperties(@object.ObjectType);

        foreach (var property in properties)
        {
            var objectProperty = @object.DynamicProperties?.FirstOrDefault(x => x.Id == property.Id) ??
                @object.DynamicProperties?.FirstOrDefault(x => x.Name.EqualsInvariant(property.Name) && x.HasValuesOfType(property.ValueType));

            document.AddDynamicProperty(property, objectProperty);
        }
    }

    public static bool HasValuesOfType(this DynamicObjectProperty objectProperty, DynamicPropertyValueType valueType)
    {
        return objectProperty.Values?.Any(x => x.ValueType == valueType) ??
            objectProperty.ValueType == valueType;
    }

    public static void AddDynamicProperty(this IndexDocument document, DynamicProperty property, DynamicObjectProperty objectProperty)
    {
        var name = property.Name.ToLowerInvariant();
        var valueType = property.ValueType.ToIndexedDocumentFieldValueType();
        var isCollection = property.IsDictionary || property.IsArray;

        IList<object> values = null;

        if (objectProperty != null)
        {
            if (objectProperty.IsDictionary)
            {
                // Add all locales in dictionary to the index
                values = objectProperty.Values
                    .Select(x => x.Value)
                    .Cast<DynamicPropertyDictionaryItem>()
                    .Where(x => !string.IsNullOrEmpty(x.Name))
                    .Select(x => x.Name)
                    .ToList<object>();
            }
            else
            {
                values = objectProperty.Values
                    .Where(x => x.Value != null)
                    .Select(x => x.Value)
                    .ToList();
            }

            // Add DynamicProperties that have the ShortText value type to __content
            if (property.ValueType == DynamicPropertyValueType.ShortText)
            {
                foreach (var value in values)
                {
                    document.AddContentString(value.ToString());
                }
            }
        }

        // Replace empty value for Boolean property with default 'False'
        if (property.ValueType == DynamicPropertyValueType.Boolean && values.IsNullOrEmpty())
        {
            document.Add(new IndexDocumentField(name, false, IndexDocumentFieldValueType.Boolean)
            {
                IsRetrievable = true,
                IsFilterable = true,
                IsCollection = isCollection,
            });
        }
        else if (!values.IsNullOrEmpty())
        {
            document.Add(new IndexDocumentField(name, values, valueType)
            {
                IsRetrievable = true,
                IsFilterable = true,
                IsCollection = isCollection,
            });
        }
    }

    public static async Task<IList<DynamicProperty>> GetAllDynamicProperties(this IDynamicPropertySearchService dynamicPropertySearchService, params string[] objectTypes)
    {
        var result = new List<DynamicProperty>();

        var searchCriteria = AbstractTypeFactory<DynamicPropertySearchCriteria>.TryCreateInstance();
        searchCriteria.ObjectTypes = objectTypes;
        searchCriteria.Take = 50;

        int totalCount;

        do
        {
            var searchResult = await dynamicPropertySearchService.SearchAsync(searchCriteria);

            var validProperties = searchResult.Results.Where(x =>
                !string.IsNullOrEmpty(x.Name) &&
                x.ValueType.ToIndexedDocumentFieldValueType() != IndexDocumentFieldValueType.Undefined);

            result.AddRange(validProperties);

            totalCount = searchResult.TotalCount;
            searchCriteria.Skip += searchCriteria.Take;
        }
        while (searchCriteria.Skip < totalCount);

        return result;
    }

    public static object GetSchemaValue(DynamicPropertyValueType type)
    {
        return type switch
        {
            DynamicPropertyValueType.ShortText => SchemaStringValue,
            DynamicPropertyValueType.Html => SchemaStringValue,
            DynamicPropertyValueType.LongText => SchemaStringValue,
            DynamicPropertyValueType.Image => SchemaStringValue,
            DynamicPropertyValueType.Integer => default(int),
            DynamicPropertyValueType.Decimal => default(decimal),
            DynamicPropertyValueType.DateTime => default(DateTime),
            DynamicPropertyValueType.Boolean => default(bool),
            DynamicPropertyValueType.Undefined => null,
            _ => null
        };
    }
}
