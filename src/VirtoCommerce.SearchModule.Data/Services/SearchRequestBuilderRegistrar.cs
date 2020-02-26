using System;
using System.Collections.Concurrent;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class SearchRequestBuilderRegistrar : ISearchRequestBuilderRegistrar
    {
        private readonly ConcurrentDictionary<string, Func<ISearchRequestBuilder>> _searchRequestBuilders = new ConcurrentDictionary<string, Func<ISearchRequestBuilder>>();
        public ISearchRequestBuilder GetRequestBuilderByDocumentType(string documentType)
        {

            var factory = _searchRequestBuilders[documentType];
            if (factory == null)
            {
                throw new InvalidOperationException($"Search request builder for document type {documentType} not registered yet");
            }

            return factory();

        }

        public void Register<TSearchRequestBuilder>(string documentType, Func<TSearchRequestBuilder> factory) where TSearchRequestBuilder : class, ISearchRequestBuilder
        {
            if (!_searchRequestBuilders.TryAdd(documentType, factory))
            {
                throw new InvalidOperationException($"There is already registered Search Request Builder for the \"{documentType}\" document type.");
            }
        }

        public void Override<TSearchRequestBuilder>(string documentType, Func<TSearchRequestBuilder> factory) where TSearchRequestBuilder : class, ISearchRequestBuilder
        {
            _searchRequestBuilders.AddOrUpdate(documentType, factory, (key, oldValue) => factory);
        }
    }
}
