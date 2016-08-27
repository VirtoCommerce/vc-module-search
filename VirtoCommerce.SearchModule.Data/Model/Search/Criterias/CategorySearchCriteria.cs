using System.Collections.Specialized;

namespace VirtoCommerce.SearchModule.Data.Model.Search.Criterias
{
    public class CategorySearchCriteria : KeywordSearchCriteria
    {
        public const string DocType = "category";

        /// <summary>
        /// Initializes a new instance of the <see cref="CategorySearchCriteria"/> class.
        /// </summary>
        /// <param name="documentType">Type of the document.</param>
        public CategorySearchCriteria(string documentType)
            : base(documentType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategorySearchCriteria"/> class.
        /// </summary>
        public CategorySearchCriteria()
            : base(DocType)
        {
        }

        private StringCollection _outlines = new StringCollection();
        /// <summary>
        /// Gets or sets the outlines. Outline consists of "Category1/Category2".
        /// </summary>
        /// <example>Everything/digital-cameras</example>
        /// <value>The outlines.</value>
        public virtual StringCollection Outlines
        {
            get { return _outlines; }
            set { ChangeState(); _outlines = value; }
        }
    }
}
