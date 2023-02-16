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
    private readonly ISearchProvider _searchProvider;
    private readonly IIndexQueueServiceFactory _indexQueueServiceFactory;

    public ScalableIndexingManager(
        ISearchProvider searchProvider,
        IEnumerable<IndexDocumentConfiguration> configurations,
        ISettingsManager settingsManager,
        IIndexQueueServiceFactory indexQueueServiceFactory)
        : base(searchProvider, configurations, settingsManager)
    {
        _searchProvider = searchProvider;
        _indexQueueServiceFactory = indexQueueServiceFactory;
    }

    public virtual async Task IndexAllDocuments(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
    {
        var indexQueueService = _indexQueueServiceFactory.Create();

        Console.WriteLine($">>> IndexAllDocuments > Start {indexQueueService.GetType().Name}");
        ValidateOptions(options);

        var documentType = options.DocumentType;
        var processedCount = 0L;
        var totalCount = 0L;

        void Progress(string message = null, IList<string> errors = null)
        {
            ReportProgress(progressCallback, documentType, message, processedCount, totalCount, errors);
        }

        Progress("Preparing index");
        await PrepareIndex(options, progressCallback, cancellationToken);

        Progress("Calculating total count");
        var queueId = await indexQueueService.CreateQueue(options);

        await foreach (var documentIds in EnumerateAllDocumentIds(options, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalCount += documentIds.Count;
            options.DocumentIds = documentIds;
            await indexQueueService.Enqueue(queueId, options);
        }

        // Report total count
        Progress();

        await indexQueueService.Wait(queueId, cancellationToken, batchResult =>
        {
            processedCount += batchResult?.Options?.DocumentIds?.Count ?? 0L;
            Progress(errors: GetIndexingErrors(batchResult?.Result));
        });

        await indexQueueService.DeleteQueue(queueId);
        await SwapIndices(options);

        Progress("Indexation finished");
        Console.WriteLine(">>> IndexAllDocuments > End");
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
