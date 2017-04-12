using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using u = Lucene.Net.Util;

namespace VirtoCommerce.SearchModule.Data.Providers.LuceneSearch
{
    public class LuceneSearchQueryBuilder : LuceneSearchQueryBuilderBase
    {
        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public override object BuildQuery<T>(string scope, ISearchCriteria criteria)
        {
            var result = base.BuildQuery<T>(scope, criteria) as LuceneSearchQuery;

            var query = result.Query as BooleanQuery;
            var analyzer = new StandardAnalyzer(u.Version.LUCENE_30);

            var fuzzyMinSimilarity = 0.7f;
            var isFuzzySearch = false;

            // add standard keyword search
            if (criteria is KeywordSearchCriteria)
            {
                var c = criteria as KeywordSearchCriteria;
                // Add search
                if (!string.IsNullOrEmpty(c.SearchPhrase))
                {
                    var searchPhrase = c.SearchPhrase;
                    if (isFuzzySearch)
                    {

                        var keywords = c.SearchPhrase.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        searchPhrase = string.Empty;
                        searchPhrase = keywords.Aggregate(
                            searchPhrase,
                            (current, keyword) =>
                                current + string.Format("{0}~{1}", keyword.Replace("~", ""), fuzzyMinSimilarity.ToString(CultureInfo.InvariantCulture)));
                    }

                    var fields = new List<string> { "__content" };
                    if (c.Locale != null)
                    {
                        var contentField = $"__content_{c.Locale.ToLower()}";
                        fields.Add(contentField);
                    }

                    var parser = new MultiFieldQueryParser(u.Version.LUCENE_30, fields.ToArray(), analyzer)
                    {
                        DefaultOperator = QueryParser.Operator.AND
                    };

                    var searchQuery = parser.Parse(searchPhrase);
                    query.Add(searchQuery, Occur.MUST);
                }
            }

            return result;
        }

    }
}
