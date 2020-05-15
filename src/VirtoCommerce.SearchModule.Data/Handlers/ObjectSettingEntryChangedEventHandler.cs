using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings.Events;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;

namespace VirtoCommerce.SearchModule.Data.Handlers
{
    public class ObjectSettingEntryChangedEventHandler : IEventHandler<ObjectSettingChangedEvent>
    {
        private readonly BackgroundJobsRunner _backgroundJobsRunner;

        public ObjectSettingEntryChangedEventHandler(BackgroundJobsRunner backgroundJobsRunner)
        {
            _backgroundJobsRunner = backgroundJobsRunner;
        }

        public virtual async Task Handle(ObjectSettingChangedEvent message)
        {
            if (message.ChangedEntries.Any(x => (x.EntryState == EntryState.Modified
                                              || x.EntryState == EntryState.Added)
                                  && (x.NewEntry.Name == ModuleConstants.Settings.IndexingJobs.Enable.Name
                                   || x.NewEntry.Name == ModuleConstants.Settings.IndexingJobs.CronExpression.Name)))
            {
                await _backgroundJobsRunner.StartStopIndexingJobs();
            }
        }
    }
}
