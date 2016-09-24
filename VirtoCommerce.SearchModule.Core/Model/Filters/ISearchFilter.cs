namespace VirtoCommerce.SearchModule.Core.Model.Filters
{
    public interface ISearchFilter
    {
        string Key { get; }

        string CacheKey { get; }
    }
}
