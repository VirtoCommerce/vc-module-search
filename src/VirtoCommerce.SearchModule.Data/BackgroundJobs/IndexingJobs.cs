using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.BackgroundJobs;

public sealed class IndexingJobs : IIndexingJobService
{
    private const string _recurringJobId = $"{nameof(IndexingJobs)}.{nameof(IndexChangesJob)}";
    private static readonly MethodInfo _recurringJobMethod = typeof(IndexingJobs).GetMethod(nameof(IndexChangesJob), [typeof(string), typeof(PerformContext), typeof(CancellationToken)]);
    private static readonly MethodInfo _manualJobMethod = typeof(IndexingJobs).GetMethod(nameof(IndexAllDocumentsJob), [typeof(string), typeof(string), typeof(IndexingOptions[]), typeof(PerformContext), typeof(CancellationToken)]);

    private readonly IEnumerable<IndexDocumentConfiguration> _documentsConfigs;
    private readonly IIndexingManager _indexingManager;
    private readonly ISettingsManager _settingsManager;
    private readonly IndexProgressHandler _progressHandler;
    private readonly ILogger<IndexingJobs> _log;

    public IndexingJobs(
        IEnumerable<IndexDocumentConfiguration> documentsConfigs,
        IIndexingManager indexingManager,
        ISettingsManager settingsManager,
        IndexProgressHandler progressHandler,
        ILogger<IndexingJobs> log)
    {
        _documentsConfigs = documentsConfigs;
        _indexingManager = indexingManager;
        _settingsManager = settingsManager;
        _progressHandler = progressHandler;
        _log = log;
    }

    // Backwards-compatible constructor for callers that wired up IndexingJobs before the
    // ILogger<IndexingJobs> parameter was introduced. Cancellation logging will be silently
    // skipped when no logger is supplied; everything else continues to work.
    [Obsolete("Use the constructor that accepts ILogger<IndexingJobs>.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public IndexingJobs(
        IEnumerable<IndexDocumentConfiguration> documentsConfigs,
        IIndexingManager indexingManager,
        ISettingsManager settingsManager,
        IndexProgressHandler progressHandler)
        : this(documentsConfigs, indexingManager, settingsManager, progressHandler, log: null)
    {
    }

    // Enqueue a background job with single notification object for all given options
    public IndexProgressPushNotification Enqueue(string currentUserName, IndexingOptions[] options)
    {
        var notification = IndexProgressHandler.CreateNotification(currentUserName, null);

        // Hangfire substitutes CancellationToken.None with a real token at execution time.
        notification.JobId = BackgroundJob.Enqueue<IndexingJobs>(j => j.IndexAllDocumentsJob(currentUserName, notification.Id, options, null, CancellationToken.None));

        return notification;
    }

    public async Task StartStopRecurringJobs()
    {
        var scheduleJobs = await _settingsManager.GetValueAsync<bool>(ModuleConstants.Settings.IndexingJobs.Enable);

        if (scheduleJobs)
        {
            var cronExpression = await _settingsManager.GetValueAsync<string>(ModuleConstants.Settings.IndexingJobs.CronExpression);
            RecurringJob.AddOrUpdate<IndexingJobs>(_recurringJobId, x => x.IndexChangesJob(null, null, CancellationToken.None), cronExpression);
        }
        else
        {
            CancelJob(_recurringJobMethod);
            RecurringJob.RemoveIfExists(_recurringJobId);
        }
    }

    // Cancel current indexation if there is one
    public void CancelIndexation()
    {
        CancelJob(_manualJobMethod);
    }

    private static void CancelJob(MethodInfo method)
    {
        var processingJobs = JobStorage.Current.GetMonitoringApi().ProcessingJobs(0, int.MaxValue);

        // Match by name + declaring type (rather than by exact MethodInfo reference) so that
        // jobs running through the [Obsolete] IJobCancellationToken-flavored shim overloads
        // are also cancelled. Hangfire treats each overload as a distinct method.
        var (jobId, _) = processingJobs.FirstOrDefault(x =>
            x.Value?.Job?.Method is { } running &&
            running.DeclaringType == method.DeclaringType &&
            running.Name == method.Name);

        if (!string.IsNullOrEmpty(jobId))
        {
            try
            {
                BackgroundJob.Delete(jobId);
            }
            catch
            {
                // Ignore concurrency exceptions, when somebody else cancelled it as well.
            }
        }
    }


    // One-time job for manual indexation
    [Queue(JobPriority.Normal)]
    public async Task IndexAllDocumentsJob(string userName, string notificationId, IndexingOptions[] options, PerformContext context, CancellationToken cancellationToken)
    {
        try
        {
            await RunIndexJobAsync(userName, notificationId, false, options, IndexAllDocumentsAsync, context, cancellationToken);
        }
        finally
        {
            // Report indexation summary
            _progressHandler.Finish();
        }
    }

    // Recurring job for automatic changes indexation.
    // It should push separate notification for each document type if any changes were indexed for this type
    [Queue(JobPriority.Normal)]
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(10)]
    public async Task IndexChangesJob(string documentType, PerformContext context, CancellationToken cancellationToken)
    {
        var allOptions = await GetAllIndexingOptionsAsync(documentType);
        foreach (var options in allOptions)
        {
            await RunIndexJobAsync(null, null, true, [options], IndexChangesAsync, context, cancellationToken);
        }
    }


    private static void EnqueueIndexDocuments(string documentType, string[] documentIds, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null)
    {
        var buildersTypes = builders?.Select(x => x.GetType().FullName);

        switch (priority)
        {
            case JobPriority.High:
                BackgroundJob.Enqueue<IndexingJobs>(x => x.IndexDocumentsHighPriorityAsync(documentType, documentIds, buildersTypes, CancellationToken.None));
                break;
            case JobPriority.Normal:
                BackgroundJob.Enqueue<IndexingJobs>(x => x.IndexDocumentsNormalPriorityAsync(documentType, documentIds, buildersTypes, CancellationToken.None));
                break;
            case JobPriority.Low:
                BackgroundJob.Enqueue<IndexingJobs>(x => x.IndexDocumentsLowPriorityAsync(documentType, documentIds, buildersTypes, CancellationToken.None));
                break;
            default:
                throw new ArgumentException($"Unknown priority: {priority}", nameof(priority));
        }
    }

    private static void EnqueueDeleteDocuments(string documentType, string[] documentIds, string priority = JobPriority.Normal)
    {
        switch (priority)
        {
            case JobPriority.High:
                BackgroundJob.Enqueue<IndexingJobs>(x => x.DeleteDocumentsHighPriorityAsync(documentType, documentIds, CancellationToken.None));
                break;
            case JobPriority.Normal:
                BackgroundJob.Enqueue<IndexingJobs>(x => x.DeleteDocumentsNormalPriorityAsync(documentType, documentIds, CancellationToken.None));
                break;
            case JobPriority.Low:
                BackgroundJob.Enqueue<IndexingJobs>(x => x.DeleteDocumentsLowPriorityAsync(documentType, documentIds, CancellationToken.None));
                break;
            default:
                throw new ArgumentException($"Unknown priority: {priority}", nameof(priority));
        }
    }

    public void EnqueueIndexAndDeleteDocuments(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null)
    {
        var groupedEntriesByType = GetGroupedByTypeAndDistinctedByChangeTypeIndexEntries(indexEntries);

        foreach (var groupedEntryByType in groupedEntriesByType)
        {
            var addedEntryIds = groupedEntryByType.Where(x => x.EntryState == EntryState.Added).Select(x => x.Id).ToArray();
            var modifiedEntryIds = groupedEntryByType.Where(x => x.EntryState == EntryState.Modified).Select(x => x.Id).ToArray();
            var deletedEntryIds = groupedEntryByType.Where(x => x.EntryState == EntryState.Deleted).Select(x => x.Id).ToArray();

            if (addedEntryIds.Length > 0)
            {
                EnqueueIndexDocuments(groupedEntryByType.Key, addedEntryIds, priority, builders: null);
            }

            if (modifiedEntryIds.Length > 0)
            {
                EnqueueIndexDocuments(groupedEntryByType.Key, modifiedEntryIds, priority, builders);
            }

            if (deletedEntryIds.Length > 0)
            {
                EnqueueDeleteDocuments(groupedEntryByType.Key, deletedEntryIds, priority);
            }
        }
    }

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

    // Use hard-code methods to easily set queue for Hangfire.
    // Make sure we wait for async methods to end, so that Hangfire retries if an exception occurs.

    [Queue(JobPriority.High)]
    public Task IndexDocumentsHighPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
    {
        return IndexDocumentsCoreAsync(documentType, documentIds, builderTypes, cancellationToken);
    }

    [Queue(JobPriority.Normal)]
    public Task IndexDocumentsNormalPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
    {
        return IndexDocumentsCoreAsync(documentType, documentIds, builderTypes, cancellationToken);
    }

    [Queue(JobPriority.Low)]
    public Task IndexDocumentsLowPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
    {
        return IndexDocumentsCoreAsync(documentType, documentIds, builderTypes, cancellationToken);
    }

    [Queue(JobPriority.High)]
    public Task DeleteDocumentsHighPriorityAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
    {
        return DeleteDocumentsCoreAsync(documentType, documentIds, cancellationToken);
    }

    [Queue(JobPriority.Normal)]
    public Task DeleteDocumentsNormalPriorityAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
    {
        return DeleteDocumentsCoreAsync(documentType, documentIds, cancellationToken);
    }

    [Queue(JobPriority.Low)]
    public Task DeleteDocumentsLowPriorityAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
    {
        return DeleteDocumentsCoreAsync(documentType, documentIds, cancellationToken);
    }

    // ----------------------------------------------------------------------
    // Hangfire compatibility shims for in-flight queue items enqueued before
    // the cancellation-aware overloads above were introduced. Hangfire identifies
    // a job method by its parameter list; without these the upgrade would cause
    // JobLoadException on dequeue. New code must NOT call these directly.
    //
    // The IJobCancellationToken-flavored shims of the public IndexAllDocumentsJob /
    // IndexChangesJob entry points use IJobCancellationToken.ShutdownToken to bridge
    // to the modern CancellationToken-based path. ShutdownToken only fires on server
    // shutdown, not on Hangfire-side deletion, so jobs that flow through the shim
    // remain less responsive to "Delete" until the queue has fully drained.
    // ----------------------------------------------------------------------

    [Queue(JobPriority.Normal)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexAllDocumentsJob(string userName, string notificationId, IndexingOptions[] options, PerformContext context, IJobCancellationToken cancellationToken)
        => IndexAllDocumentsJob(userName, notificationId, options, context, cancellationToken?.ShutdownToken ?? CancellationToken.None);

    [Queue(JobPriority.Normal)]
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(10)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexChangesJob(string documentType, PerformContext context, IJobCancellationToken cancellationToken)
        => IndexChangesJob(documentType, context, cancellationToken?.ShutdownToken ?? CancellationToken.None);

    [Queue(JobPriority.High)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexDocumentsHighPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes)
        => IndexDocumentsCoreAsync(documentType, documentIds, builderTypes, CancellationToken.None);

    [Queue(JobPriority.Normal)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexDocumentsNormalPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes)
        => IndexDocumentsCoreAsync(documentType, documentIds, builderTypes, CancellationToken.None);

    [Queue(JobPriority.Low)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task IndexDocumentsLowPriorityAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes)
        => IndexDocumentsCoreAsync(documentType, documentIds, builderTypes, CancellationToken.None);

    [Queue(JobPriority.High)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task DeleteDocumentsHighPriorityAsync(string documentType, string[] documentIds)
        => DeleteDocumentsCoreAsync(documentType, documentIds, CancellationToken.None);

    [Queue(JobPriority.Normal)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task DeleteDocumentsNormalPriorityAsync(string documentType, string[] documentIds)
        => DeleteDocumentsCoreAsync(documentType, documentIds, CancellationToken.None);

    [Queue(JobPriority.Low)]
    [Obsolete("Hangfire compatibility shim for legacy queue items. Use the overload with CancellationToken.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public Task DeleteDocumentsLowPriorityAsync(string documentType, string[] documentIds)
        => DeleteDocumentsCoreAsync(documentType, documentIds, CancellationToken.None);

    private async Task IndexDocumentsCoreAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
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
            _log?.LogWarning("Bulk index job was cancelled. DocumentType: {DocumentType}, DocumentCount: {DocumentCount}",
                documentType, documentIds.Length);
            throw;
        }
    }

    private async Task DeleteDocumentsCoreAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
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
            _log?.LogWarning("Bulk delete job was cancelled. DocumentType: {DocumentType}, DocumentCount: {DocumentCount}",
                documentType, documentIds.Length);
            throw;
        }
    }


    private async Task<bool> RunIndexJobAsync(
        string currentUserName,
        string notificationId,
        bool suppressInsignificantNotifications,
        IEnumerable<IndexingOptions> allOptions,
        Func<IndexingOptions, CancellationToken, Task> indexationFunc,
        PerformContext context,
        CancellationToken cancellationToken)
    {
        // Materialize once. The parameter is typed as IEnumerable so callers could pass a deferred
        // or one-shot iterator; we read it again from the catch block for cancellation logging,
        // and we don't want re-enumeration to silently produce empty / different / side-effecting
        // results. The `as` cast avoids an extra copy when an array is passed (the common case).
        var optionsArray = allOptions as IndexingOptions[] ?? allOptions.ToArray();

        var success = false;

        // Reset progress handler to initial state
        _progressHandler.Start(currentUserName, notificationId, suppressInsignificantNotifications, context);

        // Make sure only one indexation job can run in the cluster.
        // CAUTION: locking mechanism assumes single threaded execution.
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            using (connection.AcquireDistributedLock("IndexationJob", TimeSpan.Zero))
            {
                try
                {
                    var tasks = optionsArray.Select(x => indexationFunc(x, cancellationToken)).ToArray();
                    await Task.WhenAll(tasks);

                    success = true;
                }
                // Hangfire 1.7+ injects a CancellationToken that fires on both server shutdown
                // AND Hangfire-side deletion; both surface as OperationCanceledException.
                catch (OperationCanceledException)
                {
                    var documentTypes = string.Join(", ", optionsArray.Select(o => o?.DocumentType ?? "<null>"));
                    _log?.LogWarning("Indexing job {JobId} was cancelled. User: {UserName}, NotificationId: {NotificationId}, DocumentTypes: {DocumentTypes}",
                        context?.BackgroundJob?.Id, currentUserName, notificationId, documentTypes);
                    _progressHandler.Cancel();
                }
                catch (Exception ex)
                {
                    _progressHandler.Exception(ex);
                }
                finally
                {
                    // Report indexation summary only for "Recurring job for automatic changes indexation."
                    if (notificationId.IsNullOrEmpty())
                    {
                        _progressHandler.Finish();
                    }
                }
            }
        }
        catch
        {
            // TODO: Check wait in calling method
            _progressHandler.AlreadyInProgress();
        }

        return success;
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
            var settingDescriptor = new SettingDescriptor
            {
                Name = GetLastIndexationDateName(documentType),
                ValueType = SettingValueType.DateTime,
                DefaultValue = DateTime.MaxValue,
            };

            //need to take the older date from the dates loaded from the index and settings.
            //Because the actual last indexation date stored in the index may be later than last job run are stored in the settings. e.g. after data import or direct database changes
            var settingValue = await _settingsManager.GetValueAsync<DateTime>(settingDescriptor);
            result = new DateTime(Math.Min(result.Value.Ticks, settingValue.Ticks), DateTimeKind.Utc);
        }

        return result;
    }

    private async Task SetLastIndexationDateAsync(string documentType, DateTime? oldValue, DateTime newValue)
    {
        var currentValue = await GetLastIndexationDateAsync(documentType);
        if (currentValue == oldValue)
        {
            await _settingsManager.SetValueAsync(GetLastIndexationDateName(documentType), newValue);
        }
    }

    private static string GetLastIndexationDateName(string documentType)
    {
        return $"VirtoCommerce.Search.IndexingJobs.IndexationDate.{documentType}";
    }

    private Task<int> GetBatchSizeAsync()
    {
        return _settingsManager.GetValueAsync<int>(ModuleConstants.Settings.General.IndexPartitionSize);
    }
}
