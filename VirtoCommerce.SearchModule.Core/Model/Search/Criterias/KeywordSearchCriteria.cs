namespace VirtoCommerce.SearchModule.Core.Model.Search.Criterias
{
    public class KeywordSearchCriteria : SearchCriteriaBase
    {
        private string _SearchPhrase = string.Empty;

        /// <summary>
        /// Gets or sets the search phrase.
        /// </summary>
        /// <value>The search phrase.</value>
        public virtual string SearchPhrase
        {
            get { return _SearchPhrase; }
            set { ChangeState(); _SearchPhrase = value; }
        }

        private bool _isFuzzySearch = true;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fuzzy search.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is fuzzy search; otherwise, <c>false</c>.
        /// </value>
        public bool IsFuzzySearch
        {
            get { return _isFuzzySearch; }
            set { ChangeState(); _isFuzzySearch = value; }
        }

        private float _fuzzyMinSimilarity = 0.7f;

        /// <summary>
        /// Gets or sets the fuzzy min similarity.
        /// </summary>
        /// <value>The fuzzy min similarity.</value>
        public float FuzzyMinSimilarity
        {
            get { return _fuzzyMinSimilarity; }
            set { ChangeState(); _fuzzyMinSimilarity = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordSearchCriteria"/> class.
        /// </summary>
        /// <param name="documentType">Type of the document.</param>
        public KeywordSearchCriteria(string documentType)
            : base(documentType)
        {

        }
    }
}
