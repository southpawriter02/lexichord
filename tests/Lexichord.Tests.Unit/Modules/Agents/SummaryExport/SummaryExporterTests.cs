// -----------------------------------------------------------------------
// <copyright file="SummaryExporterTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Agents.SummaryExport;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.SummaryExport;
using Lexichord.Modules.Agents.SummaryExport.Events;
using Lexichord.Modules.Agents.SummaryExport.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="SummaryExporter"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class SummaryExporterTests
{
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IEditorService> _mockEditorService;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<ISummaryCacheService> _mockCacheService;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IMediator> _mockMediator;
    private readonly ILogger<SummaryExporter> _logger;
    private readonly SummaryExporter _exporter;

    public SummaryExporterTests()
    {
        _mockFileService = new Mock<IFileService>();
        _mockEditorService = new Mock<IEditorService>();
        _mockClipboardService = new Mock<IClipboardService>();
        _mockCacheService = new Mock<ISummaryCacheService>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockMediator = new Mock<IMediator>();
        _logger = NullLogger<SummaryExporter>.Instance;

        // Default: license enabled
        _mockLicenseContext
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SummaryExport))
            .Returns(true);

        _exporter = new SummaryExporter(
            _mockFileService.Object,
            _mockEditorService.Object,
            _mockClipboardService.Object,
            _mockCacheService.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);
    }

    // ── Constructor Validation ──────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullFileService_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            null!,
            _mockEditorService.Object,
            _mockClipboardService.Object,
            _mockCacheService.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileService");
    }

    [Fact]
    public void Constructor_WithNullEditorService_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            _mockFileService.Object,
            null!,
            _mockClipboardService.Object,
            _mockCacheService.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_WithNullClipboardService_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            _mockFileService.Object,
            _mockEditorService.Object,
            null!,
            _mockCacheService.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clipboardService");
    }

    [Fact]
    public void Constructor_WithNullCacheService_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            _mockFileService.Object,
            _mockEditorService.Object,
            _mockClipboardService.Object,
            null!,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cacheService");
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            _mockFileService.Object,
            _mockEditorService.Object,
            _mockClipboardService.Object,
            _mockCacheService.Object,
            null!,
            _mockMediator.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            _mockFileService.Object,
            _mockEditorService.Object,
            _mockClipboardService.Object,
            _mockCacheService.Object,
            _mockLicenseContext.Object,
            null!,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SummaryExporter(
            _mockFileService.Object,
            _mockEditorService.Object,
            _mockClipboardService.Object,
            _mockCacheService.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── ExportAsync: Argument Validation ────────────────────────────────

    [Fact]
    public async Task ExportAsync_WithNullSummary_ThrowsArgumentNullException()
    {
        var options = new SummaryExportOptions { Destination = ExportDestination.Clipboard };

        var act = () => _exporter.ExportAsync(null!, "/path/to/doc.md", options);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("summary");
    }

    [Fact]
    public async Task ExportAsync_WithNullSourceDocumentPath_ThrowsArgumentNullException()
    {
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions { Destination = ExportDestination.Clipboard };

        var act = () => _exporter.ExportAsync(summary, null!, options);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sourceDocumentPath");
    }

    // ── ExportAsync: License Gating ─────────────────────────────────────

    [Fact]
    public async Task ExportAsync_WhenLicenseDisabled_ReturnsFailedResult()
    {
        // Arrange
        _mockLicenseContext
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SummaryExport))
            .Returns(false);

        var summary = CreateTestSummary();
        var options = new SummaryExportOptions { Destination = ExportDestination.Clipboard };

        // Act
        var result = await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("WriterPro");
    }

    // ── ExportAsync: Clipboard Destination ──────────────────────────────

    [Fact]
    public async Task ExportAsync_ToClipboard_CopiesText()
    {
        // Arrange
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.Clipboard,
            ClipboardAsMarkdown = true
        };

        // Act
        var result = await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(ExportDestination.Clipboard);
        result.CharactersWritten.Should().Be(summary.Summary.Length);

        _mockClipboardService.Verify(
            c => c.SetTextAsync(summary.Summary, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportAsync_ToClipboard_PublishesEvents()
    {
        // Arrange
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions { Destination = ExportDestination.Clipboard };

        // Act
        await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<SummaryExportStartedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMediator.Verify(
            m => m.Publish(It.IsAny<SummaryExportedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ExportAsync: File Destination ───────────────────────────────────

    [Fact]
    public async Task ExportAsync_ToFile_CreatesFile()
    {
        // Arrange
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.File,
            OutputPath = "/path/to/output.summary.md",
            IncludeMetadata = false,
            IncludeSourceReference = false
        };

        _mockFileService
            .Setup(f => f.Exists(It.IsAny<string>()))
            .Returns(false);

        _mockFileService
            .Setup(f => f.SaveAsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveResult(true, string.Empty, 1024, TimeSpan.Zero));

        // Act
        var result = await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(ExportDestination.File);
        result.OutputPath.Should().Be("/path/to/output.summary.md");
        result.BytesWritten.Should().Be(1024);
    }

    [Fact]
    public async Task ExportAsync_ToFile_WhenFileExistsAndOverwriteFalse_ReturnsFailedResult()
    {
        // Arrange
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.File,
            OutputPath = "/path/to/existing.md",
            Overwrite = false
        };

        _mockFileService
            .Setup(f => f.Exists("/path/to/existing.md"))
            .Returns(true);

        // Act
        var result = await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    // ── ExportAsync: Panel Destination ──────────────────────────────────

    [Fact]
    public async Task ExportAsync_ToPanel_CachesSummary()
    {
        // Arrange
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions { Destination = ExportDestination.Panel };

        // Act
        var result = await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(ExportDestination.Panel);

        _mockCacheService.Verify(
            c => c.SetAsync("/path/to/doc.md", summary, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ExportAsync: InlineInsert Destination ───────────────────────────

    [Fact]
    public async Task ExportAsync_ToInlineInsert_InsertsAtCursor()
    {
        // Arrange
        var summary = CreateTestSummary();
        var options = new SummaryExportOptions
        {
            Destination = ExportDestination.InlineInsert,
            UseCalloutBlock = false
        };

        _mockEditorService.Setup(e => e.CaretOffset).Returns(100);

        // Act
        var result = await _exporter.ExportAsync(summary, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeTrue();
        result.Destination.Should().Be(ExportDestination.InlineInsert);

        _mockEditorService.Verify(e => e.BeginUndoGroup("Insert Summary"), Times.Once);
        _mockEditorService.Verify(e => e.InsertText(100, It.Is<string>(s => s.Contains(summary.Summary))), Times.Once);
        _mockEditorService.Verify(e => e.EndUndoGroup(), Times.Once);
    }

    // ── ExportMetadataAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ExportMetadataAsync_WithNullMetadata_ThrowsArgumentNullException()
    {
        var options = new SummaryExportOptions { Destination = ExportDestination.Frontmatter };

        var act = () => _exporter.ExportMetadataAsync(null!, "/path/to/doc.md", options);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("metadata");
    }

    [Fact]
    public async Task ExportMetadataAsync_ToNonFrontmatter_ReturnsFailedResult()
    {
        // Arrange
        var metadata = CreateTestMetadata();
        var options = new SummaryExportOptions { Destination = ExportDestination.Clipboard };

        // Act
        var result = await _exporter.ExportMetadataAsync(metadata, "/path/to/doc.md", options);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("only supported for Frontmatter");
    }

    // ── UpdateFrontmatterAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateFrontmatterAsync_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        var summary = CreateTestSummary();

        var act = () => _exporter.UpdateFrontmatterAsync(null!, summary, null);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task UpdateFrontmatterAsync_WithBothNull_ThrowsArgumentException()
    {
        var act = () => _exporter.UpdateFrontmatterAsync("/path/to/doc.md", null, null);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetCachedSummaryAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetCachedSummaryAsync_WithNullPath_ThrowsArgumentNullException()
    {
        var act = () => _exporter.GetCachedSummaryAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task GetCachedSummaryAsync_DelegatesToCacheService()
    {
        // Arrange
        var cached = CreateTestCachedSummary();
        _mockCacheService
            .Setup(c => c.GetAsync("/path/to/doc.md", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        var result = await _exporter.GetCachedSummaryAsync("/path/to/doc.md");

        // Assert
        result.Should().BeSameAs(cached);
    }

    // ── CacheSummaryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CacheSummaryAsync_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        var summary = CreateTestSummary();

        var act = () => _exporter.CacheSummaryAsync(null!, summary, null);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task CacheSummaryAsync_WithNullSummary_ThrowsArgumentNullException()
    {
        var act = () => _exporter.CacheSummaryAsync("/path/to/doc.md", null!, null);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("summary");
    }

    // ── ClearCacheAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ClearCacheAsync_WithNullPath_ThrowsArgumentNullException()
    {
        var act = () => _exporter.ClearCacheAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task ClearCacheAsync_DelegatesToCacheService()
    {
        // Act
        await _exporter.ClearCacheAsync("/path/to/doc.md");

        // Assert
        _mockCacheService.Verify(
            c => c.ClearAsync("/path/to/doc.md", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ShowInPanelAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ShowInPanelAsync_WithNullSummary_ThrowsArgumentNullException()
    {
        var act = () => _exporter.ShowInPanelAsync(null!, null, "/path/to/doc.md");

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("summary");
    }

    [Fact]
    public async Task ShowInPanelAsync_WithNullPath_ThrowsArgumentNullException()
    {
        var summary = CreateTestSummary();

        var act = () => _exporter.ShowInPanelAsync(summary, null, null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sourceDocumentPath");
    }

    [Fact]
    public async Task ShowInPanelAsync_PublishesPanelOpenedEvent()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        await _exporter.ShowInPanelAsync(summary, null, "/path/to/doc.md");

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<SummaryPanelOpenedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private static SummarizationResult CreateTestSummary()
    {
        return new SummarizationResult
        {
            Summary = "This is a test summary with bullet points.",
            Mode = SummarizationMode.BulletPoints,
            OriginalWordCount = 1000,
            SummaryWordCount = 100,
            Usage = UsageMetrics.Zero
        };
    }

    private static DocumentMetadata CreateTestMetadata()
    {
        return new DocumentMetadata
        {
            OneLiner = "A test document",
            KeyTerms = new[] { KeyTerm.Create("test", 0.8) },
            Concepts = new[] { "testing" },
            SuggestedTags = new[] { "test", "unit" },
            EstimatedReadingMinutes = 5,
            ComplexityScore = 5,
            Usage = UsageMetrics.Zero
        };
    }

    private static CachedSummary CreateTestCachedSummary()
    {
        return CachedSummary.Create(
            "/path/to/doc.md",
            "sha256:abc123",
            CreateTestSummary());
    }
}
