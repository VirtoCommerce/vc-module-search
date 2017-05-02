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
        private readonly ISettingsManager _settingsManager;
        private readonly ISearchProvider _searchProvider;
        private readonly ISearchIndexBuilder[] _indexBuilders;

        public SearchIndexController(ISettingsManager settingsManager, ISearchProvider searchProvider, ISearchIndexBuilder[] indexBuilders)
        {
            _settingsManager = settingsManager;
            _searchProvider = searchProvider;
            _indexBuilders = indexBuilders;
        }

        public virtual void RemoveIndex(string scope, string documentType, string[] documentIds = null)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));
            if (string.IsNullOrEmpty(documentType) && !documentIds.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(documentType));

            // If documentType is not specified, delete index so mapping is also removed
            if (documentIds.IsNullOrEmpty())
            {
                _searchProvider.RemoveAll(scope, documentType ?? string.Empty);
            }

            var indexBuilders = string.IsNullOrEmpty(documentType) ? _indexBuilders : _indexBuilders.Where(b => b.DocumentType.EqualsInvariant(documentType));

            foreach (var indexBuilder in indexBuilders)
            {
                if (documentIds.IsNullOrEmpty())
                {
                    indexBuilder.RemoveAll(scope);
                    var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, indexBuilder.DocumentType);
                    _settingsManager.SetValue(lastBuildTimeName, (string)null);
                }
                else
                {
                    indexBuilder.RemoveDocuments(scope, documentIds);
                }
            }
        }

        public virtual void BuildIndex(string scope, string documentType, Action<IndexProgressInfo> progressCallback, string[] documentIds = null)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));
            if (string.IsNullOrEmpty(documentType) && !documentIds.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(documentType));

            var progressInfo = new IndexProgressInfo();

            var nowUtc = DateTime.UtcNow;
            var validBuilders = string.IsNullOrEmpty(documentType) ? _indexBuilders : _indexBuilders.Where(b => b.DocumentType.EqualsInvariant(documentType)).ToArray();

            foreach (var indexBuilder in validBuilders)
            {
                try
                {
                    var partitions = new List<Partition>();
                    var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, indexBuilder.DocumentType);
                    var lastBuildTime = _settingsManager.GetValue(lastBuildTimeName, DateTime.MinValue);

                    progressInfo.Description = $"{indexBuilder.DocumentType}: index size evaluation {(lastBuildTime == DateTime.MinValue ? string.Empty : $"Since from {lastBuildTime:MM/dd/yyyy hh:mm:ss}")}";
                    progressCallback(progressInfo);

                    if (!documentIds.IsNullOrEmpty())
                    {
                        var partition = new Partition(OperationType.Index, documentIds);
                        partitions.Add(partition);
                    }
                    else
                    {
                        partitions.AddRange(indexBuilder.GetPartitions(false, lastBuildTime, nowUtc));
                    }

                    var total = partitions.Sum(x => x.Keys.Length);
                    var processedCount = 0;
                    progressInfo.TotalCount += total;

                    foreach (var partition in partitions)
                    {
                        processedCount += partition.Keys.Length;
                        progressInfo.Description = $"{indexBuilder.DocumentType} : index documents {processedCount} of {total}";
                        progressCallback(progressInfo);

                        // create index docs
                        var docs = indexBuilder.CreateDocuments(partition);

                        // submit docs to the provider
                        var docsArray = docs.ToArray();
                        indexBuilder.PublishDocuments(scope, docsArray);
                    }

                    var lastBuildTime2 = _settingsManager.GetValue(lastBuildTimeName, DateTime.MinValue);
                    if (lastBuildTime2 >= lastBuildTime)
                    {
                        _settingsManager.SetValue(lastBuildTimeName, nowUtc);
                    }

                    progressInfo.ProcessedCount += processedCount;
                }
                catch (Exception ex)
                {
                    progressInfo.Errors.Add(ex.ToString());
                    progressCallback(progressInfo);
                }
            }
        }
    }
}
