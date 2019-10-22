using System.Configuration;
using Hangfire;
using Microsoft.Practices.Unity;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Providers.AzureSearch;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch;
using VirtoCommerce.SearchModule.Data.Providers.LuceneSearch;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing;
using VirtoCommerce.SearchModule.Web.BackgroundJobs;

namespace VirtoCommerce.SearchModule.Web
{
    public class Module : ModuleBase
    {
        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        #region IModule Members

        public override void Initialize()
        {
            base.Initialize();

            _container.RegisterType<ISearchPhraseParser, SearchPhraseParser>();
            _container.RegisterType<ISearchCriteriaPreprocessor, PhraseSearchCriteriaPreprocessor>(nameof(PhraseSearchCriteriaPreprocessor));
            _container.RegisterType<ISearchIndexController, SearchIndexController>();

            string connectionString = null;

            var configConnectionString = ConfigurationManager.ConnectionStrings["SearchConnectionString"];
            if (configConnectionString != null)
            {
                connectionString = configConnectionString.ConnectionString;
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                var settingsManager = _container.Resolve<ISettingsManager>();
                connectionString = settingsManager.GetValue("VirtoCommerce.Search.SearchConnectionString", "Lucene;server=~/App_Data/Lucene;scope=default");
            }

            var searchConnection = new SearchConnection(connectionString);
            _container.RegisterInstance<ISearchConnection>(searchConnection);

            if (searchConnection.Provider.EqualsInvariant(SearchProviders.Lucene.ToString()))
            {
                _container.RegisterType<ISearchProvider, LuceneSearchProvider>();
                _container.RegisterType<ISearchQueryBuilder, LuceneSearchQueryBuilder>(nameof(LuceneSearchQueryBuilder));
            }
            else if (searchConnection.Provider.EqualsInvariant(SearchProviders.Elasticsearch.ToString()))
            {
                _container.RegisterType<ISearchProvider, ElasticSearchProvider>(new ContainerControlledLifetimeManager());
                _container.RegisterType<ISearchQueryBuilder, ElasticSearchQueryBuilder>(nameof(ElasticSearchQueryBuilder));
            }
            else if (searchConnection.Provider.EqualsInvariant(SearchProviders.AzureSearch.ToString()))
            {
                _container.RegisterType<ISearchProvider, AzureSearchProvider>(new ContainerControlledLifetimeManager());
                _container.RegisterType<ISearchQueryBuilder, AzureSearchQueryBuilder>(nameof(AzureSearchQueryBuilder));
            }
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            // Register dynamic property for storing browsing filters
            var filteredBrowsingProperty = new DynamicProperty
            {
                Id = "2b15f370ab524186bec1ace82509a60a",
                Name = "FilteredBrowsing",
                ObjectType = typeof(Store).FullName,
                ValueType = DynamicPropertyValueType.LongText,
                CreatedBy = "Auto"
            };

            var dynamicPropertyService = _container.Resolve<IDynamicPropertyService>();
            dynamicPropertyService.SaveProperties(new[] { filteredBrowsingProperty });

            // Enable or disable periodic search index builders
            var settingsManager = _container.Resolve<ISettingsManager>();
            var scheduleJobs = settingsManager.GetValue("VirtoCommerce.Search.ScheduleJobs", false);
            if (scheduleJobs)
            {
                var cronExpression = settingsManager.GetValue("VirtoCommerce.Search.ScheduleJobsCronExpression", "0/5 * * * *");
                RecurringJob.AddOrUpdate<SearchIndexJobs>("CatalogIndexJob", x => x.Process(null, null), cronExpression);
            }
        }

        #endregion
    }
}
