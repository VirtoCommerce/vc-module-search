using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nest;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    /// <summary>
    /// NEST based elastic search provider.
    /// </summary>
    public class ElasticSearchProvider : ISearchProvider
    {
        public const string SearchAnalyzerName = "search_analyzer";
        public const string IndexAnalyzerName = "index_analyzer";

        private readonly ISearchConnection _connection;
        private readonly Dictionary<string, List<DocumentDictionary>> _pendingDocuments = new Dictionary<string, List<DocumentDictionary>>();
        private readonly Dictionary<string, TypeMapping> _mappings = new Dictionary<string, TypeMapping>();

        #region Protected Properties

        private ElasticClient _client;
        protected ElasticClient Client
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

                    var settings = new ConnectionSettings(new Uri("http://" + url));

                    if (EnableTrace)
                    {
                        settings.DisableDirectStreaming().OnRequestCompleted(details =>
                        {
                            Trace.WriteLine("### ES REQEUST ###");
                            if (details.RequestBodyInBytes != null) Trace.WriteLine(Encoding.UTF8.GetString(details.RequestBodyInBytes));
                            Trace.WriteLine("### ES RESPONSE ###");
                            if (details.ResponseBodyInBytes != null) Trace.WriteLine(Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                        })
                        .PrettyJson();
                    }

                    _client = new ElasticClient(settings);
                }

                return _client;
            }
        }

        #endregion

        #region Public Properties

        public string DefaultIndex { get; set; }

        private ISearchQueryBuilder[] _queryBuilders = { new ElasticSearchQueryBuilder() };

        public ISearchQueryBuilder[] QueryBuilders
        {
            get { return _queryBuilders; }
            set { _queryBuilders = value; }
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

        /// <summary>
        /// Tells provider to run in debug mode outputting all requests to console.
        /// </summary>
        public bool EnableTrace { get; set; }

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

        public ElasticSearchProvider(ISearchQueryBuilder[] queryBuilders, ISearchConnection connection)
        {
            _queryBuilders = queryBuilders;
            _connection = connection;
            Init();

#if DEBUG
            EnableTrace = true;
#endif
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

        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            // Build query
            var command = GetQueryBuilder(criteria).BuildQuery<T>(scope, criteria) as SearchRequest;

            ISearchResponse<T> searchResponse;

            try
            {
                searchResponse = Client.Search<T>(command);
            }
            catch (Exception ex)
            {
                throw new ElasticSearchException("Search using Elastic Search NEST provider failed, check logs for more details.", ex);
            }

            if (!searchResponse.IsValid)
                ThrowException(searchResponse.DebugInformation, null);

            var results = new SearchResults<T>(criteria, searchResponse);
            return results;
        }

        public virtual void Index<T>(string scope, string documentType, T document)
        {
            var core = GetCoreName(scope, documentType);

            // process mapping
            if (document is IDocument) // older case scenario
            {
                Index(scope, documentType, document as IDocument);
            }
            else
            {
                ThrowException(string.Format(CultureInfo.InvariantCulture, "Document type not supported: {0}", typeof(T).Name), new NotImplementedException());
            }

            // Auto commit changes when limit is reached
            if (AutoCommit && _pendingDocuments[core].Count >= AutoCommitCount)
            {
                Commit(scope);
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
                    // check if index actually exists before performing delete, since it will cause new index to be automatically created
                    if (Client.IndexExists(Indices.Parse(scope)).Exists)
                    {
                        var result = Client.DeleteByQuery(new DeleteByQueryRequest(scope, documentType) { Query = new MatchAllQuery() });

                        if (!result.IsValid && result.ApiCall.HttpStatusCode != 404)
                            throw new IndexBuildException(result.DebugInformation);
                    }
                }
                else
                {
                    var result = Client.DeleteIndex(scope);

                    if (!result.IsValid && result.ApiCall.HttpStatusCode != 404)
                        throw new IndexBuildException(result.DebugInformation);
                }

                var core = GetCoreName(scope, documentType);
                _mappings.Remove(core);
            }
            catch (Exception ex)
            {
                ThrowException("Failed to remove indexes", ex);
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

        protected virtual ISearchQueryBuilder GetQueryBuilder(ISearchCriteria criteria)
        {
            if (QueryBuilders == null)
                throw new NullReferenceException("No QueryBuilders defined");

            var queryBuilder = QueryBuilders.SingleOrDefault(x => x.DocumentType.Equals(criteria.DocumentType));

            if (queryBuilder == null) // get default builder
            {
                queryBuilder = QueryBuilders.First(x => x.DocumentType.Equals(string.Empty));
            }

            return queryBuilder;
        }

        protected virtual void Index(string scope, string documentType, IDocument document)
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
            if (mapping != null) // initialize with existing properties
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
                        SetupProperty(properties[field.Name], field);

                        submitMapping = true;
                    }
                    /* // currently can't change type of mapping for existing data
                    else // check mapping, and update it if necessary
                    {
                        var type = field.Value != null ? field.Value.GetType() : typeof(object);

                        var existingProperty = mapping.Properties[field.Name];
                        var proposedType = PropertyHelper.InferProperty(type);

                        // check if types match
                        if (proposedType.Type.Name != existingProperty.Type.Name)
                        {
                            // we only change to string type
                            if (existingProperty.Type.Name != "string" && existingProperty.Type.Name != "object")
                            {
                                properties[field.Name] = new StringProperty();
                                submitMapping = true;
                            }
                        }
                    }
                    */

                    // add fields to local document
                    localDocument.Add(key, field.Values.Length > 1 ? field.Values : field.Value);
                }
            }

            // submit mapping
            if (submitMapping)
            {
                if (!Client.IndexExists(Indices.Parse(scope)).Exists)
                {
                    CreateIndex(scope, documentType);

                    var mappingRequest = new PutMappingRequest(scope, documentType) { Properties = properties };
                    var response = Client.Map<DocumentDictionary>(m => mappingRequest);

                    if (!response.IsValid)
                    {
                        ThrowException("Failed to submit mapping. " + response.DebugInformation, response.OriginalException);
                    }

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
        }

        protected virtual void CreateIndex(string scope, string documentType)
        {
            // Use ngrams analyzer for search in the middle of the word
            // http://www.elasticsearch.org/guide/en/elasticsearch/guide/current/ngrams-compound-words.html
            Client.CreateIndex(scope, x => x.Settings(v => v
                  .Analysis(a => a.TokenFilters(f => f.NGram("trigrams_filter", ng => ng.MinGram(3).MaxGram(20)))
                  .Analyzers(an => an
                      .Custom(IndexAnalyzerName, custom => custom
                          .Tokenizer("standard")
                          .Filters("lowercase", "trigrams_filter"))
                      .Custom(SearchAnalyzerName, custom => custom
                          .Tokenizer("standard")
                          .Filters("lowercase"))))));
        }

        private static string GetCoreName(string scope, string documentType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", scope.ToLower(), documentType);
        }

        private void ThrowException(string message, Exception innerException)
        {
            throw new ElasticSearchException(string.Format(CultureInfo.InvariantCulture, "{0}. URL:{1}", message, ElasticServerUrl), innerException);
        }

        protected virtual void SetupProperty(IProperty property, IDocumentField field)
        {
            //property.DocValues = !field.ContainsAttribute(IndexStore.No);

            SetupStringProperty(property as StringProperty, field);
        }

        protected virtual void SetupStringProperty(StringProperty stringProperty, IDocumentField field)
        {
            if (stringProperty != null)
            {
                stringProperty.Store = field.ContainsAttribute(IndexStore.Yes);
                stringProperty.Index = field.ContainsAttribute(IndexType.NotAnalyzed) ? FieldIndexOption.NotAnalyzed : field.ContainsAttribute(IndexType.Analyzed) ? FieldIndexOption.Analyzed : FieldIndexOption.No;

                if (field.Name.StartsWith("__content", StringComparison.OrdinalIgnoreCase))
                {
                    stringProperty.Analyzer = IndexAnalyzerName;
                }

                if (Regex.Match(field.Name, "__content_en.*").Success)
                {
                    stringProperty.Analyzer = "english";
                }
                else if (Regex.Match(field.Name, "__content_de.*").Success)
                {
                    stringProperty.Analyzer = "german";
                }
                else if (Regex.Match(field.Name, "__content_ru.*").Success)
                {
                    stringProperty.Analyzer = "russian";
                }
                else if (Regex.Match(field.Name, "__content_fr.*").Success)
                {
                    stringProperty.Analyzer = "french";
                }
                else if (Regex.Match(field.Name, "__content_sv.*").Success)
                {
                    stringProperty.Analyzer = "swedish";
                }
                else if (Regex.Match(field.Name, "__content_nb.*").Success)
                {
                    stringProperty.Analyzer = "norwegian";
                }
            }
        }

        #endregion
    }
}
