using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class IdsFilter : IFilter
    {
        public IList<string> Values { get; set; }

        public override string ToString()
        {
            return $"ID:{string.Join(",", Values)}";
        }

        public object Clone()
        {
            var result = MemberwiseClone() as IdsFilter;
            result.Values = Values?.ToList();

            return result;
        }
    }
}
