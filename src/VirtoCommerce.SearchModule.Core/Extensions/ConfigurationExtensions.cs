using System.Linq;
using Microsoft.Extensions.Configuration;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class ConfigurationExtensions
{
    public static bool SearchProviderActive(this IConfiguration configuration, string name)
    {
        var options = configuration.GetSection("Search").Get<SearchOptions>();

        if (options is null)
        {
            return false;
        }

        return options.Provider.EqualsIgnoreCase(name) ||
               options.DocumentScopes.Any(x => x.Provider.EqualsIgnoreCase(name));
    }
}
