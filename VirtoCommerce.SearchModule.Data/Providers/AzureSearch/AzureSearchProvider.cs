using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Rest.Azure;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.AzureSearch
{
    [CLSCompliant(false)]
    public class AzureSearchProvider : ISearchProvider
    {
        private readonly ISearchConnection _connection;
        private readonly ISearchCriteriaPreprocessor[] _searchCriteriaPreprocessors;
        private readonly Dictionary<string, List<IDocument>> _pendingDocuments = new Dictionary<string, List<IDocument>>();
        private readonly Dictionary<string, IList<Field>> _mappings = new Dictionary<string, IList<Field>>();

        public AzureSearchProvider(ISearchConnection connection, ISearchCriteriaPreprocessor[] searchCriteriaPreprocessors, ISearchQueryBuilder[] queryBuilders)
        {
            _connection = connection;
            _searchCriteriaPreprocessors = searchCriteriaPreprocessors;
            QueryBuilders = queryBuilders;
        }

        private SearchServiceClient _client;
        protected SearchServiceClient Client => _client ?? (_client = CreateSearchServiceClient());

        public bool AutoCommit { get; set; } = true;
        public int AutoCommitCount { get; set; } = 100;

        #region ISearchProvider members

        public virtual ISearchQueryBuilder[] QueryBuilders { get; }

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

                        var providerFields = GetMapping(scope, documentType);
                        var oldFieldsCount = providerFields.Count;

                        var simpleDocuments = documents.Select(document => ConvertToSimpleDocument(document, providerFields, documentType)).ToList();
                        var updateMapping = providerFields.Count != oldFieldsCount;

                        var indexName = GetIndexName(scope, documentType);
                        var indexExits = IndexExists(indexName);

                        if (!indexExits)
                        {
                            CreateIndex(indexName, documentType, providerFields);
                        }
                        else if (updateMapping)
                        {
                            UpdateMapping(indexName, documentType, providerFields);
                        }

                        var batch = IndexBatch.Upload(simpleDocuments);
                        var indexClient = GetSearchIndexClient(indexName);

                        // Retry if cannot index documents after updating the mapping
                        for (var i = 9; i >= 0; i--)
                        {
                            try
                            {
                                var result = indexClient.Documents.Index(batch);

                                foreach (var r in result.Results)
                                {
                                    if (!r.Succeeded)
                                    {
                                        throw new IndexBuildException(r.ErrorMessage);
                                    }
                                }

                                break;
                            }
                            catch (IndexBatchException ex)
                            {
                                var builder = new StringBuilder();
                                builder.AppendLine(ex.Message);

                                foreach (var result in ex.IndexingResults.Where(r => !r.Succeeded))
                                {
                                    builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Key: {0}, Error: {1}", result.Key, result.ErrorMessage));
                                }

                                throw new IndexBuildException(builder.ToString(), ex);
                            }
                            catch (CloudException cloudException)
                                when (i > 0 && cloudException.Message.Contains("Make sure to only use property names that are defined by the type"))
                            {
                                // Need to wait some time until new mapping is applied
                                Thread.Sleep(1000);
                            }
                        }

                        // Remove pending documents
                        documents.Clear();
                    }
                }
            }
        }

        public virtual void Index<T>(string scope, string documentType, T document)
        {
            var doc = document as IDocument;
            if (doc == null)
            {
                ThrowException($"Document type is not supported: {typeof(T).Name}", new NotImplementedException());
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

                    if (IndexExists(indexName))
                    {
                        Client.Indexes.Delete(indexName);
                    }

                    RemoveMappingFromCache(scope, documentType);
                }
                catch (Exception ex)
                {
                    ThrowException("Failed to remove index", ex);
                }

                result = true;
            }

            return result;
        }

        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria)
            where T : class
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            _searchCriteriaPreprocessors.ForEach(p => p.Process(criteria));

            var availableFields = GetAvailableFields(scope, criteria.DocumentType);
            var queryBuilder = GetQueryBuilder(criteria);
            var query = queryBuilder.BuildQuery<T>(scope, criteria, availableFields) as AzureSearchQuery;

            var indexName = GetIndexName(scope, criteria.DocumentType);
            var indexClient = GetSearchIndexClient(indexName);

            var searchResult = indexClient.Documents.Search<DocumentDictionary>(query?.SearchText, query?.SearchParameters);

            var result = new AzureSearchResults(criteria, searchResult) as ISearchResults<T>;
            return result;
        }

        #endregion

        protected virtual IList<IFieldDescriptor> GetAvailableFields(string scope, string documentType)
        {
            return GetMapping(scope, documentType)
                .Select(f => new FieldDescriptor { Name = f.Name, DataType = f.Type.ToString() } as IFieldDescriptor)
                .ToList();
        }

        protected virtual ISearchQueryBuilder GetQueryBuilder(ISearchCriteria criteria)
        {
            if (QueryBuilders == null)
                throw new InvalidOperationException("No query builders defined");

            var queryBuilder = QueryBuilders.SingleOrDefault(b => b.DocumentType.Equals(criteria.DocumentType)) ??
                               QueryBuilders.First(b => b.DocumentType.Equals(string.Empty));

            return queryBuilder;
        }

        protected virtual DocumentDictionary ConvertToSimpleDocument(IDocument document, IList<Field> providerFields, string documentType)
        {
            var result = new DocumentDictionary();

            for (var index = 0; index < document.FieldCount; index++)
            {
                var field = document[index];
                var fieldName = AzureSearchHelper.ToAzureFieldName(field.Name);

                if (result.ContainsKey(fieldName))
                {
                    var newValues = new List<object>();

                    var currentValue = result[fieldName];
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
                    result[fieldName] = newValues.ToArray();
                }
                else
                {
                    var providerField = AddProviderField(documentType, providerFields, fieldName, field);
                    var isCollection = providerField.Type.ToString().StartsWith("Collection(");

                    result.Add(fieldName, isCollection ? field.Values : field.Value);
                }
            }

            return result;
        }

        protected virtual Field AddProviderField(string documentType, IList<Field> providerFields, string fieldName, IDocumentField field)
        {
            var providerField = providerFields.FirstOrDefault(f => f.Name == fieldName);

            if (providerField == null)
            {
                providerField = CreateProviderField(documentType, fieldName, field);
                providerFields.Add(providerField);
            }

            return providerField;
        }

        protected virtual Field CreateProviderField(string documentType, string fieldName, IDocumentField field)
        {
            var originalFieldType = field.Value?.GetType() ?? typeof(object);
            var providerFieldType = GetProviderFieldType(documentType, fieldName, originalFieldType);

            var isString = providerFieldType == DataType.String;
            var isIndexed = !field.ContainsAttribute(IndexType.No);
            var isAnalyzed = field.ContainsAttribute(IndexType.Analyzed) || isIndexed && !field.ContainsAttribute(IndexType.NotAnalyzed);
            var isStored = field.ContainsAttribute(IndexStore.Yes) || !field.ContainsAttribute(IndexStore.No);
            var isCollection = field.ContainsAttribute(IndexDataType.StringCollection) && isString;

            if (isCollection)
            {
                providerFieldType = DataType.Collection(providerFieldType);
            }

            var providerField = new Field(fieldName, providerFieldType)
            {
                IsKey = fieldName == AzureSearchHelper.KeyFieldName,
                IsRetrievable = isStored,
                IsSearchable = isString && isAnalyzed,
                IsFilterable = isIndexed,
                IsFacetable = isIndexed,
                IsSortable = isIndexed && !isCollection,
            };

            return providerField;
        }

        protected virtual DataType GetProviderFieldType(string documentType, string fieldName, Type fieldType)
        {
            if (fieldType == typeof(string))
                return DataType.String;
            if (fieldType == typeof(int))
                return DataType.Int32;
            if (fieldType == typeof(long))
                return DataType.Int64;
            if (fieldType == typeof(double) || fieldType == typeof(decimal))
                return DataType.Double;
            if (fieldType == typeof(bool))
                return DataType.Boolean;
            if (fieldType == typeof(DateTimeOffset) || fieldType == typeof(DateTime))
                return DataType.DateTimeOffset;

            throw new ArgumentException($"Field {fieldName} has unsupported type {fieldType}", nameof(fieldType));
        }

        protected virtual string GetIndexName(string scope, string documentType)
        {
            // Use different index for each document type
            return string.Join("-", scope, documentType);
        }

        protected virtual bool IndexExists(string indexName)
        {
            return Client.Indexes.Exists(indexName);
        }

        #region Create and configure index

        protected virtual void CreateIndex(string indexName, string documentType, IList<Field> providerFields)
        {
            var index = CreateIndexDefinition(indexName, providerFields);
            Client.Indexes.Create(index);
        }

        protected virtual Index CreateIndexDefinition(string indexName, IList<Field> providerFields)
        {
            var index = new Index
            {
                Name = indexName,
                Fields = providerFields.OrderBy(f => f.Name).ToArray(),
            };

            return index;
        }

        #endregion


        protected virtual IList<Field> GetMapping(string scope, string documentType)
        {
            var providerFields = GetMappingFromCache(scope, documentType);
            if (providerFields == null)
            {
                var indexName = GetIndexName(scope, documentType);
                if (IndexExists(indexName))
                {
                    providerFields = Client.Indexes.Get(indexName).Fields;
                }
            }

            providerFields = providerFields ?? new List<Field>();
            AddMappingToCache(scope, documentType, providerFields);

            return providerFields;
        }

        protected virtual void UpdateMapping(string indexName, string documentType, IList<Field> providerFields)
        {
            var index = CreateIndexDefinition(indexName, providerFields);
            var updatedIndex = Client.Indexes.CreateOrUpdate(indexName, index);

            AddMappingToCache(indexName, documentType, updatedIndex.Fields);
        }

        protected virtual IList<Field> GetMappingFromCache(string scope, string documentType)
        {
            var mappingKey = CreateCacheKey(scope, documentType);
            return _mappings.ContainsKey(mappingKey) ? _mappings[mappingKey] : null;
        }

        protected virtual void AddMappingToCache(string scope, string documentType, IList<Field> providerFields)
        {
            var mappingKey = CreateCacheKey(scope, documentType);
            _mappings[mappingKey] = providerFields;
        }

        protected virtual void RemoveMappingFromCache(string scope, string documentType)
        {
            var mappingKey = CreateCacheKey(scope, documentType);
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
            throw new SearchException($"{message}. Service name: {_connection.DataSource}", innerException);
        }


        private SearchServiceClient CreateSearchServiceClient()
        {
            var result = new SearchServiceClient(_connection.DataSource, new SearchCredentials(_connection.AccessKey));
            return result;
        }

        private ISearchIndexClient GetSearchIndexClient(string indexName)
        {
            return Client.Indexes.GetClient(indexName);
        }
    }
}
