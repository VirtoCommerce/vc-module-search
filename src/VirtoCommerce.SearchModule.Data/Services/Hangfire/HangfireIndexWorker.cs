using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Hangfire;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire;

public class HangfireIndexWorker
{
    private readonly IScalableIndexingManager _scalableIndexingManager;
    private readonly IIndexQueueServiceFactory _indexQueueServiceFactory;

    public HangfireIndexWorker(IScalableIndexingManager scalableIndexingManager, IIndexQueueServiceFactory indexQueueServiceFactory)
    {
        _scalableIndexingManager = scalableIndexingManager;
        _indexQueueServiceFactory = indexQueueServiceFactory;
    }

    [Queue("index_worker")]
    public async Task<IndexingResult> IndexDocuments(string queueId, string batchId, IndexingOptions options, IJobCancellationToken cancellationToken)
    {
        var indexQueueService = _indexQueueServiceFactory.Create();

        Console.WriteLine($">>> Worker > Start {indexQueueService.GetType().Name}");
        IndexingResult result;

        try
        {
            result = await _scalableIndexingManager.IndexDocuments(options, new JobCancellationTokenWrapper(cancellationToken));
        }
        catch (Exception ex)
        {
            var error = ex.ToString();

            result = new()
            {
                Items = options.DocumentIds
                    .Select(x => new IndexingResultItem
                    {
                        Id = x,
                        Succeeded = false,
                        ErrorMessage = error,
                    })
                    .ToList()
            };
        }

        var batchResult = new ScalableBatchResult
        {
            QueueId = queueId,
            BatchId = batchId,
            Options = options,
            Result = result,
        };

        await indexQueueService.SaveBatchResult(batchResult);

        Console.WriteLine(">>> Worker > End");
        return result;
    }
}
