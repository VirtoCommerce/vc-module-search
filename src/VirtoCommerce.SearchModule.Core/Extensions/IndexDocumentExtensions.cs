using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class IndexDocumentExtensions
{
    public const string ContentFieldName = "__content";
    public const string SchemaStringValue = "schema";

    public static void AddFilterableCollectionAndContentString(this IndexDocument schema, string name)
    {
        schema.AddFilterableCollectionAndContentString(name, new[] { SchemaStringValue });
    }

    /// <summary>
    ///  Adds given values to the filterable collection with given name and to the searchable '__content' collection
    /// </summary>
    /// <param name="document"></param>
    /// <param name="name"></param>
    /// <param name="values"></param>
    public static void AddFilterableCollectionAndContentString(this IndexDocument document, string name, ICollection<string> values)
    {
        if (values?.Any() == true)
        {
            foreach (var value in values)
            {
                document.AddFilterableCollectionAndContentString(name, value);
            }
        }
    }

    /// <summary>
    ///  Adds given value to the filterable collection with given name and to the searchable '__content' collection
    /// </summary>
    /// <param name="document"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void AddFilterableCollectionAndContentString(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.AddFilterableCollection(name, value);
            document.AddContentString(value);
        }
    }

    public static void AddFilterableStringAndContentString(this IndexDocument schema, string name)
    {
        schema.AddFilterableStringAndContentString(name, SchemaStringValue);
    }

    /// <summary>
    ///  Adds given value to the filterable field with given name and to the searchable '__content' collection
    /// </summary>
    /// <param name="document"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void AddFilterableStringAndContentString(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.AddFilterableString(name, value);
            document.AddContentString(value);
        }
    }

    public static void AddSuggestableStringAndContentString(this IndexDocument schema, string name)
    {
        schema.AddSuggestableStringAndContentString(name, SchemaStringValue);
    }

    public static void AddSuggestableStringAndContentString(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.AddSuggestableString(name, value);
            document.AddContentString(value);
        }
    }

    public static void AddSuggestableString(this IndexDocument schema, string name)
    {
        schema.AddFilterableString(name, SchemaStringValue);
    }

    public static void AddSuggestableString(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.Add(new IndexDocumentField(name, value, IndexDocumentFieldValueType.String)
            {
                IsRetrievable = true,
                IsFilterable = true,
                IsSuggestable = true,
            });
        }
    }

    public static void AddContentString(this IndexDocument document)
    {
        document.AddContentString(SchemaStringValue);
    }

    /// <summary>
    ///  Adds given value to the searchable '__content' collection
    /// </summary>
    /// <param name="document"></param>
    /// <param name="value"></param>
    public static void AddContentString(this IndexDocument document, string value)
    {
        document.AddSearchableCollection(ContentFieldName, value);
    }

    /// <summary>
    /// Adds given value to the searchable '__content_{languageCode}' collection with given language code.
    /// If languageCode is null or empty, adds value to the searchable '__content' collection.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="value"></param>
    /// <param name="languageCode"></param>
    public static void AddContentString(this IndexDocument document, string value, string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            document.AddSearchableCollection(ContentFieldName, value);
        }
        else
        {
            document.AddSearchableCollection($"{ContentFieldName}_{languageCode.ToLowerInvariant()}", value);
        }
    }

    public static void AddFilterableBoolean(this IndexDocument schema, string name)
    {
        schema.AddFilterableBoolean(name, false);
    }

    public static void AddFilterableBoolean(this IndexDocument document, string name, bool? value)
    {
        document.AddFilterableValue(name, value, IndexDocumentFieldValueType.Boolean);
    }


    public static void AddFilterableDateTime(this IndexDocument schema, string name)
    {
        schema.AddFilterableDateTime(name, DateTime.MinValue);
    }

    public static void AddFilterableDateTime(this IndexDocument document, string name, DateTime? value)
    {
        document.AddFilterableValue(name, value, IndexDocumentFieldValueType.DateTime);
    }


    public static void AddFilterableInteger(this IndexDocument schema, string name)
    {
        schema.AddFilterableInteger(name, 0);
    }

    public static void AddFilterableInteger(this IndexDocument document, string name, int? value)
    {
        document.AddFilterableValue(name, value, IndexDocumentFieldValueType.Integer);
    }


    public static void AddFilterableDecimal(this IndexDocument schema, string name)
    {
        schema.AddFilterableDecimal(name, 0m);
    }

    public static void AddFilterableDecimal(this IndexDocument document, string name, decimal? value)
    {
        document.AddFilterableValue(name, value, IndexDocumentFieldValueType.Decimal);
    }


    public static void AddFilterableDouble(this IndexDocument schema, string name)
    {
        schema.AddFilterableDouble(name, 0d);
    }

    public static void AddFilterableDouble(this IndexDocument document, string name, double? value)
    {
        document.AddFilterableValue(name, value, IndexDocumentFieldValueType.Double);
    }


    public static void AddFilterableString(this IndexDocument schema, string name)
    {
        schema.AddFilterableString(name, SchemaStringValue);
    }

    public static void AddFilterableString(this IndexDocument document, string name, string value)
    {
        document.AddFilterableValue(name, value, IndexDocumentFieldValueType.String);
    }


    public static void AddFilterableValue(this IndexDocument document, string name, object value, IndexDocumentFieldValueType valueType)
    {
        if (value != null)
        {
            document.Add(new IndexDocumentField(name, value, valueType)
            {
                IsRetrievable = true,
                IsFilterable = true,
            });
        }
    }


    public static void AddFilterableCollection(this IndexDocument schema, string name)
    {
        schema.AddFilterableCollection(name, SchemaStringValue);
    }

    public static void AddFilterableCollection(this IndexDocument document, string name, ICollection<string> values)
    {
        if (values?.Any() == true)
        {
            foreach (var value in values)
            {
                document.AddFilterableCollection(name, value);
            }
        }
    }

    public static void AddFilterableCollection(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.Add(new IndexDocumentField(name, value, IndexDocumentFieldValueType.String)
            {
                IsRetrievable = true,
                IsFilterable = true,
                IsCollection = true,
            });
        }
    }


    public static void AddSearchableCollection(this IndexDocument schema, string name)
    {
        schema.AddSearchableCollection(name, SchemaStringValue);
    }

    public static void AddSearchableCollection(this IndexDocument document, string name, ICollection<string> values)
    {
        if (values?.Any() == true)
        {
            foreach (var value in values)
            {
                document.AddSearchableCollection(name, value);
            }
        }
    }

    public static void AddSearchableCollection(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.Add(new IndexDocumentField(name, value, IndexDocumentFieldValueType.String)
            {
                IsRetrievable = true,
                IsSearchable = true,
                IsCollection = true,
            });
        }
    }


    public static void AddFilterableAndSearchableCollection(this IndexDocument schema, string name)
    {
        schema.AddFilterableAndSearchableCollection(name, SchemaStringValue);
    }

    public static void AddFilterableAndSearchableCollection(this IndexDocument document, string name, ICollection<string> values)
    {
        if (values?.Any() == true)
        {
            foreach (var value in values)
            {
                document.AddFilterableAndSearchableCollection(name, value);
            }
        }
    }

    public static void AddFilterableAndSearchableCollection(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.Add(new IndexDocumentField(name, value, IndexDocumentFieldValueType.String)
            {
                IsRetrievable = true,
                IsFilterable = true,
                IsSearchable = true,
                IsCollection = true,
            });
        }
    }
}
