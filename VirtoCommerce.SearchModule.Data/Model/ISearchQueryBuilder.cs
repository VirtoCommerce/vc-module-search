using VirtoCommerce.Domain.Search.Model;

namespace VirtoCommerce.SearchModule.Data.Model
{
    public interface ISearchQueryBuilder
    {
        object BuildQuery<T>(string scope, ISearchCriteria criteria) where T:class;
    }
}
