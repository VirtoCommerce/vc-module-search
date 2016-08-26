using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class SearchProviderManager : ISearchProviderManager, Model.ISearchProvider
    {
        private readonly ISearchConnection _connection;
        private readonly ConcurrentDictionary<string, Func<ISearchConnection, ISearchProvider>> _factories;
        private ISearchProvider _currentProvider;

        public SearchProviderManager(ISearchConnection connection)
        {
            _connection = connection;
            _factories = new ConcurrentDictionary<string, Func<ISearchConnection, Model.ISearchProvider>>(StringComparer.OrdinalIgnoreCase);
        }

        #region ISearchProviderManager Members

        public void RegisterSearchProvider(string name, Func<ISearchConnection, Model.ISearchProvider> factory)
        {
            _factories.AddOrUpdate(name, factory, (key, oldValue) => factory);
        }

        public IEnumerable<string> RegisteredProviders
        {
            get { return _factories.Keys; }
        }

        public ISearchProvider CurrentProvider
        {
            get { return _currentProvider ?? (_currentProvider = CreateProvider()); }
        }

        public ISearchConnection CurrentConnection
        {
            get { return _connection; }
        }

        #endregion

        #region ISearchProvider Members

        public Model.ISearchQueryBuilder QueryBuilder
        {
            get { return CurrentProvider.QueryBuilder; }
        }

        public ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            return CurrentProvider.Search<T>(scope, criteria);
        }

        public void Index<T>(string scope, string documentType, T document)
        {
            CurrentProvider.Index<T>(scope, documentType, document);
        }

        public int Remove(string scope, string documentType, string key, string value)
        {
            return CurrentProvider.Remove(scope, documentType, key, value);
        }

        public void RemoveAll(string scope, string documentType)
        {
            CurrentProvider.RemoveAll(scope, documentType);
        }

        public void Close(string scope, string documentType)
        {
            CurrentProvider.Close(scope, documentType);
        }

        public void Commit(string scope)
        {
            CurrentProvider.Commit(scope);
        }

        #endregion

        private ISearchProvider CreateProvider()
        {
            ISearchProvider result = null;

            Func<ISearchConnection, ISearchProvider> factory;
            if (_factories.TryGetValue(_connection.Provider, out factory))
            {
                result = factory(_connection);
            }

            return result;
        }
    }
}
