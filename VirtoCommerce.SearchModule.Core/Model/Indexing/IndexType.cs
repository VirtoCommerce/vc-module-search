namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public static class IndexType
    {
        /// <summary>
        /// Values are parsed to tokens
        /// </summary>
        public const string Analyzed = "Index.Analyzed";
        /// <summary>
        /// Values are indexed as is
        /// </summary>
        public const string NotAnalyzed = "Index.NotAnalyzed";
        /// <summary>
        /// Values are not indexed
        /// </summary>
        public const string No = "Index.No";
    }
}
