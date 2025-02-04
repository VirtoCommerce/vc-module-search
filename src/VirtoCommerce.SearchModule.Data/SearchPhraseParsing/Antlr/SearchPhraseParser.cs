//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.2
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from SearchPhrase.g4 by ANTLR 4.13.2

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace VirtoCommerce.SearchModule.Data.SearchPhraseParsing.Antlr {
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DFA = Antlr4.Runtime.Dfa.DFA;

[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.2")]
[System.CLSCompliant(false)]
public partial class SearchPhraseParser : Parser {
	protected static DFA[] decisionToDFA;
	protected static PredictionContextCache sharedContextCache = new PredictionContextCache();
	public const int
		T__0=1, FD=2, VD=3, RD=4, RangeStart=5, RangeEnd=6, String=7, DL=8;
	public const int
		RULE_searchPhrase = 0, RULE_phrase = 1, RULE_keyword = 2, RULE_filters = 3, 
		RULE_attributeFilter = 4, RULE_rangeFilter = 5, RULE_fieldName = 6, RULE_attributeFilterValue = 7, 
		RULE_rangeFilterValue = 8, RULE_range = 9, RULE_rangeStart = 10, RULE_rangeEnd = 11, 
		RULE_lower = 12, RULE_upper = 13, RULE_string = 14, RULE_negation = 15;
	public static readonly string[] ruleNames = {
		"searchPhrase", "phrase", "keyword", "filters", "attributeFilter", "rangeFilter", 
		"fieldName", "attributeFilterValue", "rangeFilterValue", "range", "rangeStart", 
		"rangeEnd", "lower", "upper", "string", "negation"
	};

	private static readonly string[] _LiteralNames = {
		null, "'!'", "':'", "','"
	};
	private static readonly string[] _SymbolicNames = {
		null, null, "FD", "VD", "RD", "RangeStart", "RangeEnd", "String", "DL"
	};
	public static readonly IVocabulary DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);

	[NotNull]
	public override IVocabulary Vocabulary
	{
		get
		{
			return DefaultVocabulary;
		}
	}

	public override string GrammarFileName { get { return "SearchPhrase.g4"; } }

	public override string[] RuleNames { get { return ruleNames; } }

	public override int[] SerializedAtn { get { return _serializedATN; } }

	static SearchPhraseParser() {
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++) {
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

		public SearchPhraseParser(ITokenStream input) : this(input, Console.Out, Console.Error) { }

		public SearchPhraseParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	public partial class SearchPhraseContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public PhraseContext[] phrase() {
			return GetRuleContexts<PhraseContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public PhraseContext phrase(int i) {
			return GetRuleContext<PhraseContext>(i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] DL() { return GetTokens(SearchPhraseParser.DL); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode DL(int i) {
			return GetToken(SearchPhraseParser.DL, i);
		}
		public SearchPhraseContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_searchPhrase; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterSearchPhrase(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitSearchPhrase(this);
		}
	}

	[RuleVersion(0)]
	public SearchPhraseContext searchPhrase() {
		SearchPhraseContext _localctx = new SearchPhraseContext(Context, State);
		EnterRule(_localctx, 0, RULE_searchPhrase);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 35;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==DL) {
				{
				{
				State = 32;
				Match(DL);
				}
				}
				State = 37;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			State = 38;
			phrase();
			State = 43;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,1,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					State = 39;
					Match(DL);
					State = 40;
					phrase();
					}
					} 
				}
				State = 45;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,1,Context);
			}
			State = 49;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==DL) {
				{
				{
				State = 46;
				Match(DL);
				}
				}
				State = 51;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class PhraseContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public KeywordContext keyword() {
			return GetRuleContext<KeywordContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public FiltersContext filters() {
			return GetRuleContext<FiltersContext>(0);
		}
		public PhraseContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_phrase; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterPhrase(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitPhrase(this);
		}
	}

	[RuleVersion(0)]
	public PhraseContext phrase() {
		PhraseContext _localctx = new PhraseContext(Context, State);
		EnterRule(_localctx, 2, RULE_phrase);
		try {
			State = 54;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,3,Context) ) {
			case 1:
				EnterOuterAlt(_localctx, 1);
				{
				State = 52;
				keyword();
				}
				break;
			case 2:
				EnterOuterAlt(_localctx, 2);
				{
				State = 53;
				filters();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class KeywordContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode String() { return GetToken(SearchPhraseParser.String, 0); }
		public KeywordContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_keyword; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterKeyword(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitKeyword(this);
		}
	}

	[RuleVersion(0)]
	public KeywordContext keyword() {
		KeywordContext _localctx = new KeywordContext(Context, State);
		EnterRule(_localctx, 4, RULE_keyword);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 56;
			Match(String);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class FiltersContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public AttributeFilterContext attributeFilter() {
			return GetRuleContext<AttributeFilterContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public RangeFilterContext rangeFilter() {
			return GetRuleContext<RangeFilterContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public NegationContext negation() {
			return GetRuleContext<NegationContext>(0);
		}
		public FiltersContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_filters; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterFilters(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitFilters(this);
		}
	}

	[RuleVersion(0)]
	public FiltersContext filters() {
		FiltersContext _localctx = new FiltersContext(Context, State);
		EnterRule(_localctx, 6, RULE_filters);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 59;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			if (_la==T__0) {
				{
				State = 58;
				negation();
				}
			}

			State = 63;
			ErrorHandler.Sync(this);
			switch ( Interpreter.AdaptivePredict(TokenStream,5,Context) ) {
			case 1:
				{
				State = 61;
				attributeFilter();
				}
				break;
			case 2:
				{
				State = 62;
				rangeFilter();
				}
				break;
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class AttributeFilterContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public FieldNameContext fieldName() {
			return GetRuleContext<FieldNameContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode FD() { return GetToken(SearchPhraseParser.FD, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public AttributeFilterValueContext attributeFilterValue() {
			return GetRuleContext<AttributeFilterValueContext>(0);
		}
		public AttributeFilterContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_attributeFilter; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterAttributeFilter(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitAttributeFilter(this);
		}
	}

	[RuleVersion(0)]
	public AttributeFilterContext attributeFilter() {
		AttributeFilterContext _localctx = new AttributeFilterContext(Context, State);
		EnterRule(_localctx, 8, RULE_attributeFilter);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 65;
			fieldName();
			State = 66;
			Match(FD);
			State = 67;
			attributeFilterValue();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RangeFilterContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public FieldNameContext fieldName() {
			return GetRuleContext<FieldNameContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode FD() { return GetToken(SearchPhraseParser.FD, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public RangeFilterValueContext rangeFilterValue() {
			return GetRuleContext<RangeFilterValueContext>(0);
		}
		public RangeFilterContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_rangeFilter; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterRangeFilter(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitRangeFilter(this);
		}
	}

	[RuleVersion(0)]
	public RangeFilterContext rangeFilter() {
		RangeFilterContext _localctx = new RangeFilterContext(Context, State);
		EnterRule(_localctx, 10, RULE_rangeFilter);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 69;
			fieldName();
			State = 70;
			Match(FD);
			State = 71;
			rangeFilterValue();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class FieldNameContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode String() { return GetToken(SearchPhraseParser.String, 0); }
		public FieldNameContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_fieldName; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterFieldName(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitFieldName(this);
		}
	}

	[RuleVersion(0)]
	public FieldNameContext fieldName() {
		FieldNameContext _localctx = new FieldNameContext(Context, State);
		EnterRule(_localctx, 12, RULE_fieldName);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 73;
			Match(String);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class AttributeFilterValueContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public StringContext[] @string() {
			return GetRuleContexts<StringContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public StringContext @string(int i) {
			return GetRuleContext<StringContext>(i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] VD() { return GetTokens(SearchPhraseParser.VD); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode VD(int i) {
			return GetToken(SearchPhraseParser.VD, i);
		}
		public AttributeFilterValueContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_attributeFilterValue; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterAttributeFilterValue(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitAttributeFilterValue(this);
		}
	}

	[RuleVersion(0)]
	public AttributeFilterValueContext attributeFilterValue() {
		AttributeFilterValueContext _localctx = new AttributeFilterValueContext(Context, State);
		EnterRule(_localctx, 14, RULE_attributeFilterValue);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 75;
			@string();
			State = 80;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==VD) {
				{
				{
				State = 76;
				Match(VD);
				State = 77;
				@string();
				}
				}
				State = 82;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RangeFilterValueContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public RangeContext[] range() {
			return GetRuleContexts<RangeContext>();
		}
		[System.Diagnostics.DebuggerNonUserCode] public RangeContext range(int i) {
			return GetRuleContext<RangeContext>(i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] VD() { return GetTokens(SearchPhraseParser.VD); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode VD(int i) {
			return GetToken(SearchPhraseParser.VD, i);
		}
		public RangeFilterValueContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_rangeFilterValue; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterRangeFilterValue(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitRangeFilterValue(this);
		}
	}

	[RuleVersion(0)]
	public RangeFilterValueContext rangeFilterValue() {
		RangeFilterValueContext _localctx = new RangeFilterValueContext(Context, State);
		EnterRule(_localctx, 16, RULE_rangeFilterValue);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 83;
			range();
			State = 88;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==VD) {
				{
				{
				State = 84;
				Match(VD);
				State = 85;
				range();
				}
				}
				State = 90;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RangeContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public RangeStartContext rangeStart() {
			return GetRuleContext<RangeStartContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode RD() { return GetToken(SearchPhraseParser.RD, 0); }
		[System.Diagnostics.DebuggerNonUserCode] public RangeEndContext rangeEnd() {
			return GetRuleContext<RangeEndContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode[] DL() { return GetTokens(SearchPhraseParser.DL); }
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode DL(int i) {
			return GetToken(SearchPhraseParser.DL, i);
		}
		[System.Diagnostics.DebuggerNonUserCode] public LowerContext lower() {
			return GetRuleContext<LowerContext>(0);
		}
		[System.Diagnostics.DebuggerNonUserCode] public UpperContext upper() {
			return GetRuleContext<UpperContext>(0);
		}
		public RangeContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_range; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterRange(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitRange(this);
		}
	}

	[RuleVersion(0)]
	public RangeContext range() {
		RangeContext _localctx = new RangeContext(Context, State);
		EnterRule(_localctx, 18, RULE_range);
		int _la;
		try {
			int _alt;
			EnterOuterAlt(_localctx, 1);
			{
			State = 91;
			rangeStart();
			State = 95;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,8,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					State = 92;
					Match(DL);
					}
					} 
				}
				State = 97;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,8,Context);
			}
			State = 99;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			if (_la==String) {
				{
				State = 98;
				lower();
				}
			}

			State = 104;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==DL) {
				{
				{
				State = 101;
				Match(DL);
				}
				}
				State = 106;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			State = 107;
			Match(RD);
			State = 111;
			ErrorHandler.Sync(this);
			_alt = Interpreter.AdaptivePredict(TokenStream,11,Context);
			while ( _alt!=2 && _alt!=global::Antlr4.Runtime.Atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					{
					{
					State = 108;
					Match(DL);
					}
					} 
				}
				State = 113;
				ErrorHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(TokenStream,11,Context);
			}
			State = 115;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			if (_la==String) {
				{
				State = 114;
				upper();
				}
			}

			State = 120;
			ErrorHandler.Sync(this);
			_la = TokenStream.LA(1);
			while (_la==DL) {
				{
				{
				State = 117;
				Match(DL);
				}
				}
				State = 122;
				ErrorHandler.Sync(this);
				_la = TokenStream.LA(1);
			}
			State = 123;
			rangeEnd();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RangeStartContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode RangeStart() { return GetToken(SearchPhraseParser.RangeStart, 0); }
		public RangeStartContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_rangeStart; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterRangeStart(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitRangeStart(this);
		}
	}

	[RuleVersion(0)]
	public RangeStartContext rangeStart() {
		RangeStartContext _localctx = new RangeStartContext(Context, State);
		EnterRule(_localctx, 20, RULE_rangeStart);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 125;
			Match(RangeStart);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RangeEndContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode RangeEnd() { return GetToken(SearchPhraseParser.RangeEnd, 0); }
		public RangeEndContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_rangeEnd; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterRangeEnd(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitRangeEnd(this);
		}
	}

	[RuleVersion(0)]
	public RangeEndContext rangeEnd() {
		RangeEndContext _localctx = new RangeEndContext(Context, State);
		EnterRule(_localctx, 22, RULE_rangeEnd);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 127;
			Match(RangeEnd);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class LowerContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode String() { return GetToken(SearchPhraseParser.String, 0); }
		public LowerContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_lower; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterLower(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitLower(this);
		}
	}

	[RuleVersion(0)]
	public LowerContext lower() {
		LowerContext _localctx = new LowerContext(Context, State);
		EnterRule(_localctx, 24, RULE_lower);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 129;
			Match(String);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class UpperContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode String() { return GetToken(SearchPhraseParser.String, 0); }
		public UpperContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_upper; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterUpper(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitUpper(this);
		}
	}

	[RuleVersion(0)]
	public UpperContext upper() {
		UpperContext _localctx = new UpperContext(Context, State);
		EnterRule(_localctx, 26, RULE_upper);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 131;
			Match(String);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class StringContext : ParserRuleContext {
		[System.Diagnostics.DebuggerNonUserCode] public ITerminalNode String() { return GetToken(SearchPhraseParser.String, 0); }
		public StringContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_string; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterString(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitString(this);
		}
	}

	[RuleVersion(0)]
	public StringContext @string() {
		StringContext _localctx = new StringContext(Context, State);
		EnterRule(_localctx, 28, RULE_string);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 133;
			Match(String);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class NegationContext : ParserRuleContext {
		public NegationContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int RuleIndex { get { return RULE_negation; } }
		[System.Diagnostics.DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.EnterNegation(this);
		}
		[System.Diagnostics.DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener) {
			ISearchPhraseListener typedListener = listener as ISearchPhraseListener;
			if (typedListener != null) typedListener.ExitNegation(this);
		}
	}

	[RuleVersion(0)]
	public NegationContext negation() {
		NegationContext _localctx = new NegationContext(Context, State);
		EnterRule(_localctx, 30, RULE_negation);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 135;
			Match(T__0);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			ErrorHandler.ReportError(this, re);
			ErrorHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	private static int[] _serializedATN = {
		4,1,8,138,2,0,7,0,2,1,7,1,2,2,7,2,2,3,7,3,2,4,7,4,2,5,7,5,2,6,7,6,2,7,
		7,7,2,8,7,8,2,9,7,9,2,10,7,10,2,11,7,11,2,12,7,12,2,13,7,13,2,14,7,14,
		2,15,7,15,1,0,5,0,34,8,0,10,0,12,0,37,9,0,1,0,1,0,1,0,5,0,42,8,0,10,0,
		12,0,45,9,0,1,0,5,0,48,8,0,10,0,12,0,51,9,0,1,1,1,1,3,1,55,8,1,1,2,1,2,
		1,3,3,3,60,8,3,1,3,1,3,3,3,64,8,3,1,4,1,4,1,4,1,4,1,5,1,5,1,5,1,5,1,6,
		1,6,1,7,1,7,1,7,5,7,79,8,7,10,7,12,7,82,9,7,1,8,1,8,1,8,5,8,87,8,8,10,
		8,12,8,90,9,8,1,9,1,9,5,9,94,8,9,10,9,12,9,97,9,9,1,9,3,9,100,8,9,1,9,
		5,9,103,8,9,10,9,12,9,106,9,9,1,9,1,9,5,9,110,8,9,10,9,12,9,113,9,9,1,
		9,3,9,116,8,9,1,9,5,9,119,8,9,10,9,12,9,122,9,9,1,9,1,9,1,10,1,10,1,11,
		1,11,1,12,1,12,1,13,1,13,1,14,1,14,1,15,1,15,1,15,0,0,16,0,2,4,6,8,10,
		12,14,16,18,20,22,24,26,28,30,0,0,135,0,35,1,0,0,0,2,54,1,0,0,0,4,56,1,
		0,0,0,6,59,1,0,0,0,8,65,1,0,0,0,10,69,1,0,0,0,12,73,1,0,0,0,14,75,1,0,
		0,0,16,83,1,0,0,0,18,91,1,0,0,0,20,125,1,0,0,0,22,127,1,0,0,0,24,129,1,
		0,0,0,26,131,1,0,0,0,28,133,1,0,0,0,30,135,1,0,0,0,32,34,5,8,0,0,33,32,
		1,0,0,0,34,37,1,0,0,0,35,33,1,0,0,0,35,36,1,0,0,0,36,38,1,0,0,0,37,35,
		1,0,0,0,38,43,3,2,1,0,39,40,5,8,0,0,40,42,3,2,1,0,41,39,1,0,0,0,42,45,
		1,0,0,0,43,41,1,0,0,0,43,44,1,0,0,0,44,49,1,0,0,0,45,43,1,0,0,0,46,48,
		5,8,0,0,47,46,1,0,0,0,48,51,1,0,0,0,49,47,1,0,0,0,49,50,1,0,0,0,50,1,1,
		0,0,0,51,49,1,0,0,0,52,55,3,4,2,0,53,55,3,6,3,0,54,52,1,0,0,0,54,53,1,
		0,0,0,55,3,1,0,0,0,56,57,5,7,0,0,57,5,1,0,0,0,58,60,3,30,15,0,59,58,1,
		0,0,0,59,60,1,0,0,0,60,63,1,0,0,0,61,64,3,8,4,0,62,64,3,10,5,0,63,61,1,
		0,0,0,63,62,1,0,0,0,64,7,1,0,0,0,65,66,3,12,6,0,66,67,5,2,0,0,67,68,3,
		14,7,0,68,9,1,0,0,0,69,70,3,12,6,0,70,71,5,2,0,0,71,72,3,16,8,0,72,11,
		1,0,0,0,73,74,5,7,0,0,74,13,1,0,0,0,75,80,3,28,14,0,76,77,5,3,0,0,77,79,
		3,28,14,0,78,76,1,0,0,0,79,82,1,0,0,0,80,78,1,0,0,0,80,81,1,0,0,0,81,15,
		1,0,0,0,82,80,1,0,0,0,83,88,3,18,9,0,84,85,5,3,0,0,85,87,3,18,9,0,86,84,
		1,0,0,0,87,90,1,0,0,0,88,86,1,0,0,0,88,89,1,0,0,0,89,17,1,0,0,0,90,88,
		1,0,0,0,91,95,3,20,10,0,92,94,5,8,0,0,93,92,1,0,0,0,94,97,1,0,0,0,95,93,
		1,0,0,0,95,96,1,0,0,0,96,99,1,0,0,0,97,95,1,0,0,0,98,100,3,24,12,0,99,
		98,1,0,0,0,99,100,1,0,0,0,100,104,1,0,0,0,101,103,5,8,0,0,102,101,1,0,
		0,0,103,106,1,0,0,0,104,102,1,0,0,0,104,105,1,0,0,0,105,107,1,0,0,0,106,
		104,1,0,0,0,107,111,5,4,0,0,108,110,5,8,0,0,109,108,1,0,0,0,110,113,1,
		0,0,0,111,109,1,0,0,0,111,112,1,0,0,0,112,115,1,0,0,0,113,111,1,0,0,0,
		114,116,3,26,13,0,115,114,1,0,0,0,115,116,1,0,0,0,116,120,1,0,0,0,117,
		119,5,8,0,0,118,117,1,0,0,0,119,122,1,0,0,0,120,118,1,0,0,0,120,121,1,
		0,0,0,121,123,1,0,0,0,122,120,1,0,0,0,123,124,3,22,11,0,124,19,1,0,0,0,
		125,126,5,5,0,0,126,21,1,0,0,0,127,128,5,6,0,0,128,23,1,0,0,0,129,130,
		5,7,0,0,130,25,1,0,0,0,131,132,5,7,0,0,132,27,1,0,0,0,133,134,5,7,0,0,
		134,29,1,0,0,0,135,136,5,1,0,0,136,31,1,0,0,0,14,35,43,49,54,59,63,80,
		88,95,99,104,111,115,120
	};

	public static readonly ATN _ATN =
		new ATNDeserializer().Deserialize(_serializedATN);


}
} // namespace VirtoCommerce.SearchModule.Data.SearchPhraseParsing.Antlr
