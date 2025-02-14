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
using Antlr4.Runtime.Misc;
using IParseTreeListener = Antlr4.Runtime.Tree.IParseTreeListener;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete listener for a parse tree produced by
/// <see cref="SearchPhraseParser"/>.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.2")]
[System.CLSCompliant(false)]
public interface ISearchPhraseListener : IParseTreeListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.searchPhrase"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterSearchPhrase([NotNull] SearchPhraseParser.SearchPhraseContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.searchPhrase"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitSearchPhrase([NotNull] SearchPhraseParser.SearchPhraseContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.phrase"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterPhrase([NotNull] SearchPhraseParser.PhraseContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.phrase"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitPhrase([NotNull] SearchPhraseParser.PhraseContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.keyword"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterKeyword([NotNull] SearchPhraseParser.KeywordContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.keyword"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitKeyword([NotNull] SearchPhraseParser.KeywordContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.filters"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterFilters([NotNull] SearchPhraseParser.FiltersContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.filters"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitFilters([NotNull] SearchPhraseParser.FiltersContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.attributeFilter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterAttributeFilter([NotNull] SearchPhraseParser.AttributeFilterContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.attributeFilter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitAttributeFilter([NotNull] SearchPhraseParser.AttributeFilterContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.rangeFilter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRangeFilter([NotNull] SearchPhraseParser.RangeFilterContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.rangeFilter"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRangeFilter([NotNull] SearchPhraseParser.RangeFilterContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.fieldName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterFieldName([NotNull] SearchPhraseParser.FieldNameContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.fieldName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitFieldName([NotNull] SearchPhraseParser.FieldNameContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.attributeFilterValue"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterAttributeFilterValue([NotNull] SearchPhraseParser.AttributeFilterValueContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.attributeFilterValue"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitAttributeFilterValue([NotNull] SearchPhraseParser.AttributeFilterValueContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.rangeFilterValue"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRangeFilterValue([NotNull] SearchPhraseParser.RangeFilterValueContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.rangeFilterValue"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRangeFilterValue([NotNull] SearchPhraseParser.RangeFilterValueContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.range"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRange([NotNull] SearchPhraseParser.RangeContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.range"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRange([NotNull] SearchPhraseParser.RangeContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.rangeStart"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRangeStart([NotNull] SearchPhraseParser.RangeStartContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.rangeStart"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRangeStart([NotNull] SearchPhraseParser.RangeStartContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.rangeEnd"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterRangeEnd([NotNull] SearchPhraseParser.RangeEndContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.rangeEnd"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitRangeEnd([NotNull] SearchPhraseParser.RangeEndContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.lower"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterLower([NotNull] SearchPhraseParser.LowerContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.lower"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitLower([NotNull] SearchPhraseParser.LowerContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.upper"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterUpper([NotNull] SearchPhraseParser.UpperContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.upper"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitUpper([NotNull] SearchPhraseParser.UpperContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.negation"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterNegation([NotNull] SearchPhraseParser.NegationContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.negation"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitNegation([NotNull] SearchPhraseParser.NegationContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="SearchPhraseParser.string"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterString([NotNull] SearchPhraseParser.StringContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="SearchPhraseParser.string"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitString([NotNull] SearchPhraseParser.StringContext context);
}
} // namespace VirtoCommerce.SearchModule.Data.SearchPhraseParsing.Antlr
