using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public interface IBatchDocumentBuilder<TItem>
    {
        /// <summary>
        /// Takes information from items and context and adds fields to corresponding documents
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="items"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        void UpdateDocuments(IList<IDocument> documents, IList<TItem> items, object context);
    }
}
