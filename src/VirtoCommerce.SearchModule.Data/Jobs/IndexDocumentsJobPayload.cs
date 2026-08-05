namespace VirtoCommerce.SearchModule.Data.Jobs
{
    /// <summary>
    /// Payload of the bulk index job for a known set of document ids.
    /// </summary>
    /// <remarks>
    /// One payload and one handler serve all three priorities. Where the Hangfire version needed a separate method per
    /// priority - each carrying its own [Queue] attribute - the queue is now an enqueue option, so the priority travels
    /// in <c>EnqueueOptions.Queue</c> and the handler stays single.
    /// </remarks>
    public class IndexDocumentsJobPayload
    {
        public string DocumentType { get; set; }

        public string[] DocumentIds { get; set; }

        /// <summary>Full names of the document builders to run; all configured ones when null.</summary>
        public string[] BuilderTypes { get; set; }
    }
}
