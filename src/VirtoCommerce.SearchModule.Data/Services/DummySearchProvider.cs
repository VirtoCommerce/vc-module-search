using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Exceptions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class DummySearchProvider : ISearchProvider
    {
        private readonly string _error = "There's no registered Search Provider. Please install at least one Search Module implementation (Lucene, Elastic Search or Azure Search).";

        bool ISearchProvider.IsIndexSwappingSupported => throw new SearchException(_error);

        public Task SwapIndexAsync(string documentType)
        {
            throw new SearchException(_error);
        }

        public Task DeleteIndexAsync(string documentType)
        {
            throw new SearchException(_error);
        }

        public Task<IndexingResult> IndexAsync(string documentType, IList<IndexDocument> documents, bool reindex = false)
        {
            throw new SearchException(_error);
        }

        public Task<IndexingResult> RemoveAsync(string documentType, IList<IndexDocument> documents)
        {
            throw new SearchException(_error);
        }

        public Task<SearchResponse> SearchAsync(string documentType, SearchRequest request)
        {
            throw new SearchException(_error);
        }
    }
}
