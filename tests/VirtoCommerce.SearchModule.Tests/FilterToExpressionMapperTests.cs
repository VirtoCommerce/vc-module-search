using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

public class TestCustomerOrderLineItemEntity
{
    public string Id { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
    public decimal ListPrice { get; set; }
}

public class TestCustomerOrderEntity
{
    public string Id { get; set; }
    public string StoreId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }

    public List<TestCustomerOrderLineItemEntity> LineItems { get; set; } = new();

}


public class FilterToExpressionMapperTests
{
    private static IQueryable<TestCustomerOrderEntity> GetMockCustomerOrders()
    {
        return new List<TestCustomerOrderEntity>
        {
            new()
            {
                Id = "1",
                StoreId = "B2B-store",
                Status = "Pending",
                Total = 39,
                LineItems =
                [
                    new() { Id = "1-1", Sku = "SKU-001", Name = "Red Shirt", ListPrice = 10 },
                    new() { Id = "1-2", Sku = "SKU-002", Name = "Blue Jeans", ListPrice = 20 }
                ]
            },
            new()
            {
                Id = "2",
                StoreId = "Retail-store",
                Status = "Completed",
                Total = 15,
                LineItems =
                [
                    new() { Id = "2-1", Sku = "SKU-003", Name = "Green Hat", ListPrice = 15 }
                ]
            },
            new()
            {
                Id = "3",
                StoreId = "B2B-store",
                Status = "Completed",
                Total = 52,
                LineItems =
                [
                    new() { Id = "3-1", Sku = "SKU-004", Name = "Yellow Scarf", ListPrice = 12 },
                    new() { Id = "3-2", Sku = "SKU-001", Name = "Red Shirt", ListPrice = 10 },
                    new() { Id = "3-3", Sku = "SKU-006", Name = "Black Shoes", ListPrice = 30 }
                ]
            },
            new()
            {
                Id = "4",
                StoreId = "Retail-store",
                Status = "Pending",
                Total = 5,
                LineItems =
                [
                    new() { Id = "4-1", Sku = "SKU-007", Name = "White Socks", ListPrice = 5 }
                ]
            }
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
    public void MapTermFilter_MultiFilter_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var termFilter = new TermFilter
        {
            FieldName = "StoreId",
            Values = ["B2B-store", "Retail-store"]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(termFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(4, result.Count);
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
                new RangeFilterValue { Lower = "39", IncludeLower = true, Upper = "52", IncludeUpper = true }
            ]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(rangeFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == "1");
        Assert.Contains(result, x => x.Id == "3");
    }

    [Fact]
    public void MapTermFilter_NestedLineItemSku_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var termFilter = new TermFilter
        {
            FieldName = "LineItems.Sku",
            Values = ["SKU-004"]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(termFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("3", result.First().Id);
    }

    [Fact]
    public void MapRangeFilter_NestedLineItemSingleRange_CreatesCorrectQuery()
    {
        // Arrange
        var mockData = GetMockCustomerOrders();
        var rangeFilter = new RangeFilter
        {
            FieldName = "LineItems.ListPrice",
            Values =
            [
                new RangeFilterValue { Lower = "5", IncludeLower = true, Upper = "10", IncludeUpper = true }
            ]
        };

        // Act
        var expression = FilterToExpressionMapper.MapFilterToExpression<TestCustomerOrderEntity>(rangeFilter);
        var result = mockData.Where(expression).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, x => x.Id == "1");
        Assert.Contains(result, x => x.Id == "3");
        Assert.Contains(result, x => x.Id == "4");
    }


}
