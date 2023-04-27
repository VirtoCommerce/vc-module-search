using System.Collections.Generic;
using System.Diagnostics;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model
{
    /// <summary>
    /// Represents a single field in a document that will be indexed by search engines such as Azure Search or Elasticsearch.
    /// It includes the name and one or more values for the field.
    /// </summary>
    [DebuggerDisplay("{Name}: {string.Join(\", \", Values)}")]
    public class IndexDocumentField
    {
        public IndexDocumentField(string name, object value)
        {
            Name = name;
            Values = new List<object> { value };
        }

        public IndexDocumentField(string name, IList<object> values)
        {
            Name = name;
            Values = values;
        }

        public string Name { get; set; }
        public IList<object> Values { get; set; }

        public object Value
        {
            get
            {
                if (Values != null && Values.Count > 0)
                {
                    return Values[0];
                }

                return null;
            }
        }

        /// <summary>
        /// Indicats whether the field value can be retrieved from the index.
        /// </summary>
        public bool IsRetrievable { get; set; }

        /// <summary>
        /// Indicats whether the field can be used in search requests for filtering.
        /// </summary>
        public bool IsFilterable { get; set; }

        /// <summary>
        /// Indicats whether the field can be used in search requests for searching.
        /// </summary>
        public bool IsSearchable { get; set; }

        /// <summary>
        /// Indicates whether the field can contain a collection of values.
        /// </summary>
        public bool IsCollection { get; set; }

        /// <summary>
        /// Indicates the data type of the field.
        /// </summary>
        public IndexDocumentFieldValueType ValueType { get; set; }

        /// <summary>
        /// Combine the values of two IndexDocumentField objects if the IsCollection property is true.
        /// </summary>
        /// <param name="field"></param>
        public void Merge(IndexDocumentField field)
        {
            if (IsCollection)
            {
                foreach (var value in field.Values)
                {
                    Values.AddDistinct(value);
                }
            }
        }
    }
}
