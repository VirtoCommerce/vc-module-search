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
        IEnumerable<IndexDocumentConfiguration> GetIndexDocumentConfigurations(string documentType);

        /// <summary>
        /// Registers Index Document Configurator for the given document type.
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="documentSource"></param>
        /// <exception cref="InvalidOperationException">Thrown when the requested document type is already registered. </exception>
        void RegisterIndexDocumentConfiguration(string documentType, IndexDocumentSource documentSource);

        /// <summary>
        /// Registers IndexDocumentSource as Related source for every Configuration with given document type
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="documentSource"></param>
        void RegisterRelatedSource(string documentType, IndexDocumentSource documentSource);
    }
}
