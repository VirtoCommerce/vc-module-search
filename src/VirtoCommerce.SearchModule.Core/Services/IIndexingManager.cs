using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

/// <summary>
/// Responsible for the functionality of indexing
/// </summary>
public interface IIndexingManager
{
    /// <summary>
    /// Return actual index stats for specific document type
    /// </summary>
    Task<IndexState> GetIndexStateAsync(string documentType);

    /// <summary>
    /// Return actual index stats for specific document type including backup indices if the Search Providers supports blue-green indexation.
    /// </summary>
    Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType);

    /// <summary>
    /// Asynchronously indexes all documents using the specified options and reports progress through a callback.
    /// </summary>
    /// <remarks>The method processes all available documents according to the provided options. Progress
    /// updates are reported periodically if a callback is supplied. The operation can be cancelled by signaling the
    /// provided cancellation token.</remarks>
    /// <param name="options">The options that configure the indexing operation, such as filters, batch size, or indexing behavior. Cannot be
    /// null.</param>
    /// <param name="progressCallback">A callback invoked to report indexing progress. Receives updates as <see cref="IndexingProgress"/> instances.
    /// Can be null if progress reporting is not required.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the indexing operation.</param>
    /// <returns>A task that represents the asynchronous indexing operation.</returns>
    Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken);

    /// <summary>
    /// Indexing the changes in the specified time interval. If both startDate and endDate are null, indexes all available objects.
    /// </summary>
    Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken);

    /// <summary>
    /// Indexing the specified documents with given options
    /// </summary>
    Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken);

    /// <summary>
    /// Indexes a batch of documents immediately. Intended to be used by IndexingJobs.
    /// </summary>
    /// <param name="documentType">Document type to index.</param>
    /// <param name="documentIds">Ids of documents to index.</param>
    /// <param name="builderTypes">Index document builder types to process the changed documents</param>
    /// <returns>Result of indexing operation.</returns>
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<IndexingResult> IndexDocumentsAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes = null)
        => IndexDocumentsAsync(documentType, documentIds, builderTypes, CancellationToken.None);

    /// <summary>
    /// Cancellation-aware overload of <see cref="IndexDocumentsAsync(string, string[], IEnumerable{string})"/>.
    /// Default implementation delegates to the legacy overload for backwards compatibility.
    /// </summary>
    Task<IndexingResult> IndexDocumentsAsync(string documentType, string[] documentIds, IEnumerable<string> builderTypes, CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => IndexDocumentsAsync(documentType, documentIds, builderTypes);
#pragma warning restore VC0014 // Type or member is obsolete

    /// <summary>
    /// Deletes a batch of documents from the index immediately. Intended to be used by IndexingJobs.
    /// </summary>
    /// <param name="documentType">Document type to delete.</param>
    /// <param name="documentIds">Ids of documents to delete.</param>
    /// <returns>Result of indexing operation.</returns>
    [Obsolete("Use the cancellation-aware overload instead.", DiagnosticId = "VC0014", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    Task<IndexingResult> DeleteDocumentsAsync(string documentType, string[] documentIds);

    /// <summary>
    /// Cancellation-aware overload of <see cref="DeleteDocumentsAsync(string, string[])"/>.
    /// Default implementation delegates to the legacy overload for backwards compatibility.
    /// </summary>
    Task<IndexingResult> DeleteDocumentsAsync(string documentType, string[] documentIds, CancellationToken cancellationToken)
#pragma warning disable VC0014 // Type or member is obsolete
        => DeleteDocumentsAsync(documentType, documentIds);
#pragma warning restore VC0014 // Type or member is obsolete
}
