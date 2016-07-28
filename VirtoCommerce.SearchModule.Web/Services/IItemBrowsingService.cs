using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Search.Model;

namespace VirtoCommerce.SearchModule.Web.Services
{
    public interface IItemBrowsingService
    {
        SearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup);
    }
}
