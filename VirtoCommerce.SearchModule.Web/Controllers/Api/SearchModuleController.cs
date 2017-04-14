using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Hangfire;
using Omu.ValueInjecter;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Security;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search.Criteria;
using VirtoCommerce.SearchModule.Web.Model;
using VirtoCommerce.SearchModule.Web.Model.PushNotifications;
using VirtoCommerce.SearchModule.Web.Security;

namespace VirtoCommerce.SearchModule.Web.Controllers.Api
{
    [RoutePrefix("api/search")]
    public class SearchModuleController : ApiController
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ISearchConnection _searchConnection;
        private readonly IPushNotificationManager _pushNotifier;
        private readonly ISearchIndexController _searchIndexController;
        private readonly IUserNameResolver _userNameResolver;

        public SearchModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection, ISearchIndexController searchIndexController, IPushNotificationManager pushNotifier, IUserNameResolver userNameResolver)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _pushNotifier = pushNotifier;
            _searchIndexController = searchIndexController;
            _userNameResolver = userNameResolver;
        }

        /// <summary>
        /// Get search index for specified document type and document id.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("index/{documentType}/{documentId}")]
        [ResponseType(typeof(DocumentDictionary[]))]
        [ApiExplorerSettings(IgnoreApi = true)]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        public IHttpActionResult GetDocumentIndex(string documentType, string documentId)
        {
            var criteria = new BaseSearchCriteria(documentType) { Ids = new[] { documentId } };
            var result = _searchProvider.Search<DocumentDictionary>(_searchConnection.Scope, criteria);
            return Ok(result?.Documents);
        }

        /// <summary>
        /// Index specified document or all documents specified type
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("index/{documentType?}")]
        [ResponseType(typeof(IndexProgressPushNotification))]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult IndexDocuments([FromBody] IndexDocumentId[] documentsIds, string documentType = null)
        {
            var notification = new IndexProgressPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Indexation process",
                Description = documentType != null ? $"Starting {documentType} indexation" : "Starting full indexation"
            };
            _pushNotifier.Upsert(notification);
            string[] ids = null;
            if (documentsIds != null)
            {
                ids = documentsIds.Select(x => x.Id).Distinct().ToArray();
            }

            BackgroundJob.Enqueue(() => BackgroundIndex(_searchConnection.Scope, documentType, ids, notification));

            return Ok(notification);
        }

        /// <summary>
        /// Reindex specified document or all documents specified type
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("reindex/{documentType?}")]
        [ResponseType(typeof(IndexProgressPushNotification))]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult ReindexDocuments([FromBody] IndexDocumentId[] documentsIds, string documentType = null)
        {
            var notification = new IndexProgressPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Reindexation process",
                Description = documentType != null ? "Starting reindex for " + documentType : "Starting full index rebuild"
            };
            _pushNotifier.Upsert(notification);

            string[] ids = null;
            if (documentsIds != null)
            {
                ids = documentsIds.Select(x => x.Id).Distinct().ToArray();
            }

            _searchIndexController.RemoveIndex(_searchConnection.Scope, documentType, ids);
            BackgroundJob.Enqueue(() => BackgroundIndex(_searchConnection.Scope, documentType, ids, notification));

            return Ok(notification);
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        // Only public methods can be invoked in the background. (Hangfire)
        public void BackgroundIndex(string scope, string documentType, string[] documentsIds, IndexProgressPushNotification notification)
        {
            Action<IndexProgressInfo> progressCallback = x =>
            {
                notification.InjectFrom(x);
                _pushNotifier.Upsert(notification);
            };

            try
            {
                _searchIndexController.BuildIndex(scope, documentType, progressCallback, documentsIds);
            }
            catch (Exception ex)
            {
                notification.Description = "Index error";
                notification.ErrorCount++;
                notification.Errors.Add(ex.ToString());
            }
            finally
            {
                notification.Finished = DateTime.UtcNow;
                notification.Description = "Indexation finished" + (notification.Errors.Any() ? " with errors" : " successfully");
                _pushNotifier.Upsert(notification);
            }
        }
    }
}
