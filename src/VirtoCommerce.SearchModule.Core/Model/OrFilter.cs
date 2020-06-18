using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class OrFilter : IFilter
    {
        public IList<IFilter> ChildFilters { get; set; }

        public override string ToString()
        {
            return ChildFilters != null ? $"({string.Join(" OR ", ChildFilters)})" : string.Empty;
        }

        public object Clone()
        {
            var result = MemberwiseClone() as OrFilter;
            result.ChildFilters = ChildFilters?.Select(x => x.Clone()).OfType<IFilter>().ToList();

            return result;
        }
    }
}
