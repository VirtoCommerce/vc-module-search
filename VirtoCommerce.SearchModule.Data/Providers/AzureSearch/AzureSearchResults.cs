using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Azure.Search.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.AzureSearch
{
    [CLSCompliant(false)]
    public class AzureSearchResults : ISearchResults<DocumentDictionary>
    {
        public AzureSearchResults(ISearchCriteria criteria, DocumentSearchResult<DocumentDictionary> searchResult)
        {
            SearchCriteria = criteria;
            Documents = ConvertDocuments(searchResult);
            DocCount = searchResult.Results.Count;
            TotalCount = searchResult.Count ?? 0;
            ProviderAggregations = searchResult.Facets;
            Facets = ConvertFacets(searchResult.Facets, criteria);
        }

        public IList<DocumentDictionary> Documents { get; }
        public ISearchCriteria SearchCriteria { get; }
        public long DocCount { get; }
        public IList<FacetGroup> Facets { get; }
        public IList<string> Suggestions { get; }
        public long TotalCount { get; }

        public FacetResults ProviderAggregations { get; set; }


        private static IList<DocumentDictionary> ConvertDocuments(DocumentSearchResult<DocumentDictionary> searchResult)
        {
            return searchResult.Results.Select(r => RenameFields(r.Document)).ToList();
        }

        private static IList<FacetGroup> ConvertFacets(FacetResults facets, ISearchCriteria criteria)
        {
            var result = criteria.Filters.Select(f => ConvertFacet(f, facets, criteria))
                .Where(f => f != null && f.Facets.Any())
                .ToList();

            return result;
        }

        private static FacetGroup ConvertFacet(ISearchFilter filter, FacetResults facets, ISearchCriteria criteria)
        {
            FacetGroup result = null;

            var attributeFilter = filter as AttributeFilter;
            var rangeFilter = filter as RangeFilter;
            var priceRangeFilter = filter as PriceRangeFilter;

            if (attributeFilter != null)
            {
                result = ConvertAttributeFacet(attributeFilter, facets);
            }
            else if (rangeFilter != null)
            {
                result = ConvertRangeFacet(rangeFilter, facets);
            }
            else if (priceRangeFilter != null)
            {
                result = ConvertPriceFacet(priceRangeFilter, facets, criteria);
            }

            return result;
        }

        private static FacetGroup ConvertAttributeFacet(AttributeFilter filter, FacetResults facets)
        {
            FacetGroup result = null;

            if (filter != null)
            {
                var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key);
                var facetResults = facets.ContainsKey(azureFieldName) ? facets[azureFieldName] : null;

                if (facetResults != null && facetResults.Any())
                {
                    result = new FacetGroup(filter.Key, filter.GetLabels())
                    {
                        FacetType = FacetTypes.Attribute
                    };

                    var values = filter.GetValues();

                    if (values != null)
                    {
                        foreach (var group in values.GroupBy(v => v.Id))
                        {
                            var facetResult = facetResults.FirstOrDefault(r => ToStringInvariant(r.Value).EqualsInvariant(group.Key));
                            AddFacet(result, facetResult, group.Key, group.GetValueLabels());
                        }
                    }
                    else
                    {
                        // Return all facet results if values are not defined
                        foreach (var facetResult in facetResults)
                        {
                            var newFacet = new Facet(result, ToStringInvariant(facetResult.Value), facetResult.Count, null);
                            result.Facets.Add(newFacet);
                        }
                    }
                }
            }

            return result;
        }

        private static FacetGroup ConvertRangeFacet(RangeFilter filter, FacetResults facets)
        {
            FacetGroup result = null;

            if (filter != null)
            {
                var azureFieldName = AzureSearchHelper.ToAzureFieldName(filter.Key);
                var facetResults = facets.ContainsKey(azureFieldName) ? facets[azureFieldName] : null;

                if (facetResults != null && facetResults.Any())
                {
                    result = new FacetGroup(filter.Key, filter.GetLabels())
                    {
                        FacetType = FacetTypes.Range
                    };

                    foreach (var group in filter.Values.GroupBy(v => v.Id))
                    {
                        var facetResult = GetRangeFacetResult(group.First(), facetResults);
                        AddFacet(result, facetResult, group.Key, group.GetValueLabels());
                    }
                }
            }

            return result;
        }

        private static FacetGroup ConvertPriceFacet(PriceRangeFilter filter, FacetResults facets, ISearchCriteria criteria)
        {
            FacetGroup result = null;

            if (filter != null && filter.Currency.EqualsInvariant(criteria?.Currency))
            {
                result = new FacetGroup(filter.Key, filter.GetLabels())
                {
                    FacetType = FacetTypes.PriceRange
                };

                foreach (var group in filter.Values.GroupBy(v => v.Id))
                {
                    // Search all price facets and take first suitable result
                    var azureFieldNames = AzureSearchHelper.GetPriceFieldNames(filter.Key, criteria?.Currency, criteria?.Pricelists, false);
                    var facetResults = azureFieldNames.SelectMany(f => facets.ContainsKey(f) ? facets[f] : Enumerable.Empty<FacetResult>()).ToList();
                    var facetResult = GetRangeFacetResult(group.First(), facetResults);

                    AddFacet(result, facetResult, group.Key, group.GetValueLabels());
                }
            }

            return result;
        }

        private static FacetResult GetRangeFacetResult(RangeFilterValue filterValue, IEnumerable<FacetResult> facetResults)
        {
            var lower = filterValue.Lower == null ? null : filterValue.Lower.Length == 0 ? null : filterValue.Lower == "0" ? null : filterValue.Lower;
            var upper = filterValue.Upper;

            return facetResults.FirstOrDefault(r => r.Count > 0 && r.From?.ToString() == lower && r.To?.ToString() == upper);
        }

        private static void AddFacet(FacetGroup facetGroup, FacetResult facetResult, string key, FacetLabel[] labels)
        {
            if (facetResult != null && facetResult.Count > 0)
            {
                var newFacet = new Facet(facetGroup, key, facetResult.Count, labels);
                facetGroup.Facets.Add(newFacet);
            }
        }

        private static DocumentDictionary RenameFields(DocumentDictionary document)
        {
            var result = new DocumentDictionary();

            foreach (var kvp in document)
            {
                var key = AzureSearchHelper.FromAzureFieldName(kvp.Key);
                result[key] = kvp.Value;
            }

            return result;
        }

        private static string ToStringInvariant(object value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }
    }
}
