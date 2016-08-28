using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using u = Lucene.Net.Util;
using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;

namespace VirtoCommerce.SearchModule.Data.Providers.Lucene
{
    public class LuceneSearchQueryBuilder : BaseSearchQueryBuilder
    {
        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public override object BuildQuery<T>(string scope, ISearchCriteria criteria)
        {
            var builder = base.BuildQuery<T>(scope, criteria) as QueryBuilder;
            var query = builder.Query as BooleanQuery;
            var analyzer = new StandardAnalyzer(u.Version.LUCENE_30);

            var fuzzyMinSimilarity = 0.7f;
            var isFuzzySearch = false;
            if (criteria is CatalogItemSearchCriteria)
            {
                var c = criteria as CatalogItemSearchCriteria;
                var datesFilterStart = new TermRangeQuery(
                    "startdate", c.StartDateFrom.HasValue ? DateTools.DateToString(c.StartDateFrom.Value, DateTools.Resolution.SECOND) : null, DateTools.DateToString(c.StartDate, DateTools.Resolution.SECOND), false, true);
                query.Add(datesFilterStart, Occur.MUST);

                if (c.EndDate.HasValue)
                {
                    var datesFilterEnd = new TermRangeQuery(
                        "enddate",
                        DateTools.DateToString(c.EndDate.Value, DateTools.Resolution.SECOND),
                        null,
                        true,
                        false);

                    query.Add(datesFilterEnd, Occur.MUST);
                }

                if (c.Outlines != null && c.Outlines.Count > 0)
                {
                    AddQuery("__outline", query, c.Outlines);
                }

                query.Add(new TermQuery(new Term("__hidden", "false")), Occur.MUST);

                if (!String.IsNullOrEmpty(c.Catalog))
                {
                    AddQuery("catalog", query, c.Catalog);
                }

                fuzzyMinSimilarity = c.FuzzyMinSimilarity;
                isFuzzySearch = c.IsFuzzySearch;
            }

            // add standard keyword search
            if (criteria is KeywordSearchCriteria)
            {
                var c = criteria as KeywordSearchCriteria;
                // Add search
                if (!String.IsNullOrEmpty(c.SearchPhrase))
                {
                    var searchPhrase = c.SearchPhrase;
                    if (isFuzzySearch)
                    {

                        var keywords = c.SearchPhrase.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        searchPhrase = string.Empty;
                        searchPhrase = keywords.Aggregate(
                            searchPhrase,
                            (current, keyword) =>
                                current + String.Format("{0}~{1}", keyword.Replace("~", ""), fuzzyMinSimilarity.ToString(CultureInfo.InvariantCulture)));
                    }

                    var fields = new List<string> { "__content" };
                    if (c.Locale != null)
                    {
                        var contentField = string.Format("__content_{0}", c.Locale.ToLower());
                        fields.Add(contentField);
                    }

                    var parser = new MultiFieldQueryParser(u.Version.LUCENE_30, fields.ToArray(), analyzer)
                    {
                        DefaultOperator = QueryParser.Operator.OR
                    };

                    var searchQuery = parser.Parse(searchPhrase);
                    query.Add(searchQuery, Occur.MUST);
                }
            }
            //else if (criteria is OrderSearchCriteria)
            //{
            //	var c = criteria as OrderSearchCriteria;

            //	if (!String.IsNullOrEmpty(c.CustomerId))
            //	{
            //		AddQuery("customerid", query, c.CustomerId);
            //	}
            //}

            return builder;
        }

    }

    public class QueryBuilder
    {
        public Query Query { get; set; }

        public Filter Filter { get; set; }

        #region Overrides of Object

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var ret = new StringBuilder();
            if (this.Query != null)
                ret.AppendFormat("query:{0}", this.Query.ToString());

            if (this.Filter != null)
                ret.AppendFormat("filter:{0}", this.Filter.ToString());

            return ret.ToString();
        }

        #endregion
    }
}