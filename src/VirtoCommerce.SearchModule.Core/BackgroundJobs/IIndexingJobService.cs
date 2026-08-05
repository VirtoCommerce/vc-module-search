using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.BackgroundJobs;

public interface IIndexingJobService
{
    /// <summary>
    /// Starts a full indexation run and returns the notification its progress is reported against.
    /// </summary>
    /// <remarks>
    /// Returns a Task now: enqueuing is asynchronous. Breaking for callers compiled against the previous
    /// synchronous signature.
    /// </remarks>
    Task<IndexProgressPushNotification> Enqueue(string currentUserName, IndexingOptions[] options);

    /// <summary>
    /// Queues index/delete work for the supplied entries.
    /// </summary>
    /// <param name="indexEntries">What to index or delete.</param>
    /// <param name="priority">Target queue - see <see cref="JobPriority"/>. Maps to <c>EnqueueOptions.Queue</c>.</param>
    /// <param name="builders">Document builders to restrict the run to; all configured ones when null.</param>
    /// <remarks>
    /// Returns a Task now: enqueuing is asynchronous. Breaking for callers compiled against the previous
    /// synchronous signature - vc-module-catalog, -pricing, -inventory and -order all call this.
    /// </remarks>
    Task EnqueueIndexAndDeleteDocuments(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null);

    /// <summary>
    /// Applies the current indexing-schedule settings. The schedule itself is declared once at startup and the engine
    /// re-evaluates it whenever either setting changes; this call additionally stops a run already in flight when
    /// indexing is switched off.
    /// </summary>
    Task StartStopRecurringJobs();

    /// <summary>
    /// Cancels the indexation currently in progress, if any.
    /// </summary>
    /// <remarks>
    /// Returns a Task now, and is best-effort: cancellation is engine-dependent. Check
    /// <c>BackgroundJob.SupportsCancellation</c> before offering it in a UI.
    /// </remarks>
    Task CancelIndexation();
}
