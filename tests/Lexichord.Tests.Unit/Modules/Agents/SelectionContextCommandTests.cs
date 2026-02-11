// -----------------------------------------------------------------------
// <copyright file="SelectionContextCommandTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Commands;
using Lexichord.Modules.Agents.Events;
using Lexichord.Modules.Agents.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents;

/// <summary>
/// Unit tests for <see cref="SelectionContextCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates command behavior:
/// </para>
/// <list type="bullet">
///   <item><description>Execute sends selection to service and publishes event</description></item>
///   <item><description>Execute with no selection does nothing</description></item>
///   <item><description>CanExecute reflects HasActiveSelection</description></item>
/// </list>
/// <para>
/// <b>Spec reference:</b> LCS-DES-v0.6.7a §9.1
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.6.7a")]
public class SelectionContextCommandTests
{
    private readonly Mock<ISelectionContextService> _selectionServiceMock;
    private readonly Mock<IEditorService> _editorMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SelectionContextCommand>> _loggerMock;
    private readonly SelectionContextCommand _sut;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    public SelectionContextCommandTests()
    {
        _selectionServiceMock = new Mock<ISelectionContextService>();
        _editorMock = new Mock<IEditorService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SelectionContextCommand>>();

        _sut = new SelectionContextCommand(
            _selectionServiceMock.Object,
            _editorMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that Execute sends selection to service and publishes event.
    /// </summary>
    [Fact]
    public void Execute_WithSelection_SendsToService()
    {
        // Arrange
        var selection = "Selected text for Co-pilot";
        _editorMock.Setup(e => e.GetSelectedText()).Returns(selection);
        _selectionServiceMock
            .Setup(s => s.SendSelectionToCoPilotAsync(selection, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        _sut.Execute(null);

        // Assert — Allow time for async void to complete
        Thread.Sleep(100);
        _selectionServiceMock.Verify(
            s => s.SendSelectionToCoPilotAsync(selection, It.IsAny<CancellationToken>()),
            Times.Once);
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SelectionContextSetEvent>(e => e.SelectionLength == selection.Length),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that Execute does nothing when no text is selected.
    /// </summary>
    [Fact]
    public void Execute_WithNoSelection_DoesNothing()
    {
        // Arrange
        _editorMock.Setup(e => e.GetSelectedText()).Returns(string.Empty);

        // Act
        _sut.Execute(null);

        // Assert
        Thread.Sleep(100);
        _selectionServiceMock.Verify(
            s => s.SendSelectionToCoPilotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that CanExecute reflects HasActiveSelection.
    /// </summary>
    [Fact]
    public void CanExecute_ReturnsHasActiveSelection()
    {
        // Arrange
        _selectionServiceMock.Setup(s => s.HasActiveSelection).Returns(true);

        // Act
        var result = _sut.CanExecute(null);

        // Assert
        result.Should().BeTrue();
    }
}
