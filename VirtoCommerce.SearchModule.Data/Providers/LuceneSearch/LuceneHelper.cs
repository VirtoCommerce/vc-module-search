using System;
using System.IO;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchModule.Data.Providers.LuceneSearch
{
    public class LuceneHelper
    {
        /// <summary>
        ///     Converts the search document to lucene document
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static Document ConvertDocument(IDocument document)
        {
            var doc = new Document();
            for (var index = 0; index < document.FieldCount; index++)
            {
                var field = document[index];
                AddFieldToDocument(doc, field);
            }

            return doc;
        }


        private static void AddFieldToDocument(Document doc, IDocumentField field)
        {
            if (field?.Value == null)
            {
                return;
            }

            var store = Field.Store.YES;
            var index = Field.Index.NOT_ANALYZED;

            if (field.ContainsAttribute(IndexStore.No))
            {
                store = Field.Store.NO;
            }

            if (field.ContainsAttribute(IndexType.Analyzed))
            {
                index = Field.Index.ANALYZED;
            }
            else if (field.ContainsAttribute(IndexType.No))
            {
                index = Field.Index.NO;
            }

            field.Name = field.Name.ToLowerInvariant();

            if (field.Name == "__key")
            {
                foreach (var value in field.Values)
                {
                    doc.Add(new Field(field.Name, value.ToString(), store, index));
                }
            }
            else if (field.Name == "__object")
            {
                if (field.Value != null)
                {
                    var value = SerializeObject(field.Value);

                    // index full web serialized object
                    doc.Add(new Field(field.Name, value, store, index));
                }
            }
            else if (field.Value is string)
            {
                foreach (var value in field.Values)
                {
                    doc.Add(new Field(field.Name, value.ToString(), store, index));
                    doc.Add(new Field("_content", value.ToString(), Field.Store.NO, Field.Index.ANALYZED));
                }
            }
            else if (field.Value is decimal) // parse prices
            {
                foreach (var value in field.Values)
                {
                    var numericField = new NumericField(field.Name, store, index != Field.Index.NO);
                    numericField.SetDoubleValue(double.Parse(value.ToString()));
                    doc.Add(numericField);
                }
            }
            else if (field.Value is DateTime) // parse dates
            {
                foreach (var value in field.Values)
                {
                    doc.Add(
                        new Field(
                            field.Name, DateTools.DateToString((DateTime)value, DateTools.Resolution.SECOND), store, index));
                }
            }
            else // try detecting the type
            {
                // TODO: instead of auto detecting, use meta data information
                decimal t;
                if (decimal.TryParse(field.Value.ToString(), out t))
                {
                    foreach (var value in field.Values)
                    {
                        var numericField = new NumericField(field.Name, store, index != Field.Index.NO);
                        numericField.SetDoubleValue(double.Parse(value.ToString()));
                        doc.Add(numericField);
                    }
                }
                else
                {
                    foreach (var value in field.Values)
                    {
                        doc.Add(new Field(field.Name, value.ToString(), store, index));
                    }
                }
            }
        }

        private static string SerializeObject(object obj)
        {
            using (var memStream = new MemoryStream())
            {
                var serializer = new JsonSerializer
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.None,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.None,
                };

                obj.SerializeJson(memStream, serializer);
                memStream.Seek(0, SeekOrigin.Begin);
                var value = memStream.ReadToString();
                return value;
            }
        }
    }
}
