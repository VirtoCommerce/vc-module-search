using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public class IndexProgressInfo
    {
        public string Description { get; set; }
        public long ErrorCount { get { return Errors.Count; } }
        public ICollection<string> Errors { get; set; }
        public long ProcessedCount { get; set; }
        public long TotalCount { get; set; }
    }
}
