// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Agents.Simplifier.Events;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;

using MediatR;

using Microsoft.Extensions.Logging;

using BatchTextSelection = Lexichord.Abstractions.Agents.Simplifier.TextSelection;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Processes entire documents through the simplification pipeline paragraph by paragraph.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationService"/> orchestrates document-wide
/// simplification by:
/// </para>
/// <list type="number">
///   <item><description>Parsing the document into paragraphs via <see cref="ParagraphParser"/></description></item>
///   <item><description>Evaluating skip conditions for each paragraph</description></item>
///   <item><description>Processing eligible paragraphs through <see cref="ISimplificationPipeline"/></description></item>
///   <item><description>Applying changes atomically with undo support</description></item>
///   <item><description>Publishing events for progress tracking and analytics</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b>
/// This service is stateless and thread-safe. Concurrent operations on different documents
/// are fully supported. Concurrent operations on the same document should be avoided.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
internal sealed class BatchSimplificationService : IBatchSimplificationService
{
    // LOGIC: Constants for estimation and preview truncation
    private const int PreviewLength = 50;
    private const double TokensPerWord = 1.3;
    private const double SecondsPerParagraph = 2.5;
    private const decimal TokenCostUsd = 0.00001m;

    private readonly ISimplificationPipeline _pipeline;
    private readonly IReadabilityService _readabilityService;
    private readonly IEditorService _editorService;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly ParagraphParser _paragraphParser;
    private readonly ILogger<BatchSimplificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchSimplificationService"/> class.
    /// </summary>
    /// <param name="pipeline">The simplification pipeline for individual paragraphs.</param>
    /// <param name="readabilityService">Service for calculating readability metrics.</param>
    /// <param name="editorService">Service for document access and modification.</param>
    /// <param name="mediator">MediatR for event publishing.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="paragraphParser">Parser for document paragraph extraction.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public BatchSimplificationService(
        ISimplificationPipeline pipeline,
        IReadabilityService readabilityService,
        IEditorService editorService,
        IMediator mediator,
        ILicenseContext licenseContext,
        ParagraphParser paragraphParser,
        ILogger<BatchSimplificationService> logger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _readabilityService = readabilityService ?? throw new ArgumentNullException(nameof(readabilityService));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _paragraphParser = paragraphParser ?? throw new ArgumentNullException(nameof(paragraphParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<BatchSimplificationResult> SimplifyDocumentAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        IProgress<BatchSimplificationProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(target);

        // LOGIC: Validate license
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
        {
            _logger.LogWarning("Batch simplification requires WriterPro license");
            return BatchSimplificationResult.Failed(
                documentPath,
                "Batch simplification requires WriterPro license tier.",
                TimeSpan.Zero);
        }

        options ??= new BatchSimplificationOptions();
        var stopwatch = Stopwatch.StartNew();
        var paragraphResults = new List<ParagraphSimplificationResult>();
        var totalTokenUsage = UsageMetrics.Zero;
        var glossaryTerms = new Dictionary<string, string>();

        _logger.LogInformation(
            "Batch simplification started: {DocumentPath}",
            documentPath);

        // LOGIC: Read document content
        var content = _editorService.GetDocumentText();
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("Document content is empty: {DocumentPath}", documentPath);
            return BatchSimplificationResult.Failed(
                documentPath,
                "Document content is empty.",
                stopwatch.Elapsed);
        }

        // LOGIC: Parse document into paragraphs
        var paragraphs = _paragraphParser.Parse(content);

        _logger.LogInformation(
            "Batch simplification: {ParagraphCount} paragraphs found in {DocumentPath}",
            paragraphs.Count, documentPath);

        if (paragraphs.Count == 0)
        {
            return BatchSimplificationResult.Failed(
                documentPath,
                "No paragraphs found in document.",
                stopwatch.Elapsed);
        }

        // LOGIC: Calculate original document metrics
        var originalDocMetrics = _readabilityService.Analyze(content);

        // LOGIC: Report initial progress
        progress?.Report(BatchSimplificationProgress.Analyzing(
            paragraphs.Count,
            TimeSpan.FromSeconds(paragraphs.Count * SecondsPerParagraph)));

        // LOGIC: Begin undo group for atomic reversion
        _editorService.BeginUndoGroup("Simplify Document");

        int simplified = 0;
        int skipped = 0;
        var paragraphTimes = new List<TimeSpan>();

        try
        {
            for (var i = 0; i < paragraphs.Count; i++)
            {
                // LOGIC: Check cancellation
                ct.ThrowIfCancellationRequested();

                // LOGIC: Check MaxParagraphs limit
                if (options.MaxParagraphs.HasValue && i >= options.MaxParagraphs.Value)
                {
                    _logger.LogDebug(
                        "Max paragraphs limit reached at {Index}/{Total}",
                        i, paragraphs.Count);

                    // LOGIC: Mark remaining paragraphs as skipped
                    for (var j = i; j < paragraphs.Count; j++)
                    {
                        var remainingParagraph = paragraphs[j];
                        var remainingMetrics = _readabilityService.Analyze(remainingParagraph.Text);

                        paragraphResults.Add(ParagraphSimplificationResult.Skipped(
                            j,
                            remainingParagraph.Text,
                            remainingParagraph.StartOffset,
                            remainingParagraph.EndOffset,
                            remainingMetrics,
                            ParagraphSkipReason.MaxParagraphsReached));

                        skipped++;
                    }

                    break;
                }

                var paragraph = paragraphs[i];
                var paragraphStopwatch = Stopwatch.StartNew();

                // LOGIC: Calculate paragraph metrics
                var paragraphMetrics = _readabilityService.Analyze(paragraph.Text);

                // LOGIC: Evaluate skip conditions
                var skipReason = EvaluateSkipReason(paragraph, paragraphMetrics, target, options);

                if (skipReason != ParagraphSkipReason.None)
                {
                    // LOGIC: Skip this paragraph
                    _logger.LogDebug(
                        "Skipping paragraph {Index}: {SkipReason}",
                        i, skipReason);

                    paragraphResults.Add(ParagraphSimplificationResult.Skipped(
                        i,
                        paragraph.Text,
                        paragraph.StartOffset,
                        paragraph.EndOffset,
                        paragraphMetrics,
                        skipReason));

                    skipped++;

                    // LOGIC: Publish skip event
                    await _mediator.Publish(new ParagraphSimplifiedEvent(
                        documentPath,
                        i,
                        paragraphs.Count,
                        paragraphMetrics.FleschKincaidGradeLevel,
                        paragraphMetrics.FleschKincaidGradeLevel,
                        WasSimplified: false,
                        skipReason), ct);
                }
                else
                {
                    // LOGIC: Simplify this paragraph
                    _logger.LogDebug(
                        "Processing paragraph {Index}/{Total}: {Preview}",
                        i, paragraphs.Count, paragraph.TextPreview);

                    var request = new SimplificationRequest
                    {
                        OriginalText = paragraph.Text,
                        Target = target,
                        DocumentPath = documentPath,
                        Strategy = options.Strategy,
                        GenerateGlossary = options.GenerateGlossary
                    };

                    var result = await _pipeline.SimplifyAsync(request, ct);

                    if (result.Success)
                    {
                        paragraphResults.Add(ParagraphSimplificationResult.Simplified(
                            i,
                            paragraph.Text,
                            result.SimplifiedText,
                            paragraph.StartOffset,
                            paragraph.EndOffset,
                            result.OriginalMetrics,
                            result.SimplifiedMetrics,
                            result.Changes,
                            result.TokenUsage,
                            paragraphStopwatch.Elapsed));

                        totalTokenUsage = totalTokenUsage.Add(result.TokenUsage);
                        simplified++;

                        _logger.LogDebug(
                            "Simplified paragraph {Index}: Grade {Before:F1} → {After:F1}",
                            i,
                            result.OriginalMetrics.FleschKincaidGradeLevel,
                            result.SimplifiedMetrics.FleschKincaidGradeLevel);

                        // LOGIC: Aggregate glossary entries
                        if (result.Glossary != null)
                        {
                            foreach (var (term, definition) in result.Glossary)
                            {
                                glossaryTerms.TryAdd(term, definition);
                            }
                        }

                        // LOGIC: Publish success event
                        await _mediator.Publish(new ParagraphSimplifiedEvent(
                            documentPath,
                            i,
                            paragraphs.Count,
                            result.OriginalMetrics.FleschKincaidGradeLevel,
                            result.SimplifiedMetrics.FleschKincaidGradeLevel,
                            WasSimplified: true,
                            ParagraphSkipReason.None), ct);
                    }
                    else
                    {
                        // LOGIC: Pipeline failed, mark as skipped with ProcessingFailed
                        _logger.LogWarning(
                            "Simplification failed for paragraph {Index}: {Error}",
                            i, result.ErrorMessage);

                        paragraphResults.Add(ParagraphSimplificationResult.Skipped(
                            i,
                            paragraph.Text,
                            paragraph.StartOffset,
                            paragraph.EndOffset,
                            paragraphMetrics,
                            ParagraphSkipReason.ProcessingFailed));

                        skipped++;

                        await _mediator.Publish(new ParagraphSimplifiedEvent(
                            documentPath,
                            i,
                            paragraphs.Count,
                            paragraphMetrics.FleschKincaidGradeLevel,
                            paragraphMetrics.FleschKincaidGradeLevel,
                            WasSimplified: false,
                            ParagraphSkipReason.ProcessingFailed), ct);
                    }
                }

                paragraphTimes.Add(paragraphStopwatch.Elapsed);

                // LOGIC: Report progress
                var avgTime = paragraphTimes.Count > 0
                    ? TimeSpan.FromTicks(paragraphTimes.Sum(t => t.Ticks) / paragraphTimes.Count)
                    : TimeSpan.FromSeconds(SecondsPerParagraph);

                progress?.Report(new BatchSimplificationProgress
                {
                    CurrentParagraph = i + 1,
                    TotalParagraphs = paragraphs.Count,
                    CurrentParagraphPreview = TruncateText(paragraph.Text, PreviewLength),
                    PercentComplete = (double)(i + 1) / paragraphs.Count * 100,
                    EstimatedTimeRemaining = TimeSpan.FromTicks(avgTime.Ticks * (paragraphs.Count - i - 1)),
                    SimplifiedSoFar = simplified,
                    SkippedSoFar = skipped,
                    StatusMessage = $"Processing paragraph {i + 1} of {paragraphs.Count}...",
                    Phase = BatchSimplificationPhase.ProcessingParagraphs
                });

                // LOGIC: Optional delay between paragraphs
                if (options.DelayBetweenParagraphs > TimeSpan.Zero && i < paragraphs.Count - 1)
                {
                    await Task.Delay(options.DelayBetweenParagraphs, ct);
                }
            }

            // LOGIC: Apply changes to document in reverse offset order
            ApplyChangesToDocument(content, paragraphs, paragraphResults);

            // LOGIC: Calculate simplified document metrics
            var simplifiedContent = _editorService.GetDocumentText() ?? content;
            var simplifiedDocMetrics = _readabilityService.Analyze(simplifiedContent);

            stopwatch.Stop();

            var batchResult = new BatchSimplificationResult
            {
                DocumentPath = documentPath,
                TotalParagraphs = paragraphs.Count,
                ProcessedParagraphs = paragraphs.Count,
                SimplifiedParagraphs = simplified,
                SkippedParagraphs = skipped,
                OriginalDocumentMetrics = originalDocMetrics,
                SimplifiedDocumentMetrics = simplifiedDocMetrics,
                ParagraphResults = paragraphResults,
                TotalTokenUsage = totalTokenUsage,
                TotalProcessingTime = stopwatch.Elapsed,
                WasCancelled = false,
                AggregateGlossary = glossaryTerms.Count > 0 ? glossaryTerms : null
            };

            // LOGIC: Publish completion event
            await _mediator.Publish(new SimplificationCompletedEvent(
                documentPath,
                simplified,
                skipped,
                paragraphs.Count,
                batchResult.GradeLevelReduction,
                stopwatch.Elapsed,
                totalTokenUsage,
                WasCancelled: false), ct);

            // LOGIC: Report completion
            progress?.Report(BatchSimplificationProgress.Completed(
                paragraphs.Count, simplified, skipped));

            _logger.LogInformation(
                "Batch simplification completed: {Simplified}/{Total} paragraphs in {ElapsedMs}ms",
                simplified, paragraphs.Count, stopwatch.ElapsedMilliseconds);

            return batchResult;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Handle cancellation
            _logger.LogWarning(
                "Batch simplification cancelled at paragraph {Index}/{Total}",
                paragraphResults.Count, paragraphs.Count);

            // LOGIC: Publish cancellation event
            await _mediator.Publish(BatchSimplificationCancelledEvent.UserCancelled(
                documentPath,
                paragraphResults.Count,
                paragraphs.Count), CancellationToken.None);

            progress?.Report(BatchSimplificationProgress.Cancelled(
                paragraphResults.Count, paragraphs.Count, simplified, skipped));

            return BatchSimplificationResult.Cancelled(
                documentPath,
                paragraphs.Count,
                paragraphResults.Count,
                simplified,
                skipped,
                paragraphResults,
                originalDocMetrics,
                totalTokenUsage,
                stopwatch.Elapsed);
        }
        finally
        {
            // LOGIC: Always end undo group
            _editorService.EndUndoGroup();
        }
    }

    /// <inheritdoc/>
    public async Task<BatchSimplificationResult> SimplifySelectionsAsync(
        IReadOnlyList<BatchTextSelection> selections,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        IProgress<BatchSimplificationProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(target);

        if (selections.Count == 0)
        {
            throw new ArgumentException("At least one selection is required.", nameof(selections));
        }

        // LOGIC: Validate license - return failed result if not licensed
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
        {
            _logger.LogWarning("Batch simplification of selections requires WriterPro license");
            var documentPathFromSelection = selections.FirstOrDefault()?.DocumentPath ?? "unknown";
            return BatchSimplificationResult.Failed(
                documentPathFromSelection,
                "Batch simplification requires WriterPro license tier.",
                TimeSpan.Zero);
        }

        // LOGIC: Validate selections
        foreach (var selection in selections)
        {
            selection.Validate();
        }

        // LOGIC: Check for overlapping selections
        var sortedSelections = selections.OrderBy(s => s.StartOffset).ToList();
        for (var i = 1; i < sortedSelections.Count; i++)
        {
            if (sortedSelections[i].StartOffset < sortedSelections[i - 1].EndOffset)
            {
                throw new ArgumentException(
                    "Selections cannot overlap.",
                    nameof(selections));
            }
        }

        var documentPath = selections[0].DocumentPath;
        var stopwatch = Stopwatch.StartNew();
        var paragraphResults = new List<ParagraphSimplificationResult>();
        var totalTokenUsage = UsageMetrics.Zero;

        options ??= new BatchSimplificationOptions();

        _logger.LogInformation(
            "Batch simplification of {Count} selections in {DocumentPath}",
            selections.Count, documentPath);

        // LOGIC: Calculate original metrics from all selections
        var combinedOriginalText = string.Join("\n\n", selections.Select(s => s.Text));
        var originalDocMetrics = _readabilityService.Analyze(combinedOriginalText);

        progress?.Report(BatchSimplificationProgress.Analyzing(
            selections.Count,
            TimeSpan.FromSeconds(selections.Count * SecondsPerParagraph)));

        _editorService.BeginUndoGroup("Simplify Selections");

        try
        {
            // LOGIC: Process in reverse order to preserve offsets
            var reversedSelections = sortedSelections.AsEnumerable().Reverse().ToList();

            for (var i = 0; i < reversedSelections.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var selection = reversedSelections[i];
                var originalIndex = sortedSelections.IndexOf(selection);
                var paragraphStopwatch = Stopwatch.StartNew();

                var metrics = _readabilityService.Analyze(selection.Text);

                var request = new SimplificationRequest
                {
                    OriginalText = selection.Text,
                    Target = target,
                    DocumentPath = documentPath,
                    Strategy = options.Strategy,
                    GenerateGlossary = options.GenerateGlossary
                };

                var result = await _pipeline.SimplifyAsync(request, ct);

                if (result.Success && result.SimplifiedText != selection.Text)
                {
                    // LOGIC: Apply change to document
                    _editorService.DeleteText(selection.StartOffset, selection.Length);
                    _editorService.InsertText(selection.StartOffset, result.SimplifiedText);

                    paragraphResults.Add(ParagraphSimplificationResult.Simplified(
                        originalIndex,
                        selection.Text,
                        result.SimplifiedText,
                        selection.StartOffset,
                        selection.EndOffset,
                        result.OriginalMetrics,
                        result.SimplifiedMetrics,
                        result.Changes,
                        result.TokenUsage,
                        paragraphStopwatch.Elapsed));

                    totalTokenUsage = totalTokenUsage.Add(result.TokenUsage);
                }
                else
                {
                    paragraphResults.Add(ParagraphSimplificationResult.Skipped(
                        originalIndex,
                        selection.Text,
                        selection.StartOffset,
                        selection.EndOffset,
                        metrics,
                        result.Success ? ParagraphSkipReason.AlreadySimple : ParagraphSkipReason.ProcessingFailed));
                }

                progress?.Report(new BatchSimplificationProgress
                {
                    CurrentParagraph = i + 1,
                    TotalParagraphs = selections.Count,
                    CurrentParagraphPreview = selection.TextPreview,
                    PercentComplete = (double)(i + 1) / selections.Count * 100,
                    EstimatedTimeRemaining = TimeSpan.FromSeconds((selections.Count - i - 1) * SecondsPerParagraph),
                    SimplifiedSoFar = paragraphResults.Count(r => r.WasSimplified),
                    SkippedSoFar = paragraphResults.Count(r => !r.WasSimplified),
                    StatusMessage = $"Processing selection {i + 1} of {selections.Count}...",
                    Phase = BatchSimplificationPhase.ProcessingParagraphs
                });
            }

            // LOGIC: Reorder results by original index
            paragraphResults = paragraphResults.OrderBy(r => r.ParagraphIndex).ToList();

            var simplified = paragraphResults.Count(r => r.WasSimplified);
            var skipped = paragraphResults.Count(r => !r.WasSimplified);

            var simplifiedContent = _editorService.GetDocumentText() ?? combinedOriginalText;
            var simplifiedDocMetrics = _readabilityService.Analyze(simplifiedContent);

            stopwatch.Stop();

            progress?.Report(BatchSimplificationProgress.Completed(
                selections.Count, simplified, skipped));

            return new BatchSimplificationResult
            {
                DocumentPath = documentPath,
                TotalParagraphs = selections.Count,
                ProcessedParagraphs = selections.Count,
                SimplifiedParagraphs = simplified,
                SkippedParagraphs = skipped,
                OriginalDocumentMetrics = originalDocMetrics,
                SimplifiedDocumentMetrics = simplifiedDocMetrics,
                ParagraphResults = paragraphResults,
                TotalTokenUsage = totalTokenUsage,
                TotalProcessingTime = stopwatch.Elapsed,
                WasCancelled = false
            };
        }
        catch (OperationCanceledException)
        {
            var simplified = paragraphResults.Count(r => r.WasSimplified);
            var skipped = paragraphResults.Count(r => !r.WasSimplified);

            return BatchSimplificationResult.Cancelled(
                documentPath,
                selections.Count,
                paragraphResults.Count,
                simplified,
                skipped,
                paragraphResults.OrderBy(r => r.ParagraphIndex).ToList(),
                originalDocMetrics,
                totalTokenUsage,
                stopwatch.Elapsed);
        }
        finally
        {
            _editorService.EndUndoGroup();
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ParagraphSimplificationResult> SimplifyDocumentStreamingAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(target);

        // LOGIC: Validate license - throw InvalidOperationException for streaming method
        // Note: Streaming methods cannot return a result, so we throw instead.
        // Callers should check license before calling this method.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SimplifierAgent))
        {
            _logger.LogWarning("Streaming batch simplification requires WriterPro license");
            throw new InvalidOperationException(
                "Batch simplification requires WriterPro license tier.");
        }

        options ??= new BatchSimplificationOptions();

        var content = _editorService.GetDocumentText();
        if (string.IsNullOrEmpty(content))
        {
            yield break;
        }

        var paragraphs = _paragraphParser.Parse(content);

        for (var i = 0; i < paragraphs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Check MaxParagraphs limit
            if (options.MaxParagraphs.HasValue && i >= options.MaxParagraphs.Value)
            {
                yield break;
            }

            var paragraph = paragraphs[i];
            var metrics = _readabilityService.Analyze(paragraph.Text);

            var skipReason = EvaluateSkipReason(paragraph, metrics, target, options);

            if (skipReason != ParagraphSkipReason.None)
            {
                yield return ParagraphSimplificationResult.Skipped(
                    i,
                    paragraph.Text,
                    paragraph.StartOffset,
                    paragraph.EndOffset,
                    metrics,
                    skipReason);
            }
            else
            {
                var request = new SimplificationRequest
                {
                    OriginalText = paragraph.Text,
                    Target = target,
                    DocumentPath = documentPath,
                    Strategy = options.Strategy,
                    GenerateGlossary = options.GenerateGlossary
                };

                var result = await _pipeline.SimplifyAsync(request, ct);

                if (result.Success)
                {
                    yield return ParagraphSimplificationResult.Simplified(
                        i,
                        paragraph.Text,
                        result.SimplifiedText,
                        paragraph.StartOffset,
                        paragraph.EndOffset,
                        result.OriginalMetrics,
                        result.SimplifiedMetrics,
                        result.Changes,
                        result.TokenUsage,
                        TimeSpan.Zero);
                }
                else
                {
                    yield return ParagraphSimplificationResult.Skipped(
                        i,
                        paragraph.Text,
                        paragraph.StartOffset,
                        paragraph.EndOffset,
                        metrics,
                        ParagraphSkipReason.ProcessingFailed);
                }
            }

            // LOGIC: Optional delay
            if (options.DelayBetweenParagraphs > TimeSpan.Zero && i < paragraphs.Count - 1)
            {
                await Task.Delay(options.DelayBetweenParagraphs, ct);
            }
        }
    }

    /// <inheritdoc/>
    public Task<BatchSimplificationEstimate> EstimateCostAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(target);

        options ??= new BatchSimplificationOptions();

        var content = _editorService.GetDocumentText();
        if (string.IsNullOrEmpty(content))
        {
            return Task.FromResult(BatchSimplificationEstimate.Empty);
        }

        var paragraphs = _paragraphParser.Parse(content);

        var toProcess = 0;
        var toSkip = 0;
        var totalWords = 0;

        foreach (var paragraph in paragraphs)
        {
            ct.ThrowIfCancellationRequested();

            var metrics = _readabilityService.Analyze(paragraph.Text);
            var skipReason = EvaluateSkipReason(paragraph, metrics, target, options);

            if (skipReason != ParagraphSkipReason.None)
            {
                toSkip++;
            }
            else
            {
                toProcess++;
                totalWords += metrics.WordCount;
            }
        }

        // LOGIC: Apply MaxParagraphs limit to estimate
        if (options.MaxParagraphs.HasValue && toProcess > options.MaxParagraphs.Value)
        {
            toSkip += toProcess - options.MaxParagraphs.Value;
            toProcess = options.MaxParagraphs.Value;
        }

        return Task.FromResult(BatchSimplificationEstimate.FromAnalysis(
            toProcess,
            toSkip,
            totalWords,
            TokenCostUsd));
    }

    // ── Private Methods ─────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates skip conditions for a paragraph.
    /// </summary>
    /// <param name="paragraph">The parsed paragraph.</param>
    /// <param name="metrics">Pre-calculated readability metrics.</param>
    /// <param name="target">The readability target.</param>
    /// <param name="options">Batch processing options.</param>
    /// <returns>The skip reason, or <see cref="ParagraphSkipReason.None"/> if should process.</returns>
    private static ParagraphSkipReason EvaluateSkipReason(
        ParsedParagraph paragraph,
        ReadabilityMetrics metrics,
        ReadabilityTarget target,
        BatchSimplificationOptions options)
    {
        // LOGIC: Check word count
        if (metrics.WordCount < options.MinParagraphWords)
        {
            return ParagraphSkipReason.TooShort;
        }

        // LOGIC: Check structural elements
        if (options.SkipHeadings && paragraph.Type == ParagraphType.Heading)
        {
            return ParagraphSkipReason.IsHeading;
        }

        if (options.SkipCodeBlocks && paragraph.Type == ParagraphType.CodeBlock)
        {
            return ParagraphSkipReason.IsCodeBlock;
        }

        if (options.SkipBlockquotes && paragraph.Type == ParagraphType.Blockquote)
        {
            return ParagraphSkipReason.IsBlockquote;
        }

        if (options.SkipListItems && paragraph.Type == ParagraphType.ListItem)
        {
            return ParagraphSkipReason.IsListItem;
        }

        // LOGIC: Check if already simple
        if (options.SkipAlreadySimple &&
            metrics.FleschKincaidGradeLevel <= target.TargetGradeLevel + options.GradeLevelTolerance)
        {
            return ParagraphSkipReason.AlreadySimple;
        }

        return ParagraphSkipReason.None;
    }

    /// <summary>
    /// Applies paragraph changes to the document in reverse offset order.
    /// </summary>
    /// <param name="originalContent">Original document content (for validation).</param>
    /// <param name="paragraphs">Parsed paragraphs.</param>
    /// <param name="results">Simplification results.</param>
    private void ApplyChangesToDocument(
        string originalContent,
        IReadOnlyList<ParsedParagraph> paragraphs,
        IReadOnlyList<ParagraphSimplificationResult> results)
    {
        // LOGIC: Process in reverse order to preserve offsets
        var changedResults = results
            .Where(r => r.WasSimplified && r.TextChanged)
            .OrderByDescending(r => r.StartOffset)
            .ToList();

        foreach (var result in changedResults)
        {
            _editorService.DeleteText(result.StartOffset, result.EndOffset - result.StartOffset);
            _editorService.InsertText(result.StartOffset, result.SimplifiedText);

            _logger.LogTrace(
                "Applied change at offset {Offset}: {Length} chars → {NewLength} chars",
                result.StartOffset,
                result.EndOffset - result.StartOffset,
                result.SimplifiedText.Length);
        }

        _logger.LogDebug(
            "Applied {Count} changes to document",
            changedResults.Count);
    }

    /// <summary>
    /// Truncates text for preview display.
    /// </summary>
    /// <param name="text">Text to truncate.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Truncated text with "..." if needed.</returns>
    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text.Replace('\n', ' ').Replace('\r', ' ');
        }

        return text[..(maxLength - 3)].Replace('\n', ' ').Replace('\r', ' ') + "...";
    }
}
