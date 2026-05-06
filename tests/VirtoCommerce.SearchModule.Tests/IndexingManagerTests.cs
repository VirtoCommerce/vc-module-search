using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests
{
    [Trait("Category", "Unit")]
    public class IndexingManagerTests
    {
        public const string Rebuild = "rebuild";
        public const string Update = "update";
        public const string Primary = "primary";
        public const string Secondary = "secondary";
        public const string DocumentType = "item";

        [Theory]
        [InlineData(Rebuild, 1, Primary)]
        [InlineData(Rebuild, 3, Primary)]
        [InlineData(Update, 1, Primary)]
        [InlineData(Update, 3, Primary)]
        [InlineData(Rebuild, 1, Primary, Secondary)]
        [InlineData(Rebuild, 3, Primary, Secondary)]
        [InlineData(Update, 1, Primary, Secondary)]
        [InlineData(Update, 3, Primary, Secondary)]
        public async Task CanIndexAllDocuments(string operation, int batchSize, params string[] sourceNames)
        {
            var rebuild = operation == Rebuild;

            var searchProvider = new SearchProvider();
            var documentSources = GetDocumentSources(sourceNames);
            var manager = GetIndexingManager(searchProvider, documentSources);
            var progress = new List<IndexingProgress>();
            var cancellationTokenSource = new CancellationTokenSource();

            var options = new IndexingOptions
            {
                DocumentType = DocumentType,
                DeleteExistingIndex = rebuild,
                StartDate = rebuild ? null : new DateTime(1, 1, 1),
                EndDate = rebuild ? null : new DateTime(1, 1, 9),
                BatchSize = batchSize,
            };

            await manager.IndexAsync(options, p => progress.Add(p), cancellationTokenSource.Token);

            var expectedBatchesCount = GetExpectedBatchesCount(rebuild, documentSources, batchSize);
            var expectedProgressItemsCount = (rebuild ? 2 : 0) + 1 + expectedBatchesCount + 1;

            Assert.Equal(expectedProgressItemsCount, progress.Count);

            var i = 0;

            if (rebuild)
            {
                Assert.Equal($"{DocumentType}: deleting index", progress[i++].Description);
                Assert.Equal($"{DocumentType}: creating index", progress[i++].Description);
            }

            Assert.Equal($"{DocumentType}: calculating total count", progress[i++].Description);

            for (var batch = 0; batch < expectedBatchesCount; batch++)
            {
                var progressItem = progress[i++];
                Assert.Equal($"{DocumentType}: {progressItem.ProcessedCount} of {progressItem.TotalCount} have been indexed", progressItem.Description);
            }

            Assert.Equal($"{DocumentType}: indexation finished", progress[i].Description);

            ValidateErrors(progress, "bad1");

            var expectedFieldNames = new List<string>(sourceNames) { KnownDocumentFields.IndexationDate };
            ValidateIndexedDocuments(searchProvider.IndexedDocuments.Values, expectedFieldNames, "good2", "good3");
        }

        [Theory]
        [InlineData(1, Primary)]
        [InlineData(3, Primary)]
        [InlineData(1, Primary, Secondary)]
        [InlineData(3, Primary, Secondary)]
        public async Task CanIndexSpecificDocuments(int batchSize, params string[] sourceNames)
        {
            var searchProvider = new SearchProvider();
            var documentSources = GetDocumentSources(sourceNames);
            var manager = GetIndexingManager(searchProvider, documentSources);
            var progress = new List<IndexingProgress>();
            var cancellationTokenSource = new CancellationTokenSource();

            var options = new IndexingOptions
            {
                DocumentType = DocumentType,
                DocumentIds = ["bad1", "good3", "non-existent-id"],
                BatchSize = batchSize,
            };

            await manager.IndexAsync(options, p => progress.Add(p), cancellationTokenSource.Token);

            var expectedBatchesCount = GetBatchesCount(options.DocumentIds.Count, batchSize);
            var expectedProgressItemsCount = 1 + expectedBatchesCount + 1;

            Assert.Equal(expectedProgressItemsCount, progress.Count);

            var i = 0;

            Assert.Equal($"{DocumentType}: calculating total count", progress[i++].Description);

            for (var batch = 0; batch < expectedBatchesCount; batch++)
            {
                var progressItem = progress[i++];
                Assert.Equal($"{DocumentType}: {progressItem.ProcessedCount} of {progressItem.TotalCount} have been indexed", progressItem.Description);
            }

            Assert.Equal($"{DocumentType}: indexation finished", progress[i].Description);

            ValidateErrors(progress, "bad1");

            var expectedFieldNames = new List<string>(sourceNames) { KnownDocumentFields.IndexationDate };
            ValidateIndexedDocuments(searchProvider.IndexedDocuments.Values, expectedFieldNames, "good3");
        }


        private static IList<DocumentSource> GetDocumentSources(IEnumerable<string> names)
        {
            return names.Select(GetDocumentSource).ToArray();
        }

        private static DocumentSource GetDocumentSource(string name)
        {
            switch (name)
            {
                case Primary:
                    return new DocumentSource(name)
                    {
                        DocumentIds =
                        [
                            "bad1",
                            "good2",
                            "good3",
                        ],
                        Changes =
                        [
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 1), DocumentId = "bad1", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 2), DocumentId = "good1", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 3), DocumentId = "good1", ChangeType = IndexDocumentChangeType.Deleted },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 4), DocumentId = "good2", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 5), DocumentId = "good3", ChangeType = IndexDocumentChangeType.Modified },
                        ],
                    };
                case Secondary:
                    return new DocumentSource(name)
                    {
                        Changes =
                        [
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 2), DocumentId = "bad1", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 3), DocumentId = "good1", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 4), DocumentId = "good1", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 5), DocumentId = "good2", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 6), DocumentId = "good2", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 7), DocumentId = "good3", ChangeType = IndexDocumentChangeType.Modified },
                            new IndexDocumentChange { ChangeDate = new DateTime(1, 1, 8), DocumentId = "good3", ChangeType = IndexDocumentChangeType.Modified },
                        ],
                    };
            }

            return null;
        }

        private static int GetExpectedBatchesCount(bool rebuild, IEnumerable<DocumentSource> documentSources, int batchSize)
        {
            int result;

            if (rebuild)
            {
                // Use documents count from primary source
                result = GetBatchesCount(documentSources?.FirstOrDefault()?.DocumentIds.Count ?? 0, batchSize);
            }
            else
            {
                // Calculate batches count for each source and return the maximum value
                result = documentSources?.Max(s => GetBatchesCount(s?.Changes.Count ?? 0, batchSize)) ?? 0;
            }

            return result;
        }

        private static int GetBatchesCount(int itemsCount, int batchSize)
        {
            return (int)Math.Ceiling((decimal)itemsCount / batchSize);
        }

        private static void ValidateErrors(IEnumerable<IndexingProgress> progress, params string[] expectedErrorDocumentIds)
        {
            var errors = progress
                .Where(p => p.Errors != null)
                .SelectMany(p => p.Errors)
                .ToList();

            Assert.Equal(expectedErrorDocumentIds.Length, errors.Count);

            foreach (var documentId in expectedErrorDocumentIds)
            {
                Assert.Equal($"ID: {documentId}, Error: Search provider error", errors[0]);
            }
        }


        private static void ValidateIndexedDocuments(ICollection<IndexDocument> documents, ICollection<string> expectedFieldNames, params string[] expectedDocumentIds)
        {
            Assert.Equal(expectedDocumentIds.Length, documents.Count);

            foreach (var document in documents)
            {
                Assert.NotNull(document);
                Assert.Contains(document.Id, expectedDocumentIds);
                Assert.NotNull(document.Fields);
                Assert.Equal(expectedFieldNames.Count, document.Fields.Count);

                foreach (var fieldName in expectedFieldNames)
                {
                    var field = document.Fields.FirstOrDefault(f => f.Name == fieldName);

                    Assert.NotNull(field);

                    if (!fieldName.EqualsIgnoreCase(KnownDocumentFields.IndexationDate))
                    {
                        Assert.Equal(document.Id, field.Value);
                    }
                }
            }
        }


        private static IIndexingManager GetIndexingManager(
            ISearchProvider searchProvider,
            IList<DocumentSource> documentSources,
            int? indexPartitionSize = null)
        {
            var primaryDocumentSource = documentSources?.FirstOrDefault();

            var configuration = new IndexDocumentConfiguration
            {
                DocumentType = DocumentType,
                DocumentSource = CreateIndexDocumentSource(primaryDocumentSource),
                RelatedSources = documentSources?.Skip(1).Select(CreateIndexDocumentSource).ToArray(),
            };

            var settingsManager = new Mock<ISettingsManager>();
            settingsManager
                .Setup(x => x.GetObjectSettingAsync(ModuleConstants.Settings.General.EnablePartialDocumentUpdate.Name, null, null))
                .ReturnsAsync(new ObjectSettingEntry
                {
                    Value = true
                });

            if (indexPartitionSize.HasValue)
            {
                // Pin VirtoCommerce.Search.IndexPartitionSize so tests that need a specific
                // sub-chunk size in BuildDocumentsInChunksAsync can override it without depending
                // on the production default.
                settingsManager
                    .Setup(x => x.GetObjectSettingAsync(ModuleConstants.Settings.General.IndexPartitionSize.Name, null, null))
                    .ReturnsAsync(new ObjectSettingEntry
                    {
                        Value = indexPartitionSize.Value
                    });
            }

            return new IndexingManager(searchProvider, [configuration], new Mock<IOptions<SearchOptions>>().Object, settingsManager.Object, Array.Empty<IIndexDocumentConverter>());
        }

        private static IndexDocumentSource CreateIndexDocumentSource(DocumentSource documentSource)
        {
            return new IndexDocumentSource
            {
                ChangesProvider = documentSource,
                DocumentBuilder = documentSource,
            };
        }


        // ----------------------------------------------------------------------
        // Cancellation behavior tests for the Hangfire-deletion fix.
        // These guard:
        //  - that cancellation is honored before any builder is called,
        //  - that sub-chunk pagination inside GetDocumentsAsync polls the token between chunks
        //    so a single large batch does not run to completion after a token is cancelled,
        //  - that the cancellation-aware IndexDocumentsAsync overload throws when given a
        //    pre-cancelled token.
        // ----------------------------------------------------------------------

        [Fact]
        public async Task IndexAsync_WhenTokenAlreadyCancelled_ThrowsImmediately()
        {
            var documentSources = GetDocumentSources([Primary]);
            var manager = GetIndexingManager(new SearchProvider(), documentSources);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var options = new IndexingOptions
            {
                DocumentType = DocumentType,
                BatchSize = 50,
            };

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                manager.IndexAsync(options, _ => { }, cts.Token));
        }

        /// <summary>
        /// Regression guard for the sub-chunking loop in <c>IndexingManager.BuildDocumentsInChunksAsync</c>:
        /// the cancellation token must be polled between sub-chunks so a token cancelled mid-batch
        /// aborts before the remaining sub-chunks run.
        /// </summary>
        /// <remarks>
        /// In production, <c>BuildDocumentsInChunksAsync</c> reads its chunk size from the
        /// <c>VirtoCommerce.Search.IndexPartitionSize</c> setting — the same setting that drives
        /// the change-feed batch size. With both equal under default settings, a batch produces
        /// exactly one sub-chunk and the inter-chunk cancellation path never executes. To make the
        /// test exercise that path, this fixture pins <c>IndexPartitionSize</c> to <c>10</c> via
        /// the mock <c>ISettingsManager</c> while keeping <c>IndexingOptions.BatchSize = 50</c> —
        /// forcing a 50-document batch to be split into five 10-document sub-chunks.
        /// </remarks>
        [Fact]
        public async Task IndexAsync_TokenCancelledDuringFirstChunk_StopsBeforeRemainingChunks()
        {
            const int totalDocuments = 50;
            const int subChunkSize = 10;
            const int expectedTotalChunks = totalDocuments / subChunkSize; // 5

            var primary = new CountingDocumentSource(Primary)
            {
                DocumentIds = Enumerable.Range(0, totalDocuments).Select(i => "id" + i).ToArray(),
            };

            using var cts = new CancellationTokenSource();
            primary.OnGetDocuments = callNumber =>
            {
                if (callNumber == 1)
                {
                    cts.Cancel();
                }
            };

            var manager = GetIndexingManager(new SearchProvider(), [primary], indexPartitionSize: subChunkSize);

            var options = new IndexingOptions
            {
                DocumentType = DocumentType,
                DeleteExistingIndex = true,
                BatchSize = totalDocuments,
            };

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                manager.IndexAsync(options, _ => { }, cts.Token));

            // The token is cancelled inside the first builder call. The next iteration of
            // BuildDocumentsInChunksAsync polls the token before invoking the builder, so
            // the abort happens after exactly 1 sub-chunk and well before the full 5.
            Assert.True(primary.GetDocumentsCallCount < expectedTotalChunks,
                $"Expected sub-chunking to abort early, but builder ran {primary.GetDocumentsCallCount}/{expectedTotalChunks} sub-chunks.");
            Assert.Equal(1, primary.GetDocumentsCallCount);
        }

        [Fact]
        public async Task IndexDocumentsAsync_TokenAware_PreCancelledTokenThrows()
        {
            var documentSources = GetDocumentSources([Primary]);
            var manager = GetIndexingManager(new SearchProvider(), documentSources);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                manager.IndexDocumentsAsync(DocumentType, ["good2", "good3"], builderTypes: null,
                    cts.Token));
        }


        private sealed class CountingDocumentSource : DocumentSource
        {
            private int _getDocumentsCallCount;

            public CountingDocumentSource(string name) : base(name) { }

            public int GetDocumentsCallCount => _getDocumentsCallCount;
            public Action<int> OnGetDocuments { get; set; }

            public override Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds, CancellationToken cancellationToken)
            {
                var n = Interlocked.Increment(ref _getDocumentsCallCount);
                OnGetDocuments?.Invoke(n);
                return base.GetDocumentsAsync(documentIds, cancellationToken);
            }
        }
    }
}
