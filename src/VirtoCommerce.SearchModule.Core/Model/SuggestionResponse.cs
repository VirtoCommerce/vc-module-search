using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model;

public class SuggestionResponse
{
    public IList<string> Suggestions { get; set; } = new List<string>();
}
