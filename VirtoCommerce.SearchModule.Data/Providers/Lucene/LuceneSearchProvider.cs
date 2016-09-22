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
using u = Lucene.Net.Util;
using VirtoCommerce.SearchModule.Data.Model.Search;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Model.Indexing;
using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;
using System.Collections.Specialized;
using VirtoCommerce.SearchModule.Data.Model.Filters;
using Lucene.Net.QueryParsers;
using System.Text;

namespace VirtoCommerce.SearchModule.Data.Providers.Lucene
{
    /// <summary>
    ///     File based search provider based on Lucene.
    /// </summary>
    public class LuceneSearchProvider : ISearchProvider
    {
        private static readonly Dictionary<string, IndexWriter> _indexFolders = new Dictionary<string, IndexWriter>();
        private static readonly object _providerlock = new object();

        private readonly ISearchConnection _connection;
        private readonly Dictionary<string, List<Document>> _pendingDocuments = new Dictionary<string, List<Document>>();
        private bool _isInitialized;
        private string _location = string.Empty;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LuceneSearchProvider" /> class.
        /// </summary>
        /// <param name="queryBuilder">The query builder.</param>
        /// <param name="connection">The connection.</param>
        public LuceneSearchProvider(ISearchConnection connection)
        {
            AutoCommit = true;
            AutoCommitCount = 100;

            _connection = connection;
            Init();
        }
        #region ISearchProvider Members
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
            // process mapping
            if (document is IDocument) // older case scenario
            {
                Index(scope, documentType, document as IDocument);
            }
            else
            {
                new NotImplementedException(string.Format("Document of type {0} not supported", typeof(T).Name));
            }
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

            _pendingDocuments[folderName].Add(LuceneHelper.ConvertDocument(document));

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
        public virtual void RemoveAll(string scope, string documentType)
        {
            // Make sure the existing writer is closed
            Close(scope, documentType, false);

            // retrieve foldername
            var folderName = GetFolderName(scope, documentType);

            // re-initialize the write, so all documents are deleted
            GetIndexWriter(folderName, true, true);

            // now close the write so changes are saved
            Close(scope, documentType, false);
        }

        /// <summary>
        ///     Searches the datasource using the specified criteria. Criteria is parsed by the query builder specified by
        ///     <typeparamref />
        ///     .
        /// </summary>
        /// <param name="scope">Name of the application.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        /// <exception cref="VirtoCommerce.SearchModule.Data.Providers.Lucene.LuceneSearchException"></exception>
        public virtual ISearchResults<T> Search<T>(string scope, ISearchCriteria criteria) where T : class
        {
            ISearchResults<T> result;

            var directoryInfo = new DirectoryInfo(GetDirectoryPath(GetFolderName(scope, criteria.DocumentType)));

            if (directoryInfo.Exists)
            {
                var dir = FSDirectory.Open(directoryInfo);
                var searcher = new IndexSearcher(dir);

                var q = BuildQuery(scope, criteria);

                // filter out empty value
                var filter = q.Filter.ToString().Equals("BooleanFilter()") ? null : q.Filter;

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

                        docs = searcher.Search(q.Query, filter, numDocs,
                            new Sort(fields.Select(field => new SortField(field.FieldName, field.DataType, field.IsDescending)).ToArray()));
                    }
                    else
                    {
                        docs = searcher.Search(q.Query, filter, numDocs);
                    }
                }
                catch (Exception ex)
                {
                    throw new LuceneSearchException("Search exception", ex);
                }

                result = new LuceneSearchResults<T>(searcher, searcher.IndexReader, docs, criteria, q.Query) as ISearchResults<T>;

                // Cleanup here
                searcher.IndexReader.Dispose();
                searcher.Dispose();
            }
            else
            {
                result = null;
            }

            return result;
        }
        #endregion

        public virtual QueryBuilder BuildQuery(string scope, ISearchCriteria criteria)
        {
            var queryBuilder = new QueryBuilder();
            var queryFilter = new BooleanFilter();
            var query = new BooleanQuery();
            var analyzer = new StandardAnalyzer(u.Version.LUCENE_30);

            var fuzzyMinSimilarity = 0.7f;
            var isFuzzySearch = false;

            queryBuilder.Query = query;
            queryBuilder.Filter = queryFilter;

            if (criteria.CurrentFilters != null)
            {
                foreach (var filter in criteria.CurrentFilters)
                {
                    // Skip currencies that are not part of the filter
                    if (filter.GetType() == typeof(PriceRangeFilter)) // special filtering 
                    {
                        var currency = (filter as PriceRangeFilter).Currency;
                        if (!currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    var filterQuery = LuceneQueryHelper.CreateQuery(criteria, filter, Occur.SHOULD);

                    // now add other values that should also be counted?

                    if (filterQuery != null)
                    {
                        var clause = new FilterClause(filterQuery, Occur.MUST);
                        queryFilter.Add(clause);
                    }
                }
            }

            // add standard keyword search
            if (criteria is KeywordSearchCriteria)
            {
                var c = criteria as KeywordSearchCriteria;
                // Add search
                if (!String.IsNullOrEmpty(c.SearchPhrase))
                {
                    var searchPhrase = c.SearchPhrase;
                    if (isFuzzySearch)
                    {

                        var keywords = c.SearchPhrase.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        searchPhrase = string.Empty;
                        searchPhrase = keywords.Aggregate(
                            searchPhrase,
                            (current, keyword) =>
                                current + String.Format("{0}~{1}", keyword.Replace("~", ""), fuzzyMinSimilarity.ToString(CultureInfo.InvariantCulture)));
                    }

                    var fields = new List<string> { "__content" };
                    if (c.Locale != null)
                    {
                        var contentField = string.Format("__content_{0}", c.Locale.ToLower());
                        fields.Add(contentField);
                    }

                    var parser = new MultiFieldQueryParser(u.Version.LUCENE_30, fields.ToArray(), analyzer)
                    {
                        DefaultOperator = QueryParser.Operator.OR
                    };

                    var searchQuery = parser.Parse(searchPhrase);
                    query.Add(searchQuery, Occur.MUST);
                }
            }

            return queryBuilder;
        }



        /// <summary>
        ///     Adds the query.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="query">The query.</param>
        /// <param name="filter">The filter.</param>
        protected void AddQuery(string fieldName, BooleanQuery query, StringCollection filter)
        {
            fieldName = fieldName.ToLower();
            if (filter.Count > 0)
            {
                if (filter.Count != 1)
                {
                    var booleanQuery = new BooleanQuery();
                    var containsFilter = false;
                    foreach (var index in filter)
                    {
                        if (String.IsNullOrEmpty(index))
                        {
                            continue;
                        }

                        var nodeQuery = new WildcardQuery(new Term(fieldName, index));
                        booleanQuery.Add(nodeQuery, Occur.SHOULD);
                        containsFilter = true;
                    }
                    if (containsFilter)
                    {
                        query.Add(booleanQuery, Occur.MUST);
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(filter[0]))
                    {
                        this.AddQuery(fieldName, query, filter[0].ToLower());
                    }
                }
            }
        }

        /// <summary>
        ///     Adds the query.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="query">The query.</param>
        /// <param name="filter">The filter.</param>
        protected void AddQuery(string fieldName, BooleanQuery query, string filter)
        {
            fieldName = fieldName.ToLower();
            var nodeQuery = new WildcardQuery(new Term(fieldName, filter.ToLower()));
            query.Add(nodeQuery, Occur.MUST);
        }


        /// <summary>
        ///     Closes the specified documentType.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="documentType">The documentType.</param>
        /// <param name="optimize">
        ///     if set to <c>true</c> [optimize].
        /// </param>
        private void Close(string scope, string documentType, bool optimize)
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

                    writer.Dispose();
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
        private string GetFolderName(string scope, string documentType)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1}", scope, documentType);
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
                        new StandardAnalyzer(u.Version.LUCENE_30),
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

    public class QueryBuilder
    {
        public Query Query { get; set; }

        public Filter Filter { get; set; }

        #region Overrides of Object

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var ret = new StringBuilder();
            if (this.Query != null)
                ret.AppendFormat("query:{0}", this.Query.ToString());

            if (this.Filter != null)
                ret.AppendFormat("filter:{0}", this.Filter.ToString());

            return ret.ToString();
        }

        #endregion
    }
}
