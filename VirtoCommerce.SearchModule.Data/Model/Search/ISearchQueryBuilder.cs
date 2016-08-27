namespace VirtoCommerce.SearchModule.Data.Model.Search
{
    public interface ISearchQueryBuilder
    {
        object BuildQuery<T>(string scope, Criterias.ISearchCriteria criteria) where T:class;
    }
}
