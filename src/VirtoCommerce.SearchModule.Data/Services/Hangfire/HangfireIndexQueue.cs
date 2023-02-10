using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire;

public class HangfireIndexQueue : IIndexQueue
{
    private readonly TimeSpan _delay = TimeSpan.FromMilliseconds(100);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();

    public virtual Task<string> NewQueue(IndexingOptions options)
    {
        var queueId = Guid.NewGuid().ToString("N");

        return Task.FromResult(queueId);
    }

    public virtual Task<string> Enqueue(string queueId, IndexingOptions options)
    {
        var cancellationToken = JobCancellationToken.Null;
        var jobId = BackgroundJob.Enqueue<HangfireIndexWorker>(x => x.IndexDocuments(queueId, options, cancellationToken));

        _queues.AddOrUpdate(
            queueId,
            _ =>
            {
                var queue = new ConcurrentQueue<string>();
                queue.Enqueue(jobId);
                return queue;
            },
            (_, queue) =>
            {
                queue.Enqueue(jobId);
                return queue;
            });

        return Task.FromResult(jobId);
    }

    public virtual async Task Wait(string queueId, ICancellationToken cancellationToken, Action<IndexingOptions, IndexingResult> callback)
    {
        var queue = _queues[queueId];

        while (queue.TryPeek(out var jobId))
        {
            var makeDelay = true;
            var stateData = JobStorage.Current.GetConnection().GetStateData(jobId);

            if (stateData != null)
            {
                if (stateData.Name == SucceededState.StateName)
                {
                    queue.TryDequeue(out _);
                    makeDelay = false;

                    var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);
                    var options = jobData.Job.Args[1] as IndexingOptions;
                    var result = GetResult<IndexingResult>(stateData);
                    callback(options, result);
                }
            }

            if (makeDelay)
            {
                await Task.Delay(_delay);
            }
        }

        _queues.TryRemove(queueId, out _);
    }

    protected static T GetResult<T>(StateData stateData)
        where T : class
    {
        T result = null;

        if (stateData.Data.TryGetValue("Result", out var resultJson))
        {
            try
            {
                result = SerializationHelper.Deserialize<T>(resultJson);
            }
            catch
            {
                // Ignore serialization errors
            }
        }

        return result;
    }
}
