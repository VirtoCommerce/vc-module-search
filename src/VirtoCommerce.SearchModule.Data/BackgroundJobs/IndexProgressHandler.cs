using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.Common;
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
        private bool _sendInsignificantNotifications;
        private bool _isCanceled;
        private PerformContext _context;
        private IProgressBar _progressBar;
        private const int _maxPercent = 100;

        public IndexProgressHandler(ILogger<IndexProgressHandler> log, IPushNotificationManager pushNotificationManager)
        {
            _log = log;
            _pushNotificationManager = pushNotificationManager;
        }

        public void Start(string currentUserName, string notificationId, bool suppressInsignificantNotifications, PerformContext context)
        {
            _notification = GetNotification(currentUserName, notificationId);

#pragma warning disable CA2254 // Template should be a static expression
            _log.LogTrace(_notification.Description);
#pragma warning restore CA2254 // Template should be a static expression

            _sendInsignificantNotifications = !suppressInsignificantNotifications;
            _context = context;
            _isCanceled = false;
            _totalCountMap = new Dictionary<string, long>();
            _processedCountMap = new Dictionary<string, long>();

            _context.WriteLine(ConsoleTextColor.White, _notification.Description);
            _progressBar = _context.WriteProgressBar();
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

            if (_sendInsignificantNotifications || totalCount > 0 || processedCount > 0)
            {
                _pushNotificationManager.Send(_notification);
            }

            UpdateHangfireProgressBar(processedCount, totalCount, documentType);
        }

        public void Exception(Exception ex)
        {
            var errorMessage = ex.ToString();
            _notification.Errors.Add(errorMessage);
            _notification.ErrorCount++;

#pragma warning disable CA2254 // Template should be a static expression
            _log.LogError(errorMessage);
#pragma warning restore CA2254 // Template should be a static expression

            _context.WriteLine(ConsoleTextColor.Red, errorMessage);
        }

        public void Finish()
        {
            var totalCount = _totalCountMap.Values.Sum();
            var processedCount = _processedCountMap.Values.Sum();

            _notification.Finished = DateTime.UtcNow;
            _notification.TotalCount = totalCount;
            _notification.ProcessedCount = processedCount;

            _notification.Description = _isCanceled
                ? "Indexation has been canceled"
                : _sendInsignificantNotifications
                    ? "Indexation completed" + (_notification.ErrorCount > 0 ? " with errors" : " successfully")
                    : $"{_notification.DocumentType}: Indexation completed. Total: {totalCount}, Processed: {processedCount}, Errors: {_notification.ErrorCount}.";

            _log.LogTrace(_notification.Description);

            if (_sendInsignificantNotifications || _isCanceled || totalCount > 0 || processedCount > 0)
            {
                _pushNotificationManager.Send(_notification);
            }

            UpdateHangfireProgressBar(processedCount, totalCount, _notification.DocumentType);
            _context.WriteLine(ConsoleTextColor.White, _notification.Description);
        }

        public static IndexProgressPushNotification CreateNotification(string currentUserName, string notificationId)
        {
            var notification = new IndexProgressPushNotification(currentUserName ?? nameof(IndexingJobs))
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

        private void UpdateHangfireProgressBar(long processedCount, long totalCount, string documentType)
        {
            if (processedCount > totalCount)
            {
                _log.LogWarning("Processed count is grater than total count. DocumentType: {DocumentType}, Processed: {Processed}, Total: {Total}",
                    documentType, processedCount, totalCount);
            }

            var progressBarValue = totalCount != 0
                ? Math.Min(_maxPercent, processedCount * _maxPercent / totalCount)
                : 0;

            _progressBar.SetValue(progressBarValue);
        }
    }
}
