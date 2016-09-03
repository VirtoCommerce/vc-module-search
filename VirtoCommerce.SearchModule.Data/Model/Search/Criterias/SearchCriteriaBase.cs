﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtoCommerce.SearchModule.Data.Model.Filters;

namespace VirtoCommerce.SearchModule.Data.Model.Search.Criterias
{
    public abstract class SearchCriteriaBase : ISearchCriteria
    {
        private readonly string _documentType;

        public virtual string DocumentType
        {
            get { return _documentType; }
        }

        bool _cacheResults = true;
        public virtual bool CacheResults
        {
            get
            {
                return _cacheResults;
            }

            set
            {
                _cacheResults = value;
            }
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>
        /// The cache key.
        /// </value>
        public virtual string CacheKey
        {
            get
            {
                var key = new StringBuilder();

                key.Append("_dc:" + DocumentType);
                key.Append("_st:" + StartingRecord);
                key.Append("_en:" + RecordsToRetrieve);
                key.Append("_lc:" + Locale);
                key.Append("_cr:" + Currency);
                if (Sort != null)
                    key.Append("_st:" + Sort);

                // Add active fields
                foreach (var field in Filters)
                {
                    key.Append("_f:" + field.CacheKey);
                }

                return key.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the starting record.
        /// </summary>
        /// <value>The starting record.</value>
        public virtual int StartingRecord { get; set; }

        int _recordsToRetrieve = 50;
        /// <summary>
        /// Gets or sets the records to retrieve.
        /// </summary>
        /// <value>The records to retrieve.</value>
        public virtual int RecordsToRetrieve
        {
            get
            {
                return _recordsToRetrieve;
            }
            set
            {
                _recordsToRetrieve = value;
            }
        }

        public virtual SearchSort Sort { get; set; }

        public virtual string Locale
        {
            get; set;
        }

        public virtual string Currency { get; set; }

        /// <summary>
        /// Gets or sets the price lists that should be considered for filtering.
        /// </summary>
        /// <value>
        /// The price lists.
        /// </value>
        public virtual string[] Pricelists
        {
            get;set;
        }

        List<ISearchFilter> _filters = new List<ISearchFilter>();

        public virtual ISearchFilter[] Filters
        {
            get { return _filters.ToArray(); }
        }

        public virtual void Add(ISearchFilter filter)
        {
            _filters.Add(filter);
        }

        List<ISearchFilter> _appliedFilters = new List<ISearchFilter>();

        public virtual ISearchFilter[] CurrentFilters
        {
            get { return _appliedFilters.ToArray(); }
        }

        public virtual void Apply(ISearchFilter filter)
        {
            if (filter != null)
            {
                _appliedFilters.Add(filter);
            }
        }

        protected SearchCriteriaBase(string documentType)
        {
            _documentType = documentType;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is modified.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is modified; otherwise, <c>false</c>.
        /// </value>
        protected bool IsModified { get; set; }

        /// <summary>
        /// Changes the state.
        /// </summary>
        protected virtual void ChangeState()
        {
            IsModified = true;
        }
    }
}
