using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;
using VirtoCommerce.SearchModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests
{
    public class SearchManagerScenarios
    {
        [Fact]
        public void Can_register_new_search_provider()
        {
            var searchConnection = new SearchConnection("provider=ElasticSearch;server=~/App_Data/Lucene;scope=default");
            var searchProviderManager = new SearchProviderManager(searchConnection);

            searchProviderManager.RegisterSearchProvider(SearchProviders.Elasticsearch.ToString(), connection => new ElasticSearchProvider(new ElasticSearchQueryBuilder(), connection));
            searchProviderManager.RegisterSearchProvider(SearchProviders.Lucene.ToString(), connection => new LuceneSearchProvider(new LuceneSearchQueryBuilder(), connection));

            searchProviderManager.RegisterSearchProvider(SearchProviders.Elasticsearch.ToString(), connection => new ElasticSearchProvider(new SampleQueryBuilder(), connection));
            Assert.True(searchProviderManager.QueryBuilder.GetType() == typeof(SampleQueryBuilder));
        }
    }

    [CLSCompliant(false)]
    public class SampleQueryBuilder : ElasticSearchQueryBuilder
    {

    }
}
