using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire
{
    public class HangfireRedisIndexQueueService : HangfireMemoryIndexQueueService
    {
        private bool _isSubscribed;
        private readonly IRedisIndexingClient _redisIndexingClient;

        public HangfireRedisIndexQueueService(IRedisIndexingClient redisIndexingClient)
        {
            _redisIndexingClient = redisIndexingClient;
        }

        public override async Task<string> CreateQueue(IndexingOptions options)
        {
            await EnsureSubscribed();
            return await base.CreateQueue(options);
        }

        public override async Task<bool> SaveBatchResult(ScalableBatchResult batchResult)
        {
            if (!SaveBatchResultToMemory(batchResult))
            {
                // Send result to all subscribers
                await _redisIndexingClient.Publish(batchResult);
            }

            return true;
        }

        protected override bool GetBatchResult(string queueId, string batchId, out ScalableBatchResult batchResult)
        {
            return GetBatchResultFromMemory(queueId, batchId, out batchResult);
        }

        protected virtual async Task EnsureSubscribed()
        {
            if (!_isSubscribed)
            {
                var key = CacheKey.With(GetType(), nameof(EnsureSubscribed));
                using (await AsyncLock.GetLockByKey(key).GetReleaserAsync())
                {
                    if (!_isSubscribed)
                    {
                        await _redisIndexingClient.Subscribe(OnRedisMessage);
                        _isSubscribed = true;
                    }
                }
            }
        }

        protected virtual void OnRedisMessage(ScalableBatchResult batchResult)
        {
            SaveBatchResultToMemory(batchResult);
        }
    }
}
