using System;
using System.Collections.Generic;
using System.Globalization;
using VirtoCommerce.SearchModule.Data.Model;
using Nest;
using VirtoCommerce.Domain.Search.Model;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    public class ElasticSearchProvider : ISearchProvider
    {
        public const string SearchAnalyzer = "search_analyzer";
        private const string _indexAnalyzer = "index_analyzer";

        private readonly ISearchConnection _connection;
        private readonly Dictionary<string, List<DocumentDictionary>> _pendingDocuments = new Dictionary<string, List<DocumentDictionary>>();
        private readonly Dictionary<string, TypeMapping> _mappings = new Dictionary<string, TypeMapping>();

        #region Private Properties
        ElasticClient _client;
        private ElasticClient Client
        {
            get
            {
                if (_client == null)
                {
                    var url = ElasticServerUrl;

                    if (url.StartsWith("https://"))
                    {
                        ThrowException("https connection is not supported", null);
                    }

                    if (url.StartsWith("http://")) // remove http prefix
                    {
                        url = url.Substring("http://".Length);
                    }

                    if (url.EndsWith("/"))
                    {
                        url = url.Remove(url.LastIndexOf("/", StringComparison.Ordinal));
                    }

                    var settings = new ConnectionSettings(new Uri("http://" + url))
                        .DisableDirectStreaming()
                        .OnRequestCompleted(details =>
                        {
                            Debug.WriteLine("### ES REQEUST ###");
                            if (details.RequestBodyInBytes != null) Debug.WriteLine(Encoding.UTF8.GetString(details.RequestBodyInBytes));
                            Debug.WriteLine("### ES RESPONSE ###");
                            if (details.ResponseBodyInBytes != null) Debug.WriteLine(Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                        })
                        .PrettyJson();

                    _client = new ElasticClient(settings);
                }

                return _client;
            }
        }
        #endregion

        #region Public Properties
        public string DefaultIndex { get; set; }

        private ISearchQueryBuilder _queryBuilder = new ElasticSearchQueryBuilder();

        public ISearchQueryBuilder QueryBuilder
        {
            get { return _queryBuilder; }
            set { _queryBuilder = value; }
        }

        private bool _autoCommit = true;

        /// <summary>
        /// Gets or sets a value indicating whether [auto commit].
        /// </summary>
        /// <value><c>true</c> if [auto commit]; otherwise, <c>false</c>.</value>
        public bool AutoCommit
        {
            get { return _autoCommit; }
            set { _autoCommit = value; }
        }

        private int _autoCommitCount = 100;

        /// <summary>
        /// Gets or sets the auto commit count.
        /// </summary>
        /// <value>The auto commit count.</value>
        public int AutoCommitCount
        {
            get { return _autoCommitCount; }
            set { _autoCommitCount = value; }
        }

        private string _elasticServerUrl = string.Empty;

        /// <summary>
        /// Gets or sets the solr server URL without Core secified.
        /// </summary>
        /// <example>localhost:9200</example>
        /// <value>The solr server URL.</value>
        public string ElasticServerUrl
        {
            get { return _elasticServerUrl; }
            set { _elasticServerUrl = value; }
        }
        #endregion

        public ElasticSearchProvider()
        {
            Init();
        }

        public ElasticSearchProvider(ISearchQueryBuilder queryBuilder, ISearchConnection connection)
        {
            _queryBuilder = queryBuilder;
            _connection = connection;
            Init();
        }

        private bool _isInitialized;
        private void Init()
        {
            if (!_isInitialized)
            {
                if (_connection != null && !string.IsNullOrEmpty(_connection.DataSource))
                {
                    _elasticServerUrl = _connection.DataSource;
                }
                else
                {
                    _elasticServerUrl = "localhost:9200";
                }

                _isInitialized = true;
            }
        }

        public ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            // Build query
            var command = QueryBuilder.BuildQuery<T>(scope, criteria) as SearchRequest;

            ISearchResponse<T> searchResponse;

            try
            {
                searchResponse = Client.Search<T>(command);
            }
            catch (Exception ex)
            {
                throw new ElasticSearchException("Search using Elastic Search NEST provider failed, check logs for more details.", ex);
            }

            // TODO: process aggregations
            var results = new SearchResults<T>(searchResponse);

            return results;
            // Parse documents returned
            var docList = new List<ResultDocument>();

            /*
            foreach (var indexDoc in searchResponse.Documents)
            {
                var document = new ResultDocument();
                foreach (var field in indexDoc.Keys)
                {
                    var fieldValue = indexDoc[field];
                    if (fieldValue is JArray)
                    {
                        var fieldArrayValue = fieldValue as JArray;
                        document.Add(new DocumentField(field, fieldArrayValue.ToArray()));
                    }
                    else
                    {
                        document.Add(new DocumentField(field, indexDoc[field]));
                    }
                }

                docList.Add(document);
            }

            var documents = new ResultDocumentSet
            {
                TotalCount = searchResponse.hits.total,
                Documents = docList.OfType<IDocument>().ToArray()
            };

            // Create search results object
            var results = new SearchResults(criteria, new[] { documents }){};

            if (searchResponse.aggregations == null)
                results.FacetGroups = CreateFacets(criteria, searchResponse.facets);
            else
                results.FacetGroups = CreateFacets(criteria, searchResponse.aggregations);

            return results;
            */
        }

        public void Index<T>(string scope, string documentType, T document)
        {
            // process mapping
            if(document is IDocument) // older case scenario
            {
                Index(scope, documentType, document as IDocument);
            }
            else
            {
                ThrowException(string.Format("Document of type {0} not supported", typeof(T).Name), new NotImplementedException());
            }
        }

        public virtual int Remove(string scope, string documentType, string key, string value)
        {
            return 0;
        }

        public virtual void RemoveAll(string scope, string documentType)
        {
            try
            {
                if (!string.IsNullOrEmpty(documentType))
                {
                    var result = Client.DeleteByQuery(new DeleteByQueryRequest(scope, documentType) { Query = new MatchAllQuery() });

                    if (!result.IsValid)
                        throw new IndexBuildException(result.ServerError.ToString());
                }
                else
                {
                    var result = Client.DeleteIndex(scope);

                    if (!result.IsValid)
                        throw new IndexBuildException(result.ServerError.ToString());
                }

                var core = GetCoreName(scope, documentType);
                _mappings.Remove(core);
            }
            catch (Exception ex)
            {
                /*
                if (ex.HttpStatusCode == 404 && (ex.Message.Contains("TypeMissingException") || ex.Message.Contains("IndexMissingException")))
                {

                }
                else
                {
                    ThrowException("Failed to remove indexes", ex);
                }
                */
            }
        }

        public virtual void Close(string scope, string documentType)
        {
        }

        public virtual void Commit(string scope)
        {
            var coreList = _pendingDocuments.Keys.ToList();

            foreach (var core in coreList)
            {
                var documents = _pendingDocuments[core];
                if (documents == null || documents.Count == 0)
                    continue;

                var coreArray = core.Split('.');
                var indexName = coreArray[0];
                var indexType = coreArray[1];

                var bulkDefinition = new BulkDescriptor();
                bulkDefinition.IndexMany(documents).Index(indexName).Type(indexType);
                var result = Client.Bulk(bulkDefinition);

                if (result == null)
                {
                    throw new IndexBuildException("no results");
                }

                foreach (var op in result.Items)
                {
                    if (!op.IsValid)
                    {
                        throw new IndexBuildException(op.Error.Reason);
                    }
                }

                // Remove documents
                _pendingDocuments[core].Clear();
            }
        }

        #region Helper Methods
        private void Index(string scope, string documentType, IDocument document)
        {
            var core = GetCoreName(scope, documentType);
            if (!_pendingDocuments.ContainsKey(core))
            {
                _pendingDocuments.Add(core, new List<DocumentDictionary>());
            }

            TypeMapping mapping = null;
            if (!_mappings.ContainsKey(core))
            {
                // Get mapping info
                if (Client.IndexExists(Indices.Parse(scope)).Exists)
                {
                    mapping = Client.GetMapping(new GetMappingRequest(scope, documentType)).Mapping;
                }
            }
            else
            {
                mapping = _mappings[core];
            }

            var submitMapping = false;

            var properties = new Properties<IProperties>();
            if(mapping != null) // initialize with existing properties
            {
                properties = new Properties<IProperties>(mapping.Properties);
            }

            var localDocument = new DocumentDictionary();

            // convert to simple dictionary document
            for (var index = 0; index < document.FieldCount; index++)
            {
                var field = document[index];

                var key = field.Name.ToLower();

                if (localDocument.ContainsKey(key))
                {
                    var newValues = new List<object>();

                    var currentValue = localDocument[key];
                    var currentValues = currentValue as object[];

                    if (currentValues != null)
                    {
                        newValues.AddRange(currentValues);
                    }
                    else
                    {
                        newValues.Add(currentValue);
                    }

                    newValues.AddRange(field.Values);
                    localDocument[key] = newValues.ToArray();
                }
                else
                {
                    // need to create new mapping or update it here
                    if (mapping == null || !mapping.Properties.ContainsKey(field.Name))
                    {
                        var type = field.Value != null ? field.Value.GetType() : typeof(object);

                        if (type == typeof(decimal))
                        {
                            type = typeof(double);
                        }

                        properties.Add(field.Name, PropertyHelper.InferProperty(type));
                        submitMapping = true;
                    }

                    // add fields to local document
                    if (field.Values.Length > 1)
                    {
                        localDocument.Add(key, field.Values);
                    }
                    else
                    {
                        localDocument.Add(key, field.Value);
                    }
                }
            }
            
            // submit mapping
            if (submitMapping)
            {
                if (!Client.IndexExists(Indices.Parse(scope)).Exists)
                {
                    Client.CreateIndex(scope);
                    var mappingRequest = new PutMappingRequest(scope, documentType) { Properties = properties };
                    Client.Map<DocumentDictionary>(m=>mappingRequest);
                    Client.Refresh(scope);
                }
                else // update existing mappings
                {
                    var mappingRequest = new PutMappingRequest(scope, documentType) { Properties = properties };
                    Client.Map<DocumentDictionary>(m => mappingRequest);
                    Client.Refresh(scope);
                }
            }

            _pendingDocuments[core].Add(localDocument);

            // Auto commit changes when limit is reached
            if (AutoCommit && _pendingDocuments[core].Count > AutoCommitCount)
            {
                Commit(scope);
            }

        }

        private static string GetCoreName(string scope, string documentType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", scope.ToLower(), documentType);
        }

        private void ThrowException(string message, Exception innerException)
        {
            throw new ElasticSearchException(string.Format(CultureInfo.InvariantCulture, "{0}. URL:{1}", message, ElasticServerUrl), innerException);
        }
        #endregion
    }
}
