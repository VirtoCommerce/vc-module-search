using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.Jobs;

namespace VirtoCommerce.SearchModule.Data.BackgroundJobs;

public sealed class IndexingJobs : IIndexingJobService
{
    /// <summary>
    /// Resource key of the distributed lock that keeps two indexation runs from overlapping. Same name the Hangfire
    /// implementation used, so a mixed-version cluster mid-upgrade still contends on one key.
    /// </summary>
    public const string IndexationLockKey = "IndexationJob";

    /// <summary>
    /// How long the indexation lock is held. Hangfire's lock lived as long as its connection; this one is a TTL, so a
    /// run that outlasts it can be joined by a second one. A full reindex of a large catalog runs for hours.
    /// </summary>
    private static readonly TimeSpan _indexationLockTimeout = TimeSpan.FromHours(12);

    /// <summary>
    /// Where the id of the in-flight indexation is parked so <see cref="CancelIndexation"/> can find it.
    /// </summary>
    /// <remarks>
    /// The Hangfire version asked the broker instead - GetMonitoringApi().ProcessingJobs() scanned every running job
    /// and matched by reflected MethodInfo. The engine-agnostic API has no such query, and RabbitMQ could not answer
    /// it anyway: it keeps no job ledger. Recording our own id is both simpler and portable, and settings are already
    /// this module's cluster-visible scratch space - see the per-type IndexationDate settings for the same pattern.
    /// The setting itself is registered (hidden) as ModuleConstants.Settings.IndexingJobs.CurrentJobId.
    /// </remarks>
    private readonly IEnumerable<IndexDocumentConfiguration> _documentsConfigs;
    private readonly IIndexingManager _indexingManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IndexProgressHandler _progressHandler;
    private readonly IDistributedLockService _distributedLockService;
    private readonly ILogger<IndexingJobs> _logger;

    public IndexingJobs(
        IEnumerable<IndexDocumentConfiguration> documentsConfigs,
        IIndexingManager indexingManager,
        ISettingsManager settingsManager,
        IndexProgressHandler progressHandler,
        IDistributedLockService distributedLockService,
        ILogger<IndexingJobs> logger)
    {
        _documentsConfigs = documentsConfigs;
        _indexingManager = indexingManager;
        _settingsManager = settingsManager;
        _progressHandler = progressHandler;
        _distributedLockService = distributedLockService;
        _logger = logger ?? NullLogger<IndexingJobs>.Instance;
    }

    public async Task<IndexProgressPushNotification> EnqueueAsync(string currentUserName, IndexingOptions[] options, CancellationToken cancellationToken = default)
    {
        var notification = IndexProgressHandler.CreateNotification(currentUserName, null);

        var payload = AbstractTypeFactory<IndexAllDocumentsJobPayload>.TryCreateInstance();
        payload.UserName = currentUserName;
        payload.NotificationId = notification.Id;
        payload.Options = options;

        notification.JobId = await BackgroundJob.Enqueue<IndexAllDocumentsJobHandler>(
            payload,
            new EnqueueOptions { Queue = JobPriority.Normal });

        return notification;
    }

    [Obsolete("Use EnqueueAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
#pragma warning disable S4462
    public IndexProgressPushNotification Enqueue(string currentUserName, IndexingOptions[] options)
        => EnqueueAsync(currentUserName, options).GetAwaiter().GetResult();
#pragma warning restore S4462

    public Task StartStopRecurringJobs()
    {
        return Task.CompletedTask;
    }

    // Cancel current indexation if there is one
    public async Task CancelIndexationAsync(CancellationToken cancellationToken = default)
    {
        if (!BackgroundJob.SupportsCancellation)
        {
            _logger.LogWarning("Indexation cancellation was requested, but the active background job engine does not support it.");
            return;
        }

        var jobId = await GetCurrentJobIdAsync();

        if (string.IsNullOrEmpty(jobId))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Attempting to cancel indexing job. JobId: {JobId}", jobId);

            var canceled = await BackgroundJob.Cancel(jobId, cancellationToken);

            _logger.LogInformation("Indexing job cancellation requested. JobId: {JobId}, Canceled: {Canceled}", jobId, canceled);
        }
        catch (Exception ex)
        {
            // Ignore concurrency exceptions, when somebody else cancelled it as well.
            _logger.LogError(ex, "Error cancelling indexing job {JobId}", jobId);
        }
    }

    [Obsolete("Use CancelIndexationAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
#pragma warning disable S4462
    public void CancelIndexation()
        => CancelIndexationAsync().GetAwaiter().GetResult();
#pragma warning restore S4462


    public Task IndexAllDocumentsJob(string userName, string notificationId, IndexingOptions[] options, IJobExecutionContext context, CancellationToken cancellationToken)
    {
        return RunIndexJobAsync(userName, notificationId, false, options, IndexAllDocumentsAsync, context, cancellationToken);
    }

    /// <remarks>
    /// [AutomaticRetry(Attempts = 0)] moved to the enqueue site as EnqueueOptions.MaxRetryAttempts = 0, and
    /// [DisableConcurrentExecution(10)] is redundant: <see cref="RunIndexJobAsync"/> takes a distributed lock that
    /// already serializes every indexation path across the whole worker fleet.
    /// </remarks>
    public async Task IndexChangesJob(string documentType, IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var allOptions = await GetAllIndexingOptionsAsync(documentType);
        foreach (var options in allOptions)
        {
            await RunIndexJobAsync(null, null, true, [options], IndexChangesAsync, context, cancellationToken);
        }
    }


    private static Task EnqueueIndexDocuments(string documentType, string[] documentIds, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null, CancellationToken cancellationToken = default)
    {
        var payload = AbstractTypeFactory<IndexDocumentsJobPayload>.TryCreateInstance();
        payload.DocumentType = documentType;
        payload.DocumentIds = documentIds;
        payload.BuilderTypes = builders?.Select(x => x.GetType().FullName).ToArray();

        // One handler for all priorities: the priority is the target queue, not a separate method.
        return BackgroundJob.Enqueue<IndexDocumentsJobHandler>(payload, new EnqueueOptions { Queue = ValidatePriority(priority) }, cancellationToken);
    }

    private static Task EnqueueDeleteDocuments(string documentType, string[] documentIds, string priority = JobPriority.Normal, CancellationToken cancellationToken = default)
    {
        var payload = AbstractTypeFactory<DeleteDocumentsJobPayload>.TryCreateInstance();
        payload.DocumentType = documentType;
        payload.DocumentIds = documentIds;

        return BackgroundJob.Enqueue<DeleteDocumentsJobHandler>(payload, new EnqueueOptions { Queue = ValidatePriority(priority) }, cancellationToken);
    }

    // Kept as an explicit check because the queue is now a free-form string: an unknown priority used to be rejected
    // by the switch below, and silently enqueuing onto a queue nobody drains would be a much quieter failure.
    private static string ValidatePriority(string priority)
    {
        return priority switch
        {
            JobPriority.High or JobPriority.Normal or JobPriority.Low => priority,
            _ => throw new ArgumentException($"Unknown priority: {priority}", nameof(priority)),
        };
    }

    public async Task EnqueueIndexAndDeleteDocumentsAsync(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null, CancellationToken cancellationToken = default)
    {
        var groupedEntriesByType = GetGroupedByTypeAndDistinctedByChangeTypeIndexEntries(indexEntries);

        foreach (var groupedEntryByType in groupedEntriesByType)
        {
            var addedEntryIds = groupedEntryByType.Where(x => x.EntryState == EntryState.Added).Select(x => x.Id).ToArray();
            var modifiedEntryIds = groupedEntryByType.Where(x => x.EntryState == EntryState.Modified).Select(x => x.Id).ToArray();
            var deletedEntryIds = groupedEntryByType.Where(x => x.EntryState == EntryState.Deleted).Select(x => x.Id).ToArray();

            if (addedEntryIds.Length > 0)
            {
                await EnqueueIndexDocuments(groupedEntryByType.Key, addedEntryIds, priority, builders: null, cancellationToken: cancellationToken);
            }

            if (modifiedEntryIds.Length > 0)
            {
                await EnqueueIndexDocuments(groupedEntryByType.Key, modifiedEntryIds, priority, builders, cancellationToken: cancellationToken);
            }

            if (deletedEntryIds.Length > 0)
            {
                await EnqueueDeleteDocuments(groupedEntryByType.Key, deletedEntryIds, priority, cancellationToken: cancellationToken);
            }
        }
    }

    [Obsolete("Use EnqueueIndexAndDeleteDocumentsAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
#pragma warning disable S4462
    public void EnqueueIndexAndDeleteDocuments(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null)
        => EnqueueIndexAndDeleteDocumentsAsync(indexEntries, priority, builders).GetAwaiter().GetResult();
#pragma warning restore S4462

    public static IEnumerable<IGrouping<string, IndexEntry>> GetGroupedByTypeAndDistinctedByChangeTypeIndexEntries(IEnumerable<IndexEntry> indexEntries)
    {
        var indexEntriesFilteredFromEmptyIds = indexEntries.Where(x => !string.IsNullOrEmpty(x.Id));

        var result = new List<IndexEntry>();

        foreach (var indexEntryGroupedByType in indexEntriesFilteredFromEmptyIds.GroupBy(x => x.Type))
        {
            foreach (var indexEntryGroupedById in indexEntryGroupedByType.GroupBy(x => x.Id))
            {
                var entryWasAdded = indexEntryGroupedById.Any(x => x.EntryState is EntryState.Added);
                var entryWasModified = indexEntryGroupedById.Any(x => x.EntryState is EntryState.Modified);
                var entryWasDeleted = indexEntryGroupedById.Any(x => x.EntryState is EntryState.Deleted);

                if (entryWasDeleted)
                {
                    result.Add(indexEntryGroupedById.First(x => x.EntryState is EntryState.Deleted));
                }
                else if (entryWasAdded)
                {
                    result.Add(indexEntryGroupedById.First(x => x.EntryState is EntryState.Added));
                }
                else if (entryWasModified)
                {
                    result.Add(indexEntryGroupedById.First(x => x.EntryState is EntryState.Modified));
                }
            }
        }

        return result.GroupBy(x => x.Type);
    }

    public async Task IndexDocumentsAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
    {
        if (documentIds.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _indexingManager.IndexDocumentsAsync(documentType, documentIds, builderTypes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Bulk index job was cancelled. DocumentType: {DocumentType}, DocumentCount: {DocumentCount}",
                documentType, documentIds.Length);
            throw;
        }
    }

    public async Task DeleteDocumentsAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
    {
        if (documentIds.IsNullOrEmpty())
        {
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _indexingManager.DeleteDocumentsAsync(documentType, documentIds, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Bulk delete job was cancelled. DocumentType: {DocumentType}, DocumentCount: {DocumentCount}",
                documentType, documentIds.Length);
            throw;
        }
    }


    #region Legacy Hangfire entry points

    // Kept for indexing jobs enqueued by an earlier version, which reference these methods by name. Hangfire persists
    // a queued job as type name plus method name plus parameter types plus serialized args, so the signatures are
    // byte-identical on purpose. The priority they used to carry in a [Queue] attribute is now the queue chosen at
    // enqueue time, which is why all six collapse onto two methods here. Remove once no such job can still be pending.

    [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses IndexDocumentsJobHandler.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexDocumentsHighPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
        => IndexDocumentsAsync(documentType, documentIds, builderTypes, cancellationToken);

    [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses IndexDocumentsJobHandler.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexDocumentsNormalPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
        => IndexDocumentsAsync(documentType, documentIds, builderTypes, cancellationToken);

    [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses IndexDocumentsJobHandler.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexDocumentsLowPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
        => IndexDocumentsAsync(documentType, documentIds, builderTypes, cancellationToken);

    [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses DeleteDocumentsJobHandler.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task DeleteDocumentsHighPriorityAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
        => DeleteDocumentsAsync(documentType, documentIds, cancellationToken);

    [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses DeleteDocumentsJobHandler.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task DeleteDocumentsNormalPriorityAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
        => DeleteDocumentsAsync(documentType, documentIds, cancellationToken);

    [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses DeleteDocumentsJobHandler.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task DeleteDocumentsLowPriorityAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
        => DeleteDocumentsAsync(documentType, documentIds, cancellationToken);

    #endregion


    private async Task<bool> RunIndexJobAsync(
        string currentUserName,
        string notificationId,
        bool suppressInsignificantNotifications,
        IEnumerable<IndexingOptions> allOptions,
        Func<IndexingOptions, CancellationToken, Task> indexationFunc,
        IJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Materialize once. The parameter is typed as IEnumerable so callers could pass a deferred
        // or one-shot iterator; we read it again from the catch block for cancellation logging,
        // and we don't want re-enumeration to silently produce empty / different / side-effecting
        // results. The `as` cast avoids an extra copy when an array is passed (the common case).
        var optionsArray = allOptions as IndexingOptions[] ?? allOptions.ToArray();

        var success = false;

        // Reset progress handler to initial state.
        // Capture the notification owned by THIS run so it is always sealed (Finished set) below,
        // even if a concurrent indexation reassigns the shared handler state. Otherwise the
        // Admin "Indexation" blade keeps spinning "In progress" forever (VCST-5091).
        var notification = _progressHandler.Start(currentUserName, notificationId, suppressInsignificantNotifications, context);

        // Make sure only one indexation job can run in the cluster.
        // CAUTION: locking mechanism assumes single threaded execution.
        try
        {
            // tryLockTimeout stays null: fail immediately when another run holds the lock, matching the TimeSpan.Zero
            // the Hangfire AcquireDistributedLock was called with.
            await _distributedLockService.ExecuteAsync(
                IndexationLockKey,
                async () =>
                {
                    // Publish the engine-assigned id ONLY after the lock is held - i.e. only for the run that is
                    // actually executing - so CancelIndexation targets the live job. A run that loses the lock never
                    // reaches here, so it can neither overwrite the winner's id nor clear it out from under it.
                    await SetCurrentJobIdAsync(context?.JobId);
                    try
                    {
                        var tasks = optionsArray.Select(x => indexationFunc(x, cancellationToken)).ToArray();
                        await Task.WhenAll(tasks);

                        success = true;
                    }
                    // The engine passes a token that fires on shutdown AND on job cancellation;
                    // both surface as OperationCanceledException, exactly as under Hangfire.
                    catch (OperationCanceledException)
                    {
                        var documentTypes = string.Join(", ", optionsArray.Select(o => o?.DocumentType ?? "<null>"));
                        _logger.LogWarning("Indexing job {JobId} was cancelled. User: {UserName}, NotificationId: {NotificationId}, DocumentTypes: {DocumentTypes}",
                            context?.JobId, currentUserName, notificationId, documentTypes);
                        _progressHandler.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _progressHandler.Exception(ex, notification);
                    }
                    finally
                    {
                        // Always seal this run's notification so it reaches a terminal state.
                        // (Previously deferred to IndexAllDocumentsJob's outer finally for manual jobs,
                        // which sealed whatever the shared _notification pointed at by then.)
                        _progressHandler.Finish(notification);

                        // Clear only the id this run set, and only because this run is the lock holder.
                        await SetCurrentJobIdAsync(null);
                    }

                    return true;
                },
                lockTimeout: _indexationLockTimeout,
                cancellationToken: cancellationToken);
        }
        catch (PlatformException)
        {
            // Another indexation holds the lock. IDistributedLockService reports that as PlatformException, where the
            // Hangfire lock threw DistributedLockTimeoutException and the bare catch below swallowed everything.
            _progressHandler.AlreadyInProgress();
        }

        return success;
    }

    private Task<string> GetCurrentJobIdAsync()
    {
        return _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.IndexingJobs.CurrentJobId);
    }

    private Task SetCurrentJobIdAsync(string jobId)
    {
        return _settingsManager.SetValueAsync(ModuleConstants.Settings.IndexingJobs.CurrentJobId.Name, jobId ?? string.Empty);
    }

    private async Task IndexAllDocumentsAsync(IndexingOptions options, CancellationToken cancellationToken)
    {
        var oldIndexationDate = await GetLastIndexationDateAsync(options.DocumentType);
        var newIndexationDate = DateTime.UtcNow;

        await _indexingManager.IndexAllDocumentsAsync(options, _progressHandler.Progress, cancellationToken);

        // Save indexation date to prevent changes from being indexed again
        await SetLastIndexationDateAsync(options.DocumentType, oldIndexationDate, newIndexationDate);
    }

    private async Task IndexChangesAsync(IndexingOptions options, CancellationToken cancellationToken)
    {
        var oldIndexationDate = options.StartDate;
        var newIndexationDate = DateTime.UtcNow;

        options.EndDate = oldIndexationDate == null ? null : newIndexationDate;

        await _indexingManager.IndexChangesAsync(options, _progressHandler.Progress, cancellationToken);

        // Save indexation date. It will be used as a start date for the next indexation
        await SetLastIndexationDateAsync(options.DocumentType, oldIndexationDate, newIndexationDate);
    }

    private async Task<IList<IndexingOptions>> GetAllIndexingOptionsAsync(string documentType)
    {
        var configs = _documentsConfigs;

        if (!string.IsNullOrEmpty(documentType))
        {
            configs = configs.Where(c => c.DocumentType.EqualsIgnoreCase(documentType));
        }

        var tasks = configs.Select(x => GetIndexingOptionsAsync(x.DocumentType)).ToArray();
        var result = await Task.WhenAll(tasks);

        return result;
    }

    private async Task<IndexingOptions> GetIndexingOptionsAsync(string documentType)
    {
        return new IndexingOptions
        {
            DocumentType = documentType,
            DeleteExistingIndex = false,
            StartDate = await GetLastIndexationDateAsync(documentType),
            BatchSize = await GetBatchSizeAsync(),
        };
    }

    private async Task<DateTime?> GetLastIndexationDateAsync(string documentType)
    {
        var result = (await _indexingManager.GetIndexStateAsync(documentType)).LastIndexationDate;
        if (result != null)
        {
            //need to take the older date from the dates loaded from the index and settings.
            //Because the actual last indexation date stored in the index may be later than last job run are stored in the settings. e.g. after data import or direct database changes
            var settingValue = await _settingsManager.GetValueAsync<DateTime>(ModuleConstants.Settings.IndexingJobs.IndexationDate(documentType));
            result = new DateTime(Math.Min(result.Value.Ticks, settingValue.Ticks), DateTimeKind.Utc);
        }

        return result;
    }

    private async Task SetLastIndexationDateAsync(string documentType, DateTime? oldValue, DateTime newValue)
    {
        var currentValue = await GetLastIndexationDateAsync(documentType);
        if (currentValue == oldValue)
        {
            await _settingsManager.SetValueAsync(ModuleConstants.Settings.IndexingJobs.IndexationDate(documentType).Name, newValue);
        }
    }

    private Task<int> GetBatchSizeAsync()
    {
        return _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.IndexPartitionSize);
    }
}
