using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class IndexDocumentRegistrar : IIndexDocumentRegistrar
    {
        private readonly List<IndexDocumentConfiguration> _indexDocument = new List<IndexDocumentConfiguration>();
        public IEnumerable<IndexDocumentConfiguration> GetIndexDocumentConfigurations()
        {
            return _indexDocument;
        }

        public IEnumerable<IndexDocumentConfiguration> GetIndexDocumentConfigurations(string documentType)
        {
            var indexDocumentConfigurations = _indexDocument.Where(x => string.Equals(x.DocumentType, documentType, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            if (indexDocumentConfigurations.IsNullOrEmpty())
            {
                throw new InvalidOperationException($"IndexDocumentConfiguration with document type :'{documentType}' not exists ");
            }

            return indexDocumentConfigurations;
        }

        public void RegisterIndexDocumentConfiguration(string documentType, IndexDocumentSource documentSource)
        {
            var existIndexDocumentConfiguration = GetIndexDocumentConfigurations(documentType);
            if (existIndexDocumentConfiguration != null)
            {
                throw new InvalidOperationException($"IndexDocumentConfiguration with document type :'{documentType}' already exists ");
            }

            _indexDocument.Add(new IndexDocumentConfiguration {DocumentType = documentType, DocumentSource = documentSource});
        }

        public void RegisterRelatedSource(string documentType, IndexDocumentSource documentSource)
        {
            foreach (var configuration in GetIndexDocumentConfigurations(documentType))
            {
                if (configuration.RelatedSources == null)
                {
                    configuration.RelatedSources = new List<IndexDocumentSource>();
                }

                configuration.RelatedSources.Add(documentSource);
            }
        }
    }
}
