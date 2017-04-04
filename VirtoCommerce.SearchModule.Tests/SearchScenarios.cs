using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using Xunit;

namespace VirtoCommerce.SearchModule.Test
{
    [CLSCompliant(false)]
    [Collection("Search")]
    [Trait("Category", "CI")]
    public class SearchScenarios : SearchTestsBase
    {
        private const string _scope = "test";

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_find_pricelists_prices(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope);

            var criteria = new KeywordSearchCriteria("catalogitem")
            {
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Currency = "usd",
                Pricelists = new[] { "default", "sale" }
            };

            var priceRangefilter = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue {Id = "0_to_100", Lower = "0", Upper = "100"},
                    new RangeFilterValue {Id = "100_to_700", Lower = "100", Upper = "700"}
                }
            };

            criteria.Add(priceRangefilter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 6, $"Returns {results.DocCount} instead of 6");

            var priceCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceCount == 2, $"Returns {priceCount} facets of 0_to_100 prices instead of 2");

            var priceCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount2 == 3, $"Returns {priceCount2} facets of 100_to_700 prices instead of 3");

            criteria = new KeywordSearchCriteria("catalogitem")
            {
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Currency = "usd",
                Pricelists = new[] { "sale", "default" }
            };

            criteria.Add(priceRangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 6, $"\"Sample Product\" search returns {results.DocCount} instead of 6");

            var priceSaleCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceSaleCount == 3, $"Returns {priceSaleCount} facets of 0_to_100 prices instead of 2");

            var priceSaleCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceSaleCount2 == 2, $"Returns {priceSaleCount2} facets of 100_to_700 prices instead of 3");

        }

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_create_search_index(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope);
        }

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_find_item_using_search(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope);

            var criteria = new KeywordSearchCriteria("catalogitem")
            {
                SearchPhrase = "product",
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Pricelists = new string[] { },
                Sort = new SearchSort("somefield") // specifically add non-existent field
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 1, $"Returns {results.DocCount} instead of 1");

            criteria = new KeywordSearchCriteria("catalogitem")
            {
                SearchPhrase = "sample product ",
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Pricelists = new string[] { }
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 1, $"\"Sample Product\" search returns {results.DocCount} instead of 1");
        }

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_sort_using_search(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope);

            var criteria = new KeywordSearchCriteria("catalogitem")
            {
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Pricelists = new string[] { },
                Sort = new SearchSort("name")
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(6, results.DocCount);
            var productName = results.Documents.ElementAt(0)["name"] as string; // black sox
            Assert.True(productName == "black sox");

            criteria = new KeywordSearchCriteria("catalogitem")
            {
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Pricelists = new string[] { },
                Sort = new SearchSort("name", true)
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(6, results.DocCount);
            productName = results.Documents.ElementAt(0)["name"] as string; // sample product
            Assert.True(productName == "sample product");
        }

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_get_item_facets(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);

            SearchHelper.CreateSampleIndex(provider, _scope);

            var criteria = new KeywordSearchCriteria("catalogitem")
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                RecordsToRetrieve = 0,
                StartingRecord = 0,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var filter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "red", Value = "red"},
                    new AttributeFilterValue {Id = "blue", Value = "blue"},
                    new AttributeFilterValue {Id = "black", Value = "black"}
                }
            };

            var rangefilter = new RangeFilter
            {
                Key = "size",
                Values = new[]
                {
                    new RangeFilterValue {Id = "0_to_5", Lower = "0", Upper = "5"},
                    new RangeFilterValue {Id = "5_to_10", Lower = "5", Upper = "10"}
                }
            };

            var priceRangefilter = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue {Id = "0_to_100", Lower = "0", Upper = "100"},
                    new RangeFilterValue {Id = "100_to_700", Lower = "100", Upper = "700"},
                    new RangeFilterValue {Id = "over_700", Lower = "700"},
                    new RangeFilterValue {Id = "under_100", Upper = "100"},
                }
            };

            criteria.Add(filter);
            criteria.Add(rangefilter);
            criteria.Add(priceRangefilter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 0, $"Returns {results.DocCount} instead of 0");

            var redCount = GetFacetCount(results, "Color", "red");
            Assert.True(redCount == 3, $"Returns {redCount} facets of red instead of 3");

            var priceCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceCount == 2, $"Returns {priceCount} facets of 0_to_100 prices instead of 2");

            var priceCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount2 == 3, $"Returns {priceCount2} facets of 100_to_700 prices instead of 3");

            var priceCount3 = GetFacetCount(results, "Price", "over_700");
            Assert.True(priceCount3 == 1, $"Returns {priceCount3} facets of over_700 prices instead of 1");

            var priceCount4 = GetFacetCount(results, "Price", "under_100");
            Assert.True(priceCount4 == 2, $"Returns {priceCount4} facets of priceCount4 prices instead of 2");

            var sizeCount = GetFacetCount(results, "size", "0_to_5");
            Assert.True(sizeCount == 3, $"Returns {sizeCount} facets of 0_to_5 size instead of 3");

            var sizeCount2 = GetFacetCount(results, "size", "5_to_10");
            Assert.True(sizeCount2 == 1, $"Returns {sizeCount2} facets of 5_to_10 size instead of 1"); // only 1 result because upper bound is not included
        }

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_get_item_outlines(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);

            SearchHelper.CreateSampleIndex(provider, _scope);

            var criteria = new KeywordSearchCriteria("catalogitem")
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                RecordsToRetrieve = 6,
                StartingRecord = 0,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 6, $"Returns {results.DocCount} instead of 6");

            int outlineCount;
            var outlineObject = results.Documents.ElementAt(0)["__outline"]; // can be JArray or object[] depending on provider used
            if (outlineObject is JArray)
                outlineCount = (outlineObject as JArray).Count;
            else
                outlineCount = ((object[])outlineObject).Length;

            Assert.True(outlineCount == 2, $"Returns {outlineCount} outlines instead of 2");
        }

        [Theory]
        //[InlineData("Lucene")]
        //[InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_get_item_multiple_filters(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope);

            var criteria = new KeywordSearchCriteria("catalogitem")
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
                StartingRecord = 0,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var colorFilter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "red", Value = "red"},
                    new AttributeFilterValue {Id = "blue", Value = "blue"},
                    new AttributeFilterValue {Id = "black", Value = "black"}
                }
            };

            var filter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "black", Value = "black"}
                }
            };

            var rangefilter = new RangeFilter
            {
                Key = "size",
                Values = new[]
                {
                    new RangeFilterValue {Id = "0_to_5", Lower = "0", Upper = "5"},
                    new RangeFilterValue {Id = "5_to_10", Lower = "5", Upper = "11"}
                }
            };

            var priceRangefilter = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue {Id = "100_to_700", Lower = "100", Upper = "700"}
                }
            };

            criteria.Add(colorFilter);
            criteria.Add(rangefilter);
            criteria.Add(priceRangefilter);

            // add applied filters
            criteria.Apply(filter);
            criteria.Apply(rangefilter);
            criteria.Apply(priceRangefilter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            var blackCount = GetFacetCount(results, "Color", "black");
            Assert.True(blackCount == 1, $"Returns {blackCount} facets of black instead of 1");

            var redCount = GetFacetCount(results, "Color", "red");
            Assert.True(redCount == 2, $"Returns {redCount} facets of black instead of 2");

            var priceCount = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount == 1, $"Returns {priceCount} facets of 100_to_700 instead of 1");

            Assert.True(results.DocCount == 1, $"Returns {results.DocCount} instead of 1");
        }


        private static int GetFacetCount(ISearchResults<DocumentDictionary> results, string fieldName, string facetKey)
        {
            if (results.Facets == null || results.Facets.Length == 0)
            {
                return 0;
            }

            var group = results.Facets.SingleOrDefault(fg => fg.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            return group?.Facets
                .Where(facet => facet.Key == facetKey)
                .Select(facet => facet.Count)
                .FirstOrDefault() ?? 0;
        }
    }
}
