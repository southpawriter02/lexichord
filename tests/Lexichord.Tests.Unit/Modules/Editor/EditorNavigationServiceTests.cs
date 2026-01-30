using FluentAssertions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for EditorNavigationService (v0.2.6b).
/// </summary>
[Trait("Category", "Unit")]
public class EditorNavigationServiceTests
{
    private readonly Mock<IEditorService> _editorServiceMock = new();
    private readonly Mock<ILogger<EditorNavigationService>> _loggerMock = new();

    private EditorNavigationService CreateService()
    {
        return new EditorNavigationService(_editorServiceMock.Object, _loggerMock.Object);
    }

    private Mock<IManuscriptViewModel> CreateDocumentMock(string id = "doc-1", string content = "Line 1\nLine 2\nLine 3")
    {
        var docMock = new Mock<IManuscriptViewModel>();
        docMock.Setup(x => x.DocumentId).Returns(id);
        docMock.Setup(x => x.Title).Returns("Test.md");
        docMock.Setup(x => x.Content).Returns(content);
        return docMock;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEditorServiceIsNull()
    {
        // Act
        var act = () => new EditorNavigationService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new EditorNavigationService(_editorServiceMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region NavigateToViolationAsync Tests

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenDocumentIdIsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.NavigateToViolationAsync(null!, 1, 1, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Document ID");
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenDocumentIdIsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.NavigateToViolationAsync(string.Empty, 1, 1, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Document ID");
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenLineIsLessThanOne()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.NavigateToViolationAsync("doc-1", 0, 1, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("line");
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenColumnIsLessThanOne()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.NavigateToViolationAsync("doc-1", 1, 0, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("column");
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenDocumentNotFound()
    {
        // Arrange
        var service = CreateService();
        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns((IManuscriptViewModel?)null);

        // Act
        var result = await service.NavigateToViolationAsync("doc-1", 1, 1, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenActivationFails()
    {
        // Arrange
        var service = CreateService();
        var docMock = CreateDocumentMock();

        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns(docMock.Object);
        _editorServiceMock.Setup(x => x.ActivateDocumentAsync(docMock.Object)).ReturnsAsync(false);

        // Act
        var result = await service.NavigateToViolationAsync("doc-1", 1, 1, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("activate");
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsSuccess_WhenNavigationSucceeds()
    {
        // Arrange
        var service = CreateService();
        var docMock = CreateDocumentMock();

        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns(docMock.Object);
        _editorServiceMock.Setup(x => x.ActivateDocumentAsync(docMock.Object)).ReturnsAsync(true);

        // Act
        var result = await service.NavigateToViolationAsync("doc-1", 2, 3, 5);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().Be("doc-1");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task NavigateToViolationAsync_CallsSetCaretPositionAsync()
    {
        // Arrange
        var service = CreateService();
        var docMock = CreateDocumentMock();

        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns(docMock.Object);
        _editorServiceMock.Setup(x => x.ActivateDocumentAsync(docMock.Object)).ReturnsAsync(true);

        // Act
        await service.NavigateToViolationAsync("doc-1", 2, 3, 5);

        // Assert
        docMock.Verify(x => x.SetCaretPositionAsync(2, 3), Times.Once);
    }

    [Fact]
    public async Task NavigateToViolationAsync_CallsHighlightSpanAsync_WhenLengthGreaterThanZero()
    {
        // Arrange
        var service = CreateService();
        var docMock = CreateDocumentMock("doc-1", "Line 1\nLine 2\nLine 3");

        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns(docMock.Object);
        _editorServiceMock.Setup(x => x.ActivateDocumentAsync(docMock.Object)).ReturnsAsync(true);

        // Act
        await service.NavigateToViolationAsync("doc-1", 1, 1, 4);

        // Assert
        docMock.Verify(x => x.HighlightSpanAsync(0, 4, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task NavigateToViolationAsync_SkipsHighlight_WhenLengthIsZero()
    {
        // Arrange
        var service = CreateService();
        var docMock = CreateDocumentMock();

        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns(docMock.Object);
        _editorServiceMock.Setup(x => x.ActivateDocumentAsync(docMock.Object)).ReturnsAsync(true);

        // Act
        await service.NavigateToViolationAsync("doc-1", 1, 1, 0);

        // Assert
        docMock.Verify(x => x.HighlightSpanAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task NavigateToViolationAsync_ReturnsFailure_WhenCancelled()
    {
        // Arrange
        var service = CreateService();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await service.NavigateToViolationAsync("doc-1", 1, 1, 5, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    #endregion

    #region NavigateToOffsetAsync Tests

    [Fact]
    public async Task NavigateToOffsetAsync_ReturnsFailure_WhenDocumentIdIsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.NavigateToOffsetAsync(null!, 0, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Document ID");
    }

    [Fact]
    public async Task NavigateToOffsetAsync_ReturnsFailure_WhenOffsetIsNegative()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.NavigateToOffsetAsync("doc-1", -1, 5);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("offset");
    }

    [Fact]
    public async Task NavigateToOffsetAsync_ReturnsSuccess_WhenNavigationSucceeds()
    {
        // Arrange
        var service = CreateService();
        var docMock = CreateDocumentMock("doc-1", "Line 1\nLine 2\nLine 3");

        _editorServiceMock.Setup(x => x.GetDocumentById("doc-1")).Returns(docMock.Object);
        _editorServiceMock.Setup(x => x.ActivateDocumentAsync(docMock.Object)).ReturnsAsync(true);

        // Act - offset 7 is start of "Line 2"
        var result = await service.NavigateToOffsetAsync("doc-1", 7, 4);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().Be("doc-1");
    }

    #endregion

    #region NavigationResult Tests

    [Fact]
    public void NavigationResult_Succeeded_CreatesSuccessResult()
    {
        // Act
        var result = NavigationResult.Succeeded("doc-1");

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().Be("doc-1");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void NavigationResult_Failed_CreatesFailureResult()
    {
        // Act
        var result = NavigationResult.Failed("Something went wrong");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Something went wrong");
        result.DocumentId.Should().BeNull();
    }

    #endregion
}
