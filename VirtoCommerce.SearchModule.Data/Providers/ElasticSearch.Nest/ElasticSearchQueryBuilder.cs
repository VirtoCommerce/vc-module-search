using Nest;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using VirtoCommerce.Domain.Search.Filters;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.SearchModule.Data.Model;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    [CLSCompliant(false)]
    public class ElasticSearchQueryBuilder : ISearchQueryBuilder
    {
        #region ISearchQueryBuilder Members
        public object BuildQuery<T>(string scope, ISearchCriteria criteria) where T:class
        {
            var builder = new SearchRequest(scope, criteria.DocumentType);

            //var mainFilter = new Filter<ESDocument>();
            var mainQuery = new List<QueryContainer>();

            #region Sorting

            // Add sort order
            if (criteria.Sort != null)
            {
                var fields = criteria.Sort.GetSort();
                foreach (var field in fields)
                {
                    builder.Sort.Add(
                        new SortField
                        {
                            Field = field.FieldName,
                            Order = field.IsDescending ? SortOrder.Descending : SortOrder.Ascending,
                            Missing = "_last"
                        });
                }
            }

            #endregion

            /*
             * 
             * new BoolQuery
{
    Filter = new QueryContainer[] {
        !new TermQuery
        {
            IsVerbatim = true,
            Field = "name",
            Value = ""
        } &&
        new ExistsQuery
        {
            Field = "numberOfCommits"
        }
    }
}
            #region Filters
            // Perform facet filters
            if (criteria.CurrentFilters != null && criteria.CurrentFilters.Any())
            {
                var combinedFilter = new BoolFilter<ESDocument>();
                // group filters
                foreach (var filter in criteria.CurrentFilters)
                {
                    // Skip currencies that are not part of the filter
                    if (filter.GetType() == typeof(PriceRangeFilter)) // special filtering 
                    {
                        var priceRangeFilter = filter as PriceRangeFilter;
                        if (priceRangeFilter != null)
                        {
                            var currency = priceRangeFilter.Currency;
                            if (!currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                    }

                    var filterQuery = ElasticQueryHelper.CreateQuery(criteria, filter);

                    if (filterQuery != null)
                    {
                        combinedFilter.Must(c => c.Bool(q => filterQuery));
                    }
                }

                mainFilter.Bool(bl => combinedFilter);
            }
            #endregion
            */

            #region CatalogItemSearchCriteria
            if (criteria is Model.CatalogIndexedSearchCriteria)
            {
                var c = criteria as Model.CatalogIndexedSearchCriteria;

                mainQuery.Add(new DateRangeQuery() { Field = "startdate", LessThanOrEqualTo = c.StartDate });


                if (c.StartDateFrom.HasValue)
                {
                    mainQuery.Add(new DateRangeQuery() { Field = "startdate", GreaterThan = c.StartDateFrom.Value });
                }

                if (c.EndDate.HasValue)
                {
                    mainQuery.Add(new DateRangeQuery() { Field = "enddate", GreaterThan = c.StartDateFrom.Value });
                }

                mainQuery.Add(new TermQuery() { Field = "__hidden", Value = false });

                if (c.Outlines != null && c.Outlines.Count > 0)
                {
                    AddQuery("__outline", mainQuery, c.Outlines);
                }

                if (!string.IsNullOrEmpty(c.SearchPhrase))
                {
                    var searchFields = new List<string>();

                    searchFields.Add("__content");
                    if (!string.IsNullOrEmpty(c.Locale))
                    {
                        searchFields.Add(string.Format("__content_{0}", c.Locale.ToLower()));
                    }

                    AddQueryString(mainQuery, c, searchFields.ToArray());
                }

                if (!string.IsNullOrEmpty(c.Catalog))
                {
                    AddQuery("catalog", mainQuery, c.Catalog);
                }

                if (c.ClassTypes != null && c.ClassTypes.Count > 0)
                {
                    AddQuery("__type", mainQuery, c.ClassTypes, false);
                }
            }
            #endregion

            var boolQuery = new BoolQuery() { Must = mainQuery };
            builder.Query = boolQuery;
            //builder.Filter(f => mainFilter);

            // Add search facets
            //var facets = GetFacets(criteria);
            //builder.Facets(f => facets);
            var aggregations = GetAggregations<T>(criteria);
            builder.Aggregations = aggregations;

            return builder;
        }
        #endregion

        #region Aggregations
        protected virtual AggregationDictionary GetAggregations<T>(ISearchCriteria criteria) where T:class
        {
            // Now add aggregations
            var container = new Dictionary<string, AggregationContainer>();
            foreach (var filter in criteria.Filters)
            {
                if (filter is AttributeFilter)
                {
                    AddAggregationQueries<T>(container, filter.Key, criteria);
                }
                /*
                else if (filter is PriceRangeFilter)
                {
                    var currency = ((PriceRangeFilter)filter).Currency;
                    if (currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                    {
                        AddAggregationPriceQueries(aggregations, filter.Key, ((PriceRangeFilter)filter).Values, criteria);
                    }
                }
                else if (filter is RangeFilter)
                {
                    AddAggregationQueries(aggregations, filter.Key, ((RangeFilter)filter).Values, criteria);
                }
                */
            }

            return container;
        }

        private void AddAggregationQueries<T>(Dictionary<string, AggregationContainer> container, string field, ISearchCriteria criteria) where T:class
        {
            var agg = new TermsAggregation(field) { Field = field.ToLower() };
            container.Add(field, agg);

            /*
            var existing_filters = new BoolFilter<DocumentDictionary>();
            foreach (var f in criteria.CurrentFilters)
            {
                // don't filter within the same keyfield
                if (!f.Key.Equals(field))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    existing_filters.Must(ff => ff.Bool(bb => q));
                }
            }


            aggs.Aggregate
            var facet_filters = new FilterAggregation<DocumentDictionary>();

            facet_filters
                .Filter(f => f.Bool(a => existing_filters))
                .Aggregations(a =>
                    a.Terms(af => af.AggregationName(field.ToLower()).Field(field.ToLower())));

            aggs.Filter(filter => facet_filters.AggregationName(field.ToLower()));
            */
        }
        #endregion

        #region Helper Query Methods
        protected void AddQuery(string fieldName, List<QueryContainer> query, StringCollection filter, bool lowerCase = true)
        {
            fieldName = fieldName.ToLower();
            if (filter.Count > 0)
            {
                if (filter.Count == 1)
                {
                    if (!string.IsNullOrEmpty(filter[0]))
                    {
                        AddQuery(fieldName, query, filter[0], lowerCase);
                    }
                }
                else
                {
                    var booleanQuery = new BoolQuery();
                    var containsFilter = false;
                    var valueContainer = new List<QueryContainer>();
                    foreach (var index in filter.Cast<string>().Where(index => !String.IsNullOrEmpty(index)))
                    {
                        valueContainer.Add(new WildcardQuery() { Field = fieldName.ToLower(), Value = lowerCase ? index.ToLower() : index });
                        containsFilter = true;
                    }
                    if (containsFilter)
                    {
                        booleanQuery.Should = valueContainer;
                        query.Add(booleanQuery);
                    }
                        
                }
            }
        }

        protected void AddQuery(string fieldName, List<QueryContainer> query, string filter, bool lowerCase = true)
        {
            query.Add(new WildcardQuery() { Field = fieldName.ToLower(), Value = lowerCase ? filter.ToLower() : filter });
        }

        protected void AddQueryString(List<QueryContainer> query, Model.CatalogIndexedSearchCriteria filter, params string[] fields)
        {
            var searchPhrase = filter.SearchPhrase;
            MultiMatchQuery multiMatch;
            if (filter.IsFuzzySearch)
            {
                multiMatch = new MultiMatchQuery()
                {
                    Fields = fields,
                    Query = searchPhrase,
                    Fuzziness = Fuzziness.Auto,
                    Analyzer = "standard",
                    Operator = Operator.And
                };
            }
            else
            {
                multiMatch = new MultiMatchQuery()
                {
                    Fields = fields,
                    Query = searchPhrase,
                    Analyzer = "standard",
                    Operator = Operator.And
                };
            }

            query.Add(multiMatch);
        }
        #endregion
    }
}
