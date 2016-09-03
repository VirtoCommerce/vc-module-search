using System.Collections.Generic;
using VirtoCommerce.SearchModule.Data.Model.Filters;

namespace VirtoCommerce.SearchModule.Data.Model.Search.Criterias
{
    public interface ISearchCriteria
    {
        /// <summary>
        /// The type of document that will be retrived from the search index.
        /// </summary>
        string DocumentType { get; }

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

        string[] Pricelists { get; set; }

        /*
        /// <summary>
        /// A list of aggegators to apply
        /// </summary>
        string[] Facets { get; set; }

        /// <summary>
        /// A list of filters with key values that need to be applied
        /// </summary>
        string[] Filters { get; set; }
        */

        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <value>The filters.</value>
        ISearchFilter[] Filters { get; }

        /// <summary>
        /// Adds the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        void Add(ISearchFilter filter);

        ISearchFilter[] CurrentFilters { get; }

        /// <summary>
        /// Applies the specified filter.
        /// </summary>
        /// <param name="field">The field.</param>
        void Apply(ISearchFilter filter);
    }
}
