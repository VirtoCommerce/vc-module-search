using System;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public class Operation
    {
        public string ObjectId { get; set; }
        public DateTime Timestamp { get; set; }
        public OperationType OperationType { get; set; }
    }
}
