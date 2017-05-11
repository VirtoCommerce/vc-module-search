using System;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public interface IOperationProvider
    {
        string DocumentType { get; }

        /// <summary>
        /// Returns operations for objects changed in the given time period.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        IList<Operation> GetOperations(DateTime startDate, DateTime endDate);
    }
}
