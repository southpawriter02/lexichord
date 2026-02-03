// =============================================================================
// File: ContextPreviewViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContextPreviewViewModel.
// =============================================================================
// LOGIC: Tests constructor validation, license gating, expand/collapse behavior,
//   caching, error handling, and computed property calculations.
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="ContextPreviewViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// - Constructor validation (null parameters)
/// - Initial state (license check, breadcrumb from heading)
/// - License gating (upgrade prompt for unlicensed users)
/// - Expand/collapse behavior with context fetching
/// - Caching (skip service call when cached)
/// - Error handling (set error message on service failure)
/// - Computed properties (ShowLockIcon, ExpandButtonText, etc.)
/// - RefreshContext command behavior
/// - AcknowledgeUpgradePrompt method
/// </remarks>
[Trait("Feature", "v0.5.3")]
[Trait("Category", "Unit")]
public class ContextPreviewViewModelTests
{
    private readonly IContextExpansionService _mockContextExpansionService;
    private readonly ILicenseContext _mockLicenseContext;
    private readonly Chunk _testChunk;

    public ContextPreviewViewModelTests()
    {
        _mockContextExpansionService = Substitute.For<IContextExpansionService>();
        _mockLicenseContext = Substitute.For<ILicenseContext>();

        _testChunk = new Chunk(
            Id: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            Content: "Test chunk content",
            Embedding: null,
            ChunkIndex: 5,
            StartOffset: 0,
            EndOffset: 18,
            Heading: "Test Heading",
            HeadingLevel: 2);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullChunk_ThrowsArgumentNullException()
    {
        // Arrange
        SetupLicensedUser();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModel(
                null!,
                _mockContextExpansionService,
                _mockLicenseContext,
                NullLogger<ContextPreviewViewModel>.Instance));

        Assert.Equal("chunk", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContextExpansionService_ThrowsArgumentNullException()
    {
        // Arrange
        SetupLicensedUser();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModel(
                _testChunk,
                null!,
                _mockLicenseContext,
                NullLogger<ContextPreviewViewModel>.Instance));

        Assert.Equal("contextExpansionService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModel(
                _testChunk,
                _mockContextExpansionService,
                null!,
                NullLogger<ContextPreviewViewModel>.Instance));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        SetupLicensedUser();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModel(
                _testChunk,
                _mockContextExpansionService,
                _mockLicenseContext,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithLicensedUser_SetsIsLicensedTrue()
    {
        // Arrange
        SetupLicensedUser();

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.True(sut.IsLicensed);
    }

    [Fact]
    public void Constructor_WithUnlicensedUser_SetsIsLicensedFalse()
    {
        // Arrange
        SetupUnlicensedUser();

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.IsLicensed);
    }

    [Fact]
    public void Constructor_WithChunkHeading_InitializesBreadcrumb()
    {
        // Arrange
        SetupLicensedUser();

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.Equal("Test Heading", sut.Breadcrumb);
        Assert.True(sut.HasBreadcrumb);
    }

    [Fact]
    public void Constructor_WithChunkWithoutHeading_InitializesEmptyBreadcrumb()
    {
        // Arrange
        SetupLicensedUser();
        var chunkWithoutHeading = new Chunk(
            Id: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            Content: "Content",
            Embedding: null,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 7,
            Heading: null,
            HeadingLevel: 0);

        // Act
        var sut = new ContextPreviewViewModel(
            chunkWithoutHeading,
            _mockContextExpansionService,
            _mockLicenseContext,
            NullLogger<ContextPreviewViewModel>.Instance);

        // Assert
        Assert.Equal(string.Empty, sut.Breadcrumb);
        Assert.False(sut.HasBreadcrumb);
    }

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        // Arrange
        SetupLicensedUser();

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.IsExpanded);
        Assert.False(sut.IsLoading);
        Assert.Null(sut.ExpandedChunk);
        Assert.Null(sut.ErrorMessage);
        Assert.False(sut.ShowUpgradePromptRequested);
        Assert.False(sut.HasExpandedData);
        Assert.False(sut.HasPrecedingContext);
        Assert.False(sut.HasFollowingContext);
        Assert.False(sut.HasError);
    }

    #endregion

    #region ToggleExpandedAsync Tests

    [Fact]
    public async Task ToggleExpandedAsync_WhenNotLicensed_ShowsUpgradePrompt()
    {
        // Arrange
        SetupUnlicensedUser();
        var sut = CreateViewModel();

        // Act
        await sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.ShowUpgradePromptRequested);
        Assert.False(sut.IsExpanded);
        await _mockContextExpansionService.DidNotReceive()
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleExpandedAsync_WhenLicensed_ExpandsAndFetchesContext()
    {
        // Arrange
        SetupLicensedUser();
        var expandedChunk = CreateExpandedChunk();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expandedChunk));

        var sut = CreateViewModel();

        // Act
        await sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.IsExpanded);
        Assert.NotNull(sut.ExpandedChunk);
        await _mockContextExpansionService.Received(1)
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleExpandedAsync_WhenAlreadyExpanded_CollapsesWithoutServiceCall()
    {
        // Arrange
        SetupLicensedUser();
        var expandedChunk = CreateExpandedChunk();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expandedChunk));

        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null); // First expand
        _mockContextExpansionService.ClearReceivedCalls();

        // Act
        await sut.ToggleExpandedCommand.ExecuteAsync(null); // Collapse

        // Assert
        Assert.False(sut.IsExpanded);
        await _mockContextExpansionService.DidNotReceive()
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleExpandedAsync_WhenHasCachedData_DoesNotCallService()
    {
        // Arrange
        SetupLicensedUser();
        var expandedChunk = CreateExpandedChunk();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expandedChunk));

        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null); // First expand
        await sut.ToggleExpandedCommand.ExecuteAsync(null); // Collapse
        _mockContextExpansionService.ClearReceivedCalls();

        // Act
        await sut.ToggleExpandedCommand.ExecuteAsync(null); // Re-expand

        // Assert
        Assert.True(sut.IsExpanded);
        await _mockContextExpansionService.DidNotReceive()
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleExpandedAsync_OnLoadError_SetsErrorMessage()
    {
        // Arrange
        SetupLicensedUser();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var sut = CreateViewModel();

        // Act
        await sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.IsExpanded);
        Assert.True(sut.HasError);
        Assert.NotNull(sut.ErrorMessage);
        Assert.Contains("Failed to load context", sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task ToggleExpandedAsync_UpdatesBreadcrumbFromExpandedChunk()
    {
        // Arrange
        SetupLicensedUser();
        var expandedChunk = CreateExpandedChunk(new[] { "Chapter 1", "Section 2", "Subsection A" });
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expandedChunk));

        var sut = CreateViewModel();

        // Act
        await sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Chapter 1", sut.Breadcrumb);
        Assert.Contains("Section 2", sut.Breadcrumb);
        Assert.Contains("Subsection A", sut.Breadcrumb);
    }

    [Fact]
    public async Task ToggleExpandedAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        SetupLicensedUser();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateViewModel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.ToggleExpandedCommand.ExecuteAsync(cts.Token));
    }

    #endregion

    #region RefreshContextAsync Tests

    [Fact]
    public async Task RefreshContextAsync_WhenNotLicensed_DoesNothing()
    {
        // Arrange
        SetupUnlicensedUser();
        var sut = CreateViewModel();

        // Act
        await sut.RefreshContextCommand.ExecuteAsync(null);

        // Assert
        await _mockContextExpansionService.DidNotReceive()
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshContextAsync_ClearsErrorAndFetchesFreshData()
    {
        // Arrange
        SetupLicensedUser();
        var expandedChunk = CreateExpandedChunk();

        // First call throws, second succeeds
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(
                x => throw new InvalidOperationException("First call fails"),
                x => Task.FromResult(expandedChunk));

        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null); // First expand fails
        Assert.True(sut.HasError);

        // Act
        await sut.RefreshContextCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.HasError);
        Assert.NotNull(sut.ExpandedChunk);
    }

    [Fact]
    public async Task RefreshContextAsync_ClearsCachedDataBeforeFetching()
    {
        // Arrange
        SetupLicensedUser();
        var firstExpandedChunk = CreateExpandedChunk();
        var secondExpandedChunk = CreateExpandedChunk(new[] { "New", "Breadcrumb" });

        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(firstExpandedChunk),
                Task.FromResult(secondExpandedChunk));

        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null);
        var originalBreadcrumb = sut.Breadcrumb;

        // Act
        await sut.RefreshContextCommand.ExecuteAsync(null);

        // Assert
        Assert.NotEqual(originalBreadcrumb, sut.Breadcrumb);
        Assert.Contains("New", sut.Breadcrumb);
    }

    [Fact]
    public void RefreshContextCommand_CannotExecute_WhenLoading()
    {
        // Arrange
        SetupLicensedUser();
        var tcs = new TaskCompletionSource<ExpandedChunk>();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var sut = CreateViewModel();

        // Start loading
        _ = sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.IsLoading);
        Assert.False(sut.RefreshContextCommand.CanExecute(null));

        // Complete the task
        tcs.SetResult(CreateExpandedChunk());
    }

    #endregion

    #region Computed Property Tests

    [Theory]
    [InlineData(false, "expand_more")]
    [InlineData(true, "expand_less")]
    public async Task ExpandButtonIcon_ReflectsExpandedState(bool isExpanded, string expectedIcon)
    {
        // Arrange
        SetupLicensedUser();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateExpandedChunk()));

        var sut = CreateViewModel();
        if (isExpanded)
        {
            await sut.ToggleExpandedCommand.ExecuteAsync(null);
        }

        // Assert
        Assert.Equal(expectedIcon, sut.ExpandButtonIcon);
    }

    [Theory]
    [InlineData(false, "Show More Context")]
    [InlineData(true, "Show Less")]
    public async Task ExpandButtonText_ReflectsExpandedState(bool isExpanded, string expectedText)
    {
        // Arrange
        SetupLicensedUser();
        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateExpandedChunk()));

        var sut = CreateViewModel();
        if (isExpanded)
        {
            await sut.ToggleExpandedCommand.ExecuteAsync(null);
        }

        // Assert
        Assert.Equal(expectedText, sut.ExpandButtonText);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShowLockIcon_ReflectsLicenseState(bool isLicensed, bool expectedShowLock)
    {
        // Arrange
        if (isLicensed)
        {
            SetupLicensedUser();
        }
        else
        {
            SetupUnlicensedUser();
        }

        var sut = CreateViewModel();

        // Assert
        Assert.Equal(expectedShowLock, sut.ShowLockIcon);
    }

    [Fact]
    public async Task HasPrecedingContext_ReturnsTrueWhenBeforeChunksExist()
    {
        // Arrange
        SetupLicensedUser();
        var beforeChunks = new List<Chunk>
        {
            new Chunk(Guid.NewGuid(), _testChunk.DocumentId, "Before content", null, 4, 0, 14)
        };
        var expandedChunk = new ExpandedChunk(
            Core: _testChunk,
            Before: beforeChunks.AsReadOnly(),
            After: Array.Empty<Chunk>().ToList().AsReadOnly(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expandedChunk));

        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.HasPrecedingContext);
    }

    [Fact]
    public async Task HasFollowingContext_ReturnsTrueWhenAfterChunksExist()
    {
        // Arrange
        SetupLicensedUser();
        var afterChunks = new List<Chunk>
        {
            new Chunk(Guid.NewGuid(), _testChunk.DocumentId, "After content", null, 6, 0, 13)
        };
        var expandedChunk = new ExpandedChunk(
            Core: _testChunk,
            Before: Array.Empty<Chunk>().ToList().AsReadOnly(),
            After: afterChunks.AsReadOnly(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());

        _mockContextExpansionService
            .ExpandAsync(Arg.Any<Chunk>(), Arg.Any<ContextOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expandedChunk));

        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.HasFollowingContext);
    }

    #endregion

    #region AcknowledgeUpgradePrompt Tests

    [Fact]
    public async Task AcknowledgeUpgradePrompt_ResetsFlag()
    {
        // Arrange
        SetupUnlicensedUser();
        var sut = CreateViewModel();
        await sut.ToggleExpandedCommand.ExecuteAsync(null);
        Assert.True(sut.ShowUpgradePromptRequested);

        // Act
        sut.AcknowledgeUpgradePrompt();

        // Assert
        Assert.False(sut.ShowUpgradePromptRequested);
    }

    #endregion

    #region RefreshLicenseState Tests

    [Fact]
    public void RefreshLicenseState_UpdatesIsLicensed()
    {
        // Arrange
        SetupUnlicensedUser();
        var sut = CreateViewModel();
        Assert.False(sut.IsLicensed);

        // Simulate user upgrading their license
        SetupLicensedUser();

        // Act
        sut.RefreshLicenseState();

        // Assert
        Assert.True(sut.IsLicensed);
        Assert.False(sut.ShowLockIcon);
    }

    #endregion

    #region Helper Methods

    private void SetupLicensedUser()
    {
        _mockLicenseContext.GetCurrentTier().Returns(LicenseTier.WriterPro);
        _mockLicenseContext.IsFeatureEnabled(FeatureCodes.ContextExpansion).Returns(true);
    }

    private void SetupUnlicensedUser()
    {
        _mockLicenseContext.GetCurrentTier().Returns(LicenseTier.Core);
        _mockLicenseContext.IsFeatureEnabled(FeatureCodes.ContextExpansion).Returns(false);
    }

    private ContextPreviewViewModel CreateViewModel()
    {
        return new ContextPreviewViewModel(
            _testChunk,
            _mockContextExpansionService,
            _mockLicenseContext,
            NullLogger<ContextPreviewViewModel>.Instance);
    }

    private ExpandedChunk CreateExpandedChunk(string[]? breadcrumb = null)
    {
        var beforeChunks = new List<Chunk>
        {
            new Chunk(Guid.NewGuid(), _testChunk.DocumentId, "Before 1", null, 3, 0, 8),
            new Chunk(Guid.NewGuid(), _testChunk.DocumentId, "Before 2", null, 4, 0, 8)
        };

        var afterChunks = new List<Chunk>
        {
            new Chunk(Guid.NewGuid(), _testChunk.DocumentId, "After 1", null, 6, 0, 7),
            new Chunk(Guid.NewGuid(), _testChunk.DocumentId, "After 2", null, 7, 0, 7)
        };

        return new ExpandedChunk(
            Core: _testChunk,
            Before: beforeChunks.AsReadOnly(),
            After: afterChunks.AsReadOnly(),
            ParentHeading: breadcrumb?.LastOrDefault(),
            HeadingBreadcrumb: breadcrumb ?? new[] { "Test Heading" });
    }

    #endregion
}
