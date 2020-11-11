using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class AndFilter : IFilter
    {
        public IList<IFilter> ChildFilters { get; set; }

        public override string ToString()
        {
            return ChildFilters != null ? $"({string.Join(" AND ", ChildFilters)})" : string.Empty;
        }


        public object Clone()
        {
            var result = MemberwiseClone() as AndFilter;
            result.ChildFilters = ChildFilters?.Select(x => x.Clone()).OfType<IFilter>().ToList();
          
            return result;
        }

    }
}
