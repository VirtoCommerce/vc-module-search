using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using VirtoCommerce.SearchModule.Data.Jobs;
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

            serviceCollection.AddSingleton<IIndexingJobService, IndexingJobs>();
            // The job handlers take IndexingJobs by concrete type, so it must resolve as itself too - the singleton
            // above only registers the interface.
            serviceCollection.AddSingleton(provider => (IndexingJobs)provider.GetRequiredService<IIndexingJobService>());

            // Not triggerable by name: a caller-supplied payload here would rebuild every index in the deployment.
            serviceCollection.AddBackgroundJob<IndexAllDocumentsJobHandler, IndexAllDocumentsJobPayload>(triggerable: false);
            serviceCollection.AddBackgroundJob<IndexDocumentsJobHandler, IndexDocumentsJobPayload>();
            serviceCollection.AddBackgroundJob<DeleteDocumentsJobHandler, DeleteDocumentsJobPayload>();

            // Periodic "index what changed". Declared once here instead of being added and removed at runtime by
            // StartStopRecurringJobs: the engine re-evaluates the schedule whenever either setting changes.
            serviceCollection.AddRecurringJob<IndexChangesJobHandler, IndexChangesJobPayload>(schedule => schedule
                .WithId($"{nameof(IndexingJobs)}.{nameof(IndexingJobs.IndexChangesJob)}")
                .FromSettings(
                    ModuleConstants.Settings.IndexingJobs.Enable,
                    ModuleConstants.Settings.IndexingJobs.CronExpression));

            serviceCollection.AddTransient<IIndexFieldSettingService, IndexFieldSettingService>();
            serviceCollection.AddTransient<IIndexFieldSettingSearchService, IndexFieldSettingService>();
            serviceCollection.AddTransient<IIndexDocumentConverter, FieldValueConverter>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var serviceProvider = appBuilder.ApplicationServices;

            var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            // The per-document-type "last indexation date" scratch settings have dynamic names, so register one per
            // known document type here (all IndexDocumentConfiguration are registered by now, in modules' Initialize).
            // Without this the now-strict settings manager rejects IndexingJobs' get/set of these names.
            var indexationDateSettings = serviceProvider.GetService<IEnumerable<IndexDocumentConfiguration>>()
                ?.Select(x => x.DocumentType)
                .Where(documentType => !string.IsNullOrEmpty(documentType))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(ModuleConstants.Settings.IndexingJobs.IndexationDate)
                .ToArray() ?? [];

            if (indexationDateSettings.Length != 0)
            {
                settingsRegistrar.RegisterSettings(indexationDateSettings, ModuleInfo.Id);
            }

            var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Search", ModuleConstants.Security.Permissions.AllPermissions);

            // Register fallback provider
            appBuilder.UseSearchProvider<DummySearchProvider>(name: null);
        }

        public void Uninstall()
        {
            // Method intentionally left empty.
        }
    }
}
