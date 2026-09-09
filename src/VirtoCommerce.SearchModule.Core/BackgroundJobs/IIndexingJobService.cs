using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.BackgroundJobs;

public interface IIndexingJobService
{
    [Obsolete("Use EnqueueAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
#pragma warning disable S4462 // Calls to "async" methods should not be blocking - the void/blocking signature is the legacy contract being shimmed.
    IndexProgressPushNotification Enqueue(string currentUserName, IndexingOptions[] options)
        => EnqueueAsync(currentUserName, options).GetAwaiter().GetResult();
#pragma warning restore S4462

    Task<IndexProgressPushNotification> EnqueueAsync(string currentUserName, IndexingOptions[] options, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    [Obsolete("Use EnqueueIndexAndDeleteDocumentsAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
#pragma warning disable S4462
    void EnqueueIndexAndDeleteDocuments(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null)
        => EnqueueIndexAndDeleteDocumentsAsync(indexEntries, priority, builders).GetAwaiter().GetResult();
#pragma warning restore S4462

    Task EnqueueIndexAndDeleteDocumentsAsync(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <summary>
    /// No-op since the module moved to the platform's engine-agnostic BackgroundJob API: recurring schedules are
    /// declared at module initialization, not started and stopped from here.
    /// </summary>
    [Obsolete("Recurring jobs are declared during module initialization; this method is no longer called", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task StartStopRecurringJobs() => Task.CompletedTask;

    [Obsolete("Use CancelIndexationAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
#pragma warning disable S4462
    void CancelIndexation()
        => CancelIndexationAsync().GetAwaiter().GetResult();
#pragma warning restore S4462

    Task CancelIndexationAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
