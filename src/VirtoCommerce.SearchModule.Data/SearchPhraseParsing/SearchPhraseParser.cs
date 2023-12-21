using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing.Antlr;

namespace VirtoCommerce.SearchModule.Data.SearchPhraseParsing
{
    public class SearchPhraseParser : ISearchPhraseParser
    {
        private readonly ILogger<SearchPhraseParser> _logger;

        public SearchPhraseParser(ILogger<SearchPhraseParser> logger)
        {
            _logger = logger;
        }

        public virtual SearchPhraseParseResult Parse(string input)
        {
            var stream = CharStreams.fromString(input);
            var lexer = new SearchPhraseLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new Antlr.SearchPhraseParser(tokens) { BuildParseTree = true };
            // Attach custom error listener
            var errorListener = new ErrorListener(_logger, input);
            parser.AddErrorListener(errorListener);

            var listener = new SearchPhraseListener();

            ParseTreeWalker.Default.Walk(listener, parser.searchPhrase());

            var result = new SearchPhraseParseResult
            {
                Keyword = string.Join(" ", listener.Keywords),
                Filters = listener.Filters,
            };

            return result;
        }
    }
}
