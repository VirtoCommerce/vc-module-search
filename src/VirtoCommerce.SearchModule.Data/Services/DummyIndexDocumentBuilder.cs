using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class DummyIndexDocumentBuilder : IIndexSchemaBuilder, IIndexDocumentBuilder
{
    public Task BuildSchemaAsync(IndexDocument schema)
    {
        throw new NotImplementedException();
    }

    public Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds)
    {
        throw new NotImplementedException();
    }
}
