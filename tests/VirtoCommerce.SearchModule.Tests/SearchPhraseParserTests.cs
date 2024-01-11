using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests
{
    [Trait("Category", "Unit")]
    public class SearchPhraseParserTests
    {
        [Theory]
        [InlineData("customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\" createddate:[2023-12-01 TO]")]
        [InlineData("customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\" createddate:[2023-12-01 TO 2023-12-31]")]
        [InlineData("customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\" createddate:[\"2023-12-01T01:12:00Z\" TO]")]
        [InlineData("customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\" createddate:[\"2023-12-01T01:12:00Z\" TO \"2023-12-31T01:12:00Z\"]")]
        [InlineData("createddate:[2023-12-01 TO] customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\"")]
        [InlineData("createddate:[2023-12-01 TO 2023-12-31] customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\"")]
        [InlineData("createddate:[\"2023-12-01T01:12:00Z\" TO] customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\"")]
        [InlineData("createddate:[\"2023-12-01T01:12:00Z\" TO \"2023-12-31T01:12:00Z\"] customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\"")]
        public void TestDateTimeRange(string filter)
        {
            var parser = GetParser();
            var result = parser.Parse(filter);

            Assert.Equal(2, result.Filters.Count);
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId"));
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "createddate"));
        }

        /// <summary>
        /// This test demonstrates that currrent grammar failed and stop parcing next filters if date time value is not wrapped with double quotes, check logs for the details.
        /// </summary>
        [Fact]
        public void TestWrongDateTimeWithoutQuoеtes()
        {
            var parser = GetParser();
            var result = parser.Parse("createddate:[2023-12-01T01:12:00Z TO] customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\"");

            Assert.Equal(1, result.Filters.Count);
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "createddate"));
            Assert.Null(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId"));
        }

        /// <summary>
        /// This test demonstrates that currrent grammar failed and stop parcing next filters if date time value is not wrapped with double quotes, check logs for the details.
        /// </summary>
        [Fact]
        public void TestWrongDateTimeWithoutQuoеtes2()
        {
            var parser = GetParser();
            var result = parser.Parse("customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\" createddate:[2023-12-01T01:12:00Z TO] color:Black,White");

            Assert.Equal(2, result.Filters.Count);
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId"));
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "createddate"));
            Assert.Null(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "color"));
        }

        [Fact]
        public void TestKeywords()
        {
            var parser = GetParser();
            var result = parser.Parse(" one two three ");

            Assert.NotNull(result);
            Assert.Equal("one two three", result.Keyword);
            Assert.Empty(result.Filters);
        }

        [Fact]
        public void TestNegationFilter()
        {
            // Arrange
            var parser = GetParser();

            // Act
            var result = parser.Parse("!size:medium");

            // Assert
            result.Should().NotBeNull();
            result.Keyword.Should().BeEmpty();
            result.Filters.Should().NotBeNull();
            result.Filters.Count.Should().Be(1);
            var firstFilter = result.Filters.First().As<NotFilter>();
            firstFilter.Should().NotBeNull();
            firstFilter.ChildFilter.Should().NotBeNull();
        }

        [Fact]
        public void TestAttributeFilter()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red,blue");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Count);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value);

            value = filter.Values.Last();
            Assert.NotNull(value);
            Assert.Equal("blue", value);
        }

        [Fact]
        public void TestRangeFilter()
        {
            var parser = GetParser();
            var result = parser.Parse("size:(10 TO 20],[30 to 40)");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as RangeFilter;
            Assert.NotNull(filter);
            Assert.Equal("size", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Count);

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
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            filter = result.Filters.First() as RangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("size", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Null(value.Lower);
            Assert.Equal("10", value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);

            result = parser.Parse("size:(10 TO]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            filter = result.Filters.First() as RangeFilter;

            Assert.NotNull(filter);
            Assert.Equal("size", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("10", value.Lower);
            Assert.Null(value.Upper);
            Assert.False(value.IncludeLower);
            Assert.True(value.IncludeUpper);
        }

        [Fact]
        public void TestKeywordAndFilter()
        {
            var parser = GetParser();
            var result = parser.Parse("one brand:apple two");

            Assert.NotNull(result);
            Assert.Equal("one two", result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;

            Assert.NotNull(filter);

            Assert.NotNull(filter);
            Assert.Equal("brand", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("apple", value);
        }

        [Fact]
        public void TestQuotedStrings()
        {
            var parser = GetParser();

            // Keywords
            var result = parser.Parse("one \"two \\r\\n\\t\\\\ three\" four");

            Assert.NotNull(result);
            Assert.Equal("one two \r\n\t\\ three four", result.Keyword);
            Assert.Empty(result.Filters);

            // Attribute filter
            result = parser.Parse("\"color \\r\\n\\t\\\\ 2\":\"light \\r\\n\\t\\\\ blue\"");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color \r\n\t\\ 2", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("light \r\n\t\\ blue", value);

            // Range filter
            result = parser.Parse("date:[\"2017-04-23T15:24:31.180Z\" to \"2017-04-28T15:24:31.180Z\"]");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var rangeFilter = result.Filters.Last() as RangeFilter;
            Assert.NotNull(rangeFilter);
            Assert.Equal("date", rangeFilter.FieldName);
            Assert.NotNull(rangeFilter.Values);
            Assert.Single(rangeFilter.Values);

            var rangeValue = rangeFilter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("2017-04-23T15:24:31.180Z", rangeValue.Lower);
            Assert.Equal("2017-04-28T15:24:31.180Z", rangeValue.Upper);
            Assert.True(rangeValue.IncludeLower);
            Assert.True(rangeValue.IncludeUpper);
        }

        [Theory]
        [InlineData("name:", null)]         // Value must be specified
        [InlineData("name:\\", null)]       // Value with special characters must be wrapped with double quotes
        [InlineData("name:\"", null)]       // Value with special characters must be wrapped with double quotes
        [InlineData("name:\r", null)]       // Value with special characters must be wrapped with double quotes
        [InlineData("name:\n", null)]       // Value with special characters must be wrapped with double quotes
        [InlineData("name:\t", null)]       // Value with special characters must be wrapped with double quotes
        [InlineData("name:\"\\\"", null)]   // Special characters must be escaped with \
        [InlineData("name:\"\"\"", "")]     // Special characters must be escaped with \
        [InlineData("name:\"\r\"", null)]   // Special characters must be escaped with \
        [InlineData("name:\"\n\"", null)]   // Special characters must be escaped with \
        [InlineData("name:\"\t\"", null)]   // Special characters must be escaped with \
        [InlineData("name:\"\\a\"", null)]  // Unknown escape sequence \a
        [InlineData("name:\"\\\\\"", "\\")]
        [InlineData("name:\"\\\"\"", "\"")]
        [InlineData("name:\"\\r\"", "\r")]
        [InlineData("name:\"\\n\"", "\n")]
        [InlineData("name:\"\\t\"", "\t")]
        [InlineData("name:\"value\"", "value")]
        [InlineData("name:\"\"", "")]
        [InlineData("name:\"\\\\n\"", "\\n")]
        [InlineData("name:\"\\\\\\n\"", "\\\n")]
        [InlineData("name:\"\\\\\\\\n\"", "\\\\n")]
        public void TestUnescape(string input, string expectedValue)
        {
            var parser = GetParser();

            var result = parser.Parse(input);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);

            if (expectedValue is null)
            {
                Assert.Empty(result.Filters);
            }
            else
            {
                Assert.Single(result.Filters);

                var filter = result.Filters.First() as TermFilter;
                Assert.NotNull(filter);
                Assert.NotNull(filter.Values);
                Assert.Single(filter.Values);

                var value = filter.Values.First();
                Assert.NotNull(value);
                Assert.Equal(expectedValue, value);
            }
        }


        private static ISearchPhraseParser GetParser()
        {
            var logger = new Mock<ILogger<SearchPhraseParser>>();

            return new SearchPhraseParser(logger.Object);
        }
    }
}
