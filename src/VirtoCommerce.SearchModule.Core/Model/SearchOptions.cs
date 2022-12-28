using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class SearchOptions
    {
        [Required]
        public string Provider { get; set; }
        [Required]
        public string Scope { get; set; }

        public List<DocumentScope> DocumentScopes { get; set; } = new();

        public string GetScope(string documentType) => string.IsNullOrEmpty(documentType)
            ? Scope
            : DocumentScopes?.FirstOrDefault(x => x.DocumentType.EqualsInvariant(documentType))?.Scope ?? Scope;

        public class DocumentScope
        {
            public string DocumentType { get; set; }

            public string Scope { get; set; }
        }
    }
}
