using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Nest;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    public class ElasticSearchHelper
    {
        public static QueryContainer CreateQuery<T>(ISearchCriteria criteria, ISearchFilter filter)
            where T : class
        {
            QueryContainer result = null;

            var values = filter.GetValues();
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

            var fieldName = filter.Key.ToLowerInvariant();

            if (filter is AttributeFilter)
            {
                query = new TermQuery { Field = fieldName, Value = ((AttributeFilterValue)value).Value };
            }
            else if (filter is RangeFilter)
            {
                query = CreateTermRangeQuery(fieldName, value as RangeFilterValue);
            }
            else if (filter is PriceRangeFilter)
            {
                var currency = ((PriceRangeFilter)filter).Currency;
                query = CreatePriceRangeFilter<T>(criteria, fieldName, currency, value as RangeFilterValue);
            }

            return query;
        }

        public static TermRangeQuery CreateTermRangeQuery(string fieldName, RangeFilterValue value)
        {
            var lower = string.IsNullOrEmpty(value.Lower) ? null : value.Lower;
            var upper = string.IsNullOrEmpty(value.Upper) ? null : value.Upper;
            return CreateTermRangeQuery(fieldName, lower, upper, value.IncludeLower, value.IncludeUpper);
        }

        public static TermRangeQuery CreateTermRangeQuery(string fieldName, string lower, string upper, bool includeLower, bool includeUpper)
        {
            var termRangeQuery = new TermRangeQuery { Field = fieldName };

            if (includeLower)
            {
                termRangeQuery.GreaterThanOrEqualTo = lower;
            }
            else
            {
                termRangeQuery.GreaterThan = lower;
            }

            if (includeUpper)
            {
                termRangeQuery.LessThanOrEqualTo = upper;
            }
            else
            {
                termRangeQuery.LessThan = upper;
            }

            return termRangeQuery;
        }

        /// <summary>
        /// Creates the price range filter.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="field">The field.</param>
        /// <param name="currency">The currency.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static QueryContainer CreatePriceRangeFilter<T>(ISearchCriteria criteria, string field, string currency, RangeFilterValue value)
            where T : class
        {
            return CreatePriceRangeFilterValueQuery<T>(criteria.Pricelists, 0, field, currency, value.Lower.AsDouble(), value.Upper.AsDouble(), value.IncludeLower, value.IncludeUpper);
        }

        public static QueryContainer CreatePriceRangeFilterValueQuery<T>(IList<string> pricelists, int index, string field, string currency, double? lower, double? upper, bool includeLower, bool includeUpper)
            where T : class
        {
            QueryContainer result = null;

            if (pricelists.IsNullOrEmpty())
            {
                var fieldName = JoinNonEmptyStrings("_", field, currency).ToLowerInvariant();
                result = CreateNumericRangeQuery<T>(fieldName, lower, upper, includeLower, includeUpper);
            }
            else if (index < pricelists.Count)
            {
                // Create negative query for previous pricelist
                QueryContainer previousPricelistQuery = null;
                if (index > 0)
                {
                    var previousFieldName = JoinNonEmptyStrings("_", field, currency, pricelists[index - 1]).ToLowerInvariant();
                    previousPricelistQuery = CreateNumericRangeQuery<T>(previousFieldName, 0, null, false, false);
                }

                // Create positive query for current pricelist
                var currentFieldName = JoinNonEmptyStrings("_", field, currency, pricelists[index]).ToLowerInvariant();
                var currentPricelistQuery = CreateNumericRangeQuery<T>(currentFieldName, lower, upper, includeLower, includeUpper);

                // Get query for next pricelist
                var nextPricelistQuery = CreatePriceRangeFilterValueQuery<T>(pricelists, index + 1, field, currency, lower, upper, includeLower, includeUpper);

                result = !previousPricelistQuery & (currentPricelistQuery | nextPricelistQuery);
            }

            return result;
        }

        public static QueryContainer CreateNumericRangeQuery<T>(string fieldName, double? lower, double? upper, bool includeLower, bool includeUpper)
            where T : class
        {
            var range = new NumericRangeQueryDescriptor<T>().Field(fieldName);

            range = includeLower ? range.GreaterThanOrEquals(lower) : range.GreaterThan(lower);
            range = includeUpper ? range.LessThanOrEquals(upper) : range.LessThan(upper);

            return Query<T>.Range(r => range);
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

    public static class StringExtensions
    {
        public static double? AsDouble(this string input)
        {
            double? result = null;

            double value;
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                result = value;
            }

            return result;
        }
    }
}
