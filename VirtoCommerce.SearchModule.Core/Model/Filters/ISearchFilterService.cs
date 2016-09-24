using System;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Filters
{
   public interface IBrowseFilterService
    {
        ISearchFilter[] GetFilters(IDictionary<string, object> context);
    }
}
