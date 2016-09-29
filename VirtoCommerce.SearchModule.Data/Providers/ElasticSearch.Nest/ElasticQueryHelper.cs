using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nest;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    public class ElasticQueryHelper
    {
        public static BoolQuery CreateQuery<T>(ISearchCriteria criteria, ISearchFilter filter)
            where T : class
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

        public static BoolQuery CreateQueryForValue<T>(ISearchCriteria criteria, ISearchFilter filter, ISearchFilterValue value)
            where T : class
        {
            //var query = new List<QueryContainer>();
            var query = new BoolQuery();
            var field = filter.Key.ToLower();
            if (filter.GetType() == typeof(PriceRangeFilter))
            {
                var tempQuery = CreatePriceRangeFilter<T>(criteria, field, value as RangeFilterValue);
                if (tempQuery != null)
                {
                    query.Must = new List<QueryContainer> { tempQuery };
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
        public static BoolQuery CreatePriceRangeFilter<T>(ISearchCriteria criteria, string field, RangeFilterValue value)
            where T : class
        {
            var lowerbound = value.Lower;
            var upperbound = value.Upper;

            const bool lowerBoundIncluded = true;
            const bool upperBoundIncluded = false;

            // format is "fieldname_store_currency_pricelist"
            var pls = criteria.Pricelists;
            var currency = criteria.Currency.ToLower();

            // Create  filter of type 
            // price_USD_pricelist1:[100 TO 200} (-price_USD_pricelist1:[* TO *} +(price_USD_pricelist2:[100 TO 200} (-price_USD_pricelist2:[* TO *} (+price_USD_pricelist3[100 TO 200}))))

            var priceListId = string.Empty;
            if (pls != null && pls.Any())
            {
                priceListId = pls[0].ToLower();
            }

            var fieldName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}{2}", field, currency, string.IsNullOrEmpty(priceListId) ? "" : "_" + priceListId);

            var topRangeQuery = Query<T>.Range(r => r
                    .Field(fieldName)
                    .GreaterThanOrEquals(lowerbound.AsDouble())
                    .LessThan(upperbound.AsDouble())
                );

            var queryContainer = new List<QueryContainer> { topRangeQuery };

            if (pls != null && pls.Length > 1)
            {
                var subRangeQuery = CreatePriceRangeFilter<T>(pls, 1, field, currency, lowerbound.AsDouble(), upperbound.AsDouble(), lowerBoundIncluded, upperBoundIncluded);
                var subRangeBoolQuery = Query<T>.Bool(b => b.Should(subRangeQuery));
                queryContainer.Add(subRangeBoolQuery);
            }

            return new BoolQuery { Should = queryContainer };
        }

        private static BoolQuery CreatePriceRangeFilter<T>(string[] priceLists, int index, string field, string currency, double? lowerbound, double? upperbound, bool lowerboundincluded, bool upperboundincluded)
            where T : class
        {
            var query = new BoolQuery();

            // create left part
            var notRangeQuery = Query<T>.Range(r => r
                    .Field(string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", field, currency, priceLists[index - 1].ToLower())).GreaterThan(int.MinValue)
                );

            //range_query.Field(string.Format("{0}_{1}_{2}", field, currency, priceLists[index - 1].ToLower()))/*.From("*").To("*")*/.IncludeLower(lowerboundincluded).IncludeUpper(upperboundincluded);
            query.MustNot = new List<QueryContainer> { notRangeQuery };

            // create right part
            if (index == priceLists.Length - 1) // last element
            {
                var rangeQuery = Query<T>.Range(r => r
                        .Field(string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", field, currency, priceLists[index].ToLower()))
                        .GreaterThanOrEquals(lowerbound)
                        .LessThan(upperbound)
                    );

                var rangeQueryContainer = new List<QueryContainer>();
                var subRangeBoolQuery = Query<T>.Bool(b => b.Should(rangeQuery));
                rangeQueryContainer.Add(subRangeBoolQuery);
                query.Must = rangeQueryContainer;
            }
            else
            {
                var rangeQueryContainer = new List<QueryContainer>();
                var rangeQuery = CreatePriceRangeFilter<T>(priceLists, index + 1, field, currency, lowerbound, upperbound, lowerboundincluded, upperboundincluded);
                var subRangeBoolQuery = Query<T>.Bool(b => b.Should(rangeQuery));
                rangeQueryContainer.Add(subRangeBoolQuery);
                query.Should = rangeQueryContainer;
            }

            return query;
        }
    }

    public static class DoubleExtensions
    {
        public static double? AsDouble(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return new double?();

            double convertedValue;
            if (double.TryParse(input, out convertedValue))
            {
                return convertedValue;
            }

            return new double?();
        }
    }
}
