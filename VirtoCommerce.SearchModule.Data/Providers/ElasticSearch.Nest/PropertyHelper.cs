using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections;
using Nest;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest
{
    class PropertyHelper
    {

        public static IProperty InferProperty(Type type)
        {
            type = GetUnderlyingType(type);

            if (type == typeof(string))
                return new StringProperty();

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
                        return new StringProperty();
                }
            }

            if (type == typeof(GeoLocation))
                return new GeoPointProperty();

            if (type.IsGeneric() && type.GetGenericTypeDefinition() == typeof(CompletionField<>))
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

        internal static bool IsGenericType(this Type type) => type.IsGenericType;

        internal static bool IsGenericTypeDefinition(this Type type) => type.IsGenericTypeDefinition;

        internal static bool IsValueType(this Type type) => type.IsValueType;
    }

    internal static class DotNetCoreTypeExtensions
    {
        internal static bool IsGeneric(this Type type)
        {
#if DOTNETCORE
			return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        internal static bool IsValue(this Type type)
        {
#if DOTNETCORE
			return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        internal static TypeCode GetTypeCode(this Type type)
        {
#if !DOTNETCORE
            return Type.GetTypeCode(type);
#else
			if (type == null)
				return TypeCode.Empty;
			else if (type == typeof(bool))
				return TypeCode.Boolean;
			else if (type == typeof(char))
				return TypeCode.Char;
			else if (type == typeof(sbyte))
				return TypeCode.SByte;
			else if (type == typeof(byte))
				return TypeCode.Byte;
			else if (type == typeof(short))
				return TypeCode.Int16;
			else if (type == typeof(ushort))
				return TypeCode.UInt16;
			else if (type == typeof(int))
				return TypeCode.Int32;
			else if (type == typeof(uint))
				return TypeCode.UInt32;
			else if (type == typeof(long))
				return TypeCode.Int64;
			else if (type == typeof(ulong))
				return TypeCode.UInt64;
			else if (type == typeof(float))
				return TypeCode.Single;
			else if (type == typeof(double))
				return TypeCode.Double;
			else if (type == typeof(decimal))
				return TypeCode.Decimal;
			else if (type == typeof(System.DateTime))
				return TypeCode.DateTime;
			else if (type == typeof(string))
				return TypeCode.String;
			else if (type.GetTypeInfo().IsEnum)
				return GetTypeCode(Enum.GetUnderlyingType(type));
			else
				return TypeCode.Object;
#endif
        }

#if DOTNETCORE
		internal static bool IsAssignableFrom(this Type t, Type other) => t.GetTypeInfo().IsAssignableFrom(other.GetTypeInfo());
#endif

        internal static bool IsEnumType(this Type type)
        {
#if DOTNETCORE
			return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

#if DOTNETCORE
		internal static IEnumerable<Type> GetInterfaces(this Type type)
		{
			return type.GetTypeInfo().ImplementedInterfaces;
		}
#endif
    }
}
