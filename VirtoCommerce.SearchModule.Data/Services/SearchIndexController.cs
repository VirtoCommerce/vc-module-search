using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class SearchIndexController : ISearchIndexController
    {
        private readonly ISearchIndexBuilder[] _indexBuilders;
        private readonly ISettingsManager _settingManager;
        private readonly ISearchProvider _searchProvider;

        public SearchIndexController(ISettingsManager settingManager, ISearchProvider searchProvider, params ISearchIndexBuilder[] indexBuilders)
        {
            _settingManager = settingManager;
            _indexBuilders = indexBuilders;
            _searchProvider = searchProvider;
        }

        #region ISearchIndexController

        public void RemoveIndex(string scope, string documentType, string[] documentsIds = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            // if documentType not specified delete index so mapping is also removed
            if (documentsIds.IsNullOrEmpty())
            {
                _searchProvider.RemoveAll(scope, documentType ?? string.Empty);

                var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, documentType);
                _settingManager.SetValue(lastBuildTimeName, DateTime.MinValue);
            }
            else
            {
                var indexBuilder = _indexBuilders.FirstOrDefault(b => b.DocumentType.EqualsInvariant(documentType));

                if (indexBuilder != null)
                {
                    // remove existing index
                    indexBuilder.RemoveDocuments(scope, documentsIds);
                }
            }
        }

        public void BuildIndex(string scope, string documentType, Action<IndexProgressInfo> progressCallback, string[] documentsIds = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

      
            var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, documentType);
            var lastBuildTime = _settingManager.GetValue(lastBuildTimeName, DateTime.MinValue);
            var progressInfo = new IndexProgressInfo
            {
                Description = lastBuildTime == DateTime.MinValue ? "Initial indexation" : string.Format("Date of last indexation : {0}", lastBuildTime)
            };
            progressCallback(progressInfo);

            var nowUtc = DateTime.UtcNow;
            var validBuilders = string.IsNullOrEmpty(documentType) ? _indexBuilders : _indexBuilders.Where(b => b.DocumentType.EqualsInvariant(documentType)).ToArray();
            var partitions = new List<Partition>();

            if (!documentsIds.IsNullOrEmpty())
            {
                var partition = new Partition(OperationType.Index, documentsIds);
                partitions.Add(partition);
            }
            else
            {
                progressInfo.Description = string.Format("Evaluate data size");
                progressCallback(progressInfo);

                foreach (var indexBuilder in validBuilders)
                {
                    partitions.AddRange(indexBuilder.GetPartitions(false, lastBuildTime, nowUtc));
                }
            }
            progressInfo.TotalCount = partitions.Sum(x => x.Keys.Count());

            //Indexation
            foreach (var partition in partitions)
            {
                progressInfo.Description = string.Format("Adding data to index");
                progressInfo.ProcessedCount += partition.Keys.Count();
                progressCallback(progressInfo);

                foreach (var indexBuilder in validBuilders)
                {   
                    // create index docs
                    var docs = indexBuilder.CreateDocuments(partition);

                    // submit docs to the provider
                    var docsArray = docs.ToArray();
                    indexBuilder.PublishDocuments(scope, docsArray);
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
