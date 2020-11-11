namespace VirtoCommerce.SearchModule.Core.Model
{
    public class WildCardTermFilter : IFilter, INamedFilter
    {
        public string FieldName { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return $"{FieldName}:{Value}";
        }

        public object Clone()
        {
            var result = MemberwiseClone() as WildCardTermFilter;
            return result;
        }
    }
}
