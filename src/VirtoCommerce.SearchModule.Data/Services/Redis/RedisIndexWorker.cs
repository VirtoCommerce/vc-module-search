using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public class RedisIndexWorker : ScalableIndexingWorker, IRedisIndexWorker
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisIndexingOptions _options;
    private bool _processing;

    public RedisIndexWorker(
        IScalableIndexingManager scalableIndexingManager,
        IIndexQueueServiceFactory indexQueueServiceFactory,
        IConnectionMultiplexer connection,
        IOptions<RedisIndexingOptions> redisIndexingOptions)
        : base(scalableIndexingManager, indexQueueServiceFactory)
    {
        _connection = connection;
        _options = redisIndexingOptions.Value;
    }

    protected ISubscriber Subscriber => _connection.GetSubscriber();
    protected IDatabase Database => _connection.GetDatabase();

    public async Task Start()
    {
        await Subscriber.SubscribeAsync(_options.QueueChannelName, OnQueueMessage, CommandFlags.FireAndForget);
        await Task.Run(ProcessJobs);
    }

    private void OnQueueMessage(RedisChannel channel, RedisValue redisValue)
    {
        Task.Run(ProcessJobs);
    }

    private async Task ProcessJobs()
    {
        if (_processing)
        {
            return;
        }

        _processing = true;

        var queueValue = await Database.ListRightPopAsync(_options.QueueName);

        while (!queueValue.IsNullOrEmpty)
        {
            var message = JsonConvert.DeserializeObject<BatchOptionsMessage>(queueValue);
            await IndexDocuments(message.Value, new CancellationTokenWrapper(new CancellationToken()));

            queueValue = await Database.ListRightPopAsync(_options.QueueName);
        }

        _processing = false;
    }
}
