using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public abstract class IndexQueueServiceBase : IIndexQueueService
{
    private readonly TimeSpan _waitDelay = TimeSpan.FromMilliseconds(100);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();

    public virtual Task<string> CreateQueue(IndexingOptions options)
    {
        var queueId = NewId();
        var queue = new ConcurrentQueue<string>();
        _queues[queueId] = queue;

        return Task.FromResult(queueId);
    }

    public virtual Task DeleteQueue(string queueId)
    {
        _queues.TryRemove(queueId, out _);

        return Task.CompletedTask;
    }

    public virtual async Task<string> Enqueue(string queueId, IndexingOptions options)
    {
        var queue = _queues[queueId];
        var batchId = NewId();
        await CreateBatch(queueId, batchId, options);
        queue.Enqueue(batchId);

        return batchId;
    }

    public abstract Task<bool> SaveBatchResult(ScalableBatchResult batchResult);

    public virtual async Task Wait(string queueId, ICancellationToken cancellationToken, Action<ScalableBatchResult> callback)
    {
        while (_queues.TryGetValue(queueId, out var queue) &&
               queue.TryPeek(out var batchId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (GetBatchResult(queueId, batchId, out var batchResult))
            {
                queue.TryDequeue(out _);
                callback(batchResult);
            }
            else
            {
                await Task.Delay(_waitDelay);
            }
        }
    }

    protected static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }

    protected abstract Task CreateBatch(string queueId, string batchId, IndexingOptions options);
    protected abstract bool GetBatchResult(string queueId, string batchId, out ScalableBatchResult batchResult);
}
