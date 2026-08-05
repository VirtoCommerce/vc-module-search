using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Data.BackgroundJobs
{
    public class IndexProgressHandler
    {
        private readonly ILogger _log;
        private readonly IPushNotificationManager _pushNotificationManager;

        private IDictionary<string, long> _totalCountMap;
        private IDictionary<string, long> _processedCountMap;
        private IndexProgressPushNotification _notification;
        private bool _suppressInsignificantNotifications;
        private bool _isCanceled;
        private IJobProgress _progress;

        public IndexProgressHandler(ILogger<IndexProgressHandler> log, IPushNotificationManager pushNotificationManager)
        {
            _log = log;
            _pushNotificationManager = pushNotificationManager;
        }

        /// <param name="context">
        /// Job execution context supplied by the engine, or null when the indexation is driven outside a job (tests).
        /// Replaces Hangfire's PerformContext: the console writes and progress bar it carried are now a single
        /// <see cref="IJobProgress"/>, which the admin UI renders over SignalR instead of the Hangfire dashboard.
        /// </param>
        public IndexProgressPushNotification Start(string currentUserName, string notificationId, bool suppressInsignificantNotifications, IJobExecutionContext context)
        {
            _notification = GetNotification(currentUserName, notificationId);

#pragma warning disable CA2254 // Template should be a static expression
            _log.LogTrace(_notification.Description);
#pragma warning restore CA2254 // Template should be a static expression

            _suppressInsignificantNotifications = suppressInsignificantNotifications;
            _progress = context?.Progress;
            _isCanceled = false;
            // Progress() is invoked concurrently across document-type configurations
            // (IndexingManager.ProcessConfigurationAsync -> ReportProgress), so these counter maps
            // must be thread-safe. A plain Dictionary corrupts its internal arrays on concurrent
            // indexer writes -> IndexOutOfRangeException in Dictionary.TryInsert (VCST-5416).
            _totalCountMap = new ConcurrentDictionary<string, long>();
            _processedCountMap = new ConcurrentDictionary<string, long>();

            ReportProgress(_notification.Description, processedCount: 0, totalCount: 0);

            // Return the notification owned by THIS run so the caller can seal exactly this one,
            // even if a concurrent indexation reassigns the handler's shared _notification field.
            return _notification;
        }

        public void AlreadyInProgress()
        {
            _notification.ErrorCount++;
            _notification.Errors.Add("Indexation is already in progress.");
            Finish();
        }

        public void Cancel()
        {
            _isCanceled = true;
        }

        public void Progress(IndexingProgress progress)
        {
#pragma warning disable CA2254 // Template should be a static expression
            _log.LogTrace(progress.Description);
#pragma warning restore CA2254 // Template should be a static expression

            var documentType = progress.DocumentType;
            var totalCount = progress.TotalCount ?? 0;
            var processedCount = progress.ProcessedCount ?? 0;

            _totalCountMap[documentType] = totalCount;
            _processedCountMap[documentType] = processedCount;

            _notification.DocumentType = documentType;
            _notification.Description = progress.Description;
            _notification.TotalCount = totalCount;
            _notification.ProcessedCount = processedCount;

            if (!progress.Errors.IsNullOrEmpty())
            {
                _notification.Errors.AddRange(progress.Errors);
                _notification.ErrorCount = _notification.Errors.Count;
            }

            if (!_suppressInsignificantNotifications || totalCount > 0 || processedCount > 0)
            {
                _pushNotificationManager.Send(_notification);
            }

            WarnIfOvershooting(processedCount, totalCount, documentType);
            ReportProgress(progress.Description, processedCount, totalCount);
        }

        public void Exception(Exception ex)
        {
            Exception(ex, _notification);
        }

        // Records the error on a specific notification so it is attributed to the run that failed,
        // independent of any concurrent run reassigning the handler's shared _notification. See VCST-5091.
        public void Exception(Exception ex, IndexProgressPushNotification notification)
        {
            var errorMessage = ex.ToString();
#pragma warning disable CA2254 // Template should be a static expression
            _log.LogError(errorMessage);
#pragma warning restore CA2254 // Template should be a static expression

            notification ??= _notification;
            notification.Errors.Add(errorMessage);
            notification.ErrorCount++;
        }

        public void Finish()
        {
            Finish(_notification);
        }

        // Seals a specific notification (sets Finished + terminal Description) and pushes it to the client.
        // Taking the notification explicitly lets each indexation run finish the notification it started,
        // so a concurrent run that reassigns the handler's shared _notification cannot leave an
        // earlier run's notification stuck without a Finished timestamp (the Admin "Indexation" blade
        // spins "In progress" until Finished is set). See VCST-5091.
        public void Finish(IndexProgressPushNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            var totalCount = _totalCountMap.Values.Sum();
            var processedCount = _processedCountMap.Values.Sum();

            notification.Finished = DateTime.UtcNow;
            notification.TotalCount = totalCount;
            notification.ProcessedCount = processedCount;

            if (_isCanceled)
            {
                notification.Description = "Indexation has been canceled";
            }
            else
            {
                var resultSuffix = notification.ErrorCount > 0 ? " with errors" : " successfully";
                notification.Description = !_suppressInsignificantNotifications
                    ? "Indexation completed" + resultSuffix
                    : $"{notification.DocumentType}: Indexation completed. Total: {totalCount}, Processed: {processedCount}, Errors: {notification.ErrorCount}.";
            }

            _log.LogTrace(notification.Description);

            if (!_suppressInsignificantNotifications || _isCanceled || totalCount > 0 || processedCount > 0)
            {
                _pushNotificationManager.Send(notification);
            }

            WarnIfOvershooting(processedCount, totalCount, notification.DocumentType);
            ReportProgress(notification.Description, processedCount, totalCount);
        }

        public static IndexProgressPushNotification CreateNotification(string currentUserName, string notificationId)
        {
            var notification = new IndexProgressPushNotification(currentUserName ?? "BackgroundJob")
            {
                Title = "Indexation process",
                Description = "Starting indexation...",
            };

            if (!string.IsNullOrEmpty(notificationId))
            {
                notification.Id = notificationId;
            }

            return notification;
        }


        private IndexProgressPushNotification GetNotification(string currentUserName, string notificationId)
        {
            IndexProgressPushNotification notification = null;

            if (!string.IsNullOrEmpty(notificationId))
            {
                var searchCriteria = new PushNotificationSearchCriteria
                {
                    Ids = new[] { notificationId }
                };

                var searchResult = _pushNotificationManager.SearchNotifies(currentUserName, searchCriteria);

                notification = searchResult?.NotifyEvents.OfType<IndexProgressPushNotification>().FirstOrDefault();
            }

            var result = notification ?? CreateNotification(currentUserName, notificationId);
            return result;
        }

        private void WarnIfOvershooting(long processedCount, long totalCount, string documentType)
        {
            if (processedCount > totalCount)
            {
                _log.LogWarning("Processed count is grater than total count. DocumentType: {DocumentType}, Processed: {Processed}, Total: {Total}",
                    documentType, processedCount, totalCount);
            }
        }

        /// <summary>
        /// Reports one progress update to the engine. Replaces the Hangfire.Console progress bar and WriteLine calls:
        /// message and counters now travel together, and the engine pushes them to the admin UI.
        /// </summary>
        /// <remarks>
        /// Fire-and-forget on purpose. This is called from <see cref="Progress"/>, which the indexing manager invokes
        /// as a synchronous <c>Action</c> on a hot path; blocking on the report would stall indexing, and a failed
        /// progress push must never fail the run - exactly the contract the fire-and-forget
        /// <c>IPushNotificationManager.Send</c> above it already has.
        /// </remarks>
        private void ReportProgress(string message, long processedCount, long totalCount)
        {
            if (_progress is null)
            {
                return;
            }

            var info = new JobProgressInfo
            {
                Message = message,
                ProcessedCount = processedCount,
                TotalCount = totalCount,
            };

            _ = _progress.Report(info).ContinueWith(
                t => _log.LogDebug(t.Exception, "Failed to report indexing progress"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
