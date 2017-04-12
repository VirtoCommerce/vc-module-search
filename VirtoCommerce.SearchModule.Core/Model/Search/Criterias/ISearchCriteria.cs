using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Filters;

namespace VirtoCommerce.SearchModule.Core.Model.Search.Criterias
{
    public interface ISearchCriteria
    {
        /// <summary>
        /// The type of document that will be retrived from the search index.
        /// </summary>
        string DocumentType { get; }

        string RawQuery { get; set; }

        IList<string> Ids { get; set; }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        string CacheKey { get; }

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
        /// Gets the sorts.
        /// </summary>
        /// <value>The sorts.</value>
        SearchSort Sort { get; set; }

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        /// <value>The locale.</value>
        string Locale { get; set; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        /// <value>The currency.</value>
        string Currency { get; set; }

        IList<string> Pricelists { get; set; }

        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <value>The filters.</value>
        IList<ISearchFilter> Filters { get; }

        IList<ISearchFilter> CurrentFilters { get; }

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
