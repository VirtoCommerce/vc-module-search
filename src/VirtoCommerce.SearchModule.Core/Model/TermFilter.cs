using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class TermFilter : IFilter, INamedFilter
    {
        public string FieldName { get; set; }
        public IList<string> Values { get; set; }
        public bool IsGenerated { get; set; }

        public override string ToString()
        {
            return $"{FieldName}:{string.Join(",", Values)}";
        }

        public object Clone()
        {
            var result = MemberwiseClone() as TermFilter;
            result.Values = Values?.ToList();

            return result;
        }
    }
}
