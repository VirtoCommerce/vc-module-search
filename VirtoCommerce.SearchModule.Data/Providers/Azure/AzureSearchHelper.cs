using System.Text;

namespace VirtoCommerce.SearchModule.Data.Providers.Azure
{
    public static class AzureSearchHelper
    {
        public const string FieldNamePrefix = "f_";

        public static string ToAzureFieldName(string fieldName)
        {
            return FieldNamePrefix + fieldName.ToLowerInvariant();
        }

        public static string FromAzureFieldName(string azureFieldName)
        {
            return azureFieldName.StartsWith(FieldNamePrefix) ? azureFieldName.Substring(FieldNamePrefix.Length) : azureFieldName;
        }

        public static string JoinNonEmptyStrings(string separator, bool encloseInParenthesis, params string[] values)
        {
            var builder = new StringBuilder();
            var valuesCount = 0;

            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (valuesCount > 0)
                    {
                        builder.Append(separator);
                    }

                    builder.Append(value);
                    valuesCount++;
                }
            }

            if (valuesCount > 1 && encloseInParenthesis)
            {
                builder.Insert(0, "(");
                builder.Append(")");
            }

            return builder.ToString();
        }
    }
}
