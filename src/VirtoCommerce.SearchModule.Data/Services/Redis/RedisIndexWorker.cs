using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public class RedisIndexWorker : ScalableIndexingWorker, IRedisIndexWorker
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisIndexingOptions _options;
    private readonly ISettingsManager _settingsManager;
    private int _currentWorkersCount;

    public RedisIndexWorker(
        IScalableIndexingManager scalableIndexingManager,
        IIndexQueueServiceFactory indexQueueServiceFactory,
        IConnectionMultiplexer connection,
        IOptions<RedisIndexingOptions> redisIndexingOptions,
        ISettingsManager settingsManager)
        : base(scalableIndexingManager, indexQueueServiceFactory)
    {
        _connection = connection;
        _options = redisIndexingOptions.Value;
        _settingsManager = settingsManager;
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
        var maxWorkersCount = await _settingsManager.GetValueByDescriptorAsync<int>(ModuleConstants.Settings.General.MaxWorkersCount);
        var currentWorkersCount = Interlocked.Increment(ref _currentWorkersCount);

        try
        {
            if (currentWorkersCount <= maxWorkersCount)
            {
                var queueValue = await Database.ListRightPopAsync(_options.QueueName);

                while (!queueValue.IsNullOrEmpty)
                {
                    var message = JsonConvert.DeserializeObject<BatchOptionsMessage>(queueValue);
                    await IndexDocuments(message.Value, new CancellationTokenWrapper(new CancellationToken()));

                    queueValue = await Database.ListRightPopAsync(_options.QueueName);
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _currentWorkersCount);
        }
    }
}
