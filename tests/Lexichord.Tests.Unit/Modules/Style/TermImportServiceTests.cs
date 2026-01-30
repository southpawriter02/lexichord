using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TermImportService.
/// </summary>
/// <remarks>
/// LOGIC: Tests verify CSV/Excel parsing, validation, and commit operations.
/// Version: v0.2.5d
/// </remarks>
[Trait("Category", "Unit")]
public class TermImportServiceTests
{
    private readonly Mock<ITerminologyRepository> _repositoryMock = new();
    private readonly Mock<ITerminologyService> _terminologyServiceMock = new();
    private readonly ILogger<TermImportService> _logger = NullLogger<TermImportService>.Instance;

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TermImportService(null!, _terminologyServiceMock.Object, _logger));
    }

    [Fact]
    public void Constructor_NullTerminologyService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TermImportService(_repositoryMock.Object, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TermImportService(_repositoryMock.Object, _terminologyServiceMock.Object, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ParseCsvAsync Tests

    [Fact]
    public async Task ParseCsvAsync_ValidCsv_ReturnsCorrectRowCount()
    {
        // Arrange
        var csv = """
            pattern,recommendation,category,severity,match_case,is_active
            click on,Use select,Terminology,warning,false,true
            utilize,Use 'use',Clarity,suggestion,false,true
            """;

        await using var stream = CreateStream(csv);
        SetupEmptyRepository();
        var service = CreateService();

        // Act
        var result = await service.ParseCsvAsync(stream);

        // Assert
        result.TotalCount.Should().Be(2);
        result.ValidCount.Should().Be(2);
        result.InvalidCount.Should().Be(0);
        result.DuplicateCount.Should().Be(0);
    }

    [Fact]
    public async Task ParseCsvAsync_WithDuplicates_DetectsDuplicates()
    {
        // Arrange
        var csv = """
            pattern,recommendation
            click on,Use select
            """;

        await using var stream = CreateStream(csv);
        SetupRepositoryWithTerm("click on");
        var service = CreateService();

        // Act
        var result = await service.ParseCsvAsync(stream);

        // Assert
        result.TotalCount.Should().Be(1);
        result.DuplicateCount.Should().Be(1);
        result.ValidCount.Should().Be(0);
        result.Rows[0].Status.Should().Be(ImportRowStatus.Duplicate);
    }

    [Fact]
    public async Task ParseCsvAsync_InvalidPattern_MarksAsInvalid()
    {
        // Arrange - unbalanced parentheses is invalid regex
        var csv = """
            pattern,recommendation
            (unbalanced,Use something
            """;

        await using var stream = CreateStream(csv);
        SetupEmptyRepository();
        var service = CreateService();

        // Act
        var result = await service.ParseCsvAsync(stream);

        // Assert
        result.TotalCount.Should().Be(1);
        result.InvalidCount.Should().Be(1);
        result.Rows[0].Status.Should().Be(ImportRowStatus.Invalid);
        result.Rows[0].ErrorMessage.Should().Contain("Invalid regex");
    }

    [Fact]
    public async Task ParseCsvAsync_EmptyPattern_MarksAsInvalid()
    {
        // Arrange
        var csv = """
            pattern,recommendation
            ,Use something
            """;

        await using var stream = CreateStream(csv);
        SetupEmptyRepository();
        var service = CreateService();

        // Act
        var result = await service.ParseCsvAsync(stream);

        // Assert
        result.InvalidCount.Should().Be(1);
        result.Rows[0].Status.Should().Be(ImportRowStatus.Invalid);
        result.Rows[0].ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task ParseCsvAsync_FileTooLarge_ThrowsImportException()
    {
        // Arrange - create a stream that reports being larger than max
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.Length).Returns(ITermImportService.MaxFileSizeBytes + 1);
        SetupEmptyRepository();
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ImportException>(() =>
            service.ParseCsvAsync(mockStream.Object));
    }

    [Fact]
    public async Task ParseCsvAsync_AutoDetectsHeaders()
    {
        // Arrange - use alternative header names
        var csv = """
            term,replacement,type,level
            click on,Use select,Terminology,warning
            """;

        await using var stream = CreateStream(csv);
        SetupEmptyRepository();
        var service = CreateService();

        // Act
        var result = await service.ParseCsvAsync(stream);

        // Assert
        result.ValidCount.Should().Be(1);
        result.Rows[0].Pattern.Should().Be("click on");
        result.Rows[0].Recommendation.Should().Be("Use select");
    }

    [Fact]
    public async Task ParseCsvAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var csv = "pattern,recommendation\n" +
            string.Join("\n", Enumerable.Range(1, 200).Select(i => $"term{i},replacement{i}"));

        await using var stream = CreateStream(csv);
        SetupEmptyRepository();
        var service = CreateService();

        var progressValues = new List<int>();
        var progress = new Progress<int>(v => progressValues.Add(v));

        // Act
        await service.ParseCsvAsync(stream, progress: progress);

        // Assert - should have reported progress
        progressValues.Should().Contain(100);
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_ValidRows_CreatesTerms()
    {
        // Arrange
        var parseResult = new ImportParseResult(
            new[]
            {
                new ImportRow(1, "click on", "Use select", "Terminology", "warning", false, true, ImportRowStatus.Valid)
            },
            ValidCount: 1,
            InvalidCount: 0,
            DuplicateCount: 0);

        _terminologyServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateTermCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var service = CreateService();

        // Act
        var result = await service.CommitAsync(parseResult);

        // Assert
        result.Success.Should().BeTrue();
        result.ImportedCount.Should().Be(1);
        _terminologyServiceMock.Verify(
            s => s.CreateAsync(It.Is<CreateTermCommand>(c => c.Term == "click on"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitAsync_DuplicatesWithSkip_DoesNotProcessDuplicates()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var row = new ImportRow(1, "click on", "Use select", "Terminology", "warning", false, true,
            ImportRowStatus.Duplicate, ExistingTermId: existingId);
        row.IsSelected = true;

        var parseResult = new ImportParseResult(
            new[] { row },
            ValidCount: 0,
            InvalidCount: 0,
            DuplicateCount: 1);

        var service = CreateService();

        // Act - With Skip handling, duplicate rows are filtered out so no operations occur
        var result = await service.CommitAsync(parseResult, DuplicateHandling.Skip);

        // Assert - No creates or updates should occur
        result.ImportedCount.Should().Be(0);
        result.UpdatedCount.Should().Be(0);
        _terminologyServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateTermCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _terminologyServiceMock.Verify(
            s => s.UpdateAsync(It.IsAny<UpdateTermCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CommitAsync_DuplicatesWithOverwrite_UpdatesTerms()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var row = new ImportRow(1, "click on", "Use select", "Terminology", "warning", false, true,
            ImportRowStatus.Duplicate, ExistingTermId: existingId);
        row.IsSelected = true;

        var parseResult = new ImportParseResult(
            new[] { row },
            ValidCount: 0,
            InvalidCount: 0,
            DuplicateCount: 1);

        _terminologyServiceMock
            .Setup(s => s.UpdateAsync(It.IsAny<UpdateTermCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var service = CreateService();

        // Act
        var result = await service.CommitAsync(parseResult, DuplicateHandling.Overwrite);

        // Assert
        result.UpdatedCount.Should().Be(1);
        _terminologyServiceMock.Verify(
            s => s.UpdateAsync(It.Is<UpdateTermCommand>(c => c.Id == existingId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitAsync_UnselectedRows_AreSkipped()
    {
        // Arrange
        var row = new ImportRow(1, "click on", "Use select", "Terminology", "warning", false, true, ImportRowStatus.Valid);
        row.IsSelected = false;

        var parseResult = new ImportParseResult(
            new[] { row },
            ValidCount: 1,
            InvalidCount: 0,
            DuplicateCount: 0);

        var service = CreateService();

        // Act
        var result = await service.CommitAsync(parseResult);

        // Assert
        result.ImportedCount.Should().Be(0);
        _terminologyServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateTermCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private TermImportService CreateService() =>
        new(_repositoryMock.Object, _terminologyServiceMock.Object, _logger);

    private static MemoryStream CreateStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));

    private void SetupEmptyRepository()
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm>());
    }

    private void SetupRepositoryWithTerm(string term)
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm>
            {
                new() { Id = Guid.NewGuid(), Term = term, IsActive = true }
            });
    }

    #endregion
}
