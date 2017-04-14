using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Filters;

namespace VirtoCommerce.SearchModule.Core.Model.Search.Criteria
{
    public interface ISearchCriteria
    {
        /// <summary>
        /// The type of document that will be retrived from the search index.
        /// </summary>
        string DocumentType { get; }

        IList<string> Ids { get; set; }

        string RawQuery { get; set; }

        IList<ISearchFilter> CurrentFilters { get; }

        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <value>The filters.</value>
        IList<ISearchFilter> Filters { get; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        /// <value>The currency.</value>
        string Currency { get; set; }

        IList<string> Pricelists { get; set; }

        /// <summary>
        /// Gets the sorts.
        /// </summary>
        /// <value>The sorts.</value>
        SearchSort Sort { get; set; }

        /// <summary>
        /// Gets or sets the starting record.
        /// </summary>
        /// <value>The starting record.</value>
        int StartingRecord { get; set; }

        /// <summary>
        /// Gets or sets the records to retrieve.
        /// </summary>
        /// <value>The records to retrieve.</value>
        int RecordsToRetrieve { get; set; }

        /// <summary>
        /// Gets or sets the search phrase.
        /// </summary>
        /// <value>The search phrase.</value>
        string SearchPhrase { get; set; }

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        /// <value>The locale.</value>
        string Locale { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fuzzy search.
        /// </summary>
        /// <value><c>true</c> if this instance is fuzzy search; otherwise, <c>false</c>.</value>
        bool IsFuzzySearch { get; set; }

        /// <summary>
        /// Supported values: 0, 1, 2, null (=auto)
        /// </summary>
        int? Fuzziness { get; set; }


        /// <summary>
        /// Adds the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        void Add(ISearchFilter filter);

        /// <summary>
        /// Applies the specified filter.
        /// </summary>
        /// <param name="filter"></param>
        void Apply(ISearchFilter filter);
    }
}
