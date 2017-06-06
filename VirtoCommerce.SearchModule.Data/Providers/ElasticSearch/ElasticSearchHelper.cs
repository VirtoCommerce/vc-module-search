using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Nest;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    public static class ElasticSearchHelper
    {
        public static string ToElasticFieldName(string fieldName)
        {
            return fieldName.ToLowerInvariant();
        }

        public static IFieldDescriptor Get(this IList<IFieldDescriptor> fields, string rawName)
        {
            var azureFieldName = ToElasticFieldName(rawName);
            return fields?.FirstOrDefault(f => f.Name.EqualsInvariant(azureFieldName));
        }

        public static QueryContainer CreateQuery<T>(ISearchCriteria criteria, ISearchFilter filter, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            QueryContainer result = null;

            var values = filter.GetValues();
            if (values != null)
            {
                var field = availableFields.Get(filter.Key);

                foreach (var value in values)
                {
                    var valueQuery = CreateQueryForValue<T>(criteria, filter, value, field);
                    result |= valueQuery;
                }
            }

            return result;
        }

        public static QueryContainer CreateQueryForValue<T>(ISearchCriteria criteria, ISearchFilter filter, ISearchFilterValue value, IFieldDescriptor field)
            where T : class
        {
            QueryContainer query = null;

            var fieldName = ToElasticFieldName(filter.Key);

            if (filter is AttributeFilter)
            {
                var termValue = ((AttributeFilterValue)value).Value;

                if (field?.DataType.EqualsInvariant("boolean") == true)
                {
                    termValue = termValue.ToLowerInvariant();
                }

                query = new TermQuery { Field = fieldName, Value = termValue };
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
        /// <param name="fieldName">The field name.</param>
        /// <param name="currency">The currency.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static QueryContainer CreatePriceRangeFilter<T>(ISearchCriteria criteria, string fieldName, string currency, RangeFilterValue value)
            where T : class
        {
            return CreatePriceRangeFilterValueQuery<T>(criteria.Pricelists, 0, fieldName, currency, value.Lower.AsDouble(), value.Upper.AsDouble(), value.IncludeLower, value.IncludeUpper);
        }

        public static QueryContainer CreatePriceRangeFilterValueQuery<T>(IList<string> pricelists, int index, string fieldName, string currency, double? lower, double? upper, bool includeLower, bool includeUpper)
            where T : class
        {
            QueryContainer result = null;

            if (pricelists.IsNullOrEmpty())
            {
                var priceFieldName = JoinNonEmptyStrings("_", fieldName, currency).ToLowerInvariant();
                result = CreateNumericRangeQuery<T>(priceFieldName, lower, upper, includeLower, includeUpper);
            }
            else if (index < pricelists.Count)
            {
                // Create negative query for previous pricelist
                QueryContainer previousPricelistQuery = null;
                if (index > 0)
                {
                    var previousFieldName = JoinNonEmptyStrings("_", fieldName, currency, pricelists[index - 1]).ToLowerInvariant();
                    previousPricelistQuery = CreateNumericRangeQuery<T>(previousFieldName, 0, null, false, false);
                }

                // Create positive query for current pricelist
                var currentFieldName = JoinNonEmptyStrings("_", fieldName, currency, pricelists[index]).ToLowerInvariant();
                var currentPricelistQuery = CreateNumericRangeQuery<T>(currentFieldName, lower, upper, includeLower, includeUpper);

                // Get query for next pricelist
                var nextPricelistQuery = CreatePriceRangeFilterValueQuery<T>(pricelists, index + 1, fieldName, currency, lower, upper, includeLower, includeUpper);

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
