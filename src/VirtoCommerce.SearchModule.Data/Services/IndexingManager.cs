using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    /// <summary>
    /// Implement the functionality of indexing
    /// </summary>
    public class IndexingManager : IndexingManagerBase, IIndexingManager
    {
        private readonly ISearchProvider _searchProvider;
        private readonly IEnumerable<IndexDocumentConfiguration> _configurations;
        private readonly IIndexingWorker _backgroundWorker;
        private readonly SearchOptions _searchOptions;

        public IndexingManager(
            ISearchProvider searchProvider,
            IEnumerable<IndexDocumentConfiguration> configurations,
            IOptions<SearchOptions> searchOptions,
            ISettingsManager settingsManager = null,
            IIndexingWorker backgroundWorker = null)
            : base(searchProvider, configurations, settingsManager)
        {
            _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _searchOptions = searchOptions.Value;
            _backgroundWorker = backgroundWorker;
        }

        public virtual async Task<IndexState> GetIndexStateAsync(string documentType)
        {
            var result = await GetIndexStateAsync(documentType, getBackupIndexState: false);

            return result;
        }

        public virtual async Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType)
        {
            var result = new List<IndexState> { await GetIndexStateAsync(documentType, getBackupIndexState: false) };

            if (_searchProvider is ISupportIndexSwap)
            {
                result.Add(await GetIndexStateAsync(documentType, getBackupIndexState: true));
            }

            return result;
        }

        public virtual async Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            ValidateOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            var documentType = options.DocumentType;

            // each Search Engine implementation has its own way of handing index rebuild
            if (options.DeleteExistingIndex)
            {
                progressCallback?.Invoke(new IndexingProgress($"{documentType}: deleting index", documentType));
                await _searchProvider.DeleteIndexAsync(documentType);
            }

            if (GetConfiguration(documentType, out var configuration))
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

            if (builderTypesList.Any() && additionalDocumentBuilders.Any() && _searchProvider is ISupportPartialUpdate)
            {
                partialUpdate = true;
                additionalDocumentBuilders = additionalDocumentBuilders.Where(x => builderTypesList.Contains(x.GetType().FullName)).ToList();

                // In case of changing main object itself, there would be only primary document builder,
                // but in the other cases, when changed additional dependent objects, primary builder should be nulled.
                if (!builderTypesList.Contains(primaryDocumentBuilder.GetType().FullName))
                {
                    primaryDocumentBuilder = null;
                }
            }

            var cancellationToken = new CancellationTokenWrapper(CancellationToken.None);
            var documents = await GetDocumentsAsync(documentIds, primaryDocumentBuilder, additionalDocumentBuilders, cancellationToken);

            var result = partialUpdate && _searchProvider is ISupportPartialUpdate supportPartialUpdateProvider
                ? await supportPartialUpdateProvider.IndexPartialAsync(documentType, documents)
                : await _searchProvider.IndexAsync(documentType, documents);

            return result;
        }

        public virtual Task<IndexingResult> DeleteDocumentsAsync(string documentType, string[] documentIds)
        {
            return base.DeleteDocuments(documentType, documentIds);
        }

        protected virtual async Task ProcessConfigurationAsync(
            IndexDocumentConfiguration configuration,
            IndexingOptions options,
            Action<IndexingProgress> progressCallback,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documentType = options.DocumentType;

            progressCallback?.Invoke(new IndexingProgress($"{documentType}: calculating total count", documentType));

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

            var feeds = await GetChangeFeeds(configuration, options);

            // Try to get total count to indicate progress. Some feeds don't have a total count.
            var totalCount = feeds.Any(x => x.TotalCount == null)
                ? (long?)null
                : feeds.Sum(x => x.TotalCount ?? 0);

            long processedCount = 0;

            var changes = await GetNextChangesAsync(feeds);
            while (changes.Any())
            {
                IList<string> errors = null;

                if (_backgroundWorker == null)
                {
                    var indexingResult = await ProcessChangesAsync(changes, batchOptions, cancellationToken);
                    errors = GetIndexingErrors(indexingResult);
                }
                else
                {
                    // We're executing a job to index all documents or the changes since a specific time.
                    // Priority for this indexation work should be quite low.
                    var documentIds = changes
                        .Select(x => x.DocumentId)
                        .Distinct()
                        .ToArray();

                    _backgroundWorker.IndexDocuments(documentType, documentIds, IndexingPriority.Background);
                }

                processedCount += changes.Count;

                var description = totalCount != null
                    ? $"{documentType}: {processedCount} of {totalCount} have been indexed"
                    : $"{documentType}: {processedCount} have been indexed";

                progressCallback?.Invoke(new IndexingProgress(description, documentType, totalCount, processedCount, errors));

                cancellationToken.ThrowIfCancellationRequested();

                changes = await GetNextChangesAsync(feeds);
            }

            // indexation complete, swap indexes back
            await SwapIndices(options);

            progressCallback?.Invoke(new IndexingProgress($"{documentType}: indexation finished", documentType, totalCount ?? processedCount, processedCount));
        }

        protected virtual async Task<IndexingResult> ProcessChangesAsync(
            IEnumerable<IndexDocumentChange> changes,
            BatchIndexingOptions batchOptions,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new IndexingResult();

            var changesList = changes as IList<IndexDocumentChange> ?? changes.ToList();

            // Full changes don't have changes provider specified because we don't set it for manual indexation.
            var fullChanges = _searchProvider is ISupportPartialUpdate
                ? changesList
                    .Where(x =>
                        x.ChangeType is IndexDocumentChangeType.Deleted or IndexDocumentChangeType.Created ||
                        !_configurations.GetBuildersForProvider(x.Provider?.GetType()).Any())
                    .ToList()
                : changesList;

            var partialChanges = changesList.Except(fullChanges);
            var partialResult = await ProcessPartialDocumentsAsync(partialChanges, batchOptions, cancellationToken);

            var groups = GetLatestChangesForEachDocumentGroupedByChangeType(fullChanges);

            foreach (var (changeType, changedIds) in groups)
            {
                var groupResult = await ProcessDocumentsAsync(changeType, changedIds, batchOptions, cancellationToken);

                if (groupResult?.Items != null)
                {
                    result.Items.AddRange(groupResult.Items);
                }
            }

            result.Items.AddRange(partialResult.Items);

            return result;
        }

        protected virtual async Task<IndexingResult> ProcessPartialDocumentsAsync(
            IEnumerable<IndexDocumentChange> changes,
            BatchIndexingOptions batchOptions,
            ICancellationToken cancellationToken)
        {
            var result = new IndexingResult();

            var changesList = changes as IList<IndexDocumentChange> ?? changes.ToList();
            var changeIds = changesList.Select(x => x.DocumentId).Distinct();

            foreach (var id in changeIds)
            {
                var builders = changesList
                    .Where(x => x.DocumentId == id)
                    .SelectMany(x => _configurations.GetBuildersForProvider(x.Provider.GetType()));

                var documents = await GetDocumentsAsync(new[] { id }, null, builders, cancellationToken);
                var indexingResult = await ((ISupportPartialUpdate)_searchProvider).IndexPartialAsync(batchOptions.DocumentType, documents);

                result.Items.AddRange(indexingResult.Items);
            }

            return result;
        }

        protected virtual Task<IndexingResult> ProcessDocumentsAsync(
            IndexDocumentChangeType changeType,
            string[] changedIds,
            BatchIndexingOptions batchOptions,
            ICancellationToken cancellationToken)
        {
            return base.ProcessDocuments(changeType, changedIds, batchOptions, cancellationToken);
        }

        protected virtual IDictionary<IndexDocumentChangeType, string[]> GetLatestChangesForEachDocumentGroupedByChangeType(IEnumerable<IndexDocumentChange> changes)
        {
            var result = changes
                .GroupBy(c => c.DocumentId)
                .Select(g => g.OrderByDescending(o => o.ChangeDate).First())
                .GroupBy(c => c.ChangeType)
                .ToDictionary(g => g.Key, g => g.Select(c => c.DocumentId).ToArray());

            return result;
        }

        protected virtual Task<IList<IndexDocument>> GetDocumentsAsync(
            IList<string> documentIds,
            IIndexDocumentBuilder primaryDocumentBuilder,
            IEnumerable<IIndexDocumentBuilder> additionalDocumentBuilders,
            ICancellationToken cancellationToken)
        {
            return base.GetDocuments(documentIds, primaryDocumentBuilder, additionalDocumentBuilders.ToList(), cancellationToken);
        }

        protected virtual Task<IList<IndexDocument>> GetSecondaryDocumentsAsync(
            IEnumerable<IIndexDocumentBuilder> secondaryDocumentBuilders,
            IList<string> documentIds,
            ICancellationToken cancellationToken)
        {
            return base.GetSecondaryDocuments(secondaryDocumentBuilders.ToList(), documentIds, cancellationToken);
        }

        private async Task<IndexState> GetIndexStateAsync(string documentType, bool getBackupIndexState)
        {
            var result = new IndexState
            {
                DocumentType = documentType,
                Provider = _searchOptions.Provider,
                Scope = _searchOptions.GetScope(documentType),
                IsActive = !getBackupIndexState,
            };

            var searchRequest = new SearchRequest
            {
                UseBackupIndex = getBackupIndexState,
                Sorting = new[] { new SortingField { FieldName = KnownDocumentFields.IndexationDate, IsDescending = true } },
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
    }
}
