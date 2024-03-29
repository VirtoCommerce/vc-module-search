using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extenstions
{
    [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
    public static class FilterExtensions
    {
        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter Not(this IFilter filter)
        {
            var notFilter = filter as NotFilter;

            return notFilter != null
                ? notFilter.ChildFilter
                : filter != null
                    ? new NotFilter { ChildFilter = filter }
                    : null;
        }

        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter And(this IFilter left, IFilter right)
        {
            return new[] { left, right }.And();
        }

        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter And(this IEnumerable<IFilter> filters, IFilter filter)
        {
            return new List<IFilter>(filters ?? Enumerable.Empty<IFilter>()) { filter }.And();
        }

        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter And(this IEnumerable<IFilter> allFilters)
        {
            var filters = allFilters?.Where(f => f != null).ToList();
            return filters?.Count > 1 ? new AndFilter { ChildFilters = filters } : filters?.FirstOrDefault();
        }

        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter Or(this IFilter left, IFilter right)
        {
            return new[] { left, right }.Or();
        }

        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter Or(this IEnumerable<IFilter> filters, IFilter filter)
        {
            return new List<IFilter>(filters ?? Enumerable.Empty<IFilter>()) { filter }.Or();
        }

        [Obsolete("Use VirtoCommerce.SearchModule.Core.Extensions namespace")]
        public static IFilter Or(this IEnumerable<IFilter> allFilters)
        {
            var filters = allFilters?.Where(f => f != null).ToList();
            return filters?.Count > 1 ? new OrFilter { ChildFilters = filters } : filters?.FirstOrDefault();
        }
    }
}
