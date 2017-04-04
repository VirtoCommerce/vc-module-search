using System;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{
    public class AzureSearchQueryBuilder : ISearchQueryBuilder
    {
        public string DocumentType => string.Empty;

        public object BuildQuery<T>(string scope, ISearchCriteria criteria)
            where T : class
        {
            throw new NotImplementedException();
        }
    }
}
