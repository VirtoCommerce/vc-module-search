using System.Linq;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing;
using Xunit;

namespace VirtoCommerce.SearchModule.Test
{
    [Trait("Category", "CI")]
    public class SearchPhraseParserTests
    {
        [Fact]
        public void TestKeywords()
        {
            var parser = Getparser();
            var result = parser.Parse(" one two three ");

            Assert.NotNull(result);
            Assert.Equal("one two three", result.SearchPhrase);
            Assert.Empty(result.CurrentFilters);
        }

        [Fact]
        public void TestAttributeFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("color:red,blue");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as AttributeFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value.Value);

            value = filter.Values.Last();
            Assert.NotNull(value);
            Assert.Equal("blue", value.Value);
        }

        [Fact]
        public void TestRangeFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("size:(10 TO 20],[30 to 40)");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as RangeFilter;
            Assert.NotNull(filter);
            Assert.Equal("size", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("10", value.Lower);
            Assert.Equal("20", value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);

            value = filter.Values.Last();
            Assert.NotNull(value);
            Assert.Equal("30", value.Lower);
            Assert.Equal("40", value.Upper);
            Assert.True(value.IncludeLower);
            Assert.False(value.IncludeUpper);


            result = parser.Parse("size:(TO 10]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            filter = result.CurrentFilters.First() as RangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("size", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal(null, value.Lower);
            Assert.Equal("10", value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);


            result = parser.Parse("size:(10 TO]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            filter = result.CurrentFilters.First() as RangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("size", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("10", value.Lower);
            Assert.Equal(null, value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);
        }

        [Fact]
        public void TestPriceRangeFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("price:[100 TO 200),(300 to 400]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as PriceRangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("price", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("100", value.Lower);
            Assert.Equal("200", value.Upper);
            Assert.True(value.IncludeLower);
            Assert.False(value.IncludeUpper);

            value = filter.Values.Last();
            Assert.NotNull(value);
            Assert.Equal("300", value.Lower);
            Assert.Equal("400", value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);
        }

        [Fact]
        public void TestKeywordAndFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("one brand:apple two");

            Assert.NotNull(result);
            Assert.Equal("one two", result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as AttributeFilter;

            Assert.NotNull(filter);

            Assert.NotNull(filter);
            Assert.Equal("brand", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("apple", value.Value);
        }

        [Fact]
        public void TestQuotedStrings()
        {
            var parser = Getparser();

            // Keywords
            var result = parser.Parse("one \"two \\r\\n\\t\\\\ three\" four");

            Assert.NotNull(result);
            Assert.Equal("one two \r\n\t\\ three four", result.SearchPhrase);
            Assert.Empty(result.CurrentFilters);


            // Attribute filter
            result = parser.Parse("\"color \\r\\n\\t\\\\ 2\":\"light \\r\\n\\t\\\\ blue\"");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as AttributeFilter;
            Assert.NotNull(filter);
            Assert.Equal("color \r\n\t\\ 2", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("light \r\n\t\\ blue", value.Value);


            // Range filter
            result = parser.Parse("date:[\"2017-04-23T15:24:31.180Z\" to \"2017-04-28T15:24:31.180Z\"]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var rangeFilter = result.CurrentFilters.Last() as RangeFilter;
            Assert.NotNull(rangeFilter);
            Assert.Equal("date", rangeFilter.Key);
            Assert.NotNull(rangeFilter.Values);
            Assert.Equal(1, rangeFilter.Values.Length);

            var rangeValue = rangeFilter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("2017-04-23T15:24:31.180Z", rangeValue.Lower);
            Assert.Equal("2017-04-28T15:24:31.180Z", rangeValue.Upper);
            Assert.True(rangeValue.IncludeLower);
            Assert.True(rangeValue.IncludeUpper);
        }


        private static ISearchPhraseParser Getparser()
        {
            return new SearchPhraseParser();
        }
    }
}
