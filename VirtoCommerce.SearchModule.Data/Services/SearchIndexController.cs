using System;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Data.Model.Indexing;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class SearchIndexController : ISearchIndexController
    {
        private readonly ISearchIndexBuilder[] _indexBuilders;
        private readonly ISettingsManager _settingManager;
        private readonly Model.ISearchProvider _searchProvider;

        public SearchIndexController(ISettingsManager settingManager, Model.ISearchProvider searchProvider, params ISearchIndexBuilder[] indexBuilders)
        {
            _settingManager = settingManager;
            _indexBuilders = indexBuilders;
            _searchProvider = searchProvider;
        }

        #region ISearchIndexController

        /// <summary>
        /// Processes the staged indexes.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="documentType"></param>
        /// <param name="rebuild"></param>
        public void Process(string scope, string documentType, bool rebuild)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            var validBuilders = string.IsNullOrEmpty(documentType) ? _indexBuilders : _indexBuilders.Where(b => string.Equals(b.DocumentType, documentType, StringComparison.OrdinalIgnoreCase)).ToArray();

            var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, documentType);
            var lastBuildTime = _settingManager.GetValue(lastBuildTimeName, DateTime.MinValue);
            var nowUtc = DateTime.UtcNow;

            // if full rebuild, delete index so mapping is also removed
            if(string.IsNullOrEmpty(documentType) && rebuild)
                _searchProvider.RemoveAll(scope, String.Empty);

            foreach (var indexBuilder in validBuilders)
            {
                if (rebuild)
                {
                    indexBuilder.RemoveAll(scope);
                }

                var startDate = rebuild ? DateTime.MinValue : lastBuildTime;
                var partitions = indexBuilder.GetPartitions(rebuild, startDate, nowUtc);

                foreach (var partition in partitions)
                {
                    if (partition.OperationType == OperationType.Remove)
                    {
                        indexBuilder.RemoveDocuments(scope, partition.Keys);
                    }
                    else
                    {
                        // create index docs
                        var docs = indexBuilder.CreateDocuments(partition);

                        // submit docs to the provider
                        var docsArray = docs.ToArray();
                        indexBuilder.PublishDocuments(scope, docsArray);
                    }
                }
            }

            var lastBuildTime2 = _settingManager.GetValue(lastBuildTimeName, DateTime.MinValue);
            if (lastBuildTime2 >= lastBuildTime)
            {
                _settingManager.SetValue(lastBuildTimeName, nowUtc);
            }
        }

        #endregion
    }
}
