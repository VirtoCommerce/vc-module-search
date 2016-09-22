using System;
using System.IO;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;

namespace VirtoCommerce.SearchModule.Tests
{
    public class SearchTestsBase : IDisposable
    {
        private string _LuceneStorageDir = Path.Combine(Path.GetTempPath(), "lucene");

        protected Data.Model.ISearchProvider GetSearchProvider(string searchProvider, string scope)
        {
            if (searchProvider == "Lucene")
            {
           
                var conn = new SearchConnection(_LuceneStorageDir, scope);
                var provider = new LuceneSearchProvider(conn);

                return provider;
            }

            if (searchProvider == "Elastic")
            {
                var conn = new SearchConnection("localhost:9200", scope);
                var provider = new ElasticSearchProvider(conn);
                provider.EnableTrace = true;

                return provider;
            }

            throw new NullReferenceException(string.Format("{0} is not supported", searchProvider));
        }

        public virtual void Dispose()
        {
            try
            {
                if(Directory.Exists(_LuceneStorageDir))
                    Directory.Delete(_LuceneStorageDir, true);
            }
            finally
            {
            }
        }

    }
}
