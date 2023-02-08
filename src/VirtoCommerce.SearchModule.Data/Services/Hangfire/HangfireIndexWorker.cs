using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Hangfire;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Hangfire;

public class HangfireIndexWorker
{
    private readonly IScalableIndexingManager _scalableIndexingManager;

    public HangfireIndexWorker(IScalableIndexingManager scalableIndexingManager)
    {
        _scalableIndexingManager = scalableIndexingManager;
    }

    public async Task<IndexingResult> IndexDocuments(string queueId, IndexingOptions options, IJobCancellationToken cancellationToken)
    {
        IndexingResult result;

        try
        {
            result = await _scalableIndexingManager.IndexDocuments(options, new JobCancellationTokenWrapper(cancellationToken));
        }
        catch (Exception ex)
        {
            var error = ex.Message;

            result = new()
            {
                Items = options.DocumentIds
                    .Select(x => new IndexingResultItem
                    {
                        Id = x,
                        Succeeded = false,
                        ErrorMessage = error,
                    })
                    .ToList()
            };
        }

        return result;
    }
}
