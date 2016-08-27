//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Threading.Tasks;
//using VirtoCommerce.Domain.Catalog.Model;
//using VirtoCommerce.Domain.Catalog.Services;
//using VirtoCommerce.Domain.Pricing.Model;
//using VirtoCommerce.Platform.Core.ChangeLog;
//using VirtoCommerce.Platform.Core.Common;
//using VirtoCommerce.SearchModule.Data.Model.Indexing;
//using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;

//namespace VirtoCommerce.SearchModule.Data.Services
//{
//    public class CategoryIndexBuilder : ISearchIndexBuilder
//    {
//        private const int _partitionSizeCount = 100; // the maximum partition size, keep it smaller to prevent too big of the sql requests and too large messages in the queue

//        private readonly Model.ISearchProvider _searchProvider;
//        private readonly ICatalogSearchService _catalogSearchService;
//        private readonly IChangeLogService _changeLogService;
//        private readonly ICategoryService _categoryService;

//        public CategoryIndexBuilder(Func<ICatalogRepository> catalogRepositoryFactory, Model.ISearchProvider searchProvider, ICatalogSearchService catalogSearchService,
//                                       ICategoryService categoryService, IChangeLogService changeLogService)
//        {
//            _searchProvider = searchProvider;
//            _categoryService = categoryService;
//            _catalogSearchService = catalogSearchService;
//            _changeLogService = changeLogService;
//        }

//        #region ISearchIndexBuilder Members

//        public string DocumentType
//        {
//            get
//            {
//                return "category";
//            }
//        }

//        public IEnumerable<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
//        {
//            var partitions = (rebuild || startDate == DateTime.MinValue)
//                ? GetPartitionsForAllProducts()
//                : GetPartitionsForModifiedProducts(startDate, endDate);

//            return partitions;
//        }

//        public IEnumerable<IDocument> CreateDocuments(Partition partition)
//        {
//            if (partition == null)
//                throw new ArgumentNullException("partition");

//            var documents = new ConcurrentBag<IDocument>();

//            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

//            Parallel.ForEach(partition.Keys, parallelOptions, key =>
//            {
//                //Trace.TraceInformation(string.Format("Processing documents starting {0} of {1} - {2}%", partition.Start, partition.Total, (partition.Start * 100 / partition.Total)));
//                if (key != null)
//                {
//                    var doc = new ResultDocument();
//                    IndexItem(doc, key);
//                    documents.Add(doc);
//                }
//            });

//            return documents;
//        }

//        public void PublishDocuments(string scope, IDocument[] documents)
//        {
//            foreach (var doc in documents)
//            {
//                _searchProvider.Index(scope, DocumentType, doc);
//            }

//            _searchProvider.Commit(scope);
//            _searchProvider.Close(scope, DocumentType);
//        }

//        public void RemoveDocuments(string scope, string[] documents)
//        {
//            foreach (var doc in documents)
//            {
//                _searchProvider.Remove(scope, DocumentType, "__key", doc);
//            }
//            _searchProvider.Commit(scope);
//        }

//        public void RemoveAll(string scope)
//        {
//            _searchProvider.RemoveAll(scope, DocumentType);
//        }

//        #endregion

//        protected virtual void IndexItem(ResultDocument doc, string productId)
//        {
//            var item = _itemService.GetById(productId, ItemResponseGroup.ItemProperties | ItemResponseGroup.Variations | ItemResponseGroup.Outlines);
//            if (item == null)
//                return;

//            var indexStoreNotAnalyzed = new[] { IndexStore.Yes, IndexType.NotAnalyzed };
//            var indexStoreNotAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection };
//            var indexStoreAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection };

//            doc.Add(new DocumentField("__key", item.Id.ToLower(), indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("__type", item.GetType().Name, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("__sort", item.Name, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("__hidden", (item.IsActive != true || item.MainProductId != null).ToString().ToLower(), indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("code", item.Code, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("name", item.Name, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("startdate", item.StartDate, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("enddate", item.EndDate.HasValue ? item.EndDate : DateTime.MaxValue, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("createddate", item.CreatedDate, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("lastmodifieddate", item.ModifiedDate ?? DateTime.MaxValue, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("priority", item.Priority, indexStoreNotAnalyzed));
//            doc.Add(new DocumentField("vendor", item.Vendor, indexStoreNotAnalyzed));

//            // Add priority in virtual categories to search index
//            foreach (var link in item.Links)
//            {
//                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "priority_{0}_{1}", link.CatalogId, link.CategoryId), link.Priority, indexStoreNotAnalyzed));
//            }

//            // Add catalogs to search index
//            var catalogs = item.Outlines
//                .Select(o => o.Items.First().Id)
//                .Distinct(StringComparer.OrdinalIgnoreCase)
//                .ToArray();

//            foreach (var catalogId in catalogs)
//            {
//                doc.Add(new DocumentField("catalog", catalogId.ToLower(), indexStoreNotAnalyzedStringCollection));
//            }

//            // Add outlines to search index
//            var outlineStrings = GetOutlineStrings(item.Outlines);
//            foreach (var outline in outlineStrings)
//            {
//                doc.Add(new DocumentField("__outline", outline.ToLower(), indexStoreNotAnalyzedStringCollection));
//            }

//            // Index custom properties
//            IndexItemCustomProperties(doc, item);

//            if (item.Variations != null)
//            {
//                if (item.Variations.Any(c => c.ProductType == "Physical"))
//                {
//                    doc.Add(new DocumentField("producttype", "Physical", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
//                }

//                if (item.Variations.Any(c => c.ProductType == "Digital"))
//                {
//                    doc.Add(new DocumentField("producttype", "Digital", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
//                }

//                foreach (var variation in item.Variations)
//                {
//                    IndexItemCustomProperties(doc, variation);
//                }
//            }

//            // Index item prices
//            IndexItemPrices(doc, item);

//            //Index item reviews
//            //IndexReviews(doc, item);

//            // add to content
//            doc.Add(new DocumentField("__content", item.Name, indexStoreAnalyzedStringCollection));
//            doc.Add(new DocumentField("__content", item.Code, indexStoreAnalyzedStringCollection));
//        }

//        protected virtual string[] GetOutlineStrings(IEnumerable<Outline> outlines)
//        {
//            return outlines
//                .SelectMany(ExpandOutline)
//                .Distinct(StringComparer.OrdinalIgnoreCase)
//                .ToArray();
//        }

//        protected virtual IEnumerable<string> ExpandOutline(Outline outline)
//        {
//            // Outline structure: catalog/category1/.../categoryN/product

//            var items = outline.Items
//                .Take(outline.Items.Count - 1) // Exclude last item, which is product ID
//                .Select(i => i.Id)
//                .ToList();

//            var catalogId = items.First();

//            var result = new List<string>
//            {
//                catalogId,
//                string.Join("/", items)
//            };

//            // For each child category create a separate outline: catalog/child_category
//            if (items.Count > 2)
//            {
//                result.AddRange(
//                    items.Skip(1)
//                    .Select(i => string.Join("/", catalogId, i)));
//            }

//            return result;
//        }

//        protected virtual void IndexItemCustomProperties(ResultDocument doc, CatalogProduct item)
//        {
//            var properties = item.Properties;

//            foreach (var propValue in item.PropertyValues.Where(x => x.Value != null))
//            {
//                var propertyName = propValue.PropertyName.ToLower();
//                var property = properties.FirstOrDefault(x => string.Equals(x.Name, propValue.PropertyName, StringComparison.InvariantCultureIgnoreCase) && x.ValueType == propValue.ValueType);
//                var contentField = string.Concat("__content", property != null && property.Multilanguage && !string.IsNullOrWhiteSpace(propValue.LanguageCode) ? "_" + propValue.LanguageCode.ToLower() : string.Empty);

//                switch (propValue.ValueType)
//                {
//                    case PropertyValueType.LongText:
//                    case PropertyValueType.ShortText:
//                        var stringValue = propValue.Value.ToString();

//                        if (!string.IsNullOrWhiteSpace(stringValue)) // don't index empty values
//                        {
//                            doc.Add(new DocumentField(contentField, stringValue.ToLower(), new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection }));
//                        }

//                        break;
//                }

//                switch (propValue.ValueType)
//                {
//                    case PropertyValueType.Boolean:
//                    case PropertyValueType.DateTime:
//                    case PropertyValueType.Number:
//                        doc.Add(new DocumentField(propertyName, propValue.Value, new[] { IndexStore.Yes, IndexType.Analyzed }));
//                        break;
//                    case PropertyValueType.LongText:
//                        doc.Add(new DocumentField(propertyName, propValue.Value.ToString().ToLowerInvariant(), new[] { IndexStore.Yes, IndexType.Analyzed }));
//                        break;
//                    case PropertyValueType.ShortText: // do not tokenize small values as they will be used for lookups and filters
//                        doc.Add(new DocumentField(propertyName, propValue.Value.ToString(), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
//                        break;
//                }
//            }
//        }
        
//        private IEnumerable<Partition> GetPartitionsForAllCategories()
//        {
//            var partitions = new List<Partition>();

//            var criteria = new SearchCriteria
//            {
//                ResponseGroup = SearchResponseGroup.WithCategories,
//                Take = 0
//            };

//            var result = _catalogSearchService.Search(criteria);

//            for (var start = 0; start < result.ProductsTotalCount; start += _partitionSizeCount)
//            {
//                criteria.Skip = start;
//                criteria.Take = _partitionSizeCount;

//                // TODO: Need optimize search to return only product ids
//                result = _catalogSearchService.Search(criteria);

//                var productIds = result.Products.Select(p => p.Id).ToArray();
//                partitions.Add(new Partition(OperationType.Index, productIds));
//            }

//            return partitions;
//        }

//        private IEnumerable<Partition> GetPartitionsForModifiedCategories(DateTime startDate, DateTime endDate)
//        {
//            var partitions = new List<Partition>();

//            var categoryChanges = GetCategoryChanges(startDate, endDate);
//            var deletedCategoryIds = categoryChanges.Where(c => c.OperationType == EntryState.Deleted).Select(c => c.ObjectId).ToList();
//            var modifiedCategoryIds = categoryChanges.Where(c => c.OperationType != EntryState.Deleted).Select(c => c.ObjectId).ToList();

//            partitions.AddRange(CreatePartitions(OperationType.Remove, deletedCategoryIds));
//            partitions.AddRange(CreatePartitions(OperationType.Index, modifiedCategoryIds));

//            return partitions;
//        }

//        private List<OperationLog> GetCategoryChanges(DateTime startDate, DateTime endDate)
//        {
//            var allCategoryChanges = _changeLogService.FindChangeHistory("Category", startDate, endDate).ToList();

//            // Return latest operation type for each product
//            var result = allCategoryChanges
//                .GroupBy(c => c.ObjectId)
//                .Select(g => new OperationLog { ObjectId = g.Key, OperationType = g.OrderByDescending(c => c.ModifiedDate).Select(c => c.OperationType).First() })
//                .ToList();

//            return result;
//        }

//        private static IEnumerable<Partition> CreatePartitions(OperationType operationType, List<string> allCategoriesIds)
//        {
//            var partitions = new List<Partition>();

//            var totalCount = allCategoriesIds.Count;

//            for (var start = 0; start < totalCount; start += _partitionSizeCount)
//            {
//                var categoryIds = allCategoriesIds.Skip(start).Take(_partitionSizeCount).ToArray();
//                partitions.Add(new Partition(operationType, categoryIds));
//            }

//            return partitions;
//        }
//    }
//}
