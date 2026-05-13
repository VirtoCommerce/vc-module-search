using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

/// <summary>
/// Used by indexing manager to get documents to be indexed
/// </summary>
public interface IIndexDocumentBuilder
{
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds)
        => throw new NotImplementedException();

    /// <summary>
    /// Cancellation-aware overload. Implementations should poll the token at their own loop boundaries
    /// (e.g. between document fetches or between paginated sub-queries) so that a Hangfire-deletion
    /// of the owning job aborts the call promptly. The default implementation delegates to the
    /// legacy overload to preserve backwards compatibility.
    /// </summary>
    Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds, CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => GetDocumentsAsync(documentIds);
#pragma warning restore VC0014 // Type or member is obsolete
}
