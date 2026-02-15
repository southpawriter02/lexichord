// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Agents.Simplifier.Events;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Simplifier;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

using BatchTextSelection = Lexichord.Abstractions.Agents.Simplifier.TextSelection;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="BatchSimplificationService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Test Coverage:</b>
/// <list type="bullet">
///   <item><description>Constructor null validation (7 dependencies)</description></item>
///   <item><description>SimplifyDocumentAsync: success, empty doc, all skipped, cancellation</description></item>
///   <item><description>Event publishing: completion, paragraph, cancellation</description></item>
///   <item><description>Undo group integration</description></item>
///   <item><description>Skip logic: headings, code blocks, already simple, too short</description></item>
///   <item><description>MaxParagraphs limit</description></item>
///   <item><description>License validation</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4d")]
public class BatchSimplificationServiceTests
{
    private readonly Mock<ISimplificationPipeline> _pipelineMock;
    private readonly Mock<IReadabilityService> _readabilityServiceMock;
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly ParagraphParser _paragraphParser;
    private readonly BatchSimplificationService _sut;

    private readonly ReadabilityTarget _defaultTarget;
    private readonly ReadabilityMetrics _complexMetrics;
    private readonly ReadabilityMetrics _simpleMetrics;

    public BatchSimplificationServiceTests()
    {
        _pipelineMock = new Mock<ISimplificationPipeline>();
        _readabilityServiceMock = new Mock<IReadabilityService>();
        _editorServiceMock = new Mock<IEditorService>();
        _mediatorMock = new Mock<IMediator>();
        _licenseContextMock = new Mock<ILicenseContext>();

        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();
        _paragraphParser = new ParagraphParser(parserLogger);

        // LOGIC: Default license to enabled for most tests
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
            .Returns(true);

        _sut = new BatchSimplificationService(
            _pipelineMock.Object,
            _readabilityServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            _paragraphParser,
            NullLogger<BatchSimplificationService>.Instance);

        // LOGIC: Create reusable test fixtures
        _defaultTarget = ReadabilityTarget.FromExplicit(8.0, 20, true);

        _complexMetrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 14.0,
            GunningFogIndex = 15.0,
            FleschReadingEase = 35,
            WordCount = 50,
            SentenceCount = 3,
            SyllableCount = 80,
            ComplexWordCount = 10
        };

        _simpleMetrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 6.0,
            GunningFogIndex = 7.0,
            FleschReadingEase = 75,
            WordCount = 45,
            SentenceCount = 4,
            SyllableCount = 60,
            ComplexWordCount = 3
        };
    }

    // ── Constructor Tests ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullPipeline_ThrowsArgumentNullException()
    {
        // Arrange
        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();

        // Act
        var act = () => new BatchSimplificationService(
            null!,
            _readabilityServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            new ParagraphParser(parserLogger),
            NullLogger<BatchSimplificationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipeline");
    }

    [Fact]
    public void Constructor_NullReadabilityService_ThrowsArgumentNullException()
    {
        // Arrange
        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();

        // Act
        var act = () => new BatchSimplificationService(
            _pipelineMock.Object,
            null!,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            new ParagraphParser(parserLogger),
            NullLogger<BatchSimplificationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readabilityService");
    }

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Arrange
        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();

        // Act
        var act = () => new BatchSimplificationService(
            _pipelineMock.Object,
            _readabilityServiceMock.Object,
            null!,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            new ParagraphParser(parserLogger),
            NullLogger<BatchSimplificationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();

        // Act
        var act = () => new BatchSimplificationService(
            _pipelineMock.Object,
            _readabilityServiceMock.Object,
            _editorServiceMock.Object,
            null!,
            _licenseContextMock.Object,
            new ParagraphParser(parserLogger),
            NullLogger<BatchSimplificationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Arrange
        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();

        // Act
        var act = () => new BatchSimplificationService(
            _pipelineMock.Object,
            _readabilityServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            null!,
            new ParagraphParser(parserLogger),
            NullLogger<BatchSimplificationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullParagraphParser_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new BatchSimplificationService(
            _pipelineMock.Object,
            _readabilityServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            null!,
            NullLogger<BatchSimplificationService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("paragraphParser");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var parserLogger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();

        // Act
        var act = () => new BatchSimplificationService(
            _pipelineMock.Object,
            _readabilityServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            new ParagraphParser(parserLogger),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── SimplifyDocumentAsync - Input Validation ──────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.SimplifyDocumentAsync(null!, _defaultTarget);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task SimplifyDocumentAsync_NullTarget_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.SimplifyDocumentAsync("/path/to/doc.md", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("target");
    }

    // ── SimplifyDocumentAsync - License Validation ────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_NoLicense_ReturnsFailedResult()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
            .Returns(false);

        // Act
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("WriterPro license");
    }

    // ── SimplifyDocumentAsync - Empty Document ────────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_EmptyDocument_ReturnsFailedResult()
    {
        // Arrange
        _editorServiceMock
            .Setup(e => e.GetDocumentText())
            .Returns(string.Empty);

        // Act
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task SimplifyDocumentAsync_NullDocument_ReturnsFailedResult()
    {
        // Arrange
        _editorServiceMock
            .Setup(e => e.GetDocumentText())
            .Returns((string?)null);

        // Act
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    // ── SimplifyDocumentAsync - Success Path ──────────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_SingleParagraph_ProcessesSuccessfully()
    {
        // Arrange
        const string documentContent = "This is a complex paragraph with sophisticated vocabulary that needs simplification.";
        const string simplifiedContent = "This is a simple paragraph with easy words.";

        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification(simplifiedContent);

        // Act
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalParagraphs.Should().Be(1);
        result.SimplifiedParagraphs.Should().Be(1);
        result.SkippedParagraphs.Should().Be(0);
        result.ParagraphResults.Should().HaveCount(1);
        result.ParagraphResults[0].WasSimplified.Should().BeTrue();
    }

    [Fact]
    public async Task SimplifyDocumentAsync_MultipleParagraphs_ProcessesAll()
    {
        // Arrange
        const string documentContent = """
            First paragraph with complex content that requires simplification.

            Second paragraph also with sophisticated vocabulary needing changes.

            Third paragraph is equally complex and verbose in its construction.
            """;
        const string simplifiedContent = "Simplified text.";

        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification(simplifiedContent);

        // Act
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalParagraphs.Should().Be(3);
        result.SimplifiedParagraphs.Should().Be(3);
        result.SkippedParagraphs.Should().Be(0);
    }

    [Fact]
    public async Task SimplifyDocumentAsync_BeginsAndEndsUndoGroup()
    {
        // Arrange
        const string documentContent = "A paragraph that will be simplified.";
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification("Simplified.");

        // Act
        await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        _editorServiceMock.Verify(e => e.BeginUndoGroup("Simplify Document"), Times.Once);
        _editorServiceMock.Verify(e => e.EndUndoGroup(), Times.Once);
    }

    // ── SimplifyDocumentAsync - Skip Logic ────────────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_AlreadySimple_SkipsWithReason()
    {
        // Arrange
        const string documentContent = "A simple paragraph.";
        var simpleOriginalMetrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 5.0, // Below target
            WordCount = 20,
            SentenceCount = 2
        };

        SetupDocumentContent(documentContent);
        _readabilityServiceMock
            .Setup(r => r.Analyze(It.IsAny<string>()))
            .Returns(simpleOriginalMetrics);

        // Act
        var options = new BatchSimplificationOptions { SkipAlreadySimple = true };
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, options);

        // Assert
        result.Success.Should().BeTrue();
        result.SkippedParagraphs.Should().Be(1);
        result.SimplifiedParagraphs.Should().Be(0);
        result.ParagraphResults[0].SkipReason.Should().Be(ParagraphSkipReason.AlreadySimple);
    }

    [Fact]
    public async Task SimplifyDocumentAsync_TooShort_SkipsWithReason()
    {
        // Arrange
        const string documentContent = "Short.";
        var shortMetrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 12.0,
            WordCount = 1, // Below minimum
            SentenceCount = 1
        };

        SetupDocumentContent(documentContent);
        _readabilityServiceMock
            .Setup(r => r.Analyze(It.IsAny<string>()))
            .Returns(shortMetrics);

        // Act
        var options = new BatchSimplificationOptions { MinParagraphWords = 5 };
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, options);

        // Assert
        result.ParagraphResults[0].SkipReason.Should().Be(ParagraphSkipReason.TooShort);
    }

    [Fact]
    public async Task SimplifyDocumentAsync_Heading_SkipsWhenConfigured()
    {
        // Arrange
        const string documentContent = "# This is a heading";
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);

        // Act
        var options = new BatchSimplificationOptions { SkipHeadings = true };
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, options);

        // Assert
        result.SkippedParagraphs.Should().Be(1);
        result.ParagraphResults[0].SkipReason.Should().Be(ParagraphSkipReason.IsHeading);
    }

    [Fact]
    public async Task SimplifyDocumentAsync_CodeBlock_SkipsWhenConfigured()
    {
        // Arrange
        const string documentContent = """
            ```csharp
            var x = 42;
            ```
            """;
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);

        // Act
        var options = new BatchSimplificationOptions { SkipCodeBlocks = true };
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, options);

        // Assert
        result.SkippedParagraphs.Should().Be(1);
        result.ParagraphResults[0].SkipReason.Should().Be(ParagraphSkipReason.IsCodeBlock);
    }

    // ── SimplifyDocumentAsync - MaxParagraphs Limit ───────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_MaxParagraphsLimit_StopsAtLimit()
    {
        // Arrange
        const string documentContent = """
            First paragraph content here.

            Second paragraph content here.

            Third paragraph content here.
            """;
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification("Simplified.");

        // Act
        var options = new BatchSimplificationOptions { MaxParagraphs = 2 };
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, options);

        // Assert
        result.TotalParagraphs.Should().Be(3);
        result.SimplifiedParagraphs.Should().Be(2);
        result.SkippedParagraphs.Should().Be(1);
        result.ParagraphResults.Last().SkipReason.Should().Be(ParagraphSkipReason.MaxParagraphsReached);
    }

    // ── SimplifyDocumentAsync - Cancellation ──────────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_CancellationRequested_ReturnsCancelledResult()
    {
        // Arrange
        const string documentContent = """
            First paragraph.

            Second paragraph.
            """;
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);

        var cts = new CancellationTokenSource();
        var callCount = 0;

        _pipelineMock
            .Setup(p => p.SimplifyAsync(It.IsAny<SimplificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount >= 1)
                {
                    cts.Cancel();
                    throw new OperationCanceledException();
                }

                return CreateSuccessSimplificationResult("Simplified");
            });

        // Act
        var result = await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, ct: cts.Token);

        // Assert
        result.WasCancelled.Should().BeTrue();
        result.Success.Should().BeFalse();
    }

    // ── SimplifyDocumentAsync - Event Publishing ──────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_Success_PublishesCompletionEvent()
    {
        // Arrange
        const string documentContent = "A paragraph to simplify.";
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification("Simplified.");

        SimplificationCompletedEvent? capturedEvent = null;
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<SimplificationCompletedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => capturedEvent = (SimplificationCompletedEvent)e)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.ParagraphsSimplified.Should().Be(1);
        capturedEvent.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task SimplifyDocumentAsync_PerParagraph_PublishesParagraphEvent()
    {
        // Arrange
        const string documentContent = "A paragraph to simplify.";
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification("Simplified.");

        var paragraphEvents = new List<ParagraphSimplifiedEvent>();
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<ParagraphSimplifiedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => paragraphEvents.Add((ParagraphSimplifiedEvent)e))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        paragraphEvents.Should().HaveCount(1);
        paragraphEvents[0].WasSimplified.Should().BeTrue();
        paragraphEvents[0].ParagraphIndex.Should().Be(0);
    }

    // ── SimplifyDocumentAsync - Progress Reporting ────────────────────────

    [Fact]
    public async Task SimplifyDocumentAsync_ReportsProgress()
    {
        // Arrange
        const string documentContent = """
            First paragraph content.

            Second paragraph content.
            """;
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification("Simplified.");

        var progressReports = new List<BatchSimplificationProgress>();
        var progress = new Progress<BatchSimplificationProgress>(p => progressReports.Add(p));

        // Act
        await _sut.SimplifyDocumentAsync("/path/to/doc.md", _defaultTarget, progress: progress);

        // LOGIC: Allow async progress reporting to complete
        await Task.Delay(50);

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.Should().Contain(p => p.Phase == BatchSimplificationPhase.AnalyzingDocument);
        progressReports.Should().Contain(p => p.Phase == BatchSimplificationPhase.ProcessingParagraphs);
    }

    // ── SimplifySelectionsAsync Tests ─────────────────────────────────────

    [Fact]
    public async Task SimplifySelectionsAsync_NullSelections_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.SimplifySelectionsAsync(null!, _defaultTarget);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("selections");
    }

    [Fact]
    public async Task SimplifySelectionsAsync_EmptySelections_ThrowsArgumentException()
    {
        // Arrange
        var selections = Array.Empty<BatchTextSelection>();

        // Act
        var act = () => _sut.SimplifySelectionsAsync(selections, _defaultTarget);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("selections");
    }

    [Fact]
    public async Task SimplifySelectionsAsync_NoLicense_ReturnsFailedResult()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
            .Returns(false);

        var selections = new[]
        {
            new BatchTextSelection("/path/to/doc.md", 0, 10, "Some text.")
        };

        // Act
        var result = await _sut.SimplifySelectionsAsync(selections, _defaultTarget);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("WriterPro license");
    }

    // ── SimplifyDocumentStreamingAsync Tests ──────────────────────────────

    [Fact]
    public async Task SimplifyDocumentStreamingAsync_NoLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
            .Returns(false);

        // Act
        var act = async () =>
        {
            await foreach (var _ in _sut.SimplifyDocumentStreamingAsync("/path/to/doc.md", _defaultTarget))
            {
                // Consume the stream
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*WriterPro license*");
    }

    [Fact]
    public async Task SimplifyDocumentStreamingAsync_EmptyDocument_YieldsNoResults()
    {
        // Arrange
        _editorServiceMock
            .Setup(e => e.GetDocumentText())
            .Returns(string.Empty);

        // Act
        var results = new List<ParagraphSimplificationResult>();
        await foreach (var result in _sut.SimplifyDocumentStreamingAsync("/path/to/doc.md", _defaultTarget))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SimplifyDocumentStreamingAsync_YieldsResultPerParagraph()
    {
        // Arrange
        const string documentContent = """
            First paragraph.

            Second paragraph.
            """;
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);
        SetupSuccessfulSimplification("Simplified.");

        // Act
        var results = new List<ParagraphSimplificationResult>();
        await foreach (var result in _sut.SimplifyDocumentStreamingAsync("/path/to/doc.md", _defaultTarget))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].ParagraphIndex.Should().Be(0);
        results[1].ParagraphIndex.Should().Be(1);
    }

    // ── EstimateCostAsync Tests ───────────────────────────────────────────

    [Fact]
    public async Task EstimateCostAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.EstimateCostAsync(null!, _defaultTarget);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task EstimateCostAsync_EmptyDocument_ReturnsEmptyEstimate()
    {
        // Arrange
        _editorServiceMock
            .Setup(e => e.GetDocumentText())
            .Returns(string.Empty);

        // Act
        var estimate = await _sut.EstimateCostAsync("/path/to/doc.md", _defaultTarget);

        // Assert
        estimate.EstimatedParagraphs.Should().Be(0);
        estimate.EstimatedSkipped.Should().Be(0);
    }

    [Fact]
    public async Task EstimateCostAsync_CalculatesCorrectCounts()
    {
        // Arrange
        const string documentContent = """
            First paragraph with enough words to process.

            # Heading to skip

            Third paragraph with enough words as well.
            """;
        SetupDocumentContent(documentContent);
        SetupMetrics(_complexMetrics);

        // Act
        var options = new BatchSimplificationOptions { SkipHeadings = true };
        var estimate = await _sut.EstimateCostAsync("/path/to/doc.md", _defaultTarget, options);

        // Assert
        estimate.EstimatedParagraphs.Should().Be(2);
        estimate.EstimatedSkipped.Should().Be(1);
    }

    // ── Helper Methods ────────────────────────────────────────────────────

    private void SetupDocumentContent(string content)
    {
        _editorServiceMock
            .Setup(e => e.GetDocumentText())
            .Returns(content);
    }

    private void SetupMetrics(ReadabilityMetrics metrics)
    {
        _readabilityServiceMock
            .Setup(r => r.Analyze(It.IsAny<string>()))
            .Returns(metrics);
    }

    private void SetupSuccessfulSimplification(string simplifiedText)
    {
        _pipelineMock
            .Setup(p => p.SimplifyAsync(It.IsAny<SimplificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessSimplificationResult(simplifiedText));
    }

    private SimplificationResult CreateSuccessSimplificationResult(string simplifiedText)
    {
        return new SimplificationResult
        {
            SimplifiedText = simplifiedText,
            OriginalMetrics = _complexMetrics,
            SimplifiedMetrics = _simpleMetrics,
            Changes = new[]
            {
                new SimplificationChange(
                    "complex vocabulary",
                    "simple words",
                    SimplificationChangeType.WordSimplification,
                    "Simplified vocabulary")
            },
            Glossary = null,
            TokenUsage = new UsageMetrics(100, 50, 0.003m),
            ProcessingTime = TimeSpan.FromSeconds(1),
            StrategyUsed = SimplificationStrategy.Balanced,
            TargetUsed = _defaultTarget,
            Success = true
        };
    }
}
