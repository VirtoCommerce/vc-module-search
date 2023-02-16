using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire;

public class HangfireIndexQueueService : IndexQueueServiceBase
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _queues = new();

    public override async Task<string> CreateQueue(IndexingOptions options)
    {
        var queueId = await base.CreateQueue(options);
        _queues[queueId] = new ConcurrentDictionary<string, string>();

        return queueId;
    }

    public override async Task DeleteQueue(string queueId)
    {
        await base.DeleteQueue(queueId);
        _queues.TryRemove(queueId, out _);
    }

    public override Task<bool> SaveBatchResult(ScalableBatchResult batchResult)
    {
        // Result is stored by Hangfire
        return Task.FromResult(_queues.ContainsKey(batchResult.QueueId));
    }

    protected override Task CreateBatch(string queueId, string batchId, IndexingOptions options)
    {
        var jobIdsByBatchIds = _queues[queueId];
        var cancellationToken = JobCancellationToken.Null;
        var jobId = BackgroundJob.Enqueue<HangfireIndexWorker>(x => x.IndexDocuments(queueId, batchId, options, cancellationToken));
        jobIdsByBatchIds[batchId] = jobId;

        return Task.CompletedTask;
    }

    protected override bool GetBatchResult(string queueId, string batchId, out ScalableBatchResult batchResult)
    {
        var success = false;
        batchResult = null;

        var jobIdsByBatchIds = _queues[queueId];
        var jobId = jobIdsByBatchIds[batchId];
        var stateData = JobStorage.Current.GetConnection().GetStateData(jobId);

        if (stateData?.Name == SucceededState.StateName)
        {
            var jobData = JobStorage.Current.GetConnection().GetJobData(jobId);
            var options = jobData.Job.Args[2] as IndexingOptions;
            var result = GetResult<IndexingResult>(stateData);

            batchResult = new ScalableBatchResult
            {
                QueueId = queueId,
                BatchId = batchId,
                Options = options,
                Result = result,
            };

            success = true;
        }

        return success;
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
