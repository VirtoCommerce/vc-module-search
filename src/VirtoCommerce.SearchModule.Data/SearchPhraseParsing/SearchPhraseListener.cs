using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Misc;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing.Antlr;
using AntlrSPP = VirtoCommerce.SearchModule.Data.SearchPhraseParsing.Antlr.SearchPhraseParser;

namespace VirtoCommerce.SearchModule.Data.SearchPhraseParsing
{
    public class SearchPhraseListener : SearchPhraseBaseListener
    {
        public IList<string> Keywords { get; } = new List<string>();
        public IList<IFilter> Filters { get; } = new List<IFilter>();

        public override void ExitKeyword([NotNull] AntlrSPP.KeywordContext context)
        {
            base.ExitKeyword(context);
            Keywords.Add(UnEscape(context.GetText()));
        }

        public override void ExitOrExpression([NotNull] AntlrSPP.OrExpressionContext context)
        {
            base.ExitOrExpression(context);

            var orExpression = new OrFilter { ChildFilters = [.. Filters] };
            Filters.Clear();
            Filters.Add(orExpression);
        }

        public override void ExitAndExpression([NotNull] AntlrSPP.AndExpressionContext context)
        {
            base.ExitAndExpression(context);

            var andExpression = new AndFilter { ChildFilters = [.. Filters] };
            Filters.Clear();
            Filters.Add(andExpression);
        }

        public override void ExitFilters([NotNull] AntlrSPP.FiltersContext context)
        {
            base.ExitFilters(context);

            var negationContext = context.GetChild<AntlrSPP.NegationContext>(0);
            var attributeFilterContext = context.GetChild<AntlrSPP.AttributeFilterContext>(0);
            var rangeFilterContext = context.GetChild<AntlrSPP.RangeFilterContext>(0);

            IFilter filter = null;
            if (attributeFilterContext != null)
            {
                filter = GetTermFilter(attributeFilterContext);
            }
            else if (rangeFilterContext != null)
            {
                filter = GetRangeFilter(rangeFilterContext);
            }

            if (filter == null)
            {
                return;
            }

            if (negationContext != null)
            {
                filter = new NotFilter { ChildFilter = filter };
            }

            Filters.Add(filter);
        }

        protected virtual TermFilter GetTermFilter(AntlrSPP.AttributeFilterContext context)
        {
            var fieldNameContext = context.GetChild<AntlrSPP.FieldNameContext>(0);
            var attributeValueContext = context.GetChild<AntlrSPP.AttributeFilterValueContext>(0);

            if (fieldNameContext == null || attributeValueContext == null)
            {
                return null;
            }

            var values = attributeValueContext.children.OfType<AntlrSPP.StringContext>().ToArray();

            return new TermFilter
            {
                FieldName = UnEscape(fieldNameContext.GetText()),
                Values = values.Select(v => UnEscape(v.GetText())).ToArray(),
            };
        }

        protected virtual RangeFilter GetRangeFilter(AntlrSPP.RangeFilterContext context)
        {
            var fieldNameContext = context.GetChild<AntlrSPP.FieldNameContext>(0);
            var rangeValueContext = context.GetChild<AntlrSPP.RangeFilterValueContext>(0);

            if (fieldNameContext == null || rangeValueContext == null)
            {
                return null;
            }

            var values = rangeValueContext.children
                .OfType<AntlrSPP.RangeContext>()
                .Select(GetRangeFilterValue)
                .ToArray();

            return new RangeFilter
            {
                FieldName = UnEscape(fieldNameContext.GetText()),
                Values = values,
            };
        }

        protected virtual RangeFilterValue GetRangeFilterValue(AntlrSPP.RangeContext context)
        {
            var lower = context.GetChild<AntlrSPP.LowerContext>(0)?.GetText();
            var upper = context.GetChild<AntlrSPP.UpperContext>(0)?.GetText();
            var rangeStart = context.GetChild<AntlrSPP.RangeStartContext>(0)?.GetText();
            var rangeEnd = context.GetChild<AntlrSPP.RangeEndContext>(0)?.GetText();

            return new RangeFilterValue
            {
                Lower = UnEscape(lower),
                Upper = UnEscape(upper),
                IncludeLower = rangeStart.EqualsInvariant("["),
                IncludeUpper = rangeEnd.EqualsInvariant("]"),
            };
        }

        protected virtual string UnEscape(string value)
        {
            if (value == null ||
                value.Length < 2 ||
                value.First() != '"' ||
                value.Last() != '"')
            {
                return value;
            }

            var result = new StringBuilder(value.Length);
            var unescaping = false;

            // Skip first and last double quote characters
            foreach (var character in value[1..^1])
            {
                if (unescaping)
                {
                    var newCharacter = character switch
                    {
                        'r' => '\r',
                        'n' => '\n',
                        't' => '\t',
                        _ => character,
                    };

                    result.Append(newCharacter);
                    unescaping = false;
                }
                else if (character == '\\')
                {
                    unescaping = true;
                }
                else
                {
                    result.Append(character);
                }
            }

            return result.ToString();
        }
    }
}
