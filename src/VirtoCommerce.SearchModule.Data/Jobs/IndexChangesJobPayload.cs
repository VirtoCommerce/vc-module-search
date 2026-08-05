namespace VirtoCommerce.SearchModule.Data.Jobs
{
    /// <summary>
    /// Payload of the recurring "index what changed" job. <see cref="DocumentType"/> is null on the scheduled run,
    /// which means every configured document type.
    /// </summary>
    public class IndexChangesJobPayload
    {
        public string DocumentType { get; set; }
    }
}
