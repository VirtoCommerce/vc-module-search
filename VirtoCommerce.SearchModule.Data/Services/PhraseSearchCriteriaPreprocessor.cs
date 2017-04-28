using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class PhraseSearchCriteriaPreprocessor : ISearchCriteriaPreprocessor
    {
        private readonly ISearchPhraseParser _searchPhraseParser;

        public PhraseSearchCriteriaPreprocessor(ISearchPhraseParser searchPhraseParser)
        {
            _searchPhraseParser = searchPhraseParser;
        }

        public virtual void Process(ISearchCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria?.SearchPhrase))
            {
                var newCriteria = _searchPhraseParser.Parse(criteria.SearchPhrase);
                criteria.SearchPhrase = newCriteria.SearchPhrase;
                criteria.CurrentFilters.AddRange(newCriteria.CurrentFilters);
            }
        }
    }
}
