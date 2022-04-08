using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface ISupportPartialUpdate
    {
        Task<IndexingResult> IndexPartialAsync(string documentType, IList<IndexDocument> documents);
    }
}
