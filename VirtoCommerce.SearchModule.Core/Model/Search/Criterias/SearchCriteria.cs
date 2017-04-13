using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Filters;

namespace VirtoCommerce.SearchModule.Core.Model.Search.Criterias
{
    public class SearchCriteria : ISearchCriteria
    {
        public SearchCriteria(string documentType)
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
