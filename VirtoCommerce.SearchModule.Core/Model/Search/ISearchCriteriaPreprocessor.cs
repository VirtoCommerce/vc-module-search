namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public interface ISearchCriteriaPreprocessor
    {
        void Process(ISearchCriteria criteria);
    }
}
