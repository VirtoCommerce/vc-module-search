using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class IndexDocumentRegistrar : IIndexDocumentRegistrar
    {
        private readonly ConcurrentDictionary<string, IndexDocumentConfiguration> _documentConfiguration = new ConcurrentDictionary<string, IndexDocumentConfiguration>();
        public IEnumerable<IndexDocumentConfiguration> GetIndexDocumentConfigurations()
        {
            return _documentConfiguration.Values;
        }

        public IndexDocumentConfiguration GetIndexDocumentConfiguration(string documentType)
        {
            if (!_documentConfiguration.ContainsKey(documentType))
            {
                throw new InvalidOperationException($"IndexDocumentConfiguration with document type '{documentType}' does not exist.");
            }

            return _documentConfiguration[documentType];
        }

        public void RegisterIndexDocumentConfiguration(IndexDocumentConfiguration configuration)
        {

            if (_documentConfiguration.ContainsKey(configuration.DocumentType))
            {
                throw new InvalidOperationException($"IndexDocumentConfiguration with document type :'{configuration.DocumentType}' already exists ");
            }

            _documentConfiguration.TryAdd(configuration.DocumentType, configuration);
        }

    }
}
