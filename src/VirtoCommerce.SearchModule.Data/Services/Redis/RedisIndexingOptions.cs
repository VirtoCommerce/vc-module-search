namespace VirtoCommerce.SearchModule.Data.Services.Redis;

public class RedisIndexingOptions
{
    public string QueueName { get; set; } = "VirtoCommerce-Indexing-Queue";
    public string QueueChannelName { get; set; } = "VirtoCommerce-Indexing-QueueChannel";
    public string ResultsChannelName { get; set; } = "VirtoCommerce-Indexing-ResultsChannel";
}
