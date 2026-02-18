// -----------------------------------------------------------------------
// <copyright file="SummaryPanelViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Agents.SummaryExport;
using Lexichord.Modules.Agents.SummaryExport.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="SummaryPanelViewModel"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class SummaryPanelViewModelTests
{
    private readonly Mock<ISummaryExporter> _mockExporter;
    private readonly Mock<ISummarizerAgent> _mockSummarizer;
    private readonly Mock<IMetadataExtractor> _mockMetadataExtractor;
    private readonly ILogger<SummaryPanelViewModel> _logger;
    private readonly SummaryPanelViewModel _viewModel;

    public SummaryPanelViewModelTests()
    {
        _mockExporter = new Mock<ISummaryExporter>();
        _mockSummarizer = new Mock<ISummarizerAgent>();
        _mockMetadataExtractor = new Mock<IMetadataExtractor>();
        _logger = NullLogger<SummaryPanelViewModel>.Instance;

        _viewModel = new SummaryPanelViewModel(
            _mockExporter.Object,
            _mockSummarizer.Object,
            _mockMetadataExtractor.Object,
            _logger);
    }

    // ── Constructor Validation ──────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullExporter_ThrowsArgumentNullException()
    {
        var act = () => new SummaryPanelViewModel(
            null!,
            _mockSummarizer.Object,
            _mockMetadataExtractor.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exporter");
    }

    [Fact]
    public void Constructor_WithNullSummarizer_ThrowsArgumentNullException()
    {
        var act = () => new SummaryPanelViewModel(
            _mockExporter.Object,
            null!,
            _mockMetadataExtractor.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("summarizer");
    }

    [Fact]
    public void Constructor_WithNullMetadataExtractor_ThrowsArgumentNullException()
    {
        var act = () => new SummaryPanelViewModel(
            _mockExporter.Object,
            _mockSummarizer.Object,
            null!,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadataExtractor");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SummaryPanelViewModel(
            _mockExporter.Object,
            _mockSummarizer.Object,
            _mockMetadataExtractor.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── Initial State ───────────────────────────────────────────────────

    [Fact]
    public void InitialState_HasDefaultValues()
    {
        // Assert
        _viewModel.DocumentPath.Should().BeNull();
        _viewModel.DocumentName.Should().BeNull();
        _viewModel.Summary.Should().BeNull();
        _viewModel.Metadata.Should().BeNull();
        _viewModel.SelectedMode.Should().Be(SummarizationMode.BulletPoints);
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.GenerationInfo.Should().BeNull();
        _viewModel.ErrorMessage.Should().BeNull();
        _viewModel.HasContent.Should().BeFalse();
        _viewModel.HasMetadata.Should().BeFalse();
        _viewModel.KeyTerms.Should().BeEmpty();
    }

    [Fact]
    public void InitialState_CommandsAreNotNull()
    {
        // Assert
        _viewModel.RefreshCommand.Should().NotBeNull();
        _viewModel.CopySummaryCommand.Should().NotBeNull();
        _viewModel.AddToFrontmatterCommand.Should().NotBeNull();
        _viewModel.ExportFileCommand.Should().NotBeNull();
        _viewModel.CloseCommand.Should().NotBeNull();
        _viewModel.CopyKeyTermsCommand.Should().NotBeNull();
        _viewModel.ClearCacheCommand.Should().NotBeNull();
    }

    [Fact]
    public void InitialState_AvailableModesContainsAllModes()
    {
        // Assert
        _viewModel.AvailableModes.Should().HaveCount(6);
        _viewModel.AvailableModes.Should().Contain(SummarizationMode.Abstract);
        _viewModel.AvailableModes.Should().Contain(SummarizationMode.TLDR);
        _viewModel.AvailableModes.Should().Contain(SummarizationMode.BulletPoints);
        _viewModel.AvailableModes.Should().Contain(SummarizationMode.KeyTakeaways);
        _viewModel.AvailableModes.Should().Contain(SummarizationMode.Executive);
        _viewModel.AvailableModes.Should().Contain(SummarizationMode.Custom);
    }

    // ── LoadSummary ─────────────────────────────────────────────────────

    [Fact]
    public void LoadSummary_WithNullSummary_ThrowsArgumentNullException()
    {
        var act = () => _viewModel.LoadSummary(null!, null, "/path/to/doc.md");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("summary");
    }

    [Fact]
    public void LoadSummary_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        var summary = CreateTestSummary();

        var act = () => _viewModel.LoadSummary(summary, null, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public void LoadSummary_SetsDocumentProperties()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        _viewModel.LoadSummary(summary, null, "/path/to/document.md");

        // Assert
        _viewModel.DocumentPath.Should().Be("/path/to/document.md");
        _viewModel.DocumentName.Should().Be("document.md");
        _viewModel.Summary.Should().BeSameAs(summary);
        _viewModel.HasContent.Should().BeTrue();
    }

    [Fact]
    public void LoadSummary_SetsSelectedModeFromSummary()
    {
        // Arrange
        var summary = new SummarizationResult
        {
            Summary = "Test summary",
            Mode = SummarizationMode.Executive,
            Usage = UsageMetrics.Zero
        };

        // Act
        _viewModel.LoadSummary(summary, null, "/path/to/doc.md");

        // Assert
        _viewModel.SelectedMode.Should().Be(SummarizationMode.Executive);
    }

    [Fact]
    public void LoadSummary_ClearsErrorMessage()
    {
        // Arrange
        _viewModel.LoadSummary(CreateTestSummary(), null, "/path/to/doc.md");

        // Assert
        _viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void LoadSummary_SetsCompressionDisplay()
    {
        // Arrange
        var summary = new SummarizationResult
        {
            Summary = "Test",
            Mode = SummarizationMode.BulletPoints,
            OriginalWordCount = 1000,
            SummaryWordCount = 100,
            Usage = UsageMetrics.Zero
        };

        // Act
        _viewModel.LoadSummary(summary, null, "/path/to/doc.md");

        // Assert
        _viewModel.CompressionDisplay.Should().Contain("1000");
        _viewModel.CompressionDisplay.Should().Contain("100");
    }

    [Fact]
    public void LoadSummary_WithMetadata_SetsMetadataProperties()
    {
        // Arrange
        var summary = CreateTestSummary();
        var metadata = CreateTestMetadata();

        // Act
        _viewModel.LoadSummary(summary, metadata, "/path/to/doc.md");

        // Assert
        _viewModel.Metadata.Should().BeSameAs(metadata);
        _viewModel.HasMetadata.Should().BeTrue();
        _viewModel.ReadingTimeDisplay.Should().Contain("5");
        _viewModel.ComplexityDisplay.Should().Contain("6");
    }

    [Fact]
    public void LoadSummary_WithMetadata_LoadsKeyTerms()
    {
        // Arrange
        var summary = CreateTestSummary();
        var metadata = new DocumentMetadata
        {
            OneLiner = "Test",
            KeyTerms = new[]
            {
                KeyTerm.Create("api", 0.9),
                KeyTerm.Create("design", 0.8),
                KeyTerm.Create("patterns", 0.7)
            },
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            Usage = UsageMetrics.Zero
        };

        // Act
        _viewModel.LoadSummary(summary, metadata, "/path/to/doc.md");

        // Assert
        _viewModel.KeyTerms.Should().HaveCount(3);
        _viewModel.KeyTerms[0].Term.Should().Be("api");
        _viewModel.KeyTerms[1].Term.Should().Be("design");
        _viewModel.KeyTerms[2].Term.Should().Be("patterns");
    }

    [Fact]
    public void LoadSummary_LimitsKeyTermsToEight()
    {
        // Arrange
        var summary = CreateTestSummary();
        var keyTerms = Enumerable.Range(1, 15)
            .Select(i => KeyTerm.Create($"term{i}", 0.5))
            .ToArray();

        var metadata = new DocumentMetadata
        {
            OneLiner = "Test",
            KeyTerms = keyTerms,
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            Usage = UsageMetrics.Zero
        };

        // Act
        _viewModel.LoadSummary(summary, metadata, "/path/to/doc.md");

        // Assert
        _viewModel.KeyTerms.Should().HaveCount(8);
    }

    // ── Command CanExecute ──────────────────────────────────────────────

    [Fact]
    public void RefreshCommand_WhenNoDocumentPath_CannotExecute()
    {
        // Assert
        _viewModel.RefreshCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RefreshCommand_WithDocumentPathAndNotLoading_CanExecute()
    {
        // Arrange
        _viewModel.LoadSummary(CreateTestSummary(), null, "/path/to/doc.md");

        // Assert
        _viewModel.RefreshCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CopySummaryCommand_WhenNoSummary_CannotExecute()
    {
        // Assert
        _viewModel.CopySummaryCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CopySummaryCommand_WithSummary_CanExecute()
    {
        // Arrange
        _viewModel.LoadSummary(CreateTestSummary(), null, "/path/to/doc.md");

        // Assert
        _viewModel.CopySummaryCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void CopyKeyTermsCommand_WhenNoKeyTerms_CannotExecute()
    {
        // Assert
        _viewModel.CopyKeyTermsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CopyKeyTermsCommand_WithKeyTerms_CanExecute()
    {
        // Arrange
        var metadata = CreateTestMetadata();
        _viewModel.LoadSummary(CreateTestSummary(), metadata, "/path/to/doc.md");

        // Assert
        _viewModel.CopyKeyTermsCommand.CanExecute(null).Should().BeTrue();
    }

    // ── CloseCommand ────────────────────────────────────────────────────

    [Fact]
    public void CloseCommand_RaisesCloseRequestedEvent()
    {
        // Arrange
        var raised = false;
        _viewModel.CloseRequested += (_, _) => raised = true;

        // Act
        _viewModel.CloseCommand.Execute(null);

        // Assert
        raised.Should().BeTrue();
    }

    // ── CopySummaryCommand Execution ────────────────────────────────────

    [Fact]
    public async Task CopySummaryCommand_ExecutesExporter()
    {
        // Arrange
        var summary = CreateTestSummary();
        _viewModel.LoadSummary(summary, null, "/path/to/doc.md");

        _mockExporter
            .Setup(e => e.ExportAsync(
                summary,
                "/path/to/doc.md",
                It.IsAny<SummaryExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SummaryExportResult.Succeeded(ExportDestination.Clipboard));

        // Act
        await _viewModel.CopySummaryCommand.ExecuteAsync(null);

        // Assert
        _mockExporter.Verify(
            e => e.ExportAsync(
                summary,
                "/path/to/doc.md",
                It.Is<SummaryExportOptions>(o => o.Destination == ExportDestination.Clipboard),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── AddToFrontmatterCommand Execution ───────────────────────────────

    [Fact]
    public async Task AddToFrontmatterCommand_ExecutesExporter()
    {
        // Arrange
        var summary = CreateTestSummary();
        var metadata = CreateTestMetadata();
        _viewModel.LoadSummary(summary, metadata, "/path/to/doc.md");

        _mockExporter
            .Setup(e => e.UpdateFrontmatterAsync(
                "/path/to/doc.md",
                summary,
                metadata,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SummaryExportResult.Succeeded(ExportDestination.Frontmatter));

        // Act
        await _viewModel.AddToFrontmatterCommand.ExecuteAsync(null);

        // Assert
        _mockExporter.Verify(
            e => e.UpdateFrontmatterAsync(
                "/path/to/doc.md",
                summary,
                metadata,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ExportFileCommand Execution ─────────────────────────────────────

    [Fact]
    public async Task ExportFileCommand_ExecutesExporter()
    {
        // Arrange
        var summary = CreateTestSummary();
        _viewModel.LoadSummary(summary, null, "/path/to/doc.md");

        _mockExporter
            .Setup(e => e.ExportAsync(
                summary,
                "/path/to/doc.md",
                It.IsAny<SummaryExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SummaryExportResult.Succeeded(ExportDestination.File, "/path/to/doc.summary.md"));

        // Act
        await _viewModel.ExportFileCommand.ExecuteAsync(null);

        // Assert
        _mockExporter.Verify(
            e => e.ExportAsync(
                summary,
                "/path/to/doc.md",
                It.Is<SummaryExportOptions>(o => o.Destination == ExportDestination.File),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ClearCacheCommand Execution ─────────────────────────────────────

    [Fact]
    public async Task ClearCacheCommand_ExecutesExporter()
    {
        // Arrange
        _viewModel.LoadSummary(CreateTestSummary(), null, "/path/to/doc.md");

        // Act
        await _viewModel.ClearCacheCommand.ExecuteAsync(null);

        // Assert
        _mockExporter.Verify(
            e => e.ClearCacheAsync("/path/to/doc.md", It.IsAny<CancellationToken>()),
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
            GeneratedAt = DateTimeOffset.UtcNow,
            Model = "test-model",
            Usage = UsageMetrics.Zero
        };
    }

    private static DocumentMetadata CreateTestMetadata()
    {
        return new DocumentMetadata
        {
            OneLiner = "A test document for unit testing",
            KeyTerms = new[] { KeyTerm.Create("test", 0.8), KeyTerm.Create("unit", 0.7) },
            Concepts = new[] { "testing", "development" },
            SuggestedTags = new[] { "test", "unit", "dev" },
            EstimatedReadingMinutes = 5,
            ComplexityScore = 6,
            TargetAudience = "developers",
            PrimaryCategory = "Testing",
            Usage = UsageMetrics.Zero
        };
    }
}
