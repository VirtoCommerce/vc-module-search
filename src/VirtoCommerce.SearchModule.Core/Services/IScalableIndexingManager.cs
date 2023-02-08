using System;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface IScalableIndexingManager
{
    Task IndexAllDocuments(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken);
    Task<IndexingResult> IndexDocuments(IndexingOptions options, ICancellationToken cancellationToken);
}
