namespace VirtoCommerce.SearchModule.Data.Jobs
{
    /// <summary>
    /// Payload of the bulk delete job for a known set of document ids.
    /// </summary>
    /// <remarks>
    /// One payload and one handler serve all three priorities; the priority travels in <c>EnqueueOptions.Queue</c>.
    /// </remarks>
    public class DeleteDocumentsJobPayload
    {
        public string DocumentType { get; set; }

        public string[] DocumentIds { get; set; }
    }
}
