using System.Text.RegularExpressions;

namespace VirtoCommerce.SearchModule.Data.Services;

public static partial class FilterRegexHelper
{
    [GeneratedRegex(@"^(?<fieldName>[A-Za-z0-9\-]+)(_[A-Za-z]{3})?$", RegexOptions.IgnoreCase, "en-US")]
    public static partial Regex FieldName();
}
