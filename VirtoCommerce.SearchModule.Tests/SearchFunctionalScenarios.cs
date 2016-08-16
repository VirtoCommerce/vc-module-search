using CacheManager.Core;
using System;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Data.Services;
using VirtoCommerce.CoreModule.Data.Repositories;
using VirtoCommerce.CoreModule.Data.Services;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Services;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Domain.Search.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.ChangeLog;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Services;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.SearchModule.Web.Services;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using VirtoCommerce.Domain.Search.Filters;
using VirtoCommerce.SearchModule.Data.Model;
using System.Threading;

namespace VirtoCommerce.SearchModule.Tests
{
    [CLSCompliant(false)]
    public class SearchFunctionalScenarios : SearchTestsBase
    {
        private readonly ITestOutputHelper _output;

        public SearchFunctionalScenarios(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        public void Can_index_demo_data_and_search(string providerType)
        {
            var scope = "test";
            var provider = GetSearchProvider(providerType, scope);
            provider.RemoveAll(scope, "catalogitem");
            var controller = GetSearchIndexController(provider);
            controller.Process(scope, CatalogIndexedSearchCriteria.DocType, true);

            // sleep for index to be commited
            Thread.Sleep(5000);

            // get catalog id by name
            var catalogRepo = GetCatalogRepository();
            var catalog = catalogRepo.Catalogs.SingleOrDefault(x => x.Name.Equals("electronics", StringComparison.OrdinalIgnoreCase));

            // find all prodducts in the category
            var catalogCriteria = new CatalogIndexedSearchCriteria() { Catalog = catalog.Id, Currency = "USD" };

            // Add all filters
            var filter = new AttributeFilter { Key = "color", IsLocalized = true };
            filter.Values = new[]
                                {
                                    new AttributeFilterValue { Id = "Red", Value = "Red" },
                                    new AttributeFilterValue { Id = "Gray", Value = "Gray" },
                                    new AttributeFilterValue { Id = "Black", Value = "Black" }
                                };

            var rangefilter = new RangeFilter { Key = "size" };
            rangefilter.Values = new[]
                                     {
                                         new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                                         new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" }
                                     };

            var priceRangefilter = new PriceRangeFilter { Currency = "USD" };
            priceRangefilter.Values = new[]
                                          {
                                              new RangeFilterValue { Id = "under-100", Upper = "100" },
                                              new RangeFilterValue { Id = "200-600", Lower = "200", Upper = "600" }
                                          };

            catalogCriteria.Add(filter);
            //catalogCriteria.Add(rangefilter);
            catalogCriteria.Add(priceRangefilter);

            var ibs = GetItemBrowsingService(provider);
            var searchResults = ibs.SearchItems(scope, catalogCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.ProductsTotalCount > 0, String.Format("Didn't find any products using {0} search", providerType));
            Assert.True(searchResults.Aggregations.Count() > 0, String.Format("Didn't find any aggregations using {0} search", providerType));

            var colorAggregation = searchResults.Aggregations.SingleOrDefault(a => a.Field.Equals("color", StringComparison.OrdinalIgnoreCase));
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Red", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 6);
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Gray", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 3);
            Assert.True(colorAggregation.Items.Where(x => x.Value.ToString().Equals("Black", StringComparison.OrdinalIgnoreCase)).SingleOrDefault().Count == 13);

            //var results = provider.Search(scope, catalogCriteria);
            //_output.WriteLine(String.Format("Found {0} documents", results.DocCount));
            //Assert.True(results.DocCount > 0);            

            var keywordSearchCriteria = new KeywordSearchCriteria(CatalogIndexedSearchCriteria.DocType) { Currency = "USD", Locale = "en-us", SearchPhrase = "sony" };
            searchResults = ibs.SearchItems(scope, keywordSearchCriteria, Domain.Catalog.Model.ItemResponseGroup.ItemLarge);
            Assert.True(searchResults.ProductsTotalCount > 0);
        }

        private ItemBrowsingService GetItemBrowsingService(ISearchProvider provider)
        {
            var service = new ItemBrowsingService(GetItemService(), provider);
            return service;
        }

        private SearchIndexController GetSearchIndexController(ISearchProvider provider)
        {
            var settings = new Moq.Mock<ISettingsManager>();
            var builder = new CatalogItemIndexBuilder(provider, GetSearchService(), GetItemService(), GetPricingService(), GetChangeLogService());
            return new SearchIndexController(settings.Object, builder);
        }

        private ICommerceService GetCommerceService()
        {
            return new CommerceServiceImpl(GetCommerceRepository);
        }

        private ICatalogSearchService GetSearchService()
        {
            return new CatalogSearchServiceImpl(GetCatalogRepository, GetItemService(), GetCatalogService(), GetCategoryService());
        }

        private IOutlineService GetOutlineService()
        {
            return new OutlineService(GetCatalogRepository);
        }

        private IPricingService GetPricingService()
        {
            var cacheManager = new Moq.Mock<ICacheManager<object>>();
            return new PricingServiceImpl(GetPricingRepository, GetItemService(), null, cacheManager.Object, null);
        }

        private IPropertyService GetPropertyService()
        {
            return new PropertyServiceImpl(GetCatalogRepository);
        }

        private ICategoryService GetCategoryService()
        {
            return new CategoryServiceImpl(GetCatalogRepository, GetCommerceService(), GetOutlineService());
        }

        private ICatalogService GetCatalogService()
        {
            return new CatalogServiceImpl(GetCatalogRepository, GetCommerceService());
        }

        private IItemService GetItemService()
        {
            return new ItemServiceImpl(GetCatalogRepository, GetCommerceService(), GetOutlineService());
        }

        private IChangeLogService GetChangeLogService()
        {
            return new ChangeLogService(GetPlatformRepository);
        }

        private IPlatformRepository GetPlatformRepository()
        {
            var result = new PlatformRepository("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private IPricingRepository GetPricingRepository()
        {
            var result = new PricingRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private ICatalogRepository GetCatalogRepository()
        {
            var result = new CatalogRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private static IСommerceRepository GetCommerceRepository()
        {
            var result = new CommerceRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }
    }
}
