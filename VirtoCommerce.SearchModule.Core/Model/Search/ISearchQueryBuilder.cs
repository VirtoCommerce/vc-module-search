namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public interface ISearchQueryBuilder
    {
        /// <summary>
        /// Transforms criteria to the search provider specific request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scope"></param>
        /// <param name="criteria"></param>
        /// <returns></returns>
        object BuildQuery<T>(string scope, Criterias.ISearchCriteria criteria) where T:class;

        /// <summary>
        /// Defines type of document this query builder handles
        /// </summary>
        string DocumentType { get; }
    }
}
