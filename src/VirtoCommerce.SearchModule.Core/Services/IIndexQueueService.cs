using System;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public class ScalableBatchResult
{
    public string QueueId { get; set; }
    public string BatchId { get; set; }
    public IndexingOptions Options { get; set; }
    public IndexingResult Result { get; set; }
}

public interface IIndexQueueService
{
    Task<string> CreateQueue(IndexingOptions options);
    Task DeleteQueue(string queueId);
    Task<string> Enqueue(string queueId, IndexingOptions options);
    Task<bool> SaveBatchResult(ScalableBatchResult batchResult);
    Task Wait(string queueId, ICancellationToken cancellationToken, Action<ScalableBatchResult> callback);
}
