namespace VirtoCommerce.SearchModule.Data.Model.Filters
{
    public interface ISearchFilter
    {
        string Key { get; }

        string CacheKey { get; }
    }
}
