using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Testing;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests
{
    [Trait("Category", "Unit")]
    public class CloningTests
    {
        [Theory]
        [MemberData(nameof(TestFilters))]
        public void CloneCategory(IFilter filter)
        {
            filter.AssertCloneIndependency();
        }

        public static IEnumerable<object[]> TestFilters
        {
            get
            {
                var idsFilter = new IdsFilter
                {
                    Values = new[] { "a", "b" }
                };
                var rangeFilter = new RangeFilter
                {
                    FieldName = "range",
                    Values = new[] {
                        new RangeFilterValue
                        {
                             IncludeLower = true,
                             IncludeUpper = true,
                             Lower = "100",
                             Upper = "200"
                        }
                   }
                };
                var termFilter = new TermFilter
                {
                    FieldName = "term",
                    Values = new[] { "term1", "term2" }
                };
                var wildcardFilter = new WildCardTermFilter
                {
                    FieldName = "wildcard",
                    Value = "*"
                };
                var andFilter = new AndFilter
                {
                    ChildFilters = new IFilter[] { idsFilter, rangeFilter, termFilter, wildcardFilter }.ToList()
                };
                var orFilter = new OrFilter
                {
                    ChildFilters = new IFilter[] { andFilter, idsFilter, rangeFilter, termFilter, wildcardFilter }.ToList()
                };
                var notFilter = new NotFilter
                {
                    ChildFilter = orFilter
                };
                yield return new[] { idsFilter };
                yield return new[] { rangeFilter };
                yield return new[] { termFilter };
                yield return new[] { wildcardFilter };
                yield return new[] { andFilter };
                yield return new[] { orFilter };
                yield return new[] { notFilter };
            }

        }
    }
}
