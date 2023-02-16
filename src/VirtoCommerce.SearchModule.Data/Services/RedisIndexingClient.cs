using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Redis;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class RedisIndexingOptions
    {
        public string ChannelName { get; set; } = "VirtoCommerceIndexingChannel";
    }

    public class RedisIndexingMessage
    {
        public string InstanceId { get; set; }
        public ScalableBatchResult BatchResult { get; set; }
    }

    public interface IRedisIndexingClient
    {
        Task Subscribe(Action<ScalableBatchResult> messageHandler);
        Task Publish(ScalableBatchResult batchResult);
    }

    public class RedisIndexingClient : IRedisIndexingClient, IDisposable
    {
        private static readonly string _instanceId = $"{Environment.MachineName}_{Guid.NewGuid():N}";
        private readonly ConcurrentBag<Action<ScalableBatchResult>> _messageHandlers = new();
        private bool _isSubscribed;
        private bool _disposed;

        private readonly IConnectionMultiplexer _connection;
        private readonly ISubscriber _subscriber;
        private readonly RedisIndexingOptions _redisIndexingOptions;
        private readonly ILogger _log;

        public RedisIndexingClient(
            IConnectionMultiplexer connection,
            ISubscriber subscriber,
            IOptions<RedisIndexingOptions> redisIndexingOptions,
            ILogger<RedisPlatformMemoryCache> log)
        {
            _connection = connection;
            _subscriber = subscriber;
            _redisIndexingOptions = redisIndexingOptions.Value;
            _log = log;
        }

        public async Task Subscribe(Action<ScalableBatchResult> messageHandler)
        {
            await EnsureRedisServerConnection();
            _messageHandlers.Add(messageHandler);
        }

        public async Task Publish(ScalableBatchResult batchResult)
        {
            var message = new RedisIndexingMessage
            {
                InstanceId = _instanceId,
                BatchResult = batchResult,
            };

            await EnsureRedisServerConnection();
            await _subscriber.PublishAsync(_redisIndexingOptions.ChannelName, JsonConvert.SerializeObject(message), CommandFlags.FireAndForget);
            _log.LogTrace("Published message to Redis: {Message}", message);
        }


        private async Task EnsureRedisServerConnection()
        {
            if (!_isSubscribed)
            {
                var key = CacheKey.With(GetType(), nameof(EnsureRedisServerConnection));
                using (await AsyncLock.GetLockByKey(key).GetReleaserAsync())
                {
                    if (!_isSubscribed)
                    {
                        _connection.ConnectionFailed += OnConnectionFailed;
                        _connection.ConnectionRestored += OnConnectionRestored;

                        await _subscriber.SubscribeAsync(_redisIndexingOptions.ChannelName, OnMessage, CommandFlags.FireAndForget);
                        _isSubscribed = true;

                        _log.LogTrace("Successfully subscribed to Redis channel '{ChannelName}' with instance id '{InstanceId}'",
                            _redisIndexingOptions.ChannelName, _instanceId);
                    }
                }
            }
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _log.LogError("Redis connection failed for instance '{InstanceId}'. Endpoint is '{EndPoint}', failure type is '{FailureType}'",
                _instanceId, e.EndPoint, e.FailureType);
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _log.LogTrace("Redis connection restored for instance '{InstanceId}'", _instanceId);
        }

        private void OnMessage(RedisChannel channel, RedisValue redisValue)
        {
            var message = JsonConvert.DeserializeObject<RedisIndexingMessage>(redisValue);
            _log.LogTrace("Received message from Redis: {Message}", message.ToString());

            if (!string.IsNullOrEmpty(message.InstanceId) && !message.InstanceId.EqualsInvariant(_instanceId))
            {
                // Call subscribers
                foreach (var messageHandler in _messageHandlers)
                {
                    messageHandler(message.BatchResult);
                }
            }
        }

        ~RedisIndexingClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _subscriber.Unsubscribe(_redisIndexingOptions.ChannelName, null, CommandFlags.FireAndForget);
                    _connection.ConnectionFailed -= OnConnectionFailed;
                    _connection.ConnectionRestored -= OnConnectionRestored;
                }
                _disposed = true;
            }
        }
    }
}
