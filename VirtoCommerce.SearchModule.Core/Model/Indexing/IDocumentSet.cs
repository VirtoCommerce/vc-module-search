using System;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    [Obsolete("Will be removed in one of the next versions")]
    public interface IDocumentSet
    {
        /// <summary>
        /// Gets or sets the total count.
        /// </summary>
        /// <value>The total count.</value>
        int TotalCount { get; set; }
        object[] Properties { get; set; }
        IDocument[] Documents { get; set; }
        string Name { get; set; }
    }
}
