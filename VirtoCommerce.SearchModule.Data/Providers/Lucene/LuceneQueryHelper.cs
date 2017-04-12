using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Services;

namespace VirtoCommerce.SearchModule.Data.Providers.Lucene
{
    public class LuceneQueryHelper
    {
        /// <summary>
        /// Converts to searchable.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="tryConvertToNumber">if set to <c>true</c> [try convert to number].</param>
        /// <returns></returns>
        public static string ConvertToSearchable(object value, bool tryConvertToNumber = true)
        {
            var stringValue = value?.ToString();

            if (string.IsNullOrEmpty(stringValue))
            {
                return string.Empty;
            }

            if (tryConvertToNumber)
            {
                decimal decimalVal;
                int intVal;

                // Try converting to a known type
                if (decimal.TryParse(stringValue, out decimalVal))
                {
                    value = decimalVal;
                }
                else if (int.TryParse(stringValue, out intVal))
                {
                    value = intVal;
                }
            }

            if (value is string)
            {
                return stringValue;
            }

            if (value is decimal)
            {
                return NumericUtils.DoubleToPrefixCoded(Convert.ToDouble(value));
            }

            if (value.GetType() != typeof(int) || value.GetType() != typeof(long) || value.GetType() != typeof(double))
            {
                return stringValue;
            }

            return NumericUtils.DoubleToPrefixCoded((double)value);
        }

        public static Filter CreateQuery(ISearchCriteria criteria, ISearchFilter filter, Occur clause)
        {
            Filter result = null;

            var values = filter.GetValues();
            if (values != null)
            {
                var query = new BooleanFilter();
                foreach (var value in values)
                {
                    var valueQuery = CreateQueryForValue(criteria, filter, value);
                    query.Add(new FilterClause(valueQuery, Occur.SHOULD));
                }

                result = query;
            }

            return result;
        }

        public static Filter CreateQueryForValue(ISearchCriteria criteria, ISearchFilter filter, ISearchFilterValue value)
        {
            Filter result = null;

            var fieldName = filter.Key.ToLowerInvariant();

            if (filter is AttributeFilter)
            {
                result = CreateTermsFilter(fieldName, value as AttributeFilterValue);
            }
            else if (filter is RangeFilter)
            {
                result = CreateTermRangeFilter(fieldName, value as RangeFilterValue);
            }
            else if (filter is PriceRangeFilter)
            {
                result = CreatePriceRangeFilter(criteria, fieldName, value as RangeFilterValue);
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
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Filter CreateTermsFilter(string field, AttributeFilterValue value)
        {
            object val = value.Value;
            var query = new TermsFilter();
            query.AddTerm(new Term(field, ConvertToSearchable(val)));
            return query;
        }

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Filter CreateTermRangeFilter(string field, RangeFilterValue value)
        {
            object lowerbound = value.Lower;
            object upperbound = value.Upper;

            var query = new TermRangeFilter(field, ConvertToSearchable(lowerbound), ConvertToSearchable(upperbound), true, false);
            return query;
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
                var nodeQuery = new PrefixFilter(new Term(field, outline.ToLower()));
                query.Add(new FilterClause(nodeQuery, Occur.MUST));
            }
            return query;
        }

        /// <summary>
        ///     Creates the query.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Filter CreatePriceRangeFilter(ISearchCriteria criteria, string field, RangeFilterValue value)
        {
            var lowerBound = value.Lower != null ? ConvertToSearchable(value.Lower) : NumericUtils.LongToPrefixCoded(long.MinValue);
            var upperBound = value.Upper != null ? ConvertToSearchable(value.Upper) : NumericUtils.LongToPrefixCoded(long.MaxValue);

            return CreatePriceRangeFilterValueQuery(criteria.Pricelists, 0, field, criteria.Currency, lowerBound, upperBound, true, false);
        }


        private static Filter CreatePriceRangeFilterValueQuery(IList<string> pricelists, int index, string field, string currency, string lowerBound, string upperBound, bool lowerBoundIncluded, bool upperBoundIncluded)
        {
            var result = new BooleanFilter();

            if (pricelists.IsNullOrEmpty())
            {
                var fieldName = string.Join("_", field, currency).ToLower();
                var filter = new TermRangeFilter(fieldName, lowerBound, upperBound, lowerBoundIncluded, upperBoundIncluded);
                result.Add(new FilterClause(filter, Occur.MUST));
            }
            else if (index < pricelists.Count)
            {
                // Create negative query for previous pricelist
                if (index > 0)
                {
                    var previousFieldName = string.Join("_", field, currency, pricelists[index - 1]).ToLower();
                    var previousPricelistFilter = new TermRangeFilter(previousFieldName, NumericUtils.LongToPrefixCoded(long.MinValue), NumericUtils.LongToPrefixCoded(long.MaxValue), true, false);
                    result.Add(new FilterClause(previousPricelistFilter, Occur.MUST_NOT));
                }

                // Create positive query for current pricelist
                var currentFieldName = string.Join("_", field, currency, pricelists[index]).ToLower();
                var currentPricelistFilter = new TermRangeFilter(currentFieldName, lowerBound, upperBound, lowerBoundIncluded, upperBoundIncluded);

                // Get query for next pricelist
                var nextPricelistFilter = CreatePriceRangeFilterValueQuery(pricelists, index + 1, field, currency, lowerBound, upperBound, lowerBoundIncluded, upperBoundIncluded);

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

    }
}
