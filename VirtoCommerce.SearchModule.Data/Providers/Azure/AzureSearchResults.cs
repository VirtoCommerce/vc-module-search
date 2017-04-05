using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search.Models;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{
    [CLSCompliant(false)]
    public class AzureSearchResults : ISearchResults<DocumentDictionary>
    {
        public AzureSearchResults(ISearchCriteria criteria, DocumentSearchResult<DocumentDictionary> searchResult)
        {
            SearchCriteria = criteria;
            Documents = GetDocuments(searchResult);
            DocCount = searchResult.Results.Count;
            TotalCount = searchResult.Count ?? 0;
            ProviderAggregations = searchResult.Facets;
            //Facets = CreateFacets(criteria, searchResult.Facets);
        }

        public IEnumerable<DocumentDictionary> Documents { get; }
        public ISearchCriteria SearchCriteria { get; }
        public long DocCount { get; }
        public FacetGroup[] Facets { get; }
        public string[] Suggestions { get; }
        public long TotalCount { get; }

        public FacetResults ProviderAggregations { get; set; }


        private static IList<DocumentDictionary> GetDocuments(DocumentSearchResult<DocumentDictionary> searchResult)
        {
            return searchResult.Results.Select(r => RenameFields(r.Document)).ToList();
        }

        private static DocumentDictionary RenameFields(DocumentDictionary document)
        {
            var result = new DocumentDictionary();

            foreach (var kvp in document)
            {
                var key = AzureFieldNameConverter.FromAzureFieldName(kvp.Key);
                result[key] = kvp.Value;
            }

            return result;
        }
    }
}
