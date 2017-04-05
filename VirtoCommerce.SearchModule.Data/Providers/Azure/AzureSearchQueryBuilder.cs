using System;
using System.Collections.Generic;
using Microsoft.Azure.Search.Models;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{
    [CLSCompliant(false)]
    public class AzureSearchQueryBuilder : ISearchQueryBuilder
    {
        public string DocumentType => string.Empty;

        public object BuildQuery<T>(string scope, ISearchCriteria criteria)
            where T : class
        {
            return new AzureSearchQuery
            {
                SearchText = GetSearchText(criteria as KeywordSearchCriteria),
                SearchParameters = GetSearchParameters(criteria),
            };
        }


        protected virtual string GetSearchText(ISearchCriteria criteria)
        {
            return (criteria as KeywordSearchCriteria)?.SearchPhrase;
        }

        protected virtual SearchParameters GetSearchParameters(ISearchCriteria criteria)
        {
            return new SearchParameters
            {
                IncludeTotalResultCount = true,
                OrderBy = GetSorting(criteria),
            };
        }

        protected virtual IList<string> GetSorting(ISearchCriteria criteria)
        {
            IList<string> result = null;

            if (criteria.Sort != null)
            {
                var fields = criteria.Sort.GetSort();

                foreach (var field in fields)
                {
                    if (result == null)
                    {
                        result = new List<string>();
                    }

                    result.Add(string.Join(" ", AzureFieldNameConverter.ToAzureFieldName(field.FieldName), field.IsDescending ? "desc" : "asc"));
                }
            }

            return result;
        }
    }
}
