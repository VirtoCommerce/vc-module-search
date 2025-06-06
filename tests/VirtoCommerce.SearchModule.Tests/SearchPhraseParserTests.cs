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
        /// This test demonstrates that currrent grammar doesn't failed and continue parcing next filters if date time value is not wrapped with double quotes, check logs for the details.
        /// </summary>
        [Fact]
        public void TestWrongDateTimeWithoutQuoеtes()
        {
            var parser = GetParser();
            var result = parser.Parse("createddate:[2023-12-01T01:12:00Z TO] customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\"");

            Assert.Equal(string.Empty, result.Keyword);
            Assert.Equal(2, result.Filters.Count);
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "createddate"));
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId"));
            Assert.Equal("78b0208a-bb52-4a33-9250-583d63aa1f77", ((TermFilter)result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId")).Values[0]);

        }

        /// <summary>
        /// This test demonstrates that currrent grammar doesn't failed and continue parcing next filters if date time value is not wrapped with double quotes, check logs for the details.
        /// </summary>
        [Fact]
        public void TestWrongDateTimeWithoutQuoеtes2()
        {
            var parser = GetParser();
            var result = parser.Parse("customerId:\"78b0208a-bb52-4a33-9250-583d63aa1f77\" createddate:[2023-12-01T01:12:00Z TO] color:Black,White");

            Assert.Equal(string.Empty, result.Keyword);
            Assert.Equal(3, result.Filters.Count);
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId"));
            Assert.Equal("78b0208a-bb52-4a33-9250-583d63aa1f77", ((TermFilter)result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "customerId")).Values[0]);
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "createddate"));
            Assert.NotNull(result.Filters.FirstOrDefault(x => (x as INamedFilter).FieldName == "color"));
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
            var result = parser.Parse("color:Red123 Привет мир");
            var result_quoted = parser.Parse("color:Red123 \"Привет мир\"");
            var result_reverse = parser.Parse("Привет мир color:Red123");

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


        [Fact]
        public void TestNotAttributeFilter()
        {
            var parser = GetParser();
            var result = parser.Parse("!color:red,blue");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var notFilter = result.Filters.First() as NotFilter;
            Assert.NotNull(notFilter);
            var filter = notFilter.ChildFilter as TermFilter;
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
        public void TestNotAttributeFilterWithOthers()
        {
            var parser = GetParser();
            var result = parser.Parse("size:small !color:red,blue");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Equal(2, result.Filters.Count);

            var notFilter = result.Filters.Last() as NotFilter;
            Assert.NotNull(notFilter);
            var filter = notFilter.ChildFilter as TermFilter;
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
        public void ExclamationMarkShouldBeEscaped()
        {
            var parser = GetParser();
            var result = parser.Parse("name:\"Hello World!\" !brand:\"Apple!\"");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Equal(2, result.Filters.Count);

            var firstFilter = result.Filters.First() as TermFilter;
            Assert.NotNull(firstFilter);
            Assert.Equal("name", firstFilter.FieldName);
            Assert.NotNull(firstFilter.Values);

            var firstValue = firstFilter.Values.First();
            Assert.NotNull(firstValue);
            Assert.Equal("Hello World!", firstValue);

            var notSecondFilter = result.Filters.Last() as NotFilter;
            Assert.NotNull(notSecondFilter);
            var secondFilter = notSecondFilter.ChildFilter as TermFilter;
            Assert.NotNull(secondFilter);
            Assert.Equal("brand", secondFilter.FieldName);
            Assert.NotNull(secondFilter.Values);

            var secondValue = secondFilter.Values.First();
            Assert.NotNull(secondValue);
            Assert.Equal("Apple!", secondValue);
        }

        [Fact]
        public void TestOrConditionBetweenTerms()
        {
            var parser = GetParser();
            var result = parser.Parse("category:books OR color:red");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var orFilter = result.Filters.First() as OrFilter;
            Assert.NotNull(orFilter);
            Assert.NotNull(orFilter.ChildFilters);
            Assert.Equal(2, orFilter.ChildFilters.Count);

            var categoryFilter = orFilter.ChildFilters.First() as TermFilter;
            Assert.NotNull(categoryFilter);
            Assert.Equal("category", categoryFilter.FieldName);
            Assert.NotNull(categoryFilter.Values);
            Assert.Single(categoryFilter.Values);
            Assert.Equal("books", categoryFilter.Values.First());

            var colorFilter = orFilter.ChildFilters.Last() as TermFilter;
            Assert.NotNull(colorFilter);
            Assert.Equal("color", colorFilter.FieldName);
            Assert.NotNull(colorFilter.Values);
            Assert.Single(colorFilter.Values);
            Assert.Equal("red", colorFilter.Values.First());
        }

        [Fact]
        public void TestAndConditionBetweenTerms()
        {
            var parser = GetParser();
            var result = parser.Parse("category:books AND color:red");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var andFilter = result.Filters.First() as AndFilter;
            Assert.NotNull(andFilter);
            Assert.NotNull(andFilter.ChildFilters);
            Assert.Equal(2, andFilter.ChildFilters.Count);

            var categoryFilter = andFilter.ChildFilters.First() as TermFilter;
            Assert.NotNull(categoryFilter);
            Assert.Equal("category", categoryFilter.FieldName);
            Assert.NotNull(categoryFilter.Values);
            Assert.Single(categoryFilter.Values);
            Assert.Equal("books", categoryFilter.Values.First());

            var colorFilter = andFilter.ChildFilters.Last() as TermFilter;
            Assert.NotNull(colorFilter);
            Assert.Equal("color", colorFilter.FieldName);
            Assert.NotNull(colorFilter.Values);
            Assert.Single(colorFilter.Values);
            Assert.Equal("red", colorFilter.Values.First());
        }


        [Fact]
        public void TestOrConditionWithMultipleTermsAndKeyword()
        {
            var parser = GetParser();
            var result = parser.Parse("category:books OR color:red OR size:XL ABC 123");

            Assert.NotNull(result);
            Assert.Equal("ABC 123", result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var orFilter = result.Filters.First() as OrFilter;
            Assert.NotNull(orFilter);
            Assert.NotNull(orFilter.ChildFilters);
            Assert.Equal(2, orFilter.ChildFilters.Count);

            var orFilter2 = orFilter.ChildFilters.First() as OrFilter;

            var categoryFilter = orFilter2.ChildFilters.First() as TermFilter;
            Assert.NotNull(categoryFilter);
            Assert.Equal("category", categoryFilter.FieldName);
            Assert.NotNull(categoryFilter.Values);
            Assert.Single(categoryFilter.Values);
            Assert.Equal("books", categoryFilter.Values.First());

            var colorFilter = orFilter2.ChildFilters.ElementAt(1) as TermFilter;
            Assert.NotNull(colorFilter);
            Assert.Equal("color", colorFilter.FieldName);
            Assert.NotNull(colorFilter.Values);
            Assert.Single(colorFilter.Values);
            Assert.Equal("red", colorFilter.Values.First());

            var sizeFilter = orFilter.ChildFilters.Last() as TermFilter;
            Assert.NotNull(sizeFilter);
            Assert.Equal("size", sizeFilter.FieldName);
            Assert.NotNull(sizeFilter.Values);
            Assert.Single(sizeFilter.Values);
            Assert.Equal("XL", sizeFilter.Values.First());
        }

        [Fact]
        public void Test_And_Or_Parentheses_Complex_Expression()
        {
            var parser = GetParser();
            var result = parser.Parse("gtin:44 AND (code:ZQL-92511859 OR itemLineNOM:99)");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var andFilter = result.Filters.First() as AndFilter;
            Assert.NotNull(andFilter);
            Assert.NotNull(andFilter.ChildFilters);
            Assert.Equal(2, andFilter.ChildFilters.Count);

            var gtinFilter = andFilter.ChildFilters.First() as TermFilter;
            Assert.NotNull(gtinFilter);
            Assert.Equal("gtin", gtinFilter.FieldName);
            Assert.NotNull(gtinFilter.Values);
            Assert.Single(gtinFilter.Values);
            Assert.Equal("44", gtinFilter.Values.First());

            var orFilter = andFilter.ChildFilters.Last() as OrFilter;
            Assert.NotNull(orFilter);
            Assert.NotNull(orFilter.ChildFilters);
            Assert.Equal(2, orFilter.ChildFilters.Count);

            var codeFilter = orFilter.ChildFilters.First() as TermFilter;
            Assert.NotNull(codeFilter);
            Assert.Equal("code", codeFilter.FieldName);
            Assert.NotNull(codeFilter.Values);
            Assert.Single(codeFilter.Values);
            Assert.Equal("ZQL-92511859", codeFilter.Values.First());

            var itemLineNOMFilter = orFilter.ChildFilters.Last() as TermFilter;
            Assert.NotNull(itemLineNOMFilter);
            Assert.Equal("itemLineNOM", itemLineNOMFilter.FieldName);
            Assert.NotNull(itemLineNOMFilter.Values);
            Assert.Single(itemLineNOMFilter.Values);
            Assert.Equal("99", itemLineNOMFilter.Values.First());
        }

        [Fact]
        public void Test_OrCondition_With_AndGroups_And_Parentheses()
        {
            var parser = GetParser();
            var result = parser.Parse("(gtin:44 AND code:ZQL-92511859) OR (gtin:44 AND itemLineNOM:99)");

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var orFilter = result.Filters.First() as OrFilter;
            Assert.NotNull(orFilter);
            Assert.NotNull(orFilter.ChildFilters);
            Assert.Equal(2, orFilter.ChildFilters.Count);

            // First AND group: gtin:44 AND code:ZQL-92511859
            var andFilter1 = orFilter.ChildFilters.First() as AndFilter;
            Assert.NotNull(andFilter1);
            Assert.Equal(2, andFilter1.ChildFilters.Count);

            var gtinFilter1 = andFilter1.ChildFilters.First() as TermFilter;
            Assert.NotNull(gtinFilter1);
            Assert.Equal("gtin", gtinFilter1.FieldName);
            Assert.Single(gtinFilter1.Values);
            Assert.Equal("44", gtinFilter1.Values.First());

            var codeFilter = andFilter1.ChildFilters.Last() as TermFilter;
            Assert.NotNull(codeFilter);
            Assert.Equal("code", codeFilter.FieldName);
            Assert.Single(codeFilter.Values);
            Assert.Equal("ZQL-92511859", codeFilter.Values.First());

            // Second AND group: gtin:44 AND itemLineNOM:99
            var andFilter2 = orFilter.ChildFilters.Last() as AndFilter;
            Assert.NotNull(andFilter2);
            Assert.Equal(2, andFilter2.ChildFilters.Count);

            var gtinFilter2 = andFilter2.ChildFilters.First() as TermFilter;
            Assert.NotNull(gtinFilter2);
            Assert.Equal("gtin", gtinFilter2.FieldName);
            Assert.Single(gtinFilter2.Values);
            Assert.Equal("44", gtinFilter2.Values.First());

            var itemLineNOMFilter = andFilter2.ChildFilters.Last() as TermFilter;
            Assert.NotNull(itemLineNOMFilter);
            Assert.Equal("itemLineNOM", itemLineNOMFilter.FieldName);
            Assert.Single(itemLineNOMFilter.Values);
            Assert.Equal("99", itemLineNOMFilter.Values.First());
        }

        [Fact]
        public void Test_And_Or_Parentheses_Complex_With_Keyword_Expression()
        {
            var parser = GetParser();
            var result = parser.Parse("keywordbefore gtin:44 AND (code:ZQL-92511859 OR itemLineNOM:99) keywordafter");

            Assert.NotNull(result);
            Assert.Equal("keywordbefore keywordafter", result.Keyword);
            Assert.NotNull(result.Filters);
            Assert.Single(result.Filters);

            var andFilter = result.Filters.First() as AndFilter;
            Assert.NotNull(andFilter);
            Assert.NotNull(andFilter.ChildFilters);
            Assert.Equal(2, andFilter.ChildFilters.Count);

            var gtinFilter = andFilter.ChildFilters.First() as TermFilter;
            Assert.NotNull(gtinFilter);
            Assert.Equal("gtin", gtinFilter.FieldName);
            Assert.NotNull(gtinFilter.Values);
            Assert.Single(gtinFilter.Values);
            Assert.Equal("44", gtinFilter.Values.First());

            var orFilter = andFilter.ChildFilters.Last() as OrFilter;
            Assert.NotNull(orFilter);
            Assert.NotNull(orFilter.ChildFilters);
            Assert.Equal(2, orFilter.ChildFilters.Count);

            var codeFilter = orFilter.ChildFilters.First() as TermFilter;
            Assert.NotNull(codeFilter);
            Assert.Equal("code", codeFilter.FieldName);
            Assert.NotNull(codeFilter.Values);
            Assert.Single(codeFilter.Values);
            Assert.Equal("ZQL-92511859", codeFilter.Values.First());

            var itemLineNOMFilter = orFilter.ChildFilters.Last() as TermFilter;
            Assert.NotNull(itemLineNOMFilter);
            Assert.Equal("itemLineNOM", itemLineNOMFilter.FieldName);
            Assert.NotNull(itemLineNOMFilter.Values);
            Assert.Single(itemLineNOMFilter.Values);
            Assert.Equal("99", itemLineNOMFilter.Values.First());
        }

        private static ISearchPhraseParser GetParser()
        {
            var logger = new Mock<ILogger<SearchPhraseParser>>();

            return new SearchPhraseParser(logger.Object);
        }
    }
}
