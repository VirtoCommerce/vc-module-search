using System;
using Microsoft.Azure.Search.Models;

namespace VirtoCommerce.SearchModule.Data.Providers.AzureSearch
{
    [CLSCompliant(false)]
    public class AzureSearchQuery
    {
        public string SearchText { get; set; }
        public SearchParameters SearchParameters { get; set; }
    }
}
