using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Domain.Search.Model;
using VirtoCommerce.Domain.Search.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SearchModule.Data.Services
{
    public class CatalogItemIndexBuilder : ISearchIndexBuilder
    {
        private const int _partitionSizeCount = 100; // the maximum partition size, keep it smaller to prevent too big of the sql requests and too large messages in the queue

        private readonly Model.ISearchProvider _searchProvider;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IPricingService _pricingService;
        private readonly IItemService _itemService;
        private readonly IChangeLogService _changeLogService;

        public CatalogItemIndexBuilder(Model.ISearchProvider searchProvider, ICatalogSearchService catalogSearchService,
                                       IItemService itemService, IPricingService pricingService,
                                       IChangeLogService changeLogService)
        {
            _searchProvider = searchProvider;
            _itemService = itemService;
            _catalogSearchService = catalogSearchService;
            _pricingService = pricingService;
            _changeLogService = changeLogService;
        }

        #region ISearchIndexBuilder Members

        public string DocumentType
        {
            get
            {
                return Model.CatalogIndexedSearchCriteria.DocType;
            }
        }

        public IEnumerable<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            var partitions = (rebuild || startDate == DateTime.MinValue)
                ? GetPartitionsForAllProducts()
                : GetPartitionsForModifiedProducts(startDate, endDate);

            return partitions;
        }

        public IEnumerable<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException("partition");

            var documents = new ConcurrentBag<IDocument>();

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

            Parallel.ForEach(partition.Keys, parallelOptions, key =>
            {
                //Trace.TraceInformation(string.Format("Processing documents starting {0} of {1} - {2}%", partition.Start, partition.Total, (partition.Start * 100 / partition.Total)));
                if (key != null)
                {
                    var doc = new ResultDocument();
                    IndexItem(doc, key);
                    documents.Add(doc);
                }
            });

            return documents;
        }

        public void PublishDocuments(string scope, IDocument[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Index(scope, DocumentType, doc);
            }

            _searchProvider.Commit(scope);
            _searchProvider.Close(scope, DocumentType);
        }

        public void RemoveDocuments(string scope, string[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Remove(scope, DocumentType, "__key", doc);
            }
            _searchProvider.Commit(scope);
        }

        public void RemoveAll(string scope)
        {
            _searchProvider.RemoveAll(scope, DocumentType);
        }

        #endregion

        protected virtual void IndexItem(ResultDocument doc, string productId)
        {
            var item = _itemService.GetById(productId, ItemResponseGroup.ItemProperties | ItemResponseGroup.Variations | ItemResponseGroup.Outlines);
            if (item == null)
                return;

            var indexStoreNotAnalyzed = new[] { IndexStore.Yes, IndexType.NotAnalyzed };
            var indexStoreNotAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection };
            var indexStoreAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection };

            doc.Add(new DocumentField("__key", item.Id.ToLower(), indexStoreNotAnalyzed));
            doc.Add(new DocumentField("__type", item.GetType().Name, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("__sort", item.Name, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("__hidden", (item.IsActive != true || item.MainProductId != null).ToString().ToLower(), indexStoreNotAnalyzed));
            doc.Add(new DocumentField("code", item.Code, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("name", item.Name, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("startdate", item.StartDate, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("enddate", item.EndDate.HasValue ? item.EndDate : DateTime.MaxValue, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("createddate", item.CreatedDate, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("lastmodifieddate", item.ModifiedDate ?? DateTime.MaxValue, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("priority", item.Priority, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("vendor", item.Vendor, indexStoreNotAnalyzed));

            // Add priority in virtual categories to search index
            foreach (var link in item.Links)
            {
                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "priority_{0}_{1}", link.CatalogId, link.CategoryId), link.Priority, indexStoreNotAnalyzed));
            }

            // Add catalogs to search index
            var catalogs = item.Outlines
                .Select(o => o.Items.First().Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var catalogId in catalogs)
            {
                doc.Add(new DocumentField("catalog", catalogId.ToLower(), indexStoreNotAnalyzedStringCollection));
            }

            // Add outlines to search index
            var outlineStrings = GetOutlineStrings(item.Outlines);
            foreach (var outline in outlineStrings)
            {
                doc.Add(new DocumentField("__outline", outline.ToLower(), indexStoreNotAnalyzedStringCollection));
            }

            // Index custom properties
            IndexItemCustomProperties(doc, item);

            if (item.Variations != null)
            {
                if (item.Variations.Any(c => c.ProductType == "Physical"))
                {
                    doc.Add(new DocumentField("producttype", "Physical", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
                }

                if (item.Variations.Any(c => c.ProductType == "Digital"))
                {
                    doc.Add(new DocumentField("producttype", "Digital", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
                }

                foreach (var variation in item.Variations)
                {
                    IndexItemCustomProperties(doc, variation);
                }
            }

            // Index item prices
            IndexItemPrices(doc, item);

            //Index item reviews
            //IndexReviews(doc, item);

            // add to content
            doc.Add(new DocumentField("__content", item.Name, indexStoreAnalyzedStringCollection));
            doc.Add(new DocumentField("__content", item.Code, indexStoreAnalyzedStringCollection));
        }

        protected virtual string[] GetOutlineStrings(IEnumerable<Outline> outlines)
        {
            return outlines
                .SelectMany(ExpandOutline)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        protected virtual IEnumerable<string> ExpandOutline(Outline outline)
        {
            // Outline structure: catalog/category1/.../categoryN/product

            var items = outline.Items
                .Take(outline.Items.Count - 1) // Exclude last item, which is product ID
                .Select(i => i.Id)
                .ToList();

            var catalogId = items.First();

            var result = new List<string>
            {
                catalogId,
                string.Join("/", items)
            };

            // For each child category create a separate outline: catalog/child_category
            if (items.Count > 2)
            {
                result.AddRange(
                    items.Skip(1)
                    .Select(i => string.Join("/", catalogId, i)));
            }

            return result;
        }

        protected virtual void IndexItemCustomProperties(ResultDocument doc, CatalogProduct item)
        {
            var properties = item.Properties;

            foreach (var propValue in item.PropertyValues.Where(x => x.Value != null))
            {
                var propertyName = propValue.PropertyName.ToLower();
                var property = properties.FirstOrDefault(x => string.Equals(x.Name, propValue.PropertyName, StringComparison.InvariantCultureIgnoreCase) && x.ValueType == propValue.ValueType);
                var contentField = string.Concat("__content", property != null && property.Multilanguage && !string.IsNullOrWhiteSpace(propValue.LanguageCode) ? "_" + propValue.LanguageCode.ToLower() : string.Empty);

                switch (propValue.ValueType)
                {
                    case PropertyValueType.LongText:
                    case PropertyValueType.ShortText:
                        var stringValue = propValue.Value.ToString();

                        if (!string.IsNullOrWhiteSpace(stringValue)) // don't index empty values
                        {
                            doc.Add(new DocumentField(contentField, stringValue.ToLower(), new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection }));
                        }

                        break;
                }

                switch (propValue.ValueType)
                {
                    case PropertyValueType.Boolean:
                    case PropertyValueType.DateTime:
                    case PropertyValueType.Number:
                        doc.Add(new DocumentField(propertyName, propValue.Value, new[] { IndexStore.Yes, IndexType.Analyzed }));
                        break;
                    case PropertyValueType.LongText:
                        doc.Add(new DocumentField(propertyName, propValue.Value.ToString().ToLowerInvariant(), new[] { IndexStore.Yes, IndexType.Analyzed }));
                        break;
                    case PropertyValueType.ShortText: // do not tokenize small values as they will be used for lookups and filters
                        doc.Add(new DocumentField(propertyName, propValue.Value.ToString(), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                        break;
                }
            }
        }


        #region Price Lists Indexing

        protected virtual void IndexItemPrices(ResultDocument doc, CatalogProduct item)
        {
            /*
            var priceLists = _pricingService.GetPriceLists();
            if (_prices != null)
            {
                var prices = (from p in _prices where p.ItemId.Equals(item.Id, StringComparison.OrdinalIgnoreCase) select p).ToArray();

                foreach (var price in prices)
                {
                    //var priceList = price.Pricelist;
                    var priceList = (from p in _priceLists where p.PricelistId == price.PricelistId select p).SingleOrDefault();
                    doc.Add(new DocumentField(string.Format("price_{0}_{1}", priceList.Currency, priceList.PricelistId), price.Sale ?? price.List, new[] { IndexStore.NO, IndexType.NOT_ANALYZED }));
                    doc.Add(new DocumentField(string.Format("price_{0}_{1}_value", priceList.Currency, priceList.PricelistId), price.Sale == null ? price.List.ToString() : price.Sale.ToString(), new[] { IndexStore.YES, IndexType.NOT_ANALYZED }));
                }
            }
            */

            var evalContext = new PriceEvaluationContext
            {
                ProductIds = new[] { item.Id }
            };

            var prices = _pricingService.EvaluateProductPrices(evalContext);

            foreach (var price in prices)
            {
                //var priceList = price.Pricelist;
                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "price_{0}_{1}", price.Currency, price.PricelistId).ToLower(), price.EffectiveValue, new[] { IndexStore.No, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "price_{0}_{1}_value", price.Currency, price.PricelistId).ToLower(), (price.EffectiveValue).ToString(CultureInfo.InvariantCulture), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }
        }

        #endregion

        //protected virtual void IndexReviews(ResultDocument doc, CatalogProduct item)
        //{
        //	var reviews = ReviewRepository.Reviews.Where(r => r.ItemId == item.ItemId).ToArray();
        //	var count = reviews.Count();
        //	var avg = count > 0 ? Math.Round(reviews.Average(r => r.OverallRating), 2) : 0;
        //	doc.Add(new DocumentField("__reviewstotal", count, new[] { IndexStore.YES, IndexType.NOT_ANALYZED }));
        //	doc.Add(new DocumentField("__reviewsavg", avg, new[] { IndexStore.YES, IndexType.NOT_ANALYZED }));
        //}


        private IEnumerable<Partition> GetPartitionsForAllProducts()
        {
            var partitions = new List<Partition>();

            var criteria = new SearchCriteria
            {
                ResponseGroup = SearchResponseGroup.WithProducts,
                Take = 0
            };

            var result = _catalogSearchService.Search(criteria);

            for (var start = 0; start < result.ProductsTotalCount; start += _partitionSizeCount)
            {
                criteria.Skip = start;
                criteria.Take = _partitionSizeCount;

                // TODO: Need optimize search to return only product ids
                result = _catalogSearchService.Search(criteria);

                var productIds = result.Products.Select(p => p.Id).ToArray();
                partitions.Add(new Partition(OperationType.Index, productIds));
            }

            return partitions;
        }

        private IEnumerable<Partition> GetPartitionsForModifiedProducts(DateTime startDate, DateTime endDate)
        {
            var partitions = new List<Partition>();

            var productChanges = GetProductChanges(startDate, endDate);
            var deletedProductIds = productChanges.Where(c => c.OperationType == EntryState.Deleted).Select(c => c.ObjectId).ToList();
            var modifiedProductIds = productChanges.Where(c => c.OperationType != EntryState.Deleted).Select(c => c.ObjectId).ToList();

            partitions.AddRange(CreatePartitions(OperationType.Remove, deletedProductIds));
            partitions.AddRange(CreatePartitions(OperationType.Index, modifiedProductIds));

            return partitions;
        }

        private List<OperationLog> GetProductChanges(DateTime startDate, DateTime endDate)
        {
            var allProductChanges = _changeLogService.FindChangeHistory("Item", startDate, endDate).ToList();
            var allPriceChanges = _changeLogService.FindChangeHistory("Price", startDate, endDate).ToList();

            var priceIds = allPriceChanges.Select(c => c.ObjectId).ToArray();
            var prices = GetPrices(priceIds);

            // TODO: How to get product for deleted price?
            var productsWithChangedPrice = allPriceChanges
                .Select(c => new { c.ModifiedDate, Price = prices.ContainsKey(c.ObjectId) ? prices[c.ObjectId] : null })
                .Where(x => x.Price != null)
                .Select(x => new OperationLog { ObjectId = x.Price.ProductId, ModifiedDate = x.ModifiedDate, OperationType = EntryState.Modified })
                .ToList();

            allProductChanges.AddRange(productsWithChangedPrice);

            // Return latest operation type for each product
            var result = allProductChanges
                .GroupBy(c => c.ObjectId)
                .Select(g => new OperationLog { ObjectId = g.Key, OperationType = g.OrderByDescending(c => c.ModifiedDate).Select(c => c.OperationType).First() })
                .ToList();

            return result;
        }

        private IDictionary<string, Price> GetPrices(ICollection<string> priceIds)
        {
            // TODO: Get pageSize and degreeOfParallelism from settings
            return GetPricesWithPagingAndParallelism(priceIds, 1000, 10);
        }

        private IDictionary<string, Price> GetPricesWithPagingAndParallelism(ICollection<string> priceIds, int pageSize, int degreeOfParallelism)
        {
            IDictionary<string, Price> result;

            if (degreeOfParallelism > 1)
            {
                var dictionary = new ConcurrentDictionary<string, Price>();

                var pages = new List<string[]>();
                priceIds.ProcessWithPaging(pageSize, (ids, skipCount, totalCount) => pages.Add(ids.ToArray()));

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism };

                Parallel.ForEach(pages, parallelOptions, ids =>
                {
                    var prices = _pricingService.GetPricesById(ids);
                    foreach (var price in prices)
                    {
                        dictionary.AddOrUpdate(price.Id, price, (key, oldValue) => price);
                    }
                });

                result = dictionary;
            }
            else
            {
                var dictionary = new Dictionary<string, Price>();

                priceIds.ProcessWithPaging(pageSize, (ids, skipCount, totalCount) =>
                {
                    foreach (var price in _pricingService.GetPricesById(ids.ToArray()))
                    {
                        dictionary[price.Id] = price;
                    }
                });

                result = dictionary;
            }

            return result;
        }

        private static IEnumerable<Partition> CreatePartitions(OperationType operationType, List<string> allProductIds)
        {
            var partitions = new List<Partition>();

            var totalCount = allProductIds.Count;

            for (var start = 0; start < totalCount; start += _partitionSizeCount)
            {
                var productIds = allProductIds.Skip(start).Take(_partitionSizeCount).ToArray();
                partitions.Add(new Partition(operationType, productIds));
            }

            return partitions;
        }
    }
}
