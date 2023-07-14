using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.BackgroundJobs;

public interface IIndexingJobService
{
    IndexProgressPushNotification Enqueue(string currentUserName, IndexingOptions[] options);
    void EnqueueIndexAndDeleteDocuments(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null);
    Task StartStopRecurringJobs();
    void CancelIndexation();
}
