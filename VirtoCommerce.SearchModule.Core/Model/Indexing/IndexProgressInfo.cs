using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public class IndexProgressInfo
    {
        public IndexProgressInfo()
        {
            Errors = new List<string>();
        }
        public string Description { get; set; }
        public long ErrorCount => Errors?.Count ?? 0;
        public ICollection<string> Errors { get; set; }
        public long ProcessedCount { get; set; }
        public long TotalCount { get; set; }
    }
}
