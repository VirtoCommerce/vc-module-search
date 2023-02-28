using System;
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
        var serviceType = _settingsManager.GetValueByDescriptor<string>(ModuleConstants.Settings.General.IndexQueueServiceType);
        var service = _indexQueueServices.FirstOrDefault(x => x.GetType().Name == serviceType);

        return service ?? throw new InvalidOperationException($"Unknown index queue service type: {serviceType}");
    }
}
