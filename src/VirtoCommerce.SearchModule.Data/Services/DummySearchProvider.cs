using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Exceptions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class DummySearchProvider : ISearchProvider
    {
        public Task DeleteIndexAsync(string documentType)
        {
            throw new SearchException("There is no a registered Search Provider!");
        }

        public Task<IndexingResult> IndexAsync(string documentType, IList<IndexDocument> documents, bool update = false)
        {
            throw new SearchException("There is no a registered Search Provider!");
        }

        public Task<IndexingResult> RemoveAsync(string documentType, IList<IndexDocument> documents)
        {
            throw new SearchException("There is no a registered Search Provider!");
        }

        public Task<SearchResponse> SearchAsync(string documentType, SearchRequest request)
        {
            throw new SearchException("There is no a registered Search Provider!");
        }
    }
}
