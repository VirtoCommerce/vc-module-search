using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public class IndexProgressInfo
    {
        public string Description { get; set; }
        public long ErrorCount { get; }
        public ICollection<string> Errors { get; set; }
        public long ProcessedCount { get; set; }
        public long TotalCount { get; set; }
    }
}
