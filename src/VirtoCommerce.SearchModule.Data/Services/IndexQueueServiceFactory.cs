using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class IndexQueueServiceFactory : IIndexQueueServiceFactory
{
    private readonly ISettingsManager _settingsManager;
    private readonly IEnumerable<IIndexQueueService> _indexQueueServices;

    public IndexQueueServiceFactory(ISettingsManager settingsManager, IEnumerable<IIndexQueueService> indexQueueServices)
    {
        _settingsManager = settingsManager;
        _indexQueueServices = indexQueueServices;
    }

    public IIndexQueueService Create()
    {
        var selectedServiceType = _settingsManager.GetValueByDescriptor<string>(ModuleConstants.Settings.General.IndexQueueServiceType);

        return _indexQueueServices.First(x => x.GetType().Name == selectedServiceType);
    }
}
