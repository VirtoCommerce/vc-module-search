using System.Xml.Serialization;

namespace VirtoCommerce.SearchModule.Data.Model.Filters
{
    public class FilterDisplayName
    {
        [XmlAttribute("language")]
        public string Language { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
    }
}
