using System;
using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchModule.Core.Services
{
    public interface IIndexDocumentRegistrar
    {
        /// <summary>
        /// Gets list of registered IndexDocumentConfiguration.
        /// </summary>
        /// <param></param>
        /// <returns>IEnumerable<IndexDocumentConfiguration></returns>
        IEnumerable<IndexDocumentConfiguration> GetIndexDocumentConfigurations();

        /// <summary>
        /// Gets list of registered IndexDocumentConfiguration filtered by document type (e.g. KnownDocumentTypes.Product).
        /// </summary>
        /// <param></param>
        /// <param name="documentType"></param>
        /// <returns>IEnumerable<IndexDocumentConfiguration></returns>
        /// <exception cref="InvalidOperationException">Thrown when IndexDocumentConfigurations with the requested document type is not registered. </exception>
        IndexDocumentConfiguration GetIndexDocumentConfiguration(string documentType);

        /// <summary>
        /// Registers Index Document Configurator for the given document type.
        /// </summary>
        /// <param name="indexDocumentConfiguration"></param>
        /// <exception cref="InvalidOperationException">Thrown when the requested document type is already registered. </exception>
        void RegisterIndexDocumentConfiguration(IndexDocumentConfiguration indexDocumentConfiguration);

    }
}
