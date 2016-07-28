using System;
using System.IO;
using VirtoCommerce.Domain.Search.Services;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;

namespace VirtoCommerce.SearchModule.Tests
{
    public class SearchTestsBase : IDisposable
    {
        private string _LuceneStorageDir = Path.Combine(Path.GetTempPath(), "lucene");

        protected ISearchProvider GetSearchProvider(string searchProvider, string scope)
        {
            if (searchProvider == "Lucene")
            {
                var queryBuilder = new LuceneSearchQueryBuilder();

                var conn = new SearchConnection(_LuceneStorageDir, scope);
                var provider = new LuceneSearchProvider(queryBuilder, conn);

                return provider;
            }

            throw new NullReferenceException(string.Format("{0} is not supported", searchProvider));
        }

        public virtual void Dispose()
        {
            try
            {
                //Directory.Delete(_LuceneStorageDir, true);
            }
            finally
            {
            }
        }

    }
}
