namespace VirtoCommerce.SearchModule.Core.Services;

public interface ISearchGateway
{
    void AddSearchProvider(ISearchProvider provider, string name);
    ISearchProvider GetSearchProvider(string documentType);
}
