using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extensions
{
    public static class DynamicPropertyTypeExtensions
    {
        public static IndexDocumentFieldValueType ToIndexedDocumentFieldValueType(this DynamicPropertyValueType type)
        {
            return type switch
            {
                DynamicPropertyValueType.ShortText => IndexDocumentFieldValueType.String,
                DynamicPropertyValueType.Html => IndexDocumentFieldValueType.String,
                DynamicPropertyValueType.LongText => IndexDocumentFieldValueType.String,
                DynamicPropertyValueType.Image => IndexDocumentFieldValueType.String,
                DynamicPropertyValueType.Integer => IndexDocumentFieldValueType.Integer,
                DynamicPropertyValueType.Decimal => IndexDocumentFieldValueType.Decimal,
                DynamicPropertyValueType.DateTime => IndexDocumentFieldValueType.DateTime,
                DynamicPropertyValueType.Boolean => IndexDocumentFieldValueType.Boolean,
                DynamicPropertyValueType.Undefined => IndexDocumentFieldValueType.Undefined,
                _ => IndexDocumentFieldValueType.Undefined
            };
        }
    }
}
