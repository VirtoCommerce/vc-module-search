using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Domain.Search.Filters;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.SearchModule.Data.Services;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    public class SearchResults<T> : Model.ISearchResults<T> where T:class
    {
        public SearchResults(ISearchCriteria criteria, ISearchResponse<T> response)
        {
            this.SearchCriteria = criteria;
            this.Documents = response.Documents;
            this.DocCount = response.HitsMetaData.Total;
            this.TotalCount = response.Total;
            this.ProviderAggregations = response.Aggregations;
            this.Facets = CreateFacets(criteria, response.Aggregations);
        }

        public IDictionary<string, IAggregate> ProviderAggregations
        {
            get;
            private set;
        }

        public FacetGroup[] Facets
        {
            get;
            private set;
        }

        public long DocCount
        {
            get;
            private set;
        }

        public IEnumerable<T> Documents
        {
            get;
            private set;
        }

        public ISearchCriteria SearchCriteria
        {
            get;
            private set;
        }

        public long TotalCount
        {
            get;
            private set;
        }

        public string[] Suggestions
        {
            get; private set;
        }

        private static FacetGroup[] CreateFacets(ISearchCriteria criteria, IDictionary<string, IAggregate> facets)
        {
            var result = new List<FacetGroup>();

            if (facets != null)
            {
                foreach (var filter in criteria.Filters)
                {
                    var groupLabels = filter.GetLabels();
                    var facetGroup = new FacetGroup(filter.Key, groupLabels);

                    var values = filter.GetValues();

                    // Return all facet terms for attribute filter if values are not defined
                    if (values == null && filter is AttributeFilter)
                    {
                        facetGroup.FacetType = FacetTypes.Attribute;

                        var key = filter.Key.ToLower();
                        if (facets.ContainsKey(key))
                        {
                            var facet = facets[key] as FiltersAggregate;
                            if (facet != null)
                            {
                                if (facet.Aggregations != null)
                                {
                                    var termAgg = facet.Aggregations[key] as TermsAggregate;
                                    if (termAgg != null)
                                    {
                                        foreach (var term in termAgg.Buckets)
                                        {
                                            var newFacet = new Facet(facetGroup, term.Key, (int)term.DocCount, null);
                                            facetGroup.Facets.Add(newFacet);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (values != null)
                    {
                        foreach (var group in values.GroupBy(v => v.Id))
                        {
                            var valueLabels = group.GetValueLabels();

                            if (filter is AttributeFilter)
                            {
                                facetGroup.FacetType = FacetTypes.Attribute;

                                var key = filter.Key.ToLower();
                                if (facets.ContainsKey(key))
                                {
                                    var facet = facets[key] as SingleBucketAggregate;
                                    if (facet != null)
                                    {
                                        if (facet.Aggregations != null)
                                        {
                                            var termAgg = facet.Aggregations[key] as BucketAggregate;
                                            if (termAgg != null)
                                            {
                                                var term = termAgg.Items.OfType<KeyedBucket>().FirstOrDefault(t => t.Key.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
                                                if (term != null)
                                                {
                                                    var newFacet = new Facet(facetGroup, group.Key, (int)term.DocCount, valueLabels);
                                                    facetGroup.Facets.Add(newFacet);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (filter is PriceRangeFilter)
                            {
                                facetGroup.FacetType = FacetTypes.PriceRange;

                                var rangeFilter = filter as PriceRangeFilter;
                                if (rangeFilter.Currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                                {
                                    var key = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", filter.Key, group.Key).ToLower();
                                    if (facets.ContainsKey(key))
                                    {
                                        var facet = facets[key] as SingleBucketAggregate;
                                        if (facet != null && facet.DocCount > 0)
                                        {
                                            var newFacet = new Facet(facetGroup, group.Key, (int)facet.DocCount, valueLabels);
                                            facetGroup.Facets.Add(newFacet);
                                        }
                                    }
                                }
                            }
                            else if (filter is RangeFilter)
                            {
                                facetGroup.FacetType = FacetTypes.Range;

                                var key = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", filter.Key, group.Key).ToLower();
                                if (facets.ContainsKey(key))
                                {
                                    var facet = facets[key] as SingleBucketAggregate;
                                    if (facet != null && facet.DocCount > 0)
                                    {

                                        var newFacet = new Facet(facetGroup, group.Key, (int)facet.DocCount, valueLabels);
                                        facetGroup.Facets.Add(newFacet);
                                    }
                                }
                            }
                            else if (filter is CategoryFilter)
                            {
                                facetGroup.FacetType = FacetTypes.Category;

                                var key = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", filter.Key, group.Key).ToLower();
                                if (facets.ContainsKey(key))
                                {
                                    var facet = facets[key] as SingleBucketAggregate;
                                    if (facet != null && facet.DocCount > 0)
                                    {
                                        var newFacet = new Facet(facetGroup, group.Key, (int)facet.DocCount, valueLabels);
                                        facetGroup.Facets.Add(newFacet);
                                    }
                                }
                            }
                        }
                    }

                    // Add facet group only if has items
                    if (facetGroup.Facets.Any())
                    {
                        result.Add(facetGroup);
                    }
                }
            }

            return result.ToArray();
        }
    }
}
