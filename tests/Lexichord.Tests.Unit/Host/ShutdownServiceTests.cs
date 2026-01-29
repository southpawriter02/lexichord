using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.ViewModels;
using Lexichord.Host.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for ShutdownService.
/// </summary>
[Trait("Category", "Unit")]
public class ShutdownServiceTests
{
    private readonly Mock<ILogger<ShutdownService>> _loggerMock = new();

    private ShutdownService CreateService() => new(_loggerMock.Object);

    [Fact]
    public void HasDirtyDocuments_NoDocuments_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.HasDirtyDocuments;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasDirtyDocuments_WithCleanDocument_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument(isDirty: false);
        service.RegisterDocument(document);

        // Act
        var result = service.HasDirtyDocuments;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasDirtyDocuments_WithDirtyDocument_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument(isDirty: true);
        service.RegisterDocument(document);

        // Act
        var result = service.HasDirtyDocuments;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RegisterDocument_AddsToList()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument();

        // Act
        service.RegisterDocument(document);

        // Assert
        service.GetRegisteredDocuments().Should().Contain(document);
    }

    [Fact]
    public void RegisterDocument_SameDocumentTwice_OnlyOnce()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument();

        // Act
        service.RegisterDocument(document);
        service.RegisterDocument(document);

        // Assert
        service.GetRegisteredDocuments().Should().HaveCount(1);
    }

    [Fact]
    public void UnregisterDocument_RemovesFromList()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument();
        service.RegisterDocument(document);

        // Act
        service.UnregisterDocument(document);

        // Assert
        service.GetRegisteredDocuments().Should().NotContain(document);
    }

    [Fact]
    public void GetDirtyDocuments_ReturnsOnlyDirty()
    {
        // Arrange
        var service = CreateService();
        var cleanDoc = CreateDocument(isDirty: false);
        var dirtyDoc = CreateDocument(isDirty: true);
        service.RegisterDocument(cleanDoc);
        service.RegisterDocument(dirtyDoc);

        // Act
        var result = service.GetDirtyDocuments();

        // Assert
        result.Should().Contain(dirtyDoc);
        result.Should().NotContain(cleanDoc);
    }

    [Fact]
    public async Task RequestShutdownAsync_NoDirty_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument(isDirty: false);
        service.RegisterDocument(document);

        // Act
        var result = await service.RequestShutdownAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RequestShutdownAsync_WithDirty_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument(isDirty: true);
        service.RegisterDocument(document);

        // Act
        var result = await service.RequestShutdownAsync();

        // Assert
        // Returns true because there are dirty documents that need handling
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShutdownRequested_EventRaised()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument(isDirty: true);
        service.RegisterDocument(document);
        ShutdownRequestedEventArgs? capturedArgs = null;
        service.ShutdownRequested += (_, args) => capturedArgs = args;

        // Act
        await service.RequestShutdownAsync();

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.DirtyDocuments.Should().Contain(document);
    }

    [Fact]
    public async Task RequestShutdownAsync_EventHandlerCancels_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var document = CreateDocument(isDirty: true);
        service.RegisterDocument(document);
        service.ShutdownRequested += (_, args) => args.Cancel = true;

        // Act
        var result = await service.RequestShutdownAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsShuttingDown_InitiallyFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsShuttingDown;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Test document for use in tests where DocumentViewModelBase is needed.
    /// </summary>
    private class TestDocument : DocumentViewModelBase
    {
        private readonly string _documentId;
        private readonly string _title;

        public TestDocument(bool isDirty = false, string title = "Test")
            : base(null)
        {
            _documentId = Guid.NewGuid().ToString();
            _title = title;
            if (isDirty)
            {
                // Use the protected setter via reflection or mark dirty
                SetDirty(true);
            }
        }

        public override string DocumentId => _documentId;
        public override string Title => _title;

        public void SetDirty(bool isDirty)
        {
            // Access the private field directly
            IsDirty = isDirty;
        }
    }

    private static DocumentViewModelBase CreateDocument(bool isDirty = false, string title = "Test")
    {
        return new TestDocument(isDirty, title);
    }
}
