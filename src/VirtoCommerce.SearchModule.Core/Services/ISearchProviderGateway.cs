namespace VirtoCommerce.SearchModule.Core.Services;

public interface ISearchProviderGateway
{
    ISearchProvider GetSearchProvider(string documentType);
}
