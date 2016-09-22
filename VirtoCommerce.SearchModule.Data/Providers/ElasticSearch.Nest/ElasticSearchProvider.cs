using System;
using System.Collections.Generic;
using System.Globalization;
using VirtoCommerce.SearchModule.Data.Model;
using Nest;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using VirtoCommerce.SearchModule.Data.Model.Indexing;
using VirtoCommerce.SearchModule.Data.Model.Search;
using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Model.Filters;
using System.Collections.Specialized;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    /// <summary>
    /// NEST based elastic search provider.
    /// </summary>
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

                    var settings = new ConnectionSettings(new Uri("http://" + url));

                    if (EnableTrace)
                    {
                        settings.DisableDirectStreaming().OnRequestCompleted(details =>
                        {
                            Debug.WriteLine("### ES REQEUST ###");
                            if (details.RequestBodyInBytes != null) Trace.WriteLine(Encoding.UTF8.GetString(details.RequestBodyInBytes));
                            Debug.WriteLine("### ES RESPONSE ###");
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
        public bool EnableTrace
        {
            get; set;
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

        public ElasticSearchProvider(ISearchConnection connection)
        {
            _connection = connection;
            Init();

#if DEBUG
            this.EnableTrace = true;
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
        #region ISearchProvider Members

        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            // Build query
            var command = BuildQuery<T>(scope, criteria);

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
                ThrowException(string.Format("Document of type {0} not supported", typeof(T).Name), new NotImplementedException());
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

                        if (!result.IsValid && !(result.ApiCall.HttpStatusCode == 404))
                            throw new IndexBuildException(result.DebugInformation);
                    }
                }
                else
                {
                    var result = Client.DeleteIndex(scope);

                    if (!result.IsValid && !(result.ApiCall.HttpStatusCode == 404))
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
        #endregion

        public virtual SearchRequest BuildQuery<T>(string scope, ISearchCriteria criteria) where T : class
        {
            var builder = new SearchRequest(scope, criteria.DocumentType);
            builder.From = criteria.StartingRecord;
            builder.Size = criteria.RecordsToRetrieve;

            QueryContainer mainFilter = null;
            QueryContainer mainQuery = null;

            #region Sorting

            // Add sort order
            if (criteria.Sort != null)
            {
                var fields = criteria.Sort.GetSort();
                foreach (var field in fields)
                {
                    if (builder.Sort == null)
                    {
                        builder.Sort = new List<ISort>();
                    }

                    builder.Sort.Add(
                        new SortField
                        {
                            Field = field.FieldName,
                            Order = field.IsDescending ? SortOrder.Descending : SortOrder.Ascending,
                            Missing = "_last",
                            IgnoreUnmappedFields = true
                        });
                }
            }
            #endregion

            #region Filters
            // Perform facet filters
            if (criteria.CurrentFilters != null && criteria.CurrentFilters.Any())
            {
                //var combinedFilter = new List<QueryContainer>();
                // group filters
                foreach (var filter in criteria.CurrentFilters)
                {
                    // Skip currencies that are not part of the filter
                    if (filter.GetType() == typeof(PriceRangeFilter)) // special filtering 
                    {
                        var priceRangeFilter = filter as PriceRangeFilter;
                        if (priceRangeFilter != null)
                        {
                            var currency = priceRangeFilter.Currency;
                            if (!currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                    }

                    var filterQuery = ElasticQueryHelper.CreateQuery<T>(criteria, filter);

                    if (filterQuery != null)
                    {
                        mainFilter &= filterQuery;
                        //combinedFilter.Add(filterQuery);
                    }
                }


            }
            #endregion

            #region KeywordSearchCriteria
            if (criteria is KeywordSearchCriteria)
            {
                var c = criteria as KeywordSearchCriteria;
                if (!string.IsNullOrEmpty(c.SearchPhrase))
                {
                    var searchFields = new List<string>();

                    searchFields.Add("__content");
                    if (!string.IsNullOrEmpty(c.Locale))
                    {
                        searchFields.Add(string.Format("__content_{0}", c.Locale.ToLower()));
                    }

                    mainQuery &= CreateQuery(c, searchFields.ToArray());
                }
            }
            #endregion

            builder.PostFilter = mainFilter;
            builder.Query = mainQuery;

            // Add search aggregations
            var aggregations = GetAggregations<T>(criteria);
            builder.Aggregations = aggregations;

            return builder;
        }

        #region Aggregations
        protected virtual AggregationDictionary GetAggregations<T>(ISearchCriteria criteria) where T : class
        {
            // Now add aggregations
            var container = new Dictionary<string, AggregationContainer>();
            foreach (var filter in criteria.Filters)
            {
                if (filter is AttributeFilter)
                {
                    AddAggregationQueries<T>(container, filter.Key.ToLower(), criteria);
                }
                else if (filter is PriceRangeFilter)
                {
                    var currency = ((PriceRangeFilter)filter).Currency;
                    if (currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                    {
                        AddAggregationPriceQueries<T>(container, filter.Key.ToLower(), ((PriceRangeFilter)filter).Values, criteria);
                    }
                }
                else if (filter is RangeFilter)
                {
                    AddAggregationQueries<T>(container, filter.Key.ToLower(), ((RangeFilter)filter).Values, criteria);
                }
            }

            return container;
        }

        protected virtual void AddAggregationQueries<T>(Dictionary<string, AggregationContainer> container, string field, ISearchCriteria criteria) where T : class
        {
            var existing_filters = GetExistingFilters<T>(criteria, field);

            var termAgg = new TermsAggregation(field) { Field = field };
            var agg = new FilterAggregation(field);

            var boolQuery = new BoolQuery() { Must = existing_filters };
            agg.Filter = boolQuery;
            agg.Aggregations = termAgg;
            container.Add(field, agg);
        }

        protected virtual void AddAggregationPriceQueries<T>(Dictionary<string, AggregationContainer> container, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria) where T : class
        {
            if (values == null)
                return;

            var existing_filters = GetExistingFilters<T>(criteria, fieldName);

            foreach (var value in values)
            {
                var query = ElasticQueryHelper.CreatePriceRangeFilter<T>(criteria, fieldName, value);

                if (query != null)
                {
                    var agg = new FilterAggregation(string.Format("{0}-{1}", fieldName, value.Id));
                    var all_filters = new List<QueryContainer>();
                    all_filters.AddRange(existing_filters);
                    all_filters.Add(query);
                    var boolQuery = new BoolQuery() { Must = all_filters };
                    agg.Filter = boolQuery;
                    container.Add(string.Format("{0}-{1}", fieldName, value.Id), agg);
                }
            }
        }

        protected virtual BoolQuery CreatePriceRangeFilter<T>(ISearchCriteria criteria, string field, RangeFilterValue value) where T : class
        {
            return ElasticQueryHelper.CreatePriceRangeFilter<T>(criteria, field, value);
        }

        protected virtual void AddAggregationQueries<T>(Dictionary<string, AggregationContainer> container, string fieldName, IEnumerable<RangeFilterValue> values, ISearchCriteria criteria) where T : class
        {
            if (values == null)
                return;

            var existing_filters = GetExistingFilters<T>(criteria, fieldName);

            foreach (var value in values)
            {
                var agg = new FilterAggregation(string.Format("{0}-{1}", fieldName, value.Id));
                var range_query = new TermRangeQuery() { Field = fieldName, GreaterThanOrEqualTo = value.Lower, LessThan = value.Upper };

                var all_filters = new List<QueryContainer>();
                all_filters.AddRange(existing_filters);
                all_filters.Add(range_query);
                var boolQuery = new BoolQuery() { Must = all_filters };
                agg.Filter = boolQuery;
                container.Add(string.Format("{0}-{1}", fieldName, value.Id), agg);
            }
        }
        #endregion

        #region Helper Query Methods
        protected QueryContainer CreateQuery(string fieldName, StringCollection filter, bool lowerCase = true)
        {
            QueryContainer query = null;
            fieldName = fieldName.ToLower();
            if (filter.Count > 0)
            {
                if (filter.Count == 1)
                {
                    if (!string.IsNullOrEmpty(filter[0]))
                    {
                        query &= CreateQuery(fieldName, filter[0], lowerCase) as QueryContainer;
                    }
                }
                else
                {
                    var booleanQuery = new BoolQuery();
                    var containsFilter = false;
                    var valueContainer = new List<QueryContainer>();
                    foreach (var index in filter.Cast<string>().Where(index => !String.IsNullOrEmpty(index)))
                    {
                        valueContainer.Add(new WildcardQuery() { Field = fieldName.ToLower(), Value = lowerCase ? index.ToLower() : index });
                        containsFilter = true;
                    }
                    if (containsFilter)
                    {
                        booleanQuery.Should = valueContainer;
                        query |= booleanQuery;
                    }

                }
            }

            return query;
        }

        protected virtual QueryContainer CreateQuery(string fieldName, string filter, bool lowerCase = true)
        {
            QueryContainer query = null;
            query &= new WildcardQuery() { Field = fieldName.ToLower(), Value = lowerCase ? filter.ToLower() : filter };
            return query;
        }

        protected virtual QueryContainer CreateQuery(KeywordSearchCriteria filter, params string[] fields)
        {
            QueryContainer query = null;
            var searchPhrase = filter.SearchPhrase;
            MultiMatchQuery multiMatch;
            if (filter.IsFuzzySearch)
            {
                multiMatch = new MultiMatchQuery()
                {
                    Fields = fields,
                    Query = searchPhrase,
                    Fuzziness = Fuzziness.Auto,
                    Analyzer = "standard",
                    Operator = Operator.And
                };
            }
            else
            {
                multiMatch = new MultiMatchQuery()
                {
                    Fields = fields,
                    Query = searchPhrase,
                    Analyzer = "standard",
                    Operator = Operator.And
                };
            }

            query &= multiMatch;
            return query;
        }

        private List<QueryContainer> GetExistingFilters<T>(ISearchCriteria criteria, string field) where T : class
        {
            var existing_filters = new List<QueryContainer>();
            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.Equals(field, StringComparison.OrdinalIgnoreCase))
                {
                    var q = ElasticQueryHelper.CreateQuery<T>(criteria, f);
                    existing_filters.Add(q);
                }
            }

            return existing_filters;
        }
        #endregion

        #region Helper Methods
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
                    //Use ngrams analyzer for search in the middle of word
                    //http://www.elasticsearch.org/guide/en/elasticsearch/guide/current/ngrams-compound-words.html
                    Client.CreateIndex(scope, x => x.Settings(v => v
                          .Analysis(a => a.TokenFilters(f => f.NGram("trigrams_filter", ng => ng.MinGram(3).MaxGram(20)))
                          .Analyzers(an => an
                              .Custom(_indexAnalyzer, custom => custom
                                  .Tokenizer("standard")
                                  .Filters("lowercase", "trigrams_filter"))
                              .Custom(SearchAnalyzer, custom => custom
                                  .Tokenizer("standard")
                                  .Filters("lowercase"))))));

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

        private static string GetCoreName(string scope, string documentType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", scope.ToLower(), documentType);
        }

        private void ThrowException(string message, Exception innerException)
        {
            throw new ElasticSearchException(string.Format(CultureInfo.InvariantCulture, "{0}. URL:{1}", message, ElasticServerUrl), innerException);
        }

        private void SetupProperty(IProperty property, IDocumentField field)
        {
            //property.DocValues = !field.ContainsAttribute(IndexStore.No);

            if (property is StringProperty)
            {
                var stringProperty = property as StringProperty;
                stringProperty.Store = field.ContainsAttribute(IndexStore.Yes);
                stringProperty.Index = field.ContainsAttribute(IndexType.NotAnalyzed) ? FieldIndexOption.NotAnalyzed : field.ContainsAttribute(IndexType.Analyzed) ? FieldIndexOption.Analyzed : FieldIndexOption.No;

                if (field.Name.StartsWith("__content", StringComparison.OrdinalIgnoreCase))
                {
                    stringProperty.Analyzer = _indexAnalyzer;
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
                else if (Regex.Match(field.Name, "__content_se.*").Success)
                {
                    stringProperty.Analyzer = "swedish";
                }
                else if (Regex.Match(field.Name, "__content_no.*").Success)
                {
                    stringProperty.Analyzer = "norwegian";
                }
            }
        }
        #endregion
    }
}
