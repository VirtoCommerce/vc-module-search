namespace VirtoCommerce.SearchModule.Core.Model;

public interface IHasIndexDocumentAggregationInfo
{
    public string Id { get; set; }
    public string AggregationKey { get; set; }
}
