using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;
using Version = Lucene.Net.Util.Version;

namespace VirtoCommerce.SearchModule.Data.Providers.LuceneSearch
{
    public class LuceneSearchQueryBuilder : ISearchQueryBuilder
    {
        public virtual string DocumentType => string.Empty;

        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="availableFields"></param>
        /// <returns></returns>
        public virtual object BuildQuery<T>(string scope, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
            where T : class
        {
            var result = new LuceneSearchQuery
            {
                Query = GetQuery(criteria),
                Filter = GetFilters(criteria, availableFields),
            };

            return result;
        }


        protected virtual BooleanQuery GetQuery(ISearchCriteria criteria)
        {
            var query = new BooleanQuery();

            AddIdsQuery(criteria, query);
            AddRawQuery(criteria, query);
            AddKeywordQuery(criteria, query);

            return query;
        }

        [CLSCompliant(false)]
        protected virtual BooleanFilter GetFilters(ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            var filter = new BooleanFilter();

            ApplyCurrentFilters(criteria, filter, availableFields);

            return filter;
        }

        protected virtual void AddIdsQuery(ISearchCriteria criteria, BooleanQuery query)
        {
            if (criteria?.Ids != null && criteria.Ids.Any())
            {
                AddQuery("__key", query, criteria.Ids);
            }
        }

        [CLSCompliant(false)]
        protected virtual void ApplyCurrentFilters(ISearchCriteria criteria, BooleanFilter queryFilter, IList<IFieldDescriptor> availableFields)
        {
            if (criteria.CurrentFilters != null)
            {
                foreach (var filter in criteria.CurrentFilters)
                {
                    if (!string.IsNullOrEmpty(criteria.Currency))
                    {
                        // Skip price range filters with currencies not equal to criteria currency
                        var priceRangeFilter = filter as PriceRangeFilter;
                        if (priceRangeFilter != null && !priceRangeFilter.Currency.EqualsInvariant(criteria.Currency))
                        {
                            continue;
                        }
                    }

                    var filterQuery = LuceneSearchHelper.CreateQuery(criteria, filter, Occur.SHOULD, availableFields);

                    // now add other values that should also be counted?

                    if (filterQuery != null)
                    {
                        var clause = new FilterClause(filterQuery, Occur.MUST);
                        queryFilter.Add(clause);
                    }
                }
            }
        }

        protected virtual void AddRawQuery(ISearchCriteria criteria, BooleanQuery query)
        {
            if (!string.IsNullOrEmpty(criteria.RawQuery))
            {
                const Version matchVersion = Version.LUCENE_30;
                var analyzer = new StandardAnalyzer(matchVersion);

                var parser = new QueryParser(matchVersion, "__content", analyzer)
                {
                    DefaultOperator = QueryParser.Operator.AND
                };
                var parsedQuery = parser.Parse(criteria.RawQuery);
                query.Add(parsedQuery, Occur.MUST);
            }
        }

        protected virtual void AddKeywordQuery(ISearchCriteria criteria, BooleanQuery query)
        {
            if (!string.IsNullOrEmpty(criteria?.SearchPhrase))
            {
                var searchPhrase = criteria.SearchPhrase;
                if (criteria.IsFuzzySearch)
                {
                    const float fuzzyMinSimilarity = 0.7f;
                    var keywords = criteria.SearchPhrase.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    searchPhrase = string.Empty;
                    searchPhrase = keywords.Aggregate(
                        searchPhrase,
                        (current, keyword) =>
                            current + $"{keyword.Replace("~", "")}~{fuzzyMinSimilarity.ToString(CultureInfo.InvariantCulture)}");
                }

                var fields = new List<string> { "__content" };
                if (criteria.Locale != null)
                {
                    var contentField = $"__content_{criteria.Locale.ToLowerInvariant()}";
                    fields.Add(contentField);
                }

                const Version matchVersion = Version.LUCENE_30;
                var analyzer = new StandardAnalyzer(matchVersion);

                var parser = new MultiFieldQueryParser(matchVersion, fields.ToArray(), analyzer)
                {
                    DefaultOperator = QueryParser.Operator.AND
                };

                var searchQuery = parser.Parse(searchPhrase);
                query.Add(searchQuery, Occur.MUST);
            }
        }

        /// <summary>
        ///     Adds the query.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="query">The query.</param>
        /// <param name="values">The values.</param>
        protected virtual void AddQuery(string fieldName, BooleanQuery query, IList<string> values)
        {
            if (values.Count > 0)
            {
                fieldName = fieldName.ToLowerInvariant();

                if (values.Count > 1)
                {
                    var booleanQuery = new BooleanQuery();
                    var containsFilter = false;

                    foreach (var value in values)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            var nodeQuery = new WildcardQuery(new Term(fieldName, value));
                            booleanQuery.Add(nodeQuery, Occur.SHOULD);
                            containsFilter = true;
                        }
                    }

                    if (containsFilter)
                    {
                        query.Add(booleanQuery, Occur.MUST);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(values[0]))
                    {
                        AddWildcardQuery(fieldName, query, values[0].ToLowerInvariant());
                    }
                }
            }
        }

        /// <summary>
        ///     Adds the query.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="query">The query.</param>
        /// <param name="value">The filter.</param>
        protected virtual void AddWildcardQuery(string fieldName, BooleanQuery query, string value)
        {
            fieldName = fieldName.ToLowerInvariant();
            var nodeQuery = new WildcardQuery(new Term(fieldName, value.ToLowerInvariant()));
            query.Add(nodeQuery, Occur.MUST);
        }
    }
}
