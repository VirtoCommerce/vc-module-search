namespace VirtoCommerce.SearchModule.Core.Model;

public interface IHasAggregationKey
{
    public string Id { get; set; }
    public string AggregationKey { get; set; }
}
