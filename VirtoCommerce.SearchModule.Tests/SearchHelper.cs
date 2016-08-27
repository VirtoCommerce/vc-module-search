using System;
using System.Threading;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Model.Indexing;

namespace VirtoCommerce.SearchModule.Tests
{
    public class SearchHelper
    {
        public class Price
        {
            public Price(string priceList, decimal amount)
            {
                this.Amount = amount;
                this.PriceList = priceList;
            }

            public decimal Amount;
            public string PriceList;
        }

        public static void CreateSampleIndex(ISearchProvider provider, string scope)
        {
            provider.RemoveAll(scope, "");
            provider.Index(scope, "catalogitem", CreateDocument("12345", "sample product", "red",  new[] { new Price("price_usd_default", 123.23m) }, 2, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, "catalogitem", CreateDocument("red3", "red shirt 2", "red", new[] { new Price("price_usd_default", 200m), new Price("price_usd_sale", 99m), new Price("price_eur_sale", 300m) }, 4, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, "catalogitem", CreateDocument("sad121", "red shirt", "red", new[] { new Price("price_usd_default", 10m) }, 3, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, "catalogitem", CreateDocument("jdashf", "blue shirt", "blue", new[] { new Price("price_usd_default", 23.12m) }, 8, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }, true));
            provider.Index(scope, "catalogitem", CreateDocument("32894hjf", "black sox", "black", new[] { new Price("price_usd_default", 243.12m) }, 10, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, "catalogitem", CreateDocument("another", "black sox2", "silver", new[] { new Price("price_usd_default", 700m) }, 20, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Commit(scope);
            provider.Close(scope, "catalogitem");

            // sleep for index to be commited
            Thread.Sleep(2000);
        }

        public static ResultDocument CreateDocument(string key, string name, string color, Price[] prices, int size, string[] outlines, bool extraProperties = false)
        {
            var doc = new ResultDocument();

            doc.Add(new DocumentField("__key", key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__type", "product", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__sort", "1", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__hidden", "false", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("code", "prd12321", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("name", name, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("startdate", DateTime.UtcNow.AddDays(-1), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("enddate", DateTime.MaxValue, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            foreach (var price in prices)
            {
                doc.Add(new DocumentField(price.PriceList, price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField(string.Format("{0}_value", price.PriceList), price.Amount.ToString(), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }
            
            doc.Add(new DocumentField("color", color, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("catalog", "goods", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("size", size, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("currency", "USD", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            if(extraProperties) // adds extra properties to test mapping updates for indexer
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