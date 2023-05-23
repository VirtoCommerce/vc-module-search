using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model;

public class SuggestionRequest
{
    /// <summary>
    /// A word, phrase, or text fragment which will be used to make suggestions
    /// </summary>
    public string Query { get; set; }

    /// <summary>
    /// Indexed document fields for which to return suggestions
    /// </summary>
    public IList<string> Fields { get; set; }

    /// <summary>
    /// Number of suggestions to return
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Various request data that for a provider to interpred
    /// </summary>
    public IDictionary<string, object> QueryContext { get; set; }

    /// <summary>
    /// Gets or sets the option to specifically use backup index.
    /// </summary>
    public bool UseBackupIndex { get; set; }
}
