using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.Extensions;

public static class IndexDocumentConfigurationExtensions
{
    public static IEnumerable<IIndexDocumentBuilder> GetDocumentBuilders(this IEnumerable<IndexDocumentConfiguration> configurations, string documentType, Type providerType)
    {
        return GetDocumentSources(configurations, documentType)
            .Where(x => x.DocumentBuilder != null && x.ChangesProvider != null && x.ChangesProvider.GetType() == providerType)
            .Select(x => x.DocumentBuilder);
    }

    public static IEnumerable<IndexDocumentSource> GetDocumentSources(this IEnumerable<IndexDocumentConfiguration> configurations, string documentType)
    {
        if (!GetConfiguration(configurations, documentType, out var configuration))
        {
            yield break;
        }

        yield return configuration.DocumentSource;

        if (configuration.RelatedSources != null)
        {
            foreach (var relatedSource in configuration.RelatedSources)
            {
                yield return relatedSource;
            }
        }
    }

    public static bool GetConfiguration(this IEnumerable<IndexDocumentConfiguration> configurations, string documentType, out IndexDocumentConfiguration configuration)
    {
        // There should be only one configuration per document type
        configuration = configurations.FirstOrDefault(x => x.DocumentType.EqualsIgnoreCase(documentType));

        if (configuration != null)
        {
            ValidateConfiguration(configuration);
        }

        return configuration != null;
    }

    public static void ValidateConfiguration(this IndexDocumentConfiguration configuration)
    {
        const string documentType = nameof(configuration.DocumentType);
        const string documentSource = nameof(configuration.DocumentSource);
        const string documentBuilder = nameof(configuration.DocumentSource.DocumentBuilder);
        const string changesProvider = nameof(configuration.DocumentSource.ChangesProvider);
        const string changeFeedFactory = nameof(configuration.DocumentSource.ChangeFeedFactory);

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrEmpty(configuration.DocumentType))
        {
            throw new ArgumentException($"{documentType} is empty", nameof(configuration));
        }

        if (configuration.DocumentSource == null)
        {
            throw new ArgumentException($"{documentSource} is null", nameof(configuration));
        }

        if (configuration.DocumentSource.DocumentBuilder == null)
        {
            throw new ArgumentException($"{documentSource}.{documentBuilder} is null", nameof(configuration));
        }

        if (configuration.DocumentSource.ChangesProvider == null && configuration.DocumentSource.ChangeFeedFactory == null)
        {
            throw new ArgumentException($"Both {documentSource}.{changesProvider} and {documentSource}.{changeFeedFactory} are null", nameof(configuration));
        }
    }
}
