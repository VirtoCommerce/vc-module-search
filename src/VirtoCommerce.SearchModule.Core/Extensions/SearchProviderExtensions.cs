using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class SearchProviderExtensions
{
    public static string GetProviderName(this ISearchProvider provider, string documentType, string defaultValue)
    {
        return provider is ISearchGateway gateway
            ? gateway.GetSearchProvider(documentType).GetType().Name
            : defaultValue;
    }

    public static bool Is<T>(this ISearchProvider provider, string documentType)
    {
        if (provider is ISearchGateway gateway)
        {
            provider = gateway.GetSearchProvider(documentType);
        }

        return provider is T;
    }

    public static bool Is<T>(this ISearchProvider provider, string documentType, out T extendedProvider)
    {
        if (provider is ISearchGateway gateway)
        {
            provider = gateway.GetSearchProvider(documentType);
        }

        if (provider is T t)
        {
            extendedProvider = t;
            return true;
        }

        extendedProvider = default;
        return false;
    }
}
