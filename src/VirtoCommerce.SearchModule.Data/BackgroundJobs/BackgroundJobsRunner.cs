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
            var scheduleJobs = await _settingsManager.GetValueByDescriptorAsync<bool>(ModuleConstants.Settings.IndexingJobs.Enable);
            if (scheduleJobs)
            {
                var cronExpression = await _settingsManager.GetValueByDescriptorAsync<string>(ModuleConstants.Settings.IndexingJobs.CronExpression);
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
