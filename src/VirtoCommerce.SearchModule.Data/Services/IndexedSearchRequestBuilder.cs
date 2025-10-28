using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public abstract class IndexedSearchRequestBuilder<TCriteria>(ISearchPhraseParser searchPhraseParser) : ISearchRequestBuilder
    where TCriteria : SearchCriteriaBase, IHasFacet, ICloneable
{
    public abstract string DocumentType { get; }

    public virtual Task<SearchRequest> BuildRequestAsync(SearchCriteriaBase criteria)
    {
        if (criteria is not TCriteria searchCriteria)
        {
            return Task.FromResult<SearchRequest>(null);
        }

        // GetFilters() modifies Keyword
        searchCriteria = searchCriteria.CloneTyped();

        var filter = GetFilters(searchCriteria).And();
        var aggregations = GetAggregations(searchCriteria);
        aggregations = ApplyMultiSelectFacetSearch(aggregations, filter);

        var request = new SearchRequest
        {
            SearchKeywords = searchCriteria.Keyword,
            SearchFields = [IndexDocumentExtensions.ContentFieldName],
            Filter = filter,
            Aggregations = aggregations,
            Sorting = GetSorting(searchCriteria),
            Skip = searchCriteria.Skip,
            Take = searchCriteria.Take,
        };

        return Task.FromResult(request);
    }

    protected virtual IList<IFilter> GetFilters(TCriteria criteria)
    {
        var result = new List<IFilter>();

        if (!criteria.Keyword.IsNullOrEmpty())
        {
            var parseResult = searchPhraseParser.Parse(criteria.Keyword);
            criteria.Keyword = parseResult.Keyword;
            result.AddRange(parseResult.Filters);
        }

        return result;
    }

    protected virtual IList<SortingField> GetSorting(TCriteria criteria)
    {
        var result = new List<SortingField>();

        foreach (var sortInfo in criteria.SortInfos)
        {
            var fieldName = sortInfo.SortColumn.ToLowerInvariant();
            var isDescending = sortInfo.SortDirection == SortDirection.Descending;
            result.Add(new SortingField(fieldName, isDescending));
        }

        return result;
    }

    protected virtual IList<AggregationRequest> GetAggregations(TCriteria criteria)
    {
        var result = new List<AggregationRequest>();

        if (criteria.Facet.IsNullOrEmpty())
        {
            return result;
        }

        var parseResult = searchPhraseParser.Parse(criteria.Facet);
        if (!parseResult.Keyword.IsNullOrEmpty())
        {
            var termFacetExpressions = parseResult.Keyword.Split(" ");
            parseResult.Filters.AddRange(termFacetExpressions.Select(x => new TermFilter
            {
                FieldName = x,
                Values = new List<string>(),
            }));
        }

        result = parseResult.Filters
            .Select<IFilter, AggregationRequest>(filter =>
            {
                return filter switch
                {
                    RangeFilter rangeFilter => new RangeAggregationRequest
                    {
                        Id = filter.Stringify(true),
                        FieldName = rangeFilter.FieldName,
                        Values = rangeFilter.Values.Select(x => new RangeAggregationRequestValue
                        {
                            Id = x.Stringify(),
                            Lower = x.Lower,
                            Upper = x.Upper,
                            IncludeLower = x.IncludeLower,
                            IncludeUpper = x.IncludeUpper,
                        }).ToList(),
                    },
                    TermFilter termFilter => new TermAggregationRequest
                    {
                        FieldName = termFilter.FieldName,
                        Id = filter.Stringify(),
                        Size = 0,
                    },
                    _ => null,
                };
            })
            .Where(x => x != null)
            .ToList();

        return result;
    }

    protected virtual IList<AggregationRequest> ApplyMultiSelectFacetSearch(IList<AggregationRequest> aggregations, IFilter filter)
    {
        foreach (var aggregation in aggregations)
        {
            var aggregationFilterFieldName = aggregation.FieldName ?? (aggregation.Filter as INamedFilter)?.FieldName;

            var clonedFilter = filter.Clone() as AndFilter;
            if (clonedFilter == null)
            {
                clonedFilter = new AndFilter { ChildFilters = new List<IFilter> { null } };
            }

            // For multi-select facet mechanism, we should select
            // search request filters which do not have the same
            // names such as aggregation filter
            clonedFilter.ChildFilters = clonedFilter.ChildFilters
                .Where(x =>
                {
                    var result = true;

                    if (x is INamedFilter namedFilter)
                    {
                        result = !(aggregationFilterFieldName?.StartsWith(namedFilter.FieldName, true, CultureInfo.InvariantCulture) ?? false);
                    }

                    return result;
                })
                .ToList();

            aggregation.Filter = aggregation.Filter == null ? clonedFilter : aggregation.Filter.And(clonedFilter);
        }

        return aggregations;
    }
}
