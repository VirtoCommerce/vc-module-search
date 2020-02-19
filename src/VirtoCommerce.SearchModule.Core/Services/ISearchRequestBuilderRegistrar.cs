using System;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface ISearchRequestBuilderRegistrar
    {
        /// <summary>
        /// Gets registered request builder by document type (e.g. KnownDocumentTypes.Product).
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when the requested document type is not registered. </exception>
        ISearchRequestBuilder GetRequestBuilderByDocumentType(string documentType);

        /// <summary>
        /// Registers Search Request Builder factory for the given document type.
        /// </summary>
        /// <typeparam name="TSearchRequestBuilder"></typeparam>
        /// <param name="documentType">Document type for which we register Search Request Builder.</param>
        /// <param name="factory"><typeparamref name="TSearchRequestBuilder"/> creation factory.</param>
        /// <exception cref="InvalidOperationException">Thrown when Search Request Builder is already registered for the <paramref name="documentType"/>.</exception>
        void Register<TSearchRequestBuilder>(string documentType, Func<TSearchRequestBuilder> factory) where TSearchRequestBuilder : class, ISearchRequestBuilder;

        /// <summary>
        /// Overrides existing Search Request Builder registration for a document type.
        /// </summary>
        /// <typeparam name="TSearchRequestBuilder"></typeparam>
        /// <param name="documentType">Document type for which we register Search Request Builder.</param>
        /// <param name="factory"><typeparamref name="TSearchRequestBuilder"/> creation factory.</param>
        void Override<TSearchRequestBuilder>(string documentType, Func<TSearchRequestBuilder> factory) where TSearchRequestBuilder : class, ISearchRequestBuilder;
    }
}
