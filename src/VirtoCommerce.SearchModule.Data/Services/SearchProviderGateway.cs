using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services;

public class SearchProviderGateway : ISearchProvider, ISearchProviderGateway
{
    private static readonly StringComparer _ignoreCase = StringComparer.OrdinalIgnoreCase;

    private readonly string _defaultProviderName;
    private readonly Dictionary<string, string> _providerNameByDocumentType;
    private ISearchProvider _fallbackProvider;
    private readonly ConcurrentDictionary<string, ISearchProvider> _providerByName = new(_ignoreCase);

    public SearchProviderGateway(IOptions<SearchOptions> options)
    {
        _defaultProviderName = options.Value.Provider;
        _providerNameByDocumentType = options.Value.DocumentScopes.ToDictionary(x => x.DocumentType, x => x.Provider, _ignoreCase);
    }

    public virtual Task DeleteIndexAsync(string documentType)
    {
        return GetSearchProvider(documentType).DeleteIndexAsync(documentType);
    }

    public virtual Task<IndexingResult> IndexAsync(string documentType, IList<IndexDocument> documents)
    {
        return GetSearchProvider(documentType).IndexAsync(documentType, documents);
    }

    public virtual Task<IndexingResult> RemoveAsync(string documentType, IList<IndexDocument> documents)
    {
        return GetSearchProvider(documentType).RemoveAsync(documentType, documents);
    }

    public virtual Task<SearchResponse> SearchAsync(string documentType, SearchRequest request)
    {
        return GetSearchProvider(documentType).SearchAsync(documentType, request);
    }

    public virtual void AddSearchProvider(ISearchProvider provider, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            _fallbackProvider = provider;
        }
        else
        {
            _providerByName[name] = provider;
        }
    }

    public virtual ISearchProvider GetSearchProvider(string documentType)
    {
        var providerName = _providerNameByDocumentType.GetValueSafe(documentType) ?? _defaultProviderName;
        return _providerByName.GetValueSafe(providerName) ?? _fallbackProvider;
    }
}
