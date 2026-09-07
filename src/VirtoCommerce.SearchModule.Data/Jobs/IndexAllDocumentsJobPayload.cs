using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Data.Jobs
{
    /// <summary>
    /// Payload of the on-demand full indexation job.
    /// </summary>
    public class IndexAllDocumentsJobPayload
    {
        public string UserName { get; set; }

        /// <summary>Id of the notification the run reports progress against; the job re-reads it from the store.</summary>
        public string NotificationId { get; set; }

        public IndexingOptions[] Options { get; set; }
    }
}
