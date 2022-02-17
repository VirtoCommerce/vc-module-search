using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public static class Extensions
    {
        public static IEnumerable<IIndexDocumentBuilder> GetBuildersForProvider(this IEnumerable<IndexDocumentConfiguration> configuration, Type providerType)
        {
            var result = new List<IIndexDocumentBuilder>();

            var indexDocumentConfigurations = configuration as IndexDocumentConfiguration[] ?? configuration.ToArray();

            var mainBuilder = indexDocumentConfigurations
                .FirstOrDefault(x => x.DocumentSource.ChangesProvider.GetType() == providerType)?
                .DocumentSource.DocumentBuilder;

            var builders = indexDocumentConfigurations
                .Where(x => x.RelatedSources != null)
                .SelectMany(x => x.RelatedSources)
                .Where(x => x.ChangesProvider.GetType() == providerType)
                .Select(x => x.DocumentBuilder);

            if (mainBuilder != null)
            {
                result.Add(mainBuilder);
            }

            result.AddRange(builders);

            return result;
        }
    }
}
