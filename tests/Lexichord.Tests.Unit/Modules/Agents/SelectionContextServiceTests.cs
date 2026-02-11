// -----------------------------------------------------------------------
// <copyright file="SelectionContextServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Chat.ViewModels;
using Lexichord.Modules.Agents.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents;

/// <summary>
/// Unit tests for <see cref="SelectionContextService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates the service's behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Successful selection context setting</description></item>
///   <item><description>License enforcement (WriterPro required)</description></item>
///   <item><description>Empty selection handling</description></item>
///   <item><description>Active selection detection</description></item>
///   <item><description>Context clearing</description></item>
/// </list>
/// <para>
/// <b>Spec reference:</b> LCS-DES-v0.6.7a §9.2
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.6.7a")]
public class SelectionContextServiceTests
{
    private readonly Mock<CoPilotViewModel> _viewModelMock;
    private readonly Mock<IEditorService> _editorMock;
    private readonly Mock<ILicenseContext> _licenseMock;
    private readonly Mock<ILogger<SelectionContextService>> _loggerMock;
    private readonly SelectionContextService _sut;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    /// <remarks>
    /// Default setup: WriterPro license enabled, no active selection.
    /// </remarks>
    public SelectionContextServiceTests()
    {
        _viewModelMock = new Mock<CoPilotViewModel>();
        _editorMock = new Mock<IEditorService>();
        _licenseMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<SelectionContextService>>();

        // Default: WriterPro license available
        _licenseMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        _sut = new SelectionContextService(
            _viewModelMock.Object,
            _editorMock.Object,
            _licenseMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that a valid selection is properly sent to the ViewModel.
    /// </summary>
    [Fact]
    public async Task SendSelectionToCoPilotAsync_WithValidSelection_SetsContext()
    {
        // Arrange
        var selection = "The quick brown fox jumps over the lazy dog.";

        // Act
        await _sut.SendSelectionToCoPilotAsync(selection);

        // Assert
        _viewModelMock.Verify(v => v.SetSelectionContext(selection), Times.Once);
        _viewModelMock.Verify(v => v.FocusChatInput(), Times.Once);
    }

    /// <summary>
    /// Verifies that license enforcement throws when tier is insufficient.
    /// </summary>
    [Fact]
    public async Task SendSelectionToCoPilotAsync_WithoutLicense_ThrowsException()
    {
        // Arrange
        _licenseMock.Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Core);
        var selection = "Some text";

        // Act & Assert
        await Assert.ThrowsAsync<LicenseTierException>(
            () => _sut.SendSelectionToCoPilotAsync(selection));
    }

    /// <summary>
    /// Verifies that empty/whitespace selections throw ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendSelectionToCoPilotAsync_WithEmptySelection_ThrowsArgumentException(
        string? selection)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SendSelectionToCoPilotAsync(selection!));
    }

    /// <summary>
    /// Verifies HasActiveSelection returns true when editor has selection.
    /// </summary>
    [Fact]
    public void HasActiveSelection_WhenEditorHasSelection_ReturnsTrue()
    {
        // Arrange
        _editorMock.Setup(e => e.GetSelectedText()).Returns("Selected text");

        // Act
        var result = _sut.HasActiveSelection;

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies HasActiveSelection returns false when no selection exists.
    /// </summary>
    [Fact]
    public void HasActiveSelection_WhenNoSelection_ReturnsFalse()
    {
        // Arrange
        _editorMock.Setup(e => e.GetSelectedText()).Returns(string.Empty);

        // Act
        var result = _sut.HasActiveSelection;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that clearing context delegates to the ViewModel.
    /// </summary>
    [Fact]
    public void ClearSelectionContext_ClearsViewModelContext()
    {
        // Act
        _sut.ClearSelectionContext();

        // Assert
        _viewModelMock.Verify(v => v.ClearSelectionContext(), Times.Once);
    }
}
