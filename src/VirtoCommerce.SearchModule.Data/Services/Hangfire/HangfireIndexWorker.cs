using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Hangfire;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire;

public class HangfireIndexWorker : ScalableIndexingWorker
{
    public HangfireIndexWorker(IScalableIndexingManager scalableIndexingManager, IIndexQueueServiceFactory indexQueueServiceFactory)
        : base(scalableIndexingManager, indexQueueServiceFactory)
    {
    }

    [Queue("index_worker")]
    public Task<ScalableIndexingBatchResult> IndexDocuments(ScalableIndexingBatch batch, IJobCancellationToken cancellationToken)
    {
        return IndexDocuments(batch, new JobCancellationTokenWrapper(cancellationToken));
    }
}
