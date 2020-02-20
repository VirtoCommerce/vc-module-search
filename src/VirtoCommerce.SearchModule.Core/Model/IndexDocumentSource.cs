using System;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.SearchModule.Core.Model
{
    public class IndexDocumentSourceBuilder
    {
        public IndexDocumentSource IndexDocumentSource { get; private set; }

        protected IndexDocumentSourceBuilder(IndexDocumentSource indexDocumentSource)
        {
            IndexDocumentSource = indexDocumentSource ?? throw new ArgumentNullException(nameof(indexDocumentSource));
        }

        public static IndexDocumentSourceBuilder Build(IIndexDocumentBuilder documentBuilder)
        {
            return new IndexDocumentSourceBuilder(new IndexDocumentSource(documentBuilder));
        }

        public static implicit operator IndexDocumentSource(IndexDocumentSourceBuilder builder)
        {
            return builder.IndexDocumentSource;
        }

    }

    public static class IndexDocumentSourceBuilderExtensions
    {
        public static IndexDocumentSourceBuilder WithChangesProvider(this IndexDocumentSourceBuilder builder, IIndexDocumentChangesProvider changesProvider)
        {
            builder.IndexDocumentSource.ChangesProvider = changesProvider;
            return builder;
        }
        public static IndexDocumentSourceBuilder WithChangeFeedFactory(this IndexDocumentSourceBuilder builder, IIndexDocumentChangeFeedFactory changeFeedFactory)
        {
            builder.IndexDocumentSource.ChangeFeedFactory = changeFeedFactory;
            return builder;
        }

    }

    public class IndexDocumentSource
    {
        public IndexDocumentSource(IIndexDocumentBuilder documentBuilder)
        {
            DocumentBuilder = documentBuilder;
        }

        public IIndexDocumentBuilder DocumentBuilder { get; set; }

        /// <summary>
        /// Older paged abstraction to get changes, this still works although it's quite inefficient.
        /// </summary>
        public IIndexDocumentChangesProvider ChangesProvider { get; set; }

        /// <summary>
        /// Newer statefull feed to get changes.
        /// If this one is present, the changes provider will be ignored.
        /// </summary>
        public IIndexDocumentChangeFeedFactory ChangeFeedFactory { get; set; }
    }
}
