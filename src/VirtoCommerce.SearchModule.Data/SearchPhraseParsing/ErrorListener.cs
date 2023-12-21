using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using Microsoft.Extensions.Logging;

namespace VirtoCommerce.SearchModule.Data.SearchPhraseParsing
{
    public class ErrorListener : BaseErrorListener
    {
        private readonly ILogger<SearchPhraseParser> _logger;
        private readonly string _input;

        public ErrorListener(ILogger<SearchPhraseParser> logger, string input)
        {
            _logger = logger;
            _input = input;
        }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _logger.LogError("Syntax error at line {Line}, position {CharPositionInLine}: {Msg} for {Input}.", line, charPositionInLine, msg, _input);
        }

        public override void ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts, ATNConfigSet configs)
        {
            _logger.LogWarning("Ambiguity at {StartIndex}-{StopIndex}. Possibly a parsing issue for {Input}.", startIndex, stopIndex, _input);
        }

        public override void ReportAttemptingFullContext(Parser recognizer, DFA dfa, int startIndex, int stopIndex, BitSet conflictingAlts, ATNConfigSet configs)
        {
            _logger.LogWarning("Attempting full context at {StartIndex}-{StopIndex}. Possibly a parsing issue  for {Input}.", startIndex, stopIndex, _input);
        }

        public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, ATNConfigSet configs)
        {
            _logger.LogWarning("Context sensitivity at {StartIndex}-{StopIndex}. Possibly a parsing issue  for {Input}.", startIndex, stopIndex, _input);
        }
    }
}
