using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model;

public class IndexDocumentAggregationGroup
{
    public IndexDocument AggregationTarget { get; set; }
    public IList<IndexDocument> Documents { get; set; }
}
