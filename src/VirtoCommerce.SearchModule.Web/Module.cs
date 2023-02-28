using System;
using System.Collections.Generic;
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
using VirtoCommerce.SearchModule.Data.Services.Hangfire;
using VirtoCommerce.SearchModule.Data.Services.Redis;

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

            serviceCollection.AddSingleton<IRedisIndexWorker, RedisIndexWorker>();
            serviceCollection.AddSingleton<IIndexQueueService, RedisIndexQueueService>();
            serviceCollection.AddSingleton<IIndexQueueService, HangfireIndexQueueService>();
            serviceCollection.AddSingleton<IIndexQueueServiceFactory, IndexQueueServiceFactory>();
            serviceCollection.AddSingleton<IScalableIndexingManager, ScalableIndexingManager>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var serviceProvider = appBuilder.ApplicationServices;

            var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(GetSettings(serviceProvider), ModuleInfo.Id);

            var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Search", ModuleConstants.Security.Permissions.AllPermissions);

            //Subscribe for Indexation job configuration changes
            var handlerRegistrar = serviceProvider.GetService<IHandlerRegistrar>();
            handlerRegistrar.RegisterHandler<ObjectSettingChangedEvent>(async (message, _) => await serviceProvider.GetService<ObjectSettingEntryChangedEventHandler>().Handle(message));

            //Schedule periodic Indexation job
            var jobsRunner = serviceProvider.GetService<BackgroundJobsRunner>();
            jobsRunner.StartStopIndexingJobs().GetAwaiter().GetResult();

            var redisWorker = serviceProvider.GetRequiredService<IRedisIndexWorker>();
            redisWorker.Start();

            var indexQueueServiceFactory = serviceProvider.GetRequiredService<IIndexQueueServiceFactory>();
            var queueService = indexQueueServiceFactory.Create();
            queueService.Start();
        }

        public void Uninstall()
        {
            // Method intentionally left empty.
        }


        private static IEnumerable<SettingDescriptor> GetSettings(IServiceProvider serviceProvider)
        {
            var queueServices = serviceProvider
                .GetServices<IIndexQueueService>()
                .Select(x => x.GetType().Name)
                .ToArray<object>();

            foreach (var descriptor in ModuleConstants.Settings.AllSettings)
            {
                if (descriptor.Name == ModuleConstants.Settings.General.IndexQueueServiceType.Name)
                {
                    descriptor.AllowedValues = queueServices;
                    descriptor.DefaultValue = queueServices.First();
                }

                yield return descriptor;
            }
        }
    }
}
