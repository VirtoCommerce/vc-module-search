using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;

namespace VirtoCommerce.SearchModule.Data.Jobs
{
    /// <summary>
    /// Runs an on-demand full indexation. Not triggerable by name: a caller-supplied payload could rebuild every index
    /// in the deployment, which is a denial-of-service in one request.
    /// </summary>
    public class IndexAllDocumentsJobHandler(IndexingJobs jobs) : IBackgroundJobHandler<IndexAllDocumentsJobPayload>
    {
        public virtual Task Execute(IndexAllDocumentsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return jobs.IndexAllDocumentsJob(payload.UserName, payload.NotificationId, payload.Options, context, cancellationToken);
        }
    }

    /// <summary>
    /// Indexes what changed since the last run. Target of the recurring schedule.
    /// </summary>
    public class IndexChangesJobHandler(IndexingJobs jobs) : IBackgroundJobHandler<IndexChangesJobPayload>
    {
        public virtual Task Execute(IndexChangesJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return jobs.IndexChangesJob(payload?.DocumentType, context, cancellationToken);
        }
    }

    /// <summary>
    /// Indexes a known set of documents. Serves all three priorities - the queue comes from the enqueue options.
    /// </summary>
    public class IndexDocumentsJobHandler(IndexingJobs jobs) : IBackgroundJobHandler<IndexDocumentsJobPayload>
    {
        public virtual Task Execute(IndexDocumentsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return jobs.IndexDocumentsAsync(payload.DocumentType, payload.DocumentIds, payload.BuilderTypes, cancellationToken);
        }
    }

    /// <summary>
    /// Deletes a known set of documents from the index. Serves all three priorities.
    /// </summary>
    public class DeleteDocumentsJobHandler(IndexingJobs jobs) : IBackgroundJobHandler<DeleteDocumentsJobPayload>
    {
        public virtual Task Execute(DeleteDocumentsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return jobs.DeleteDocumentsAsync(payload.DocumentType, payload.DocumentIds, cancellationToken);
        }
    }
}
