using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public interface ISearchResults<T> where T : class
    {
        IEnumerable<T> Documents { get; }

        ISearchCriteria SearchCriteria { get; }

        long DocCount { get; }

        FacetGroup[] Facets { get; }

        string[] Suggestions { get;}

        long TotalCount { get; }
    }
}
