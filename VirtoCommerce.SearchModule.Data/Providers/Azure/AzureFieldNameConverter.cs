namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{
    public class AzureFieldNameConverter
    {
        public const string FieldNamePrefix = "f_";

        public static string ToAzureFieldName(string fieldName)
        {
            return FieldNamePrefix + fieldName;
        }

        public static string FromAzureFieldName(string azureFieldName)
        {
            return azureFieldName.StartsWith(FieldNamePrefix) ? azureFieldName.Substring(FieldNamePrefix.Length) : azureFieldName;
        }
    }
}
