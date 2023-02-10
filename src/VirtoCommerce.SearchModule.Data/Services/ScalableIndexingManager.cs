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
        long? totalCount = null;
        long? processedCount = null;
        var queueId = await _indexQueue.NewQueue(options);

        void ReportProgress(string message = null, IList<string> errors = null)
        {
            var description = message != null
                ? $"{documentType}: {message}"
                : $"{documentType}: {processedCount} of {totalCount} have been indexed";

            progressCallback?.Invoke(new IndexingProgress(description, documentType, totalCount, processedCount, errors));
        }

        ReportProgress("Preparing index");
        await PrepareIndex(options);

        ReportProgress("Calculating total count");
        totalCount = 0L;
        processedCount = 0L;

        await foreach (var documentIds in EnumerateAllDocumentIds(options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalCount += documentIds.Count;
            options.DocumentIds = documentIds;
            await _indexQueue.Enqueue(queueId, options);
        }

        ReportProgress();
        await _indexQueue.Wait(queueId, cancellationToken, (batchOptions, batchResult) =>
        {
            processedCount += batchOptions?.DocumentIds?.Count ?? 0L;
            ReportProgress(errors: GetIndexingErrors(batchResult));
        });

        await SwapIndices(options);

        ReportProgress("Indexation finished");
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


    protected virtual async Task PrepareIndex(IndexingOptions options)
    {
        await DeleteIndex(options);
        await CreateIndex(options);
    }

    protected virtual async Task DeleteIndex(IndexingOptions options)
    {
        if (options.DeleteExistingIndex)
        {
            await _searchProvider.DeleteIndexAsync(options.DocumentType);
            // TODO: Report progress
            // TODO: Wait until index is deleted
        }
    }

    protected virtual async Task<IndexingResult> CreateIndex(IndexingOptions options)
    {
        // TODO: Report progress

        var temporaryDocumentId = Guid.NewGuid().ToString("N");
        var document = new IndexDocument(temporaryDocumentId);
        var documents = new[] { document };

        AddSystemFields(documents);
        // TODO: Define other fields

        var result = options.DeleteExistingIndex && _searchProvider is ISupportIndexSwap supportIndexSwapProvider
            ? await supportIndexSwapProvider.IndexWithBackupAsync(options.DocumentType, documents)
            : await _searchProvider.IndexAsync(options.DocumentType, documents);

        await _searchProvider.RemoveAsync(options.DocumentType, documents);

        return result;
    }

    protected virtual async IAsyncEnumerable<IList<string>> EnumerateAllDocumentIds(IndexingOptions options)
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
            var changes = await GetNextChangesAsync(feeds);

            if (changes.IsNullOrEmpty())
            {
                yield break;
            }

            yield return changes.Select(x => x.DocumentId).ToList();
        }
    }
}
