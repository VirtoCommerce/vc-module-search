using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public interface ISearchProvider
    {
        ISearchQueryBuilder[] QueryBuilders { get; }

        void Close(string scope, string documentType);

        void Commit(string scope);

        void Index<T>(string scope, string documentType, T document);

        int Remove(string scope, string documentType, string key, string value);

        void RemoveAll(string scope, string documentType);

        ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class;
    }

}
