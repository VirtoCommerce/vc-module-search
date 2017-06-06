using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Azure.Search.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.AzureSearch
{
    [CLSCompliant(false)]
    public class AzureSearchQueryBuilder : ISearchQueryBuilder
    {
        public string DocumentType => string.Empty;

        public virtual object BuildQuery<T>(string scope, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            var result = new AzureSearchQuery
            {
                SearchText = GetSearchText(criteria),
                SearchParameters = new SearchParameters
                {
                    QueryType = GetQueryType(criteria, availableFields),
                    Filter = GetFilters(criteria, availableFields),
                    Facets = GetFacets(criteria, availableFields),
                    OrderBy = GetSorting(criteria, availableFields),
                    Skip = criteria.StartingRecord,
                    Top = criteria.RecordsToRetrieve,
                    IncludeTotalResultCount = true,
                    SearchMode = SearchMode.All,
                }
            };

            return result;
        }


        protected virtual QueryType GetQueryType(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            return string.IsNullOrEmpty(criteria?.RawQuery) ? QueryType.Simple : QueryType.Full;
        }

        protected virtual string GetSearchText(ISearchCriteria criteria)
        {
            return criteria?.RawQuery ?? criteria?.SearchPhrase;
        }

        protected virtual string GetFilters(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            IList<string> filters = new List<string>();

            AddFilters(criteria, filters, availableFields);

            var result = string.Join(" and ", filters.Where(f => !string.IsNullOrEmpty(f)));
            return result;
        }

        protected virtual void AddFilters(ISearchCriteria criteria, IList<string> filters, IList<IFieldDescriptor> availableFields)
        {
            AddIdsFilter(criteria, filters, availableFields);
            AddCurrentFilters(criteria, filters, availableFields);
        }

        protected virtual void AddIdsFilter(ISearchCriteria criteria, IList<string> filters, IList<IFieldDescriptor> availableFields)
        {
            if (criteria?.Ids != null && criteria.Ids.Any())
            {
                var availableField = availableFields.Get(AzureSearchHelper.RawKeyFieldName);
                var filter = GetEqualsFilterExpression(availableField, criteria.Ids);
                filters.Add(filter);
            }
        }

        protected virtual void AddCurrentFilters(ISearchCriteria criteria, IList<string> filters, IList<IFieldDescriptor> availableFields)
        {
            foreach (var filter in criteria.CurrentFilters)
            {
                var attributeFilter = filter as AttributeFilter;
                var rangeFilter = filter as RangeFilter;
                var priceRangeFilter = filter as PriceRangeFilter;

                string expression = null;

                if (attributeFilter != null)
                {
                    expression = GetAttributeFilterExpression(attributeFilter, criteria, availableFields);
                }
                else if (rangeFilter != null)
                {
                    expression = GetRangeFilterExpression(rangeFilter, criteria, availableFields);
                }
                else if (priceRangeFilter != null)
                {
                    expression = GetPriceRangeFilterExpression(priceRangeFilter, criteria, availableFields);
                }

                if (!string.IsNullOrEmpty(expression))
                {
                    filters.Add(expression);
                }
            }
        }

        protected virtual string GetAttributeFilterExpression(AttributeFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            string result;

            var availableField = availableFields.Get(filter.Key);
            if (availableField != null)
            {
                result = availableField.DataType.StartsWith("Collection(")
                    ? GetContainsFilterExpression(availableField, filter.Values.Select(v => v.Value))
                    : GetEqualsFilterExpression(availableField, filter.Values.Select(v => v.Value));
            }
            else
            {
                result = AzureSearchHelper.NonExistentFieldFilter;
            }

            return result;
        }

        protected virtual string GetContainsFilterExpression(IFieldDescriptor availableField, IEnumerable<string> rawValues)
        {
            var azureFieldName = availableField.Name;
            var values = rawValues.Where(v => !string.IsNullOrEmpty(v)).Select(GetStringFilterValue).ToArray();
            return AzureSearchHelper.JoinNonEmptyStrings(" or ", true, values.Select(v => $"{azureFieldName}/any(v: v eq {v})").ToArray());
        }

        protected virtual string GetEqualsFilterExpression(IFieldDescriptor availableField, IEnumerable<string> rawValues)
        {
            var azureFieldName = availableField.Name;
            var values = rawValues.Where(v => !string.IsNullOrEmpty(v)).Select(v => GetFilterValue(availableField, v)).ToArray();
            return AzureSearchHelper.JoinNonEmptyStrings(" or ", true, values.Select(v => $"{azureFieldName} eq {v}").ToArray());
        }

        protected virtual string GetRangeFilterExpression(RangeFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            string result;

            var availableField = availableFields.Get(filter.Key);
            if (availableField != null)
            {
                var expressions = filter.Values
                    .Select(v => GetRangeFilterValueExpression(v, availableField.Name))
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();

                result = AzureSearchHelper.JoinNonEmptyStrings(" or ", true, expressions);
            }
            else
            {
                result = AzureSearchHelper.NonExistentFieldFilter;
            }

            return result;
        }

        protected virtual string GetPriceRangeFilterExpression(PriceRangeFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            string result = null;

            if (string.IsNullOrEmpty(criteria.Currency) || filter.Currency.EqualsInvariant(criteria.Currency))
            {
                var priceFieldNames = AzureSearchHelper.GetPriceFieldNames(filter.Key, filter.Currency, criteria.Pricelists, true)
                    .Where(availableFields.Contains)
                    .ToList();

                if (priceFieldNames.Any())
                {
                    var expressions = filter.Values
                        .Select(v => GetPriceRangeFilterValueExpression(1, priceFieldNames, filter, v))
                        .Where(e => !string.IsNullOrEmpty(e))
                        .ToArray();

                    result = AzureSearchHelper.JoinNonEmptyStrings(" or ", true, expressions);
                }
            }

            return result;
        }

        protected virtual string GetPriceRangeFilterValueExpression(int priceFieldIndex, IList<string> priceFieldNames, PriceRangeFilter filter, RangeFilterValue filterValue)
        {
            string result = null;

            if (priceFieldNames.Count == 1)
            {
                var azureFieldName = priceFieldNames.First();
                result = GetRangeFilterValueExpression(filterValue, azureFieldName);
            }
            else if (priceFieldIndex < priceFieldNames.Count)
            {
                // Get negative expression for previous pricelist
                string previousPricelistExpression = null;
                if (priceFieldIndex > 1)
                {
                    var previousAzureFieldName = priceFieldNames[priceFieldIndex - 1];
                    previousPricelistExpression = $"not({previousAzureFieldName} gt 0)";
                }

                // Get positive expression for current pricelist
                var currentAzureFieldName = priceFieldNames[priceFieldIndex];
                var currentPricelistExpresion = GetRangeFilterValueExpression(filterValue, currentAzureFieldName);

                // Get expression for next pricelist
                var nextPricelistExpression = GetPriceRangeFilterValueExpression(priceFieldIndex + 1, priceFieldNames, filter, filterValue);

                var currentExpression = AzureSearchHelper.JoinNonEmptyStrings(" or ", true, currentPricelistExpresion, nextPricelistExpression);
                result = AzureSearchHelper.JoinNonEmptyStrings(" and ", false, previousPricelistExpression, currentExpression);
            }

            return result;
        }

        protected virtual string GetRangeFilterValueExpression(RangeFilterValue filterValue, string azureFieldName)
        {
            var lowerCondition = filterValue.IncludeLower ? "ge" : "gt";
            var upperCondition = filterValue.IncludeUpper ? "le" : "lt";
            return GetRangeFilterExpression(azureFieldName, filterValue.Lower, lowerCondition, filterValue.Upper, upperCondition);
        }

        protected virtual string GetRangeFilterExpression(string rawName, DateTime? lowerBound, bool lowerBoundIncluded, DateTime? upperBound, bool upperBoundIncluded)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(rawName);
            var lower = lowerBound?.ToString("O");
            var upper = upperBound?.ToString("O");
            var lowerCondition = lowerBoundIncluded ? "ge" : "gt";
            var upperCondition = upperBoundIncluded ? "le" : "lt";

            return GetRangeFilterExpression(azureFieldName, lower, lowerCondition, upper, upperCondition);
        }

        protected virtual string GetRangeFilterExpression(string azureFieldName, string lowerBound, string lowerCondition, string upperBound, string upperCondition)
        {
            string result = null;

            if (lowerBound?.Length > 0 && lowerCondition?.Length > 0 || upperBound?.Length > 0 && upperCondition?.Length > 0)
            {
                var builder = new StringBuilder();

                if (lowerBound?.Length > 0)
                {
                    builder.Append($"{azureFieldName} {lowerCondition} {lowerBound}");

                    if (upperBound?.Length > 0)
                    {
                        builder.Append(" and ");
                    }
                }

                if (upperBound?.Length > 0)
                {
                    builder.Append($"{azureFieldName} {upperCondition} {upperBound}");
                }

                result = builder.ToString();
            }

            return result;
        }

        protected virtual string GetFilterValue(IFieldDescriptor availableField, string rawValue)
        {
            string result;

            if (availableField?.DataType == DataType.Boolean.ToString())
            {
                result = rawValue.ToLowerInvariant();
            }
            else if (availableField?.DataType != DataType.String.ToString())
            {
                result = rawValue;
            }
            else
            {
                result = GetStringFilterValue(rawValue);
            }

            return result;
        }

        protected virtual string GetStringFilterValue(string rawValue)
        {
            return $"'{rawValue.Replace("'", "''")}'";
        }

        protected virtual IList<string> GetSorting(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            IList<string> result = null;

            if (criteria.Sort != null)
            {
                var fields = criteria.Sort.GetSort();

                foreach (var field in fields)
                {
                    var azureFieldName = AzureSearchHelper.ToAzureFieldName(field.FieldName);
                    if (availableFields.Contains(azureFieldName))
                    {
                        if (result == null)
                        {
                            result = new List<string>();
                        }

                        result.Add(string.Join(" ", azureFieldName, field.IsDescending ? "desc" : "asc"));
                    }
                }
            }

            return result;
        }

        protected virtual IList<string> GetFacets(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            var result = new List<string>();

            foreach (var filter in criteria.Filters)
            {
                var attributeFilter = filter as AttributeFilter;
                var priceRangeFilter = filter as PriceRangeFilter;
                var rangeFilter = filter as RangeFilter;

                string facet = null;
                IList<string> facets = null;

                if (attributeFilter != null)
                {
                    facet = GetAttributeFilterFacet(attributeFilter, criteria, availableFields);
                }
                else if (rangeFilter != null)
                {
                    facet = GetRangeFilterFacet(rangeFilter, criteria, availableFields);
                }
                else if (priceRangeFilter != null && priceRangeFilter.Currency.EqualsInvariant(criteria.Currency))
                {
                    facets = GetPriceRangeFilterFacets(priceRangeFilter, criteria, availableFields);
                }

                if (!string.IsNullOrEmpty(facet))
                {
                    result.Add(facet);
                }

                if (facets != null)
                {
                    result.AddRange(facets.Where(f => !string.IsNullOrEmpty(f)));
                }
            }

            return result;
        }

        protected virtual string GetAttributeFilterFacet(AttributeFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            string result = null;

            var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key);
            if (availableFields.Contains(azureFieldName))
            {
                var builder = new StringBuilder(azureFieldName);

                if (filter.FacetSize != null)
                {
                    builder.AppendFormat(CultureInfo.InvariantCulture, ",count:{0}", filter.FacetSize);
                }

                result = builder.ToString();
            }

            return result;
        }

        protected virtual string GetRangeFilterFacet(RangeFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key);
            return GetRangeFilterFacet(azureFieldName, filter.Values, criteria, availableFields);
        }

        protected virtual IList<string> GetPriceRangeFilterFacets(PriceRangeFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            var azureFieldNames = AzureSearchHelper.GetPriceFieldNames(filter.Key, criteria?.Currency, criteria?.Pricelists, false);
            return azureFieldNames.Select(f => GetRangeFilterFacet(f, filter.Values, criteria, availableFields)).ToArray();
        }

        protected virtual string GetRangeFilterFacet(string azureFieldName, RangeFilterValue[] filterValues, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            string result = null;

            if (availableFields.Contains(azureFieldName))
            {
                var edgeValues = filterValues
                    .SelectMany(v => new[] { ConvertToDecimal(v.Lower), ConvertToDecimal(v.Upper) })
                    .Where(v => v > 0m)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToArray();

                var values = string.Join("|", edgeValues);

                result = $"{azureFieldName},values:{values}";
            }

            return result;
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
