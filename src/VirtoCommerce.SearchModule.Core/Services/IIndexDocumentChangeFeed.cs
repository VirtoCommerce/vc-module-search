using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

/// <summary>
/// Stateful change feed for indexation.
/// Storage sub systems should be able to implement this efficiently, whether it be SQL or NoSQL.
/// </summary>
public interface IIndexDocumentChangeFeed
{
    /// <summary>
    /// Optional total count, feed is not required to implement this.
    /// This is only informational so that the user might have an idea how long the process will still take.
    /// </summary>
    long? TotalCount { get; }

    /// <summary>
    /// Gets the next batch with changes.
    /// </summary>
    /// <returns>Batch of changes or null when at end of feed.</returns>
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<IReadOnlyCollection<IndexDocumentChange>> GetNextBatch()
         => throw new NotImplementedException();

    /// <summary>
    /// Cancellation-aware overload. Default implementation delegates to the legacy method for backwards compatibility.
    /// </summary>
    Task<IReadOnlyCollection<IndexDocumentChange>> GetNextBatch(CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => GetNextBatch();
#pragma warning restore VC0014 // Type or member is obsolete
}
