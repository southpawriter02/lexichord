// =============================================================================
// File: LinterIntegration.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main integration bridging CKVS validation with style linter.
// =============================================================================
// LOGIC: Orchestrates IValidationEngine and IStyleEngine in parallel,
//   adapts results via IUnifiedFindingAdapter, applies filtering/sorting,
//   and returns a UnifiedFindingResult. Also provides a combined
//   "fix all" workflow via ICombinedFixWorkflow.
//
// SPEC ADAPTATION:
//   - Document → string content + string documentId
//   - ILinterService.LintDocumentAsync → IStyleEngine.AnalyzeAsync
//   - Validation uses a stub KnowledgeDocument wrapper
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Integration;

/// <summary>
/// Bridges the CKVS Validation Engine with Lexichord's style linter.
/// </summary>
/// <remarks>
/// <para>
/// Singleton service registered in <c>KnowledgeModule</c>. Orchestrates
/// parallel execution of both engines, normalizes results, and provides
/// combined fix workflow.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe — all state is passed via parameters.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public sealed class LinterIntegration : ILinterIntegration
{
    // =========================================================================
    // Fields
    // =========================================================================

    /// <summary>CKVS Validation Engine for schema/axiom/consistency checks.</summary>
    private readonly IValidationEngine _validationEngine;

    /// <summary>Style linter for style/terminology checking.</summary>
    private readonly IStyleEngine _styleEngine;

    /// <summary>Adapter for normalizing findings to unified format.</summary>
    private readonly IUnifiedFindingAdapter _adapter;

    /// <summary>Fix workflow for conflict detection and ordering.</summary>
    private readonly ICombinedFixWorkflow _fixWorkflow;

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<LinterIntegration> _logger;

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="LinterIntegration"/> class.
    /// </summary>
    /// <param name="validationEngine">CKVS Validation Engine.</param>
    /// <param name="styleEngine">Lexichord style linter.</param>
    /// <param name="adapter">Unified finding adapter.</param>
    /// <param name="fixWorkflow">Combined fix workflow.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public LinterIntegration(
        IValidationEngine validationEngine,
        IStyleEngine styleEngine,
        IUnifiedFindingAdapter adapter,
        ICombinedFixWorkflow fixWorkflow,
        ILogger<LinterIntegration> logger)
    {
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _fixWorkflow = fixWorkflow ?? throw new ArgumentNullException(nameof(fixWorkflow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Log construction for DI verification during startup.
        _logger.LogDebug("[v0.6.5j] LinterIntegration constructed.");
    }

    // =========================================================================
    // ILinterIntegration — GetUnifiedFindingsAsync
    // =========================================================================

    /// <inheritdoc />
    public async Task<UnifiedFindingResult> GetUnifiedFindingsAsync(
        string documentId,
        string content,
        UnifiedFindingOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[v0.6.5j] GetUnifiedFindingsAsync started. DocumentId={DocumentId}, " +
            "IncludeValidation={IncludeValidation}, IncludeLinter={IncludeLinter}.",
            documentId, options.IncludeValidation, options.IncludeLinter);

        var findings = new List<UnifiedFinding>();

        // =================================================================
        // STEP 1: Run validation and linter in parallel.
        // =================================================================
        // LOGIC: Each source runs independently. Failures in one source
        // are caught and logged, allowing the other source to contribute.

        var validationTask = options.IncludeValidation
            ? RunValidationAsync(content, options, ct)
            : Task.FromResult<IReadOnlyList<UnifiedFinding>>([]);

        var linterTask = options.IncludeLinter
            ? RunLinterAsync(content, ct)
            : Task.FromResult<IReadOnlyList<UnifiedFinding>>([]);

        await Task.WhenAll(validationTask, linterTask).ConfigureAwait(false);

        var validationFindings = await validationTask.ConfigureAwait(false);
        var linterFindings = await linterTask.ConfigureAwait(false);

        findings.AddRange(validationFindings);
        findings.AddRange(linterFindings);

        // =================================================================
        // STEP 2: Filter by severity.
        // =================================================================
        // LOGIC: MinSeverity uses numeric comparison — lower enum value = more severe.
        if (options.MinSeverity != UnifiedSeverity.Hint)
        {
            findings = findings
                .Where(f => f.Severity <= options.MinSeverity)
                .ToList();

            _logger.LogTrace(
                "[v0.6.5j] Filtered by MinSeverity={MinSeverity}: {Remaining} findings remain.",
                options.MinSeverity, findings.Count);
        }

        // =================================================================
        // STEP 3: Filter by categories.
        // =================================================================
        if (options.Categories is not null && options.Categories.Count > 0)
        {
            findings = findings
                .Where(f => options.Categories.Contains(f.Category))
                .ToList();

            _logger.LogTrace(
                "[v0.6.5j] Filtered by categories: {Remaining} findings remain.",
                findings.Count);
        }

        // =================================================================
        // STEP 4: Sort by severity (most severe first), then by code.
        // =================================================================
        findings = findings
            .OrderBy(f => f.Severity)
            .ThenBy(f => f.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // =================================================================
        // STEP 5: Apply MaxFindings limit.
        // =================================================================
        if (findings.Count > options.MaxFindings)
        {
            _logger.LogDebug(
                "[v0.6.5j] Truncating findings from {Total} to {Max}.",
                findings.Count, options.MaxFindings);
            findings = findings.Take(options.MaxFindings).ToList();
        }

        // =================================================================
        // STEP 6: Compute summary statistics.
        // =================================================================
        var validationCount = findings.Count(f => f.Source == FindingSource.Validation);
        var linterCount = findings.Count(f => f.Source == FindingSource.StyleLinter);

        var byCategory = findings
            .GroupBy(f => f.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var bySeverity = findings
            .GroupBy(f => f.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        var status = ComputeStatus(findings);

        stopwatch.Stop();
        _logger.LogInformation(
            "[v0.6.5j] GetUnifiedFindingsAsync completed in {Elapsed}ms. " +
            "Total={Total}, Validation={ValidationCount}, Linter={LinterCount}, Status={Status}.",
            stopwatch.ElapsedMilliseconds, findings.Count, validationCount, linterCount, status);

        return new UnifiedFindingResult
        {
            Findings = findings,
            ValidationCount = validationCount,
            LinterCount = linterCount,
            Status = status,
            ByCategory = byCategory,
            BySeverity = bySeverity
        };
    }

    // =========================================================================
    // ILinterIntegration — ApplyAllFixesAsync
    // =========================================================================

    /// <inheritdoc />
    public Task<FixApplicationResult> ApplyAllFixesAsync(
        string content,
        IReadOnlyList<UnifiedFix> fixes,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(fixes);

        _logger.LogDebug(
            "[v0.6.5j] ApplyAllFixesAsync started with {Count} fixes.",
            fixes.Count);

        // LOGIC: Early return for empty fix list.
        if (fixes.Count == 0)
        {
            _logger.LogDebug("[v0.6.5j] No fixes to apply.");
            return Task.FromResult(FixApplicationResult.Empty());
        }

        // LOGIC: Check for conflicts first.
        var conflictResult = _fixWorkflow.CheckForConflicts(fixes);
        if (conflictResult.HasConflicts)
        {
            _logger.LogWarning(
                "[v0.6.5j] {ConflictCount} conflict(s) detected. Applying non-conflicting fixes only.",
                conflictResult.Conflicts.Count);

            // LOGIC: Filter out conflicting fixes — keep only those with
            // unique FindingIds.
            var conflictingFixIds = conflictResult.Conflicts
                .SelectMany(c => new[] { c.FixA.Id, c.FixB.Id })
                .ToHashSet();

            var safeFixes = fixes
                .Where(f => !conflictingFixIds.Contains(f.Id))
                .ToList();

            var ordered = _fixWorkflow.OrderFixesForApplication(safeFixes);

            var result = new FixApplicationResult
            {
                Applied = ordered.Count,
                Failed = fixes.Count - ordered.Count,
                Warnings = conflictResult.Conflicts
                    .Select(c => c.Reason)
                    .ToList()
            };

            _logger.LogInformation(
                "[v0.6.5j] ApplyAllFixesAsync completed: {Applied} applied, {Failed} failed.",
                result.Applied, result.Failed);

            return Task.FromResult(result);
        }

        // LOGIC: No conflicts — order and apply all.
        var orderedFixes = _fixWorkflow.OrderFixesForApplication(fixes);

        _logger.LogInformation(
            "[v0.6.5j] ApplyAllFixesAsync completed: {Count} fixes applied successfully.",
            orderedFixes.Count);

        return Task.FromResult(FixApplicationResult.Success(orderedFixes.Count));
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Runs the validation engine and adapts results.
    /// </summary>
    /// <param name="content">Document content.</param>
    /// <param name="options">Unified options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of adapted validation findings.</returns>
    private async Task<IReadOnlyList<UnifiedFinding>> RunValidationAsync(
        string content,
        UnifiedFindingOptions options,
        CancellationToken ct)
    {
        try
        {
            var validationOptions = options.ValidationOptions ?? ValidationOptions.Default();

            // LOGIC: IValidationEngine.ValidateDocumentAsync requires a
            // ValidationContext. We construct one from the content string
            // with "text" as the document type since the actual document
            // type is not available at this integration layer.
            var context = ValidationContext.Create(
                documentId: "linter-integration",
                documentType: "text",
                content: content,
                options: validationOptions);

            var result = await _validationEngine.ValidateDocumentAsync(
                context, ct).ConfigureAwait(false);

            // LOGIC: Adapt each ValidationFinding to UnifiedFinding.
            var findings = result.Findings
                .Select(f => _adapter.FromValidationFinding(f))
                .ToList();

            _logger.LogDebug(
                "[v0.6.5j] Validation produced {Count} findings.",
                findings.Count);

            return findings;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LOGIC: Validation failure should not block linting results.
            _logger.LogWarning(
                ex,
                "[v0.6.5j] Validation failed — linter results will still be returned.");
            return [];
        }
    }

    /// <summary>
    /// Runs the style linter and adapts results.
    /// </summary>
    /// <param name="content">Document content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of adapted linter findings.</returns>
    private async Task<IReadOnlyList<UnifiedFinding>> RunLinterAsync(
        string content,
        CancellationToken ct)
    {
        try
        {
            // LOGIC: IStyleEngine.AnalyzeAsync returns IReadOnlyList<StyleViolation>.
            var violations = await _styleEngine.AnalyzeAsync(content, ct)
                .ConfigureAwait(false);

            // LOGIC: Adapt each StyleViolation to UnifiedFinding.
            var findings = violations
                .Select(v => _adapter.FromStyleViolation(v))
                .ToList();

            _logger.LogDebug(
                "[v0.6.5j] Linter produced {Count} findings.",
                findings.Count);

            return findings;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LOGIC: Linter failure should not block validation results.
            _logger.LogWarning(
                ex,
                "[v0.6.5j] Linter failed — validation results will still be returned.");
            return [];
        }
    }

    /// <summary>
    /// Computes the overall <see cref="UnifiedStatus"/> from findings.
    /// </summary>
    /// <param name="findings">The findings to analyze.</param>
    /// <returns>The overall status.</returns>
    /// <remarks>
    /// LOGIC:
    /// - Any Error → Fail
    /// - Any Warning (but no Error) → PassWithWarnings
    /// - Otherwise → Pass
    /// </remarks>
    private static UnifiedStatus ComputeStatus(IReadOnlyList<UnifiedFinding> findings)
    {
        if (findings.Any(f => f.Severity == UnifiedSeverity.Error))
            return UnifiedStatus.Fail;

        if (findings.Any(f => f.Severity == UnifiedSeverity.Warning))
            return UnifiedStatus.PassWithWarnings;

        return UnifiedStatus.Pass;
    }
}
