using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface IIndexDocumentAggregator : IIndexDocumentBuilder
    {
        void AggregateDocuments(IList<IndexDocument> documents);
    }
}
