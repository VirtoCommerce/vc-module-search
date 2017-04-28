using System.Xml.Serialization;

namespace VirtoCommerce.SearchModule.Core.Model.Filters
{
    public partial class RangeFilterValue
    {
        [XmlAttribute("includeLower")]
        public bool IncludeLower { get; set; } = true; // The default value is 'true' for compatibility with previous ranges implementation
        [XmlAttribute("includeUpper")]
        public bool IncludeUpper { get; set; }
    }
}
