using System;
using System.Threading;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Test
{
    public class SearchTestsHelper
    {
        public class Price
        {
            public Price(string currency, string pricelist, decimal amount)
            {
                Currency = currency;
                Pricelist = pricelist;
                Amount = amount;
            }

            public string Currency;
            public string Pricelist;
            public decimal Amount;
        }

        public static void CreateSampleIndex(ISearchProvider provider, string scope, string documentType, bool addExtraFields = false)
        {
            provider.RemoveAll(scope, documentType);

            provider.Index(scope, documentType, CreateDocument("Item-1", "Sample Product", "Red", 2, "2017-04-28T15:24:31.180Z", false, new Price("USD", "default", 123.23m)));
            provider.Index(scope, documentType, CreateDocument("Item-2", "Red Shirt 2", "Red", 4, "2017-04-27T15:24:31.180Z", false, new Price("USD", "default", 200m), new Price("USD", "sale", 99m), new Price("EUR", "sale", 300m)));
            provider.Index(scope, documentType, CreateDocument("Item-3", "Red Shirt", "Red", 3, "2017-04-26T15:24:31.180Z", false, new Price("USD", "default", 10m)));
            provider.Index(scope, documentType, CreateDocument("Item-4", "Black Sox", "Black", 10, "2017-04-25T15:24:31.180Z", false, new Price("USD", "default", 243.12m), new Price("USD", "supersale", 89m)));
            provider.Index(scope, documentType, CreateDocument("Item-5", "Black Sox2", "Silver", 20, "2017-04-24T15:24:31.180Z", false, new Price("USD", "default", 700m)));

            if (addExtraFields)
            {
                provider.Commit(scope);
                provider.Close(scope, documentType);
                Thread.Sleep(2000);
            }

            provider.Index(scope, documentType, CreateDocument("Item-6", "Blue Shirt", "Blue", 8, "2017-04-23T15:24:31.180Z", addExtraFields, new Price("USD", "default", 23.12m)));

            provider.Commit(scope);
            provider.Close(scope, documentType);

            // sleep for index to be commited
            Thread.Sleep(2000);
        }


        private static ResultDocument CreateDocument(string key, string name, string color, int size, string date, bool addExtraFields, params Price[] prices)
        {
            var doc = new ResultDocument();

            // Fields with special meaning
            doc.Add(new DocumentField("__key", key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__content", name, new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("__content", color, new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection }));

            doc.Add(new DocumentField("Code", key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("Name", name, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("Color", color, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("Size", size, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("Date", DateTime.Parse(date), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            doc.Add(new DocumentField("Catalog", "Goods", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("Catalog", "Stuff", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));

            doc.Add(new DocumentField("Is", "Priced", new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("Is", color, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("Is", key, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));

            doc.Add(new DocumentField("StoredField", "This value should not be processed in any way, it is just stored in the index.", new[] { IndexStore.Yes, IndexType.No }));

            foreach (var price in prices)
            {
                doc.Add(new DocumentField($"Price_{price.Currency}_{price.Pricelist}", price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField($"Price_{price.Currency}", price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }

            var hasMultiplePrices = prices.Length > 1;
            doc.Add(new DocumentField("HasMultiplePrices", hasMultiplePrices, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            // Adds extra fields to test mapping updates for indexer
            if (addExtraFields)
            {
                doc.Add(new DocumentField("Name 2", name, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField("Date (2)", DateTime.UtcNow, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }

            return doc;
        }
    }
}
