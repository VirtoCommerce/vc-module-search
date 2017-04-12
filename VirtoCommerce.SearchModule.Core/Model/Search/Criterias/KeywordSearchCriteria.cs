using System;

namespace VirtoCommerce.SearchModule.Core.Model.Search.Criterias
{
    public class KeywordSearchCriteria : SearchCriteriaBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordSearchCriteria"/> class.
        /// </summary>
        /// <param name="documentType">Type of the document.</param>
        public KeywordSearchCriteria(string documentType)
            : base(documentType)
        {
            SearchPhrase = string.Empty;
            IsFuzzySearch = true;
        }

        /// <summary>
        /// Gets or sets the search phrase.
        /// </summary>
        /// <value>The search phrase.</value>
        public string SearchPhrase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fuzzy search.
        /// </summary>
        /// <value><c>true</c> if this instance is fuzzy search; otherwise, <c>false</c>.</value>
        public bool IsFuzzySearch { get; set; }

        /// <summary>
        /// Supported values: 0, 1, 2, null (=auto)
        /// </summary>
        public int? Fuzziness { get; set; }

        /// <summary>
        /// Gets or sets the fuzzy min similarity.
        /// </summary>
        /// <value>The fuzzy min similarity.</value>
        [Obsolete("Use Fuzziness")]
        public float FuzzyMinSimilarity { get; set; }
    }
}
