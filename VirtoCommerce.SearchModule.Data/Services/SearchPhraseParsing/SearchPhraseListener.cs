using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing.Antlr;

namespace VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing
{
    [CLSCompliant(false)]
    public class SearchPhraseListener : SearchPhraseBaseListener
    {
        private static readonly string[] _rangeValueDelimiter = { "TO" };

        public IList<string> Keywords { get; } = new List<string>();
        public IList<ISearchFilter> Filters { get; } = new List<ISearchFilter>();

        public override void ExitKeyword(Antlr.SearchPhraseParser.KeywordContext context)
        {
            base.ExitKeyword(context);
            Keywords.Add(Unescape(context.GetText()));
        }

        public override void ExitAttributeFilter(Antlr.SearchPhraseParser.AttributeFilterContext context)
        {
            base.ExitAttributeFilter(context);

            var fieldNameContext = context.GetChild<Antlr.SearchPhraseParser.FieldNameContext>(0);
            var attributeValueContext = context.GetChild<Antlr.SearchPhraseParser.AttributeFilterValueContext>(0);

            if (fieldNameContext != null && attributeValueContext != null)
            {
                var values = attributeValueContext.children.OfType<Antlr.SearchPhraseParser.StringContext>().ToArray();

                var filter = new AttributeFilter
                {
                    Key = Unescape(fieldNameContext.GetText()),
                    Values = values.Select(v => new AttributeFilterValue { Value = Unescape(v.GetText()) }).ToArray(),
                };

                Filters.Add(filter);
            }
        }

        public override void ExitRangeFilter(Antlr.SearchPhraseParser.RangeFilterContext context)
        {
            base.ExitRangeFilter(context);

            var fieldNameContext = context.GetChild<Antlr.SearchPhraseParser.FieldNameContext>(0);
            var rangeValueContext = context.GetChild<Antlr.SearchPhraseParser.RangeFilterValueContext>(0);

            if (fieldNameContext != null && rangeValueContext != null)
            {
                var values = rangeValueContext.children
                    .OfType<Antlr.SearchPhraseParser.RangeContext>()
                    .Select(GetRangeFilterValue)
                    .ToArray();

                ISearchFilter filter;

                var fieldName = Unescape(fieldNameContext.GetText());
                if (fieldName.EqualsInvariant("price") || fieldName.StartsWith("price_", StringComparison.OrdinalIgnoreCase))
                {
                    var nameParts = fieldName.Split('_');
                    filter = new PriceRangeFilter
                    {
                        Currency = nameParts.Length > 1 ? nameParts[1] : null,
                        Values = values,
                    };
                }
                else
                {
                    filter = new RangeFilter
                    {
                        Key = fieldName,
                        Values = values,
                    };
                }

                Filters.Add(filter);
            }
        }

        protected virtual RangeFilterValue GetRangeFilterValue(Antlr.SearchPhraseParser.RangeContext context)
        {
            var lower = context.GetChild<Antlr.SearchPhraseParser.LowerContext>(0)?.GetText();
            var upper = context.GetChild<Antlr.SearchPhraseParser.UpperContext>(0)?.GetText();
            var rangeStart = context.GetChild<Antlr.SearchPhraseParser.RangeStartContext>(0)?.GetText();
            var rangeEnd = context.GetChild<Antlr.SearchPhraseParser.RangeEndContext>(0)?.GetText();

            return new RangeFilterValue
            {
                Lower = Unescape(lower),
                Upper = Unescape(upper),
                IncludeLower = rangeStart.EqualsInvariant("["),
                IncludeUpper = rangeEnd.EqualsInvariant("]"),
            };
        }

        protected virtual string Unescape(string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Trim('"').Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
        }
    }
}
