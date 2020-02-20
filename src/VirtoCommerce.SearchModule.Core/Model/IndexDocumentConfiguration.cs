using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class IndexDocumentConfigurationBuilder
    {
        public IndexDocumentConfiguration IndexDocumentConfiguration { get; private set; }

        protected IndexDocumentConfigurationBuilder(IndexDocumentConfiguration indexDocumentConfiguration)
        {
            IndexDocumentConfiguration = indexDocumentConfiguration ?? throw new ArgumentNullException(nameof(indexDocumentConfiguration));
        }

        public static IndexDocumentConfigurationBuilder Build(string documentType, IndexDocumentSource indexDocumentSource)
        {
            return new IndexDocumentConfigurationBuilder(new IndexDocumentConfiguration(documentType, indexDocumentSource));
        }
        public static IndexDocumentConfigurationBuilder Build(IndexDocumentConfiguration indexDocumentConfiguration)
        {
            return new IndexDocumentConfigurationBuilder(indexDocumentConfiguration);
        }

        public static implicit operator IndexDocumentConfiguration(IndexDocumentConfigurationBuilder builder)
        {
            return builder.IndexDocumentConfiguration;
        }
   
    }

    public static class IndexDocumentConfigurationBuilderExtensions
    {
        public static IndexDocumentConfigurationBuilder AddRelatedSources(this IndexDocumentConfigurationBuilder builder, params IndexDocumentSource[] relatedSources)
        {
            builder.IndexDocumentConfiguration.RelatedSources.AddRange(relatedSources);
            return builder;
        }
    }


    public class IndexDocumentConfiguration
    {
        public string DocumentType { get; private set; }
        public IndexDocumentSource DocumentSource { get; private set; }

        public IndexDocumentConfiguration(string documentType, IndexDocumentSource documentSource)
        {
            DocumentType = documentType;
            DocumentSource = documentSource;
            RelatedSources = new List<IndexDocumentSource>();
        }

        public IList<IndexDocumentSource> RelatedSources { get; set; }
    }
}
