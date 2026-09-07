using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

// Guards the answer to the recurring review question "Full index omits ReportProgress = true, so progress never
// streams." It does stream: IndexProgressHandler pushes its own IndexProgressPushNotification (the one whose id the
// producer returned to the UI) straight through IPushNotificationManager. context.Progress (the engine's IJobProgress,
// gated on ReportProgress) is a SECONDARY channel the module does not use - so a null/NoOp context.Progress, which is
// exactly what a full index gets, changes nothing about what the admin blade receives.
public class IndexProgressReportingTests
{
    [Fact]
    public void Progress_PushesNotification_WithUiNotificationId_WhenContextProgressAbsent()
    {
        // Arrange: the notification the producer created and handed to the UI (its id travels in the job payload).
        var pushManager = new RecordingPushNotificationManager();
        var notification = IndexProgressHandler.CreateNotification("admin", "notif-1");
        pushManager.Add(notification);

        var handler = new IndexProgressHandler(NullLogger<IndexProgressHandler>.Instance, pushManager);

        // context: null models a full index enqueued without ReportProgress -> context.Progress would be NoOp.
        handler.Start("admin", notificationId: "notif-1", suppressInsignificantNotifications: true, context: null);

        // Act: the indexing manager reports progress.
        handler.Progress(new IndexingProgress("Indexing Member", "Member", totalCount: 100, processedCount: 42));

        // Assert: the UI-facing notification WAS pushed, addressed by the id the UI is watching, with live counts -
        // proving progress streams without ReportProgress / context.Progress.
        var pushed = pushManager.LastSentById("notif-1");
        Assert.NotNull(pushed);
        Assert.Equal(42, pushed.ProcessedCount);
        Assert.Equal(100, pushed.TotalCount);
        Assert.Equal("Member", pushed.DocumentType);
    }

    private sealed class RecordingPushNotificationManager : IPushNotificationManager
    {
        private readonly Dictionary<string, IndexProgressPushNotification> _store = new();
        private readonly List<IndexProgressPushNotification> _sent = new();

        public void Add(PushNotification notification)
        {
            if (notification is IndexProgressPushNotification indexNotification)
            {
                _store[indexNotification.Id] = indexNotification;
            }
        }

        public void Send(PushNotification notification)
        {
            if (notification is IndexProgressPushNotification indexNotification)
            {
                _store[indexNotification.Id] = indexNotification;
                _sent.Add(indexNotification);
            }
        }

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
                foreach (var id in criteria.Ids.Where(id => id != null && _store.ContainsKey(id)))
                {
                    result.NotifyEvents.Add(_store[id]);
                }
            }

            result.TotalCount = result.NotifyEvents.Count;
            return result;
        }

        // Latest notification pushed for the given id, or null if none was pushed.
        public IndexProgressPushNotification LastSentById(string id)
            => _sent.LastOrDefault(x => x.Id == id);
    }
}
