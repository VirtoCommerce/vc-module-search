using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface ISupportSuggestions
{
    Task<SuggestionResponse> GetSuggestionsAsync(string documentType, SuggestionRequest request);
}
