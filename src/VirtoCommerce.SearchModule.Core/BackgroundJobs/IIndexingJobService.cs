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
    IndexProgressPushNotification Enqueue(string currentUserName, IndexingOptions[] options) => throw new NotImplementedException();

    Task<IndexProgressPushNotification> EnqueueAsync(string currentUserName, IndexingOptions[] options, CancellationToken cancellationToken = default)
    {
#pragma warning disable VC0015 // Type or member is obsolete
        return Task.FromResult(Enqueue(currentUserName, options));
#pragma warning restore VC0015 // Type or member is obsolete
    }

    [Obsolete("Use EnqueueIndexAndDeleteDocumentsAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    void EnqueueIndexAndDeleteDocuments(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null) => throw new NotImplementedException();

    Task EnqueueIndexAndDeleteDocumentsAsync(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null, CancellationToken cancellationToken = default)
    {
#pragma warning disable VC0015 // Type or member is obsolete
        EnqueueIndexAndDeleteDocuments(indexEntries, priority, builders);
#pragma warning restore VC0015 // Type or member is obsolete
        return Task.FromResult(0);
    }

    Task StartStopRecurringJobs();

    [Obsolete("Use CancelIndexationAsync method instead", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    void CancelIndexation() => throw new NotImplementedException();

    Task CancelIndexationAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable VC0015 // Type or member is obsolete
        CancelIndexation();
#pragma warning restore VC0015 // Type or member is obsolete
        return Task.FromResult(0);
    }
}
