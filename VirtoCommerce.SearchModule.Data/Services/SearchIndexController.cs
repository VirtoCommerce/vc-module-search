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
            if (!documentsIds.IsNullOrEmpty() && string.IsNullOrEmpty(documentType))
            {
                throw new ArgumentNullException("documentType");
            }

            var indexBuilders = string.IsNullOrEmpty(documentType) ? _indexBuilders : _indexBuilders.Where(b => b.DocumentType.EqualsInvariant(documentType));
            // if documentType not specified delete index so mapping is also removed
            if (documentsIds.IsNullOrEmpty())
            {
                _searchProvider.RemoveAll(scope, documentType ?? string.Empty);
            }

            foreach (var indexBuilder in indexBuilders)
            {
                if(documentsIds.IsNullOrEmpty())
                {
                    indexBuilder.RemoveAll(scope);
                    var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, indexBuilder.DocumentType);
                    _settingManager.SetValue(lastBuildTimeName, (string)null);
                }
                else
                {
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
            if(!documentsIds.IsNullOrEmpty() && string.IsNullOrEmpty(documentType))
            {
                throw new ArgumentNullException("documentType");
            }

        
            var progressInfo = new IndexProgressInfo();

            //{
            //    Description = lastBuildTime == DateTime.MinValue ? "Initial indexation" : string.Format("Date of last indexation : {0}", lastBuildTime)
            //};
            progressCallback(progressInfo);

            var nowUtc = DateTime.UtcNow;
            var validBuilders = string.IsNullOrEmpty(documentType) ? _indexBuilders : _indexBuilders.Where(b => b.DocumentType.EqualsInvariant(documentType)).ToArray();
          
            foreach (var indexBuilder in validBuilders)
            {
                try
                {
                    var partitions = new List<Partition>();
                    var lastBuildTimeName = string.Format(CultureInfo.InvariantCulture, "VirtoCommerce.Search.LastBuildTime_{0}_{1}", scope, indexBuilder.DocumentType);
                    var lastBuildTime = _settingManager.GetValue(lastBuildTimeName, DateTime.MinValue);

                    progressInfo.Description = string.Format("{0}: index data size evaluation. {1}", indexBuilder.DocumentType, lastBuildTime == DateTime.MinValue ? string.Empty : string.Format("Since from {0:MM/dd/yyyy hh:mm:ss}", lastBuildTime));
                    progressCallback(progressInfo);

                    if (!documentsIds.IsNullOrEmpty())
                    {
                        var partition = new Partition(OperationType.Index, documentsIds);
                        partitions.Add(partition);
                    }
                    else
                    {
                        partitions.AddRange(indexBuilder.GetPartitions(false, lastBuildTime, nowUtc));
                    }
                    progressInfo.TotalCount = partitions.Sum(x => x.Keys.Count());
                    progressInfo.ProcessedCount = 0;
                    progressInfo.Description = string.Format("{0} : indexing process", indexBuilder.DocumentType);

                    foreach (var partition in partitions)
                    {
                        progressInfo.ProcessedCount += partition.Keys.Count();
                        progressCallback(progressInfo);
                        // create index docs
                        var docs = indexBuilder.CreateDocuments(partition);

                        // submit docs to the provider
                        var docsArray = docs.ToArray();
                        indexBuilder.PublishDocuments(scope, docsArray);
                    }

                    var lastBuildTime2 = _settingManager.GetValue(lastBuildTimeName, DateTime.MinValue);
                    if (lastBuildTime2 >= lastBuildTime)
                    {
                        _settingManager.SetValue(lastBuildTimeName, nowUtc);
                    }
                }
                catch(Exception ex)
                {
                    progressInfo.Errors.Add(ex.ToString());
                    progressCallback(progressInfo);
                }
            }
      
        }
        #endregion
    }
}
