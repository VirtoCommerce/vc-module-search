using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model;

public class IndexFieldValueSetting : Entity
{
    public string Value { get; set; }
    public IList<string> Synonyms { get; set; }
}
