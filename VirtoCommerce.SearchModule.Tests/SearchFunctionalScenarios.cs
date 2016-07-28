using CacheManager.Core;
using System;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Data.Services;
using VirtoCommerce.CoreModule.Data.Repositories;
using VirtoCommerce.CoreModule.Data.Services;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Services;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.Domain.Search.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.ChangeLog;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Services;
using VirtoCommerce.SearchModule.Data.Services;
using Xunit;
using Xunit.Abstractions;

namespace VirtoCommerce.SearchModule.Tests
{
    public class SearchFunctionalScenarios : SearchTestsBase
    {
        private readonly ITestOutputHelper _output;

        public SearchFunctionalScenarios(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SearchCatalogBuilderTest()
        {
            var scope = "test";
            var provider = GetSearchProvider("Lucene", scope);
            var controller = GetSearchIndexController(provider);
            controller.Process(scope, CatalogIndexedSearchCriteria.DocType, true);
            var results = provider.Search(scope, new CatalogIndexedSearchCriteria() { Catalog = "b61aa9d1d0024bc4be12d79bf5786e9f" });
            _output.WriteLine(String.Format("Found {0} documents", results.DocCount));
            Assert.True(results.DocCount > 0);
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
            return new PricingServiceImpl(GetPricingRepository, null, null, cacheManager.Object, null);
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
