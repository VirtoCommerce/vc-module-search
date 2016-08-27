namespace VirtoCommerce.SearchModule.Data.Model.Indexing
{
    public interface ISearchIndexController
    {
        void Process(string scope, string documentType, bool rebuild);
    }
}
