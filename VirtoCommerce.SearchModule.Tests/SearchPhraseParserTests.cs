using System.Linq;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing;
using Xunit;

namespace VirtoCommerce.SearchModule.Test
{
    public class SearchPhraseParserTests
    {
        [Fact]
        public void TestNoFilters()
        {
            var parser = Getparser();
            var result = parser.Parse("one two three");

            Assert.NotNull(result);
            Assert.Equal("one two three", result.SearchPhrase);
            Assert.Empty(result.CurrentFilters);
        }

        [Fact]
        public void TestAttributeFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("color:red");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as AttributeFilter;

            Assert.NotNull(filter);
            Assert.Equal("color", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value.Value);
        }

        [Fact]
        public void TestRangeFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("size:(10 TO 20]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as RangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("size", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("10", value.Lower);
            Assert.Equal("20", value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);


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
            Assert.Equal(string.Empty, value.Lower);
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
            Assert.Equal(string.Empty, value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);
        }

        [Fact]
        public void TestPriceRangeFilter()
        {
            var parser = Getparser();
            var result = parser.Parse("price:[100 TO 200)");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.SearchPhrase);
            Assert.NotNull(result.CurrentFilters);
            Assert.Equal(1, result.CurrentFilters.Count);

            var filter = result.CurrentFilters.First() as PriceRangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("price", filter.Key);
            Assert.NotNull(filter.Values);
            Assert.Equal(1, filter.Values.Length);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("100", value.Lower);
            Assert.Equal("200", value.Upper);
            Assert.True(value.IncludeLower);
            Assert.False(value.IncludeUpper);
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


        private static ISearchPhraseParser Getparser()
        {
            return new SearchPhraseParser();
        }
    }
}
