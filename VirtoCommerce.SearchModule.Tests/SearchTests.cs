using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using Xunit;

namespace VirtoCommerce.SearchModule.Test
{
    [CLSCompliant(false)]
    [Collection("Search")]
    [Trait("Category", "CI")]
    public class SearchTests : SearchTestsBase
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
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanUpdateIndex(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType, true);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanGetStringCollection(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(6, results.DocCount);
            Assert.Equal(6, results.TotalCount);

            var stringCollection = results.Documents.First()["catalog"]; // can be JArray or object[] depending on provider used
            var itemsCount = (stringCollection as JArray)?.Count ?? ((object[])stringCollection).Length;

            Assert.True(itemsCount == 2, $"Returns {itemsCount} collection items instead of 2");
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanSort(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                Sort = new SearchSort(new[] { "non-existent-field", "Name" }),
                RecordsToRetrieve = 1,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(1, results.DocCount);
            Assert.Equal(6, results.TotalCount);

            var productName = results.Documents.First()["name"] as string;
            Assert.Equal("Black Sox", productName);

            criteria = new BaseSearchCriteria(_documentType)
            {
                Sort = new SearchSort("Name", true),
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
        public void CanSearchByIds(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                Ids = new[] { "Item-2", "Item-3" },
                RecordsToRetrieve = 10,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(2, results.DocCount);
            Assert.Equal(2, results.TotalCount);

            Assert.True(results.Documents.Any(d => (string)d.Id == "Item-2"), "Cannot find 'Item-2'");
            Assert.True(results.Documents.Any(d => (string)d.Id == "Item-3"), "Cannot find 'Item-3'");
        }

        [Theory]
        [InlineData("Lucene", "__content:Red __content:shirt", 2)]
        [InlineData("Elastic", "__content:Red __content:shirt", 2)]
        [InlineData("Azure", "f___content:Red f___content:shirt", 2)]
        [InlineData("Lucene", "__content:Red __content:sox", 0)]
        [InlineData("Elastic", "__content:Red __content:sox", 0)]
        [InlineData("Azure", "f___content:Red f___content:sox", 0)]
        [InlineData("Elastic", "price_usd:[100 TO 199]", 1)]
        public void CanSearchByRawQuery(string providerType, string rawQuery, long expectedDocumentsCount)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                RawQuery = rawQuery,
                RecordsToRetrieve = 10,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(expectedDocumentsCount, results.DocCount);
            Assert.Equal(expectedDocumentsCount, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanSearchByPhrase(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                SearchPhrase = " shirt ",
                RecordsToRetrieve = 10,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(3, results.DocCount);
            Assert.Equal(3, results.TotalCount);


            criteria = new BaseSearchCriteria(_documentType)
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
        public void CanUseFiltersInSearchPhrase(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Color:Red" };
            var results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(3, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:[2 TO 4]" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(3, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:[2 TO 4)" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(2, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:(2 TO 4]" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(2, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:(2 TO 4)" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(1, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:[10 TO 200]", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(4, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:[10 TO 200)", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(3, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:(10 TO 200]", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(3, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:(10 TO 200)", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(2, results.TotalCount);

            // Open-ended ranges

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:[TO 10]" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(5, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:(TO 10)" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(4, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:[10 TO]" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(2, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "Size:(10 TO]" };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(1, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:(TO 200]", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(4, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:[TO 200)", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(3, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:[200 TO)", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(3, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0, SearchPhrase = "price_usd:(200 TO)", Pricelists = new[] { "default" } };
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(2, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene", 5)]
        [InlineData("Elastic", 5)]
        [InlineData("Azure", 3)] // Azure does not support collections with non-string elements
        public void CanFilterByPriceWithoutAnyPricelist(string providerType, long expectedDocumentsCount)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                RecordsToRetrieve = 10,
            };

            var priceRangefilter = new PriceRangeFilter
            {
                Currency = "USD",
                Values = new[]
                {
                    new RangeFilterValue { Upper = "100" },
                    new RangeFilterValue { Lower = "700" },
                }
            };

            criteria.Apply(priceRangefilter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(expectedDocumentsCount, results.DocCount);
            Assert.Equal(expectedDocumentsCount, results.TotalCount);
        }


        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanFilterByDate(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", "2017-04-23T15:24:31.180Z", "2017-04-28T15:24:31.180Z", true, true));
            var results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(6, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", "2017-04-23T15:24:31.180Z", "2017-04-28T15:24:31.180Z", false, true));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(5, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", "2017-04-23T15:24:31.180Z", "2017-04-28T15:24:31.180Z", true, false));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(5, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", "2017-04-23T15:24:31.180Z", "2017-04-28T15:24:31.180Z", false, false));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(4, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", null, "2017-04-28T15:24:31.180Z", true, true));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(6, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", null, "2017-04-28T15:24:31.180Z", true, false));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(5, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", "2017-04-23T15:24:31.180Z", null, true, false));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(6, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(CreateRangeFilter("Date", "2017-04-23T15:24:31.180Z", null, false, false));
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(5, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanFilter(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            // Filtering by non-existent field name leads to empty result
            var criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var stringFilter = new AttributeFilter
            {
                Key = "non-existent-field",
                Values = new[] { new AttributeFilterValue { Value = "value-does-not-matter" } }
            };

            criteria.Apply(stringFilter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(0, results.DocCount);
            Assert.Equal(0, results.TotalCount);


            // Filtering by non-existent field value leads to empty result
            criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            stringFilter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue { Value = "White" }, // Non-existent value
                    new AttributeFilterValue { Value = "Green" }, // Non-existent value
                }
            };

            criteria.Apply(stringFilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(0, results.DocCount);
            Assert.Equal(0, results.TotalCount);


            criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            stringFilter = new AttributeFilter
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


            criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            stringFilter = new AttributeFilter
            {
                Key = "is",
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


            criteria = new BaseSearchCriteria(_documentType)
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

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(2, results.DocCount);
            Assert.Equal(2, results.TotalCount);


            criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var dateFilter = new AttributeFilter
            {
                Key = "Date",
                Values = new[]
                {
                    new AttributeFilterValue { Value = "2017-04-29T15:24:31.180Z" },
                    new AttributeFilterValue { Value = "2017-04-28T15:24:31.180Z" },
                    new AttributeFilterValue { Value = "2017-04-27T15:24:31.180Z" },
                }
            };

            criteria.Apply(dateFilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(2, results.DocCount);
            Assert.Equal(2, results.TotalCount);


            criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var rangefilter = new RangeFilter
            {
                Key = "Size",
                Values = new[]
                {
                    new RangeFilterValue { Lower = "0", Upper = "5" },
                    new RangeFilterValue { Lower = "", Upper = "5" },
                    new RangeFilterValue { Lower = null, Upper = "5" },
                    new RangeFilterValue { Lower = "5", Upper = "10" },
                }
            };

            criteria.Apply(rangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(4, results.DocCount);
            Assert.Equal(4, results.TotalCount);


            // Filter for non-existent pricelist should be ignored
            criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "default", "non-existent-pricelist" },
                RecordsToRetrieve = 10,
            };

            var priceRangefilter = new PriceRangeFilter
            {
                Currency = "USD",
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


            criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "default", "sale" },
                RecordsToRetrieve = 10,
            };

            criteria.Apply(priceRangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(3, results.DocCount);
            Assert.Equal(3, results.TotalCount);


            criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "sale", "default" },
                RecordsToRetrieve = 10,
            };

            criteria.Apply(priceRangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(4, results.DocCount);
            Assert.Equal(4, results.TotalCount);


            criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "supersale", "sale", "default" },
                RecordsToRetrieve = 10,
            };

            criteria.Apply(priceRangefilter);

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(5, results.DocCount);
            Assert.Equal(5, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanFilterByBool(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            // Value should be case insensitive

            var criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(new AttributeFilter { Key = "HasMultiplePrices", Values = new[] { new AttributeFilterValue { Value = "tRue" } } });
            var results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(2, results.TotalCount);

            criteria = new BaseSearchCriteria(_documentType) { RecordsToRetrieve = 0 };
            criteria.Apply(new AttributeFilter { Key = "HasMultiplePrices", Values = new[] { new AttributeFilterValue { Value = "fAlse" } } });
            results = provider.Search<DocumentDictionary>(_scope, criteria);
            Assert.Equal(4, results.TotalCount);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanGetFacets(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            // Facets for non-existent fields and pricelists should be ignored

            var criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "default", "non-existent-pricelist" },
                RecordsToRetrieve = 0,
            };

            var nonExistentFieldAttributeFacet = new AttributeFilter
            {
                Key = "non-existent-field",
                Values = new[] { new AttributeFilterValue { Id = "Red", Value = "Red" } }
            };

            var attributeFacet = new AttributeFilter
            {
                Key = "Color",
                FacetSize = 0,
                Values = new[]
                {
                    new AttributeFilterValue { Id = "Red", Value = "Red" },
                    new AttributeFilterValue { Id = "Blue", Value = "Blue" },
                    new AttributeFilterValue { Id = "Black", Value = "Black" },
                }
            };

            var stringCollectionAttributeFacet = new AttributeFilter
            {
                Key = "Catalog",
                FacetSize = 0,
                Values = new[]
                {
                    new AttributeFilterValue { Id = "Goods", Value = "Goods" },
                    new AttributeFilterValue { Id = "Stuff", Value = "Stuff" },
                }
            };

            var nonExistentFieldRangeFacet = new RangeFilter
            {
                Key = "non-existent-field",
                Values = new[] { new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" } }
            };

            var rangeFacet = new RangeFilter
            {
                Key = "Size",
                Values = new[]
                {
                    new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" },
                    new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                }
            };

            var priceRangeFacetUsd = new PriceRangeFilter
            {
                Currency = "USD",
                Values = new[]
                {
                    new RangeFilterValue { Id = "0_to_100", Lower = "0", Upper = "100" },
                    new RangeFilterValue { Id = "100_to_700", Lower = "100", Upper = "700" },
                    new RangeFilterValue { Id = "over_700", Lower = "700" },
                    new RangeFilterValue { Id = "under_100", Upper = "100" },
                }
            };

            // This facet should not present in search results because its currency does not equal to criteria currency
            var priceRangeFacetEur = new PriceRangeFilter
            {
                Currency = "EUR",
                Values = new[]
                {
                    new RangeFilterValue { Id = "0_to_100", Lower = "0", Upper = "100" },
                    new RangeFilterValue { Id = "100_to_700", Lower = "100", Upper = "700" },
                    new RangeFilterValue { Id = "over_700", Lower = "700" },
                    new RangeFilterValue { Id = "under_100", Upper = "100" },
                }
            };

            criteria.Add(nonExistentFieldAttributeFacet);
            criteria.Add(nonExistentFieldRangeFacet);
            criteria.Add(attributeFacet);
            criteria.Add(stringCollectionAttributeFacet);
            criteria.Add(rangeFacet);
            criteria.Add(priceRangeFacetUsd);
            criteria.Add(priceRangeFacetEur);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(0, results.DocCount);
            Assert.Equal(4, results.Facets.Count);

            var redCount = GetFacetCount(results, "Color", "Red");
            Assert.True(redCount == 3, $"Returns {redCount} facets of red instead of 3");

            var goodsCount = GetFacetCount(results, "Catalog", "Goods");
            Assert.True(goodsCount == 6, $"Returns {goodsCount} facets of Goods instead of 6");

            var stuffCount = GetFacetCount(results, "Catalog", "Stuff");
            Assert.True(stuffCount == 6, $"Returns {stuffCount} facets of Stuff instead of 6");

            var sizeCount = GetFacetCount(results, "Size", "0_to_5");
            Assert.True(sizeCount == 3, $"Returns {sizeCount} facets of 0_to_5 size instead of 3");

            var sizeCount2 = GetFacetCount(results, "Size", "5_to_10");
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
        public void CanGetPriceFacetsForDisjointPricelists(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "supersale", "sale" },
                RecordsToRetrieve = 10,
            };

            var priceRangeFacet = new PriceRangeFilter
            {
                Currency = "USD",
                Values = new[]
                {
                    new RangeFilterValue {Id = "under_90", Lower = "", Upper = "90"},
                    new RangeFilterValue {Id = "90_to_100", Lower = "90", Upper = "100"},
                    new RangeFilterValue {Id = "over_100", Lower = "100", Upper = ""},
                }
            };

            criteria.Add(priceRangeFacet);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(6, results.DocCount);

            var priceCount = GetFacetCount(results, "Price", "under_90");
            Assert.True(priceCount == 1, $"Returns {priceCount} facets of under_90 prices instead of 1");

            var priceCount2 = GetFacetCount(results, "Price", "90_to_100");
            Assert.True(priceCount2 == 1, $"Returns {priceCount2} facets of 90_to_100 prices instead of 1");

            var priceCount3 = GetFacetCount(results, "Price", "over_100");
            Assert.True(priceCount3 == 0, $"Returns {priceCount2} facets of over_100 prices instead of 0");
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        //[InlineData("Azure")] // Azure does not support complex facets with filters
        public void CanGetPriceFacetsForMultiplePricelists(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "default", "sale" },
                RecordsToRetrieve = 10,
            };

            var priceRangeFacet = new PriceRangeFilter
            {
                Currency = "USD",
                Values = new[]
                {
                    new RangeFilterValue {Id = "0_to_100", Lower = "0", Upper = "100"},
                    new RangeFilterValue {Id = "100_to_700", Lower = "100", Upper = "700"},
                }
            };

            criteria.Add(priceRangeFacet);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(6, results.DocCount);

            var priceCount = GetFacetCount(results, "Price", "0_to_100");
            Assert.True(priceCount == 2, $"Returns {priceCount} facets of 0_to_100 prices instead of 2");

            var priceCount2 = GetFacetCount(results, "Price", "100_to_700");
            Assert.True(priceCount2 == 3, $"Returns {priceCount2} facets of 100_to_700 prices instead of 3");


            criteria = new BaseSearchCriteria(_documentType)
            {
                Currency = "USD",
                Pricelists = new[] { "sale", "default" },
                RecordsToRetrieve = 10,
            };

            criteria.Add(priceRangeFacet);

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
        //[InlineData("Azure")] // Azure applies filters before calculating facets
        public void CanGetAllFacetValuesWhenFilterIsApplied(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new BaseSearchCriteria(_documentType)
            {
                RecordsToRetrieve = 10,
            };

            var facet = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "Red", Value = "Red"},
                    new AttributeFilterValue {Id = "Blue", Value = "Blue"},
                    new AttributeFilterValue {Id = "Black", Value = "Black"}
                }
            };

            var filter = new AttributeFilter
            {
                Key = "Color",
                Values = new[]
                {
                    new AttributeFilterValue {Id = "Red", Value = "Red"}
                }
            };

            criteria.Add(facet);
            criteria.Apply(filter);

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(3, results.DocCount);
            Assert.Equal(3, results.TotalCount);

            var redCount = GetFacetCount(results, "Color", "Red");
            Assert.True(redCount == 3, $"Returns {redCount} facets of Red instead of 3");

            var blueCount = GetFacetCount(results, "Color", "Blue");
            Assert.True(blueCount == 1, $"Returns {blueCount} facets of Blue instead of 1");

            var blackCount = GetFacetCount(results, "Color", "Black");
            Assert.True(blackCount == 1, $"Returns {blackCount} facets of Black instead of 1");
        }
    }
}
