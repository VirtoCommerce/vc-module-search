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
        var property = Expression.Property(parameter, termFilter.FieldName);
        var constant = Expression.Constant(termFilter.Values.First());
        var equality = Expression.Equal(property, constant);
        return Expression.Lambda(equality, parameter);
    }

    private static LambdaExpression MapRangeFilter(RangeFilter rangeFilter, ParameterExpression parameter)
    {
        if (rangeFilter.Values == null || !rangeFilter.Values.Any())
        {
            throw new ArgumentException("RangeFilter must contain at least one range value.");
        }

        var property = Expression.Property(parameter, rangeFilter.FieldName);
        Expression body = null;

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

            body = body == null ? rangeExpression : Expression.OrElse(body, rangeExpression);
        }

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

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
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
}
