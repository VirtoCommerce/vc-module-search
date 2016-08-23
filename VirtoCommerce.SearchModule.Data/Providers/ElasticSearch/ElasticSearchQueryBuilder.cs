using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using PlainElastic.Net;
using PlainElastic.Net.Queries;
using VirtoCommerce.Domain.Search.Filters;
using VirtoCommerce.Domain.Search.Services;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.Domain.Search.Model;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    [CLSCompliant(false)]
    public class ElasticSearchQueryBuilder : ISearchQueryBuilder
    {
        #region ISearchQueryBuilder Members
        public object BuildQuery(ISearchCriteria criteria)
        {
            var builder = new QueryBuilder<ESDocument>();
            var mainFilter = new Filter<ESDocument>();
            var mainQuery = new BoolQuery<ESDocument>();

            #region Sorting

            // Add sort order
            if (criteria.Sort != null)
            {
                var fields = criteria.Sort.GetSort();
                foreach (var field in fields)
                {
                    builder.Sort(d => d.Field(field.FieldName, field.IsDescending ? SortDirection.desc : SortDirection.asc, ignoreUnmapped: field.IgnoredUnmapped));
                }
            }

            #endregion

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

            #region CatalogItemSearchCriteria
            if (criteria is Model.CatalogIndexedSearchCriteria)
            {
                var c = criteria as Model.CatalogIndexedSearchCriteria;

                mainQuery.Must(m => m
                    .Range(r => r.Field("startdate").To(c.StartDate.ToString("s")))
                    );


                if (c.StartDateFrom.HasValue)
                {
                    mainQuery.Must(m => m
                        .Range(r => r.Field("startdate").From(c.StartDateFrom.Value.ToString("s")))
                   );
                }

                if (c.EndDate.HasValue)
                {
                    mainQuery.Must(m => m
                        .Range(r => r.Field("enddate").From(c.EndDate.Value.ToString("s")))
                   );
                }

                mainQuery.Must(m => m.Term(t => t.Field("__hidden").Value("false")));

                if (c.Outlines != null && c.Outlines.Count > 0)
                    AddQuery("__outline", mainQuery, c.Outlines);

                if (!String.IsNullOrEmpty(c.SearchPhrase))
                {
                    var searchFields = new List<string>();

                    searchFields.Add("__content");
                    if (!string.IsNullOrEmpty(c.Locale))
                    {
                        searchFields.Add(string.Format("__content_{0}", c.Locale.ToLower()));
                    }

                    AddQueryString(mainQuery, c, searchFields.ToArray());
                }

                if (!String.IsNullOrEmpty(c.Catalog))
                {
                    AddQuery("catalog", mainQuery, c.Catalog);
                }

                if (c.ClassTypes != null && c.ClassTypes.Count > 0)
                {
                    AddQuery("__type", mainQuery, c.ClassTypes, false);
                }
            }
            #endregion

            if (criteria is ElasticSearchCriteria)
            {
                var c = criteria as ElasticSearchCriteria;
                mainQuery.Must(m => m.Custom(c.RawQuery));
            }

            builder.Query(q => q.Bool(b => mainQuery));
            builder.Filter(f => mainFilter);

            // Add search facets
            var facets = GetFacets(criteria);
            builder.Facets(f => facets);
            //var aggregations = GetAggregations(criteria);
            //builder.Aggregations(x=> aggregations);

            return builder;
        }
        #endregion

        protected void AddQuery(string fieldName, BoolQuery<ESDocument> query, StringCollection filter, bool lowerCase = true)
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
                    var booleanQuery = new BoolQuery<ESDocument>();
                    var containsFilter = false;
                    foreach (var index in filter.Cast<string>().Where(index => !String.IsNullOrEmpty(index)))
                    {
                        booleanQuery.Should(q => q.Custom("{{\"wildcard\" : {{ \"{0}\" : \"{1}\" }}}}", fieldName.ToLower(), lowerCase ? index.ToLower() : index));
                        containsFilter = true;
                    }
                    if (containsFilter)
                        query.Must(q => q.Bool(b => booleanQuery));
                }
            }
        }

        protected void AddQuery(string fieldName, BoolQuery<ESDocument> query, string filter, bool lowerCase = true)
        {
            query.Must(q => q.Custom("{{\"wildcard\" : {{ \"{0}\" : \"{1}\" }}}}", fieldName.ToLower(), lowerCase ? filter.ToLower() : filter));
        }

        protected void AddQueryString(BoolQuery<ESDocument> query, Model.CatalogIndexedSearchCriteria filter, params string[] fields)
        {
            var searchPhrase = filter.SearchPhrase;
            if (filter.IsFuzzySearch)
            {
                query.Must(
                    q =>
                    q.MultiMatch(
                        x =>
                        x.Fields(fields).Operator(Operator.AND).Fuzziness(filter.FuzzyMinSimilarity).Query(searchPhrase).Analyzer(ElasticSearchProvider.SearchAnalyzer)));
            }
            else
            {
                query.Must(
                    q =>
                    q.MultiMatch(
                        x =>
                        x.Fields(fields).Operator(Operator.AND).Query(searchPhrase).Analyzer(ElasticSearchProvider.SearchAnalyzer)));
            }
        }

        #region Aggregations
        protected virtual Aggregations<ESDocument> GetAggregations(ISearchCriteria criteria)
        {
            // Now add aggregations
            var aggregationParams = new Aggregations<ESDocument>();
            foreach (var filter in criteria.Filters)
            {
                if (filter is AttributeFilter)
                {
                    AddAggregationQueries(aggregationParams, filter.Key, criteria);
                }
                else if (filter is PriceRangeFilter)
                {
                    var currency = ((PriceRangeFilter)filter).Currency;
                    if (currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                    {
                        AddAggregationPriceQueries(aggregationParams, filter.Key, ((PriceRangeFilter)filter).Values, criteria);
                    }
                }
                else if (filter is RangeFilter)
                {
                    AddAggregationQueries(aggregationParams, filter.Key, ((RangeFilter)filter).Values, criteria);
                }
            }

            return aggregationParams;
        }

        private void AddAggregationQueries(Aggregations<ESDocument> param, string fieldName, ISearchCriteria criteria)
        {
            var existing_filters = new BoolFilter<ESDocument>();
            foreach (var f in criteria.CurrentFilters)
            {
                // don't filter within the same keyfield
                if (!f.Key.Equals(fieldName))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    existing_filters.Must(ff => ff.Bool(bb => q));
                }
            }
            
            var facet_filters = new FilterAggregation<ESDocument>();

            facet_filters
                .Filter(f => f.Bool(a => existing_filters))
                .Aggregations(a =>
                    a.Terms(af => af.AggregationName(fieldName.ToLower()).Field(fieldName.ToLower())));

            param.Filter(filter => facet_filters.AggregationName(fieldName.ToLower()));
        }

        //"aggs": {
        //    "size-0_to_5": {
        //      "aggregations": {
        //          "myfilter": {
        //        "filter": {
        //            "range": {
        //                "size": {
        //                    "from": "5",
        //                    "to": "11"
        //                }
        //            }
        //        }
        //           }
        //          }
        //        "filter": {
        //            "range": {
        //                "size": {
        //                    "gte": "0",
        //                    "lt": "5"
        //                }
        //            }
        //        }
        //    }
        //}
        private void AddAggregationQueries(Aggregations<ESDocument> param, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria)
        {
            if (values == null)
                return;

            var existing_filters = new MustFilter<ESDocument>();
            foreach (var f in criteria.CurrentFilters)
            {
                // don't filter within same key field
                if (!f.Key.Equals(fieldName))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    existing_filters.Bool(bb=>q);
                }
            }

            foreach (var value in values)
            {
                var facet_filters = new FilterAggregation<ESDocument>();

                var boolQuery = new BoolFilter<ESDocument>();
                var rangeQuery = new RangeFilter<ESDocument>();
                rangeQuery.Field(fieldName).Gte(value.Lower).Lt(value.Upper);
                boolQuery
                    .Must(a => a.Range(r=>rangeQuery))
                    .Must(f=>existing_filters);

                param.Filter(
                        ff => ff.AggregationName(string.Format("{0}-{1}", fieldName, value.Id))
                        .Filter(f => f.Bool(b => boolQuery)));

                /* working version using sub agg */
                /*
                var facet_filters = new FilterAggregation<ESDocument>();
                facet_filters
                    .Filter(f => f.Bool(a => existing_filters))
                    .Aggregations(a =>
                        a.Filter(af => af.AggregationName("main")
                            .Filter(af2 => af2.Range(r=>r.Field(fieldName).From(value.Lower).To(value.Upper)))));

                param.Filter(filter=>facet_filters.AggregationName(string.Format("{0}-{1}", fieldName, value.Id)))
                    .Filter(f1=>
                        f1.Aggregations(x => x.Terms(agg => agg.AggregationName(fieldName.ToLower()).Field(fieldName.ToLower())))
                        );

                */
            }
        }

        private void AddAggregationQueries(Aggregations<ESDocument> param, string fieldName, IEnumerable<CategoryFilterValue> values)
        {
            foreach (var val in values)
            {
                var facetName = string.Format("{0}-{1}", fieldName.ToLower(), val.Id.ToLower());
                param.Filter(ff =>
                    ff.AggregationName(facetName).Filter(f => f.Query(q => q.Bool(bf => bf.Must(bfm =>
                    bfm.Custom("{{\"wildcard\" : {{ \"{0}\" : \"{1}\" }}}}", fieldName.ToLower(), val.Outline.ToLower()))))));
            }
        }

        ///     "aggs": {
        //        "price-0_to_100": {
        //            "filter": {
        //                "bool": {
        //                    "should": [
        //                        {
        //                            "range": {
        //                                "price_usd_default": {
        //                                    "gte": "0",
        //                                    "lt": "100"
        //                                }
        //}
        //                        }
        //                    ]
        //                }
        //            }
        //        },
        //        "price-100_to_700": {
        //            "filter": {
        //                "bool": {
        //                    "should": [
        //                        {
        //                            "range": {
        //                                "price_usd_default": {
        //                                    "gte": "100",
        //                                    "lt": "700"
        //                                }
        //                            }
        //                        }
        //                    ]
        //                }
        //            }
        //        }
        //    }
        private void AddAggregationPriceQueries(Aggregations<ESDocument> param, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria)
        {
            if (values == null)
                return;

            var ffilter = new MustFilter<ESDocument>();
            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.Equals(fieldName))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    ffilter.Bool(ff => q);
                }
            }

            foreach (var value in values)
            {
                var query = ElasticQueryHelper.CreatePriceRangeFilter(criteria, fieldName, value);

                if (query != null)
                {
                    query.Must(b => ffilter);
                    param.Filter(
                        ff => ff.AggregationName(string.Format("{0}-{1}", fieldName, value.Id)).Filter(f => f.Bool(b => query)));
                }
            }
        }
        #endregion

        #region Facet Query
        /// <summary>
        /// Gets the facet parameters. Generates request similar to the one below.
        ///     "facets": {
        //        "price-0_to_100": {
        //            "filter": {
        //                "bool": {
        //                    "should": [
        //                        {
        //                            "range": {
        //                                "price_usd_default": {
        //                                    "gte": "0",
        //                                    "lt": "100"
        //                                }
        //}
        //                        }
        //                    ]
        //                }
        //            }
        //        },
        //        "price-100_to_700": {
        //            "filter": {
        //                "bool": {
        //                    "should": [
        //                        {
        //                            "range": {
        //                                "price_usd_default": {
        //                                    "gte": "100",
        //                                    "lt": "700"
        //                                }
        //                            }
        //                        }
        //                    ]
        //                }
        //            }
        //        }
        //    }
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        protected virtual Facets<ESDocument> GetFacets(ISearchCriteria criteria)
        {
            // Now add facets
            var facetParams = new Facets<ESDocument>();
            foreach (var filter in criteria.Filters)
            {
                if (filter is AttributeFilter)
                {
                    AddFacetQueries(facetParams, filter.Key, criteria);
                }
                else if (filter is RangeFilter)
                {
                    AddFacetQueries(facetParams, filter.Key, ((RangeFilter)filter).Values, criteria);
                }
                else if (filter is PriceRangeFilter)
                {
                    var currency = ((PriceRangeFilter)filter).Currency;
                    if (currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                    {
                        AddFacetPriceQueries(facetParams, filter.Key, ((PriceRangeFilter)filter).Values, criteria);
                    }
                }
                else if (filter is CategoryFilter)
                {
                    AddFacetQueries(facetParams, filter.Key, ((CategoryFilter)filter).Values);
                }
            }

            /*
            var catalogCriteria = criteria as CatalogItemSearchCriteria;

            if (catalogCriteria != null)
            {
                AddSubCategoryFacetQueries(facetParams, catalogCriteria);
            }
             * */

            return facetParams;
        }

        private void AddFacetQueries(Facets<ESDocument> param, string fieldName, IEnumerable<CategoryFilterValue> values)
        {
            foreach (var val in values)
            {
                var facetName = String.Format("{0}-{1}", fieldName.ToLower(), val.Id.ToLower());
                param.FilterFacets(ff =>
                    ff.FacetName(facetName).Filter(f => f.Query(q => q.Bool(bf => bf.Must(bfm =>
                    bfm.Custom("{{\"wildcard\" : {{ \"{0}\" : \"{1}\" }}}}", fieldName.ToLower(), val.Outline.ToLower()))))));
            }
        }

        /// <summary>
        /// Adds the facet queries.
        /// </summary>
        /// <param name="param">The param.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="criteria">Search criteria.</param>
        private void AddFacetQueries(Facets<ESDocument> param, string fieldName, ISearchCriteria criteria)
        {
            var ffilter = new BoolFilter<ESDocument>();
            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.Equals(fieldName))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    ffilter.Must(ff => ff.Bool(bb => q));
                }
            }

            var facetFilter = new FacetFilter<ESDocument>();
            facetFilter.Bool(f => ffilter);

            param.Terms(t => t.FacetName(fieldName.ToLower()).Field(fieldName.ToLower()).FacetFilter(ff => facetFilter));
        }

        /// <summary>
        /// Adds the facet queries.
        /// </summary>
        /// <param name="param">The param.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="criteria">Search criteria.</param>
        private void AddFacetQueries(Facets<ESDocument> param, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria)
        {
            if (values == null)
                return;

            var ffilter = new Filter<ESDocument>();
            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.Equals(fieldName))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    ffilter.Bool(ff => q);
                }
            }

            foreach (var value in values)
            {
                var filter = new FacetFilter<ESDocument>();
                var range = filter.Range(r => r.Field(fieldName));

                filter.Range(r => r.Field(fieldName).Gte(value.Lower).Lt(value.Upper));
                filter.And(b => ffilter);
                param.Terms(t => t.FacetName(String.Format("{0}-{1}", fieldName, value.Id)).Field(fieldName.ToLower()).FacetFilter(ff => filter));
            }
        }

        /// <summary>
        /// Adds the facet queries.
        /// </summary>
        /// <param name="param">The param.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="criteria">Search criteria.</param>
        private void AddFacetPriceQueries(Facets<ESDocument> param, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria)
        {
            if (values == null)
                return;

            var ffilter = new MustFilter<ESDocument>();
            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.Equals(fieldName))
                {
                    var q = ElasticQueryHelper.CreateQuery(criteria, f);
                    ffilter.Bool(ff => q);
                }
            }

            foreach (var value in values)
            {
                var query = ElasticQueryHelper.CreatePriceRangeFilter(criteria, fieldName, value);

                if (query != null)
                {
                    query.Must(b => ffilter);
                    param.FilterFacets(
                        ff => ff.FacetName(String.Format("{0}-{1}", fieldName, value.Id)).Filter(f => f.Bool(b => query)));
                }
            }
        }
        #endregion
    }
}
