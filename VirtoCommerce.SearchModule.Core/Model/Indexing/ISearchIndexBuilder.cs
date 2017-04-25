using System;
using System.Collections.Generic;

namespace VirtoCommerce.SearchModule.Core.Model.Indexing
{
    public interface ISearchIndexBuilder
    {
        string DocumentType { get; }
        IList<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate);
        IList<IDocument> CreateDocuments(Partition partition);
        void PublishDocuments(string scope, IDocument[] documents);
        void RemoveDocuments(string scope, string[] documents);
        void RemoveAll(string scope);
    }
}
