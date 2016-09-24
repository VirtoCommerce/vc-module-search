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

namespace VirtoCommerce.SearchModule.Web.Controllers.Api
{
    [RoutePrefix("api/search")]
    public class SearchModuleController : ApiController
    {
        private const string _filteredBrowsingPropertyName = "FilteredBrowsing";

        private readonly ISearchProvider _searchProvider;
        private readonly ISearchConnection _searchConnection;
        private readonly SearchIndexJobsScheduler _scheduler;
        private readonly IStoreService _storeService;
        private readonly ISecurityService _securityService;
        private readonly IPermissionScopeService _permissionScopeService;
        private readonly IPropertyService _propertyService;
        private readonly IBrowseFilterService _browseFilterService;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ICacheManager<object> _cacheManager;

        public SearchModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection, SearchIndexJobsScheduler scheduler,
            IStoreService storeService, ISecurityService securityService, IPermissionScopeService permissionScopeService,
            IPropertyService propertyService, IBrowseFilterService browseFilterService, 
            IBlobUrlResolver blobUrlResolver, ICatalogSearchService catalogSearchService, ICacheManager<object> cacheManager)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _scheduler = scheduler;
            _storeService = storeService;
            _securityService = securityService;
            _permissionScopeService = permissionScopeService;
            _propertyService = propertyService;
            _browseFilterService = browseFilterService;
            _blobUrlResolver = blobUrlResolver;
            _catalogSearchService = catalogSearchService;
            _cacheManager = cacheManager;
        }

        /// <summary>
        /// Rebuild the index for specified document type. If document type is not specified, then index will be recreated.
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("index/rebuild/{documentType}")]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult Rebuild(string documentType = "")
        {
            var jobId = _scheduler.ScheduleRebuildIndex(documentType);
            var result = new { Id = jobId };
            return Ok(result);
        }

        /// <summary>
        /// Rebuild the index for specified document type and document id.
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="documentId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("index/rebuild/{documentType}/{documentId}")]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult Rebuild(string documentType, string documentId)
        {
            var jobId = _scheduler.ScheduleRebuildIndex(documentType);
            var result = new { Id = jobId };
            return Ok(result);
        }

        [HttpGet]
        [Route("catalogitem/rebuild")]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IHttpActionResult RebuildFullIndex()
        {
            var jobId = _scheduler.ScheduleRebuildIndex();
            var result = new { Id = jobId };
            return Ok(result);
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
