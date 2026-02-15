// -----------------------------------------------------------------------
// <copyright file="EditorAgentContextMenuProviderTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="EditorAgentContextMenuProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates the provider's behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Menu item availability and content</description></item>
///   <item><description>License gating (WriterPro required)</description></item>
///   <item><description>Selection state tracking</description></item>
///   <item><description>CanRewriteChanged event firing</description></item>
///   <item><description>Rewrite command execution and event publishing</description></item>
/// </list>
/// <para>
/// <strong>Spec reference:</strong> LCS-DES-v0.7.3a §5
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3a")]
public class EditorAgentContextMenuProviderTests : IDisposable
{
    private readonly Mock<IEditorService> _editorMock;
    private readonly Mock<ILicenseContext> _licenseMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<EditorAgentContextMenuProvider>> _loggerMock;
    private readonly EditorAgentContextMenuProvider _sut;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    /// <remarks>
    /// Default setup: WriterPro license enabled, no active selection.
    /// </remarks>
    public EditorAgentContextMenuProviderTests()
    {
        _editorMock = new Mock<IEditorService>();
        _licenseMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<EditorAgentContextMenuProvider>>();

        // Default: WriterPro license available
        _licenseMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        // Default: No selection
        _editorMock.Setup(e => e.HasSelection).Returns(false);

        _sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sut.Dispose();
    }

    // -----------------------------------------------------------------------
    // GetRewriteMenuItems Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetRewriteMenuItems returns exactly 4 menu items.
    /// </summary>
    [Fact]
    public void GetRewriteMenuItems_ReturnsExpectedCount()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();

        // Assert
        items.Should().HaveCount(4);
    }

    /// <summary>
    /// Verifies that the menu items have the expected intents.
    /// </summary>
    [Fact]
    public void GetRewriteMenuItems_ContainsAllIntents()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();

        // Assert
        items.Should().Contain(i => i.Intent == RewriteIntent.Formal);
        items.Should().Contain(i => i.Intent == RewriteIntent.Simplified);
        items.Should().Contain(i => i.Intent == RewriteIntent.Expanded);
        items.Should().Contain(i => i.Intent == RewriteIntent.Custom);
    }

    /// <summary>
    /// Verifies that the Formal command has correct properties.
    /// </summary>
    [Fact]
    public void GetRewriteMenuItems_FormalCommand_HasCorrectProperties()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();
        var formal = items.First(i => i.Intent == RewriteIntent.Formal);

        // Assert
        formal.CommandId.Should().Be("rewrite-formal");
        formal.DisplayName.Should().Be("Rewrite Formally");
        formal.KeyboardShortcut.Should().Be("Ctrl+Shift+R");
        formal.OpensDialog.Should().BeFalse();
        formal.Icon.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that the Custom command opens a dialog.
    /// </summary>
    [Fact]
    public void GetRewriteMenuItems_CustomCommand_OpensDialog()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();
        var custom = items.First(i => i.Intent == RewriteIntent.Custom);

        // Assert
        custom.OpensDialog.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that all commands have valid command IDs.
    /// </summary>
    [Fact]
    public void GetRewriteMenuItems_AllCommands_HaveValidCommandIds()
    {
        // Act
        var items = _sut.GetRewriteMenuItems();

        // Assert
        foreach (var item in items)
        {
            var errors = item.Validate();
            errors.Should().BeEmpty($"CommandId '{item.CommandId}' should be valid");
        }
    }

    // -----------------------------------------------------------------------
    // License Gating Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that IsLicensed returns true for WriterPro tier.
    /// </summary>
    [Fact]
    public void IsLicensed_WriterProTier_ReturnsTrue()
    {
        // Arrange is done in constructor (WriterPro)

        // Assert
        _sut.IsLicensed.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsLicensed returns false for Core tier.
    /// </summary>
    [Fact]
    public void IsLicensed_CoreTier_ReturnsFalse()
    {
        // Arrange
        _licenseMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Assert
        sut.IsLicensed.Should().BeFalse();
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that IsLicensed returns true for Teams tier.
    /// </summary>
    [Fact]
    public void IsLicensed_TeamsTier_ReturnsTrue()
    {
        // Arrange
        _licenseMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Teams);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Assert
        sut.IsLicensed.Should().BeTrue();
        sut.Dispose();
    }

    // -----------------------------------------------------------------------
    // Selection State Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that HasSelection reflects initial editor state.
    /// </summary>
    [Fact]
    public void HasSelection_InitialState_MatchesEditorState()
    {
        // Arrange (default: no selection)

        // Assert
        _sut.HasSelection.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that HasSelection returns true when editor has selection.
    /// </summary>
    [Fact]
    public void HasSelection_WithEditorSelection_ReturnsTrue()
    {
        // Arrange
        _editorMock.Setup(e => e.HasSelection).Returns(true);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Assert
        sut.HasSelection.Should().BeTrue();
        sut.Dispose();
    }

    // -----------------------------------------------------------------------
    // CanRewrite Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that CanRewrite requires both license and selection.
    /// </summary>
    [Fact]
    public void CanRewrite_RequiresBothLicenseAndSelection()
    {
        // Arrange: Has license (default), no selection

        // Assert
        _sut.CanRewrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CanRewrite returns true with both conditions met.
    /// </summary>
    [Fact]
    public void CanRewrite_WithLicenseAndSelection_ReturnsTrue()
    {
        // Arrange
        _editorMock.Setup(e => e.HasSelection).Returns(true);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Assert
        sut.CanRewrite.Should().BeTrue();
        sut.Dispose();
    }

    // -----------------------------------------------------------------------
    // CanRewriteChanged Event Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that CanRewriteChanged is raised when selection changes.
    /// </summary>
    [Fact]
    public void CanRewriteChanged_WhenSelectionChanges_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.CanRewriteChanged += (_, _) => eventRaised = true;

        // Act - simulate selection change
        _editorMock.Raise(
            e => e.SelectionChanged += null,
            new SelectionChangedEventArgs("Selected text"));

        // Assert
        eventRaised.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CanRewriteChanged is raised when license changes.
    /// </summary>
    [Fact]
    public void CanRewriteChanged_WhenLicenseDowngrades_RaisesEvent()
    {
        // Arrange - Start with license and selection
        _editorMock.Setup(e => e.HasSelection).Returns(true);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        var eventRaised = false;
        sut.CanRewriteChanged += (_, _) => eventRaised = true;

        // Act - simulate license downgrade
        _licenseMock.Raise(
            l => l.LicenseChanged += null,
            new LicenseChangedEventArgs
            {
                OldTier = LicenseTier.WriterPro,
                NewTier = LicenseTier.Core,
                LicenseInfo = new LicenseInfo(LicenseTier.Core, null, null, null, false)
            });

        // Assert
        eventRaised.Should().BeTrue();
        sut.Dispose();
    }

    // -----------------------------------------------------------------------
    // ExecuteRewriteAsync Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that ExecuteRewriteAsync publishes upgrade modal when not licensed.
    /// </summary>
    [Fact]
    public async Task ExecuteRewriteAsync_WhenNotLicensed_PublishesUpgradeModal()
    {
        // Arrange
        _licenseMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Act
        await sut.ExecuteRewriteAsync(RewriteIntent.Formal);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<ShowUpgradeModalEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteRewriteAsync publishes rewrite event for Formal intent.
    /// </summary>
    [Fact]
    public async Task ExecuteRewriteAsync_FormalIntent_PublishesRewriteEvent()
    {
        // Arrange
        _editorMock.Setup(e => e.HasSelection).Returns(true);
        _editorMock.Setup(e => e.GetSelectedText()).Returns("Some text");
        _editorMock.Setup(e => e.SelectionStart).Returns(10);
        _editorMock.Setup(e => e.SelectionLength).Returns(9);
        _editorMock.Setup(e => e.CurrentDocumentPath).Returns("/path/to/doc.md");

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Act
        await sut.ExecuteRewriteAsync(RewriteIntent.Formal);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RewriteRequestedEvent>(e =>
                    e.Intent == RewriteIntent.Formal &&
                    e.SelectedText == "Some text" &&
                    e.SelectionSpan.Start == 10 &&
                    e.SelectionSpan.Length == 9),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteRewriteAsync publishes custom dialog event for Custom intent.
    /// </summary>
    [Fact]
    public async Task ExecuteRewriteAsync_CustomIntent_PublishesCustomDialogEvent()
    {
        // Arrange
        _editorMock.Setup(e => e.HasSelection).Returns(true);
        _editorMock.Setup(e => e.GetSelectedText()).Returns("Custom text");
        _editorMock.Setup(e => e.SelectionStart).Returns(0);
        _editorMock.Setup(e => e.SelectionLength).Returns(11);

        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        // Act
        await sut.ExecuteRewriteAsync(RewriteIntent.Custom);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<ShowCustomRewriteDialogEvent>(e =>
                    e.SelectedText == "Custom text"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteRewriteAsync does not publish when no selection.
    /// </summary>
    [Fact]
    public async Task ExecuteRewriteAsync_NoSelection_DoesNotPublish()
    {
        // Arrange: default has no selection

        // Act
        await _sut.ExecuteRewriteAsync(RewriteIntent.Formal);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<RewriteRequestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Dispose Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Dispose unsubscribes from events.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var sut = new EditorAgentContextMenuProvider(
            _editorMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        var eventRaised = false;
        sut.CanRewriteChanged += (_, _) => eventRaised = true;

        // Act
        sut.Dispose();

        // Simulate selection change after dispose
        _editorMock.Raise(
            e => e.SelectionChanged += null,
            new SelectionChangedEventArgs("New selection"));

        // Assert - event should not be raised after dispose
        // (The disposed flag prevents new event processing)
        eventRaised.Should().BeFalse("CanRewriteChanged should not fire after disposal");
    }
}
