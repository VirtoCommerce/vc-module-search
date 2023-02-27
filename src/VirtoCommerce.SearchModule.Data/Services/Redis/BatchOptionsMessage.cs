using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public class BatchOptionsMessage
{
    public string InstanceId { get; set; }
    public ScalableIndexingBatch Value { get; set; }
}
