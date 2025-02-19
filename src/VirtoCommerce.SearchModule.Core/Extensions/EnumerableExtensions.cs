using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class EnumerableExtensions
{
    public static bool ContainsIgnoreCase(this IEnumerable<string> values, string value)
    {
        return values.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
