using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using SpellChecker.Net.Search.Spell;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.LuceneSearch
{
    public class LuceneSearchResults<T> : ISearchResults<DocumentDictionary> where T : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LuceneSearchResults&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="searcher">The searcher.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="docs">The hits.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="query">The query.</param>
        /// <param name="availableFields"></param>
        public LuceneSearchResults(Searcher searcher, IndexReader reader, TopDocs docs, ISearchCriteria criteria, Query query, IList<IFieldDescriptor> availableFields)
        {
            this.SearchCriteria = criteria;
            CreateDocuments(searcher, docs);
            CreateFacets(reader, query, availableFields);
            //CreateSuggestions(reader, criteria);
        }

        public IList<FacetGroup> Facets { get; private set; }

        public long DocCount
        {
            get;
            private set;
        }

        public IList<DocumentDictionary> Documents { get; private set; }

        public ISearchCriteria SearchCriteria { get; private set; }

        public long TotalCount { get; private set; }

        public IList<string> Suggestions { get; private set; }


        /// <summary>
        /// Creates result document collection from Lucene documents.
        /// </summary>
        /// <param name="searcher">The searcher.</param>
        /// <param name="topDocs">The hits.</param>
        private void CreateDocuments(Searcher searcher, TopDocs topDocs)
        {
            // if no documents found return
            if (topDocs == null)
                return;

            var entries = new List<DocumentDictionary>();

            // get total hits
            var totalCount = topDocs.TotalHits;
            var recordsToRetrieve = this.SearchCriteria.RecordsToRetrieve;
            var startIndex = this.SearchCriteria.StartingRecord;
            if (recordsToRetrieve > totalCount)
                recordsToRetrieve = totalCount;

            for (var index = startIndex; index < startIndex + recordsToRetrieve; index++)
            {
                if (index >= totalCount)
                    break;

                var document = searcher.Doc(topDocs.ScoreDocs[index].Doc);
                var doc = new DocumentDictionary();

                var documentFields = document.GetFields();
                using (var fi = documentFields.GetEnumerator())
                {
                    while (fi.MoveNext())
                    {
                        if (fi.Current != null)
                        {
                            var field = fi.Current;
                            if (doc.ContainsKey(field.Name)) // convert to array
                            {
                                var newValues = new List<object>();

                                var currentValue = doc[field.Name];
                                var currentValues = currentValue as object[];

                                if (currentValues != null)
                                {
                                    newValues.AddRange(currentValues);
                                }
                                else
                                {
                                    newValues.Add(currentValue);
                                }

                                newValues.Add(field.StringValue);
                                doc[field.Name] = newValues.ToArray();
                            }
                            else
                            {
                                doc.Add(field.Name, field.StringValue);
                            }
                        }
                    }
                }

                entries.Add(doc);
            }

            this.TotalCount = totalCount;
            this.DocCount = entries.Count;
            this.Documents = entries.ToArray();
        }

        /// <summary>
        /// Creates facets.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="query">The query.</param>
        /// <param name="availableFields"></param>
        private void CreateFacets(IndexReader reader, Query query, IList<IFieldDescriptor> availableFields)
        {
            var groups = new List<FacetGroup>();
            var baseQueryFilter = new CachingWrapperFilter(new QueryWrapperFilter(query));
            var baseDocIdSet = baseQueryFilter.GetDocIdSet(reader);

            #region Subcategory filters


            /* 
            var catalogCriteria = Results.SearchCriteria as CatalogItemSearchCriteria;
            if (catalogCriteria != null && catalogCriteria.ChildCategoryFilters.Any())
            {
                var group = new FacetGroup("Subcategory");
                var groupCount = 0;

                foreach (var value in catalogCriteria.ChildCategoryFilters)
                {
                    var q = LuceneQueryHelper.CreateQuery(catalogCriteria.OutlineField, value);

                    if (q == null) continue;

                    var queryFilter = new CachingWrapperFilter(new QueryWrapperFilter(q));
                    var filterArray = queryFilter.GetDocIdSet(reader);
                    var newCount = CalculateFacetCount(baseDocIdSet, filterArray);
                    if (newCount == 0) continue;

                    var newFacet = new Facet(group, value.Code, value.Name, newCount);
                    group.Facets.Add(newFacet);
                    groupCount += newCount;
                }

                // Add only if items exist under
                if (groupCount > 0)
                {
                    groups.Add(group);
                }
            }
             * */

            #endregion

            if (SearchCriteria.Filters != null && SearchCriteria.Filters.Count > 0)
            {
                foreach (var filter in SearchCriteria.Filters)
                {

                    if (!string.IsNullOrEmpty(SearchCriteria.Currency) && filter is PriceRangeFilter)
                    {
                        var valCurrency = ((PriceRangeFilter)filter).Currency;
                        if (!valCurrency.Equals(SearchCriteria.Currency, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    var facetGroup = CalculateResultCount(reader, baseDocIdSet, filter, SearchCriteria, availableFields);
                    if (facetGroup != null)
                    {
                        groups.Add(facetGroup);
                    }
                }
            }

            Facets = groups.ToArray();
        }

        protected virtual FacetGroup CalculateResultCount(IndexReader reader, DocIdSet baseDocIdSet, ISearchFilter filter, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            FacetGroup result = null;

            BooleanFilter existing_filters = null;
            foreach (var f in criteria.CurrentFilters)
            {
                if (!f.Key.Equals(filter.Key))
                {
                    if (existing_filters == null)
                        existing_filters = new BooleanFilter();

                    var q = LuceneSearchHelper.CreateQuery(criteria, f, Occur.SHOULD, availableFields);
                    existing_filters.Add(new FilterClause(q, Occur.MUST));
                }
            }

            var groupLabels = filter.GetLabels();
            var facetGroup = new FacetGroup(filter.Key, groupLabels);

            var values = filter.GetValues();

            if (values == null && filter is AttributeFilter) // get values from the index itself
            {
                var allValues = reader.UniqueTermsFromField(filter.Key);
                if (allValues != null && allValues.Count() > 0)
                {
                    foreach (var value in allValues)
                    {
                        var attributeValue = new AttributeFilterValue() { Id = value, Value = value };
                        var valueFilter = LuceneSearchHelper.CreateQueryForValue(SearchCriteria, filter, attributeValue, availableFields);

                        if (valueFilter != null)
                        {
                            var queryFilter = new BooleanFilter();
                            queryFilter.Add(new FilterClause(valueFilter, Occur.MUST));
                            if (existing_filters != null)
                                queryFilter.Add(new FilterClause(existing_filters, Occur.MUST));

                            var filterArray = queryFilter.GetDocIdSet(reader);
                            var newCount = CalculateFacetCount(baseDocIdSet, filterArray);

                            if (newCount > 0)
                            {
                                var newFacet = new Facet(facetGroup, value, newCount, null);
                                facetGroup.Facets.Add(newFacet);
                            }
                        }
                    }
                }
            }

            if (values != null)
            {
                foreach (var group in values.GroupBy(v => v.Id))
                {
                    var value = group.FirstOrDefault();
                    var valueFilter = LuceneSearchHelper.CreateQueryForValue(SearchCriteria, filter, value, availableFields);

                    if (valueFilter != null)
                    {
                        var queryFilter = new BooleanFilter();
                        queryFilter.Add(new FilterClause(valueFilter, Occur.MUST));
                        if (existing_filters != null)
                            queryFilter.Add(new FilterClause(existing_filters, Occur.MUST));

                        var filterArray = queryFilter.GetDocIdSet(reader);
                        var newCount = CalculateFacetCount(baseDocIdSet, filterArray);

                        if (newCount > 0)
                        {
                            var valueLabels = group.GetValueLabels();
                            var newFacet = new Facet(facetGroup, group.Key, newCount, valueLabels);
                            facetGroup.Facets.Add(newFacet);
                        }
                    }
                }
            }

            if (facetGroup.Facets.Any())
            {
                result = facetGroup;
            }

            return result;
        }

        private static long CalculateFacetCount(DocIdSet baseBitSet, DocIdSet filterDocSet)
        {
            var baseDisi = new OpenBitSetDISI(baseBitSet.Iterator(), 25000);
            var filterIterator = filterDocSet.Iterator();
            baseDisi.InPlaceAnd(filterIterator);
            var total = baseDisi.Cardinality();
            return total;
        }

        /// <summary>
        /// Creates the suggestions.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="criteria">The criteria.</param>
        private void CreateSuggestions(IndexReader reader, ISearchCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria?.SearchPhrase))
            {
                Suggestions = SuggestSimilar(reader, "_content", criteria?.SearchPhrase);
            }
        }

        /// <summary>
        /// Gets the similar words.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="word">The word.</param>
        /// <returns></returns>
        private static string[] SuggestSimilar(IndexReader reader, string fieldName, string word)
        {
            var spell = new SpellChecker.Net.Search.Spell.SpellChecker(reader.Directory());
            spell.IndexDictionary(new LuceneDictionary(reader, fieldName));
            var similarWords = spell.SuggestSimilar(word, 2);

            // now make sure to close the spell checker
            spell.Close();

            return similarWords;
        }
    }

    public static class ReaderExtentions
    {
        public static IEnumerable<string> UniqueTermsFromField(
                                              this IndexReader reader, string field)
        {
            var termEnum = reader.Terms(new Term(field));

            do
            {
                var currentTerm = termEnum.Term;

                if (currentTerm.Field != field)
                    yield break;

                yield return currentTerm.Text;
            } while (termEnum.Next());
        }
    }
}
