namespace VirtoCommerce.SearchModule.Core.Services;

public interface ISearchProviderGateway
{
    void AddSearchProvider(ISearchProvider provider, string name);
    ISearchProvider GetSearchProvider(string documentType);
}
