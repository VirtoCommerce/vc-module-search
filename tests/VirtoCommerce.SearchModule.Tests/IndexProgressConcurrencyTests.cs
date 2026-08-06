using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

// Regression coverage for VCST-5416:
// IndexProgressHandler.Progress(...) is invoked concurrently across document-type configurations
// (IndexingManager.ProcessConfigurationAsync -> ReportProgress). It writes the per-document-type
// counters via a plain Dictionary<string,long> indexer, which is NOT thread-safe: concurrent
// Dictionary.TryInsert calls corrupt the internal bucket/entries arrays and throw
// IndexOutOfRangeException (or InvalidOperationException) under load, crashing indexation and
// leaving the "Indexation is already in progress" state stuck.
public class IndexProgressConcurrencyTests
{
    [Fact]
    public void Progress_CalledConcurrentlyAcrossDocumentTypes_DoesNotCorruptCountersMap_VCST5416()
    {
        // Concurrent writes to a plain Dictionary corrupt its internal arrays during a resize (TryInsert),
        // so the crash is probabilistic. Run several rounds and require EVERY round to survive; on the
        // unfixed code at least one round throws IndexOutOfRangeException / InvalidOperationException,
        // making this reliably red. With ConcurrentDictionary all rounds pass.
        const int rounds = 20;
        const int threads = 16;
        const int writesPerThread = 20000;

        var exceptions = new ConcurrentQueue<Exception>();

        for (var round = 0; round < rounds && exceptions.IsEmpty; round++)
        {
            var pushManager = new CountingPushNotificationManager();
            var handler = new IndexProgressHandler(NullLogger<IndexProgressHandler>.Instance, pushManager);

            // Start() initializes the internal counter maps. Context is null: outside a job the handler skips
            // progress reporting (ReportProgress no-ops on a null IJobProgress) — the counter maps under test still run.
            handler.Start("admin", notificationId: null, suppressInsignificantNotifications: true, context: null);

            var roundIndex = round;

            // Act: fan out Progress(...) across threads, each writing a stream of DISTINCT documentType
            // keys so the counter dictionaries keep resizing concurrently — exactly the TryInsert window
            // that corrupts a non-thread-safe Dictionary. Mirrors IndexingManager.ProcessConfigurationAsync
            // reporting progress from parallel per-configuration streams.
            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, threadIndex =>
            {
                try
                {
                    for (var i = 0; i < writesPerThread; i++)
                    {
                        // Distinct key per write keeps the map growing -> continuous resize races.
                        var documentType = $"DocType-{roundIndex}-{threadIndex}-{i}";
                        var progress = new IndexingProgress(
                            description: documentType,
                            documentType: documentType,
                            totalCount: writesPerThread,
                            processedCount: i);

                        handler.Progress(progress);
                    }
                }
                catch (Exception ex)
                {
                    // IndexOutOfRangeException / InvalidOperationException from Dictionary corruption lands here.
                    exceptions.Enqueue(ex);
                }
            });
        }

        // Assert: no concurrency corruption exception was thrown across all rounds.
        Assert.True(exceptions.IsEmpty,
            "IndexProgressHandler.Progress threw under concurrent multi-document-type load — the counter maps are not thread-safe (VCST-5416). First error: "
            + (exceptions.TryPeek(out var first) ? first.ToString() : "<none>"));
    }

    // Minimal thread-safe push-notification sink: Progress() calls Send() on every iteration,
    // so the sink itself must not become the source of contention/exceptions.
    private sealed class CountingPushNotificationManager : IPushNotificationManager
    {
        private readonly ConcurrentDictionary<string, PushNotification> _store = new();

        public void Send(PushNotification notification) => _store[notification.Id] = notification;

        public Task SendAsync(PushNotification notification)
        {
            Send(notification);
            return Task.CompletedTask;
        }

        public PushNotificationSearchResult SearchNotifies(string userId, PushNotificationSearchCriteria criteria)
            => new() { NotifyEvents = new List<PushNotification>(), TotalCount = 0 };
    }
}
