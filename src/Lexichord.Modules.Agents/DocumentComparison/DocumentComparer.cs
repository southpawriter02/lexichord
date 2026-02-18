// -----------------------------------------------------------------------
// <copyright file="DocumentComparer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implements document comparison with hybrid DiffPlex + LLM semantic
//   analysis (v0.7.6d). Provides semantic understanding of changes, categorization,
//   significance scoring, and natural language summaries.
//
//   Comparison flow (§5.1):
//     1. Validate inputs and options
//     2. Publish DocumentComparisonStartedEvent
//     3. Quick identical check (returns early if documents match)
//     4. Generate text diff via DiffPlex (fast, deterministic)
//     5. Invoke LLM with comparison prompt template
//     6. Parse JSON response into DocumentChange records
//     7. Filter changes by significance threshold
//     8. Filter formatting changes (if not included)
//     9. Order by significance (or group by section)
//     10. Identify related changes (if enabled)
//     11. Publish DocumentComparisonCompletedEvent
//     12. Return ComparisonResult
//
//   License gating:
//     - IDocumentComparer methods require WriterPro
//     - Feature code: FeatureCodes.DocumentComparison
//     - GetTextDiff() is available to all tiers (no LLM)
//
//   Error handling:
//     - OperationCanceledException from user CT → "Comparison cancelled"
//     - OperationCanceledException from timeout CTS → "Comparison timed out"
//     - Generic exceptions → logged and wrapped in failed ComparisonResult
//
//   Thread safety:
//     - All injected services are stateless or thread-safe
//     - No shared mutable state — all variables are per-invocation
//
//   Introduced in: v0.7.6d
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.DocumentComparison.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.DocumentComparison;

/// <summary>
/// Implements document comparison with hybrid DiffPlex + LLM semantic analysis.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DocumentComparer"/> provides semantic document version comparison
/// using a two-stage hybrid approach:
/// <list type="number">
/// <item><description><b>DiffPlex</b>: Fast, deterministic text diff for line-by-line changes</description></item>
/// <item><description><b>LLM</b>: Semantic analysis for categorization, significance scoring, and descriptions</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Comparison Methods:</b>
/// <list type="bullet">
/// <item><description><see cref="CompareAsync"/>: File-based comparison</description></item>
/// <item><description><see cref="CompareContentAsync"/>: Content-based comparison (core method)</description></item>
/// <item><description><see cref="CompareWithGitVersionAsync"/>: Git history comparison</description></item>
/// <item><description><see cref="GetTextDiff"/>: Pure text diff without LLM (available to all tiers)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// All semantic comparison methods require WriterPro tier via
/// <see cref="FeatureCodes.DocumentComparison"/>. The <see cref="GetTextDiff"/>
/// method is available to all tiers as it does not invoke the LLM.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d</para>
/// </remarks>
[RequiresLicense(LicenseTier.WriterPro, FeatureCode = FeatureCodes.DocumentComparison)]
public sealed class DocumentComparer : IDocumentComparer
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IChatCompletionService _chatService;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly IFileService _fileService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentComparer> _logger;

    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>
    /// Template ID for the document comparison prompt template.
    /// </summary>
    private const string TemplateId = "document-comparer";

    /// <summary>
    /// Default cost per 1,000 prompt tokens (USD).
    /// </summary>
    private const decimal DefaultPromptCostPer1K = 0.01m;

    /// <summary>
    /// Default cost per 1,000 completion tokens (USD).
    /// </summary>
    private const decimal DefaultCompletionCostPer1K = 0.03m;

    /// <summary>
    /// Timeout for the overall comparison operation.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);

    /// <summary>
    /// JSON serializer options for parsing LLM responses.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentComparer"/>.
    /// </summary>
    /// <param name="chatService">LLM chat completion service for semantic analysis.</param>
    /// <param name="promptRenderer">Template renderer for prompt assembly.</param>
    /// <param name="templateRepository">Repository for loading prompt templates.</param>
    /// <param name="fileService">File system service for reading document content.</param>
    /// <param name="licenseContext">License verification context.</param>
    /// <param name="mediator">MediatR mediator for publishing events.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public DocumentComparer(
        IChatCompletionService chatService,
        IPromptRenderer promptRenderer,
        IPromptTemplateRepository templateRepository,
        IFileService fileService,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<DocumentComparer> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── IDocumentComparer Implementation ─────────────────────────────────

    /// <inheritdoc />
    public async Task<ComparisonResult> CompareAsync(
        string originalPath,
        string newPath,
        ComparisonOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(originalPath);
        ArgumentNullException.ThrowIfNull(newPath);

        _logger.LogDebug(
            "CompareAsync: comparing files {OriginalPath} and {NewPath}",
            originalPath,
            newPath);

        // LOGIC: Load original document
        if (!_fileService.Exists(originalPath))
        {
            _logger.LogError("Original document not found: {OriginalPath}", originalPath);
            return ComparisonResult.Failed(originalPath, newPath, $"Original document not found: {originalPath}");
        }

        var originalLoadResult = await _fileService.LoadAsync(originalPath, cancellationToken: ct);
        if (!originalLoadResult.Success || string.IsNullOrEmpty(originalLoadResult.Content))
        {
            _logger.LogError("Failed to load original document: {Error}", originalLoadResult.Error?.Message);
            return ComparisonResult.Failed(
                originalPath,
                newPath,
                $"Failed to load original document: {originalLoadResult.Error?.Message ?? "Unknown error"}");
        }

        // LOGIC: Load new document
        if (!_fileService.Exists(newPath))
        {
            _logger.LogError("New document not found: {NewPath}", newPath);
            return ComparisonResult.Failed(originalPath, newPath, $"New document not found: {newPath}");
        }

        var newLoadResult = await _fileService.LoadAsync(newPath, cancellationToken: ct);
        if (!newLoadResult.Success || string.IsNullOrEmpty(newLoadResult.Content))
        {
            _logger.LogError("Failed to load new document: {Error}", newLoadResult.Error?.Message);
            return ComparisonResult.Failed(
                originalPath,
                newPath,
                $"Failed to load new document: {newLoadResult.Error?.Message ?? "Unknown error"}");
        }

        _logger.LogDebug(
            "Documents loaded: original={OriginalChars} chars, new={NewChars} chars",
            originalLoadResult.Content.Length,
            newLoadResult.Content.Length);

        // LOGIC: Delegate to content comparison
        var result = await CompareContentAsync(
            originalLoadResult.Content,
            newLoadResult.Content,
            options,
            ct);

        // LOGIC: Update result with file paths
        return result with
        {
            OriginalPath = originalPath,
            NewPath = newPath
        };
    }

    /// <inheritdoc />
    public async Task<ComparisonResult> CompareContentAsync(
        string originalContent,
        string newContent,
        ComparisonOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(originalContent);
        ArgumentNullException.ThrowIfNull(newContent);

        // LOGIC: Use default options if not provided
        options ??= ComparisonOptions.Default;
        options.Validate();

        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Calculate word counts for metrics
        var originalWordCount = CountWords(originalContent);
        var newWordCount = CountWords(newContent);

        _logger.LogDebug(
            "CompareContentAsync: original={OriginalWords} words, new={NewWords} words",
            originalWordCount,
            newWordCount);

        // LOGIC: Publish started event for observability
        await _mediator.Publish(
            DocumentComparisonStartedEvent.Create(
                originalPath: null,
                newPath: null,
                originalContent.Length,
                newContent.Length),
            ct);

        try
        {
            // 1. Quick identical check
            if (originalContent == newContent)
            {
                _logger.LogInformation("Documents are identical, returning empty result");

                stopwatch.Stop();

                await _mediator.Publish(
                    DocumentComparisonCompletedEvent.Create(
                        originalPath: null,
                        newPath: null,
                        changeCount: 0,
                        changeMagnitude: 0.0,
                        stopwatch.Elapsed),
                    ct);

                return ComparisonResult.Identical("[content]", "[content]", originalWordCount);
            }

            // 2. Generate text diff via DiffPlex (for context)
            var textDiff = options.IncludeTextDiff ? GetTextDiff(originalContent, newContent) : null;

            // 3. Get the prompt template
            var template = _templateRepository.GetTemplate(TemplateId);
            if (template is null)
            {
                _logger.LogError("Prompt template not found: {TemplateId}", TemplateId);

                await PublishFailedEventSafe(
                    originalPath: null,
                    newPath: null,
                    $"Prompt template '{TemplateId}' not found.");

                return ComparisonResult.Failed(
                    "[content]",
                    "[content]",
                    $"Prompt template '{TemplateId}' not found.",
                    originalWordCount,
                    newWordCount);
            }

            // 4. Build prompt variables
            var variables = BuildPromptVariables(originalContent, newContent, options, textDiff);

            // 5. Render prompt messages
            var messages = _promptRenderer.RenderMessages(template, variables);

            // 6. Configure LLM options (use Precise for consistent analysis)
            var chatOptions = ChatOptions.Precise
                .WithMaxTokens(options.MaxResponseTokens);

            // 7. Invoke LLM with timeout
            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var chatRequest = new ChatRequest(messages.ToImmutableArray(), chatOptions);
            var response = await _chatService.CompleteAsync(chatRequest, linkedCts.Token);

            _logger.LogDebug(
                "LLM response received: {ContentLength} chars, {PromptTokens} prompt tokens, {CompletionTokens} completion tokens",
                response.Content.Length,
                response.PromptTokens,
                response.CompletionTokens);

            // 8. Parse JSON response
            var (summary, changeMagnitude, changes, affectedSections) = ParseLlmResponse(
                response.Content,
                options);

            // 9. Calculate usage metrics
            var usage = UsageMetrics.Calculate(
                response.PromptTokens,
                response.CompletionTokens,
                DefaultPromptCostPer1K,
                DefaultCompletionCostPer1K);

            stopwatch.Stop();

            _logger.LogInformation(
                "Comparison completed: {ChangeCount} changes, magnitude={Magnitude:F2}, duration={Duration}ms",
                changes.Count,
                changeMagnitude,
                stopwatch.ElapsedMilliseconds);

            // 10. Publish completed event
            await _mediator.Publish(
                DocumentComparisonCompletedEvent.Create(
                    originalPath: null,
                    newPath: null,
                    changes.Count,
                    changeMagnitude,
                    stopwatch.Elapsed),
                ct);

            return new ComparisonResult
            {
                OriginalPath = "[content]",
                NewPath = "[content]",
                OriginalLabel = options.OriginalVersionLabel,
                NewLabel = options.NewVersionLabel,
                Summary = summary,
                Changes = changes,
                ChangeMagnitude = changeMagnitude,
                OriginalWordCount = originalWordCount,
                NewWordCount = newWordCount,
                AffectedSections = affectedSections,
                Usage = usage,
                ComparedAt = DateTimeOffset.UtcNow,
                Success = true,
                TextDiff = textDiff
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // LOGIC: User-initiated cancellation
            stopwatch.Stop();
            _logger.LogWarning("Comparison cancelled by user");

            await PublishFailedEventSafe(null, null, "Comparison cancelled by user.");

            return ComparisonResult.Failed(
                "[content]",
                "[content]",
                "Comparison cancelled by user.",
                originalWordCount,
                newWordCount);
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Timeout
            stopwatch.Stop();
            _logger.LogWarning("Comparison timed out after {Timeout}", DefaultTimeout);

            await PublishFailedEventSafe(
                null,
                null,
                $"Comparison timed out after {DefaultTimeout.TotalSeconds:F0} seconds.");

            return ComparisonResult.Failed(
                "[content]",
                "[content]",
                $"Comparison timed out after {DefaultTimeout.TotalSeconds:F0} seconds.",
                originalWordCount,
                newWordCount);
        }
        catch (Exception ex)
        {
            // LOGIC: Generic failure
            stopwatch.Stop();
            _logger.LogError(ex, "Comparison failed: {ErrorMessage}", ex.Message);

            await PublishFailedEventSafe(null, null, ex.Message);

            return ComparisonResult.Failed(
                "[content]",
                "[content]",
                ex.Message,
                originalWordCount,
                newWordCount);
        }
    }

    /// <inheritdoc />
    public async Task<ComparisonResult> CompareWithGitVersionAsync(
        string documentPath,
        string gitRef,
        ComparisonOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(gitRef);

        _logger.LogDebug(
            "CompareWithGitVersionAsync: {DocumentPath} at {GitRef}",
            documentPath,
            gitRef);

        try
        {
            // LOGIC: Load current document content
            if (!_fileService.Exists(documentPath))
            {
                _logger.LogError("Document not found: {DocumentPath}", documentPath);
                return ComparisonResult.Failed(
                    $"{gitRef}:{documentPath}",
                    documentPath,
                    $"Document not found: {documentPath}");
            }

            var currentLoadResult = await _fileService.LoadAsync(documentPath, cancellationToken: ct);
            if (!currentLoadResult.Success || string.IsNullOrEmpty(currentLoadResult.Content))
            {
                return ComparisonResult.Failed(
                    $"{gitRef}:{documentPath}",
                    documentPath,
                    $"Failed to load current document: {currentLoadResult.Error?.Message ?? "Unknown error"}");
            }

            // LOGIC: Get historical version via git show
            var gitContent = await GetGitVersionAsync(documentPath, gitRef, ct);
            if (gitContent is null)
            {
                return ComparisonResult.Failed(
                    $"{gitRef}:{documentPath}",
                    documentPath,
                    $"Failed to retrieve git version '{gitRef}' for '{documentPath}'");
            }

            // LOGIC: Set up options with version labels if not already set
            var effectiveOptions = options ?? ComparisonOptions.Default;
            if (effectiveOptions.OriginalVersionLabel is null || effectiveOptions.NewVersionLabel is null)
            {
                effectiveOptions = effectiveOptions.WithLabels(
                    effectiveOptions.OriginalVersionLabel ?? gitRef,
                    effectiveOptions.NewVersionLabel ?? "Current");
            }

            // LOGIC: Delegate to content comparison
            var result = await CompareContentAsync(
                gitContent,
                currentLoadResult.Content,
                effectiveOptions,
                ct);

            return result with
            {
                OriginalPath = $"{gitRef}:{documentPath}",
                NewPath = documentPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git comparison failed: {ErrorMessage}", ex.Message);

            await PublishFailedEventSafe(
                $"{gitRef}:{documentPath}",
                documentPath,
                ex.Message);

            return ComparisonResult.Failed(
                $"{gitRef}:{documentPath}",
                documentPath,
                $"Git comparison failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<string> GenerateChangeSummaryAsync(
        ComparisonResult comparison,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        // LOGIC: Return simple message for identical or failed results
        if (comparison.AreIdentical)
        {
            return Task.FromResult("No significant changes detected. The documents are identical.");
        }

        if (!comparison.Success)
        {
            return Task.FromResult($"Comparison failed: {comparison.ErrorMessage}");
        }

        // LOGIC: Build natural language summary from the result
        var sb = new StringBuilder();

        sb.Append(comparison.Summary);

        if (comparison.Changes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"Changes: {comparison.Changes.Count} total");
            sb.AppendLine($"  - Additions: {comparison.AdditionCount}");
            sb.AppendLine($"  - Deletions: {comparison.DeletionCount}");
            sb.AppendLine($"  - Modifications: {comparison.ModificationCount}");

            if (comparison.AffectedSections.Count > 0)
            {
                sb.AppendLine($"  - Affected sections: {string.Join(", ", comparison.AffectedSections)}");
            }
        }

        return Task.FromResult(sb.ToString());
    }

    /// <inheritdoc />
    public string GetTextDiff(string originalContent, string newContent)
    {
        ArgumentNullException.ThrowIfNull(originalContent);
        ArgumentNullException.ThrowIfNull(newContent);

        // LOGIC: Quick identical check
        if (originalContent == newContent)
        {
            return string.Empty;
        }

        // LOGIC: Use DiffPlex for text diff
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(originalContent, newContent);

        var sb = new StringBuilder();

        // LOGIC: Standard unified diff header
        sb.AppendLine("--- Original");
        sb.AppendLine("+++ New");
        sb.AppendLine("@@ @@");

        foreach (var line in diff.Lines)
        {
            var prefix = line.Type switch
            {
                ChangeType.Inserted => "+ ",
                ChangeType.Deleted => "- ",
                _ => "  "
            };

            sb.AppendLine($"{prefix}{line.Text}");
        }

        return sb.ToString();
    }

    // ── Private Methods: Prompt Construction ─────────────────────────────

    /// <summary>
    /// Builds prompt template variables from the content and options.
    /// </summary>
    private static Dictionary<string, object> BuildPromptVariables(
        string originalContent,
        string newContent,
        ComparisonOptions options,
        string? textDiff)
    {
        var variables = new Dictionary<string, object>
        {
            ["original_content"] = originalContent,
            ["new_content"] = newContent,
            ["max_changes"] = options.MaxChanges
        };

        // LOGIC: Add optional variables when present
        if (!string.IsNullOrEmpty(options.OriginalVersionLabel))
        {
            variables["original_label"] = options.OriginalVersionLabel;
        }

        if (!string.IsNullOrEmpty(options.NewVersionLabel))
        {
            variables["new_label"] = options.NewVersionLabel;
        }

        if (!string.IsNullOrEmpty(textDiff))
        {
            variables["text_diff"] = textDiff;
        }

        if (options.FocusSections is { Count: > 0 })
        {
            variables["focus_sections"] = string.Join(", ", options.FocusSections);
        }

        return variables;
    }

    // ── Private Methods: Response Parsing ────────────────────────────────

    /// <summary>
    /// Parses the LLM JSON response into structured change data.
    /// </summary>
    private (string Summary, double Magnitude, IReadOnlyList<DocumentChange> Changes, IReadOnlyList<string> AffectedSections) ParseLlmResponse(
        string responseContent,
        ComparisonOptions options)
    {
        // LOGIC: Extract JSON from response (may be wrapped in markdown code blocks)
        var json = ExtractJson(responseContent);

        try
        {
            var response = JsonSerializer.Deserialize<LlmComparisonResponse>(json, JsonOptions);

            if (response is null)
            {
                _logger.LogWarning("Failed to deserialize LLM response, using fallback");
                return (
                    "Unable to parse comparison response.",
                    0.5,
                    [],
                    []);
            }

            // LOGIC: Convert response to DocumentChange records
            var changes = response.Changes?
                .Select(c => new DocumentChange
                {
                    Category = ParseCategory(c.Category),
                    Section = c.Section,
                    Description = c.Description ?? "No description provided",
                    Significance = c.Significance,
                    OriginalText = c.OriginalText,
                    NewText = c.NewText,
                    Impact = c.Impact
                })
                .ToList() ?? [];

            // LOGIC: Filter by significance threshold
            changes = changes
                .Where(c => c.Significance >= options.SignificanceThreshold)
                .ToList();

            // LOGIC: Filter formatting changes if not included
            if (!options.IncludeFormattingChanges)
            {
                changes = changes
                    .Where(c => c.Category != ChangeCategory.Formatting)
                    .ToList();
            }

            // LOGIC: Order by significance or group by section
            if (options.GroupBySection)
            {
                changes = changes
                    .OrderBy(c => c.Section ?? string.Empty)
                    .ThenByDescending(c => c.Significance)
                    .ToList();
            }
            else
            {
                changes = changes
                    .OrderByDescending(c => c.Significance)
                    .ToList();
            }

            // LOGIC: Limit to max changes
            if (changes.Count > options.MaxChanges)
            {
                _logger.LogDebug(
                    "Truncating changes: {TotalChanges} detected, showing {MaxChanges}",
                    changes.Count,
                    options.MaxChanges);

                changes = changes.Take(options.MaxChanges).ToList();
            }

            return (
                response.Summary ?? "No summary provided.",
                response.ChangeMagnitude,
                changes.AsReadOnly(),
                response.AffectedSections?.AsReadOnly() ?? (IReadOnlyList<string>)[]);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response JSON");
            return (
                "Unable to parse comparison response.",
                0.5,
                [],
                []);
        }
    }

    /// <summary>
    /// Extracts JSON from a response that may be wrapped in markdown code blocks.
    /// </summary>
    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();

        // LOGIC: Check for ```json code block
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (endIndex > 7)
            {
                return trimmed[7..endIndex].Trim();
            }
        }

        // LOGIC: Check for generic ``` code block
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);

            if (firstNewline >= 0 && endIndex > firstNewline)
            {
                return trimmed[(firstNewline + 1)..endIndex].Trim();
            }
        }

        // LOGIC: Try to find JSON object directly
        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return trimmed[jsonStart..(jsonEnd + 1)];
        }

        return trimmed;
    }

    /// <summary>
    /// Parses a category string to <see cref="ChangeCategory"/> enum.
    /// </summary>
    private static ChangeCategory ParseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return ChangeCategory.Modified;
        }

        return category.ToLowerInvariant() switch
        {
            "added" => ChangeCategory.Added,
            "removed" => ChangeCategory.Removed,
            "modified" => ChangeCategory.Modified,
            "restructured" => ChangeCategory.Restructured,
            "clarified" => ChangeCategory.Clarified,
            "formatting" => ChangeCategory.Formatting,
            "correction" => ChangeCategory.Correction,
            "terminology" => ChangeCategory.Terminology,
            _ => ChangeCategory.Modified
        };
    }

    // ── Private Methods: Git Operations ──────────────────────────────────

    /// <summary>
    /// Retrieves historical content from git.
    /// </summary>
    private async Task<string?> GetGitVersionAsync(
        string documentPath,
        string gitRef,
        CancellationToken ct)
    {
        try
        {
            // LOGIC: Use git show to retrieve file content at the specified ref
            var relativePath = GetRelativePathToGitRoot(documentPath);
            if (relativePath is null)
            {
                _logger.LogError("Could not determine relative path for {DocumentPath}", documentPath);
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"show {gitRef}:{relativePath}",
                WorkingDirectory = Path.GetDirectoryName(documentPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "Git show failed: exit code {ExitCode}, error: {Error}",
                    process.ExitCode,
                    error);
                return null;
            }

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute git show for {GitRef}:{DocumentPath}", gitRef, documentPath);
            return null;
        }
    }

    /// <summary>
    /// Gets the relative path from git root for a file.
    /// </summary>
    private static string? GetRelativePathToGitRoot(string documentPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(documentPath);
            if (directory is null)
            {
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                return null;
            }

            // LOGIC: Calculate relative path from git root
            var fullPath = Path.GetFullPath(documentPath);
            var gitRoot = Path.GetFullPath(output);

            if (fullPath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = fullPath[(gitRoot.Length + 1)..];
                // LOGIC: Git uses forward slashes on all platforms
                return relativePath.Replace(Path.DirectorySeparatorChar, '/');
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ── Private Methods: Utilities ───────────────────────────────────────

    /// <summary>
    /// Counts words in a text string.
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Publishes a failed event without throwing if the publish fails.
    /// </summary>
    private async Task PublishFailedEventSafe(
        string? originalPath,
        string? newPath,
        string errorMessage)
    {
        try
        {
            await _mediator.Publish(
                DocumentComparisonFailedEvent.Create(originalPath, newPath, errorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish DocumentComparisonFailedEvent");
        }
    }

    // ── Private DTOs for JSON Parsing ────────────────────────────────────

    /// <summary>
    /// DTO for deserializing LLM comparison response.
    /// </summary>
    private sealed class LlmComparisonResponse
    {
        public string? Summary { get; set; }
        public double ChangeMagnitude { get; set; }
        public List<LlmChangeDto>? Changes { get; set; }
        public List<string>? AffectedSections { get; set; }
    }

    /// <summary>
    /// DTO for deserializing individual change from LLM response.
    /// </summary>
    private sealed class LlmChangeDto
    {
        public string? Category { get; set; }
        public string? Section { get; set; }
        public string? Description { get; set; }
        public double Significance { get; set; }
        public string? OriginalText { get; set; }
        public string? NewText { get; set; }
        public string? Impact { get; set; }
    }
}
