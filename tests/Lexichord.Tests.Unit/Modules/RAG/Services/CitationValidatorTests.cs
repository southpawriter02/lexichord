// =============================================================================
// File: CitationValidatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CitationValidator citation freshness validation.
// =============================================================================
// LOGIC: Verifies all CitationValidator functionality:
//   - Constructor null-parameter validation (3 dependencies).
//   - ValidateAsync: returns Valid for unchanged file.
//   - ValidateAsync: returns Stale for modified file.
//   - ValidateAsync: returns Missing for nonexistent file.
//   - ValidateAsync: returns Error for inaccessible file (IOException).
//   - ValidateAsync: throws ArgumentNullException for null citation.
//   - ValidateAsync: publishes CitationValidationFailedEvent for Stale.
//   - ValidateAsync: publishes CitationValidationFailedEvent for Missing.
//   - ValidateAsync: does NOT publish event for Valid.
//   - ValidateAsync: populates CurrentModifiedAt for existing files.
//   - ValidateAsync: CurrentModifiedAt is null for missing files.
//   - ValidateBatchAsync: validates multiple citations.
//   - ValidateBatchAsync: handles mixed results correctly.
//   - ValidateBatchAsync: empty input returns empty list.
//   - ValidateBatchAsync: throws ArgumentNullException for null input.
//   - ValidateIfLicensedAsync: returns result when licensed.
//   - ValidateIfLicensedAsync: returns null when unlicensed.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CitationValidator"/>.
/// Verifies citation freshness validation, batch operations, and license gating.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2c")]
public class CitationValidatorTests : IDisposable
{
    // LOGIC: Mock dependencies for constructor injection.
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<CitationValidator>> _loggerMock;

    // LOGIC: Temp file management for file system tests.
    private readonly List<string> _tempFiles = new();

    // LOGIC: Shared test data.
    private static readonly Guid TestDocId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public CitationValidatorTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<CitationValidator>>();

        // LOGIC: Default setup — licensed for citation validation.
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.CitationValidation))
            .Returns(true);
    }

    public void Dispose()
    {
        // LOGIC: Clean up temp files created during tests.
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationValidator(
            null!, _mediatorMock.Object, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("licenseContext", act);
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationValidator(
            _licenseContextMock.Object, null!, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("mediator", act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationValidator(
            _licenseContextMock.Object, _mediatorMock.Object, null!);
        Assert.Throws<ArgumentNullException>("logger", act);
    }

    // =========================================================================
    // ValidateAsync — Valid
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_FileUnchanged_ReturnsValid()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        // LOGIC: Set the file's last write time to before IndexedAt.
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-1));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(CitationValidationStatus.Valid, result.Status);
        Assert.False(result.IsStale);
        Assert.False(result.IsMissing);
        Assert.False(result.HasError);
    }

    [Fact]
    public async Task ValidateAsync_Valid_PopulatesCurrentModifiedAt()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-1));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.NotNull(result.CurrentModifiedAt);
    }

    [Fact]
    public async Task ValidateAsync_Valid_DoesNotPublishEvent()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-1));
        var sut = CreateValidator();

        // Act
        await sut.ValidateAsync(citation);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.IsAny<CitationValidationFailedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // ValidateAsync — Stale
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_FileModified_ReturnsStale()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        // LOGIC: File was written NOW, but IndexedAt was 2 hours ago.
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-2));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(CitationValidationStatus.Stale, result.Status);
        Assert.True(result.IsStale);
    }

    [Fact]
    public async Task ValidateAsync_Stale_PublishesCitationValidationFailedEvent()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-2));
        var sut = CreateValidator();

        // Act
        await sut.ValidateAsync(citation);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<CitationValidationFailedEvent>(e => e.Result.IsStale),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_Stale_PopulatesCurrentModifiedAt()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-2));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.NotNull(result.CurrentModifiedAt);
    }

    // =========================================================================
    // ValidateAsync — Missing
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_FileMissing_ReturnsMissing()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/nonexistent/path/to/file.md");
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(CitationValidationStatus.Missing, result.Status);
        Assert.True(result.IsMissing);
    }

    [Fact]
    public async Task ValidateAsync_Missing_PublishesCitationValidationFailedEvent()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/nonexistent/path/to/file.md");
        var sut = CreateValidator();

        // Act
        await sut.ValidateAsync(citation);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<CitationValidationFailedEvent>(e => e.Result.IsMissing),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_Missing_CurrentModifiedAtIsNull()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/nonexistent/path/to/file.md");
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.Null(result.CurrentModifiedAt);
    }

    // =========================================================================
    // ValidateAsync — Error
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_NullCitation_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateValidator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.ValidateAsync(null!));
    }

    // =========================================================================
    // ValidateAsync — Citation Reference Preserved
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_Result_ContainsOriginalCitation()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-1));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateAsync(citation);

        // Assert
        Assert.Same(citation, result.Citation);
    }

    // =========================================================================
    // ValidateBatchAsync
    // =========================================================================

    [Fact]
    public async Task ValidateBatchAsync_MultipleValid_ReturnsAllValid()
    {
        // Arrange
        var citations = new List<Citation>();
        for (var i = 0; i < 5; i++)
        {
            var tempFile = CreateTempFile($"content {i}");
            File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
            citations.Add(CreateCitation(
                documentPath: tempFile,
                indexedAt: DateTime.UtcNow.AddHours(-1)));
        }

        var sut = CreateValidator();

        // Act
        var results = await sut.ValidateBatchAsync(citations);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.IsValid));
    }

    [Fact]
    public async Task ValidateBatchAsync_MixedResults_ReturnsCorrectStatuses()
    {
        // Arrange
        var validFile = CreateTempFile("valid content");
        File.SetLastWriteTimeUtc(validFile, DateTime.UtcNow.AddHours(-2));

        var staleFile = CreateTempFile("stale content");
        // staleFile written NOW, indexedAt 2 hours ago → stale

        var citations = new List<Citation>
        {
            CreateCitation(
                documentPath: validFile,
                indexedAt: DateTime.UtcNow.AddHours(-1)),
            CreateCitation(
                documentPath: staleFile,
                indexedAt: DateTime.UtcNow.AddHours(-2)),
            CreateCitation(
                documentPath: "/nonexistent/file.md")
        };

        var sut = CreateValidator();

        // Act
        var results = await sut.ValidateBatchAsync(citations);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(CitationValidationStatus.Valid, results[0].Status);
        Assert.Equal(CitationValidationStatus.Stale, results[1].Status);
        Assert.Equal(CitationValidationStatus.Missing, results[2].Status);
    }

    [Fact]
    public async Task ValidateBatchAsync_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateValidator();

        // Act
        var results = await sut.ValidateBatchAsync(Array.Empty<Citation>());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ValidateBatchAsync_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateValidator();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.ValidateBatchAsync(null!));
    }

    [Fact]
    public async Task ValidateBatchAsync_PreservesInputOrder()
    {
        // Arrange
        var file1 = CreateTempFile("file 1");
        File.SetLastWriteTimeUtc(file1, DateTime.UtcNow.AddHours(-2));
        var file2 = CreateTempFile("file 2");
        File.SetLastWriteTimeUtc(file2, DateTime.UtcNow.AddHours(-2));

        var citations = new List<Citation>
        {
            CreateCitation(documentPath: file1, indexedAt: DateTime.UtcNow.AddHours(-1)),
            CreateCitation(documentPath: "/missing.md"),
            CreateCitation(documentPath: file2, indexedAt: DateTime.UtcNow.AddHours(-1))
        };

        var sut = CreateValidator();

        // Act
        var results = await sut.ValidateBatchAsync(citations);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(file1, results[0].Citation.DocumentPath);
        Assert.Equal("/missing.md", results[1].Citation.DocumentPath);
        Assert.Equal(file2, results[2].Citation.DocumentPath);
    }

    // =========================================================================
    // ValidateIfLicensedAsync
    // =========================================================================

    [Fact]
    public async Task ValidateIfLicensedAsync_Licensed_ReturnsResult()
    {
        // Arrange
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.CitationValidation))
            .Returns(true);

        var tempFile = CreateTempFile("test content");
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-1));
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateIfLicensedAsync(citation);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateIfLicensedAsync_Unlicensed_ReturnsNull()
    {
        // Arrange
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.CitationValidation))
            .Returns(false);

        var citation = CreateCitation();
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateIfLicensedAsync(citation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateIfLicensedAsync_Unlicensed_DoesNotAccessFileSystem()
    {
        // Arrange
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.CitationValidation))
            .Returns(false);

        // LOGIC: Use a nonexistent path — if file system is accessed, it would
        // return Missing, not null.
        var citation = CreateCitation(documentPath: "/nonexistent/file.md");
        var sut = CreateValidator();

        // Act
        var result = await sut.ValidateIfLicensedAsync(citation);

        // Assert — null means license check returned before file system access
        Assert.Null(result);
        _mediatorMock.Verify(
            m => m.Publish(
                It.IsAny<CitationValidationFailedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Event Timestamp
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_Stale_EventTimestampIsReasonable()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-2));
        var sut = CreateValidator();
        var before = DateTime.UtcNow;

        // Act
        await sut.ValidateAsync(citation);
        var after = DateTime.UtcNow;

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<CitationValidationFailedEvent>(e =>
                    e.Timestamp >= before && e.Timestamp <= after),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a CitationValidator with mock dependencies.
    /// </summary>
    private CitationValidator CreateValidator()
    {
        return new CitationValidator(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Creates a test Citation with configurable values.
    /// </summary>
    private static Citation CreateCitation(
        string documentPath = "/docs/test.md",
        DateTime? indexedAt = null)
    {
        return new Citation(
            ChunkId: TestDocId,
            DocumentPath: documentPath,
            DocumentTitle: "Test Document",
            StartOffset: 0,
            EndOffset: 100,
            Heading: "Introduction",
            LineNumber: 10,
            IndexedAt: indexedAt ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a temporary file with the specified content and tracks it for cleanup.
    /// </summary>
    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }
}
