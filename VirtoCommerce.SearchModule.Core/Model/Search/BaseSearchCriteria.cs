using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Filters;

namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public class BaseSearchCriteria : ISearchCriteria
    {
        public BaseSearchCriteria(string documentType)
        {
            DocumentType = documentType;
        }

        public virtual string DocumentType { get; }

        public virtual IList<string> Ids { get; set; }

        public virtual string RawQuery { get; set; }

        public virtual IList<ISearchFilter> CurrentFilters { get; } = new List<ISearchFilter>();

        public virtual IList<ISearchFilter> Filters { get; } = new List<ISearchFilter>();

        public virtual string Currency { get; set; }

        /// <summary>
        /// Gets or sets the price lists that should be considered for filtering.
        /// </summary>
        /// <value>
        /// The price lists.
        /// </value>
        public virtual IList<string> Pricelists { get; set; }

        public virtual SearchSort Sort { get; set; }

        /// <summary>
        /// Gets or sets the starting record.
        /// </summary>
        /// <value>The starting record.</value>
        public virtual int StartingRecord { get; set; }

        /// <summary>
        /// Gets or sets the records to retrieve.
        /// </summary>
        /// <value>The records to retrieve.</value>
        public virtual int RecordsToRetrieve { get; set; } = 50;

        /// <summary>
        /// Gets or sets the search phrase.
        /// </summary>
        /// <value>The search phrase.</value>
        public virtual string SearchPhrase { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        /// <value>The locale.</value>
        public virtual string Locale { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fuzzy search.
        /// </summary>
        /// <value><c>true</c> if this instance is fuzzy search; otherwise, <c>false</c>.</value>
        public virtual bool IsFuzzySearch { get; set; } = true;

        /// <summary>
        /// Supported values: 0, 1, 2, null (=auto)
        /// </summary>
        public virtual int? Fuzziness { get; set; }


        public virtual void Add(ISearchFilter filter)
        {
            if (filter != null)
            {
                Filters.Add(filter);
            }
        }

        public virtual void Apply(ISearchFilter filter)
        {
            if (filter != null)
            {
                CurrentFilters.Add(filter);
            }
        }
    }
}
