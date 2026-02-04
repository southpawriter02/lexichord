// =============================================================================
// File: PreviewPaneViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for PreviewPaneViewModel (v0.5.7c).
// =============================================================================
// LOGIC: Verifies preview pane ViewModel behavior:
//   - Constructor null-parameter validation.
//   - Selection changes trigger preview loading.
//   - Visibility toggle command.
//   - License gating shows upgrade prompt.
//   - Error handling displays error message.
//   - Clear resets all state.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="PreviewPaneViewModel"/>.
/// Verifies constructor validation, state management, commands, and license gating.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7c")]
public class PreviewPaneViewModelTests : IDisposable
{
    private readonly Mock<IPreviewContentBuilder> _builderMock;
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<PreviewPaneViewModel>> _loggerMock;
    private PreviewPaneViewModel? _viewModel;

    public PreviewPaneViewModelTests()
    {
        _builderMock = new Mock<IPreviewContentBuilder>();
        _editorServiceMock = new Mock<IEditorService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<PreviewPaneViewModel>>();

        // Default: feature is enabled
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(It.IsAny<string>()))
            .Returns(true);
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    /// <summary>
    /// Creates a <see cref="PreviewPaneViewModel"/> with test mocks.
    /// </summary>
    private PreviewPaneViewModel CreateViewModel()
    {
        _viewModel = new PreviewPaneViewModel(
            _builderMock.Object,
            _editorServiceMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);
        return _viewModel;
    }

    /// <summary>
    /// Creates a test <see cref="SearchHit"/>.
    /// </summary>
    private static SearchHit CreateHit(string documentPath = "/docs/test.md")
    {
        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: documentPath,
            Title: Path.GetFileNameWithoutExtension(documentPath),
            Hash: "test-hash",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        var chunk = new TextChunk(
            "Test content",
            StartOffset: 0,
            EndOffset: 12,
            new ChunkMetadata(Index: 0));

        return new SearchHit
        {
            Document = document,
            Chunk = chunk,
            Score = 0.9f
        };
    }

    /// <summary>
    /// Creates a test <see cref="PreviewContent"/>.
    /// </summary>
    private static PreviewContent CreatePreviewContent() =>
        new(
            DocumentPath: "/docs/test.md",
            DocumentTitle: "test",
            Breadcrumb: "API › Auth",
            PrecedingContext: "Before text",
            MatchedContent: "Matched text",
            FollowingContext: "After text",
            LineNumber: 10,
            HighlightSpans: Array.Empty<HighlightSpan>());

    #region Constructor Tests

    [Fact]
    public void Constructor_NullContentBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewPaneViewModel(
            null!,
            _editorServiceMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contentBuilder");
    }

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewPaneViewModel(
            _builderMock.Object,
            null!,
            _licenseContextMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewPaneViewModel(
            _builderMock.Object,
            _editorServiceMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new PreviewPaneViewModel(
            _builderMock.Object,
            _editorServiceMock.Object,
            _licenseContextMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateViewModel();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void InitialState_IsVisibleIsTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void InitialState_ContentIsNull()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.Content.Should().BeNull();
        vm.HasContent.Should().BeFalse();
    }

    [Fact]
    public void InitialState_ShowPlaceholderIsTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.ShowPlaceholder.Should().BeTrue();
    }

    #endregion

    #region TogglePreview Command Tests

    [Fact]
    public void TogglePreviewCommand_TogglesVisibility()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.IsVisible.Should().BeTrue();

        // Act
        vm.TogglePreviewCommand.Execute(null);

        // Assert
        vm.IsVisible.Should().BeFalse();

        // Act again
        vm.TogglePreviewCommand.Execute(null);

        // Assert
        vm.IsVisible.Should().BeTrue();
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task OnSelectedHitChanged_WhenUnlicensed_ShowsUpgradePrompt()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled("RAG-PREVIEW-PANE"))
            .Returns(false);

        var vm = CreateViewModel();

        // Act
        vm.SelectedHit = CreateHit();

        // Give async operation time to complete
        await Task.Delay(50);

        // Assert
        vm.ShowUpgradePrompt.Should().BeTrue();
        vm.Content.Should().BeNull();
    }

    [Fact]
    public async Task OnSelectedHitChanged_WhenLicensed_LoadsContent()
    {
        // Arrange
        var content = CreatePreviewContent();
        _builderMock
            .Setup(b => b.BuildAsync(It.IsAny<SearchHit>(), It.IsAny<PreviewOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var vm = CreateViewModel();

        // Act
        vm.SelectedHit = CreateHit();

        // Give async operation time to complete
        await Task.Delay(100);

        // Assert
        vm.ShowUpgradePrompt.Should().BeFalse();
        vm.Content.Should().NotBeNull();
        vm.Content!.MatchedContent.Should().Be("Matched text");
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_ResetsAllState()
    {
        // Arrange
        var content = CreatePreviewContent();
        _builderMock
            .Setup(b => b.BuildAsync(It.IsAny<SearchHit>(), It.IsAny<PreviewOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var vm = CreateViewModel();
        vm.SelectedHit = CreateHit();
        await Task.Delay(100);

        vm.Content.Should().NotBeNull();

        // Act
        vm.Clear();

        // Assert
        vm.SelectedHit.Should().BeNull();
        vm.Content.Should().BeNull();
        vm.ErrorMessage.Should().BeNull();
        vm.ShowUpgradePrompt.Should().BeFalse();
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task OnSelectedHitChanged_WhenBuildFails_ShowsError()
    {
        // Arrange
        _builderMock
            .Setup(b => b.BuildAsync(It.IsAny<SearchHit>(), It.IsAny<PreviewOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var vm = CreateViewModel();

        // Act
        vm.SelectedHit = CreateHit();

        // Give async operation time to complete
        await Task.Delay(100);

        // Assert
        vm.ErrorMessage.Should().NotBeNull();
        vm.Content.Should().BeNull();
    }

    #endregion

    #region Selection Change Tests

    [Fact]
    public async Task OnSelectedHitChanged_WithNull_ClearsContent()
    {
        // Arrange
        var content = CreatePreviewContent();
        _builderMock
            .Setup(b => b.BuildAsync(It.IsAny<SearchHit>(), It.IsAny<PreviewOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var vm = CreateViewModel();
        vm.SelectedHit = CreateHit();
        await Task.Delay(100);
        vm.Content.Should().NotBeNull();

        // Act
        vm.SelectedHit = null;
        await Task.Delay(50);

        // Assert
        vm.Content.Should().BeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert
        var act = () =>
        {
            vm.Dispose();
            vm.Dispose();
            vm.Dispose();
        };

        act.Should().NotThrow(because: "Dispose should be idempotent");
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ViewModel_ImplementsIDisposable()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.Should().BeAssignableTo<IDisposable>(
            because: "PreviewPaneViewModel implements IDisposable");
    }

    #endregion
}
