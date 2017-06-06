using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchModule.Data.Providers.AzureSearch
{
    public static class AzureSearchHelper
    {
        public const string FieldNamePrefix = "f_";
        public const string RawKeyFieldName = "__key";
        public const string KeyFieldName = FieldNamePrefix + RawKeyFieldName;
        public const string NonExistentFieldFilter = KeyFieldName + " eq ''";

        public static string ToAzureFieldName(string fieldName)
        {
            return FieldNamePrefix + Regex.Replace(fieldName, @"\W", "_").ToLowerInvariant();
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

        public static IList<string> GetPriceFieldNames(string fieldName, string currency, IList<string> pricelists, bool alwaysAddEmptyPricelist)
        {
            var actualPricelists = new List<string>();

            if (pricelists != null)
            {
                actualPricelists.AddRange(pricelists
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }

            if (alwaysAddEmptyPricelist || !actualPricelists.Any())
            {
                actualPricelists.Insert(0, null);
            }

            var azureFieldNames = actualPricelists.Select(p =>
                ToAzureFieldName(
                    JoinNonEmptyStrings("_", false, fieldName, currency, p)
                )
            ).ToList();

            return azureFieldNames;
        }

        public static bool Contains(this IList<IFieldDescriptor> fields, string azureFieldName)
        {
            return fields?.Any(f => f.Name.EqualsInvariant(azureFieldName)) == true;
        }

        public static IFieldDescriptor Get(this IList<IFieldDescriptor> fields, string rawName)
        {
            var azureFieldName = ToAzureFieldName(rawName);
            return fields?.FirstOrDefault(f => f.Name.EqualsInvariant(azureFieldName));
        }
    }
}
