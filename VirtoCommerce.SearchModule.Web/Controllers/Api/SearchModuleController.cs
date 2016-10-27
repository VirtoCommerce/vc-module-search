using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Serialization;
using CacheManager.Core;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Security;
using VirtoCommerce.SearchModule.Web.BackgroundJobs;
using VirtoCommerce.SearchModule.Web.Security;
using Property = VirtoCommerce.Domain.Catalog.Model.Property;
using PropertyDictionaryValue = VirtoCommerce.Domain.Catalog.Model.PropertyDictionaryValue;
using webModel = VirtoCommerce.SearchModule.Web.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.SearchModule.Web.Model.PushNotifications;
using Omu.ValueInjecter;
using Hangfire;

namespace VirtoCommerce.SearchModule.Web.Controllers.Api
{
    [RoutePrefix("api/search")]
    public class SearchModuleController : ApiController
    {
        private const string _filteredBrowsingPropertyName = "FilteredBrowsing";

        private readonly ISearchProvider _searchProvider;
        private readonly ISearchConnection _searchConnection;
        private readonly IStoreService _storeService;
        private readonly ISecurityService _securityService;
        private readonly IPermissionScopeService _permissionScopeService;
        private readonly IPropertyService _propertyService;
        private readonly IBrowseFilterService _browseFilterService;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IPushNotificationManager _pushNotifier;
        private readonly ISearchIndexController _searchIndexController;
        private readonly IUserNameResolver _userNameResolver;

        public SearchModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection, IStoreService storeService, ISecurityService securityService, IPermissionScopeService permissionScopeService,
            IPropertyService propertyService, IBrowseFilterService browseFilterService, 
            IBlobUrlResolver blobUrlResolver, ICatalogSearchService catalogSearchService, ISearchIndexController searchIndexController, IPushNotificationManager pushNotifier, IUserNameResolver userNameResolver)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _storeService = storeService;
            _securityService = securityService;
            _permissionScopeService = permissionScopeService;
            _propertyService = propertyService;
            _browseFilterService = browseFilterService;
            _blobUrlResolver = blobUrlResolver;
            _catalogSearchService = catalogSearchService;
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
            var criteria = new KeywordSearchCriteria(documentType);

            criteria.SearchPhrase = "__key:" + documentId;

            var result = _searchProvider.Search<DocumentDictionary>(_searchConnection.Scope, criteria);
            return Ok(result != null ? result.Documents : null);
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
        public IHttpActionResult IndexDocuments([FromBody] string[] documentsIds, string documentType = null)
        {
            var notification = new IndexProgressPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Indexation process",
                Description = documentType != null ? string.Format("starting {0} indexations", documentType) : "starting full indexation"
            };
            _pushNotifier.Upsert(notification);

            BackgroundJob.Enqueue(() => BackgroundIndex(_searchConnection.Scope, documentType, documentsIds, notification));

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
        public IHttpActionResult ReindexDocuments([FromBody] string[] documentsIds, string documentType = null)
        {
            var notification = new IndexProgressPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Re-indexation process",
                Description = documentType != null ? "starting re-index for " + documentType : "starting full index rebuild"
            };
            _pushNotifier.Upsert(notification);

            _searchIndexController.RemoveIndex(_searchConnection.Scope, documentType, documentsIds);
            BackgroundJob.Enqueue(() => BackgroundIndex(_searchConnection.Scope, documentType, documentsIds, notification));

            return Ok(notification);
        }
    
        /// <summary>
        /// Get filter properties for store
        /// </summary>
        /// <remarks>
        /// Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </remarks>
        /// <param name="storeId">Store ID</param>
        [HttpGet]
        [Route("storefilterproperties/{storeId}")]
        [ResponseType(typeof(webModel.FilterProperty[]))]
        public IHttpActionResult GetFilterProperties(string storeId)
        {
            var store = _storeService.GetById(storeId);
            if (store == null)
            {
                return NotFound();
            }

            CheckCurrentUserHasPermissionForObjects(SearchPredefinedPermissions.ReadFilterProperties, store);

            var allProperties = GetAllCatalogProperties(store.Catalog);
            var selectedPropertyNames = GetSelectedFilterProperties(store);

            var filterProperties = allProperties
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => ConvertToFilterProperty(g.FirstOrDefault(), selectedPropertyNames))
                .OrderBy(p => p.Name)
                .ToArray();

            // Keep the selected properties order
            var result = selectedPropertyNames
                .SelectMany(n => filterProperties.Where(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)))
                .Union(filterProperties.Where(p => !selectedPropertyNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
                .ToArray();

            return Ok(result);
        }

        /// <summary>
        /// Set filter properties for store
        /// </summary>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        [HttpPut]
        [Route("storefilterproperties/{storeId}")]
        [ResponseType(typeof(void))]
        public IHttpActionResult SetFilterProperties(string storeId, webModel.FilterProperty[] filterProperties)
        {
            var store = _storeService.GetById(storeId);
            if (store == null)
            {
                return NotFound();
            }

            CheckCurrentUserHasPermissionForObjects(SearchPredefinedPermissions.UpdateFilterProperties, store);

            var allProperties = GetAllCatalogProperties(store.Catalog);

            var selectedPropertyNames = filterProperties
                .Where(p => p.IsSelected)
                .Select(p => p.Name)
                .Distinct()
                .ToArray();

            // Keep the selected properties order
            var selectedProperties = selectedPropertyNames
                .SelectMany(n => allProperties.Where(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            var attributes = selectedProperties
                .Select(ConvertToAttributeFilter)
                .GroupBy(a => a.Key)
                .Select(g => new AttributeFilter
                {
                    Key = g.Key,
                    Values = GetDistinctValues(g.SelectMany(a => a.Values)),
                    IsLocalized = g.Any(a => a.IsLocalized),
                    DisplayNames = GetDistinctNames(g.SelectMany(a => a.DisplayNames)),
                })
                .ToArray();

            SetFilteredBrowsingAttributes(store, attributes);
            _storeService.Update(new[] { store });

            return StatusCode(HttpStatusCode.NoContent);
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

        #region Helper methods
        protected void CheckCurrentUserHasPermissionForObjects(string permission, params object[] objects)
        {
            //Scope bound security check
            var scopes = objects.SelectMany(x => _permissionScopeService.GetObjectPermissionScopeStrings(x)).Distinct().ToArray();
            if (!_securityService.UserHasAnyPermission(User.Identity.Name, scopes, permission))
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }
        }

        private static string[] GetSelectedFilterProperties(Store store)
        {
            var result = new List<string>();

            var browsing = GetFilteredBrowsing(store);
            if (browsing != null && browsing.Attributes != null)
            {
                result.AddRange(browsing.Attributes.Select(a => a.Key));
            }

            return result.ToArray();
        }

        private static FilteredBrowsing GetFilteredBrowsing(Store store)
        {
            FilteredBrowsing result = null;

            var filterSettingValue = store.GetDynamicPropertyValue(_filteredBrowsingPropertyName, string.Empty);

            if (!string.IsNullOrEmpty(filterSettingValue))
            {
                var reader = new StringReader(filterSettingValue);
                var serializer = new XmlSerializer(typeof(FilteredBrowsing));
                result = serializer.Deserialize(reader) as FilteredBrowsing;
            }

            return result;
        }

        private static void SetFilteredBrowsingAttributes(Store store, AttributeFilter[] attributes)
        {
            var browsing = GetFilteredBrowsing(store) ?? new FilteredBrowsing();
            browsing.Attributes = attributes;
            var serializer = new XmlSerializer(typeof(FilteredBrowsing));
            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            serializer.Serialize(writer, browsing);
            var value = builder.ToString();

            var property = store.DynamicProperties.FirstOrDefault(p => p.Name == _filteredBrowsingPropertyName);

            if (property == null)
            {
                property = new DynamicObjectProperty { Name = _filteredBrowsingPropertyName };
                store.DynamicProperties.Add(property);
            }

            property.Values = new List<DynamicPropertyObjectValue>(new[] { new DynamicPropertyObjectValue { Value = value } });
        }

        private Property[] GetAllCatalogProperties(string catalogId)
        {
            var properties = _propertyService.GetAllCatalogProperties(catalogId);

            var result = properties
                .GroupBy(p => p.Id)
                .Select(g => g.FirstOrDefault())
                .OrderBy(p => p.Name)
                .ToArray();

            return result;
        }

        private static FilterDisplayName[] GetDistinctNames(IEnumerable<FilterDisplayName> names)
        {
            return names
                .Where(n => !string.IsNullOrEmpty(n.Language) && !string.IsNullOrEmpty(n.Name))
                .GroupBy(n => n.Language, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault())
                .OrderBy(n => n.Language)
                .ThenBy(n => n.Name)
                .ToArray();
        }

        private static AttributeFilterValue[] GetDistinctValues(IEnumerable<AttributeFilterValue> values)
        {
            return values
                .Where(v => !string.IsNullOrEmpty(v.Id) && !string.IsNullOrEmpty(v.Value))
                .GroupBy(v => v.Id, StringComparer.OrdinalIgnoreCase)
                .SelectMany(g => g
                    .GroupBy(g2 => g2.Language, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(g2 => g2
                        .GroupBy(g3 => g3.Value, StringComparer.OrdinalIgnoreCase)
                        .Select(g3 => g3.FirstOrDefault())))
                .OrderBy(v => v.Id)
                .ThenBy(v => v.Language)
                .ThenBy(v => v.Value)
                .ToArray();
        }

        private static List<string> GetDistinctValues(string value, string[] values)
        {
            var result = new List<string>();

            if (!string.IsNullOrEmpty(value))
            {
                result.Add(value);
            }

            if (values != null)
            {
                result.AddDistinct(StringComparer.OrdinalIgnoreCase, values);
            }

            return result;
        }

        private static webModel.FilterProperty ConvertToFilterProperty(Property property, string[] selectedPropertyNames)
        {
            return new webModel.FilterProperty
            {
                Name = property.Name,
                IsSelected = selectedPropertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase),
            };
        }

        private AttributeFilter ConvertToAttributeFilter(Property property)
        {
            var values = _propertyService.SearchDictionaryValues(property.Id, null);

            var result = new AttributeFilter
            {
                Key = property.Name,
                Values = values.Select(ConvertToAttributeFilterValue).ToArray(),
                IsLocalized = property.Multilanguage,
                DisplayNames = property.DisplayNames.Select(ConvertToFilterDisplayName).ToArray(),
            };

            return result;
        }

        private static FilterDisplayName ConvertToFilterDisplayName(PropertyDisplayName displayName)
        {
            var result = new FilterDisplayName
            {
                Language = displayName.LanguageCode,
                Name = displayName.Name,
            };

            return result;
        }

        private static AttributeFilterValue ConvertToAttributeFilterValue(PropertyDictionaryValue dictionaryValue)
        {
            var result = new AttributeFilterValue
            {
                Id = dictionaryValue.Alias,
                Value = dictionaryValue.Value,
                Language = dictionaryValue.LanguageCode,
            };

            return result;
        }
        #endregion
    }
}
