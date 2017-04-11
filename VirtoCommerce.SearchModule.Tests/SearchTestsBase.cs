﻿using System;
using System.IO;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Providers.Azure;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;

namespace VirtoCommerce.SearchModule.Test
{
    public class SearchTestsBase : IDisposable
    {
        private readonly string _luceneStorageDir = Path.Combine(Path.GetTempPath(), "lucene");

        protected ISearchProvider GetSearchProvider(string searchProvider, string scope)
        {
            ISearchProvider provider = null;

            if (searchProvider == "Lucene")
            {
                var connection = new SearchConnection(_luceneStorageDir, scope);
                var queryBuilder = new LuceneSearchQueryBuilder() as ISearchQueryBuilder;
                provider = new LuceneSearchProvider(new[] { queryBuilder }, connection);
            }

            if (searchProvider == "Elastic")
            {
                var elasticsearchHost = Environment.GetEnvironmentVariable("TestElasticsearchHost") ?? "localhost:9200";

                var connection = new SearchConnection(elasticsearchHost, scope);
                var queryBuilder = new ElasticSearchQueryBuilder() as ISearchQueryBuilder;
                provider = new ElasticSearchProvider(new[] { queryBuilder }, connection) { EnableTrace = true };
            }

            if (searchProvider == "Azure")
            {
                var azureSearchServiceName = Environment.GetEnvironmentVariable("TestAzureSearchServiceName");
                var azureSearchAccessKey = Environment.GetEnvironmentVariable("TestAzureSearchAccessKey");

                var connection = new SearchConnection(azureSearchServiceName, scope, accessKey: azureSearchAccessKey);
                var queryBuilder = new AzureSearchQueryBuilder() as ISearchQueryBuilder;
                provider = new AzureSearchProvider(connection, new[] { queryBuilder });
            }

            if (provider == null)
                throw new ArgumentException($"Search provider '{searchProvider}' is not supported", nameof(searchProvider));

            return provider;
        }

        public int GetFacetCount(ISearchResults<DocumentDictionary> results, string fieldName, string facetKey)
        {
            if (results.Facets == null || results.Facets.Length == 0)
            {
                return 0;
            }

            var group = results.Facets.SingleOrDefault(fg => fg.FieldName.EqualsInvariant(fieldName));

            return group?.Facets
                       .Where(facet => facet.Key == facetKey)
                       .Select(facet => facet.Count)
                       .FirstOrDefault() ?? 0;
        }

        public virtual void Dispose()
        {
            try
            {
                if (Directory.Exists(_luceneStorageDir))
                    Directory.Delete(_luceneStorageDir, true);
            }
            catch
            {
                // ignored
            }
        }
    }
}
