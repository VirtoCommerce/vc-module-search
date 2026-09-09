using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SearchModule.Core.BackgroundJobs;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

/// <summary>
/// Guards the legacy -> current delegation direction of <see cref="IIndexingJobService"/>'s default implementations.
/// Modules built against the pre-async signatures (order, catalog, customer, inventory, pricing, ...) still reach the
/// service through the obsolete synchronous members, so those must keep working against an implementation that only
/// provides the asynchronous half of the contract.
/// </summary>
public class IndexingJobServiceContractTests
{
    /// <summary>
    /// Implements only the non-obsolete asynchronous members - what a module written against the current contract does.
    /// </summary>
    private sealed class AsyncOnlyIndexingJobService : IIndexingJobService
    {
        public IList<IndexEntry> EnqueuedEntries { get; private set; }
        public string EnqueuedPriority { get; private set; }
        public bool CancelCalled { get; private set; }
        public string EnqueuedUserName { get; private set; }

        public Task<IndexProgressPushNotification> EnqueueAsync(string currentUserName, IndexingOptions[] options, CancellationToken cancellationToken = default)
        {
            EnqueuedUserName = currentUserName;
            return Task.FromResult(new IndexProgressPushNotification(currentUserName) { Id = "notification-id" });
        }

        public Task EnqueueIndexAndDeleteDocumentsAsync(IList<IndexEntry> indexEntries, string priority = JobPriority.Normal, IList<IIndexDocumentBuilder> builders = null, CancellationToken cancellationToken = default)
        {
            EnqueuedEntries = indexEntries;
            EnqueuedPriority = priority;
            return Task.CompletedTask;
        }

        public Task CancelIndexationAsync(CancellationToken cancellationToken = default)
        {
            CancelCalled = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Implements neither half - the delegation must surface NotImplementedException rather than recurse.
    /// </summary>
    private sealed class EmptyIndexingJobService : IIndexingJobService;

#pragma warning disable VC0015 // the obsolete surface is exactly what is under test
    [Fact]
    public void EnqueueIndexAndDeleteDocuments_AsyncOnlyImplementation_ForwardsToAsyncMember()
    {
        var service = new AsyncOnlyIndexingJobService();
        var entries = new List<IndexEntry> { new() { Id = "order-1", Type = "CustomerOrder", EntryState = EntryState.Modified } };

        ((IIndexingJobService)service).EnqueueIndexAndDeleteDocuments(entries, JobPriority.High);

        service.EnqueuedEntries.Should().BeSameAs(entries);
        service.EnqueuedPriority.Should().Be(JobPriority.High);
    }

    [Fact]
    public void Enqueue_AsyncOnlyImplementation_ForwardsToAsyncMember()
    {
        var service = new AsyncOnlyIndexingJobService();

        var notification = ((IIndexingJobService)service).Enqueue("admin", []);

        notification.Id.Should().Be("notification-id");
        service.EnqueuedUserName.Should().Be("admin");
    }

    [Fact]
    public void CancelIndexation_AsyncOnlyImplementation_ForwardsToAsyncMember()
    {
        var service = new AsyncOnlyIndexingJobService();

        ((IIndexingJobService)service).CancelIndexation();

        service.CancelCalled.Should().BeTrue();
    }

    [Fact]
    public async Task StartStopRecurringJobs_NotImplemented_IsANoOp()
    {
        await ((IIndexingJobService)new EmptyIndexingJobService()).StartStopRecurringJobs();
    }

    [Fact]
    public void EnqueueIndexAndDeleteDocuments_NothingImplemented_ThrowsWithoutRecursing()
    {
        var act = () => ((IIndexingJobService)new EmptyIndexingJobService()).EnqueueIndexAndDeleteDocuments([]);

        act.Should().Throw<NotImplementedException>();
    }
#pragma warning restore VC0015
}
