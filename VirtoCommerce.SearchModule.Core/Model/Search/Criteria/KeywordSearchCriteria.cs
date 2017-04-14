namespace VirtoCommerce.SearchModule.Core.Model.Search.Criteria
{
    public class KeywordSearchCriteria1 //: BaseSearchCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordSearchCriteria1"/> class.
        /// </summary>
        /// <param name="documentType">Type of the document.</param>
        public KeywordSearchCriteria1(string documentType)
        //: base(documentType)
        {
        }

        /// <summary>
        /// Gets or sets the search phrase.
        /// </summary>
        /// <value>The search phrase.</value>
        public virtual string SearchPhrase { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        /// <value>The locale.</value>
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fuzzy search.
        /// </summary>
        /// <value><c>true</c> if this instance is fuzzy search; otherwise, <c>false</c>.</value>
        public virtual bool IsFuzzySearch { get; set; } = true;

        /// <summary>
        /// Supported values: 0, 1, 2, null (=auto)
        /// </summary>
        public virtual int? Fuzziness { get; set; }
    }
}
