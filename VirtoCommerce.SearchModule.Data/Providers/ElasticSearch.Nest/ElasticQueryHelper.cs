using System.Globalization;
using System.Text;
using Nest;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    public class ElasticQueryHelper
    {
        public static QueryContainer CreateQuery<T>(ISearchCriteria criteria, ISearchFilter filter)
            where T : class
        {
            QueryContainer result = null;

            var values = GetFilterValues(filter);
            if (values != null)
            {
                foreach (var value in values)
                {
                    var valueQuery = CreateQueryForValue<T>(criteria, filter, value);
                    result |= valueQuery;
                }
            }

            return result;
        }

        public static QueryContainer CreateQueryForValue<T>(ISearchCriteria criteria, ISearchFilter filter, ISearchFilterValue value)
            where T : class
        {
            QueryContainer query = null;

            var fieldName = filter.Key.ToLower();

            if (filter is AttributeFilter)
            {
                query = new TermQuery { Field = fieldName, Value = ((AttributeFilterValue)value).Value.ToLowerInvariant() };
            }
            else if (filter is RangeFilter)
            {
                var rangeFilterValue = value as RangeFilterValue;
                query = new TermRangeQuery { Field = fieldName, GreaterThanOrEqualTo = rangeFilterValue?.Lower, LessThan = rangeFilterValue?.Upper };
            }
            else if (filter is PriceRangeFilter)
            {
                query = CreatePriceRangeFilter<T>(criteria, fieldName, value as RangeFilterValue);
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
        public static QueryContainer CreatePriceRangeFilter<T>(ISearchCriteria criteria, string field, RangeFilterValue value)
            where T : class
        {
            return CreatePriceRangeFilterValueQuery<T>(criteria.Pricelists, 0, field, criteria.Currency, value.Lower.AsDouble(), value.Upper.AsDouble());
        }

        private static QueryContainer CreatePriceRangeFilterValueQuery<T>(string[] priceLists, int index, string field, string currency, double? lowerbound, double? upperbound)
            where T : class
        {
            QueryContainer result = null;

            if (priceLists.IsNullOrEmpty())
            {
                var fieldName = JoinNonEmptyStrings("_", field, currency).ToLower();
                result = Query<T>.Range(r => r.Field(fieldName).GreaterThanOrEquals(lowerbound).LessThan(upperbound));
            }
            else if (index < priceLists.Length)
            {
                // Create negative query for previous pricelist
                QueryContainer previousPricelistQuery = null;
                if (index > 0)
                {
                    var previousFieldName = JoinNonEmptyStrings("_", field, currency, priceLists[index - 1]).ToLower();
                    previousPricelistQuery = Query<T>.Range(r => r.Field(previousFieldName).GreaterThan(0));
                }

                // Create positive query for current pricelist
                var currentFieldName = JoinNonEmptyStrings("_", field, currency, priceLists[index]).ToLower();
                var currentPricelistQuery = Query<T>.Range(r => r.Field(currentFieldName).GreaterThanOrEquals(lowerbound).LessThan(upperbound));

                // Get expression for next pricelist
                var nextPricelistQuery = CreatePriceRangeFilterValueQuery<T>(priceLists, index + 1, field, currency, lowerbound, upperbound);

                result = !previousPricelistQuery & (currentPricelistQuery | nextPricelistQuery);
            }

            return result;
        }

        public static string JoinNonEmptyStrings(string separator, params string[] values)
        {
            var builder = new StringBuilder();
            var valuesCount = 0;

            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (valuesCount > 0)
                    {
                        builder.Append(separator);
                    }

                    builder.Append(value);
                    valuesCount++;
                }
            }

            return builder.ToString();
        }
    }

    public static class DoubleExtensions
    {
        public static double? AsDouble(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return new double?();

            double convertedValue;
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out convertedValue))
            {
                return convertedValue;
            }

            return new double?();
        }
    }
}
