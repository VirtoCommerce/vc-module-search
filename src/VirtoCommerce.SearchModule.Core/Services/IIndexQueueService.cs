using System;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services;

public class ScalableIndexingBatch
{
    public string QueueId { get; set; }
    public string BatchId { get; set; }
    public IndexingOptions Options { get; set; }
}

public class ScalableIndexingBatchResult
{
    public string QueueId { get; set; }
    public string BatchId { get; set; }
    public IndexingOptions Options { get; set; }
    public IndexingResult Result { get; set; }
}

public interface IIndexQueueService
{
    Task Start();
    Task<string> CreateQueue(IndexingOptions options);
    Task DeleteQueue(string queueId);
    Task<string> CreateBatch(string queueId, IndexingOptions options);
    Task<bool> SaveBatchResult(ScalableIndexingBatchResult batchResult);
    Task Wait(string queueId, ICancellationToken cancellationToken, Action<ScalableIndexingBatchResult> callback);
}
