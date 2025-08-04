using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface IIndexDocumentAggregationKeyProvider
    {
        void SetAggregationKeys(IList<IndexDocument> documents);
    }
}
