using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extensions;

/// <summary>
/// Allows mapping of search filters to LINQ expressions, with support for custom filter mappers.
/// </summary>
public static class FilterToExpressionMapper
{
    // Registry for filter type to mapping function
    private static readonly ConcurrentDictionary<Type, Delegate> _filterMappers = new();

    static FilterToExpressionMapper()
    {
        // Register built-in filter mappers
        Register<OrFilter>(MapOrFilter);
        Register<AndFilter>(MapAndFilter);
        Register<TermFilter>(MapTermFilter);
        Register<RangeFilter>(MapRangeFilter);
        Register<NotFilter>(MapNotFilter);
    }

    /// <summary>
    /// Registers a custom filter mapping function.
    /// </summary>
    /// <code>
    /// // You can now add support for new filter types like this:
    /// FilterToExpressionMapper.Register<MyCustomFilter>((filter, parameter) => {
    /// return a LambdaExpression for your filter
    /// });
    /// </code>
    public static void Register<TFilter>(Func<TFilter, ParameterExpression, LambdaExpression> mapper)
        where TFilter : IFilter
    {
        _filterMappers[typeof(TFilter)] = mapper;
    }

    /// <summary>
    /// Unregisters a filter mapping function.
    /// </summary>
    /// <code>
    /// // You can now add support for new filter types like this:
    /// FilterToExpressionMapper.Unregister<MyCustomFilter>();
    /// });
    /// </code> 
    public static void Unregister<TFilter>() where TFilter : IFilter
    {
        _filterMappers.TryRemove(typeof(TFilter), out _);
    }

    public static IQueryable<TEntity> ApplyFilters<TEntity>(this IQueryable<TEntity> query, IList<IFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        foreach (var filter in filters)
        {
            var expression = MapFilterToExpression<TEntity>(filter);
            query = query.Where(expression);
        }

        return query;
    }

    public static Expression<Func<TEntity, bool>> MapFilterToExpression<TEntity>(IFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var filterType = filter.GetType();
        if (_filterMappers.TryGetValue(filterType, out var mapper))
        {
            var parameter = Expression.Parameter(typeof(TEntity), "entity");
            var lambda = (LambdaExpression)mapper.DynamicInvoke(filter, parameter);
            return (Expression<Func<TEntity, bool>>)lambda;
        }

        throw new NotSupportedException($"Filter type {filterType.Name} is not supported.");
    }

    // Built-in mappers

    private static LambdaExpression MapAndFilter(AndFilter andFilter, ParameterExpression parameter)
    {
        Expression body = null;
        foreach (var child in andFilter.ChildFilters)
        {
            var lambda = MapFilterToExpressionInternal(child, parameter);
            body = body == null ? lambda.Body : Expression.AndAlso(body, lambda.Body);
        }
        return Expression.Lambda(body ?? Expression.Constant(true), parameter);
    }

    private static LambdaExpression MapOrFilter(OrFilter orFilter, ParameterExpression parameter)
    {
        Expression body = null;
        foreach (var child in orFilter.ChildFilters)
        {
            var lambda = MapFilterToExpressionInternal(child, parameter);
            body = body == null ? lambda.Body : Expression.OrElse(body, lambda.Body);
        }
        return Expression.Lambda(body ?? Expression.Constant(false), parameter);
    }

    private static LambdaExpression MapTermFilter(TermFilter termFilter, ParameterExpression parameter)
    {
        var propertyPath = termFilter.FieldName.Split('.');

        if (termFilter.Values.Count == 1)
        {
            var body = BuildNestedPropertyOrAnyExpression(
                parameter,
                propertyPath,
                0,
                property =>
                {
                    var constant = Expression.Constant(termFilter.Values.First());
                    return Expression.Equal(property, constant);
                });

            return Expression.Lambda(body, parameter);
        }
        else
        {
            var body = BuildNestedPropertyOrAnyExpression(
                parameter,
                propertyPath,
                0,
                property =>
                {
                    var values = termFilter.Values.Select(v => Expression.Constant(v, property.Type)).ToArray();
                    var valuesArray = Expression.NewArrayInit(property.Type, values);
                    return Expression.Call(typeof(Enumerable), "Contains", new[] { property.Type }, valuesArray, property);
                });

            return Expression.Lambda(body, parameter);
        }
    }


    private static LambdaExpression MapRangeFilter(RangeFilter rangeFilter, ParameterExpression parameter)
    {
        if (rangeFilter.Values == null || !rangeFilter.Values.Any())
        {
            throw new ArgumentException("RangeFilter must contain at least one range value.");
        }

        var propertyPath = rangeFilter.FieldName.Split('.');

        Expression body = BuildNestedPropertyOrAnyExpression(
            parameter,
            propertyPath,
            0,
            property =>
            {
                Expression rangeBody = null;
                foreach (var range in rangeFilter.Values)
                {
                    var lower = TryParseConstant(range.Lower, property.Type);
                    var upper = TryParseConstant(range.Upper, property.Type);

                    Expression lowerBound = null;
                    Expression upperBound = null;

                    if (lower != null)
                    {
                        lowerBound = range.IncludeLower
                            ? Expression.GreaterThanOrEqual(property, lower)
                            : Expression.GreaterThan(property, lower);
                    }

                    if (upper != null)
                    {
                        upperBound = range.IncludeUpper
                            ? Expression.LessThanOrEqual(property, upper)
                            : Expression.LessThan(property, upper);
                    }

                    Expression rangeExpression;
                    if (lowerBound != null && upperBound != null)
                    {
                        rangeExpression = Expression.AndAlso(lowerBound, upperBound);
                    }
                    else if (lowerBound != null)
                    {
                        rangeExpression = lowerBound;
                    }
                    else if (upperBound != null)
                    {
                        rangeExpression = upperBound;
                    }
                    else
                    {
                        continue;
                    }

                    rangeBody = rangeBody == null ? rangeExpression : Expression.OrElse(rangeBody, rangeExpression);
                }
                return rangeBody;
            });

        return Expression.Lambda(body, parameter);
    }


    private static LambdaExpression MapNotFilter(NotFilter notFilter, ParameterExpression parameter)
    {
        var inner = MapFilterToExpressionInternal(notFilter.ChildFilter, parameter);
        var negation = Expression.Not(inner.Body);
        return Expression.Lambda(negation, parameter);
    }

    // Helper to map any filter using the registry, reusing the same parameter
    private static LambdaExpression MapFilterToExpressionInternal(IFilter filter, ParameterExpression parameter)
    {
        var filterType = filter.GetType();
        if (_filterMappers.TryGetValue(filterType, out var mapper))
        {
            return (LambdaExpression)mapper.DynamicInvoke(filter, parameter);
        }
        throw new NotSupportedException($"Filter type {filterType.Name} is not supported.");
    }

    private static ConstantExpression TryParseConstant(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        object parsed;
        try
        {
            parsed = Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
        }
        catch
        {
            throw new InvalidCastException($"Cannot convert '{value}' to type {targetType.Name}");
        }
        return Expression.Constant(parsed, targetType);
    }

    private static Expression BuildNestedPropertyOrAnyExpression(Expression parameter, string[] propertyPath, int pathIndex, Func<Expression, Expression> leafPredicateBuilder)
    {
        var member = propertyPath[pathIndex];
        var memberInfo = parameter.Type.GetProperty(member) ?? throw new InvalidOperationException($"Property '{member}' not found on type '{parameter.Type.Name}'.");

        // If this is a collection and not the last segment, use Any
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(memberInfo.PropertyType) &&
            memberInfo.PropertyType != typeof(string) &&
            pathIndex < propertyPath.Length - 1)
        {
            var elementType = memberInfo.PropertyType.IsGenericType
                ? memberInfo.PropertyType.GetGenericArguments()[0]
                : typeof(object);

            var innerParameter = Expression.Parameter(elementType, "x");
            var innerBody = BuildNestedPropertyOrAnyExpression(
                innerParameter,
                propertyPath,
                pathIndex + 1,
                leafPredicateBuilder);

            var anyCall = Expression.Call(
                typeof(Enumerable),
                "Any",
                [elementType],
                Expression.Property(parameter, member),
                Expression.Lambda(innerBody, innerParameter)
            );
            return anyCall;
        }
        else if (pathIndex == propertyPath.Length - 1)
        {
            // Leaf property
            var propertyExpr = Expression.Property(parameter, member);
            return leafPredicateBuilder(propertyExpr);
        }
        else
        {
            // Continue traversing
            var propertyExpr = Expression.Property(parameter, member);
            return BuildNestedPropertyOrAnyExpression(propertyExpr, propertyPath, pathIndex + 1, leafPredicateBuilder);
        }
    }
}
