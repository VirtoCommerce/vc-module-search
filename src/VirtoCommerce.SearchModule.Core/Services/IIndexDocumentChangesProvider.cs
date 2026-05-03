using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

/// <summary>
/// Used by indexing manager to find objects to be indexed.
/// </summary>
/// <remarks>
/// IMPLEMENTATION CONTRACT — please read.<br/>
/// Each pair of overloads on this interface follows the same pattern:
/// the legacy (no-token) overload is abstract, and the cancellation-aware overload has a default
/// implementation that forwards to it. This guarantees that an empty implementation
/// (<c>class Foo : IIndexDocumentChangesProvider { }</c>) does not compile, and that the default
/// forwarding cannot create infinite recursion at runtime.<br/>
/// <b>Do not</b> implement a legacy overload as a one-line forwarder to its cancellation-aware
/// counterpart unless you also override the cancellation-aware overload with a real body — that
/// pattern produces a StackOverflowException on the first call. Real work goes in one method;
/// the other is a thin forwarder.
/// </remarks>
public interface IIndexDocumentChangesProvider
{
    /// <summary>
    /// Returns total count of the changes in the given time interval. If both startDate and endDate are null, returns total count of all available objects.
    /// </summary>
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Cancellation-aware overload. Default implementation delegates to the legacy method for backwards compatibility.
    /// Override when your implementation can honour cancellation.
    /// </summary>
    Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => GetTotalChangesCountAsync(startDate, endDate);
#pragma warning restore VC0014 // Type or member is obsolete

    /// <summary>
    /// Returns IDs of objects changed in the given time interval. If both startDate and endDate are null, returns IDs of all available objects.
    /// </summary>
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take);

    /// <summary>
    /// Cancellation-aware overload. Implementations should poll the token at loop boundaries
    /// so that a Hangfire-deletion of the owning indexing job aborts the call promptly.
    /// Default implementation delegates to the legacy method for backwards compatibility.
    /// </summary>
    Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take, CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => GetChangesAsync(startDate, endDate, skip, take);
#pragma warning restore VC0014 // Type or member is obsolete
}
