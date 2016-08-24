using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.Domain.Search.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.SearchModule.Web.Converters;
using moduleModel = VirtoCommerce.Domain.Catalog.Model;

namespace VirtoCommerce.SearchModule.Web.Services
{
    public class ItemBrowsingService : IItemBrowsingService
    {
        private readonly IItemService _itemService;
        private readonly Data.Model.ISearchProvider _searchProvider;

        public ItemBrowsingService(IItemService itemService, Data.Model.ISearchProvider searchService)
        {
            _searchProvider = searchService;
            _itemService = itemService;
        }

        public moduleModel.SearchResult SearchItems(string scope, ISearchCriteria criteria, moduleModel.ItemResponseGroup responseGroup)
        {
            CatalogItemSearchResults results;
            var items = Search(scope, criteria, out results, responseGroup);

            var response = new moduleModel.SearchResult();

            response.Products.AddRange(items);
            response.ProductsTotalCount = results.TotalCount;

            // TODO need better way to find applied filter values
            var appliedFilters = criteria.CurrentFilters.SelectMany(x => x.GetValues()).Select(x => x.Id).ToArray();
            if (results.FacetGroups != null)
            {
                response.Aggregations = results.FacetGroups.Select(g => g.ToModuleModel(appliedFilters)).ToArray();
            }
            return response;
        }

        private IEnumerable<moduleModel.CatalogProduct> Search(string scope, ISearchCriteria criteria, out CatalogItemSearchResults results, moduleModel.ItemResponseGroup responseGroup)
        {
            results = null;
            return null;
            //var items = new List<moduleModel.CatalogProduct>();
            //var itemsOrderedList = new List<string>();

            //var foundItemCount = 0;
            //var dbItemCount = 0;
            //var searchRetry = 0;

            ////var myCriteria = criteria.Clone();
            //var myCriteria = criteria;

            //do
            //{
            //    // Search using criteria, it will only return IDs of the items
            //    var searchResults = _searchProvider.Search(scope, criteria) as SearchResults;
            //    var itemKeyValues = searchResults.GetKeyAndOutlineFieldValueMap<string>();
            //    results = new CatalogItemSearchResults(myCriteria, itemKeyValues, searchResults);

            //    searchRetry++;

            //    if (results.Items == null)
            //    {
            //        continue;
            //    }

            //    //Get only new found itemIds
            //    var uniqueKeys = results.Items.Keys.Except(itemsOrderedList).ToArray();
            //    foundItemCount = uniqueKeys.Length;

            //    if (!results.Items.Any())
            //    {
            //        continue;
            //    }

            //    itemsOrderedList.AddRange(uniqueKeys);

            //    // if we can determine catalog, pass it to the service
            //    string catalog = null;
            //    if(criteria is Data.Model.CatalogIndexedSearchCriteria)
            //    {
            //        catalog = (criteria as Data.Model.CatalogIndexedSearchCriteria).Catalog;
            //    }

            //    // Now load items from repository
            //    var currentItems = _itemService.GetByIds(uniqueKeys.ToArray(), responseGroup, catalog);

            //    var orderedList = currentItems.OrderBy(i => itemsOrderedList.IndexOf(i.Id));
            //    items.AddRange(orderedList);
            //    dbItemCount = currentItems.Length;

            //    //If some items where removed and search is out of sync try getting extra items
            //    if (foundItemCount > dbItemCount)
            //    {
            //        //Retrieve more items to fill missing gap
            //        myCriteria.RecordsToRetrieve += (foundItemCount - dbItemCount);
            //    }
            //}
            //while (foundItemCount > dbItemCount && results.Items.Any() && searchRetry <= 3 &&
            //    (myCriteria.RecordsToRetrieve + myCriteria.StartingRecord) < results.TotalCount);

            //return items;
        }
    }
}
