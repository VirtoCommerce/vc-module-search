using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Search.Filters;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.SearchModule.Data.Model;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    [CLSCompliant(false)]
    public class ElasticQueryHelper
    {
        public static BoolQuery CreateQuery<T>(ISearchCriteria criteria, ISearchFilter filter) where T:class
        {
            var values = GetFilterValues(filter);
            if (values == null)
                return null;

            var valueContainer = new List<QueryContainer>();
            var query = new BoolQuery();
            foreach (var value in values)
            {
                var valueQuery = CreateQueryForValue<T>(criteria, filter, value);
                valueContainer.Add(valueQuery);                
            }

            query.Should = valueContainer;
            return query;
        }

        public static BoolQuery CreateQueryForValue<T>(ISearchCriteria criteria, ISearchFilter filter, ISearchFilterValue value) where T : class
        {
            //var query = new List<QueryContainer>();
            var query = new BoolQuery();
            var field = filter.Key.ToLower();
            if (filter.GetType() == typeof(PriceRangeFilter))
            {
                var tempQuery = CreatePriceRangeFilter<T>(criteria, field, value as RangeFilterValue);
                if (tempQuery != null)
                {
                    var container = new List<QueryContainer>();
                    container.Add(tempQuery);
                    query.Must = container;
                }
            }
            else
            {
                if (value.GetType() == typeof(AttributeFilterValue))
                {
                    var container = new List<QueryContainer>();
                    var termQuery = new TermQuery() { Field = field, Value = ((AttributeFilterValue)value).Value };
                    container.Add(termQuery);
                    query.Must = container;
                }
                else if (value.GetType() == typeof(RangeFilterValue))
                {
                    var container = new List<QueryContainer>();
                    var tempValue = value as RangeFilterValue;
                    var tempFilter = new TermRangeQuery() { Field = field, GreaterThanOrEqualTo = tempValue.Lower, LessThan = tempValue.Upper };
                    container.Add(tempFilter);
                    query.Should = container;
                }
            }

            return query;
        }

        public static ISearchFilterValue[] GetFilterValues(ISearchFilter filter)
        {
            ISearchFilterValue[] values = null;
            if (filter is AttributeFilter)
            {
                values = ((AttributeFilter)filter).Values;
            }
            else if (filter is RangeFilter)
            {
                values = ((RangeFilter)filter).Values;
            }
            else if (filter is PriceRangeFilter)
            {
                values = ((PriceRangeFilter)filter).Values;
            }
            else if (filter is CategoryFilter)
            {
                values = ((CategoryFilter)filter).Values;
            }

            return values;
        }

        /// <summary>
        /// Creates the price range filter.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static BoolQuery CreatePriceRangeFilter<T>(ISearchCriteria criteria, string field, RangeFilterValue value) where T:class
        {
            //var query = new BoolQuery();

            var lowerbound = value.Lower;
            var upperbound = value.Upper;

            double lowerboundVal, upperboundVal;
            double.TryParse(lowerbound, out lowerboundVal);
            double.TryParse(upperbound, out upperboundVal);

            var lowerboundincluded = true;
            var upperboundincluded = false;

            var currency = criteria.Currency.ToLower();

            // format is "fieldname_store_currency_pricelist"
            string[] pls = null;
            if (criteria is Model.CatalogIndexedSearchCriteria)
            {
                pls = ((Model.CatalogIndexedSearchCriteria)criteria).Pricelists;
            }

            var parentPriceList = string.Empty;

            // Create  filter of type 
            // price_USD_pricelist1:[100 TO 200} (-price_USD_pricelist1:[* TO *} +(price_USD_pricelist2:[100 TO 200} (-price_USD_pricelist2:[* TO *} (+price_USD_pricelist3[100 TO 200}))))

            if (pls == null || !pls.Any())
                return null;

            var priceListId = pls[0].ToLower();

            var range_query = Query<T>.Range(r=>r
                    .Field(string.Format("{0}_{1}_{2}", field, currency, priceListId))
                    .GreaterThanOrEquals(lowerboundVal)
                    .LessThan(upperboundVal)
                );

            var queryContainer = new List<QueryContainer>();

            queryContainer.Add(range_query);
            if (pls.Count() > 1)
            {
                var temp = CreatePriceRangeFilter<T>(pls, 1, field, currency, lowerboundVal, upperboundVal, lowerboundincluded, upperboundincluded);
                queryContainer.Add(temp);
            }

            var query = new BoolQuery();
            query.Should = queryContainer;
            return query;
        }

        private static BoolQuery CreatePriceRangeFilter<T>(string[] priceLists, int index, string field, string currency, double lowerbound, double upperbound, bool lowerboundincluded, bool upperboundincluded) where T:class
        {
            var query = new BoolQuery();

            // create left part
            var not_range_query = Query<T>.Range(r => r
                    .Field(string.Format("{0}_{1}_{2}", field, currency, priceLists[index - 1].ToLower()))
                );

            //range_query.Field(string.Format("{0}_{1}_{2}", field, currency, priceLists[index - 1].ToLower()))/*.From("*").To("*")*/.IncludeLower(lowerboundincluded).IncludeUpper(upperboundincluded);
            var queryContainer = new List<QueryContainer>();
            queryContainer.Add(not_range_query);
            query.MustNot = queryContainer;

            // create right part
            if (index == priceLists.Count() - 1) // last element
            {
                var range_query = Query<T>.Range(r => r
                        .Field(string.Format("{0}_{1}_{2}", field, currency, priceLists[index].ToLower()))
                        .GreaterThanOrEquals(lowerbound)
                        .LessThan(upperbound)
                    );

                var rangeQueryContainer = new List<QueryContainer>();
                rangeQueryContainer.Add(range_query);
                query.Must = rangeQueryContainer;
            }
            else
            {
                var rangeQueryContainer = new List<QueryContainer>();
                var range_query = CreatePriceRangeFilter<T>(priceLists, index + 1, field, currency, lowerbound, upperbound, lowerboundincluded, upperboundincluded);
                query.Should = rangeQueryContainer;
            }

            return query;
        }
    }
}
