using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using GeneralSettings = VirtoCommerce.SearchModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.SearchModule.Data.Services
{
    /// <summary>
    /// Implement the functionality of indexing
    /// </summary>
    public class IndexingManager : IIndexingManager
    {
        public const int DefaultBatchSize = 50;

        private readonly ISearchProvider _searchProvider;
        private readonly IEnumerable<IndexDocumentConfiguration> _configurations;
        private readonly SearchOptions _searchOptions;
        private readonly ISettingsManager _settingsManager;
        private readonly IEnumerable<IIndexDocumentConverter> _documentConverters;

        private bool PartialDocumentUpdateEnabled
        {
            get
            {
                return _settingsManager.GetValue<bool>(GeneralSettings.EnablePartialDocumentUpdate);
            }
        }

        public IndexingManager(
            ISearchProvider searchProvider,
            IEnumerable<IndexDocumentConfiguration> configurations,
            IOptions<SearchOptions> searchOptions,
            ISettingsManager settingsManager,
            IEnumerable<IIndexDocumentConverter> documentConverters)
        {
            _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _searchOptions = searchOptions.Value;
            _settingsManager = settingsManager;
            _documentConverters = documentConverters;
        }

        public virtual async Task<IndexState> GetIndexStateAsync(string documentType)
        {
            var result = await GetIndexStateAsync(documentType, getBackupIndexState: false);

            return result;
        }

        public virtual async Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType)
        {
            var result = new List<IndexState> { await GetIndexStateAsync(documentType, getBackupIndexState: false) };

            if (_searchProvider.Is<ISupportIndexSwap>(documentType))
            {
                result.Add(await GetIndexStateAsync(documentType, getBackupIndexState: true));
            }

            return result;
        }

        public virtual Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            return IndexAsync(options, progressCallback, cancellationToken);
        }

        public virtual Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            return IndexAsync(options, progressCallback, cancellationToken);
        }

        public virtual async Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            ValidateOptions(options);

            if (GetConfiguration(options.DocumentType, out var configuration))
            {
                await ProcessConfigurationAsync(configuration, options, progressCallback, cancellationToken);
            }
        }

        public virtual async Task<IndexingResult> IndexDocumentsAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes = null)
        {
            // TODO: Reuse general index API?

            if (!GetConfiguration(documentType, out var configuration))
            {
                return new IndexingResult();
            }

            var partialUpdate = false;
            var builderTypesList = (builderTypes as IList<string> ?? builderTypes?.ToList()) ?? Array.Empty<string>();
            var primaryDocumentBuilder = configuration.DocumentSource.DocumentBuilder;

            var additionalDocumentBuilders = configuration.RelatedSources
                ?.Where(s => s.DocumentBuilder != null)
                .Select(s => s.DocumentBuilder)
                .ToList() ?? new List<IIndexDocumentBuilder>();

            if (builderTypesList.Any() && additionalDocumentBuilders.Any() && _searchProvider.Is<ISupportPartialUpdate>(documentType) && PartialDocumentUpdateEnabled)
            {
                partialUpdate = true;
                additionalDocumentBuilders = additionalDocumentBuilders.Where(x => builderTypesList.Contains(x.GetType().FullName)).ToList();

                // In case of changing main object itself, there would be only primary document builder,
                // but in the other cases, when changed additional dependent objects, primary builder should be set to null.
                if (!builderTypesList.Contains(primaryDocumentBuilder.GetType().FullName))
                {
                    primaryDocumentBuilder = null;
                }
            }

            var cancellationToken = new CancellationTokenWrapper(CancellationToken.None);
            var documents = await GetDocumentsAsync(documentType, documentIds, primaryDocumentBuilder, additionalDocumentBuilders, cancellationToken);

            var result = partialUpdate && _searchProvider.Is<ISupportPartialUpdate>(documentType, out var supportPartialUpdateProvider)
                ? await supportPartialUpdateProvider.IndexPartialAsync(documentType, documents)
                : await _searchProvider.IndexAsync(documentType, documents);

            return result;
        }

        public virtual Task<IndexingResult> DeleteDocumentsAsync(string documentType, string[] documentIds)
        {
            return DeleteDocumentsAsync(documentType, documentIds as IList<string>);
        }

        public virtual async Task<IndexingResult> DeleteDocumentsAsync(string documentType, IList<string> documentIds)
        {
            var documents = documentIds.Select(id => new IndexDocument(id)).ToList();
            return await _searchProvider.RemoveAsync(documentType, documents);
        }

        protected virtual async Task ProcessConfigurationAsync(
            IndexDocumentConfiguration configuration,
            IndexingOptions options,
            Action<IndexingProgress> progressCallback,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await PrepareIndexAsync(options, progressCallback, cancellationToken);

            var documentType = options.DocumentType;
            var processedCount = 0L;
            long? totalCount = null;

            void Progress(string message = null, IList<string> errors = null)
            {
                ReportProgress(progressCallback, documentType, message, processedCount, totalCount, errors);
            }

            Progress("calculating total count");

            var batchOptions = new BatchIndexingOptions
            {
                DocumentType = documentType,
                Reindex = options.DeleteExistingIndex,
                PrimaryDocumentBuilder = configuration.DocumentSource.DocumentBuilder,
                SecondaryDocumentBuilders = configuration.RelatedSources
                    ?.Where(s => s.DocumentBuilder != null)
                    .Select(s => s.DocumentBuilder)
                    .ToList(),
            };

            var feeds = await GetChangeFeedsAsync(configuration, options);

            // Try to get total count to indicate progress. Some feeds don't have a total count.
            totalCount = feeds.Any(x => x.TotalCount == null)
                ? null
                : feeds.Sum(x => x.TotalCount ?? 0);

            var changes = await GetNextChangesAsync(feeds);
            while (changes.Any())
            {
                var indexingResult = await ProcessChangesAsync(changes, batchOptions, cancellationToken);

                processedCount += changes.Count;
                Progress(errors: GetIndexingErrors(indexingResult));

                cancellationToken.ThrowIfCancellationRequested();

                changes = await GetNextChangesAsync(feeds);
            }

            // indexation complete, swap indexes back
            await SwapIndicesAsync(options);

            totalCount ??= processedCount;
            Progress("indexation finished");
        }

        protected virtual async Task<IList<IndexDocumentChange>> GetNextChangesAsync(IList<IIndexDocumentChangeFeed> feeds)
        {
            var batches = await Task.WhenAll(feeds.Select(f => f.GetNextBatch()));

            var changes = batches
                .Where(b => b != null)
                .SelectMany(b => b)
                .ToList();

            return changes;
        }

        protected virtual async Task<IndexingResult> ProcessChangesAsync(
            IList<IndexDocumentChange> changes,
            BatchIndexingOptions batchOptions,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new IndexingResult();
            var documentType = batchOptions.DocumentType;

            // Full changes don't have changes provider specified because we don't set it for manual indexation.
            var fullChanges = _searchProvider.Is<ISupportPartialUpdate>(documentType) && PartialDocumentUpdateEnabled
                ? changes
                    .Where(x =>
                        x.ChangeType is IndexDocumentChangeType.Deleted or IndexDocumentChangeType.Created ||
                        GetDocumentBuilders(documentType, x.Provider).IsNullOrEmpty())
                    .ToList()
                : changes;

            var partialChanges = changes.Except(fullChanges).ToList();
            var partialResult = await ProcessPartialDocumentsAsync(partialChanges, batchOptions, cancellationToken);

            var groups = GetLatestChangesForEachDocumentGroupedByChangeType(fullChanges);

            foreach (var (changeType, documentIds) in groups)
            {
                var groupResult = await ProcessDocumentsAsync(changeType, documentIds, batchOptions, cancellationToken);

                if (groupResult?.Items != null)
                {
                    result.Items.AddRange(groupResult.Items);
                }
            }

            result.Items.AddRange(partialResult.Items);

            return result;
        }

        protected virtual async Task<IndexingResult> ProcessPartialDocumentsAsync(
            IList<IndexDocumentChange> changes,
            BatchIndexingOptions batchOptions,
            ICancellationToken cancellationToken)
        {
            var result = new IndexingResult();
            var documentType = batchOptions.DocumentType;

            if (!PartialDocumentUpdateEnabled || !_searchProvider.Is<ISupportPartialUpdate>(documentType, out var supportPartialUpdateProvider))
            {
                return result;
            }

            var documentIds = changes.Select(x => x.DocumentId).Distinct();

            foreach (var id in documentIds)
            {
                var builders = changes
                    .Where(x => x.DocumentId == id)
                    .SelectMany(x => GetDocumentBuilders(documentType, x.Provider))
                    .Distinct()
                    .ToList();

                var documents = await GetDocumentsAsync(batchOptions.DocumentType, [id], null, builders, cancellationToken);
                var indexingResult = await supportPartialUpdateProvider.IndexPartialAsync(documentType, documents);

                result.Items.AddRange(indexingResult.Items);
            }

            return result;
        }

        protected virtual async Task<IndexingResult> ProcessDocumentsAsync(
            IndexDocumentChangeType changeType,
            IList<string> documentIds,
            BatchIndexingOptions batchOptions,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IndexingResult result;
            var documentType = batchOptions.DocumentType;

            switch (changeType)
            {
                case IndexDocumentChangeType.Deleted:
                    result = await DeleteDocumentsAsync(documentType, documentIds);
                    break;
                case IndexDocumentChangeType.Modified or IndexDocumentChangeType.Created:
                    var documents = await GetDocumentsAsync(batchOptions.DocumentType, documentIds, batchOptions.PrimaryDocumentBuilder, batchOptions.SecondaryDocumentBuilders, cancellationToken);

                    if (documents.IsNullOrEmpty())
                    {
                        result = new IndexingResult();
                    }
                    else
                    {
                        result = batchOptions.Reindex &&
                                 _searchProvider.Is<ISupportIndexSwap>(documentType, out var supportIndexSwapProvider)
                            ? await supportIndexSwapProvider.IndexWithBackupAsync(documentType, documents)
                            : await _searchProvider.IndexAsync(documentType, documents);
                    }

                    break;
                default:
                    result = new IndexingResult();
                    break;
            }

            return result;
        }

        protected virtual async Task<IList<IIndexDocumentChangeFeed>> GetChangeFeedsAsync(IndexDocumentConfiguration configuration, IndexingOptions options)
        {
            // Return in-memory change feed for specific set of document IDs.
            if (options.DocumentIds != null)
            {
                return
                [
                    new InMemoryIndexDocumentChangeFeed(options.DocumentIds.ToArray(), IndexDocumentChangeType.Modified, options.BatchSize ?? DefaultBatchSize),
                ];
            }

            var factories = new List<IIndexDocumentChangeFeedFactory>
            {
                GetChangeFeedFactory(configuration.DocumentSource)
            };

            // In case of 'full' re-index we don't want to include the related sources,
            // because that would double the indexation work.
            // E.g. All products would get indexed for the primary document source
            // and then all products would get re-indexed for all the prices as well.
            if (configuration.RelatedSources != null && (options.StartDate != null || options.EndDate != null))
            {
                factories.AddRange(configuration.RelatedSources.Select(GetChangeFeedFactory));
            }

            return await Task.WhenAll(factories.Select(x => x.CreateFeed(options.StartDate, options.EndDate, options.BatchSize ?? DefaultBatchSize)));
        }

        protected virtual IIndexDocumentChangeFeedFactory GetChangeFeedFactory(IndexDocumentSource documentSource)
        {
            documentSource.ChangeFeedFactory ??= new IndexDocumentChangeFeedFactoryAdapter(documentSource.ChangesProvider);

            return documentSource.ChangeFeedFactory;
        }

        protected virtual IList<string> GetIndexingErrors(IndexingResult indexingResult)
        {
            var errors = indexingResult?.Items
                ?.Where(i => !i.Succeeded)
                .Select(i => $"{FormatId(i.Id)}, Error: {i.ErrorMessage}")
                .ToArray();

            return errors ?? [];
        }

        protected virtual string FormatId(string id)
        {
            return id?.Contains(':') == true
                ? id
                : $"ID: {id}";
        }

        protected virtual IDictionary<IndexDocumentChangeType, string[]> GetLatestChangesForEachDocumentGroupedByChangeType(IList<IndexDocumentChange> changes)
        {
            var result = changes
                .GroupBy(c => c.DocumentId)
                .Select(g => g.OrderByDescending(o => o.ChangeDate).First())
                .GroupBy(c => c.ChangeType)
                .ToDictionary(g => g.Key, g => g.Select(c => c.DocumentId).ToArray());

            return result;
        }

        protected virtual async Task<IList<IndexDocument>> GetDocumentsAsync(
            string documentType,
            IList<string> documentIds,
            IIndexDocumentBuilder primaryDocumentBuilder,
            IList<IIndexDocumentBuilder> secondaryDocumentBuilders,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var primaryDocuments = primaryDocumentBuilder != null
                ? (await RunPrimaryDocumentBuilder(primaryDocumentBuilder, documentIds))?.Where(x => x != null).ToList()
                : documentIds.Select(x => new IndexDocument(x)).ToList();

            if (primaryDocuments?.Count > 0)
            {
                if (secondaryDocumentBuilders != null)
                {
                    var aggregationKeys = primaryDocuments.ToDictionary(x => x.Id, x => (IHasAggregationKey)x);
                    var secondaryDocuments = await GetSecondaryDocumentsAsync(secondaryDocumentBuilders, aggregationKeys, cancellationToken);

                    MergeDocuments(primaryDocuments, secondaryDocuments);

                    //TODO: discuss
                    if (primaryDocumentBuilder is IIndexDocumentAggregator)
                    {
                        RunDocumentAggregator((IIndexDocumentAggregator)primaryDocumentBuilder, primaryDocuments);
                    }
                }

                foreach (var document in primaryDocuments)
                {
                    AddSystemFields(document);
                }

                foreach (var converter in _documentConverters)
                {
                    await converter.ConvertAsync(documentType, primaryDocuments);
                }
            }

            return primaryDocuments;
        }

        protected virtual async Task<IList<IndexDocument>> GetSecondaryDocumentsAsync(
            IList<IIndexDocumentBuilder> secondaryDocumentBuilders,
            IDictionary<string, IHasAggregationKey> aggregationKeys,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tasks = secondaryDocumentBuilders.Select(p => RunSecondaryDocumentBuilder(p, aggregationKeys));
            var results = await Task.WhenAll(tasks);

            var result = results
                .Where(r => r != null)
                .SelectMany(r => r.Where(d => d != null))
                .ToList();

            return result;
        }

        protected async Task<IList<IndexDocument>> RunPrimaryDocumentBuilder(IIndexDocumentBuilder documentBuilder, IList<string> documentIds)
        {
            var documents = await documentBuilder.GetDocumentsAsync(documentIds);

            if (documentBuilder is IIndexDocumentAggregationKeyProvider)
            {
                ((IIndexDocumentAggregationKeyProvider)documentBuilder).SetAggregationKeys(documents);
            }

            if (documentBuilder is IIndexDocumentAggregator)
            {
                RunDocumentAggregator((IIndexDocumentAggregator)documentBuilder, documents);
            }

            return documents;
        }

        protected async Task<IList<IndexDocument>> RunSecondaryDocumentBuilder(IIndexDocumentBuilder documentBuilder, IDictionary<string, IHasAggregationKey> aggregationKeys)
        {
            var documents = await documentBuilder.GetDocumentsAsync(aggregationKeys.Keys.ToList());

            if (documentBuilder is IIndexDocumentAggregator)
            {
                EnsureAggregationInfo(documents, aggregationKeys);
                RunDocumentAggregator((IIndexDocumentAggregator)documentBuilder, documents);
            }

            return documents;
        }

        protected void EnsureAggregationInfo(IList<IndexDocument> documents, IDictionary<string, IHasAggregationKey> aggregationKeys)
        {
            foreach (var document in documents.Where(x => x.AggregationKey.IsNullOrEmpty()))
            {
                if (aggregationKeys.TryGetValue(document.Id, out var aggregationInfo))
                {
                    document.AggregationKey = aggregationInfo.AggregationKey;
                }
            }
        }

        protected void RunDocumentAggregator(IIndexDocumentAggregator documentAggregator, IList<IndexDocument> documents)
        {
            var documentGroups = documents.Where(x => !x.AggregationKey.IsNullOrEmpty()).GroupBy(x => x.AggregationKey);

            foreach (var documentGroup in documentGroups)
            {
                var aggregationDocument = documentGroup.FirstOrDefault(x => x.Id == x.AggregationKey);

                if (aggregationDocument != null)
                {
                    documentAggregator.AggregateDocuments(aggregationDocument, documentGroup.ToList());
                }
            }
        }

        protected virtual void MergeDocuments(IList<IndexDocument> primaryDocuments, IList<IndexDocument> secondaryDocuments)
        {
            if (primaryDocuments.IsNullOrEmpty() || secondaryDocuments.IsNullOrEmpty())
            {
                return;
            }

            var secondaryDocumentGroups = secondaryDocuments
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g, StringComparer.OrdinalIgnoreCase);

            foreach (var primaryDocument in primaryDocuments)
            {
                if (secondaryDocumentGroups.TryGetValue(primaryDocument.Id, out var secondaryDocumentGroup))
                {
                    foreach (var secondaryDocument in secondaryDocumentGroup)
                    {
                        primaryDocument.Merge(secondaryDocument);
                    }
                }
            }
        }

        /// <summary>
        /// Swap between active and backup indices, if supported
        /// </summary>
        protected virtual async Task SwapIndicesAsync(IndexingOptions options)
        {
            var documentType = options.DocumentType;

            if (options.DeleteExistingIndex && _searchProvider.Is<ISupportIndexSwap>(documentType, out var swappingSupportedSearchProvider))
            {
                await swappingSupportedSearchProvider.SwapIndexAsync(documentType);
            }
        }

        private async Task<IndexState> GetIndexStateAsync(string documentType, bool getBackupIndexState)
        {
            var result = new IndexState
            {
                DocumentType = documentType,
                Provider = _searchProvider.GetProviderName(documentType, _searchOptions.Provider),
                Scope = _searchOptions.GetScope(documentType),
                IsActive = !getBackupIndexState,
            };

            var searchRequest = new SearchRequest
            {
                UseBackupIndex = getBackupIndexState,
                Sorting = [new SortingField { FieldName = KnownDocumentFields.IndexationDate, IsDescending = true }],
                Take = 1,
            };

            try
            {
                var searchResponse = await _searchProvider.SearchAsync(documentType, searchRequest);

                result.IndexedDocumentsCount = searchResponse.TotalCount;
                if (searchResponse.Documents?.Any() == true)
                {
                    var indexationDate = searchResponse.Documents[0].FirstOrDefault(kvp => kvp.Key.EqualsInvariant(KnownDocumentFields.IndexationDate));
                    if (DateTimeOffset.TryParse(indexationDate.Value.ToString(), out var lastIndexationDateTime))
                    {
                        result.LastIndexationDate = lastIndexationDateTime.DateTime;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return result;
        }

        protected virtual async Task PrepareIndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            if (options.DeleteExistingIndex)
            {
                await DeleteIndexAsync(options, progressCallback, cancellationToken);
                await CreateIndexAsync(options, progressCallback, cancellationToken);
            }
        }

        protected virtual async Task DeleteIndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            var documentType = options.DocumentType;
            ReportProgress(progressCallback, documentType, "deleting index");

            await _searchProvider.DeleteIndexAsync(documentType);
            // TODO: Wait until index is deleted
        }

        protected virtual async Task CreateIndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            var documentType = options.DocumentType;
            ReportProgress(progressCallback, documentType, "creating index");

            var schema = await BuildSchemaAsync(documentType);

            if (_searchProvider.Is<ISupportIndexCreate>(documentType, out var supportIndexCreate))
            {
                await supportIndexCreate.CreateIndexAsync(documentType, schema);
            }
            else
            {
                var documents = new[] { schema };

                if (_searchProvider.Is<ISupportIndexSwap>(documentType, out var supportIndexSwapProvider))
                {
                    await supportIndexSwapProvider.IndexWithBackupAsync(documentType, documents);
                }
                else
                {
                    await _searchProvider.IndexAsync(documentType, documents);
                }

                await _searchProvider.RemoveAsync(documentType, documents);
            }
        }

        protected virtual async Task<IndexDocument> BuildSchemaAsync(string documentType)
        {
            var temporaryDocumentId = Guid.NewGuid().ToString("N");
            var schema = new IndexDocument(temporaryDocumentId);

            AddSystemFields(schema);

            foreach (var schemaBuilder in GetSchemaBuilders(documentType))
            {
                await schemaBuilder.BuildSchemaAsync(schema);
            }

            return schema;
        }

        protected virtual void AddSystemFields(IndexDocument document)
        {
            document.AddFilterableDateTime(KnownDocumentFields.IndexationDate, DateTime.UtcNow);
        }

        protected virtual IEnumerable<IIndexSchemaBuilder> GetSchemaBuilders(string documentType)
        {
            return _configurations.GetDocumentSources(documentType)
                .Select(x => x.DocumentBuilder)
                .OfType<IIndexSchemaBuilder>();
        }

        protected virtual IEnumerable<IIndexDocumentBuilder> GetDocumentBuilders(string documentType, IIndexDocumentChangesProvider provider)
        {
            return _configurations.GetDocumentBuilders(documentType, provider?.GetType());
        }

        protected virtual bool GetConfiguration(string documentType, out IndexDocumentConfiguration configuration)
        {
            return _configurations.GetConfiguration(documentType, out configuration);
        }

        protected virtual void ValidateOptions(IndexingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.DocumentType))
            {
                throw new ArgumentException($"{nameof(options.DocumentType)} is empty", nameof(options));
            }

            options.BatchSize ??= _settingsManager?.GetValue<int>(GeneralSettings.IndexPartitionSize) ?? DefaultBatchSize;

            if (options.BatchSize < 1)
            {
                throw new ArgumentException($"{nameof(options.BatchSize)} {options.BatchSize} is less than 1", nameof(options));
            }
        }

        protected virtual void ReportProgress(Action<IndexingProgress> progressCallback, string documentType, string message)
        {
            ReportProgress(progressCallback, documentType, message, processedCount: 0L, totalCount: null, errors: null);
        }

        protected virtual void ReportProgress(
            Action<IndexingProgress> progressCallback,
            string documentType,
            string message,
            long processedCount,
            long? totalCount,
            IList<string> errors)
        {
            if (progressCallback == null)
            {
                return;
            }

            string description;

            if (message != null)
            {
                description = $"{documentType}: {message}";
            }
            else
            {
                description = totalCount != null
                    ? $"{documentType}: {processedCount} of {totalCount} have been indexed"
                    : $"{documentType}: {processedCount} have been indexed";
            }

            progressCallback.Invoke(new IndexingProgress(description, documentType, totalCount, processedCount, errors));
        }
    }
}
