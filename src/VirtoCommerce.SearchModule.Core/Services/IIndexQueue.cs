using System;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public interface IIndexQueue
{
    Task<string> NewQueue(IndexingOptions options);
    Task<string> Enqueue(string queueId, IndexingOptions options);
    Task Wait(string queueId, ICancellationToken cancellationToken, Action<IndexingOptions, IndexingResult> callback);
}
