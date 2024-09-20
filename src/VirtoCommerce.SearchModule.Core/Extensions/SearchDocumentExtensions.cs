using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class SearchDocumentExtensions
{
    public static double? GetRelevanceScore(this SearchDocument searchDocument)
    {
        ArgumentNullException.ThrowIfNull(searchDocument);

        return searchDocument.GetValueOrDefault(ModuleConstants.RelevanceScore) as double?;
    }

    public static void SetRelevanceScore(this SearchDocument searchDocument, double? score)
    {
        ArgumentNullException.ThrowIfNull(searchDocument);

        searchDocument[ModuleConstants.RelevanceScore] = score;
    }

    public static void SetRelevanceScore<T>(this IList<SearchDocument> documents, IList<T> items)
        where T : IEntity
    {
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(items);

        var itemsMap = items.ToDictionary(x => x.Id);
        foreach (var document in documents)
        {
            var item = itemsMap.GetValueOrDefault(document.Id);
            if (item is not IHasRelevanceScore withRelevance)
            {
                continue;
            }

            withRelevance.RelevanceScore = document.GetRelevanceScore();
        }
    }
}
