using FluentAssertions;
using Lexichord.Abstractions.Layout;
using Lexichord.Abstractions.Messaging;
using Lexichord.Abstractions.Services;
using Lexichord.Abstractions.ViewModels;
using Lexichord.Host.Layout;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Layout;

/// <summary>
/// Unit tests for <see cref="TabService"/>.
/// </summary>
public class TabServiceTests
{
    private readonly Mock<IRegionManager> _mockRegionManager;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<TabService>> _mockLogger;
    private readonly TabService _sut;

    public TabServiceTests()
    {
        _mockRegionManager = new Mock<IRegionManager>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<TabService>>();

        _sut = new TabService(
            _mockRegionManager.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    private Mock<IDocumentTab> CreateMockDocument(string id = "doc-1", bool isDirty = false, bool isPinned = false)
    {
        var mock = new Mock<IDocumentTab>();
        mock.SetupGet(x => x.DocumentId).Returns(id);
        mock.SetupGet(x => x.Title).Returns($"Document {id}");
        mock.SetupGet(x => x.CanClose).Returns(true);
        mock.SetupGet(x => x.IsDirty).Returns(isDirty);
        mock.SetupGet(x => x.IsPinned).Returns(isPinned);
        mock.Setup(x => x.CanCloseAsync()).ReturnsAsync(!isDirty);
        return mock;
    }

    [Fact]
    public async Task CloseDocumentAsync_WhenDocumentNotRegistered_ReturnsFalse()
    {
        // Act
        var result = await _sut.CloseDocumentAsync("unknown-doc");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CloseDocumentAsync_WhenDocumentIsClean_ClosesAndReturnsTrue()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1", isDirty: false);
        _sut.RegisterDocument(mockDoc.Object);
        _mockRegionManager.Setup(x => x.CloseAsync("doc-1", false)).ReturnsAsync(true);

        // Act
        var result = await _sut.CloseDocumentAsync("doc-1");

        // Assert
        result.Should().BeTrue();
        _mockRegionManager.Verify(x => x.CloseAsync("doc-1", false), Times.Once);
        _mockMediator.Verify(x => x.Publish(
            It.Is<DocumentClosingNotification>(n => n.DocumentId == "doc-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockMediator.Verify(x => x.Publish(
            It.Is<DocumentClosedNotification>(n => n.DocumentId == "doc-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CloseDocumentAsync_WhenDocumentCanCloseReturnsFalse_abortsClose()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1");
        mockDoc.Setup(x => x.CanCloseAsync()).ReturnsAsync(false);
        _sut.RegisterDocument(mockDoc.Object);

        // Act
        var result = await _sut.CloseDocumentAsync("doc-1");

        // Assert
        result.Should().BeFalse();
        _mockRegionManager.Verify(x => x.CloseAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CloseDocumentAsync_WhenForced_BypassesCanCloseAsync()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1");
        mockDoc.Setup(x => x.CanCloseAsync()).ReturnsAsync(false);
        _sut.RegisterDocument(mockDoc.Object);
        _mockRegionManager.Setup(x => x.CloseAsync("doc-1", true)).ReturnsAsync(true);

        // Act
        var result = await _sut.CloseDocumentAsync("doc-1", force: true);

        // Assert
        result.Should().BeTrue();
        mockDoc.Verify(x => x.CanCloseAsync(), Times.Never);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-1", true), Times.Once);
    }

    [Fact]
    public async Task CloseAllDocumentsAsync_ClosesAllDocuments()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1");
        var mockDoc2 = CreateMockDocument("doc-2");
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);
        _mockRegionManager.Setup(x => x.CloseAsync(It.IsAny<string>(), false)).ReturnsAsync(true);

        // Act
        var result = await _sut.CloseAllDocumentsAsync();

        // Assert
        result.Should().BeTrue();
        _mockRegionManager.Verify(x => x.CloseAsync("doc-1", false), Times.Once);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-2", false), Times.Once);
    }

    [Fact]
    public async Task CloseAllDocumentsAsync_WhenSkipPinned_SkipsPinnedDocuments()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1", isPinned: true);
        var mockDoc2 = CreateMockDocument("doc-2", isPinned: false);
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);
        _mockRegionManager.Setup(x => x.CloseAsync("doc-2", false)).ReturnsAsync(true);

        // Act
        var result = await _sut.CloseAllDocumentsAsync(skipPinned: true);

        // Assert
        result.Should().BeTrue();
        _mockRegionManager.Verify(x => x.CloseAsync("doc-1", false), Times.Never);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-2", false), Times.Once);
    }

    [Fact]
    public async Task CloseAllButThisAsync_ClosesAllExceptSpecified()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1");
        var mockDoc2 = CreateMockDocument("doc-2");
        var mockDoc3 = CreateMockDocument("doc-3");
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);
        _sut.RegisterDocument(mockDoc3.Object);
        _mockRegionManager.Setup(x => x.CloseAsync(It.IsAny<string>(), false)).ReturnsAsync(true);

        // Act
        var result = await _sut.CloseAllButThisAsync("doc-2");

        // Assert
        result.Should().BeTrue();
        _mockRegionManager.Verify(x => x.CloseAsync("doc-1", false), Times.Once);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-2", false), Times.Never);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-3", false), Times.Once);
    }

    [Fact]
    public async Task CloseToTheRightAsync_ClosesDocumentsAfterSpecified()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1");
        var mockDoc2 = CreateMockDocument("doc-2");
        var mockDoc3 = CreateMockDocument("doc-3");
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);
        _sut.RegisterDocument(mockDoc3.Object);
        _mockRegionManager.Setup(x => x.CloseAsync(It.IsAny<string>(), false)).ReturnsAsync(true);

        // Act
        var result = await _sut.CloseToTheRightAsync("doc-1");

        // Assert
        result.Should().BeTrue();
        _mockRegionManager.Verify(x => x.CloseAsync("doc-1", false), Times.Never);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-2", false), Times.Once);
        _mockRegionManager.Verify(x => x.CloseAsync("doc-3", false), Times.Once);
    }

    [Fact]
    public async Task PinDocumentAsync_SetsIsPinnedAndPublishesNotification()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1");
        _sut.RegisterDocument(mockDoc.Object);

        // Act
        await _sut.PinDocumentAsync("doc-1", true);

        // Assert
        mockDoc.VerifySet(x => x.IsPinned = true, Times.Once);
        _mockMediator.Verify(x => x.Publish(
            It.Is<DocumentPinnedNotification>(n => n.DocumentId == "doc-1" && n.IsPinned == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetDirtyDocumentIds_ReturnsOnlyDirtyDocuments()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1", isDirty: true);
        var mockDoc2 = CreateMockDocument("doc-2", isDirty: false);
        var mockDoc3 = CreateMockDocument("doc-3", isDirty: true);
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);
        _sut.RegisterDocument(mockDoc3.Object);

        // Act
        var result = _sut.GetDirtyDocumentIds();

        // Assert
        result.Should().BeEquivalentTo(["doc-1", "doc-3"]);
    }

    [Fact]
    public void HasUnsavedChanges_WhenNoDirtyDocuments_ReturnsFalse()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1", isDirty: false);
        var mockDoc2 = CreateMockDocument("doc-2", isDirty: false);
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);

        // Act & Assert
        _sut.HasUnsavedChanges().Should().BeFalse();
    }

    [Fact]
    public void HasUnsavedChanges_WhenAnyDirtyDocument_ReturnsTrue()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1", isDirty: false);
        var mockDoc2 = CreateMockDocument("doc-2", isDirty: true);
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);

        // Act & Assert
        _sut.HasUnsavedChanges().Should().BeTrue();
    }

    [Fact]
    public void GetTabOrder_ReturnsPinnedDocumentsFirst()
    {
        // Arrange
        var mockDoc1 = CreateMockDocument("doc-1", isPinned: false);
        var mockDoc2 = CreateMockDocument("doc-2", isPinned: true);
        var mockDoc3 = CreateMockDocument("doc-3", isPinned: false);
        _sut.RegisterDocument(mockDoc1.Object);
        _sut.RegisterDocument(mockDoc2.Object);
        _sut.RegisterDocument(mockDoc3.Object);

        // Act
        var result = _sut.GetTabOrder();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("doc-2"); // Pinned first
        result[1].Should().Be("doc-1");
        result[2].Should().Be("doc-3");
    }

    [Fact]
    public void GetDocument_WhenExists_ReturnsDocument()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1");
        _sut.RegisterDocument(mockDoc.Object);

        // Act
        var result = _sut.GetDocument("doc-1");

        // Assert
        result.Should().Be(mockDoc.Object);
    }

    [Fact]
    public void GetDocument_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = _sut.GetDocument("unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RegisterDocument_WhenAlreadyRegistered_DoesNotDuplicate()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1");
        _sut.RegisterDocument(mockDoc.Object);

        // Act
        _sut.RegisterDocument(mockDoc.Object);

        // Assert
        _sut.GetTabOrder().Should().HaveCount(1);
    }

    [Fact]
    public void UnregisterDocument_RemovesDocumentFromTracking()
    {
        // Arrange
        var mockDoc = CreateMockDocument("doc-1");
        _sut.RegisterDocument(mockDoc.Object);
        _sut.GetDocument("doc-1").Should().NotBeNull();

        // Act
        _sut.UnregisterDocument("doc-1");

        // Assert
        _sut.GetDocument("doc-1").Should().BeNull();
        _sut.GetTabOrder().Should().BeEmpty();
    }
}
