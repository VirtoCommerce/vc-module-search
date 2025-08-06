using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface IIndexDocumentAggregationGroupProvider
    {
        IList<IndexDocumentAggregationGroup> GetGroups(IList<IndexDocument> documents);
    }
}
