using System;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model
{
    [Serializable]
    public class SearchDocument : Dictionary<string, object>
    {
        public SearchDocument() : base(StringComparer.OrdinalIgnoreCase) { }

        public string Id { get; set; }
    }
}
