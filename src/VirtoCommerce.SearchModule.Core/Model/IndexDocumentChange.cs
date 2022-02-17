using System;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class IndexDocumentChange
    {
        public string DocumentId { get; set; }
        public DateTime ChangeDate { get; set; }
        public IndexDocumentChangeType ChangeType { get; set; }
        public IIndexDocumentChangesProvider Provider { get; set; }
    }
}
