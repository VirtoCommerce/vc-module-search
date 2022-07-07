using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;

namespace VirtoCommerce.SearchModule.Data.BackgroundJobs
{
    public class BackgroundJobsRunner
    {
        private readonly ISettingsManager _settingsManager;

        public BackgroundJobsRunner(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public async Task StartStopIndexingJobs()
        {
            var scheduleJobs = await _settingsManager.GetValueAsync(ModuleConstants.Settings.IndexingJobs.Enable.Name, (bool)ModuleConstants.Settings.IndexingJobs.Enable.DefaultValue);
            if (scheduleJobs)
            {
                var cronExpression = _settingsManager.GetValue(ModuleConstants.Settings.IndexingJobs.CronExpression.Name, (string)ModuleConstants.Settings.IndexingJobs.CronExpression.DefaultValue);
                RecurringJob.AddOrUpdate<IndexingJobs>(j => j.IndexChangesJob(null, null, JobCancellationToken.Null), cronExpression);
            }
            else
            {
                IndexingJobs.CancelIndexation();
                RecurringJob.RemoveIfExists("IndexingJobs.IndexChangesJob");
            }
        }
    }
}
