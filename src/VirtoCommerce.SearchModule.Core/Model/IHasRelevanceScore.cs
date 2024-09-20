namespace VirtoCommerce.SearchModule.Core.Model;

public interface IHasRelevanceScore
{
    double? RelevanceScore { get; set; }
}
