using Common.Logging;
using Hangfire;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Web.BackgroundJobs
{
    public class SearchIndexJobs
    {
        private readonly ISearchIndexController _controller;
        private readonly ISearchConnection _searchConnection;
        private readonly ILog _logging; 
        public SearchIndexJobs(ISearchIndexController controller, ISearchConnection searchConnection, ILog logging)
        {
            _controller = controller;
            _searchConnection = searchConnection;
            _logging = logging;
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public void Process(string documentType = null, string[] documentIds = null)
        {
            _controller.BuildIndex(_searchConnection.Scope, documentType, x=> { _logging.Trace(x.Description); }, documentIds);
        }

    }
}
