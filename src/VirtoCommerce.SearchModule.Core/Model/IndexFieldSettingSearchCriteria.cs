using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model;

public class IndexFieldSettingSearchCriteria : SearchCriteriaBase
{
    public string DocumentType { get; set; }
    public string FieldName { get; set; }
}
