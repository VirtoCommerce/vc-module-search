using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class ApplicationBuilderExtensions
{
    public static T UseSearchProvider<T>(this IApplicationBuilder applicationBuilder, string name)
        where T : ISearchProvider
    {
        var serviceProvider = applicationBuilder.ApplicationServices;
        var gateway = serviceProvider.GetRequiredService<ISearchGateway>();
        var provider = serviceProvider.GetRequiredService<T>();
        gateway.AddSearchProvider(provider, name);

        return provider;
    }
}
