using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

/// <summary>
/// Guards the legacy -> current delegation of <see cref="IIndexingManager"/>'s <see cref="ICancellationToken"/> overloads.
/// Modules built against SearchModule.Core 3.1000.0 call IndexAllDocumentsAsync / IndexChangesAsync / IndexAsync with an
/// <see cref="ICancellationToken"/>; without these overloads they fail at runtime with MissingMethodException.
/// </summary>
#pragma warning disable VC0014 // Type or member is obsolete
public class IndexingManagerContractTests
{
    /// <summary>
    /// Implements only the non-obsolete members - what the current IndexingManager does.
    /// </summary>
    private sealed class CurrentOnlyIndexingManager : IIndexingManager
    {
        public string CalledMethod { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IndexState> GetIndexStateAsync(string documentType) => Task.FromResult<IndexState>(null);

        public Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType) => Task.FromResult<IEnumerable<IndexState>>(null);

        public Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => Record(nameof(IndexAllDocumentsAsync), cancellationToken);

        public Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => Record(nameof(IndexChangesAsync), cancellationToken);

        public Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => Record(nameof(IndexAsync), cancellationToken);

        private Task Record(string method, CancellationToken cancellationToken)
        {
            CalledMethod = method;
            ReceivedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    public static TheoryData<string> LegacyMethods =>
    [
        nameof(IIndexingManager.IndexAllDocumentsAsync),
        nameof(IIndexingManager.IndexChangesAsync),
        nameof(IIndexingManager.IndexAsync),
    ];

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public async Task LegacyOverload_DelegatesWithWrappedToken(string method)
    {
        // Arrange
        IIndexingManager manager = new CurrentOnlyIndexingManager();
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await CallLegacy(manager, method, new CancellationTokenWrapper(cancellationTokenSource.Token));

        // Assert
        var target = (CurrentOnlyIndexingManager)manager;
        target.CalledMethod.Should().Be(method);
        target.ReceivedToken.Should().Be(cancellationTokenSource.Token);
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public async Task LegacyOverload_NullToken_DelegatesWithNone(string method)
    {
        // Arrange
        IIndexingManager manager = new CurrentOnlyIndexingManager();

        // Act
        await CallLegacy(manager, method, null);

        // Assert
        var target = (CurrentOnlyIndexingManager)manager;
        target.CalledMethod.Should().Be(method);
        target.ReceivedToken.Should().Be(CancellationToken.None);
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public void LegacyOverload_KeepsBinarySignature(string method)
    {
        var legacyOverload = typeof(IIndexingManager).GetMethod(method,
            [typeof(IndexingOptions), typeof(Action<IndexingProgress>), typeof(ICancellationToken)]);

        legacyOverload.Should().NotBeNull();
        legacyOverload!.ReturnType.Should().Be<Task>();
    }

    private static Task CallLegacy(IIndexingManager manager, string method, ICancellationToken cancellationToken)
    {
        var options = new IndexingOptions { DocumentType = "Product" };

        return method switch
        {
            nameof(IIndexingManager.IndexAllDocumentsAsync) => manager.IndexAllDocumentsAsync(options, _ => { }, cancellationToken),
            nameof(IIndexingManager.IndexChangesAsync) => manager.IndexChangesAsync(options, _ => { }, cancellationToken),
            nameof(IIndexingManager.IndexAsync) => manager.IndexAsync(options, _ => { }, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
    }
}
#pragma warning restore VC0014 // Type or member is obsolete
