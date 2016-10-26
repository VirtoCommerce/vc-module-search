using Hangfire;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Web.BackgroundJobs
{
    public class SearchIndexJobs
    {
        private readonly ISearchIndexController _controller;

        public SearchIndexJobs(ISearchIndexController controller)
        {
            _controller = controller;
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public void Process(string scope, string documentType, string[] documentIds = null)
        {
            //_controller.Process(scope, documentType, documentId);
        }

    }
}
