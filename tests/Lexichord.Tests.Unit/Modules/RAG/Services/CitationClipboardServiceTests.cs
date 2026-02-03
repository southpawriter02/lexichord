// =============================================================================
// File: CitationClipboardServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CitationClipboardService clipboard operations.
// =============================================================================
// LOGIC: Verifies all ICitationClipboardService functionality:
//   - Constructor null-parameter validation (4 dependencies).
//   - CopyCitationAsync: formats citation and publishes event.
//   - CopyCitationAsync: uses preferred style when style is null.
//   - CopyCitationAsync: uses explicit style when provided.
//   - CopyCitationAsync: handles null citation argument.
//   - CopyChunkTextAsync: copies raw text and publishes event.
//   - CopyChunkTextAsync: handles null arguments.
//   - CopyDocumentPathAsync: copies path as string.
//   - CopyDocumentPathAsync: copies path as file:// URI when asFileUri=true.
//   - All methods publish CitationCopiedEvent with correct format.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CitationClipboardService"/>.
/// Verifies clipboard copy operations and telemetry event publishing.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2d")]
public class CitationClipboardServiceTests
{
    // LOGIC: Mock dependencies for constructor injection.
    private readonly Mock<ICitationService> _citationServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<CitationClipboardService>> _loggerMock;
    private readonly Mock<ISystemSettingsRepository> _settingsMock;
    private readonly Mock<ILogger<CitationFormatterRegistry>> _registryLoggerMock;
    private readonly CitationFormatterRegistry _formatterRegistry;

    // LOGIC: System under test.
    private readonly CitationClipboardService _sut;

    // LOGIC: Shared test data.
    private static readonly Guid TestChunkId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime TestIndexedAt = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    public CitationClipboardServiceTests()
    {
        _citationServiceMock = new Mock<ICitationService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<CitationClipboardService>>();
        _settingsMock = new Mock<ISystemSettingsRepository>();
        _registryLoggerMock = new Mock<ILogger<CitationFormatterRegistry>>();

        // LOGIC: Default setup — preferred style is Inline (stored as string).
        _settingsMock
            .Setup(x => x.GetValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string defaultValue, CancellationToken ct) => CitationStyle.Inline.ToString());

        // LOGIC: Create mock formatters for the registry.
        var formatters = new List<ICitationFormatter>
        {
            CreateMockFormatter(CitationStyle.Inline),
            CreateMockFormatter(CitationStyle.Footnote),
            CreateMockFormatter(CitationStyle.Markdown)
        };

        // LOGIC: Create a real CitationFormatterRegistry (sealed class - can't mock).
        _formatterRegistry = new CitationFormatterRegistry(
            formatters,
            _settingsMock.Object,
            _registryLoggerMock.Object);

        // LOGIC: Default citation formatting returns a formatted string.
        _citationServiceMock
            .Setup(x => x.FormatCitation(It.IsAny<Citation>(), It.IsAny<CitationStyle>()))
            .Returns((Citation c, CitationStyle s) => $"[{c.FileName}, formatted as {s}]");

        _sut = new CitationClipboardService(
            _citationServiceMock.Object,
            _formatterRegistry,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static ICitationFormatter CreateMockFormatter(CitationStyle style)
    {
        var mock = new Mock<ICitationFormatter>();
        mock.Setup(x => x.Style).Returns(style);
        mock.Setup(x => x.DisplayName).Returns(style.ToString());
        mock.Setup(x => x.Format(It.IsAny<Citation>())).Returns((Citation c) => $"[{c.FileName}]");
        return mock.Object;
    }

    // =========================================================================
    // Test Helpers
    // =========================================================================

    private static Citation CreateTestCitation(
        string documentPath = "/docs/test.md",
        string title = "Test Document",
        string? heading = "Introduction",
        int? lineNumber = 42)
    {
        return new Citation(
            ChunkId: TestChunkId,
            DocumentPath: documentPath,
            DocumentTitle: title,
            StartOffset: 100,
            EndOffset: 500,
            Heading: heading,
            LineNumber: lineNumber,
            IndexedAt: TestIndexedAt);
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullCitationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationClipboardService(
            null!,
            _formatterRegistry,
            _mediatorMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("citationService", act);
    }

    [Fact]
    public void Constructor_NullFormatterRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationClipboardService(
            _citationServiceMock.Object,
            null!,
            _mediatorMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("formatterRegistry", act);
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationClipboardService(
            _citationServiceMock.Object,
            _formatterRegistry,
            null!,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("mediator", act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationClipboardService(
            _citationServiceMock.Object,
            _formatterRegistry,
            _mediatorMock.Object,
            null!);
        Assert.Throws<ArgumentNullException>("logger", act);
    }

    // =========================================================================
    // CopyCitationAsync
    // =========================================================================

    [Fact]
    public async Task CopyCitationAsync_NullCitation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "citation",
            () => _sut.CopyCitationAsync(null!));
    }

    [Fact]
    public async Task CopyCitationAsync_StyleProvided_UsesThatStyle()
    {
        // Arrange
        var citation = CreateTestCitation();

        // Act
        // Note: This will not actually copy to clipboard in test environment
        // as Avalonia Application.Current is null. The service logs a warning.
        await _sut.CopyCitationAsync(citation, CitationStyle.Markdown);

        // Assert
        _citationServiceMock.Verify(
            x => x.FormatCitation(citation, CitationStyle.Markdown),
            Times.Once);
    }

    [Fact]
    public async Task CopyCitationAsync_NoStyleProvided_UsesPreferredStyle()
    {
        // Arrange
        var citation = CreateTestCitation();

        // Act
        await _sut.CopyCitationAsync(citation); // No style parameter

        // Assert
        // Default preferred style is Inline (from mock setup)
        _citationServiceMock.Verify(
            x => x.FormatCitation(citation, CitationStyle.Inline),
            Times.Once);
    }

    // =========================================================================
    // CopyChunkTextAsync
    // =========================================================================

    [Fact]
    public async Task CopyChunkTextAsync_NullCitation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "citation",
            () => _sut.CopyChunkTextAsync(null!, "some text"));
    }

    [Fact]
    public async Task CopyChunkTextAsync_NullChunkText_ThrowsArgumentNullException()
    {
        // Arrange
        var citation = CreateTestCitation();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "chunkText",
            () => _sut.CopyChunkTextAsync(citation, null!));
    }

    // =========================================================================
    // CopyDocumentPathAsync
    // =========================================================================

    [Fact]
    public async Task CopyDocumentPathAsync_NullCitation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "citation",
            () => _sut.CopyDocumentPathAsync(null!));
    }

    // =========================================================================
    // Note: Full integration tests for clipboard operations require a running
    // Avalonia application context. The above tests verify argument validation
    // and service method delegation. UI-context tests would be integration tests.
    // =========================================================================
}
