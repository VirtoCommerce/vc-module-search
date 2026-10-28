using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

// Reproduces the production symptom seen on vcptcore-qa1 (VP-9334 investigation):
// indexation "completes successfully" with 0 records indexed and nothing written to the search provider,
// because the "IndexationJob" distributed lock is held by another holder.
//
// RunIndexJobAsync guards the work with IDistributedLock.TryExecuteAsync, which SKIPS (returns false) without
// throwing when the lock is held. The fix makes that skip VISIBLE - it is logged at Error level and recorded on
// the notification - instead of the previous silent swallow that made a stuck lock look like a successful run.
public class IndexationStuckLockTests
{
    [Fact]
    public async Task ManualIndexation_WhenLockHeldByAnotherHolder_IndexesNothing_AndDoesNotThrow()
    {
        // Arrange
        var pushManager = new FakePushNotificationManager();
        var notification = IndexProgressHandler.CreateNotification("admin", null);
        pushManager.Add(notification);
        var handler = new IndexProgressHandler(Microsoft.Extensions.Logging.Abstractions.NullLogger<IndexProgressHandler>.Instance, pushManager);

        var indexingManager = new RecordingIndexingManager();
        var documentConfigs = new[] { new IndexDocumentConfiguration { DocumentType = "Member" } };

        // The "IndexationJob" lock is already held by another run (e.g. a hung holder renewing its lease).
        var heldLock = new HeldDistributedLock();

        var jobs = new IndexingJobs(documentConfigs, indexingManager, CreateSettingsManager(), handler, heldLock,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IndexingJobs>.Instance);
        var options = new[] { new IndexingOptions { DocumentType = "Member" } };

        // Act: a manual full reindex while the lock is held. Must not throw (the admin UI saw "success").
        await jobs.IndexAllDocumentsJob("admin", notification.Id, options, context: null, CancellationToken.None);

        // Assert: the indexing manager was never invoked - this is why the index stays empty while the job "succeeds".
        Assert.False(indexingManager.IndexAllDocumentsCalled,
            "IndexAllDocumentsAsync ran even though the IndexationJob lock was held - expected a skip (0 records).");

        // And the notification is sealed so the Admin "Indexation" blade does not spin forever.
        Assert.True(notification.Finished.HasValue, "The lock-held run must still seal its notification.");
    }

    [Fact]
    public async Task ManualIndexation_WhenLockHeld_SkipIsVisible_LoggedAndRecordedOnNotification()
    {
        // Guards the fix: a lock-held skip must be VISIBLE (logged + on the notification), never swallowed as a
        // silent "success with 0 records" - the exact behaviour observed on QA.
        var pushManager = new FakePushNotificationManager();
        var notification = IndexProgressHandler.CreateNotification("admin", null);
        pushManager.Add(notification);
        var handler = new IndexProgressHandler(Microsoft.Extensions.Logging.Abstractions.NullLogger<IndexProgressHandler>.Instance, pushManager);
        var logger = new CapturingLogger<IndexingJobs>();

        var jobs = new IndexingJobs(
            new[] { new IndexDocumentConfiguration { DocumentType = "Member" } },
            new RecordingIndexingManager(),
            CreateSettingsManager(),
            handler,
            new HeldDistributedLock(),
            logger);

        await jobs.IndexAllDocumentsJob("admin", notification.Id, new[] { new IndexingOptions { DocumentType = "Member" } }, context: null, CancellationToken.None);

        // Visible in the logs (reaches console / App Insights).
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Indexation skipped", StringComparison.OrdinalIgnoreCase));

        // Visible on the notification the Admin blade polls.
        Assert.True(notification.ErrorCount > 0, "A lock-held skip was not surfaced on the notification.");
        Assert.Contains(notification.Errors, e => e != null && e.Contains("already in progress", StringComparison.OrdinalIgnoreCase));
    }

    private static ISettingsManager CreateSettingsManager()
    {
        var settings = new Mock<ISettingsManager>();
        settings
            .Setup(x => x.GetObjectSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string name, string _, string __) => new ObjectSettingEntry { Name = name });
        return settings.Object;
    }

    private sealed class RecordingIndexingManager : IIndexingManager
    {
        public bool IndexAllDocumentsCalled { get; private set; }

        public Task<IndexState> GetIndexStateAsync(string documentType) => Task.FromResult(new IndexState());
        public Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType) => Task.FromResult(Enumerable.Empty<IndexState>());

        public Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
        {
            IndexAllDocumentsCalled = true;
            return Task.CompletedTask;
        }

        public Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePushNotificationManager : IPushNotificationManager
    {
        private readonly Dictionary<string, PushNotification> _store = new();

        public void Add(PushNotification notification) => _store[notification.Id] = notification;
        public void Send(PushNotification notification) => _store[notification.Id] = notification;
        public Task SendAsync(PushNotification notification) { Send(notification); return Task.CompletedTask; }

        public PushNotificationSearchResult SearchNotifies(string userId, PushNotificationSearchCriteria criteria)
        {
            var result = new PushNotificationSearchResult();
            if (criteria?.Ids != null)
            {
                foreach (var id in criteria.Ids)
                {
                    if (id != null && _store.TryGetValue(id, out var notification))
                    {
                        result.NotifyEvents.Add(notification);
                    }
                }
            }
            result.TotalCount = result.NotifyEvents.Count;
            return result;
        }
    }
}
