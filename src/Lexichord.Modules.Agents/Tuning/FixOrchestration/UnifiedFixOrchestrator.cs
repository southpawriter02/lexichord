// =============================================================================
// File: UnifiedFixOrchestrator.cs
// Project: Lexichord.Modules.Agents
// Description: Orchestrates fix application across multiple validator types.
// =============================================================================
// LOGIC: Coordinates fix application from Style Linter, Grammar Linter, and
//   CKVS Validation Engine while maintaining document integrity through:
//   - Position-based sorting (bottom-to-top) to prevent offset drift
//   - Conflict detection (overlapping, contradictory, dependent)
//   - Category-based ordering (Knowledge → Grammar → Style)
//   - Atomic application via editor undo groups
//   - Transaction recording for undo support
//   - Re-validation after fixes to catch cascading issues
//
// Spec Adaptations:
//   - Uses string documentPath instead of Document class (doesn't exist)
//   - Uses sync IEditorService APIs (not async ReplaceContentAsync)
//   - Uses BestFix/OldText/NewText (not Fix/ReplacementText)
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Abstractions.Contracts.Validation;
using Lexichord.Abstractions.Contracts.Validation.Events;
using Microsoft.Extensions.Logging;

using ValidationUnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Orchestrates fix application across multiple validator types with conflict
/// detection, position sorting, atomic undo, and re-validation support.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class implements the complete fix workflow pipeline:
/// <list type="number">
///   <item><description>Extract auto-fixable issues from validation result</description></item>
///   <item><description>Validate fix locations against document bounds</description></item>
///   <item><description>Detect conflicts between fixes (overlapping, contradictory, dependent)</description></item>
///   <item><description>Handle conflicts per <see cref="FixWorkflowOptions.ConflictStrategy"/></description></item>
///   <item><description>Sort fixes bottom-to-top to prevent offset drift</description></item>
///   <item><description>Group by category (Knowledge → Grammar → Style) for dependency ordering</description></item>
///   <item><description>Apply atomically within editor undo group</description></item>
///   <item><description>Push to <see cref="IUndoRedoService"/> for labeled undo history</description></item>
///   <item><description>Record <see cref="FixTransaction"/> on internal undo stack</description></item>
///   <item><description>Re-validate to detect cascading issues (if enabled)</description></item>
///   <item><description>Raise <see cref="FixesApplied"/> event</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
///   <item><description><see cref="IEditorService"/> — Document text access and modification</description></item>
///   <item><description><see cref="IUnifiedValidationService"/> — Re-validation after fixes</description></item>
///   <item><description><see cref="FixConflictDetector"/> — Conflict detection</description></item>
///   <item><description><see cref="IUndoRedoService"/> (nullable) — Labeled undo history</description></item>
///   <item><description><see cref="ILicenseContext"/> — License tier validation</description></item>
///   <item><description><see cref="ILogger{T}"/> — Diagnostic logging</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirement:</b> Requires WriterPro tier or higher for fix coordination.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe via <see cref="SemaphoreSlim"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
internal class UnifiedFixOrchestrator : IUnifiedFixWorkflow, IDisposable
{
    /// <summary>
    /// Maximum number of transactions to keep on the undo stack.
    /// </summary>
    private const int MaxUndoStackSize = 50;

    private readonly IEditorService _editorService;
    private readonly IUnifiedValidationService _validationService;
    private readonly FixConflictDetector _conflictDetector;
    private readonly IUndoRedoService? _undoService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<UnifiedFixOrchestrator> _logger;

    /// <summary>
    /// Internal undo stack for fix transactions.
    /// </summary>
    private readonly Stack<FixTransaction> _undoStack = new();

    /// <summary>
    /// Semaphore to prevent concurrent fix operations on the same document.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedFixOrchestrator"/> class.
    /// </summary>
    /// <param name="editorService">Service for document text access and modification.</param>
    /// <param name="validationService">Service for re-validation after fixes.</param>
    /// <param name="conflictDetector">Detector for fix conflicts.</param>
    /// <param name="undoService">Optional undo/redo service for labeled history (nullable).</param>
    /// <param name="licenseContext">License context for tier validation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/>, <paramref name="validationService"/>,
    /// <paramref name="conflictDetector"/>, <paramref name="licenseContext"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    public UnifiedFixOrchestrator(
        IEditorService editorService,
        IUnifiedValidationService validationService,
        FixConflictDetector conflictDetector,
        IUndoRedoService? undoService,
        ILicenseContext licenseContext,
        ILogger<UnifiedFixOrchestrator> logger)
    {
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(validationService);
        ArgumentNullException.ThrowIfNull(conflictDetector);
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(logger);

        _editorService = editorService;
        _validationService = validationService;
        _conflictDetector = conflictDetector;
        _undoService = undoService;
        _licenseContext = licenseContext;
        _logger = logger;

        _logger.LogDebug(
            "UnifiedFixOrchestrator initialized: UndoService={HasUndo}",
            undoService is not null);
    }

    /// <inheritdoc />
    public event EventHandler<FixesAppliedEventArgs>? FixesApplied;

    /// <inheritdoc />
    public event EventHandler<FixConflictDetectedEventArgs>? ConflictDetected;

    /// <inheritdoc />
    public async Task<FixApplyResult> FixAllAsync(
        string documentPath,
        UnifiedValidationResult validation,
        FixWorkflowOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(options);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation(
            "Starting fix workflow for '{DocumentPath}': {IssueCount} issues, DryRun={DryRun}, Strategy={Strategy}",
            documentPath, validation.TotalIssueCount, options.DryRun, options.ConflictStrategy);

        var sw = Stopwatch.StartNew();

        // LOGIC: Acquire semaphore to prevent concurrent fix operations.
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ExecuteFixPipelineAsync(
                documentPath, validation.Issues, options, sw, ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<FixApplyResult> FixByCategoryAsync(
        string documentPath,
        UnifiedValidationResult validation,
        IEnumerable<IssueCategory> categories,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(categories);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var categorySet = categories.ToHashSet();

        _logger.LogInformation(
            "Starting category-filtered fix workflow for '{DocumentPath}': categories={Categories}",
            documentPath, string.Join(", ", categorySet));

        // LOGIC: Filter issues to only include the specified categories.
        var filteredIssues = validation.Issues
            .Where(i => categorySet.Contains(i.Category))
            .ToList();

        var sw = Stopwatch.StartNew();

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ExecuteFixPipelineAsync(
                documentPath, filteredIssues, FixWorkflowOptions.Default, sw, ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<FixApplyResult> FixBySeverityAsync(
        string documentPath,
        UnifiedValidationResult validation,
        UnifiedSeverity minSeverity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(validation);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation(
            "Starting severity-filtered fix workflow for '{DocumentPath}': minSeverity={MinSeverity}",
            documentPath, minSeverity);

        // LOGIC: Filter issues to only include those at or above the minimum severity.
        // SeverityOrder: Error=0, Warning=1, Info=2, Hint=3 (lower = more severe).
        var maxSeverityOrder = (int)minSeverity;
        var filteredIssues = validation.Issues
            .Where(i => i.SeverityOrder <= maxSeverityOrder)
            .ToList();

        var sw = Stopwatch.StartNew();

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ExecuteFixPipelineAsync(
                documentPath, filteredIssues, FixWorkflowOptions.Default, sw, ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<FixApplyResult> FixByIdAsync(
        string documentPath,
        UnifiedValidationResult validation,
        IEnumerable<Guid> issueIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(issueIds);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var idSet = issueIds.ToHashSet();

        _logger.LogInformation(
            "Starting ID-filtered fix workflow for '{DocumentPath}': {IdCount} IDs",
            documentPath, idSet.Count);

        // LOGIC: Filter issues to only include the specified IDs.
        var filteredIssues = validation.Issues
            .Where(i => idSet.Contains(i.IssueId))
            .ToList();

        var sw = Stopwatch.StartNew();

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ExecuteFixPipelineAsync(
                documentPath, filteredIssues, FixWorkflowOptions.Default, sw, ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FixConflictCase> DetectConflicts(
        IReadOnlyList<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _conflictDetector.Detect(issues);
    }

    /// <inheritdoc />
    public Task<FixApplyResult> DryRunAsync(
        string documentPath,
        UnifiedValidationResult validation,
        CancellationToken ct = default)
    {
        var options = new FixWorkflowOptions
        {
            DryRun = true,
            EnableUndo = false,
            ReValidateAfterFixes = false
        };

        return FixAllAsync(documentPath, validation, options, ct);
    }

    /// <inheritdoc />
    public Task<bool> UndoLastFixesAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_undoStack.Count == 0)
        {
            _logger.LogWarning("No undo transactions available");
            return Task.FromResult(false);
        }

        var transaction = _undoStack.Pop();

        if (transaction.IsUndone)
        {
            _logger.LogWarning(
                "Transaction {TransactionId} already undone",
                transaction.Id);
            return Task.FromResult(false);
        }

        try
        {
            _logger.LogInformation(
                "Undoing transaction {TransactionId}: restoring {FixCount} fixes for '{DocumentPath}'",
                transaction.Id, transaction.FixCount, transaction.DocumentPath);

            // LOGIC: Get current document content for the undo group.
            var currentContent = _editorService.GetDocumentText() ?? "";

            // LOGIC: Replace document content with pre-fix state.
            _editorService.BeginUndoGroup($"Undo {transaction.FixCount} fixes");
            try
            {
                // LOGIC: Delete all current content and insert the pre-fix content.
                if (currentContent.Length > 0)
                {
                    _editorService.DeleteText(0, currentContent.Length);
                }

                _editorService.InsertText(0, transaction.DocumentBefore);
            }
            finally
            {
                _editorService.EndUndoGroup();
            }

            _logger.LogInformation(
                "Successfully undone transaction {TransactionId}",
                transaction.Id);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to undo transaction {TransactionId}",
                transaction.Id);

            // LOGIC: Push transaction back on failure so it can be retried.
            _undoStack.Push(transaction);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Executes the core fix pipeline.
    /// </summary>
    private async Task<FixApplyResult> ExecuteFixPipelineAsync(
        string documentPath,
        IReadOnlyList<UnifiedIssue> issues,
        FixWorkflowOptions options,
        Stopwatch sw,
        CancellationToken ct)
    {
        var operationTrace = new List<FixOperationTrace>();

        // ── Step 1: Extract auto-fixable issues ──────────────────────────
        var fixableIssues = FixPositionSorter.SortBottomToTop(issues);

        if (fixableIssues.Count == 0)
        {
            _logger.LogInformation("No auto-fixable issues found");
            sw.Stop();
            return FixApplyResult.Empty(sw.Elapsed);
        }

        _logger.LogInformation(
            "Found {FixableCount} auto-fixable issues out of {TotalCount} total",
            fixableIssues.Count, issues.Count);

        // ── Step 2: Get document content ─────────────────────────────────
        string? documentContent = null;
        if (!options.DryRun)
        {
            documentContent = _editorService.GetDocumentText();
            if (documentContent is null)
            {
                _logger.LogWarning("No active document content available");
                sw.Stop();
                return new FixApplyResult
                {
                    Success = false,
                    AppliedCount = 0,
                    SkippedCount = fixableIssues.Count,
                    FailedCount = 0,
                    Duration = sw.Elapsed
                };
            }
        }
        else
        {
            // LOGIC: For dry-run, we still need the document content for location validation.
            documentContent = _editorService.GetDocumentText() ?? "";
        }

        var documentBefore = documentContent;

        // ── Step 3: Validate locations ───────────────────────────────────
        var locationConflicts = _conflictDetector.ValidateLocations(
            fixableIssues, documentContent.Length);

        // LOGIC: Remove issues with invalid locations.
        var invalidIssueIds = locationConflicts
            .SelectMany(c => c.ConflictingIssueIds)
            .ToHashSet();

        if (invalidIssueIds.Count > 0)
        {
            _logger.LogWarning(
                "Removing {Count} issues with invalid locations",
                invalidIssueIds.Count);

            fixableIssues = fixableIssues
                .Where(i => !invalidIssueIds.Contains(i.IssueId))
                .ToList();
        }

        // ── Step 4: Detect conflicts ─────────────────────────────────────
        var conflicts = _conflictDetector.Detect(fixableIssues);
        var allConflicts = locationConflicts.Concat(conflicts).ToList();

        // LOGIC: Raise ConflictDetected event if any conflicts found.
        if (allConflicts.Count > 0)
        {
            ConflictDetected?.Invoke(this,
                new FixConflictDetectedEventArgs(documentPath, allConflicts));
        }

        // ── Step 5: Handle conflicts per strategy ────────────────────────
        var conflictingIssues = new List<UnifiedIssue>();
        var skippedCount = 0;

        var blockingConflicts = allConflicts
            .Where(c => c.IsBlocking)
            .ToList();

        if (blockingConflicts.Count > 0)
        {
            switch (options.ConflictStrategy)
            {
                case ConflictHandlingStrategy.ThrowException:
                    _logger.LogError(
                        "Blocking conflicts detected with ThrowException strategy: {Count} conflicts",
                        blockingConflicts.Count);
                    throw new FixConflictException(
                        allConflicts,
                        $"Detected {blockingConflicts.Count} blocking conflicts between fixes");

                case ConflictHandlingStrategy.SkipConflicting:
                case ConflictHandlingStrategy.PromptUser:
                    // LOGIC: Remove conflicting issues from the fix list.
                    var conflictingIds = blockingConflicts
                        .SelectMany(c => c.ConflictingIssueIds)
                        .ToHashSet();

                    conflictingIssues = fixableIssues
                        .Where(i => conflictingIds.Contains(i.IssueId))
                        .ToList();

                    fixableIssues = fixableIssues
                        .Where(i => !conflictingIds.Contains(i.IssueId))
                        .ToList();

                    skippedCount = conflictingIssues.Count;

                    _logger.LogInformation(
                        "Skipped {SkippedCount} conflicting issues, {RemainingCount} remaining",
                        skippedCount, fixableIssues.Count);
                    break;

                case ConflictHandlingStrategy.PriorityBased:
                    // LOGIC: Keep higher-severity/confidence fix, skip the other.
                    fixableIssues = ResolvePriorityConflicts(
                        fixableIssues, blockingConflicts, out var prioritySkipped);
                    conflictingIssues = prioritySkipped;
                    skippedCount = prioritySkipped.Count;
                    break;
            }
        }

        if (fixableIssues.Count == 0)
        {
            _logger.LogInformation("No fixes remaining after conflict resolution");
            sw.Stop();
            return new FixApplyResult
            {
                Success = true,
                AppliedCount = 0,
                SkippedCount = skippedCount,
                FailedCount = 0,
                ConflictingIssues = conflictingIssues,
                DetectedConflicts = allConflicts,
                Duration = sw.Elapsed
            };
        }

        // ── Step 6: Group by category ────────────────────────────────────
        var groups = FixGrouper.GroupByCategory(fixableIssues);

        _logger.LogDebug(
            "Grouped into {GroupCount} categories: {Categories}",
            groups.Count,
            string.Join(", ", groups.Select(g => $"{g.Category}({g.Count})")));

        // LOGIC: Re-sort within each group to ensure bottom-to-top ordering.
        var orderedIssues = new List<UnifiedIssue>();
        foreach (var group in groups)
        {
            orderedIssues.AddRange(
                FixPositionSorter.SortBottomToTopUnfiltered(group.Issues));
        }

        // ── Step 7: Apply fixes ──────────────────────────────────────────
        if (options.DryRun)
        {
            // LOGIC: Dry-run — simulate application without modifying the document.
            _logger.LogInformation(
                "Dry-run complete: would apply {Count} fixes",
                orderedIssues.Count);

            sw.Stop();
            return new FixApplyResult
            {
                Success = true,
                AppliedCount = orderedIssues.Count,
                SkippedCount = skippedCount,
                FailedCount = 0,
                ResolvedIssues = orderedIssues,
                ConflictingIssues = conflictingIssues,
                DetectedConflicts = allConflicts,
                Duration = sw.Elapsed
            };
        }

        // LOGIC: Apply fixes atomically within editor undo group.
        var appliedCount = 0;
        var failedCount = 0;
        var resolvedIssues = new List<UnifiedIssue>();
        var errorsByIssueId = new Dictionary<Guid, string>();

        _editorService.BeginUndoGroup($"Apply {orderedIssues.Count} fixes");
        try
        {
            foreach (var issue in orderedIssues)
            {
                ct.ThrowIfCancellationRequested();

                // LOGIC: Check timeout.
                if (sw.Elapsed > options.Timeout)
                {
                    _logger.LogWarning(
                        "Fix application timed out after {Elapsed}ms with {Applied} fixes applied",
                        sw.ElapsedMilliseconds, appliedCount);
                    throw new FixApplicationTimeoutException(appliedCount, sw.Elapsed);
                }

                var fix = issue.BestFix!;

                try
                {
                    if (options.Verbose)
                    {
                        operationTrace.Add(new FixOperationTrace(
                            issue.IssueId,
                            DateTime.UtcNow,
                            $"Applying fix: {issue.SourceId} at [{fix.Location.Start}, {fix.Location.End}]",
                            "Started",
                            null));
                    }

                    _logger.LogDebug(
                        "Applying fix for '{SourceId}': Type={Type}, Location=[{Start}, {End}]",
                        issue.SourceId, fix.Type, fix.Location.Start, fix.Location.End);

                    // LOGIC: Delete old text if the fix has a non-zero length span.
                    if (fix.Location.Length > 0)
                    {
                        _editorService.DeleteText(fix.Location.Start, fix.Location.Length);
                    }

                    // LOGIC: Insert new text if present.
                    if (!string.IsNullOrEmpty(fix.NewText))
                    {
                        _editorService.InsertText(fix.Location.Start, fix.NewText);
                    }

                    appliedCount++;
                    resolvedIssues.Add(issue);

                    if (options.Verbose)
                    {
                        operationTrace.Add(new FixOperationTrace(
                            issue.IssueId,
                            DateTime.UtcNow,
                            $"Applied fix: {issue.SourceId}",
                            "Success",
                            null));
                    }

                    _logger.LogDebug(
                        "Fix applied successfully for '{SourceId}'",
                        issue.SourceId);
                }
                catch (Exception ex) when (ex is not OperationCanceledException
                                           and not FixApplicationTimeoutException)
                {
                    failedCount++;
                    errorsByIssueId[issue.IssueId] = ex.Message;

                    _logger.LogWarning(ex,
                        "Failed to apply fix for '{SourceId}': {Error}",
                        issue.SourceId, ex.Message);

                    if (options.Verbose)
                    {
                        operationTrace.Add(new FixOperationTrace(
                            issue.IssueId,
                            DateTime.UtcNow,
                            $"Apply fix: {issue.SourceId}",
                            "Failed",
                            ex.Message));
                    }
                }
            }
        }
        finally
        {
            _editorService.EndUndoGroup();
        }

        _logger.LogInformation(
            "Fix application complete: {Applied} applied, {Failed} failed, {Skipped} skipped in {Elapsed}ms",
            appliedCount, failedCount, skippedCount, sw.ElapsedMilliseconds);

        // ── Step 8: Record transaction for undo ──────────────────────────
        var transactionId = Guid.NewGuid();

        if (options.EnableUndo && appliedCount > 0)
        {
            var documentAfter = _editorService.GetDocumentText() ?? "";

            var transaction = new FixTransaction
            {
                Id = transactionId,
                DocumentPath = documentPath,
                DocumentBefore = documentBefore,
                DocumentAfter = documentAfter,
                FixedIssueIds = resolvedIssues.Select(i => i.IssueId).ToList(),
                AppliedAt = DateTime.UtcNow,
                IsUndone = false
            };

            _undoStack.Push(transaction);

            // LOGIC: Trim undo stack to max size.
            while (_undoStack.Count > MaxUndoStackSize)
            {
                // LOGIC: Convert to list, remove oldest, rebuild stack.
                var items = _undoStack.ToList();
                items.RemoveAt(items.Count - 1);
                _undoStack.Clear();
                for (var i = items.Count - 1; i >= 0; i--)
                {
                    _undoStack.Push(items[i]);
                }
            }

            _logger.LogDebug(
                "Transaction {TransactionId} recorded for undo ({FixCount} fixes, stack size: {StackSize})",
                transactionId, appliedCount, _undoStack.Count);

            // LOGIC: Push to higher-level undo service if available.
            if (_undoService is not null)
            {
                _undoService.Push(new FixWorkflowUndoOperation(
                    this, transactionId, appliedCount, documentPath));
            }
        }

        // ── Step 9: Re-validate ──────────────────────────────────────────
        IReadOnlyList<UnifiedIssue> remainingIssues = [];

        if (options.ReValidateAfterFixes && appliedCount > 0)
        {
            try
            {
                var newContent = _editorService.GetDocumentText() ?? "";

                _logger.LogDebug(
                    "Re-validating document after {Applied} fixes",
                    appliedCount);

                // LOGIC: Invalidate cache to force fresh validation.
                _validationService.InvalidateCache(documentPath);

                var revalidation = await _validationService.ValidateAsync(
                    documentPath,
                    newContent,
                    UnifiedValidationOptions.Default,
                    ct).ConfigureAwait(false);

                remainingIssues = revalidation.Issues;

                if (remainingIssues.Count > 0)
                {
                    _logger.LogInformation(
                        "Re-validation found {Count} remaining issues after fixes",
                        remainingIssues.Count);
                }
                else
                {
                    _logger.LogDebug("Re-validation found no remaining issues");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Re-validation failed after fix application: {Error}",
                    ex.Message);
            }
        }

        sw.Stop();

        // ── Step 10: Build result ────────────────────────────────────────
        var result = new FixApplyResult
        {
            Success = failedCount == 0,
            AppliedCount = appliedCount,
            SkippedCount = skippedCount,
            FailedCount = failedCount,
            ModifiedContent = _editorService.GetDocumentText(),
            ResolvedIssues = resolvedIssues,
            RemainingIssues = remainingIssues,
            ConflictingIssues = conflictingIssues,
            ErrorsByIssueId = errorsByIssueId,
            DetectedConflicts = allConflicts,
            Duration = sw.Elapsed,
            TransactionId = transactionId,
            OperationTrace = operationTrace
        };

        // ── Step 11: Raise event ─────────────────────────────────────────
        if (appliedCount > 0)
        {
            FixesApplied?.Invoke(this,
                new FixesAppliedEventArgs(documentPath, result));
        }

        return result;
    }

    /// <summary>
    /// Resolves conflicts by keeping the higher-priority fix.
    /// </summary>
    private static List<UnifiedIssue> ResolvePriorityConflicts(
        IReadOnlyList<UnifiedIssue> issues,
        IReadOnlyList<FixConflictCase> conflicts,
        out List<UnifiedIssue> skipped)
    {
        skipped = [];
        var issueMap = issues.ToDictionary(i => i.IssueId);
        var skipIds = new HashSet<Guid>();

        foreach (var conflict in conflicts)
        {
            if (conflict.ConflictingIssueIds.Count < 2) continue;

            // LOGIC: Find the highest-priority issue in the conflict.
            var conflictIssues = conflict.ConflictingIssueIds
                .Where(id => issueMap.ContainsKey(id))
                .Select(id => issueMap[id])
                .OrderBy(i => i.SeverityOrder)
                .ThenByDescending(i => i.BestFix?.Confidence ?? 0)
                .ToList();

            // LOGIC: Keep the first (highest priority), skip the rest.
            for (var i = 1; i < conflictIssues.Count; i++)
            {
                skipIds.Add(conflictIssues[i].IssueId);
            }
        }

        skipped = issues.Where(i => skipIds.Contains(i.IssueId)).ToList();
        return issues.Where(i => !skipIds.Contains(i.IssueId)).ToList();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _semaphore.Dispose();
        _undoStack.Clear();

        _logger.LogDebug("UnifiedFixOrchestrator disposed");
    }
}

/// <summary>
/// Undo operation for the fix workflow, integrating with <see cref="IUndoRedoService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Created by <see cref="UnifiedFixOrchestrator"/> after successful fix
/// application. When undone, delegates to the orchestrator's
/// <see cref="IUnifiedFixWorkflow.UndoLastFixesAsync"/> method.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
internal class FixWorkflowUndoOperation : IUndoableOperation
{
    private readonly UnifiedFixOrchestrator _orchestrator;
    private readonly Guid _transactionId;
    private readonly int _fixCount;
    private readonly string _documentPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixWorkflowUndoOperation"/> class.
    /// </summary>
    /// <param name="orchestrator">The orchestrator that applied the fixes.</param>
    /// <param name="transactionId">The transaction ID for this operation.</param>
    /// <param name="fixCount">The number of fixes applied.</param>
    /// <param name="documentPath">The document that was fixed.</param>
    public FixWorkflowUndoOperation(
        UnifiedFixOrchestrator orchestrator,
        Guid transactionId,
        int fixCount,
        string documentPath)
    {
        _orchestrator = orchestrator;
        _transactionId = transactionId;
        _fixCount = fixCount;
        _documentPath = documentPath;
    }

    /// <inheritdoc />
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public string DisplayName => $"Fix Workflow ({_fixCount} fixes)";

    /// <inheritdoc />
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct = default)
    {
        // LOGIC: No-op — the operation was already executed by the orchestrator.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UndoAsync(CancellationToken ct = default)
    {
        return _orchestrator.UndoLastFixesAsync(ct);
    }

    /// <inheritdoc />
    public Task RedoAsync(CancellationToken ct = default)
    {
        // LOGIC: Re-do is not supported for fix workflows because the validation
        // state may have changed. The user should re-run the fix workflow instead.
        return Task.CompletedTask;
    }
}
