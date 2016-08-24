using System.Collections.Generic;
using VirtoCommerce.Domain.Search.Model;

namespace VirtoCommerce.SearchModule.Data.Model
{
    public interface ISearchResults<T> where T : class
    {
        IEnumerable<T> Documents { get; }

        ISearchCriteria SearchCriteria { get; }

        long DocCount { get; }

        FacetGroup[] Facets { get; }
        
        long TotalCount { get; }
    }
}
