using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class RangeFilter : IFilter, INamedFilter
    {
        public string FieldName { get; set; }
        public IList<RangeFilterValue> Values { get; set; }

        public override string ToString()
        {
            return FieldName != null && Values != null ? $"{FieldName}:{string.Join(",", Values)}" : string.Empty;
        }
        public object Clone()
        {
            var result = MemberwiseClone() as RangeFilter;
            result.Values = Values?.Select(x => x.Clone()).OfType<RangeFilterValue>().ToList();

            return result;
        }
    }
}
