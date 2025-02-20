using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model;

public class IndexFieldSetting : Entity
{
    public string DocumentType { get; set; }
    public string FieldName { get; set; }
    public IList<IndexFieldValueSetting> Values { get; set; }
}
