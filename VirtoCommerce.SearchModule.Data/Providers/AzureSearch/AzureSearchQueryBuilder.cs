using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Azure.Search.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.AzureSearch
{
    [CLSCompliant(false)]
    public class AzureSearchQueryBuilder : ISearchQueryBuilder
    {
        public string DocumentType => string.Empty;

        public virtual object BuildQuery<T>(string scope, ISearchCriteria criteria)
            where T : class
        {
            var result = new AzureSearchQuery
            {
                SearchText = GetSearchText(criteria),
                SearchParameters = new SearchParameters
                {
                    QueryType = GetQueryType(criteria),
                    Filter = GetFilters(criteria),
                    Facets = GetFacets(criteria),
                    OrderBy = GetSorting(criteria),
                    Skip = criteria.StartingRecord,
                    Top = criteria.RecordsToRetrieve,
                    IncludeTotalResultCount = true,
                    SearchMode = SearchMode.All,
                }
            };

            return result;
        }


        protected virtual QueryType GetQueryType(ISearchCriteria criteria)
        {
            return string.IsNullOrEmpty(criteria?.RawQuery) ? QueryType.Simple : QueryType.Full;
        }

        protected virtual string GetSearchText(ISearchCriteria criteria)
        {
            return criteria?.RawQuery ?? (criteria as KeywordSearchCriteria)?.SearchPhrase;
        }

        protected virtual string GetFilters(ISearchCriteria criteria)
        {
            IList<string> filters = new List<string>();

            AddFilters(criteria, filters);

            var result = string.Join(" and ", filters.Where(f => !string.IsNullOrEmpty(f)));
            return result;
        }

        protected virtual void AddFilters(ISearchCriteria criteria, IList<string> filters)
        {
            AddIdsFilter(criteria, filters);
            AddCurrentFilters(criteria, filters);
        }

        protected virtual void AddIdsFilter(ISearchCriteria criteria, IList<string> filters)
        {
            if (criteria?.Ids != null && criteria.Ids.Any())
            {
                var filter = GetEqualsFilterExpression(AzureSearchHelper.RawKeyFieldName, criteria.Ids, false);
                filters.Add(filter);
            }
        }

        protected virtual void AddCurrentFilters(ISearchCriteria criteria, IList<string> filters)
        {
            foreach (var filter in criteria.CurrentFilters)
            {
                var attributeFilter = filter as AttributeFilter;
                var rangeFilter = filter as RangeFilter;
                var priceRangeFilter = filter as PriceRangeFilter;

                string expression = null;

                if (attributeFilter != null)
                {
                    expression = GetAttributeFilterExpression(attributeFilter, criteria);
                }
                else if (rangeFilter != null)
                {
                    expression = GetRangeFilterExpression(rangeFilter, criteria);
                }
                else if (priceRangeFilter != null && priceRangeFilter.Currency.EqualsInvariant(criteria.Currency))
                {
                    expression = GetPriceRangeFilterExpression(priceRangeFilter, criteria);
                }

                if (!string.IsNullOrEmpty(expression))
                {
                    filters.Add(expression);
                }
            }
        }

        protected virtual string GetAttributeFilterExpression(AttributeFilter filter, ISearchCriteria criteria)
        {
            return GetEqualsFilterExpression(filter.Key, filter.Values.Select(v => v.Value), true);
        }

        protected virtual string GetContainsFilterExpression(string rawName, IEnumerable<string> rawValues)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(rawName).ToLower();
            var values = rawValues.Where(v => !string.IsNullOrEmpty(v)).Select(GetStringFilterValue).ToArray();
            return AzureSearchHelper.JoinNonEmptyStrings(" or ", true, values.Select(v => $"{azureFieldName}/any(v: v eq {v})").ToArray());
        }

        protected virtual string GetEqualsFilterExpression(string rawName, IEnumerable<string> rawValues, bool parseValues)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(rawName).ToLower();
            var values = rawValues.Where(v => !string.IsNullOrEmpty(v)).Select(v => GetFilterValue(v, parseValues)).ToArray();
            return AzureSearchHelper.JoinNonEmptyStrings(" or ", true, values.Select(v => $"{azureFieldName} eq {v}").ToArray());
        }

        protected virtual string GetRangeFilterExpression(RangeFilter filter, ISearchCriteria criteria)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key).ToLower();

            var expressions = filter.Values
                .Select(v => GetRangeFilterValueExpression(v, azureFieldName))
                .Where(e => !string.IsNullOrEmpty(e))
                .ToArray();

            var result = AzureSearchHelper.JoinNonEmptyStrings(" or ", true, expressions);
            return result;
        }

        protected virtual string GetPriceRangeFilterExpression(PriceRangeFilter filter, ISearchCriteria criteria)
        {
            string result = null;

            if (filter.Currency.EqualsInvariant(criteria.Currency))
            {
                var expressions = filter.Values
                    .Select(v => GetPriceRangeFilterValueExpression(0, filter, v, criteria))
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();

                result = AzureSearchHelper.JoinNonEmptyStrings(" or ", true, expressions);
            }

            return result;
        }

        protected virtual string GetPriceRangeFilterValueExpression(int pricelistIndex, PriceRangeFilter filter, RangeFilterValue filterValue, ISearchCriteria criteria)
        {
            string result = null;

            if (criteria.Pricelists.IsNullOrEmpty())
            {
                var fieldName = string.Join("_", filter.Key, criteria.Currency);
                var azureFieldName = AzureSearchHelper.ToAzureFieldName(fieldName).ToLower();
                result = GetRangeFilterValueExpression(filterValue, azureFieldName);
            }
            else if (pricelistIndex < criteria.Pricelists.Count)
            {
                // Get negative expression for previous pricelist
                string previousPricelistExpression = null;
                if (pricelistIndex > 0)
                {
                    var previousFieldName = string.Join("_", filter.Key, criteria.Currency, criteria.Pricelists[pricelistIndex - 1]);
                    var previousAzureFieldName = AzureSearchHelper.ToAzureFieldName(previousFieldName).ToLower();
                    previousPricelistExpression = $"not({previousAzureFieldName} gt 0)";
                }

                // Get positive expression for current pricelist
                var currentFieldName = string.Join("_", filter.Key, criteria.Currency, criteria.Pricelists[pricelistIndex]);
                var currentAzureFieldName = AzureSearchHelper.ToAzureFieldName(currentFieldName).ToLower();
                var currentPricelistExpresion = GetRangeFilterValueExpression(filterValue, currentAzureFieldName);

                // Get expression for next pricelist
                var nextPricelistExpression = GetPriceRangeFilterValueExpression(pricelistIndex + 1, filter, filterValue, criteria);

                var currentExpression = AzureSearchHelper.JoinNonEmptyStrings(" or ", true, currentPricelistExpresion, nextPricelistExpression);
                result = AzureSearchHelper.JoinNonEmptyStrings(" and ", false, previousPricelistExpression, currentExpression);
            }

            return result;
        }

        protected virtual string GetRangeFilterValueExpression(RangeFilterValue filterValue, string azureFieldName)
        {
            string result = null;

            var lower = ConvertToDecimal(filterValue.Lower);
            var upper = ConvertToDecimal(filterValue.Upper);

            if (lower != null || upper != null)
            {
                var builder = new StringBuilder();

                if (lower != null)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0} ge {1}", azureFieldName, lower);

                    if (upper != null)
                    {
                        builder.Append(" and ");
                    }
                }

                if (upper != null)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0} lt {1}", azureFieldName, upper);
                }

                result = builder.ToString();
            }

            return result;
        }

        protected virtual string GetFilterValue(string rawValue, bool parseValue)
        {
            string result = null;

            if (parseValue)
            {
                long integerValue;
                double doubleValue;

                if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out integerValue))
                {
                    result = rawValue;
                }
                else if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                {
                    result = rawValue;
                }
            }

            return result ?? GetStringFilterValue(rawValue);
        }

        protected virtual string GetStringFilterValue(string rawValue)
        {
            return $"'{rawValue.Replace("'", "''")}'";
        }

        protected virtual IList<string> GetSorting(ISearchCriteria criteria)
        {
            IList<string> result = null;

            if (criteria.Sort != null)
            {
                var fields = criteria.Sort.GetSort();

                foreach (var field in fields)
                {
                    if (result == null)
                    {
                        result = new List<string>();
                    }

                    result.Add(string.Join(" ", AzureSearchHelper.ToAzureFieldName(field.FieldName), field.IsDescending ? "desc" : "asc"));
                }
            }

            return result;
        }

        protected virtual IList<string> GetFacets(ISearchCriteria criteria)
        {
            var result = new List<string>();

            foreach (var filter in criteria.Filters)
            {
                var attributeFilter = filter as AttributeFilter;
                var priceRangeFilter = filter as PriceRangeFilter;
                var rangeFilter = filter as RangeFilter;

                string facet = null;

                if (attributeFilter != null)
                {
                    facet = GetAttributeFilterFacet(attributeFilter, criteria);
                }
                else if (rangeFilter != null)
                {
                    facet = GetRangeFilterFacet(rangeFilter, criteria);
                }
                else if (priceRangeFilter != null && priceRangeFilter.Currency.EqualsInvariant(criteria.Currency))
                {
                    facet = GetPriceRangeFilterFacet(priceRangeFilter, criteria);
                }

                if (!string.IsNullOrEmpty(facet))
                {
                    result.Add(facet);
                }
            }

            return result;
        }

        protected virtual string GetAttributeFilterFacet(AttributeFilter filter, ISearchCriteria criteria)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key).ToLower();
            var builder = new StringBuilder(azureFieldName);

            if (filter.FacetSize != null)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, ",count:{0}", filter.FacetSize);
            }

            return builder.ToString();
        }

        protected virtual string GetRangeFilterFacet(RangeFilter filter, ISearchCriteria criteria)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key).ToLower();
            return GetRangeFilterFacet(azureFieldName, filter.Values, criteria);
        }

        protected virtual string GetPriceRangeFilterFacet(PriceRangeFilter filter, ISearchCriteria criteria)
        {
            var fieldName = AzureSearchHelper.JoinNonEmptyStrings("_", false, filter.Key, criteria.Currency, criteria.Pricelists?.FirstOrDefault());
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(fieldName).ToLower();
            return GetRangeFilterFacet(azureFieldName, filter.Values, criteria);
        }

        protected virtual string GetRangeFilterFacet(string azureFieldName, RangeFilterValue[] filterValues, ISearchCriteria criteria)
        {
            var edgeValues = filterValues
                .SelectMany(v => new[] { ConvertToDecimal(v.Lower), ConvertToDecimal(v.Upper) })
                .Where(v => v > 0m)
                .Distinct()
                .OrderBy(v => v)
                .ToArray();

            var values = string.Join("|", edgeValues);

            return $"{azureFieldName},values:{values}";
        }

        protected virtual decimal? ConvertToDecimal(string input)
        {
            decimal? result = null;

            decimal value;
            if (decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                result = value;
            }

            return result;
        }
    }
}
