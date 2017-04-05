using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Rest.Azure;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{
    [CLSCompliant(false)]
    public class AzureSearchProvider : ISearchProvider
    {
        private const string _fieldNamePrefix = "f_";
        private const string _keyFieldName = _fieldNamePrefix + "__key";

        private readonly ISearchConnection _connection;
        private readonly Dictionary<string, List<IDocument>> _pendingDocuments = new Dictionary<string, List<IDocument>>();
        private readonly Dictionary<string, IList<Field>> _mappings = new Dictionary<string, IList<Field>>();

        public AzureSearchProvider(ISearchConnection connection, ISearchQueryBuilder[] queryBuilders)
        {
            _connection = connection;
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
                        var indexName = GetIndexName(scope, documentType);

                        var providerFields = GetMapping(indexName, documentType);
                        var oldFieldsCount = providerFields.Count;

                        var simpleDocuments = documents.Select(document => ConvertToSimpleDocument(document, providerFields, documentType)).ToList();

                        var updateMapping = providerFields.Count != oldFieldsCount;
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
                        }
                        catch (CloudException ex)
                        {
                            throw new IndexBuildException(ex.Message, ex);
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
                ThrowException($"Document type not supported: {typeof(T).Name}", new NotImplementedException());
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
            try
            {
                var indexName = GetIndexName(scope, documentType);

                if (string.IsNullOrEmpty(documentType))
                {
                    Client.Indexes.Delete(indexName);
                }
                else
                {
                    throw new NotImplementedException();
                }

                RemoveMappingFromCache(indexName, documentType);
            }
            catch (Exception ex)
            {
                ThrowException("Failed to remove indexes", ex);
            }

            return true;
        }

        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria)
            where T : class
        {
            throw new NotImplementedException();
        }

        #endregion


        protected virtual DocumentDictionary ConvertToSimpleDocument(IDocument document, IList<Field> providerFields, string documentType)
        {
            var result = new DocumentDictionary();

            for (var index = 0; index < document.FieldCount; index++)
            {
                var field = document[index];
                field.Name = ConvertToAzureFieldName(field.Name);

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
                    var providerField = AddProviderField(documentType, providerFields, field.Name, field);
                    var isCollection = providerField.Type.ToString().StartsWith("Collection(");

                    result.Add(key, isCollection ? field.Values : field.Value);
                }
            }

            return result;
        }

        protected virtual string ConvertToAzureFieldName(string fieldName)
        {
            return _fieldNamePrefix + fieldName;
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
            var isStored = field.ContainsAttribute(IndexStore.Yes);
            var isAnalyzed = field.ContainsAttribute(IndexType.Analyzed);
            var isNotAnalyzed = field.ContainsAttribute(IndexType.NotAnalyzed);
            var isCollection = field.ContainsAttribute(IndexDataType.StringCollection);

            var originalFieldType = field.Value?.GetType() ?? typeof(object);
            var providerFieldType = GetProviderFieldType(documentType, fieldName, originalFieldType);

            if (isCollection)
            {
                providerFieldType = DataType.Collection(providerFieldType);
            }

            var providerField = new Field(fieldName, providerFieldType)
            {
                IsKey = fieldName == _keyFieldName,
                IsRetrievable = isStored,
                IsSearchable = isAnalyzed,
                IsFilterable = isNotAnalyzed,
                IsFacetable = isNotAnalyzed,
                IsSortable = isNotAnalyzed && !isCollection,
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
            return scope;
        }

        protected virtual bool IndexExists(string indexName)
        {
            return Client.Indexes.Exists(indexName);
        }

        #region Create and configure index

        protected virtual void CreateIndex(string indexName, string documentType, IList<Field> providerFields)
        {
            var index = new Index
            {
                Name = indexName,
                Fields = providerFields,
            };

            Client.Indexes.Create(index);
        }

        #endregion


        protected virtual IList<Field> GetMapping(string indexName, string documentType)
        {
            var providerFields = GetMappingFromCache(indexName, documentType);
            if (providerFields == null)
            {
                if (IndexExists(indexName))
                {
                    providerFields = Client.Indexes.Get(indexName).Fields;
                }
            }

            providerFields = providerFields ?? new List<Field>();
            AddMappingToCache(indexName, documentType, providerFields);

            return providerFields;
        }

        protected virtual void UpdateMapping(string indexName, string documentType, IList<Field> providerFields)
        {
            var index = new Index
            {
                Name = indexName,
                Fields = providerFields,
            };

            var updatedIndex = Client.Indexes.CreateOrUpdate(indexName, index);

            // TODO: Need to wait some time until changes are applied

            AddMappingToCache(indexName, documentType, updatedIndex.Fields);
        }

        protected virtual IList<Field> GetMappingFromCache(string indexName, string documentType)
        {
            var mappingKey = CreateCacheKey(indexName, documentType);
            return _mappings.ContainsKey(mappingKey) ? _mappings[mappingKey] : null;
        }

        protected virtual void AddMappingToCache(string indexName, string documentType, IList<Field> providerFields)
        {
            var mappingKey = CreateCacheKey(indexName, documentType);
            _mappings[mappingKey] = providerFields;
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
            throw new AzureSearchException($"{message}. Service name: {_connection.DataSource}", innerException);
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
