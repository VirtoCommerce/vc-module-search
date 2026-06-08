using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

// Regression coverage for VCST-5091:
// When a fatal exception is thrown during a MANUALLY-triggered full indexation
// (non-empty notificationId), the IndexProgressPushNotification.Finished timestamp
// must still be set so the Admin "Indexation" blade leaves the "In progress" state.
public class IndexationFinishOnErrorTests
{
    public IndexationFinishOnErrorTests()
    {
        // A real (in-memory) Hangfire storage so RunIndexJobAsync can acquire the
        // distributed "IndexationJob" lock and build a PerformContext exactly like production.
        JobStorage.Current = new MemoryStorage();
    }

    [Fact]
    public async Task ManualIndexation_WhenIndexationThrows_SealsNotificationFinished_VCST5091()
    {
        // Arrange
        var pushManager = new FakePushNotificationManager();

        // The notification the Admin blade polls is created at Enqueue time (Finished == null).
        var notification = IndexProgressHandler.CreateNotification("admin", null);
        pushManager.Add(notification);

        var handler = new IndexProgressHandler(NullLogger<IndexProgressHandler>.Instance, pushManager);

        // ISearchProvider.CreateIndexAsync / IndexingManager.IndexAllDocumentsAsync rejects the
        // index definition with a fatal error (mirrors Azure/Elastic/OpenSearch rejection).
        var indexingManager = new ThrowingIndexingManager();

        var documentConfigs = new[]
        {
            new IndexDocumentConfiguration { DocumentType = "Member" },
        };

        var jobs = new IndexingJobs(documentConfigs, indexingManager, new Mock<ISettingsManager>().Object, handler, NullLogger<IndexingJobs>.Instance);

        var options = new[] { new IndexingOptions { DocumentType = "Member" } };
        var context = CreatePerformContext();

        // Act: run the manual job (non-empty notificationId) whose indexation throws.
        await jobs.IndexAllDocumentsJob("admin", notification.Id, options, context, CancellationToken.None);

        // Assert: the blade-polled notification must be sealed (terminal state) with the error recorded.
        Assert.True(notification.Finished.HasValue,
            "IndexProgressPushNotification.Finished was never set after a fatal error on a manual indexation job — the Admin blade stays 'In progress' forever (VCST-5091).");
        Assert.True(notification.ErrorCount > 0, "Expected the fatal error to be recorded on the notification.");
    }

    // Reproduces the documented trigger: a second manual "Build new index" is enqueued while the
    // first manual job is still running on the same (reused) IndexProgressHandler DI instance.
    // The first job's notification must still end sealed.
    [Fact]
    public async Task ManualIndexation_WhenSecondJobStartsDuringFirst_FirstNotificationStillSealed_VCST5091()
    {
        // Arrange
        var pushManager = new FakePushNotificationManager();

        var firstNotification = IndexProgressHandler.CreateNotification("admin", null);
        pushManager.Add(firstNotification);

        var secondNotification = IndexProgressHandler.CreateNotification("admin", null);
        pushManager.Add(secondNotification);

        var handler = new IndexProgressHandler(NullLogger<IndexProgressHandler>.Instance, pushManager);
        var documentConfigs = new[] { new IndexDocumentConfiguration { DocumentType = "Member" } };
        var jobs = new IndexingJobs(documentConfigs, new ReentrantThrowingIndexingManager(), new Mock<ISettingsManager>().Object, handler, NullLogger<IndexingJobs>.Instance);

        IndexingManagerCallback.SecondJob = async () =>
        {
            // A second manual indexation is triggered while the first is mid-flight (same handler instance).
            var secondOptions = new[] { new IndexingOptions { DocumentType = "Member" } };
            await jobs.IndexAllDocumentsJob("admin", secondNotification.Id, secondOptions, CreatePerformContext(), CancellationToken.None);
        };

        var options = new[] { new IndexingOptions { DocumentType = "Member" } };

        // Act
        await jobs.IndexAllDocumentsJob("admin", firstNotification.Id, options, CreatePerformContext(), CancellationToken.None);

        // Assert: BOTH notifications must end sealed.
        Assert.True(secondNotification.Finished.HasValue, "Second job notification was not sealed.");
        Assert.True(firstNotification.Finished.HasValue,
            "First manual job's notification.Finished was lost because a second job reassigned the shared handler state before the first job sealed it (VCST-5091).");
    }

    private static PerformContext CreatePerformContext()
    {
        var storage = JobStorage.Current;
        var connection = storage.GetConnection();
        var backgroundJob = new BackgroundJob(Guid.NewGuid().ToString("N"), null, DateTime.UtcNow);
#pragma warning disable CS0618 // JobCancellationToken is the only public IJobCancellationToken impl available for tests
        return new PerformContext(storage, connection, backgroundJob, new JobCancellationToken(false));
#pragma warning restore CS0618
    }

    private sealed class ThrowingIndexingManager : StubIndexingManager
    {
        public override Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Index definition rejected by search provider.");
    }

    private sealed class ReentrantThrowingIndexingManager : StubIndexingManager
    {
        public override async Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
        {
            var secondJob = IndexingManagerCallback.SecondJob;
            if (secondJob != null)
            {
                IndexingManagerCallback.SecondJob = null;
                await secondJob();
            }

            throw new InvalidOperationException("Index definition rejected by search provider.");
        }
    }

    private static class IndexingManagerCallback
    {
        public static Func<Task> SecondJob;
    }

    private abstract class StubIndexingManager : IIndexingManager
    {
        public Task<IndexState> GetIndexStateAsync(string documentType) => Task.FromResult(new IndexState());
        public Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType) => Task.FromResult(Enumerable.Empty<IndexState>());
        public abstract Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken);
        public Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // A stateful test double that mirrors the real PushNotificationManager: notifications are stored
    // by Id and SearchNotifies returns the same instance, so the handler reuses it across Start() calls.
    private sealed class FakePushNotificationManager : IPushNotificationManager
    {
        private readonly Dictionary<string, PushNotification> _store = new();

        public void Add(PushNotification notification) => _store[notification.Id] = notification;

        public void Send(PushNotification notification) => _store[notification.Id] = notification;

        public Task SendAsync(PushNotification notification)
        {
            Send(notification);
            return Task.CompletedTask;
        }

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
