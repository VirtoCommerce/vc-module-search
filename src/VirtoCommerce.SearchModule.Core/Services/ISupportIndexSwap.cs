using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface ISupportIndexSwap
    {
        /// <summary>
        /// Whether of not a searh index implementation supports index swapping (blue-green indexation)
        /// </summary>
        bool IsIndexSwappingSupported { get; }
        Task SwapIndexAsync(string documentType);
        Task<IndexingResult> IndexWithBackupAsync(string documentType, IList<IndexDocument> documents);
    }
}
