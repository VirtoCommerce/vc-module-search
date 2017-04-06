using System;
using System.Threading;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Test
{
    public class SearchHelper
    {
        public class Price
        {
            public Price(string priceList, decimal amount)
            {
                Amount = amount;
                PriceList = priceList;
            }

            public decimal Amount;
            public string PriceList;
        }

        public static void CreateSampleIndex(ISearchProvider provider, string scope, string documentType, bool addExtraFields = false)
        {
            provider.RemoveAll(scope, documentType);

            provider.Index(scope, documentType, CreateDocument("12345", "Sample Product", "Red", 2, new[] { new Price("price_usd_default", 123.23m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument("red3", "Red Shirt 2", "Red", 4, new[] { new Price("price_usd_default", 200m), new Price("price_usd_sale", 99m), new Price("price_eur_sale", 300m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument("sad121", "Red Shirt", "Red", 3, new[] { new Price("price_usd_default", 10m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument("32894hjf", "Black Sox", "Black", 10, new[] { new Price("price_usd_default", 243.12m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument("another", "Black Sox2", "Silver", 20, new[] { new Price("price_usd_default", 700m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));

            if (addExtraFields)
            {
                provider.Commit(scope);
                provider.Close(scope, documentType);
                Thread.Sleep(2000);
            }

            provider.Index(scope, documentType, CreateDocument("jdashf", "Blue Shirt", "Blue", 8, new[] { new Price("price_usd_default", 23.12m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }, addExtraFields));

            provider.Commit(scope);
            provider.Close(scope, documentType);

            // sleep for index to be commited
            Thread.Sleep(2000);
        }

        public static ResultDocument CreateDocument(string key, string name, string color, int size, Price[] prices, string[] outlines, bool addExtraFields = false)
        {
            var doc = new ResultDocument();

            doc.Add(new DocumentField("__key", key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__type", "product", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__sort", "1", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("status", "visible", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("is", "visible", new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("is", "priced", new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("is", color, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("is", key, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("code", key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("name", name, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("startdate", DateTime.UtcNow.AddDays(-1), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("enddate", DateTime.MaxValue, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            foreach (var price in prices)
            {
                doc.Add(new DocumentField(price.PriceList, price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField("price_usd", price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }

            doc.Add(new DocumentField("color", color, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("catalog", "goods", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("size", size, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("currency", "USD", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            if (addExtraFields) // adds extra properties to test mapping updates for indexer
            {
                doc.Add(new DocumentField("name2", name, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField("startdate2", DateTime.UtcNow.AddDays(-1), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }

            if (outlines != null)
            {
                foreach (var outline in outlines)
                {
                    doc.Add(new DocumentField("__outline", outline, new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
                }
            }

            doc.Add(new DocumentField("__content", name, new[] { IndexStore.Yes, IndexType.Analyzed }));

            return doc;
        }
    }
}
