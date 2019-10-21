using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class IndexEntry
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public EntryState EntryState { get; set; }
    }
}
