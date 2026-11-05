using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

/// <summary>
/// Guards the legacy -> current delegation of the ICancellationToken overloads of IIndexingManager and IndexingManager.
/// Modules built against SearchModule 3.1000.0 call IndexAllDocumentsAsync / IndexChangesAsync / IndexAsync with an
/// ICancellationToken; without these overloads they fail at runtime with MissingMethodException.
/// </summary>
#pragma warning disable VC0014 // Type or member is obsolete
public class IndexingManagerContractTests
{
    private const string Current = "current";
    private const string Legacy = "legacy";

    /// <summary>
    /// Implements only the non-obsolete members - what a module written against the current contract does.
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

    /// <summary>
    /// Implements both overloads of IndexAsync and records which one a call bound to.
    /// </summary>
    private sealed class BothOverloadsIndexingManager : IIndexingManager
    {
        public string CalledOverload { get; private set; }

        public Task<IndexState> GetIndexStateAsync(string documentType) => Task.FromResult<IndexState>(null);

        public Task<IEnumerable<IndexState>> GetIndicesStateAsync(string documentType) => Task.FromResult<IEnumerable<IndexState>>(null);

        public Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
        {
            CalledOverload = Current;
            return Task.CompletedTask;
        }

        public Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
        {
            CalledOverload = Legacy;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// The real IndexingManager with its current overloads recorded instead of indexing.
    /// </summary>
    private class RecordingIndexingManager() : IndexingManager(
        new Mock<ISearchProvider>().Object,
        [],
        new Mock<IOptions<SearchOptions>>().Object,
        new Mock<ISettingsManager>().Object,
        [])
    {
        public string CalledMethod { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public override Task IndexAllDocumentsAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => Record(nameof(IndexAllDocumentsAsync), cancellationToken);

        public override Task IndexChangesAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => Record(nameof(IndexChangesAsync), cancellationToken);

        public override Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, CancellationToken cancellationToken)
            => Record(nameof(IndexAsync), cancellationToken);

        protected Task Record(string method, CancellationToken cancellationToken)
        {
            CalledMethod = method;
            ReceivedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A subclass written against 3.1000.0: it overrides the legacy overload of IndexAsync.
    /// </summary>
    private sealed class LegacyOverridingIndexingManager : RecordingIndexingManager
    {
#pragma warning disable CS0672 // Member overrides obsolete member: a 3.1000.0 module has no [Obsolete] here
        public override Task IndexAsync(IndexingOptions options, Action<IndexingProgress> progressCallback, ICancellationToken cancellationToken)
            => Record(Legacy, CancellationToken.None);
#pragma warning restore CS0672
    }

    /// <summary>
    /// An ICancellationToken that is not a CancellationTokenWrapper, e.g. a Hangfire job token.
    /// </summary>
    private sealed class CustomCancellationToken(bool isCancelled) : ICancellationToken
    {
        public void ThrowIfCancellationRequested()
        {
            if (isCancelled)
            {
                throw new OperationCanceledException();
            }
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
        var manager = new CurrentOnlyIndexingManager();
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await CallLegacy(manager, method, new CancellationTokenWrapper(cancellationTokenSource.Token));

        // Assert
        manager.CalledMethod.Should().Be(method);
        manager.ReceivedToken.Should().Be(cancellationTokenSource.Token);
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public async Task LegacyOverload_NullToken_DelegatesWithNone(string method)
    {
        // Arrange
        var manager = new CurrentOnlyIndexingManager();

        // Act
        await CallLegacy(manager, method, null);

        // Assert
        manager.CalledMethod.Should().Be(method);
        manager.ReceivedToken.Should().Be(CancellationToken.None);
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public async Task LegacyOverload_CustomToken_DelegatesWithNone(string method)
    {
        // Arrange
        var manager = new CurrentOnlyIndexingManager();

        // Act
        await CallLegacy(manager, method, new CustomCancellationToken(isCancelled: false));

        // Assert
        manager.CalledMethod.Should().Be(method);
        manager.ReceivedToken.Should().Be(CancellationToken.None);
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public async Task LegacyOverload_CancelledCustomToken_Throws(string method)
    {
        // Arrange
        var manager = new CurrentOnlyIndexingManager();

        // Act
        var act = () => CallLegacy(manager, method, new CustomCancellationToken(isCancelled: true));

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        manager.CalledMethod.Should().BeNull();
    }

    [Fact]
    public async Task DefaultToken_BindsToCurrentOverload()
    {
        // Arrange
        IIndexingManager manager = new BothOverloadsIndexingManager();

        // Act: the literal `default` is what is under test
#pragma warning disable xUnit1051
        await manager.IndexAsync(new IndexingOptions { DocumentType = "Product" }, _ => { }, default);
#pragma warning restore xUnit1051

        // Assert
        ((BothOverloadsIndexingManager)manager).CalledOverload.Should().Be(Current);
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public void LegacyOverload_KeepsBinarySignature(string method)
    {
        Type[] legacyParameters = [typeof(IndexingOptions), typeof(Action<IndexingProgress>), typeof(ICancellationToken)];

        var interfaceOverload = typeof(IIndexingManager).GetMethod(method, legacyParameters);
        var classOverload = typeof(IndexingManager).GetMethod(method, legacyParameters);

        interfaceOverload.Should().NotBeNull();
        interfaceOverload!.ReturnType.Should().Be<Task>();
        classOverload.Should().NotBeNull();
        classOverload!.ReturnType.Should().Be<Task>();
        classOverload.IsVirtual.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(LegacyMethods))]
    public async Task IndexingManager_LegacyOverload_DelegatesWithWrappedToken(string method)
    {
        // Arrange
        var manager = new RecordingIndexingManager();
        using var cancellationTokenSource = new CancellationTokenSource();
        var options = new IndexingOptions { DocumentType = "Product" };
        var token = new CancellationTokenWrapper(cancellationTokenSource.Token);

        // Act: called on the class, as a module typed against IndexingManager does
        var task = method switch
        {
            nameof(IndexingManager.IndexAllDocumentsAsync) => manager.IndexAllDocumentsAsync(options, _ => { }, token),
            nameof(IndexingManager.IndexChangesAsync) => manager.IndexChangesAsync(options, _ => { }, token),
            nameof(IndexingManager.IndexAsync) => manager.IndexAsync(options, _ => { }, token),
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
        await task;

        // Assert
        manager.CalledMethod.Should().Be(method);
        manager.ReceivedToken.Should().Be(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task IndexingManager_LegacyOverride_IsCalledThroughInterface()
    {
        // Arrange
        IIndexingManager manager = new LegacyOverridingIndexingManager();

        // Act
        await manager.IndexAsync(new IndexingOptions { DocumentType = "Product" }, _ => { }, (ICancellationToken)null);

        // Assert
        ((LegacyOverridingIndexingManager)manager).CalledMethod.Should().Be(Legacy);
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
