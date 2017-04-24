using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nest;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch
{
    internal class PropertyHelper
    {
        public static IProperty InferProperty(IDocumentField field)
        {
            var type = field.Value?.GetType() ?? typeof(object);

            type = GetUnderlyingType(type);

            if (type == typeof(string))
            {
                if (field.ContainsAttribute(IndexType.NotAnalyzed))
                    return new KeywordProperty();

                return new TextProperty();
            }

            if (type.IsEnumType())
                return new NumberProperty(NumberType.Integer);

            if (type.IsValue())
            {
                switch (type.Name)
                {
                    case "Int32":
                    case "UInt16":
                        return new NumberProperty(NumberType.Integer);
                    case "Int16":
                    case "Byte":
                        return new NumberProperty(NumberType.Short);
                    case "SByte":
                        return new NumberProperty(NumberType.Byte);
                    case "Int64":
                    case "UInt32":
                    case "TimeSpan":
                        return new NumberProperty(NumberType.Long);
                    case "Single":
                        return new NumberProperty(NumberType.Float);
                    case "Decimal":
                    case "Double":
                    case "UInt64":
                        return new NumberProperty(NumberType.Double);
                    case "DateTime":
                    case "DateTimeOffset":
                        return new DateProperty();
                    case "Boolean":
                        return new BooleanProperty();
                    case "Char":
                    case "Guid":
                        return new KeywordProperty();
                }
            }

            if (type == typeof(GeoLocation))
                return new GeoPointProperty();

            if (type.IsGeneric() && type.GetGenericTypeDefinition() == typeof(CompletionField))
                return new CompletionProperty();

            if (type == typeof(Attachment))
                return new AttachmentProperty();

            return new ObjectProperty();
        }

        private static Type GetUnderlyingType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && type.GetGenericArguments().Length == 1
                && (typeInfo.ImplementedInterfaces.HasAny(t => t == typeof(IEnumerable)) || Nullable.GetUnderlyingType(type) != null))
                return type.GetGenericArguments()[0];

            return type;
        }
    }

    internal static class TypeExtensions
    {
        internal static bool HasAny<T>(this IEnumerable<T> list, Func<T, bool> predicate)
        {
            return list != null && list.Any(predicate);
        }

        internal static bool HasAny<T>(this IEnumerable<T> list)
        {
            return list != null && list.Any();
        }

        internal static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        internal static bool IsGenericTypeDefinition(this Type type)
        {
            return type.IsGenericTypeDefinition;
        }

        internal static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }
    }

    internal static class DotNetCoreTypeExtensions
    {
        internal static bool IsGeneric(this Type type)
        {
            return type.IsGenericType;
        }

        internal static bool IsValue(this Type type)
        {
            return type.IsValueType;
        }

        internal static TypeCode GetTypeCode(this Type type)
        {
            return Type.GetTypeCode(type);
        }

        internal static bool IsEnumType(this Type type)
        {
            return type.IsEnum;
        }
    }
}
