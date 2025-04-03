using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.SearchModule.Core.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using Permissions = VirtoCommerce.SearchModule.Core.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.SearchModule.Web.Controllers.Api
{
    [Route("api/search/indexes")]
    [Produces("application/json")]
    [Authorize]
    public class SearchIndexationModuleController : Controller
    {
        private readonly IEnumerable<IndexDocumentConfiguration> _documentConfigs;
        private readonly ISearchProvider _searchProvider;
        private readonly IIndexingManager _indexingManager;
        private readonly IIndexingJobService _indexingJobService;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IPushNotificationManager _pushNotifier;

        public SearchIndexationModuleController(
            IEnumerable<IndexDocumentConfiguration> documentConfigs,
            ISearchProvider searchProvider,
            IIndexingManager indexingManager,
            IIndexingJobService indexingJobService,
            IUserNameResolver userNameResolver,
            IPushNotificationManager pushNotifier)
        {
            _documentConfigs = documentConfigs;
            _searchProvider = searchProvider;
            _indexingManager = indexingManager;
            _indexingJobService = indexingJobService;
            _userNameResolver = userNameResolver;
            _pushNotifier = pushNotifier;
        }

        [HttpGet]
        [Route("")]
        [Authorize(Permissions.IndexRead)]
        public async Task<ActionResult<IndexState[]>> GetIndicesAsync()
        {
            var documentTypes = GetDocumentTypes();
            var result = await Task.WhenAll(documentTypes.Select(_indexingManager.GetIndexStateAsync));
            return Ok(result);
        }

        [HttpGet]
        [Route("all")]
        [Authorize(Permissions.IndexRead)]
        public async Task<ActionResult<IndexState[]>> GetAllIndicesAsync()
        {
            var documentTypes = GetDocumentTypes();
            var indicesResult = await Task.WhenAll(documentTypes.Select(_indexingManager.GetIndicesStateAsync));
            var results = indicesResult.SelectMany(x => x);
            return Ok(results);
        }

        /// <summary>
        /// Get search index for specified document type and document id.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("index/{documentType}/{documentId}")]
        [Authorize(Permissions.IndexRead)]
        public async Task<ActionResult<IndexDocument[]>> GetDocumentIndexAsync(string documentType, string documentId)
        {
            var request = new SearchRequest
            {
                Filter = new IdsFilter
                {
                    Values = [documentId],
                },
            };

            var result = await _searchProvider.SearchAsync(documentType, request);
            return Ok(result.Documents);
        }

        /// <summary>
        /// Run indexation process for specified options
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("index")]
        [Authorize(Permissions.IndexRebuild)]
        public ActionResult<IndexProgressPushNotification> IndexDocuments([FromBody] IndexingOptions[] options)
        {
            var currentUserName = _userNameResolver.GetCurrentUserName();
            var notification = _indexingJobService.Enqueue(currentUserName, options);
            _pushNotifier.Send(notification);
            return Ok(notification);
        }


        [HttpGet]
        [Route("tasks/{taskId}/cancel")]
        [Authorize(Permissions.IndexRebuild)]
        public ActionResult CancelIndexationProcess(string taskId)
        {
            _indexingJobService.CancelIndexation();
            return Ok();
        }

        [HttpGet]
        [Route("swapIndexSupported")]
        [Authorize(Permissions.IndexRead)]
        public ActionResult GetSwapIndexSupported()
        {
            var documentTypes = GetDocumentTypes();
            return Ok(new { Result = documentTypes.Any(x => _searchProvider.Is<ISupportIndexSwap>(x)) });
        }

        [HttpPost]
        [Route("swapIndex")]
        [Authorize(Permissions.IndexRebuild)]
        public async Task<ActionResult> SwapIndexAsync([FromBody] IndexingOptions option)
        {
            var documentType = option.DocumentType;

            if (_searchProvider.Is<ISupportIndexSwap>(documentType, out var supportIndexSwapSearchProvider))
            {
                await supportIndexSwapSearchProvider.SwapIndexAsync(documentType);
            }

            return Ok();
        }


        private IEnumerable<string> GetDocumentTypes()
        {
            return _documentConfigs.Select(x => x.DocumentType).Distinct();
        }
    }
}
