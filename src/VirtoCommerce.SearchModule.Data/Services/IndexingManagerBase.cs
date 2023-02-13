using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Extenstions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using GeneralSettings = VirtoCommerce.SearchModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.SearchModule.Data.Services;

public abstract class IndexingManagerBase
{
    public const int DefaultBatchSize = 50;

    private readonly ISearchProvider _searchProvider;
    private readonly IEnumerable<IndexDocumentConfiguration> _configurations;
    private readonly ISettingsManager _settingsManager;

    protected IndexingManagerBase(
        ISearchProvider searchProvider,
        IEnumerable<IndexDocumentConfiguration> configurations,
        ISettingsManager settingsManager)
    {
        _searchProvider = searchProvider;
        _configurations = configurations;
        _settingsManager = settingsManager;
    }

    protected virtual bool GetConfiguration(string documentType, out IndexDocumentConfiguration configuration)
    {
        // There should be only one configuration per document type
        configuration = _configurations.FirstOrDefault(x => x.DocumentType.EqualsInvariant(documentType));

        if (configuration != null)
        {
            ValidateConfiguration(configuration);
        }

        return configuration != null;
    }

    protected virtual void ValidateOptions(IndexingOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrEmpty(options.DocumentType))
        {
            throw new ArgumentException($"{nameof(options.DocumentType)} is empty", nameof(options));
        }

        options.BatchSize ??= _settingsManager?.GetValueByDescriptor<int>(GeneralSettings.IndexPartitionSize) ?? DefaultBatchSize;

        if (options.BatchSize < 1)
        {
            throw new ArgumentException(@$"{nameof(options.BatchSize)} {options.BatchSize} is less than 1", nameof(options));
        }
    }

    protected virtual void ValidateConfiguration(IndexDocumentConfiguration configuration)
    {
        const string documentType = nameof(configuration.DocumentType);
        const string documentSource = nameof(configuration.DocumentSource);
        const string documentBuilder = nameof(configuration.DocumentSource.DocumentBuilder);
        const string changesProvider = nameof(configuration.DocumentSource.ChangesProvider);
        const string changeFeedFactory = nameof(configuration.DocumentSource.ChangeFeedFactory);

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrEmpty(configuration.DocumentType))
        {
            throw new ArgumentException($"{documentType} is empty", nameof(configuration));
        }

        if (configuration.DocumentSource == null)
        {
            throw new ArgumentException($"{documentSource} is null", nameof(configuration));
        }

        if (configuration.DocumentSource.DocumentBuilder == null)
        {
            throw new ArgumentException($"{documentSource}.{documentBuilder} is null", nameof(configuration));
        }

        if (configuration.DocumentSource.ChangesProvider == null && configuration.DocumentSource.ChangeFeedFactory == null)
        {
            throw new ArgumentException($"Both {documentSource}.{changesProvider} and {documentSource}.{changeFeedFactory} are null", nameof(configuration));
        }
    }

    protected virtual async Task<IList<IIndexDocumentChangeFeed>> GetChangeFeeds(IndexDocumentConfiguration configuration, IndexingOptions options)
    {
        // Return in-memory change feed for specific set of document IDs.
        if (options.DocumentIds != null)
        {
            return new IIndexDocumentChangeFeed[]
            {
                new InMemoryIndexDocumentChangeFeed(options.DocumentIds.ToArray(), IndexDocumentChangeType.Modified, options.BatchSize ?? DefaultBatchSize)
            };
        }

        var factories = new List<IIndexDocumentChangeFeedFactory>
        {
            configuration.DocumentSource.ChangeFeedFactory ?? CreateChangeFeedFactory(configuration.DocumentSource.ChangesProvider)
        };

        // In case of 'full' re-index we don't want to include the related sources,
        // because that would double the indexation work.
        // E.g. All products would get indexed for the primary document source
        // and afterwards all products would get re-indexed for all the prices as well.
        if (configuration.RelatedSources != null && (options.StartDate != null || options.EndDate != null))
        {
            factories.AddRange(configuration.RelatedSources.Select(x => x.ChangeFeedFactory ?? CreateChangeFeedFactory(x.ChangesProvider)));
        }

        return await Task.WhenAll(factories.Select(x => x.CreateFeed(options.StartDate, options.EndDate, options.BatchSize ?? DefaultBatchSize)));
    }

    protected virtual IIndexDocumentChangeFeedFactory CreateChangeFeedFactory(IIndexDocumentChangesProvider provider)
    {
        return new IndexDocumentChangeFeedFactoryAdapter(provider);
    }

    protected virtual async Task<IList<IndexDocumentChange>> GetNextChangesAsync(IList<IIndexDocumentChangeFeed> feeds)
    {
        var batches = await Task.WhenAll(feeds.Select(x => x.GetNextBatch()));

        var changes = batches
            .Where(x => x != null)
            .SelectMany(x => x)
            .ToList();

        return changes;
    }

    protected virtual void AddSystemFields(IList<IndexDocument> documents)
    {
        foreach (var document in documents)
        {
            document.AddFilterableValue(KnownDocumentFields.IndexationDate, DateTime.UtcNow, IndexDocumentFieldValueType.DateTime);
        }
    }

    protected virtual async Task<IndexingResult> ProcessDocuments(
        IndexDocumentChangeType changeType,
        IList<string> documentIds,
        BatchIndexingOptions batchOptions,
        ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IndexingResult result;

        switch (changeType)
        {
            case IndexDocumentChangeType.Deleted:
                result = await DeleteDocuments(batchOptions.DocumentType, documentIds);
                break;
            case IndexDocumentChangeType.Modified or IndexDocumentChangeType.Created:
                var documents = await GetDocuments(documentIds, batchOptions.PrimaryDocumentBuilder, batchOptions.SecondaryDocumentBuilders, cancellationToken);

                result = batchOptions.Reindex && _searchProvider is ISupportIndexSwap supportIndexSwapProvider
                    ? await supportIndexSwapProvider.IndexWithBackupAsync(batchOptions.DocumentType, documents)
                    : await _searchProvider.IndexAsync(batchOptions.DocumentType, documents);

                break;
            default:
                result = new IndexingResult();
                break;
        }

        return result;
    }

    protected virtual async Task<IndexingResult> DeleteDocuments(string documentType, IList<string> documentIds)
    {
        var documents = documentIds.Select(id => new IndexDocument(id)).ToList();
        return await _searchProvider.RemoveAsync(documentType, documents);
    }

    protected virtual async Task<IList<IndexDocument>> GetDocuments(
        IList<string> documentIds,
        IIndexDocumentBuilder primaryDocumentBuilder,
        IList<IIndexDocumentBuilder> secondaryDocumentBuilders,
        ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<IndexDocument> primaryDocuments;

        if (primaryDocumentBuilder == null)
        {
            primaryDocuments = documentIds.Select(x => new IndexDocument(x)).ToList();
        }
        else
        {
            primaryDocuments = (await primaryDocumentBuilder.GetDocumentsAsync(documentIds))
                ?.Where(x => x != null)
                .ToList();
        }

        if (primaryDocuments?.Any() == true)
        {
            if (secondaryDocumentBuilders != null)
            {
                var primaryDocumentIds = primaryDocuments.Select(d => d.Id).ToArray();
                var secondaryDocuments = await GetSecondaryDocuments(secondaryDocumentBuilders, primaryDocumentIds, cancellationToken);

                MergeDocuments(primaryDocuments, secondaryDocuments);
            }

            AddSystemFields(primaryDocuments);
        }

        return primaryDocuments;
    }

    protected virtual async Task<IList<IndexDocument>> GetSecondaryDocuments(
        IList<IIndexDocumentBuilder> secondaryDocumentBuilders,
        IList<string> documentIds,
        ICancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tasks = secondaryDocumentBuilders.Select(p => p.GetDocumentsAsync(documentIds));
        var results = await Task.WhenAll(tasks);

        var result = results
            .Where(r => r != null)
            .SelectMany(r => r.Where(d => d != null))
            .ToList();

        return result;
    }

    protected virtual void MergeDocuments(IList<IndexDocument> primaryDocuments, IList<IndexDocument> secondaryDocuments)
    {
        if (primaryDocuments.IsNullOrEmpty() || secondaryDocuments.IsNullOrEmpty())
        {
            return;
        }

        var secondaryDocumentGroups = secondaryDocuments
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g, StringComparer.OrdinalIgnoreCase);

        foreach (var primaryDocument in primaryDocuments)
        {
            if (secondaryDocumentGroups.TryGetValue(primaryDocument.Id, out var secondaryDocumentGroup))
            {
                foreach (var secondaryDocument in secondaryDocumentGroup)
                {
                    primaryDocument.Merge(secondaryDocument);
                }
            }
        }
    }

    protected virtual IList<string> GetIndexingErrors(IndexingResult indexingResult)
    {
        var errors = indexingResult?.Items
            ?.Where(x => !x.Succeeded)
            .Select(x => $"ID: {x.Id}, Error: {x.ErrorMessage}")
            .ToArray();

        return errors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Swap between active and backup indices, if supported
    /// </summary>
    protected virtual async Task SwapIndices(IndexingOptions options)
    {
        if (options.DeleteExistingIndex && _searchProvider is ISupportIndexSwap swappingSupportedSearchProvider)
        {
            await swappingSupportedSearchProvider.SwapIndexAsync(options.DocumentType);
        }
    }

    protected virtual void ReportProgress(Action<IndexingProgress> progressCallback, string documentType, string message)
    {
        ReportProgress(progressCallback, documentType, message, processedCount: 0L, totalCount: null, errors: null);
    }

    protected virtual void ReportProgress(
        Action<IndexingProgress> progressCallback,
        string documentType,
        string message,
        long processedCount,
        long? totalCount,
        IList<string> errors)
    {
        if (progressCallback == null)
        {
            return;
        }

        string description;

        if (message != null)
        {
            description = $"{documentType}: {message}";
        }
        else
        {
            description = totalCount != null
                ? $"{documentType}: {processedCount} of {totalCount} have been indexed"
                : $"{documentType}: {processedCount} have been indexed";
        }

        progressCallback.Invoke(new IndexingProgress(description, documentType, totalCount, processedCount, errors));
    }
}
