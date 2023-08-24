using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface ISupportCognitiveSearch
    {
        public Task<SearchResponse> CognitiveSearchAsync(string documentType, CognitiveSearchRequest request);
    }
}
