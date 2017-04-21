namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public interface IDocumentBuilder<in TItem, in TContext>
    {
        /// <summary>
        /// Takes information from item and context and adds fields to document
        /// </summary>
        /// <param name="document"></param>
        /// <param name="item"></param>
        /// <param name="context"></param>
        /// <returns>False if document should not be indexed</returns>
        bool UpdateDocument(IDocument document, TItem item, TContext context);
    }
}
