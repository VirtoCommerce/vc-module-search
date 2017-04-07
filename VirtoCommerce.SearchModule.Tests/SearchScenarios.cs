using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Platform.Core.Common;
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
        private const string _documentType = "item";

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanCreateIndex(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanUpdateIndex(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType, true);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanSearchByPhrase(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                SearchPhrase = " shirt ",
                RecordsToRetrieve = 10,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(3, results.DocCount);
            Assert.Equal(3, results.TotalCount);


            criteria = new KeywordSearchCriteria(_documentType)
            {
                SearchPhrase = "red shirt",
                RecordsToRetrieve = 1,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(1, results.DocCount);
            Assert.Equal(2, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanSort(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                Sort = new SearchSort("name"),
                RecordsToRetrieve = 1,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(1, results.DocCount);
            Assert.Equal(6, results.TotalCount);

            var productName = results.Documents.First()["name"] as string;
            Assert.Equal("Black Sox", productName);

            criteria = new KeywordSearchCriteria(_documentType)
            {
                Sort = new SearchSort("name", true),
                RecordsToRetrieve = 1,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(1, results.DocCount);
            Assert.Equal(6, results.TotalCount);

            productName = results.Documents.First()["name"] as string;
            Assert.Equal("Sample Product", productName);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanFilter(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var numericFilter = new AttributeFilter
            {
                Key = "Size",
                Values = new[]
                {
                    new AttributeFilterValue { Value = "1" },
                    new AttributeFilterValue { Value = "2" },
                    new AttributeFilterValue { Value = "3" },
                }
            };

            criteria.Apply(numericFilter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(2, results.DocCount);
            Assert.Equal(2, results.TotalCount);


            criteria = new KeywordSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var stringFilter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
               {
                    new AttributeFilterValue { Value = "Red" },
                    new AttributeFilterValue { Value = "Blue" },
                    new AttributeFilterValue { Value = "Black" },
                }
            };

            criteria.Apply(stringFilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(5, results.DocCount);
            Assert.Equal(5, results.TotalCount);


            criteria = new KeywordSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var rangefilter = new RangeFilter
            {
                Key = "size",
                Values = new[]
                {
                    new RangeFilterValue { Lower = "0", Upper = "5" },
                    new RangeFilterValue { Lower = "5", Upper = "10" },
                }
            };

            criteria.Apply(rangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(4, results.DocCount);
            Assert.Equal(4, results.TotalCount);


            criteria = new KeywordSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "default" },
                RecordsToRetrieve = 10,
            };

            var priceRangefilter = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue { Upper = "100" },
                    new RangeFilterValue { Lower = "700" },
                }
            };

            criteria.Apply(priceRangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(3, results.DocCount);
            Assert.Equal(3, results.TotalCount);


            criteria = new KeywordSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "sale", "default" },
                RecordsToRetrieve = 10,
            };

            priceRangefilter = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue { Upper = "100" },
                    new RangeFilterValue { Lower = "700" },
                }
            };

            criteria.Apply(priceRangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(4, results.DocCount);
            Assert.Equal(4, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanGetFacets(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "default" },
                RecordsToRetrieve = 0,
            };

            var attributeFacet = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue { Id = "red", Value = "red" },
                    new AttributeFilterValue { Id = "blue", Value = "blue" },
                    new AttributeFilterValue { Id = "black", Value = "black" },
                }
            };

            var rangeFacet = new RangeFilter
            {
                Key = "size",
                Values = new[]
                {
                    new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" },
                    new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                }
            };

            var priceRangeFacet = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue { Id = "0_to_100", Lower = "0", Upper = "100" },
                    new RangeFilterValue { Id = "100_to_700", Lower = "100", Upper = "700" },
                    new RangeFilterValue { Id = "over_700", Lower = "700" },
                    new RangeFilterValue { Id = "under_100", Upper = "100" },
                }
            };

            criteria.Add(attributeFacet);
            criteria.Add(rangeFacet);
            criteria.Add(priceRangeFacet);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(0, results.DocCount);

            var redCount = GetFacetCount(results, "Color", "red");
            Assert.True(redCount == 3, $"Returns {redCount} facets of red instead of 3");

            var sizeCount = GetFacetCount(results, "size", "0_to_5");
            Assert.True(sizeCount == 3, $"Returns {sizeCount} facets of 0_to_5 size instead of 3");

            var sizeCount2 = GetFacetCount(results, "size", "5_to_10");
            Assert.True(sizeCount2 == 1, $"Returns {sizeCount2} facets of 5_to_10 size instead of 1"); // only 1 result because upper bound is not included

            var priceCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceCount == 2, $"Returns {priceCount} facets of 0_to_100 prices instead of 2");

            var priceCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount2 == 3, $"Returns {priceCount2} facets of 100_to_700 prices instead of 3");

            var priceCount3 = GetFacetCount(results, "Price", "over_700");
            Assert.True(priceCount3 == 1, $"Returns {priceCount3} facets of over_700 prices instead of 1");

            var priceCount4 = GetFacetCount(results, "Price", "under_100");
            Assert.True(priceCount4 == 2, $"Returns {priceCount4} facets of priceCount4 prices instead of 2");
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_find_pricelists_prices(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
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

            criteria = new KeywordSearchCriteria(_documentType)
            {
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
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
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_get_item_outlines(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                RecordsToRetrieve = 6,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(results.DocCount == 6, $"Returns {results.DocCount} instead of 6");

            int outlineCount;
            var outlineObject = results.Documents.First()["__outline"]; // can be JArray or object[] depending on provider used
            if (outlineObject is JArray)
                outlineCount = (outlineObject as JArray).Count;
            else
                outlineCount = ((object[])outlineObject).Length;

            Assert.True(outlineCount == 2, $"Returns {outlineCount} outlines instead of 2");
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_get_item_multiple_filters(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new KeywordSearchCriteria(_documentType)
            {
                SearchPhrase = "",
                IsFuzzySearch = true,
                RecordsToRetrieve = 10,
                Currency = "USD",
                Pricelists = new[] { "default" }
            };

            var attributeFacet = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "red", Value = "Red"},
                    new AttributeFilterValue {Id = "blue", Value = "Blue"},
                    new AttributeFilterValue {Id = "black", Value = "Black"}
                }
            };

            var attributeFilter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "black", Value = "Black"}
                }
            };

            var rangeFacet = new RangeFilter
            {
                Key = "size",
                Values = new[]
                {
                    new RangeFilterValue {Id = "0_to_5", Lower = "0", Upper = "5"},
                    new RangeFilterValue {Id = "5_to_10", Lower = "5", Upper = "11"}
                }
            };

            var priceRangeFacet = new PriceRangeFilter
            {
                Currency = "usd",
                Values = new[]
                {
                    new RangeFilterValue {Id = "100_to_700", Lower = "100", Upper = "700"}
                }
            };

            criteria.Add(attributeFacet);
            criteria.Add(rangeFacet);
            criteria.Add(priceRangeFacet);

            // add applied filters
            criteria.Apply(attributeFilter);
            criteria.Apply(rangeFacet);
            criteria.Apply(priceRangeFacet);

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

            var group = results.Facets.SingleOrDefault(fg => fg.FieldName.EqualsInvariant(fieldName));

            return group?.Facets
                .Where(facet => facet.Key == facetKey)
                .Select(facet => facet.Count)
                .FirstOrDefault() ?? 0;
        }
    }
}
