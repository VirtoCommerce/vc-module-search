using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Core.Settings.Events;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using VirtoCommerce.SearchModule.Data.Handlers;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using VirtoCommerce.SearchModule.Data.Services;

namespace VirtoCommerce.SearchModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DummySearchProvider>();
            serviceCollection.AddSingleton<SearchGateway>();
            serviceCollection.AddSingleton<ISearchGateway>(serviceProvider => serviceProvider.GetService<SearchGateway>());
            serviceCollection.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetService<SearchGateway>());

            serviceCollection.AddTransient<ISearchPhraseParser, SearchPhraseParser>();

            serviceCollection.AddSingleton<IIndexingManager, IndexingManager>();
            serviceCollection.AddTransient<IndexProgressHandler>();
            serviceCollection.AddSingleton<ISearchRequestBuilderRegistrar, SearchRequestBuilderRegistrar>();

            serviceCollection.AddOptions<SearchOptions>().Bind(Configuration.GetSection("Search")).ValidateDataAnnotations();

            serviceCollection.AddSingleton<ObjectSettingEntryChangedEventHandler>();
            serviceCollection.AddSingleton<IIndexingJobService, IndexingJobs>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var serviceProvider = appBuilder.ApplicationServices;

            var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Search", ModuleConstants.Security.Permissions.AllPermissions);

            // Register fallback provider
            appBuilder.UseSearchProvider<DummySearchProvider>(name: null);

            // Subscribe for Indexation job configuration changes
            appBuilder.RegisterEventHandler<ObjectSettingChangedEvent, ObjectSettingEntryChangedEventHandler>();

            // Schedule periodic Indexation job
            var indexingJobService = serviceProvider.GetService<IIndexingJobService>();
            indexingJobService.StartStopRecurringJobs().GetAwaiter().GetResult();
        }

        public void Uninstall()
        {
            // Method intentionally left empty.
        }
    }
}
