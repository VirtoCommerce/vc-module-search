using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.LuceneSearch
{
    public static class LuceneSearchHelper
    {
        public const string BooleanFieldSuffix = ".boolean";

        public static string ToLuceneFieldName(string originalName)
        {
            return originalName.ToLowerInvariant();
        }

        public static string GetBooleanFieldName(string originalName)
        {
            return ToLuceneFieldName(originalName + BooleanFieldSuffix);
        }

        public static string ToStringInvariant(this object value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public static Filter CreateQuery(ISearchCriteria criteria, ISearchFilter filter, Occur clause, IList<IFieldDescriptor> availableFields)
        {
            Filter result = null;

            var values = filter.GetValues();
            if (values != null)
            {
                var query = new BooleanFilter();
                foreach (var value in values)
                {
                    var valueQuery = CreateQueryForValue(criteria, filter, value, availableFields);
                    if (valueQuery != null)
                    {
                        query.Add(new FilterClause(valueQuery, Occur.SHOULD));
                    }
                }

                result = query;
            }

            return result;
        }

        public static Filter CreateQueryForValue(ISearchCriteria criteria, ISearchFilter filter, ISearchFilterValue value, IList<IFieldDescriptor> availableFields)
        {
            Filter result = null;

            var fieldName = filter.Key.ToLowerInvariant();

            if (filter is AttributeFilter)
            {
                result = CreateTermsFilter(fieldName, value as AttributeFilterValue, availableFields);
            }
            else if (filter is RangeFilter)
            {
                result = CreateRangeFilter(fieldName, value as RangeFilterValue);
            }
            else if (filter is PriceRangeFilter)
            {
                var currency = ((PriceRangeFilter)filter).Currency;
                result = CreatePriceRangeFilter(criteria, fieldName, currency, value as RangeFilterValue);
            }
            else if (filter is CategoryFilter)
            {
                result = CreateCategoryFilter(fieldName, value as CategoryFilterValue);
            }

            return result;
        }

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <param name="availableFields"></param>
        /// <returns></returns>
        public static Filter CreateTermsFilter(string fieldName, AttributeFilterValue value, IList<IFieldDescriptor> availableFields)
        {
            var booleanFieldName = GetBooleanFieldName(fieldName);
            var booleanField = availableFields.FirstOrDefault(f => f.Name.EqualsInvariant(booleanFieldName));
            var termValue = booleanField != null ? bool.Parse(value.Value).ToStringInvariant() : ConvertToSearchable(value.Value);

            var query = new TermsFilter();
            query.AddTerm(new Term(fieldName, termValue));
            return query;
        }

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Filter CreateRangeFilter(string field, RangeFilterValue value)
        {
            Filter result = null;

            // If both bounds are empty, ignore this range
            if (!string.IsNullOrEmpty(value.Lower) || !string.IsNullOrEmpty(value.Upper))
            {
                var lowerLong = ConvertToDateTimeTicks(value.Lower);
                var upperLong = ConvertToDateTimeTicks(value.Upper);
                if (lowerLong != null || upperLong != null)
                {
                    result = NumericRangeFilter.NewLongRange(field, lowerLong, upperLong, value.IncludeLower, value.IncludeUpper);
                }
                else
                {
                    var lowerDouble = ConvertToDouble(value.Lower);
                    var upperDouble = ConvertToDouble(value.Upper);
                    if (lowerDouble != null || upperDouble != null)
                    {
                        result = NumericRangeFilter.NewDoubleRange(field, lowerDouble, upperDouble, value.IncludeLower, value.IncludeUpper);
                    }
                    else
                    {
                        result = new TermRangeFilter(field, value.Lower, value.Upper, value.IncludeLower, value.IncludeUpper);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates the query.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Filter CreateCategoryFilter(string field, CategoryFilterValue value)
        {
            var query = new BooleanFilter();
            if (!string.IsNullOrEmpty(value.Outline))
            {
                // workaround since there is no wildcard filter in current lucene version
                var outline = value.Outline.TrimEnd('*');
                var nodeQuery = new PrefixFilter(new Term(field, outline.ToLowerInvariant()));
                query.Add(new FilterClause(nodeQuery, Occur.MUST));
            }
            return query;
        }

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <param name="field">The field.</param>
        /// <param name="currency">The currency.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Filter CreatePriceRangeFilter(ISearchCriteria criteria, string field, string currency, RangeFilterValue value)
        {
            var lower = ConvertToDouble(value.Lower);
            var upper = ConvertToDouble(value.Upper);

            return CreatePriceRangeFilterValueQuery(criteria.Pricelists, 0, field, currency, lower, upper, value.IncludeLower, value.IncludeUpper);
        }


        private static Filter CreatePriceRangeFilterValueQuery(IList<string> pricelists, int index, string field, string currency, double? lower, double? upper, bool includeLower, bool includeUpper)
        {
            var result = new BooleanFilter();

            if (pricelists.IsNullOrEmpty())
            {
                var fieldName = JoinNonEmptyStrings("_", field, currency).ToLowerInvariant();
                var filter = NumericRangeFilter.NewDoubleRange(fieldName, lower, upper, includeLower, includeUpper);
                result.Add(new FilterClause(filter, Occur.MUST));
            }
            else if (index < pricelists.Count)
            {
                // Create negative query for previous pricelist
                if (index > 0)
                {
                    var previousFieldName = JoinNonEmptyStrings("_", field, currency, pricelists[index - 1]).ToLowerInvariant();
                    var previousPricelistFilter = NumericRangeFilter.NewDoubleRange(previousFieldName, 0, null, false, false);
                    result.Add(new FilterClause(previousPricelistFilter, Occur.MUST_NOT));
                }

                // Create positive query for current pricelist
                var currentFieldName = JoinNonEmptyStrings("_", field, currency, pricelists[index]).ToLowerInvariant();
                var currentPricelistFilter = NumericRangeFilter.NewDoubleRange(currentFieldName, lower, upper, includeLower, includeUpper);

                // Get query for next pricelist
                var nextPricelistFilter = CreatePriceRangeFilterValueQuery(pricelists, index + 1, field, currency, lower, upper, includeLower, includeUpper);

                if (nextPricelistFilter != null)
                {
                    result.Add(new FilterClause(currentPricelistFilter, Occur.SHOULD));
                    result.Add(new FilterClause(nextPricelistFilter, Occur.SHOULD));
                }
                else
                {
                    result.Add(new FilterClause(currentPricelistFilter, Occur.MUST));
                }
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

        public static string ConvertToSearchable(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var dateValue = ConvertToDateTimeTicks(value);
            if (dateValue != null)
            {
                return NumericUtils.LongToPrefixCoded(dateValue.Value);
            }

            var dobuleValue = ConvertToDouble(value);
            return dobuleValue != null ? NumericUtils.DoubleToPrefixCoded(dobuleValue.Value) : value;
        }

        public static long? ConvertToDateTimeTicks(string input)
        {
            long? result = null;

            DateTime value;
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                result = value.Ticks;
            }

            return result;
        }

        public static double? ConvertToDouble(string input)
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
