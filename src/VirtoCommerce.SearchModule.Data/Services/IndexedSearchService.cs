using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public abstract class IndexedSearchService<TCriteria, TResult, TModel>(
    ICrudService<TModel> crudService,
    ISearchProvider searchProvider,
    ISearchRequestBuilderRegistrar searchRequestBuilderRegistrar)
    : ISearchService<TCriteria, TResult, TModel>
    where TCriteria : SearchCriteriaBase
    where TResult : GenericSearchResult<TModel>, IHasAggregations
    where TModel : Entity, IHasRelevanceScore
{
    public abstract string DocumentType { get; }

    public virtual async Task<TResult> SearchAsync(TCriteria criteria, bool clone = true)
    {
        EnsureIndexedSearchEnabled();

        var requestBuilder = searchRequestBuilderRegistrar.GetRequestBuilderByDocumentType(DocumentType);
        var request = await requestBuilder.BuildRequestAsync(criteria);

        var response = await searchProvider.SearchAsync(DocumentType, request);

        var result = await ConvertResponseAsync(response, criteria, request);

        return result;
    }

    protected virtual void EnsureIndexedSearchEnabled()
    {
    }

    protected virtual async Task<TResult> ConvertResponseAsync(SearchResponse response, TCriteria criteria, SearchRequest searchRequest)
    {
        var result = AbstractTypeFactory<TResult>.TryCreateInstance();

        if (response != null)
        {
            result.TotalCount = (int)response.TotalCount;
            result.Results = await ConvertDocumentsAsync(response.Documents, criteria);
            result.Aggregations = await ConvertAggregationsAsync(response.Aggregations, searchRequest, criteria);
        }

        return result;
    }

    protected virtual async Task<IList<TModel>> ConvertDocumentsAsync(IList<SearchDocument> documents, TCriteria criteria)
    {
        var result = new List<TModel>();

        if (!(documents?.Count > 0))
        {
            return result;
        }

        var ids = documents.Select(x => x.Id).ToArray();
        var models = await crudService.GetAsync(ids, criteria.ResponseGroup);
        var modelsMap = models.ToDictionary(x => x.Id);

        // Preserve documents order
        foreach (var document in documents)
        {
            var model = modelsMap.GetValueOrDefault(document.Id);

            if (model != null)
            {
                model.RelevanceScore = document.GetRelevanceScore();
                result.Add(model);
            }
        }

        return result;
    }

    protected virtual async Task<IList<Aggregation>> ConvertAggregationsAsync(IList<AggregationResponse> aggregationResponses, SearchRequest searchRequest, TCriteria criteria)
    {
        var result = new List<Aggregation>();

        foreach (var aggregationRequest in searchRequest.Aggregations)
        {
            var aggregationResponse = aggregationResponses.FirstOrDefault(x => x.Id == aggregationRequest.Id);
            if (aggregationResponse != null)
            {
                Aggregation aggregation = null;

                if (aggregationRequest is RangeAggregationRequest rangeAggregationRequest)
                {
                    aggregation = new Aggregation
                    {
                        AggregationType = "range",
                        Field = aggregationRequest.FieldName,
                        Items = GetRangeAggregationItems(aggregationResponse.Values, rangeAggregationRequest, criteria),
                    };
                }
                else if (aggregationRequest is TermAggregationRequest termAggregationRequest)
                {
                    aggregation = new Aggregation
                    {
                        AggregationType = "attr",
                        Field = aggregationRequest.FieldName,
                        Items = await GetAttributeAggregationItemsAsync(aggregationResponse.Values, termAggregationRequest, criteria),
                    };
                }

                if (aggregation?.Items?.Count > 0)
                {
                    result.Add(aggregation);
                }
            }
        }

        searchRequest.SetAppliedAggregations(result);

        return result;
    }

    protected virtual List<AggregationItem> GetRangeAggregationItems(
        IList<AggregationResponseValue> values,
        RangeAggregationRequest rangeAggregationRequest,
        TCriteria criteria)
    {
        var result = new List<AggregationItem>();

        foreach (var requestValue in rangeAggregationRequest.Values)
        {
            var resultValue = values.FirstOrDefault(x => x.Id == requestValue.Id);
            if (resultValue != null)
            {
                var aggregationItem = new AggregationItem
                {
                    Value = resultValue.Id,
                    Count = (int)resultValue.Count,
                    RequestedLowerBound = requestValue.Lower,
                    RequestedUpperBound = requestValue.Upper,
                    IncludeLower = requestValue.IncludeLower,
                    IncludeUpper = requestValue.IncludeUpper,
                };

                result.Add(aggregationItem);
            }
        }

        return result;
    }

    protected virtual Task<IList<AggregationItem>> GetAttributeAggregationItemsAsync(
        IList<AggregationResponseValue> values,
        TermAggregationRequest termAggregationRequest,
        TCriteria criteria)
    {
        var result = values
            .Select(x =>
            {
                var item = new AggregationItem
                {
                    Value = x.Id,
                    Count = (int)x.Count,
                };

                return item;
            })
            .ToList();

        return Task.FromResult<IList<AggregationItem>>(result);
    }
}
