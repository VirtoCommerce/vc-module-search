using System.Text;
using System.Xml.Serialization;

namespace VirtoCommerce.SearchModule.Data.Model.Filters
{
    public partial class AttributeFilter
    {
        [XmlElement("display")]
        public FilterDisplayName[] DisplayNames { get; set; }

        public string CacheKey
        {
            get
            {
                var key = new StringBuilder();
                key.Append("_af:" + Key);
                foreach (var field in this.Values)
                {
                    key.Append("_af:" + field.Id);
                }
                return key.ToString();
            }
        }
    }
}
