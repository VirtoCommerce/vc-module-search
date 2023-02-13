using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class ScalableIndexingManager : IndexingManagerBase, IScalableIndexingManager
{
    private readonly IIndexQueue _indexQueue;
    private readonly ISearchProvider _searchProvider;

    public ScalableIndexingManager(
        ISearchProvider searchProvider,
        IEnumerable<IndexDocumentConfiguration> configurations,
        ISettingsManager settingsManager,
        IIndexQueue indexQueue)
        : base(searchProvider, configurations, settingsManager)
    {
        _indexQueue = indexQueue;
        _searchProvider = searchProvider;
    }

    public virtual async Task IndexAllDocuments(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
    {
        ValidateOptions(options);

        var documentType = options.DocumentType;
        var processedCount = 0L;
        long? totalCount = null;

        void Progress(string message = null, IList<string> errors = null)
        {
            ReportProgress(progressCallback, documentType, message, processedCount, totalCount, errors);
        }

        Progress("Preparing index");
        await PrepareIndex(options, progressCallback, cancellationToken);

        Progress("Calculating total count");
        totalCount = 0L;
        var queueId = await _indexQueue.NewQueue(options);

        await foreach (var documentIds in EnumerateAllDocumentIds(options, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalCount += documentIds.Count;
            options.DocumentIds = documentIds;
            await _indexQueue.Enqueue(queueId, options);
        }

        Progress();
        await _indexQueue.Wait(queueId, cancellationToken, (batchOptions, batchResult) =>
        {
            processedCount += batchOptions?.DocumentIds?.Count ?? 0L;
            Progress(errors: GetIndexingErrors(batchResult));
        });

        await SwapIndices(options);

        Progress("Indexation finished");
    }

    public virtual async Task<IndexingResult> IndexDocuments(IndexingOptions options, ICancellationToken cancellationToken)
    {
        ValidateOptions(options);

        if (!GetConfiguration(options.DocumentType, out var configuration))
        {
            return new IndexingResult();
        }

        var batchOptions = new BatchIndexingOptions
        {
            DocumentType = options.DocumentType,
            Reindex = options.DeleteExistingIndex,
            PrimaryDocumentBuilder = configuration.DocumentSource.DocumentBuilder,
            SecondaryDocumentBuilders = configuration.RelatedSources
                ?.Where(x => x.DocumentBuilder != null)
                .Select(x => x.DocumentBuilder)
                .ToList(),
        };

        var result = await ProcessDocuments(IndexDocumentChangeType.Created, options.DocumentIds, batchOptions, cancellationToken);

        return result;
    }


    protected virtual async Task PrepareIndex(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
    {
        await DeleteIndex(options, progressCallback, cancellationToken);
        await CreateIndex(options, progressCallback, cancellationToken);
    }

    protected virtual async Task DeleteIndex(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
    {
        if (options.DeleteExistingIndex)
        {
            var documentType = options.DocumentType;
            ReportProgress(progressCallback, documentType, "Deleting index");
            await _searchProvider.DeleteIndexAsync(documentType);
            // TODO: Wait until index is deleted
        }
    }

    protected virtual async Task<IndexingResult> CreateIndex(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
    {
        var documentType = options.DocumentType;
        ReportProgress(progressCallback, documentType, "Creating index");

        var temporaryDocumentId = Guid.NewGuid().ToString("N");
        var document = new IndexDocument(temporaryDocumentId);
        var documents = new[] { document };

        AddSystemFields(documents);
        // TODO: Define other fields

        var result = options.DeleteExistingIndex && _searchProvider is ISupportIndexSwap supportIndexSwapProvider
            ? await supportIndexSwapProvider.IndexWithBackupAsync(documentType, documents)
            : await _searchProvider.IndexAsync(documentType, documents);

        await _searchProvider.RemoveAsync(documentType, documents);

        return result;
    }

    protected virtual async IAsyncEnumerable<IList<string>> EnumerateAllDocumentIds(IndexingOptions options, ICancellationToken cancellationToken)
    {
        if (!GetConfiguration(options.DocumentType, out var configuration))
        {
            yield break;
        }

        var feeds = await GetChangeFeeds(configuration, options);

        // For full indexation there should be only one feed
        Debug.Assert(feeds.Count == 1);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var changes = await GetNextChangesAsync(feeds);

            if (changes.IsNullOrEmpty())
            {
                yield break;
            }

            yield return changes.Select(x => x.DocumentId).ToList();
        }
    }
}
