using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.LuceneSearch
{
    /// <summary>
    /// File based search provider based on Lucene.
    /// </summary>
    public class LuceneSearchProvider : ISearchProvider
    {
        private static readonly Dictionary<string, IndexWriter> _indexFolders = new Dictionary<string, IndexWriter>();
        private static readonly object _providerlock = new object();

        private readonly ISearchConnection _connection;
        private readonly ISearchCriteriaPreprocessor[] _searchCriteriaPreprocessors;
        private readonly Dictionary<string, List<Document>> _pendingDocuments = new Dictionary<string, List<Document>>();
        private bool _isInitialized;
        private string _location = string.Empty;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuceneSearchProvider" /> class.
        /// </summary>
        /// <param name="queryBuilders">The query builders.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="searchCriteriaPreprocessors"></param>
        public LuceneSearchProvider(ISearchQueryBuilder[] queryBuilders, ISearchConnection connection, ISearchCriteriaPreprocessor[] searchCriteriaPreprocessors)
        {
            AutoCommit = true;
            AutoCommitCount = 100;

            QueryBuilders = queryBuilders;
            _connection = connection;
            _searchCriteriaPreprocessors = searchCriteriaPreprocessors;
            Init();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether [auto commit].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [auto commit]; otherwise, <c>false</c>.
        /// </value>
        public bool AutoCommit { get; set; }

        /// <summary>
        ///     Gets or sets the auto commit count.
        /// </summary>
        /// <value>The auto commit count.</value>
        public int AutoCommitCount { get; set; }

        /// <summary>
        ///     Gets or sets the query builder.
        /// </summary>
        /// <value>
        ///     The query builder.
        /// </value>
        public ISearchQueryBuilder[] QueryBuilders { get; set; }

        /// <summary>
        ///     Closes the specified provider.
        /// </summary>
        /// <param name="scope">Name of the application.</param>
        /// <param name="documentType">The documentType.</param>
        public virtual void Close(string scope, string documentType)
        {
            Close(scope, documentType, true);
        }

        /// <summary>
        ///     Writes all documents saved in memory to the index writer.
        /// </summary>
        /// <param name="scope">Name of the application.</param>
        public virtual void Commit(string scope)
        {
            var documentTypes = _pendingDocuments.Keys.ToList();

            lock (_providerlock)
            {
                foreach (var documentType in documentTypes)
                {
                    var documents = _pendingDocuments[documentType];
                    if (documents == null || documents.Count == 0)
                    {
                        continue;
                    }

                    var writer = GetIndexWriter(documentType, true, false);

                    foreach (var doc in documents)
                    {
                        const string keyName = "__key";
                        var key = doc.GetValues(keyName);

                        if (key != null && key.Length > 0)
                        {
                            var term = new Term(keyName, key[0]);
                            writer.UpdateDocument(term, doc);
                        }
                        else
                        {
                            writer.AddDocument(doc);
                        }
                    }

                    // Remove documents
                    _pendingDocuments[documentType].Clear();
                }
            }
        }

        public virtual void Index<T>(string scope, string documentType, T document)
        {
            var doc = document as IDocument;
            if (doc == null)
            {
                throw new ArgumentException($"Document type '{typeof(T).Name}' is not supported", nameof(document));
            }

            Index(scope, documentType, doc);
        }

        /// <summary>
        ///     Adds the document to the index. Depending on the provider, the document will be commited only after commit is called.
        /// </summary>
        /// <param name="scope">The scope of the document, used to seperate indexes for different applications.</param>
        /// <param name="documentType">The type of the document, typically simply the name associated with an indexer like catalog, order or catalogitem.</param>
        /// <param name="document">The document.</param>
        protected virtual void Index(string scope, string documentType, IDocument document)
        {
            var folderName = GetFolderName(scope, documentType);
            if (!_pendingDocuments.ContainsKey(folderName))
            {
                _pendingDocuments.Add(folderName, new List<Document>());
            }

            _pendingDocuments[folderName].Add(ConvertDocument(document));

            // Make sure to auto commit changes when limit is reached, still need to call close before changes are written
            if (AutoCommit && _pendingDocuments.Count > AutoCommitCount)
            {
                Commit(scope);
            }
        }

        /// <summary>
        ///     Removes the document type in the specified scope by document key value.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="documentType">Type of the document.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual int Remove(string scope, string documentType, string key, string value)
        {
            var result = 0;

            var term = new Term(key, value);

            // Close writer first
            Close(scope, documentType, false);

            var directoryInfo = new DirectoryInfo(GetDirectoryPath(GetFolderName(scope, documentType)));

            if (directoryInfo.Exists)
            {
                var dir = FSDirectory.Open(directoryInfo);

                using (var indexReader = IndexReader.Open(dir, false))
                {
                    var num = indexReader.DeleteDocuments(term);
                    result = num;
                }
            }

            return result;
        }

        /// <summary>
        ///     Removes all documents in the specified documentType and scope.
        /// </summary>
        /// <param name="scope">Name of the application.</param>
        /// <param name="documentType">The documentType.</param>
        public virtual bool RemoveAll(string scope, string documentType)
        {
            var result = false;

            if (!string.IsNullOrEmpty(documentType))
            {
                // Make sure the existing writer is closed
                Close(scope, documentType, false);

                // retrieve foldername
                var folderName = GetFolderName(scope, documentType);

                // re-initialize the write, so all documents are deleted
                GetIndexWriter(folderName, true, true);

                // now close the write so changes are saved
                Close(scope, documentType, false);

                result = true;
            }

            return result;
        }

        /// <summary>
        ///     Searches the datasource using the specified criteria. Criteria is parsed by the query builder specified by
        ///     <typeparamref />
        ///     .
        /// </summary>
        /// <param name="scope">Name of the application.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        /// <exception cref="SearchException"></exception>
        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            _searchCriteriaPreprocessors.ForEach(p => p.Process(criteria));

            ISearchResults<T> result = null;

            var directoryInfo = new DirectoryInfo(GetDirectoryPath(GetFolderName(scope, criteria.DocumentType)));

            if (directoryInfo.Exists)
            {
                var dir = FSDirectory.Open(directoryInfo);
                var searcher = new IndexSearcher(dir);

                var reader = searcher.IndexReader;
                var availableFields = GetAvailableFields(reader);
                var q = (LuceneSearchQuery)GetQueryBuilder(criteria).BuildQuery<T>(scope, criteria, availableFields);

                // filter out empty value
                var filter = q.Filter.ToString().Equals("BooleanFilter()") ? null : q.Filter;
                var query = q.Query.ToString().Equals(string.Empty) ? new MatchAllDocsQuery() : q.Query;

                Debug.WriteLine("Search Lucene Query:{0}", q.ToString());

                TopDocs docs;

                try
                {
                    var numDocs = criteria.StartingRecord + criteria.RecordsToRetrieve;

                    // numDocs must be > 0
                    if (numDocs < 1)
                    {
                        numDocs = 1;
                    }

                    if (criteria.Sort != null)
                    {
                        var fields = criteria.Sort.GetSort();
                        var sort = new Sort(fields.Select(field => new SortField(field.FieldName.ToLowerInvariant(), field.DataType, field.IsDescending)).ToArray());
                        docs = searcher.Search(query, filter, numDocs, sort);
                    }
                    else
                    {
                        docs = searcher.Search(query, filter, numDocs);
                    }
                }
                catch (Exception ex)
                {
                    throw new SearchException("Search exception", ex);
                }

                result = new LuceneSearchResults<T>(searcher, reader, docs, criteria, query, availableFields) as ISearchResults<T>;

                // Cleanup here
                reader.Dispose();
                searcher.Dispose();
            }

            return result;
        }


        protected virtual IList<IFieldDescriptor> GetAvailableFields(IndexReader reader)
        {
            var result = reader.GetFieldNames(IndexReader.FieldOption.ALL)
                .Select(f => new FieldDescriptor { Name = f } as IFieldDescriptor)
                .ToArray();
            return result;
        }

        protected virtual ISearchQueryBuilder GetQueryBuilder(ISearchCriteria criteria)
        {
            if (QueryBuilders == null)
                throw new InvalidOperationException("No query builders defined");

            var queryBuilder = QueryBuilders.SingleOrDefault(b => b.DocumentType.Equals(criteria.DocumentType)) ??
                               QueryBuilders.First(b => b.DocumentType.Equals(string.Empty));

            return queryBuilder;
        }
        /// <summary>
        ///     Converts the search document to lucene document
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        protected virtual Document ConvertDocument(IDocument document)
        {
            var doc = new Document();
            for (var index = 0; index < document.FieldCount; index++)
            {
                var field = document[index];
                AddFieldToDocument(doc, field);
            }

            return doc;
        }

        protected virtual void AddFieldToDocument(Document doc, IDocumentField field)
        {
            if (field?.Value == null)
            {
                return;
            }

            var fieldName = LuceneSearchHelper.ToLuceneFieldName(field.Name);
            var store = Field.Store.YES;
            var index = Field.Index.NOT_ANALYZED;

            if (field.ContainsAttribute(IndexStore.No))
            {
                store = Field.Store.NO;
            }

            if (field.ContainsAttribute(IndexType.Analyzed))
            {
                index = Field.Index.ANALYZED;
            }
            else if (field.ContainsAttribute(IndexType.No))
            {
                index = Field.Index.NO;
            }

            var isIndexed = !field.ContainsAttribute(IndexType.No);

            if (fieldName == "__key")
            {
                foreach (var value in field.Values)
                {
                    doc.Add(new Field(fieldName, value.ToString(), store, index));
                }
            }
            else if (field.Value is string)
            {
                foreach (var value in field.Values)
                {
                    doc.Add(new Field(fieldName, value.ToString(), store, index));

                    if (isIndexed)
                    {
                        doc.Add(new Field("_content", value.ToString(), Field.Store.NO, Field.Index.ANALYZED));
                    }
                }
            }
            else if (field.Value is bool)
            {
                var booleanFieldName = LuceneSearchHelper.GetBooleanFieldName(field.Name);

                foreach (var value in field.Values)
                {
                    var stringValue = value.ToStringInvariant();
                    doc.Add(new Field(fieldName, stringValue, store, index));
                    doc.Add(new Field(booleanFieldName, stringValue, Field.Store.NO, Field.Index.NOT_ANALYZED));
                }
            }
            else if (field.Value is DateTime)
            {
                foreach (var value in field.Values)
                {
                    var numericField = new NumericField(fieldName, store, index != Field.Index.NO);
                    numericField.SetLongValue(((DateTime)value).Ticks);
                    doc.Add(numericField);
                }
            }
            else
            {
                decimal t;
                if (decimal.TryParse(field.Value.ToString(), out t))
                {
                    foreach (var value in field.Values)
                    {
                        var stringValue = value.ToStringInvariant();

                        var numericField = new NumericField(fieldName, store, index != Field.Index.NO);
                        numericField.SetDoubleValue(double.Parse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture));
                        doc.Add(numericField);
                    }
                }
                else
                {
                    foreach (var value in field.Values)
                    {
                        doc.Add(new Field(fieldName, value.ToString(), store, index));
                    }
                }
            }
        }


        /// <summary>
        ///     Closes the specified documentType.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="documentType">The documentType.</param>
        /// <param name="optimize">
        ///     if set to <c>true</c> [optimize].
        /// </param>
        private static void Close(string scope, string documentType, bool optimize)
        {
            lock (_providerlock)
            {
                var folderName = GetFolderName(scope, documentType);

                if (_indexFolders.ContainsKey(folderName) && _indexFolders[folderName] != null)
                {
                    var writer = _indexFolders[folderName];
                    if (optimize)
                    {
                        writer.Optimize();
                    }

                    writer.Dispose(true); // added waiting for all merges to complete
                    _indexFolders.Remove(folderName);
                }
            }
        }

        /// <summary>
        ///     Gets the directory path.
        /// </summary>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns></returns>
        private string GetDirectoryPath(string folderName)
        {
            return Path.Combine(_location, folderName);
        }

        /// <summary>
        ///     Gets the name of the folder.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="documentType">Type of the document.</param>
        /// <returns></returns>
        private static string GetFolderName(string scope, string documentType)
        {
            return string.Join("-", scope, documentType);
        }

        /// <summary>
        ///     Gets the index writer.
        /// </summary>
        /// <param name="folderName">The folder name.</param>
        /// <param name="create">
        ///     if set to <c>true</c> [create].
        /// </param>
        /// <param name="isNew">
        ///     if set to <c>true</c> [is new].
        /// </param>
        /// <returns></returns>
        private IndexWriter GetIndexWriter(string folderName, bool create, bool isNew)
        {
            lock (_providerlock)
            {
                // Do this again to make sure _solr is still null
                if (!_indexFolders.ContainsKey(folderName) || _indexFolders[folderName] == null)
                {
                    if (!create)
                        return null;
                    var localDirectory = FSDirectory.Open(GetDirectoryPath(folderName));
                    if (!localDirectory.Directory.Exists)
                        isNew = true; // create new if directory doesn't exist
                    if (_indexFolders.ContainsKey(folderName))
                        _indexFolders.Remove(folderName);

                    var indexWriter = new IndexWriter(
                        localDirectory,
                        new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30),
                        isNew,
                        IndexWriter.MaxFieldLength.LIMITED);

                    _indexFolders.Add(folderName, indexWriter);
                }

                return _indexFolders[folderName];
            }
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        private void Init()
        {
            if (!_isInitialized)
            {
                // set location for indexes
                _location = _connection.DataSource;

                // resolve path, if we running in web environment
                if (_location.StartsWith("~"))
                {
                    if (HostingEnvironment.IsHosted)
                    {
                        _location = HostingEnvironment.MapPath(_location);
                    }
                    else
                    {
                        _location = _location.Substring(1);
                        if (_location.StartsWith("/"))
                            _location = _location.Substring(1);

                        _location = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _location);
                    }
                }

                _isInitialized = true;
            }
        }
    }
}
