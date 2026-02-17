// =============================================================================
// File: FixTransaction.cs
// Project: Lexichord.Abstractions
// Description: Represents a transaction of applied fixes for undo support.
// =============================================================================
// LOGIC: Records the document state before and after fix application, enabling
//   the fix orchestrator to undo the last set of applied fixes.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Represents a transaction of applied fixes for undo support.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Each call to <see cref="IUnifiedFixWorkflow.FixAllAsync"/>,
/// <see cref="IUnifiedFixWorkflow.FixByCategoryAsync"/>,
/// <see cref="IUnifiedFixWorkflow.FixBySeverityAsync"/>, or
/// <see cref="IUnifiedFixWorkflow.FixByIdAsync"/> creates a transaction
/// that records the document state. Transactions are maintained on an
/// internal undo stack (max 50 entries) for
/// <see cref="IUnifiedFixWorkflow.UndoLastFixesAsync"/> operations.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="IUnifiedFixWorkflow.UndoLastFixesAsync"/>
/// <seealso cref="FixApplyResult"/>
public record FixTransaction
{
    /// <summary>
    /// Gets the unique transaction identifier.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Correlates with <see cref="FixApplyResult.TransactionId"/>
    /// for tracing undo operations back to the original fix application.
    /// </remarks>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the path to the document that was fixed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Used to verify that undo is being applied to the correct
    /// document. If the active document has changed since the fix, undo may
    /// need to switch documents first.
    /// </remarks>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Gets the complete document content before fixes were applied.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The full text of the document captured before any fixes
    /// in this transaction were applied. Used by
    /// <see cref="IUnifiedFixWorkflow.UndoLastFixesAsync"/> to restore
    /// the document to its pre-fix state.
    /// </remarks>
    public required string DocumentBefore { get; init; }

    /// <summary>
    /// Gets the complete document content after fixes were applied.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The full text of the document after all fixes in this
    /// transaction were applied. Used for verification during undo — if the
    /// current document content doesn't match, the undo may be stale.
    /// </remarks>
    public required string DocumentAfter { get; init; }

    /// <summary>
    /// Gets the IDs of issues that were fixed in this transaction.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the <see cref="UnifiedIssue.IssueId"/> values
    /// of all issues whose fixes were applied. Used for logging and
    /// tracing which fixes are being undone.
    /// </remarks>
    public required IReadOnlyList<Guid> FixedIssueIds { get; init; }

    /// <summary>
    /// Gets the timestamp when fixes were applied.
    /// </summary>
    public required DateTime AppliedAt { get; init; }

    /// <summary>
    /// Gets whether this transaction has been undone.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Set to true after <see cref="IUnifiedFixWorkflow.UndoLastFixesAsync"/>
    /// successfully restores the document. Prevents double-undo of the same transaction.
    /// </remarks>
    public bool IsUndone { get; init; }

    /// <summary>
    /// Gets the number of fixes in this transaction.
    /// </summary>
    public int FixCount => FixedIssueIds.Count;
}
