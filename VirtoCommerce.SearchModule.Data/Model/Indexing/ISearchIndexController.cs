namespace VirtoCommerce.SearchModule.Data.Model.Indexing
{
    public interface ISearchIndexController
    {
        /// <summary>
        /// Builds or rebuilds either specific documentType or all documentTypes if documentType passed is null.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="documentType"></param>
        /// <param name="rebuild"></param>
        void Process(string scope, string documentType, bool rebuild);

        /// <summary>
        /// Rebuilds specific document.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="documentType"></param>
        /// <param name="documentId"></param>
        void Process(string scope, string documentType, string documentId);
    }
}
