using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extenstions
{
    public static class DynamicPropertyTypeExtensions
    {
        public static IndexDocumentFieldValueType ToIndexedDocumentFieldValueType(this DynamicPropertyValueType type)
        {
            switch (type)
            {
                case DynamicPropertyValueType.ShortText:
                case DynamicPropertyValueType.Html:
                case DynamicPropertyValueType.LongText:
                case DynamicPropertyValueType.Image:
                    return IndexDocumentFieldValueType.String;

                case DynamicPropertyValueType.Integer:
                    return IndexDocumentFieldValueType.Integer;

                case DynamicPropertyValueType.Decimal:
                    return IndexDocumentFieldValueType.Double;

                case DynamicPropertyValueType.DateTime:
                    return IndexDocumentFieldValueType.DateTime;

                case DynamicPropertyValueType.Boolean:
                    return IndexDocumentFieldValueType.Boolean;

                default:
                    return IndexDocumentFieldValueType.Undefined;
            }
        }
    }
}
