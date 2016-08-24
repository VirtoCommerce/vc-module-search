using Nest;
using System;
using System.Collections.Generic;
using VirtoCommerce.Domain.Search.Model;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    public class SearchResults<T> : Model.ISearchResults<T> where T:class
    {
        public SearchResults(ISearchResponse<T> response)
        {
            this.Documents = response.Documents;
            this.DocCount = response.HitsMetaData.Total;
            this.TotalCount = response.Total;
        }

        public IDictionary<string, object> Aggregations
        {
            get;
            private set;
        }

        public long DocCount
        {
            get;
            private set;
        }

        public IEnumerable<T> Documents
        {
            get;
            private set;
        }

        public ISearchCriteria SearchCriteria
        {
            get;
            private set;
        }

        public long TotalCount
        {
            get;
            private set;
        }
    }
}
