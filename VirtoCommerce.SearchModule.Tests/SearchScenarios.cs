using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using VirtoCommerce.Domain.Search.Filters;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests
{
        [CLSCompliant(false)]
        public class SearchScenarios : SearchTestsBase
        {
            private string _DefaultScope = "test";

    //        [Theory]
    //        [InlineData("Lucene")]
    //        [InlineData("Elastic")]
    //        [Trait("Category", "CI")]
    //        public void Can_create_search_index(string providerType)
    //        {
    //            var scope = _DefaultScope;
    //            var provider = GetSearchProvider(providerType, scope);
    //            SearchHelper.CreateSampleIndex(provider, scope);
    //        }

        [Theory]
        //[InlineData("Lucene")]
        [InlineData("Elastic")]
        [Trait("Category", "CI")]
        public void Can_find_item_using_search(string providerType)
        {
            var scope = _DefaultScope;
            var provider = GetSearchProvider(providerType, scope);
            SearchHelper.CreateSampleIndex(provider, scope);

            var criteria = new Data.Model.CatalogIndexedSearchCriteria
            {
                SearchPhrase = "product",
                IsFuzzySearch = true,
                Catalog = "goods",
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Pricelists = new string[] { }
            };

            var results = provider.Search<DocumentDictionary>(scope, criteria);

            Assert.True(results.DocCount == 1, string.Format("Returns {0} instead of 1", results.DocCount));

            criteria = new Data.Model.CatalogIndexedSearchCriteria
            {
                SearchPhrase = "sample product ",
                IsFuzzySearch = true,
                Catalog = "goods",
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Pricelists = new string[] { }
            };

            results = provider.Search<DocumentDictionary>(scope, criteria);

            Assert.True(results.DocCount == 1, string.Format("\"Sample Product\" search returns {0} instead of 1", results.DocCount));
        }

        [Theory]
        //[InlineData("Lucene")]
        [InlineData("Elastic")]
        [Trait("Category", "CI")]
        public void Can_get_item_facets(string providerType)
        {
            var scope = _DefaultScope;
            var provider = GetSearchProvider(providerType, scope);

            SearchHelper.CreateSampleIndex(provider, scope);

            var criteria = new Data.Model.CatalogIndexedSearchCriteria
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                Catalog = "goods",
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var filter = new AttributeFilter { Key = "Color" };
            filter.Values = new[]
                                {
                                    new AttributeFilterValue { Id = "red", Value = "red" },
                                    new AttributeFilterValue { Id = "blue", Value = "blue" },
                                    new AttributeFilterValue { Id = "black", Value = "black" }
                                };

            var rangefilter = new RangeFilter { Key = "size" };
            rangefilter.Values = new[]
                                     {
                                         new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                                         new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" }
                                     };

            var priceRangefilter = new PriceRangeFilter { Currency = "usd" };
            priceRangefilter.Values = new[]
                                          {
                                              new RangeFilterValue { Id = "0_to_100", Lower = "0", Upper = "100" },
                                              new RangeFilterValue { Id = "100_to_700", Lower = "100", Upper = "700" }
                                          };

            criteria.Add(filter);
            criteria.Add(rangefilter);
            criteria.Add(priceRangefilter);

            var results = provider.Search<DocumentDictionary>(scope, criteria);

            Assert.True(results.DocCount == 5, string.Format("Returns {0} instead of 5", results.DocCount));

            var redCount = GetFacetCount(results, "Color", "red");
            Assert.True(redCount == 3, string.Format("Returns {0} facets of red instead of 3", redCount));

            var priceCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceCount == 2, string.Format("Returns {0} facets of 0_to_100 prices instead of 2", priceCount));

            var priceCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount2 == 3, string.Format("Returns {0} facets of 100_to_700 prices instead of 3", priceCount2));

            var sizeCount = GetFacetCount(results, "size", "0_to_5");
            Assert.True(sizeCount == 3, string.Format("Returns {0} facets of 0_to_5 size instead of 3", sizeCount));

            var sizeCount2 = GetFacetCount(results, "size", "5_to_10");
            Assert.True(sizeCount2 == 1, string.Format("Returns {0} facets of 5_to_10 size instead of 1", sizeCount2)); // only 1 result because upper bound is not included

            var outlineCount = (results.Documents.ElementAt(0)["__outline"] as JArray).Count();
            Assert.True(outlineCount == 2, string.Format("Returns {0} outlines instead of 2", outlineCount));
        }

        [Theory]
        //[InlineData("Lucene")]
        [InlineData("Elastic")]
        [Trait("Category", "CI")]
        public void Can_get_item_multiple_filters(string providerType)
        {
            var scope = _DefaultScope;
            var provider = GetSearchProvider(providerType, scope);
            SearchHelper.CreateSampleIndex(provider, scope);

            var criteria = new Data.Model.CatalogIndexedSearchCriteria
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                Catalog = "goods",
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var colorFilter = new AttributeFilter { Key = "Color" };
            colorFilter.Values = new[]
                                {
                                            new AttributeFilterValue { Id = "red", Value = "red" },
                                            new AttributeFilterValue { Id = "blue", Value = "blue" },
                                            new AttributeFilterValue { Id = "black", Value = "black" }
                                        };

            var filter = new AttributeFilter { Key = "Color" };
            filter.Values = new[]
                                {
                                            new AttributeFilterValue { Id = "black", Value = "black" }
                                        };

            var rangefilter = new RangeFilter { Key = "size" };
            rangefilter.Values = new[]
                                     {
                                                 new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                                                 new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "11" }
                                             };

            var priceRangefilter = new PriceRangeFilter { Currency = "usd" };
            priceRangefilter.Values = new[]
                                          {
                                                      new RangeFilterValue { Id = "100_to_700", Lower = "100", Upper = "700" }
                                                  };

            criteria.Add(colorFilter);
            criteria.Add(rangefilter);
            criteria.Add(priceRangefilter);

            // add applied filters
            criteria.Apply(filter);
            criteria.Apply(rangefilter);
            criteria.Apply(priceRangefilter);

            var results = provider.Search<DocumentDictionary>(scope, criteria);

            var blackCount = GetFacetCount(results, "Color", "black");
            Assert.True(blackCount == 1, string.Format("Returns {0} facets of black instead of 1", blackCount));

            var redCount = GetFacetCount(results, "Color", "red");
            Assert.True(redCount == 2, string.Format("Returns {0} facets of black instead of 2", redCount));

            var priceCount = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount == 1, string.Format("Returns {0} facets of 100_to_700 instead of 1", priceCount));

            Assert.True(results.DocCount == 1, string.Format("Returns {0} instead of 1", results.DocCount));
        }

        private int GetFacetCount(ISearchResults results, string fieldName, string facetKey)
        {
            if (results.FacetGroups == null || results.FacetGroups.Length == 0)
            {
                return 0;
            }

            var group = (from fg in results.FacetGroups where fg.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase) select fg).SingleOrDefault();

            return @group == null ? 0 : (from facet in @group.Facets where facet.Key == facetKey select facet.Count).FirstOrDefault();
        }

        private int GetFacetCount(ISearchResults<DocumentDictionary> results, string fieldName, string facetKey)
        {
            if (results.Facets == null || results.Facets.Length == 0)
            {
                return 0;
            }

            var group = (from fg in results.Facets where fg.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase) select fg).SingleOrDefault();

            return @group == null ? 0 : (from facet in @group.Facets where facet.Key == facetKey select facet.Count).FirstOrDefault();
        }
    }
}