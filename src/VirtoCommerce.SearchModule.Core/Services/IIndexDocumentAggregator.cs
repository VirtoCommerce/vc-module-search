using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface IIndexDocumentAggregator
    {
        void Aggregate(IndexDocumentAggregationGroup aggregationGroup);
    }
}
