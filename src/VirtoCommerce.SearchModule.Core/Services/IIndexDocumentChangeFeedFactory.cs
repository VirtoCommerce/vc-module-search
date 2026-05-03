using System;
using System.Threading;
using System.Threading.Tasks;

namespace VirtoCommerce.SearchModule.Core.Services;

/// <summary>
/// Allows creating a stateful change feed as a source for the indexation process.
/// </summary>
public interface IIndexDocumentChangeFeedFactory
{
    /// <summary>
    /// Creates the change feed.
    /// </summary>
    /// <param name="startDate">Start date as a filter for the changes.</param>
    /// <param name="endDate">End date as a filter for the changes.</param>
    /// <param name="batchSize">Size of the batches to use.</param>
    /// <returns>Created feed, never null.</returns>
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<IIndexDocumentChangeFeed> CreateFeed(DateTime? startDate, DateTime? endDate, int batchSize);

    /// <summary>
    /// Cancellation-aware overload. Default implementation delegates to the legacy method for backwards compatibility.
    /// </summary>
    Task<IIndexDocumentChangeFeed> CreateFeed(DateTime? startDate, DateTime? endDate, int batchSize, CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => CreateFeed(startDate, endDate, batchSize);
#pragma warning restore VC0014 // Type or member is obsolete
}
