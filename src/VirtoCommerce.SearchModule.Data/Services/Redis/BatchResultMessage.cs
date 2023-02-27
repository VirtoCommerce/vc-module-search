using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public class BatchResultMessage
{
    public string InstanceId { get; set; }
    public ScalableIndexingBatchResult Value { get; set; }
}
