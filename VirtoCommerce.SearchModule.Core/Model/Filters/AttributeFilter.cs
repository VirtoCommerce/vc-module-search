using System.Text;
using System.Xml.Serialization;

namespace VirtoCommerce.SearchModule.Core.Model.Filters
{
    public partial class AttributeFilter
    {
        [XmlElement("display")]
        public FilterDisplayName[] DisplayNames { get; set; }

        [XmlElement("facetSize")]
        public int? FacetSize { get; set; }

        public string CacheKey
        {
            get
            {
                var key = new StringBuilder();
                key.Append("_af:" + Key);
                foreach (var field in Values)
                {
                    key.Append("_af:" + field.Id);
                }
                return key.ToString();
            }
        }
    }
}
