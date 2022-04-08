using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface ISupportIndexSwap
    {
        Task SwapIndexAsync(string documentType);
        Task<IndexingResult> IndexWithBackupAsync(string documentType, IList<IndexDocument> documents);
    }
}
