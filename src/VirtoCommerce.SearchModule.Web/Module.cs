using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Core.Settings.Events;
using VirtoCommerce.SearchModule.Core;
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
            serviceCollection.AddTransient<ISearchPhraseParser, SearchPhraseParser>();
            serviceCollection.AddScoped<IIndexingWorker>(_ => null);

            serviceCollection.AddScoped<IIndexingManager, IndexingManager>();
            serviceCollection.AddScoped<IndexProgressHandler>();
            serviceCollection.AddSingleton<ISearchProvider, DummySearchProvider>();
            serviceCollection.AddSingleton<ISearchRequestBuilderRegistrar, SearchRequestBuilderRegistrar>();

            serviceCollection.AddOptions<SearchOptions>().Bind(Configuration.GetSection("Search")).ValidateDataAnnotations();

            serviceCollection.AddTransient<ObjectSettingEntryChangedEventHandler>();
            serviceCollection.AddTransient<BackgroundJobsRunner>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var serviceProvider = appBuilder.ApplicationServices;

            var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Search", ModuleConstants.Security.Permissions.AllPermissions);

            //Subscribe for Indexation job configuration changes
            var handlerRegistrar = serviceProvider.GetService<IHandlerRegistrar>();
            handlerRegistrar.RegisterHandler<ObjectSettingChangedEvent>(async (message, _) => await serviceProvider.GetService<ObjectSettingEntryChangedEventHandler>().Handle(message));

            //Schedule periodic Indexation job
            var jobsRunner = serviceProvider.GetService<BackgroundJobsRunner>();
            jobsRunner.StartStopIndexingJobs().GetAwaiter().GetResult();
        }

        public void Uninstall()
        {
            // Method intentionally left empty.
        }
    }
}
