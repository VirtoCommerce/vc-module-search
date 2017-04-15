using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Search.Criteria;

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
        /// <param name="availableFields"></param>
        /// <returns></returns>
        object BuildQuery<T>(string scope, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields) where T : class;

        /// <summary>
        /// Defines type of document this query builder handles
        /// </summary>
        string DocumentType { get; }
    }
}
