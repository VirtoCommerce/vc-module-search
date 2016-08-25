using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Search.Model;
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
            var items = new List<moduleModel.CatalogProduct>();
            var itemsOrderedList = new List<string>();

            var foundItemCount = 0;
            var dbItemCount = 0;
            var searchRetry = 0;

            //var myCriteria = criteria.Clone();
            var myCriteria = criteria;

            ISearchResults<DocumentDictionary> searchResults = null;

            do
            {
                // Search using criteria, it will only return IDs of the items
                searchResults = _searchProvider.Search<DocumentDictionary>(scope, criteria);
                //var itemKeyValues = searchResults.GetKeyAndOutlineFieldValueMap<string>();
                //results = new CatalogItemSearchResults(myCriteria, itemKeyValues, searchResults);

                searchRetry++;

                if (searchResults.Documents == null)
                {
                    continue;
                }

                //Get only new found itemIds
                var uniqueKeys = searchResults.Documents.Select(x=>x.Id.ToString()).Except(itemsOrderedList).ToArray();
                foundItemCount = uniqueKeys.Length;

                if (!searchResults.Documents.Any())
                {
                    continue;
                }

                itemsOrderedList.AddRange(uniqueKeys);

                // if we can determine catalog, pass it to the service
                string catalog = null;
                if (criteria is Data.Model.CatalogIndexedSearchCriteria)
                {
                    catalog = (criteria as Data.Model.CatalogIndexedSearchCriteria).Catalog;
                }

                // Now load items from repository
                var currentItems = _itemService.GetByIds(uniqueKeys.ToArray(), responseGroup, catalog);

                var orderedList = currentItems.OrderBy(i => itemsOrderedList.IndexOf(i.Id));
                items.AddRange(orderedList);
                dbItemCount = currentItems.Length;

                //If some items where removed and search is out of sync try getting extra items
                if (foundItemCount > dbItemCount)
                {
                    //Retrieve more items to fill missing gap
                    myCriteria.RecordsToRetrieve += (foundItemCount - dbItemCount);
                }
            }
            while (foundItemCount > dbItemCount && searchResults!=null && searchResults.Documents.Any() && searchRetry <= 3 &&
                (myCriteria.RecordsToRetrieve + myCriteria.StartingRecord) < searchResults.TotalCount);

            var response = new moduleModel.SearchResult();

            response.Products.AddRange(items);
            response.ProductsTotalCount = (int)searchResults.TotalCount;

            // TODO need better way to find applied filter values
            var appliedFilters = criteria.CurrentFilters.SelectMany(x => x.GetValues()).Select(x => x.Id).ToArray();
            if (searchResults.Facets != null)
            {
                response.Aggregations = searchResults.Facets.Select(g => g.ToModuleModel(appliedFilters)).ToArray();
            }
            return response;
        }
    }
}
