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

            Assert.Single(result.Filters);
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
        public void Mix_Filter_And_Keyword_In_Quotation_Marks()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red \"B2B, test\"");

            Assert.Equal("B2B, test", result.Keyword);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value);
        }

        [Fact]
        public void Mix_MultiValue_Filter_And_Keyword_In_Quotation_Marks8()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red,blue \"B2B, test\"");

            Assert.Equal("B2B, test", result.Keyword);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Count);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value);

            var value2 = filter.Values.Last();
            Assert.NotNull(value2);
            Assert.Equal("blue", value2);

        }


        [Fact]
        public void Mix_Filter_And_Keyword_Dot()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red B2B.@# test");
            var result_quoted = parser.Parse("color:red \"B2B. test\"");
            var result_reverse = parser.Parse("B2B.@# test color:red");

            Assert.Equal("B2B. test", result.Keyword);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value);

            result.Should().BeEquivalentTo(result_quoted);
            result.Should().BeEquivalentTo(result_reverse);
        }

        [Fact]
        public void Mix_Filter_And_Keyword_Russian()
        {
            var parser = GetParser();
            var result = parser.Parse("color:Red123 Привет мир!");
            var result_quoted = parser.Parse("color:Red123 \"Привет мир\"");
            var result_reverse = parser.Parse("Привет мир! color:Red123");

            Assert.Equal("Привет мир", result.Keyword);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("Red123", value);

            result.Should().BeEquivalentTo(result_quoted);
            result.Should().BeEquivalentTo(result_reverse);
        }

        [Fact]
        public void MultipleFilters()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red brand:apple");

            Assert.Equal(string.Empty, result.Keyword);
            Assert.Equal(2, result.Filters.Count);

            var colorFilter = result.Filters.First() as TermFilter;
            Assert.NotNull(colorFilter);
            Assert.Equal("color", colorFilter.FieldName);
            Assert.NotNull(colorFilter.Values);
            Assert.Single(colorFilter.Values);

            var brandFilter = result.Filters.Last() as TermFilter;
            Assert.NotNull(brandFilter);
            Assert.Equal("brand", brandFilter.FieldName);
            Assert.NotNull(brandFilter.Values);
            Assert.Single(brandFilter.Values);
        }

        [Fact]
        public void Mix_Filter_And_Keyword()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red B2B, test");
            var result_quoted = parser.Parse("color:red \"B2B test\"");

            var result_reverse = parser.Parse("B2B, test color:red");

            Assert.Equal("B2B test", result.Keyword);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Single(filter.Values);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value);

            result.Should().BeEquivalentTo(result_quoted);
            result.Should().BeEquivalentTo(result_reverse);
        }

        [Fact]
        public void Mix_MultiValue_Filter_And_Keyword()
        {
            var parser = GetParser();
            var result = parser.Parse("color:red,blue B2B, test");
            var result_quoted = parser.Parse("color:red,blue \"B2B test\"");

            var result_reverse = parser.Parse("B2B, test color:red,blue");

            Assert.Equal("B2B test", result.Keyword);
            Assert.Single(result.Filters);

            var filter = result.Filters.First() as TermFilter;
            Assert.NotNull(filter);
            Assert.Equal("color", filter.FieldName);
            Assert.NotNull(filter.Values);
            Assert.Equal(2, filter.Values.Count);

            var value = filter.Values.First();
            Assert.NotNull(value);
            Assert.Equal("red", value);

            var value2 = filter.Values.Last();
            Assert.NotNull(value2);
            Assert.Equal("blue", value2);


            result.Should().BeEquivalentTo(result_quoted);
            result.Should().BeEquivalentTo(result_reverse);
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


        [Fact]
        public void TestCategorySubtreePriceAndBrand()
        {
            var parser = GetParser();
            var result = parser.Parse("category.subtree:e7499b13-c61f-4bf9-9cc7-4c2b00903771 price.USD:(0 TO) \"BRAND\":\"Amstel\"");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Equal(3, result.Filters.Count);

            var categoryFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "category.subtree") as TermFilter;
            Assert.NotNull(categoryFilter);
            Assert.NotNull(categoryFilter.Values);
            Assert.Single(categoryFilter.Values);
            Assert.Equal("e7499b13-c61f-4bf9-9cc7-4c2b00903771", categoryFilter.Values.First());

            var priceFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "price.USD") as RangeFilter;
            Assert.NotNull(priceFilter);
            Assert.NotNull(priceFilter.Values);
            Assert.Single(priceFilter.Values);
            var priceRange = priceFilter.Values.First();
            Assert.NotNull(priceRange);
            Assert.Equal("0", priceRange.Lower);
            Assert.Null(priceRange.Upper);
            Assert.False(priceRange.IncludeLower);
            Assert.False(priceRange.IncludeUpper);

            var brandFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "BRAND") as TermFilter;
            Assert.NotNull(brandFilter);
            Assert.NotNull(brandFilter.Values);
            Assert.Single(brandFilter.Values);
            Assert.Equal("Amstel", brandFilter.Values.First());
        }

        [Fact]
        public void TestPermalinkFilter_With_Escaping()
        {
            var parser = GetParser();
            var result = parser.Parse("permalink:\"/catalog/product\"");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var permalinkFilter = result.Filters.First() as TermFilter;
            Assert.NotNull(permalinkFilter);
            Assert.Equal("permalink", permalinkFilter.FieldName);
            Assert.NotNull(permalinkFilter.Values);
            Assert.Single(permalinkFilter.Values);
            Assert.Equal("/catalog/product", permalinkFilter.Values.First());
        }

        [Fact]
        public void TestPermalinkFilter_Without_Escaping()
        {
            var parser = GetParser();
            var result = parser.Parse("permalink:/catalog/product");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var permalinkFilter = result.Filters.First() as TermFilter;
            Assert.NotNull(permalinkFilter);
            Assert.Equal("permalink", permalinkFilter.FieldName);
            Assert.NotNull(permalinkFilter.Values);
            Assert.Single(permalinkFilter.Values);
            Assert.Equal("/catalog/product", permalinkFilter.Values.First());
        }

        [Fact]
        public void TestCategorySubtreePriceBrandAndAvailability()
        {
            var parser = GetParser();
            var result = parser.Parse("category.subtree:fc596540864a41bf8ab78734ee7353a3/cda4eb77-5ee5-492e-89f5-fa0954dfcfbb price.USD:(0 TO) \"BRAND\":\"Affligem\" availability:InStock");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Equal(4, result.Filters.Count);

            var categoryFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "category.subtree") as TermFilter;
            Assert.NotNull(categoryFilter);
            Assert.NotNull(categoryFilter.Values);
            Assert.Single(categoryFilter.Values);
            Assert.Equal("fc596540864a41bf8ab78734ee7353a3/cda4eb77-5ee5-492e-89f5-fa0954dfcfbb", categoryFilter.Values.First());

            var priceFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "price.USD") as RangeFilter;
            Assert.NotNull(priceFilter);
            Assert.NotNull(priceFilter.Values);
            Assert.Single(priceFilter.Values);
            var priceRange = priceFilter.Values.First();
            Assert.NotNull(priceRange);
            Assert.Equal("0", priceRange.Lower);
            Assert.Null(priceRange.Upper);
            Assert.False(priceRange.IncludeLower);
            Assert.False(priceRange.IncludeUpper);

            var brandFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "BRAND") as TermFilter;
            Assert.NotNull(brandFilter);
            Assert.NotNull(brandFilter.Values);
            Assert.Single(brandFilter.Values);
            Assert.Equal("Affligem", brandFilter.Values.First());

            var availabilityFilter = result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "availability") as TermFilter;
            Assert.NotNull(availabilityFilter);
            Assert.NotNull(availabilityFilter.Values);
            Assert.Single(availabilityFilter.Values);
            Assert.Equal("InStock", availabilityFilter.Values.First());
        }


        private static ISearchPhraseParser GetParser()
        {
            var logger = new Mock<ILogger<SearchPhraseParser>>();

            return new SearchPhraseParser(logger.Object);
        }
    }
}
