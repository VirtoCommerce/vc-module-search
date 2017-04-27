using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing.Antlr;

namespace VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing
{
    public class SearchPhraseParser : ISearchPhraseParser
    {
        public ISearchCriteria Parse(string input)
        {
            var stream = CharStreams.fromstring(input);
            var lexer = new SearchPhraseLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new Antlr.SearchPhraseParser(tokens) { BuildParseTree = true };
            var listener = new SearchPhraseListener();
            ParseTreeWalker.Default.Walk(listener, parser.searchPhrase());

            var result = new BaseSearchCriteria(null)
            {
                SearchPhrase = string.Join(" ", listener.Keywords),
            };
            result.CurrentFilters.AddRange(listener.Filters);

            return result;
        }

        public void ParseAndApply(ISearchCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria?.SearchPhrase))
            {
                var newCriteria = Parse(criteria.SearchPhrase);
                criteria.SearchPhrase = newCriteria.SearchPhrase;
                criteria.CurrentFilters.AddRange(newCriteria.CurrentFilters);
            }
        }
    }
}
