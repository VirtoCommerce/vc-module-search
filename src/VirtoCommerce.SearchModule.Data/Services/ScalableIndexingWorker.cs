using System;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class ScalableIndexingWorker
    {
        private readonly IScalableIndexingManager _scalableIndexingManager;
        private readonly IIndexQueueServiceFactory _indexQueueServiceFactory;

        public ScalableIndexingWorker(IScalableIndexingManager scalableIndexingManager, IIndexQueueServiceFactory indexQueueServiceFactory)
        {
            _scalableIndexingManager = scalableIndexingManager;
            _indexQueueServiceFactory = indexQueueServiceFactory;
        }

        public async Task<ScalableIndexingBatchResult> IndexDocuments(ScalableIndexingBatch batch, ICancellationToken cancellationToken)
        {
            var indexQueueService = _indexQueueServiceFactory.Create();

            IndexingResult result;

            try
            {
                result = await _scalableIndexingManager.IndexDocuments(batch.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                var error = ex.ToString();

                result = new()
                {
                    Items = batch.Options.DocumentIds
                        .Select(x => new IndexingResultItem
                        {
                            Id = x,
                            Succeeded = false,
                            ErrorMessage = error,
                        })
                        .ToList()
                };
            }

            var batchResult = new ScalableIndexingBatchResult
            {
                QueueId = batch.QueueId,
                BatchId = batch.BatchId,
                Options = batch.Options,
                Result = result,
            };

            await indexQueueService.SaveBatchResult(batchResult);

            return batchResult;
        }
    }
}
