namespace VirtoCommerce.SearchModule.Core.Model
{
    public class NotFilter : IFilter
    {
        public IFilter ChildFilter { get; set; }

        public override string ToString()
        {
            return ChildFilter != null ? $"NOT({ChildFilter})" : string.Empty;
        }

        public object Clone()
        {
            var result = MemberwiseClone() as NotFilter;
            result.ChildFilter = ChildFilter?.Clone() as IFilter;

            return result;
        }
    }
}
