using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public class RedisIndexQueueService : IndexQueueServiceBase
{
    private static readonly string _instanceId = $"{Environment.MachineName}_{NewId()}";
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ScalableIndexingBatchResult>> _results = new();

    private readonly IConnectionMultiplexer _connection;
    private readonly RedisIndexingOptions _redisOptions;

    public RedisIndexQueueService(
        IConnectionMultiplexer connection,
        IOptions<RedisIndexingOptions> redisIndexingOptions)
    {
        _connection = connection;
        _redisOptions = redisIndexingOptions.Value;
    }

    protected ISubscriber Subscriber => _connection.GetSubscriber();
    protected IDatabase Database => _connection.GetDatabase();

    public override async Task Start()
    {
        await Subscriber.SubscribeAsync(_redisOptions.ResultsChannelName, OnResultsMessage, CommandFlags.FireAndForget);
    }

    public override async Task<string> CreateQueue(IndexingOptions options)
    {
        var queueId = await base.CreateQueue(options);
        _results[queueId] = new ConcurrentDictionary<string, ScalableIndexingBatchResult>();

        return queueId;
    }

    public override async Task DeleteQueue(string queueId)
    {
        await base.DeleteQueue(queueId);
        _results.TryRemove(queueId, out _);
    }

    public override async Task<bool> SaveBatchResult(ScalableIndexingBatchResult batchResult)
    {
        if (!SaveBatchResultToMemory(batchResult))
        {
            // Send result to other instances
            var message = new BatchResultMessage
            {
                InstanceId = _instanceId,
                Value = batchResult,
            };

            await Subscriber.PublishAsync(_redisOptions.ResultsChannelName, JsonConvert.SerializeObject(message), CommandFlags.FireAndForget);
        }

        return true;
    }

    protected override async Task SaveBatch(ScalableIndexingBatch batch)
    {
        var message = new BatchOptionsMessage
        {
            InstanceId = _instanceId,
            Value = batch,
        };

        await Database.ListLeftPushAsync(_redisOptions.QueueName, JsonConvert.SerializeObject(message), When.Always, CommandFlags.FireAndForget);
        await Subscriber.PublishAsync(_redisOptions.QueueChannelName, RedisValue.EmptyString, CommandFlags.FireAndForget);
    }

    protected override bool GetBatchResult(string queueId, string batchId, out ScalableIndexingBatchResult batchResult)
    {
        return GetBatchResultFromMemory(queueId, batchId, out batchResult);
    }

    private void OnResultsMessage(RedisChannel channel, RedisValue redisValue)
    {
        var message = JsonConvert.DeserializeObject<BatchResultMessage>(redisValue);
        SaveBatchResultToMemory(message.Value);
    }

    private bool SaveBatchResultToMemory(ScalableIndexingBatchResult batchResult)
    {
        if (_results.TryGetValue(batchResult.QueueId, out var batchResults))
        {
            batchResults[batchResult.BatchId] = batchResult;
            return true;
        }
        return false;
    }

    private bool GetBatchResultFromMemory(string queueId, string batchId, out ScalableIndexingBatchResult batchResult)
    {
        var batchResults = _results[queueId];
        return batchResults.TryGetValue(batchId, out batchResult);
    }
}
