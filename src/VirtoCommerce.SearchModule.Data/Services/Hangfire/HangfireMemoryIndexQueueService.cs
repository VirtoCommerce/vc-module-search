using System.Collections.Concurrent;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire
{
    public class HangfireMemoryIndexQueueService : HangfireIndexQueueService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ScalableBatchResult>> _queues = new();

        public override async Task<string> CreateQueue(IndexingOptions options)
        {
            var queueId = await base.CreateQueue(options);
            _queues[queueId] = new ConcurrentDictionary<string, ScalableBatchResult>();
            return queueId;
        }

        public override async Task DeleteQueue(string queueId)
        {
            await base.DeleteQueue(queueId);
            _queues.TryRemove(queueId, out _);
        }

        public override Task<bool> SaveBatchResult(ScalableBatchResult batchResult)
        {
            return Task.FromResult(SaveBatchResultToMemory(batchResult));
        }

        protected virtual bool SaveBatchResultToMemory(ScalableBatchResult batchResult)
        {
            if (_queues.TryGetValue(batchResult.QueueId, out var batchResults))
            {
                batchResults[batchResult.BatchId] = batchResult;
                return true;
            }
            return false;
        }

        protected override bool GetBatchResult(string queueId, string batchId, out ScalableBatchResult batchResult)
        {
            return GetBatchResultFromMemory(queueId, batchId, out batchResult) ||
                base.GetBatchResult(queueId, batchId, out batchResult);
        }

        protected virtual bool GetBatchResultFromMemory(string queueId, string batchId, out ScalableBatchResult batchResult)
        {
            var batchResults = _queues[queueId];

            return batchResults.TryGetValue(batchId, out batchResult);
        }
    }
}
