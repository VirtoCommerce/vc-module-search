using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nest;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criteria;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    /// <summary>
    /// NEST based elastic search provider.
    /// </summary>
    public class ElasticSearchProvider : ISearchProvider
    {
        public const string ContentAnalyzerName = "content_analyzer";
        public const string KeywordAnalyzerName = "keyword_analyzer";
        public const string NGramFilterName = "ngram_filter";

        private readonly ISearchConnection _connection;
        private readonly Dictionary<string, List<IDocument>> _pendingDocuments = new Dictionary<string, List<IDocument>>();
        private readonly Dictionary<string, Properties<IProperties>> _mappings = new Dictionary<string, Properties<IProperties>>();

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

        public ISearchQueryBuilder[] QueryBuilders { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [auto commit].
        /// </summary>
        /// <value><c>true</c> if [auto commit]; otherwise, <c>false</c>.</value>
        public bool AutoCommit { get; set; } = true;

        /// <summary>
        /// Gets or sets the auto commit count.
        /// </summary>
        /// <value>The auto commit count.</value>
        public int AutoCommitCount { get; set; } = 100;

        /// <summary>
        /// Tells provider to run in debug mode outputting all requests to console.
        /// </summary>
        public bool EnableTrace { get; set; }

        /// <summary>
        /// Gets or sets the solr server URL without Core secified.
        /// </summary>
        /// <example>localhost:9200</example>
        /// <value>The solr server URL.</value>
        public string ElasticServerUrl { get; set; } = string.Empty;

        #endregion

        public ElasticSearchProvider()
        {
            Init();
        }

        public ElasticSearchProvider(ISearchQueryBuilder[] queryBuilders, ISearchConnection connection)
        {
            QueryBuilders = queryBuilders;
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
                ElasticServerUrl = !string.IsNullOrEmpty(_connection?.DataSource) ? _connection.DataSource : "localhost:9200";
                _isInitialized = true;
            }
        }

        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            var indexName = GetIndexName(scope, criteria.DocumentType);

            // Build query
            var command = GetQueryBuilder(criteria).BuildQuery<T>(indexName, criteria) as SearchRequest;

            ISearchResponse<T> searchResponse;

            try
            {
                searchResponse = Client.Search<T>(command);
            }
            catch (Exception ex)
            {
                throw new SearchException("Search using Elastic Search NEST provider failed, check logs for more details.", ex);
            }

            if (!searchResponse.IsValid)
                ThrowException(searchResponse.DebugInformation, null);

            var results = new ElasticSearchResults<T>(criteria, searchResponse);
            return results;
        }

        public virtual void Index<T>(string scope, string documentType, T document)
        {
            var doc = document as IDocument;
            if (doc == null)
            {
                var message = $"Document type '{typeof(T).Name}' is not supported";
                ThrowException(message, new ArgumentException(message, nameof(document)));
            }
            else
            {
                var key = CreateCacheKey(scope, documentType);

                if (!_pendingDocuments.ContainsKey(key))
                {
                    _pendingDocuments.Add(key, new List<IDocument>());
                }

                _pendingDocuments[key].Add(doc);

                // Auto commit changes when limit is reached
                if (AutoCommit && _pendingDocuments[key].Count >= AutoCommitCount)
                {
                    Commit(scope);
                }
            }
        }

        public virtual int Remove(string scope, string documentType, string key, string value)
        {
            return 0;
        }

        public virtual bool RemoveAll(string scope, string documentType)
        {
            var result = false;

            if (!string.IsNullOrEmpty(documentType))
            {
                try
                {
                    var indexName = GetIndexName(scope, documentType);

                    var response = Client.DeleteIndex(indexName);
                    if (!response.IsValid && response.ApiCall.HttpStatusCode != 404)
                    {
                        throw new IndexBuildException(response.DebugInformation);
                    }

                    RemoveMappingFromCache(indexName, documentType);
                }
                catch (Exception ex)
                {
                    ThrowException("Failed to remove index", ex);
                }

                result = true;
            }

            return result;
        }

        public virtual void Close(string scope, string documentType)
        {
        }

        public virtual void Commit(string scope)
        {
            foreach (var key in _pendingDocuments.Keys)
            {
                var documents = _pendingDocuments[key];
                if (documents != null && documents.Count > 0)
                {
                    var keyParts = ParseCacheKey(key);
                    if (keyParts[0] == scope)
                    {
                        var documentType = keyParts[1];
                        var indexName = GetIndexName(scope, documentType);

                        var properties = GetMappedProperties(indexName, documentType);
                        var oldPropertiesCount = properties.Count();

                        var simpleDocuments = documents.Select(document => ConvertToSimpleDocument(document, properties, documentType)).ToList();

                        var updateMapping = properties.Count() != oldPropertiesCount;
                        var indexExits = IndexExists(indexName);

                        if (!indexExits)
                        {
                            CreateIndex(indexName, documentType);
                        }

                        if (!indexExits || updateMapping)
                        {
                            UpdateMapping(indexName, documentType, properties);
                        }

                        var bulkDefinition = new BulkDescriptor();
                        bulkDefinition.IndexMany(simpleDocuments).Index(indexName).Type(documentType);
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

                        // Remove pending documents
                        documents.Clear();
                    }
                }
            }
        }

        #region Helper Methods

        protected virtual ISearchQueryBuilder GetQueryBuilder(ISearchCriteria criteria)
        {
            if (QueryBuilders == null)
                throw new InvalidOperationException("No query builders defined");

            var queryBuilder = QueryBuilders.SingleOrDefault(b => b.DocumentType.Equals(criteria.DocumentType)) ??
                               QueryBuilders.First(b => b.DocumentType.Equals(string.Empty));

            return queryBuilder;
        }

        // Convert to simple dictionary document
        protected virtual DocumentDictionary ConvertToSimpleDocument(IDocument document, Properties<IProperties> properties, string documentType)
        {
            var result = new DocumentDictionary();

            for (var index = 0; index < document.FieldCount; index++)
            {
                var field = document[index];
                var key = field.Name.ToLower();

                if (result.ContainsKey(key))
                {
                    var newValues = new List<object>();

                    var currentValue = result[key];
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
                    result[key] = newValues.ToArray();
                }
                else
                {
                    var dictionary = properties as IDictionary<PropertyName, IProperty>;
                    if (dictionary != null && !dictionary.ContainsKey(field.Name))
                    {
                        // Create new property mapping

                        var type = field.Value?.GetType() ?? typeof(object);

                        if (type == typeof(decimal))
                        {
                            type = typeof(double);
                        }

                        properties.Add(field.Name, PropertyHelper.InferProperty(type));
                        SetupProperty(properties[field.Name], field, documentType);
                    }

                    result.Add(key, field.Values.Length > 1 ? field.Values : field.Value);
                }
            }

            return result;
        }

        protected virtual string GetIndexName(string scope, string documentType)
        {
            // Use different index for each document type
            return string.Join("-", scope, documentType);
        }

        protected virtual bool IndexExists(string indexName)
        {
            return Client.IndexExists(indexName).Exists;
        }

        #region Create and configure index

        protected virtual void CreateIndex(string indexName, string documentType)
        {
            Client.CreateIndex(indexName, i => i
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tokenFilters => SetupTokenFilters(tokenFilters, documentType))
                        .Analyzers(analyzers => SetupAnalyzers(analyzers, documentType)))));
        }

        protected virtual AnalyzersDescriptor SetupAnalyzers(AnalyzersDescriptor analyzers, string documentType)
        {
            return analyzers
                .Custom(ContentAnalyzerName, customAnalyzer => SetupContentAnalyzer(customAnalyzer, documentType))
                .Custom(KeywordAnalyzerName, customAnalyzer => SetupKeywordAnalyzer(customAnalyzer, documentType));
        }

        protected virtual CustomAnalyzerDescriptor SetupContentAnalyzer(CustomAnalyzerDescriptor customAnalyzer, string documentType)
        {
            // Use ngrams analyzer for search in the middle of the word
            // http://www.elasticsearch.org/guide/en/elasticsearch/guide/current/ngrams-compound-words.html
            return customAnalyzer
                .Tokenizer("standard")
                .Filters("lowercase", NGramFilterName);
        }

        protected virtual CustomAnalyzerDescriptor SetupKeywordAnalyzer(CustomAnalyzerDescriptor customAnalyzer, string documentType)
        {
            return customAnalyzer
                .Tokenizer("keyword")
                .Filters("lowercase");
        }

        protected virtual TokenFiltersDescriptor SetupTokenFilters(TokenFiltersDescriptor tokenFilters, string documentType)
        {
            return tokenFilters.NGram(NGramFilterName, descriptor => SetupNGramFilter(descriptor, documentType));
        }

        protected virtual NGramTokenFilterDescriptor SetupNGramFilter(NGramTokenFilterDescriptor nGram, string documentType)
        {
            return nGram.MinGram(3).MaxGram(20);
        }

        #endregion

        protected virtual Properties<IProperties> GetMappedProperties(string indexName, string documentType)
        {
            var properties = GetMappingFromCache(indexName, documentType);
            if (properties == null)
            {
                if (IndexExists(indexName))
                {
                    var mapping = Client.GetMapping(new GetMappingRequest(indexName, documentType)).Mapping;
                    if (mapping != null)
                    {
                        properties = new Properties<IProperties>(mapping.Properties);
                    }
                }
            }

            properties = properties ?? new Properties<IProperties>();
            AddMappingToCache(indexName, documentType, properties);

            return properties;
        }

        protected virtual void UpdateMapping(string indexName, string documentType, Properties<IProperties> properties)
        {
            var mappingRequest = new PutMappingRequest(indexName, documentType) { Properties = properties };
            var response = Client.Map<DocumentDictionary>(m => mappingRequest);

            if (!response.IsValid)
            {
                ThrowException("Failed to submit mapping. " + response.DebugInformation, response.OriginalException);
            }

            AddMappingToCache(indexName, documentType, properties);

            Client.Refresh(indexName);
        }

        protected virtual Properties<IProperties> GetMappingFromCache(string indexName, string documentType)
        {
            var mappingKey = CreateCacheKey(indexName, documentType);
            return _mappings.ContainsKey(mappingKey) ? _mappings[mappingKey] : null;
        }

        protected virtual void AddMappingToCache(string indexName, string documentType, Properties<IProperties> properties)
        {
            var mappingKey = CreateCacheKey(indexName, documentType);
            _mappings[mappingKey] = properties;
        }

        protected virtual void RemoveMappingFromCache(string indexName, string documentType)
        {
            var mappingKey = CreateCacheKey(indexName, documentType);
            _mappings.Remove(mappingKey);
        }

        protected virtual string CreateCacheKey(params string[] parts)
        {
            return string.Join("/", parts);
        }

        protected virtual string[] ParseCacheKey(string key)
        {
            return key.Split('/');
        }

        protected virtual void ThrowException(string message, Exception innerException)
        {
            throw new SearchException($"{message}. URL:{ElasticServerUrl}", innerException);
        }

        protected virtual void SetupProperty(IProperty property, IDocumentField field, string documentType)
        {
            //property.DocValues = !field.ContainsAttribute(IndexStore.No);

            SetupStringProperty(property as StringProperty, field, documentType);
        }

        protected virtual void SetupStringProperty(StringProperty stringProperty, IDocumentField field, string documentType)
        {
            if (stringProperty != null)
            {
                stringProperty.Store = field.ContainsAttribute(IndexStore.Yes);
                stringProperty.Index = field.ContainsAttribute(IndexType.NotAnalyzed) ? FieldIndexOption.NotAnalyzed : field.ContainsAttribute(IndexType.Analyzed) ? FieldIndexOption.Analyzed : FieldIndexOption.No;

                // for not analyzed fields use KeywordAnalyzer instead
                if (stringProperty.Index == FieldIndexOption.NotAnalyzed)
                {
                    stringProperty.Analyzer = KeywordAnalyzerName;
                    stringProperty.Index = FieldIndexOption.Analyzed;
                }

                if (field.Name.StartsWith("__content", StringComparison.OrdinalIgnoreCase))
                {
                    stringProperty.Analyzer = ContentAnalyzerName;
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
