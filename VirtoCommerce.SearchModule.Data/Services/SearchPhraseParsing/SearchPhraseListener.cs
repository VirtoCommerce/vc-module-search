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
            Keywords.Add(context.GetText());
        }

        public override void ExitFilter(Antlr.SearchPhraseParser.FilterContext context)
        {
            base.ExitFilter(context);

            if (context.ChildCount == 3)
            {
                var fieldName = context.children[0].GetText();

                ISearchFilter searchFilter = null;

                var attributeValueContext = context.GetChild<Antlr.SearchPhraseParser.AttributeFilterValueContext>(0);
                var rangeValueContext = context.GetChild<Antlr.SearchPhraseParser.RangeFilterValueContext>(0);

                if (attributeValueContext != null)
                {
                    var value = attributeValueContext.GetText();
                    searchFilter = new AttributeFilter
                    {
                        Key = fieldName,
                        Values = new[] { new AttributeFilterValue { Value = value } },
                    };
                }
                else if (rangeValueContext != null)
                {
                    if (fieldName.EqualsInvariant("price") || fieldName.StartsWith("price_", StringComparison.OrdinalIgnoreCase))
                    {
                        var nameParts = fieldName.Split('_');
                        searchFilter = new PriceRangeFilter
                        {
                            Currency = nameParts.Length > 1 ? nameParts[1] : null,
                            Values = new[] { GetRangeFilterValue(rangeValueContext) },
                        };
                    }
                    else
                    {
                        searchFilter = new RangeFilter
                        {
                            Key = fieldName,
                            Values = new[] { GetRangeFilterValue(rangeValueContext) },
                        };
                    }
                }

                if (searchFilter != null)
                {
                    Filters.Add(searchFilter);
                }
            }
        }

        protected virtual RangeFilterValue GetRangeFilterValue(Antlr.SearchPhraseParser.RangeFilterValueContext context)
        {
            var value = context.GetText();
            var bounds = value.Split(_rangeValueDelimiter, StringSplitOptions.None)
                .Select(b => b.Trim(' ', '[', ']', '(', ')'))
                .ToArray();

            return new RangeFilterValue
            {
                Lower = bounds.First(),
                Upper = bounds.Last(),
                IncludeLower = value.StartsWith("["),
                IncludeUpper = value.EndsWith("]"),
            };
        }
    }
}
