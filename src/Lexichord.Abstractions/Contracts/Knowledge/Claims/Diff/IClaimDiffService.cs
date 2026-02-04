// =============================================================================
// File: IClaimDiffService.cs
// Project: Lexichord.Abstractions
// Description: Interface for comparing claims between versions.
// =============================================================================
// LOGIC: Defines the contract for claim comparison operations. Includes
//   comparison, snapshot creation, history tracking, and contradiction detection.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: Claim (v0.5.6e), Diff contracts (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Service for comparing claims between versions.
/// </summary>
/// <remarks>
/// <para>
/// Provides operations for tracking changes to claims over time:
/// </para>
/// <list type="bullet">
///   <item><b>Diff:</b> Compare two claim sets to find changes.</item>
///   <item><b>Snapshot:</b> Create baseline checkpoints for comparison.</item>
///   <item><b>History:</b> Track changes over time for audit.</item>
///   <item><b>Contradictions:</b> Detect conflicting claims across documents.</item>
/// </list>
/// <para>
/// <b>License:</b> Full functionality requires Teams tier. Free tier limited
/// to basic diff operations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage: compare two claim sets
/// var result = diffService.Diff(oldClaims, newClaims);
/// 
/// // Compare document versions
/// var docChanges = await diffService.DiffDocumentVersionsAsync(docId, 1, 2);
/// 
/// // Create a baseline snapshot
/// var snapshot = await diffService.CreateSnapshotAsync(docId, "v1.0");
/// 
/// // Later, compare to baseline
/// var sinceBaseline = await diffService.DiffFromBaselineAsync(docId, snapshot.Id);
/// </code>
/// </example>
public interface IClaimDiffService
{
    /// <summary>
    /// Compare two sets of claims to find differences.
    /// </summary>
    /// <param name="oldClaims">The previous version of claims.</param>
    /// <param name="newClaims">The current version of claims.</param>
    /// <param name="options">Optional configuration for the diff operation.</param>
    /// <returns>
    /// A <see cref="ClaimDiffResult"/> containing added, removed, modified, and unchanged claims.
    /// </returns>
    /// <remarks>
    /// LOGIC: Compares claims by ID first, then by semantic similarity if
    /// <see cref="DiffOptions.UseSemanticMatching"/> is enabled. Generates
    /// field-level changes for modified claims.
    /// </remarks>
    ClaimDiffResult Diff(
        IReadOnlyList<Claim> oldClaims,
        IReadOnlyList<Claim> newClaims,
        DiffOptions? options = null);

    /// <summary>
    /// Compare claims between specific document versions.
    /// </summary>
    /// <param name="documentId">Document to compare.</param>
    /// <param name="oldVersion">Previous version number.</param>
    /// <param name="newVersion">Current version number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ClaimDiffResult"/> for the version comparison.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves claims for each version from the repository and
    /// performs a diff with default options.
    /// </remarks>
    Task<ClaimDiffResult> DiffDocumentVersionsAsync(
        Guid documentId,
        int oldVersion,
        int newVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Compare current claims against a baseline snapshot.
    /// </summary>
    /// <param name="documentId">Document to compare.</param>
    /// <param name="baselineSnapshotId">Snapshot to compare against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ClaimDiffResult"/> showing changes since the baseline.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves baseline claims from snapshot, current claims from
    /// repository, and performs a diff.
    /// </remarks>
    Task<ClaimDiffResult> DiffFromBaselineAsync(
        Guid documentId,
        Guid baselineSnapshotId,
        CancellationToken ct = default);

    /// <summary>
    /// Create a snapshot of current claims for a document.
    /// </summary>
    /// <param name="documentId">Document to snapshot.</param>
    /// <param name="label">Optional descriptive label.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The created <see cref="ClaimSnapshot"/>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Captures current claim IDs as a baseline for future comparisons.
    /// Creates a history entry with <see cref="ClaimHistoryAction.SnapshotCreated"/>.
    /// </remarks>
    Task<ClaimSnapshot> CreateSnapshotAsync(
        Guid documentId,
        string? label = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get change history for a document.
    /// </summary>
    /// <param name="documentId">Document to get history for.</param>
    /// <param name="limit">Maximum entries to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="ClaimHistoryEntry"/> records, most recent first.
    /// </returns>
    Task<IReadOnlyList<ClaimHistoryEntry>> GetHistoryAsync(
        Guid documentId,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Find contradictions between claims in a project.
    /// </summary>
    /// <param name="projectId">Project to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of detected <see cref="ClaimContradiction"/> records.
    /// </returns>
    /// <remarks>
    /// LOGIC: Analyzes claims across all documents in a project to find
    /// conflicting assertions. Detects direct contradictions (same subject
    /// and predicate, different objects) and other conflict types.
    /// </remarks>
    Task<IReadOnlyList<ClaimContradiction>> FindContradictionsAsync(
        Guid projectId,
        CancellationToken ct = default);
}
