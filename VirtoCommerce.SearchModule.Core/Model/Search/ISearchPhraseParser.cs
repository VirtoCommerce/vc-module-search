namespace VirtoCommerce.SearchModule.Core.Model.Search
{
    public interface ISearchPhraseParser
    {
        ISearchCriteria Parse(string input);
    }
}
