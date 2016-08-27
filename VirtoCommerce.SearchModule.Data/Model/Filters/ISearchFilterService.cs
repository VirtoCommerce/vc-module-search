using System;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Data.Model.Filters
{
   public interface IBrowseFilterService
    {
        ISearchFilter[] GetFilters(IDictionary<string, object> context);
    }
}
