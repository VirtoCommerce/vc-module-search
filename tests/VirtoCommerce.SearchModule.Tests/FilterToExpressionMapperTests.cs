using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

public class TestCustomerOrderEntity
{
    public string Id { get; set; }
    public string StoreId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
}


public class FilterToExpressionMapperTests
{
    private IQueryable<TestCustomerOrderEntity> GetMockCustomerOrders()
    {
        return new List<TestCustomerOrderEntity>
            {
                new() { Id = "1", StoreId = "B2B-store", Status = "Pending", Total = 1  },
                new() { Id = "2", StoreId = "Retail-store", Status = "Completed", Total = 2 },
                new() { Id = "3", StoreId = "B2B-store", Status = "Completed", Total = 3 },
                new() { Id = "4", StoreId = "Retail-store", Status = "Pending", Total = 4 }
            }.AsQueryable();
    }

    [Fact]
    public void MapTermFilter_SingleFilter_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var termFilter = new TermFilter
        {
            FieldName = "StoreId",
            Values = ["B2B-store"]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(termFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("1", result.First().Id);
        Assert.Equal("3", result.Last().Id);
    }

    [Fact]
    public void MapOrFilter_MultipleFilters_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var orFilter = new OrFilter
        {
            ChildFilters =
                [
                    new TermFilter { FieldName = "StoreId", Values = ["B2B-store"] },
                    new TermFilter { FieldName = "Status", Values = ["Pending"] }
                ]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(orFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, x => x.Id == "1");
        Assert.Contains(result, x => x.Id == "3");
        Assert.Contains(result, x => x.Id == "4");
    }

    [Fact]
    public void MapAndFilter_MultipleFilters_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var andFilter = new AndFilter
        {
            ChildFilters =
                [
                    new TermFilter { FieldName = "StoreId", Values = ["B2B-store"] },
                    new TermFilter { FieldName = "Status", Values = ["Pending"] }
                ]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(andFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("1", result.First().Id);
    }

    [Fact]
    public void MapNotFilter_NegatesFilter_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var notFilter = new NotFilter
        {
            ChildFilter = new TermFilter { FieldName = "StoreId", Values = ["B2B-store"] }
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(notFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.StoreId == "B2B-store");
    }


    [Fact]
    public void MapRangeFilter_SingleRange_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var rangeFilter = new RangeFilter
        {
            FieldName = "Total",
            Values =
            [
                new RangeFilterValue { Lower = "2", IncludeLower = true, Upper = "3", IncludeUpper = true }
            ]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(rangeFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == "2");
        Assert.Contains(result, x => x.Id == "3");
    }


}
