using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RestSharp;
using VirtoCommerce.SearchModule.Client.Client;
using VirtoCommerce.SearchModule.Client.Model;

namespace VirtoCommerce.SearchModule.Client.Api
{
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IVirtoCommerceSearchApi : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// Get filter properties for store
        /// </summary>
        /// <remarks>
        /// Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>List&lt;FilterProperty&gt;</returns>
        List<FilterProperty> SearchModuleGetFilterProperties(string storeId);

        /// <summary>
        /// Get filter properties for store
        /// </summary>
        /// <remarks>
        /// Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>ApiResponse of List&lt;FilterProperty&gt;</returns>
        ApiResponse<List<FilterProperty>> SearchModuleGetFilterPropertiesWithHttpInfo(string storeId);
        /// <summary>
        /// Search for products and categories
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>CatalogSearchResult</returns>
        CatalogSearchResult SearchModuleSearch(SearchCriteria criteria);

        /// <summary>
        /// Search for products and categories
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>ApiResponse of CatalogSearchResult</returns>
        ApiResponse<CatalogSearchResult> SearchModuleSearchWithHttpInfo(SearchCriteria criteria);
        /// <summary>
        /// Set filter properties for store
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns></returns>
        void SearchModuleSetFilterProperties(string storeId, List<FilterProperty> filterProperties);

        /// <summary>
        /// Set filter properties for store
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns>ApiResponse of Object(void)</returns>
        ApiResponse<Object> SearchModuleSetFilterPropertiesWithHttpInfo(string storeId, List<FilterProperty> filterProperties);
        #endregion Synchronous Operations
        #region Asynchronous Operations
        /// <summary>
        /// Get filter properties for store
        /// </summary>
        /// <remarks>
        /// Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>Task of List&lt;FilterProperty&gt;</returns>
        System.Threading.Tasks.Task<List<FilterProperty>> SearchModuleGetFilterPropertiesAsync(string storeId);

        /// <summary>
        /// Get filter properties for store
        /// </summary>
        /// <remarks>
        /// Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>Task of ApiResponse (List&lt;FilterProperty&gt;)</returns>
        System.Threading.Tasks.Task<ApiResponse<List<FilterProperty>>> SearchModuleGetFilterPropertiesAsyncWithHttpInfo(string storeId);
        /// <summary>
        /// Search for products and categories
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>Task of CatalogSearchResult</returns>
        System.Threading.Tasks.Task<CatalogSearchResult> SearchModuleSearchAsync(SearchCriteria criteria);

        /// <summary>
        /// Search for products and categories
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>Task of ApiResponse (CatalogSearchResult)</returns>
        System.Threading.Tasks.Task<ApiResponse<CatalogSearchResult>> SearchModuleSearchAsyncWithHttpInfo(SearchCriteria criteria);
        /// <summary>
        /// Set filter properties for store
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns>Task of void</returns>
        System.Threading.Tasks.Task SearchModuleSetFilterPropertiesAsync(string storeId, List<FilterProperty> filterProperties);

        /// <summary>
        /// Set filter properties for store
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns>Task of ApiResponse</returns>
        System.Threading.Tasks.Task<ApiResponse<object>> SearchModuleSetFilterPropertiesAsyncWithHttpInfo(string storeId, List<FilterProperty> filterProperties);
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class VirtoCommerceSearchApi : IVirtoCommerceSearchApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtoCommerceSearchApi"/> class
        /// using Configuration object
        /// </summary>
        /// <param name="apiClient">An instance of ApiClient.</param>
        /// <returns></returns>
        public VirtoCommerceSearchApi(ApiClient apiClient)
        {
            ApiClient = apiClient;
            Configuration = apiClient.Configuration;
        }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public string GetBasePath()
        {
            return ApiClient.RestClient.BaseUrl.ToString();
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public Configuration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the API client object
        /// </summary>
        /// <value>An instance of the ApiClient</value>
        public ApiClient ApiClient { get; set; }

        /// <summary>
        /// Get filter properties for store Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>List&lt;FilterProperty&gt;</returns>
        public List<FilterProperty> SearchModuleGetFilterProperties(string storeId)
        {
             ApiResponse<List<FilterProperty>> localVarResponse = SearchModuleGetFilterPropertiesWithHttpInfo(storeId);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Get filter properties for store Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>ApiResponse of List&lt;FilterProperty&gt;</returns>
        public ApiResponse<List<FilterProperty>> SearchModuleGetFilterPropertiesWithHttpInfo(string storeId)
        {
            // verify the required parameter 'storeId' is set
            if (storeId == null)
                throw new ApiException(400, "Missing required parameter 'storeId' when calling VirtoCommerceSearchApi->SearchModuleGetFilterProperties");

            var localVarPath = "/api/search/storefilterproperties/{storeId}";
            var localVarPathParams = new Dictionary<string, string>();
            var localVarQueryParams = new Dictionary<string, string>();
            var localVarHeaderParams = new Dictionary<string, string>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<string, string>();
            var localVarFileParams = new Dictionary<string, FileParameter>();
            object localVarPostBody = null;

            // to determine the Content-Type header
            string[] localVarHttpContentTypes = new string[] {
            };
            string localVarHttpContentType = ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            string[] localVarHttpHeaderAccepts = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml"
            };
            string localVarHttpHeaderAccept = ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (storeId != null) localVarPathParams.Add("storeId", ApiClient.ParameterToString(storeId)); // path parameter


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)ApiClient.CallApi(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (localVarStatusCode >= 400 && (localVarStatusCode != 404 || Configuration.ThrowExceptionWhenStatusCodeIs404))
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleGetFilterProperties: " + localVarResponse.Content, localVarResponse.Content);
            else if (localVarStatusCode == 0)
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleGetFilterProperties: " + localVarResponse.ErrorMessage, localVarResponse.ErrorMessage);

            return new ApiResponse<List<FilterProperty>>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (List<FilterProperty>)ApiClient.Deserialize(localVarResponse, typeof(List<FilterProperty>)));
            
        }

        /// <summary>
        /// Get filter properties for store Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>Task of List&lt;FilterProperty&gt;</returns>
        public async System.Threading.Tasks.Task<List<FilterProperty>> SearchModuleGetFilterPropertiesAsync(string storeId)
        {
             ApiResponse<List<FilterProperty>> localVarResponse = await SearchModuleGetFilterPropertiesAsyncWithHttpInfo(storeId);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Get filter properties for store Returns all store catalog properties: selected properties are ordered manually, unselected properties are ordered by name.
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <returns>Task of ApiResponse (List&lt;FilterProperty&gt;)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<List<FilterProperty>>> SearchModuleGetFilterPropertiesAsyncWithHttpInfo(string storeId)
        {
            // verify the required parameter 'storeId' is set
            if (storeId == null)
                throw new ApiException(400, "Missing required parameter 'storeId' when calling VirtoCommerceSearchApi->SearchModuleGetFilterProperties");

            var localVarPath = "/api/search/storefilterproperties/{storeId}";
            var localVarPathParams = new Dictionary<string, string>();
            var localVarQueryParams = new Dictionary<string, string>();
            var localVarHeaderParams = new Dictionary<string, string>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<string, string>();
            var localVarFileParams = new Dictionary<string, FileParameter>();
            object localVarPostBody = null;

            // to determine the Content-Type header
            string[] localVarHttpContentTypes = new string[] {
            };
            string localVarHttpContentType = ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            string[] localVarHttpHeaderAccepts = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml"
            };
            string localVarHttpHeaderAccept = ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (storeId != null) localVarPathParams.Add("storeId", ApiClient.ParameterToString(storeId)); // path parameter


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)await ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (localVarStatusCode >= 400 && (localVarStatusCode != 404 || Configuration.ThrowExceptionWhenStatusCodeIs404))
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleGetFilterProperties: " + localVarResponse.Content, localVarResponse.Content);
            else if (localVarStatusCode == 0)
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleGetFilterProperties: " + localVarResponse.ErrorMessage, localVarResponse.ErrorMessage);

            return new ApiResponse<List<FilterProperty>>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (List<FilterProperty>)ApiClient.Deserialize(localVarResponse, typeof(List<FilterProperty>)));
            
        }
        /// <summary>
        /// Search for products and categories 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>CatalogSearchResult</returns>
        public CatalogSearchResult SearchModuleSearch(SearchCriteria criteria)
        {
             ApiResponse<CatalogSearchResult> localVarResponse = SearchModuleSearchWithHttpInfo(criteria);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Search for products and categories 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>ApiResponse of CatalogSearchResult</returns>
        public ApiResponse<CatalogSearchResult> SearchModuleSearchWithHttpInfo(SearchCriteria criteria)
        {
            // verify the required parameter 'criteria' is set
            if (criteria == null)
                throw new ApiException(400, "Missing required parameter 'criteria' when calling VirtoCommerceSearchApi->SearchModuleSearch");

            var localVarPath = "/api/search";
            var localVarPathParams = new Dictionary<string, string>();
            var localVarQueryParams = new Dictionary<string, string>();
            var localVarHeaderParams = new Dictionary<string, string>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<string, string>();
            var localVarFileParams = new Dictionary<string, FileParameter>();
            object localVarPostBody = null;

            // to determine the Content-Type header
            string[] localVarHttpContentTypes = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml", 
                "application/x-www-form-urlencoded"
            };
            string localVarHttpContentType = ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            string[] localVarHttpHeaderAccepts = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml"
            };
            string localVarHttpHeaderAccept = ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (criteria.GetType() != typeof(byte[]))
            {
                localVarPostBody = ApiClient.Serialize(criteria); // http body (model) parameter
            }
            else
            {
                localVarPostBody = criteria; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)ApiClient.CallApi(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (localVarStatusCode >= 400 && (localVarStatusCode != 404 || Configuration.ThrowExceptionWhenStatusCodeIs404))
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSearch: " + localVarResponse.Content, localVarResponse.Content);
            else if (localVarStatusCode == 0)
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSearch: " + localVarResponse.ErrorMessage, localVarResponse.ErrorMessage);

            return new ApiResponse<CatalogSearchResult>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (CatalogSearchResult)ApiClient.Deserialize(localVarResponse, typeof(CatalogSearchResult)));
            
        }

        /// <summary>
        /// Search for products and categories 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>Task of CatalogSearchResult</returns>
        public async System.Threading.Tasks.Task<CatalogSearchResult> SearchModuleSearchAsync(SearchCriteria criteria)
        {
             ApiResponse<CatalogSearchResult> localVarResponse = await SearchModuleSearchAsyncWithHttpInfo(criteria);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Search for products and categories 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="criteria">Search parameters</param>
        /// <returns>Task of ApiResponse (CatalogSearchResult)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<CatalogSearchResult>> SearchModuleSearchAsyncWithHttpInfo(SearchCriteria criteria)
        {
            // verify the required parameter 'criteria' is set
            if (criteria == null)
                throw new ApiException(400, "Missing required parameter 'criteria' when calling VirtoCommerceSearchApi->SearchModuleSearch");

            var localVarPath = "/api/search";
            var localVarPathParams = new Dictionary<string, string>();
            var localVarQueryParams = new Dictionary<string, string>();
            var localVarHeaderParams = new Dictionary<string, string>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<string, string>();
            var localVarFileParams = new Dictionary<string, FileParameter>();
            object localVarPostBody = null;

            // to determine the Content-Type header
            string[] localVarHttpContentTypes = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml", 
                "application/x-www-form-urlencoded"
            };
            string localVarHttpContentType = ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            string[] localVarHttpHeaderAccepts = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml"
            };
            string localVarHttpHeaderAccept = ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (criteria.GetType() != typeof(byte[]))
            {
                localVarPostBody = ApiClient.Serialize(criteria); // http body (model) parameter
            }
            else
            {
                localVarPostBody = criteria; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)await ApiClient.CallApiAsync(localVarPath,
                Method.POST, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (localVarStatusCode >= 400 && (localVarStatusCode != 404 || Configuration.ThrowExceptionWhenStatusCodeIs404))
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSearch: " + localVarResponse.Content, localVarResponse.Content);
            else if (localVarStatusCode == 0)
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSearch: " + localVarResponse.ErrorMessage, localVarResponse.ErrorMessage);

            return new ApiResponse<CatalogSearchResult>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (CatalogSearchResult)ApiClient.Deserialize(localVarResponse, typeof(CatalogSearchResult)));
            
        }
        /// <summary>
        /// Set filter properties for store 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns></returns>
        public void SearchModuleSetFilterProperties(string storeId, List<FilterProperty> filterProperties)
        {
             SearchModuleSetFilterPropertiesWithHttpInfo(storeId, filterProperties);
        }

        /// <summary>
        /// Set filter properties for store 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns>ApiResponse of Object(void)</returns>
        public ApiResponse<object> SearchModuleSetFilterPropertiesWithHttpInfo(string storeId, List<FilterProperty> filterProperties)
        {
            // verify the required parameter 'storeId' is set
            if (storeId == null)
                throw new ApiException(400, "Missing required parameter 'storeId' when calling VirtoCommerceSearchApi->SearchModuleSetFilterProperties");
            // verify the required parameter 'filterProperties' is set
            if (filterProperties == null)
                throw new ApiException(400, "Missing required parameter 'filterProperties' when calling VirtoCommerceSearchApi->SearchModuleSetFilterProperties");

            var localVarPath = "/api/search/storefilterproperties/{storeId}";
            var localVarPathParams = new Dictionary<string, string>();
            var localVarQueryParams = new Dictionary<string, string>();
            var localVarHeaderParams = new Dictionary<string, string>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<string, string>();
            var localVarFileParams = new Dictionary<string, FileParameter>();
            object localVarPostBody = null;

            // to determine the Content-Type header
            string[] localVarHttpContentTypes = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml", 
                "application/x-www-form-urlencoded"
            };
            string localVarHttpContentType = ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            string[] localVarHttpHeaderAccepts = new string[] {
            };
            string localVarHttpHeaderAccept = ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (storeId != null) localVarPathParams.Add("storeId", ApiClient.ParameterToString(storeId)); // path parameter
            if (filterProperties.GetType() != typeof(byte[]))
            {
                localVarPostBody = ApiClient.Serialize(filterProperties); // http body (model) parameter
            }
            else
            {
                localVarPostBody = filterProperties; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)ApiClient.CallApi(localVarPath,
                Method.PUT, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (localVarStatusCode >= 400 && (localVarStatusCode != 404 || Configuration.ThrowExceptionWhenStatusCodeIs404))
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSetFilterProperties: " + localVarResponse.Content, localVarResponse.Content);
            else if (localVarStatusCode == 0)
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSetFilterProperties: " + localVarResponse.ErrorMessage, localVarResponse.ErrorMessage);

            
            return new ApiResponse<object>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                null);
        }

        /// <summary>
        /// Set filter properties for store 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns>Task of void</returns>
        public async System.Threading.Tasks.Task SearchModuleSetFilterPropertiesAsync(string storeId, List<FilterProperty> filterProperties)
        {
             await SearchModuleSetFilterPropertiesAsyncWithHttpInfo(storeId, filterProperties);

        }

        /// <summary>
        /// Set filter properties for store 
        /// </summary>
        /// <exception cref="VirtoCommerce.SearchModule.Client.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="storeId">Store ID</param>
        /// <param name="filterProperties"></param>
        /// <returns>Task of ApiResponse</returns>
        public async System.Threading.Tasks.Task<ApiResponse<object>> SearchModuleSetFilterPropertiesAsyncWithHttpInfo(string storeId, List<FilterProperty> filterProperties)
        {
            // verify the required parameter 'storeId' is set
            if (storeId == null)
                throw new ApiException(400, "Missing required parameter 'storeId' when calling VirtoCommerceSearchApi->SearchModuleSetFilterProperties");
            // verify the required parameter 'filterProperties' is set
            if (filterProperties == null)
                throw new ApiException(400, "Missing required parameter 'filterProperties' when calling VirtoCommerceSearchApi->SearchModuleSetFilterProperties");

            var localVarPath = "/api/search/storefilterproperties/{storeId}";
            var localVarPathParams = new Dictionary<string, string>();
            var localVarQueryParams = new Dictionary<string, string>();
            var localVarHeaderParams = new Dictionary<string, string>(Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<string, string>();
            var localVarFileParams = new Dictionary<string, FileParameter>();
            object localVarPostBody = null;

            // to determine the Content-Type header
            string[] localVarHttpContentTypes = new string[] {
                "application/json", 
                "text/json", 
                "application/xml", 
                "text/xml", 
                "application/x-www-form-urlencoded"
            };
            string localVarHttpContentType = ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            string[] localVarHttpHeaderAccepts = new string[] {
            };
            string localVarHttpHeaderAccept = ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            // set "format" to json by default
            // e.g. /pet/{petId}.{format} becomes /pet/{petId}.json
            localVarPathParams.Add("format", "json");
            if (storeId != null) localVarPathParams.Add("storeId", ApiClient.ParameterToString(storeId)); // path parameter
            if (filterProperties.GetType() != typeof(byte[]))
            {
                localVarPostBody = ApiClient.Serialize(filterProperties); // http body (model) parameter
            }
            else
            {
                localVarPostBody = filterProperties; // byte array
            }


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse)await ApiClient.CallApiAsync(localVarPath,
                Method.PUT, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int)localVarResponse.StatusCode;

            if (localVarStatusCode >= 400 && (localVarStatusCode != 404 || Configuration.ThrowExceptionWhenStatusCodeIs404))
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSetFilterProperties: " + localVarResponse.Content, localVarResponse.Content);
            else if (localVarStatusCode == 0)
                throw new ApiException(localVarStatusCode, "Error calling SearchModuleSetFilterProperties: " + localVarResponse.ErrorMessage, localVarResponse.ErrorMessage);

            
            return new ApiResponse<object>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                null);
        }
    }
}
