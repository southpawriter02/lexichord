// =============================================================================
// File: IUnifiedFixWorkflow.cs
// Project: Lexichord.Abstractions
// Description: Interface for orchestrating fix application across validators.
// =============================================================================
// LOGIC: Coordinates fix application from multiple validation sources (Style
//   Linter, Grammar Linter, CKVS Validation Engine) while maintaining document
//   integrity through position sorting, conflict detection, atomic application,
//   and re-validation.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation.Events;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Orchestrates the application of auto-fixable issues from unified validation.
/// Coordinates across multiple fix types (Style, Grammar, Knowledge) while
/// maintaining document integrity and preventing conflicts.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The fix workflow follows these principles:
/// <list type="number">
///   <item><description>Position-based sorting (bottom-to-top) prevents offset drift</description></item>
///   <item><description>Conflict detection identifies incompatible fixes before application</description></item>
///   <item><description>Category ordering (Knowledge → Grammar → Style) respects dependencies</description></item>
///   <item><description>Atomic application ensures all-or-nothing semantics via editor undo groups</description></item>
///   <item><description>Re-validation after fixes catches cascading issues</description></item>
///   <item><description>Transaction support enables undo operations</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Spec Adaptations:</b>
/// <list type="bullet">
///   <item><description>Methods accept <c>string documentPath</c> instead of <c>Document document</c>
///     because the spec's mutable Document class does not exist in the codebase. Document content
///     is accessed via <c>IEditorService.GetDocumentText()</c>.</description></item>
///   <item><description>Fix application uses sync <c>IEditorService.BeginUndoGroup()</c>,
///     <c>DeleteText()</c>, <c>InsertText()</c>, <c>EndUndoGroup()</c> instead of the spec's
///     <c>ReplaceContentAsync()</c> which does not exist.</description></item>
///   <item><description>Fix data is accessed via <c>UnifiedIssue.BestFix</c> (not <c>.Fix</c>)
///     with <c>.OldText</c>/<c>.NewText</c> (not <c>.ReplacementText</c>).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirement:</b> Requires WriterPro tier or higher for fix coordination.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent calls.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var validationService = serviceProvider.GetRequiredService&lt;IUnifiedValidationService&gt;();
/// var fixWorkflow = serviceProvider.GetRequiredService&lt;IUnifiedFixWorkflow&gt;();
///
/// var validation = await validationService.ValidateAsync(
///     documentPath, content, UnifiedValidationOptions.Default);
///
/// var result = await fixWorkflow.FixAllAsync(
///     documentPath, validation, FixWorkflowOptions.Default);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Applied {result.AppliedCount} fixes");
///     if (result.HasRemainingIssues)
///         Console.WriteLine($"Re-validation found {result.RemainingIssues.Count} new issues");
/// }
/// </code>
/// </example>
/// <seealso cref="FixApplyResult"/>
/// <seealso cref="FixWorkflowOptions"/>
/// <seealso cref="FixConflictCase"/>
/// <seealso cref="IUnifiedValidationService"/>
public interface IUnifiedFixWorkflow
{
    /// <summary>
    /// Attempts to apply all auto-fixable issues from the validation result.
    /// </summary>
    /// <param name="documentPath">Path to the document to fix.</param>
    /// <param name="validation">Validation result with issues and fixes.</param>
    /// <param name="options">Fix workflow options (dry-run, conflict handling, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success, applied fixes, and any remaining issues.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="validation"/>,
    /// or <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="FixConflictException">
    /// Thrown when conflicts are detected and <see cref="FixWorkflowOptions.ConflictStrategy"/>
    /// is <see cref="ConflictHandlingStrategy.ThrowException"/>.
    /// </exception>
    /// <exception cref="FixApplicationTimeoutException">
    /// Thrown when the operation exceeds <see cref="FixWorkflowOptions.Timeout"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the service has been disposed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Applies fixes in this order:
    /// <list type="number">
    ///   <item><description>Extract auto-fixable issues (<see cref="UnifiedIssue.CanAutoFix"/>)</description></item>
    ///   <item><description>Detect conflicts between fixes</description></item>
    ///   <item><description>Handle conflicts per <see cref="FixWorkflowOptions.ConflictStrategy"/></description></item>
    ///   <item><description>Sort fixes bottom-to-top by position</description></item>
    ///   <item><description>Group by category (Knowledge first, Grammar second, Style last)</description></item>
    ///   <item><description>Apply atomically within editor undo group</description></item>
    ///   <item><description>Re-validate to detect cascading issues (if enabled)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<FixApplyResult> FixAllAsync(
        string documentPath,
        UnifiedValidationResult validation,
        FixWorkflowOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Applies fixes for issues in the specified categories only.
    /// </summary>
    /// <param name="documentPath">Path to the document to fix.</param>
    /// <param name="validation">Validation result with issues and fixes.</param>
    /// <param name="categories">Categories to fix (e.g., only Style and Grammar, not Knowledge).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of selective fix application.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="validation"/>,
    /// or <paramref name="categories"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Filters <see cref="UnifiedValidationResult.Issues"/> to only
    /// include issues matching the specified <see cref="IssueCategory"/> values,
    /// then delegates to the core fix application pipeline with default options.
    /// </remarks>
    Task<FixApplyResult> FixByCategoryAsync(
        string documentPath,
        UnifiedValidationResult validation,
        IEnumerable<IssueCategory> categories,
        CancellationToken ct = default);

    /// <summary>
    /// Applies fixes for issues at or above the specified severity level.
    /// </summary>
    /// <param name="documentPath">Path to the document to fix.</param>
    /// <param name="validation">Validation result with issues and fixes.</param>
    /// <param name="minSeverity">Minimum severity to fix (e.g., fix Errors and Warnings, not Info).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of severity-based fix application.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="validation"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Filters issues where <see cref="UnifiedIssue.SeverityOrder"/>
    /// is less than or equal to <paramref name="minSeverity"/> (lower order = more severe),
    /// then delegates to the core fix application pipeline with default options.
    /// </remarks>
    Task<FixApplyResult> FixBySeverityAsync(
        string documentPath,
        UnifiedValidationResult validation,
        UnifiedSeverity minSeverity,
        CancellationToken ct = default);

    /// <summary>
    /// Applies fixes for the specified issue IDs only.
    /// </summary>
    /// <param name="documentPath">Path to the document to fix.</param>
    /// <param name="validation">Validation result with issues and fixes.</param>
    /// <param name="issueIds">Specific issue IDs to fix.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of selective fix application.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="validation"/>,
    /// or <paramref name="issueIds"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Filters issues to only those whose <see cref="UnifiedIssue.IssueId"/>
    /// is in the provided set, then delegates to the core fix application pipeline
    /// with default options.
    /// </remarks>
    Task<FixApplyResult> FixByIdAsync(
        string documentPath,
        UnifiedValidationResult validation,
        IEnumerable<Guid> issueIds,
        CancellationToken ct = default);

    /// <summary>
    /// Detects conflicts between the fixes that would be applied.
    /// </summary>
    /// <param name="issues">Issues with fixes to analyze.</param>
    /// <returns>List of detected conflicts.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Analyzes the provided issues for conflicts without modifying
    /// the document. Conflict types include:
    /// <list type="bullet">
    ///   <item><description><see cref="FixConflictType.OverlappingPositions"/>: Two fixes target overlapping text regions</description></item>
    ///   <item><description><see cref="FixConflictType.ContradictorySuggestions"/>: Fixes suggest incompatible changes at same location</description></item>
    ///   <item><description><see cref="FixConflictType.DependentFixes"/>: One fix's success depends on another's outcome</description></item>
    ///   <item><description><see cref="FixConflictType.InvalidLocation"/>: Fix location is out of bounds</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    IReadOnlyList<FixConflictCase> DetectConflicts(
        IReadOnlyList<UnifiedIssue> issues);

    /// <summary>
    /// Simulates fix application without modifying the document.
    /// </summary>
    /// <param name="documentPath">Path to the document to analyze.</param>
    /// <param name="validation">Validation result with issues and fixes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result showing what would be fixed without actually fixing.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="validation"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Equivalent to calling <see cref="FixAllAsync"/> with
    /// <see cref="FixWorkflowOptions.DryRun"/> set to true. No document modifications
    /// are made, no undo transactions are recorded, and no events are raised.
    /// </remarks>
    Task<FixApplyResult> DryRunAsync(
        string documentPath,
        UnifiedValidationResult validation,
        CancellationToken ct = default);

    /// <summary>
    /// Undoes the last set of applied fixes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if undo was successful; <c>false</c> if no transactions to undo.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Maintains a stack of <see cref="FixTransaction"/> records (max 50).
    /// Each call pops the most recent transaction and restores the document to its
    /// pre-fix state via <c>IEditorService</c>. The transaction is marked as
    /// <see cref="FixTransaction.IsUndone"/> to prevent double-undo.
    /// </para>
    /// </remarks>
    Task<bool> UndoLastFixesAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when fixes are successfully applied.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Raised after each non-dry-run fix application that applies
    /// at least one fix. UI components can subscribe to refresh their displays.
    /// </remarks>
    event EventHandler<FixesAppliedEventArgs>? FixesApplied;

    /// <summary>
    /// Event raised when conflicts are detected during fix application.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Raised when the conflict detector finds one or more conflicts,
    /// regardless of the conflict handling strategy. UI components can subscribe to
    /// display conflict resolution dialogs.
    /// </remarks>
    event EventHandler<FixConflictDetectedEventArgs>? ConflictDetected;
}
