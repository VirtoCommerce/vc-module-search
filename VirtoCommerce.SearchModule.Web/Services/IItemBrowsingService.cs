using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;
using VirtoCommerce.Domain.Catalog.Model;

namespace VirtoCommerce.SearchModule.Web.Services
{
    public interface IItemBrowsingService
    {
        SearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup);
    }
}
