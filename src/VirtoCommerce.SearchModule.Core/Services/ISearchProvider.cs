using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface ISearchProvider
    {
        Task DeleteIndexAsync(string documentType);

        /// <summary>
        /// Single point entry for documents indexation
        /// </summary>
        /// <param name="documentType">Document type (product, category, etc)</param>
        /// <param name="documents">Batch of documents to index</param>
        /// <param name="parameters">Class wraps two options:
        /// 1. Optional indexation mode. Diffetent search providers will interprent it differently.
        /// If the prodivder supposrts index swapping then indextation will occur on the backup index.
        /// Use 'SwapIndex(documentType)' to swich indeces after all documents have been indexed.
        /// 2. Optional partial inxed partialUpdate. True value partialUpdate only passed fields and keeps unpassed</param>
        Task<IndexingResult> IndexAsync(string documentType, IList<IndexDocument> documents);
        Task<IndexingResult> RemoveAsync(string documentType, IList<IndexDocument> documents);
        Task<SearchResponse> SearchAsync(string documentType, SearchRequest request);
    }
}
