using System;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchModule.Data.Model.Search;

namespace VirtoCommerce.SearchModule.Web.Converters
{
    public static class AggregationConverters
    {
        public static Aggregation ToModuleModel(this FacetGroup facetGroup, params string[] appliedFilters)
        {
            var result = new Aggregation
            {
                AggregationType = facetGroup.FacetType,
                Field = facetGroup.FieldName,
                Items = facetGroup.Facets.Select(f => f.ToModuleModel(appliedFilters)).ToArray()
            };

            if (facetGroup.Labels != null)
            {
                result.Labels = facetGroup.Labels.Select(ToModuleModel).ToArray();
            }

            return result;
        }

        public static AggregationItem ToModuleModel(this Facet facet, params string[] appliedFilters)
        {
            var result = new AggregationItem
            {
                Value = facet.Key,
                Count = facet.Count,
                IsApplied = appliedFilters.Any(x => x.Equals(facet.Key, StringComparison.OrdinalIgnoreCase))
            };

            if (facet.Labels != null)
            {
                result.Labels = facet.Labels.Select(ToModuleModel).ToArray();
            }

            return result;
        }

        public static AggregationLabel ToModuleModel(this FacetLabel label)
        {
            return new AggregationLabel
            {
                Language = label.Language,
                Label = label.Label,
            };
        }
    }
}
