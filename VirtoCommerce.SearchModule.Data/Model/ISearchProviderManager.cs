using System;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Data.Model
{
    public interface ISearchProviderManager
    {
        ISearchConnection CurrentConnection { get; }

        ISearchProvider CurrentProvider { get; }

        IEnumerable<string> RegisteredProviders { get; }

        void RegisterSearchProvider(string name, Func<ISearchConnection, ISearchProvider> factory);
    }

}
