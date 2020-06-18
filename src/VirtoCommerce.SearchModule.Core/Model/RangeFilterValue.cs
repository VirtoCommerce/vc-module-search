using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class RangeFilterValue : ValueObject
    {
        public string Lower { get; set; }
        public string Upper { get; set; }
        public bool IncludeLower { get; set; }
        public bool IncludeUpper { get; set; }

        public override string ToString()
        {
            return Lower != null || Upper != null ? $"{(IncludeLower ? "[" : "(")}{Lower}{(Lower != null ? " " : "")}TO{(Upper != null ? " " : "")}{Upper}{(IncludeUpper ? "]" : ")")}" : string.Empty;
        }
    }
}
