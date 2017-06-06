using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Nest;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    public class ElasticSearchQueryBuilder : ISearchQueryBuilder
    {
        public virtual string DocumentType => string.Empty;

        #region ISearchQueryBuilder Members

        public virtual object BuildQuery<T>(string scope, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            var result = new SearchRequest(scope, criteria.DocumentType)
            {
                Query = GetQuery<T>(criteria),
                PostFilter = GetPostFilter<T>(criteria, availableFields),
                Aggregations = GetAggregations<T>(criteria, availableFields),
                Sort = GetSorting<T>(criteria.Sort),
                From = criteria.StartingRecord,
                Size = criteria.RecordsToRetrieve
            };

            return result;
        }

        #endregion

        protected virtual QueryContainer GetQuery<T>(ISearchCriteria criteria)
            where T : class
        {
            QueryContainer result = null;

            result &= GetIdsQuery<T>(criteria);
            result &= GetRawQuery<T>(criteria);
            result &= GetKeywordQuery<T>(criteria);

            return result;
        }

        protected virtual QueryContainer GetIdsQuery<T>(ISearchCriteria criteria)
            where T : class
        {
            QueryContainer result = null;

            if (criteria?.Ids != null && criteria.Ids.Any())
            {
                result = new IdsQuery { Values = criteria.Ids.Select(id => new Id(id)) };
            }

            return result;
        }

        protected virtual QueryContainer GetRawQuery<T>(ISearchCriteria criteria)
            where T : class
        {
            QueryContainer result = null;

            if (!string.IsNullOrEmpty(criteria?.RawQuery))
            {
                result = new QueryStringQuery { Query = criteria.RawQuery, Lenient = true, DefaultOperator = Operator.And, Analyzer = "standard" };
            }

            return result;
        }

        protected virtual QueryContainer GetKeywordQuery<T>(ISearchCriteria criteria)
            where T : class
        {
            QueryContainer result = null;

            if (!string.IsNullOrEmpty(criteria?.SearchPhrase))
            {
                var searchFields = new List<string> { "__content" };

                if (!string.IsNullOrEmpty(criteria.Locale))
                {
                    searchFields.Add(string.Concat("__content_", criteria.Locale.ToLowerInvariant()));
                }

                result = GetKeywordQuery<T>(criteria, searchFields.ToArray());
            }

            return result;
        }

        protected virtual QueryContainer GetKeywordQuery<T>(ISearchCriteria criteria, params string[] fields)
            where T : class
        {
            QueryContainer result = null;

            var searchPhrase = criteria.SearchPhrase;
            MultiMatchQuery multiMatch;
            if (criteria.IsFuzzySearch)
            {
                multiMatch = new MultiMatchQuery
                {
                    Fields = fields,
                    Query = searchPhrase,
                    Fuzziness = criteria.Fuzziness != null ? Fuzziness.EditDistance(criteria.Fuzziness.Value) : Fuzziness.Auto,
                    Analyzer = "standard",
                    Operator = Operator.And
                };
            }
            else
            {
                multiMatch = new MultiMatchQuery
                {
                    Fields = fields,
                    Query = searchPhrase,
                    Analyzer = "standard",
                    Operator = Operator.And
                };
            }

            result &= multiMatch;
            return result;
        }

        protected virtual IList<ISort> GetSorting<T>(SearchSort sorting)
            where T : class
        {
            IList<ISort> result = null;

            if (sorting != null)
            {
                var fields = sorting.GetSort();

                foreach (var field in fields)
                {
                    if (result == null)
                    {
                        result = new List<ISort>();
                    }

                    result.Add(
                        new SortField
                        {
                            Field = field.FieldName.ToLowerInvariant(),
                            Order = field.IsDescending ? SortOrder.Descending : SortOrder.Ascending,
                            Missing = "_last",
                            UnmappedType = FieldType.Long,
                        });
                }
            }

            return result;
        }

        protected virtual QueryContainer GetPostFilter<T>(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            QueryContainer result = null;

            // Perform facet filters
            if (criteria.CurrentFilters != null && criteria.CurrentFilters.Any())
            {
                foreach (var filter in criteria.CurrentFilters)
                {
                    if (!string.IsNullOrEmpty(criteria.Currency))
                    {
                        // Skip price range filters with currencies not equal to criteria currency
                        var priceRangeFilter = filter as PriceRangeFilter;
                        if (priceRangeFilter != null && !priceRangeFilter.Currency.EqualsInvariant(criteria.Currency))
                        {
                            continue;
                        }
                    }

                    var filterQuery = ElasticSearchHelper.CreateQuery<T>(criteria, filter, availableFields);

                    if (filterQuery != null)
                    {
                        result &= filterQuery;
                    }
                }
            }

            return result;
        }

        #region Aggregations

        protected virtual AggregationDictionary GetAggregations<T>(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            var container = new Dictionary<string, AggregationContainer>();

            foreach (var filter in criteria.Filters)
            {
                var fieldName = ElasticSearchHelper.ToElasticFieldName(filter.Key);

                var attributeFilter = filter as AttributeFilter;
                var priceRangeFilter = filter as PriceRangeFilter;
                var rangeFilter = filter as RangeFilter;

                if (attributeFilter != null)
                {
                    AddAttributeAggregationQueries<T>(container, fieldName, attributeFilter.FacetSize, criteria, availableFields);
                }
                else if (priceRangeFilter != null)
                {
                    var currency = priceRangeFilter.Currency;
                    if (currency.EqualsInvariant(criteria.Currency))
                    {
                        AddPriceAggregationQueries<T>(container, fieldName, priceRangeFilter.Values, criteria, availableFields);
                    }
                }
                else if (rangeFilter != null)
                {
                    AddRangeAggregationQueries<T>(container, fieldName, rangeFilter.Values, criteria, availableFields);
                }
            }

            return container;
        }

        protected virtual void AddAttributeAggregationQueries<T>(Dictionary<string, AggregationContainer> container, string fieldName, int? facetSize, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            var existingFilters = GetExistingFilters<T>(criteria, fieldName, availableFields);

            var agg = new FilterAggregation(fieldName)
            {
                Filter = new BoolQuery { Must = existingFilters },
                Aggregations = new TermsAggregation(fieldName)
                {
                    Field = fieldName,
                    Size = facetSize == null ? null : facetSize > 0 ? facetSize : int.MaxValue,
                },
            };

            container.Add(fieldName, agg);
        }

        protected virtual void AddPriceAggregationQueries<T>(Dictionary<string, AggregationContainer> container, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            if (values == null)
                return;

            var existingFilters = GetExistingFilters<T>(criteria, fieldName, availableFields);

            foreach (var value in values)
            {
                var query = ElasticSearchHelper.CreatePriceRangeFilter<T>(criteria, fieldName, criteria.Currency, value);

                if (query != null)
                {
                    var allFilters = new List<QueryContainer>();
                    allFilters.AddRange(existingFilters);
                    allFilters.Add(query);

                    var boolQuery = new BoolQuery { Must = allFilters };
                    var agg = new FilterAggregation(string.Format(CultureInfo.InvariantCulture, "{0}-{1}", fieldName, value.Id)) { Filter = boolQuery };
                    container.Add(string.Format(CultureInfo.InvariantCulture, "{0}-{1}", fieldName, value.Id), agg);
                }
            }
        }

        protected virtual void AddRangeAggregationQueries<T>(Dictionary<string, AggregationContainer> container, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            if (values == null)
                return;

            var existingFilters = GetExistingFilters<T>(criteria, fieldName, availableFields);

            foreach (var value in values)
            {
                var query = new TermRangeQuery { Field = fieldName, GreaterThanOrEqualTo = value.Lower, LessThan = value.Upper };

                var allFilters = new List<QueryContainer>();
                allFilters.AddRange(existingFilters);
                allFilters.Add(query);

                var boolQuery = new BoolQuery { Must = allFilters };
                var agg = new FilterAggregation(string.Format(CultureInfo.InvariantCulture, "{0}-{1}", fieldName, value.Id)) { Filter = boolQuery };
                container.Add(string.Format(CultureInfo.InvariantCulture, "{0}-{1}", fieldName, value.Id), agg);
            }
        }

        #endregion

        #region Helper Query Methods

        protected virtual QueryContainer CreateQuery(string fieldName, StringCollection values, bool lowerCase = true)
        {
            return CreateQuery(fieldName, values.OfType<string>().ToList(), lowerCase);
        }

        protected virtual QueryContainer CreateQuery(string fieldName, IList<string> values, bool lowerCase)
        {
            QueryContainer result = null;

            if (values.Count > 0)
            {
                if (values.Count == 1)
                {
                    var value = values[0];
                    if (!string.IsNullOrEmpty(value))
                    {
                        result &= CreateWildcardQuery(fieldName, value, lowerCase);
                    }
                }
                else
                {
                    var containsFilter = false;
                    var valueContainer = new List<QueryContainer>();

                    foreach (var value in values.Where(v => !string.IsNullOrEmpty(v)))
                    {
                        valueContainer.Add(CreateWildcardQuery(fieldName, value, lowerCase));
                        containsFilter = true;
                    }

                    if (containsFilter)
                    {
                        result |= new BoolQuery { Should = valueContainer };
                    }

                }
            }

            return result;
        }
        protected virtual QueryContainer CreateQuery(string fieldName, string value, bool lowerCase = true)
        {
            QueryContainer query = null;
            query &= CreateWildcardQuery(fieldName, value, lowerCase);
            return query;
        }


        protected virtual QueryContainer CreateWildcardQuery(string fieldName, string value, bool lowerCaseValue)
        {
            return new WildcardQuery { Field = fieldName.ToLowerInvariant(), Value = lowerCaseValue ? value.ToLowerInvariant() : value };
        }

        protected virtual List<QueryContainer> GetExistingFilters<T>(ISearchCriteria criteria, string fieldName, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            var existingFilters = new List<QueryContainer>();

            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.EqualsInvariant(fieldName))
                {
                    var q = ElasticSearchHelper.CreateQuery<T>(criteria, f, availableFields);
                    existingFilters.Add(q);
                }
            }

            return existingFilters;
        }

        #endregion
    }
}
