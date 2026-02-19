// =============================================================================
// File: IDocumentFlagger.cs
// Project: Lexichord.Abstractions
// Description: Interface for managing document flags.
// =============================================================================
// LOGIC: Provides flag creation and resolution capabilities for the
//   graph-to-doc sync workflow. Flags mark documents needing review.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: DocumentFlag, DocumentFlagOptions, FlagReason, FlagResolution
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Manages document flags for review workflows.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="IGraphToDocumentSyncProvider"/> to create and manage
/// document flags when graph changes affect document content:
/// </para>
/// <list type="bullet">
///   <item><b>FlagDocumentAsync:</b> Create a flag for a single document.</item>
///   <item><b>FlagDocumentsAsync:</b> Create flags for multiple documents.</item>
///   <item><b>ResolveFlagAsync:</b> Mark a flag as resolved.</item>
///   <item><b>GetFlagAsync:</b> Retrieve a specific flag.</item>
///   <item><b>GetPendingFlagsAsync:</b> List pending flags for a document.</item>
/// </list>
/// <para>
/// <b>Implementation:</b> See <c>DocumentFlagger</c> in
/// Lexichord.Modules.Knowledge.Sync.GraphToDoc.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Flag a document due to entity change
/// var options = new DocumentFlagOptions
/// {
///     Priority = FlagPriority.High,
///     TriggeringEntityId = entityId,
///     SendNotification = true
/// };
///
/// var flag = await flagger.FlagDocumentAsync(
///     documentId,
///     FlagReason.EntityValueChanged,
///     options);
///
/// // Later, resolve the flag
/// await flagger.ResolveFlagAsync(
///     flag.FlagId,
///     FlagResolution.UpdatedWithGraphChanges);
/// </code>
/// </example>
public interface IDocumentFlagger
{
    /// <summary>
    /// Flags a document for review.
    /// </summary>
    /// <param name="documentId">The document to flag.</param>
    /// <param name="reason">The reason for the flag.</param>
    /// <param name="options">Configuration for the flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The created <see cref="DocumentFlag"/> with generated ID and timestamp.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Flag creation process:
    /// </para>
    /// <list type="number">
    ///   <item>Generate unique flag ID.</item>
    ///   <item>Create flag record with provided options.</item>
    ///   <item>Store flag in the flag store.</item>
    ///   <item>Publish <see cref="Events.DocumentFlaggedEvent"/> if notifications enabled.</item>
    /// </list>
    /// </remarks>
    Task<DocumentFlag> FlagDocumentAsync(
        Guid documentId,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Flags multiple documents for review.
    /// </summary>
    /// <param name="documentIds">The documents to flag.</param>
    /// <param name="reason">The reason for the flags.</param>
    /// <param name="options">Configuration for the flags.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of created <see cref="DocumentFlag"/> records, one per document.
    /// </returns>
    /// <remarks>
    /// LOGIC: Creates flags for each document using the same reason
    /// and options. More efficient than calling <see cref="FlagDocumentAsync"/>
    /// multiple times when flagging in bulk.
    /// </remarks>
    Task<IReadOnlyList<DocumentFlag>> FlagDocumentsAsync(
        IReadOnlyList<Guid> documentIds,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a flag with the specified resolution.
    /// </summary>
    /// <param name="flagId">The flag ID to resolve.</param>
    /// <param name="resolution">How the flag was resolved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the flag was resolved, false if not found.</returns>
    /// <remarks>
    /// LOGIC: Updates the flag's status to <see cref="FlagStatus.Resolved"/>,
    /// records the resolution type, and sets the resolution timestamp.
    /// </remarks>
    Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves multiple flags with the same resolution.
    /// </summary>
    /// <param name="flagIds">The flag IDs to resolve.</param>
    /// <param name="resolution">How the flags were resolved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of flags successfully resolved.</returns>
    /// <remarks>
    /// LOGIC: Resolves each flag individually. Returns the count of
    /// flags that were found and resolved. Flags not found are skipped.
    /// </remarks>
    Task<int> ResolveFlagsAsync(
        IReadOnlyList<Guid> flagIds,
        FlagResolution resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific flag by ID.
    /// </summary>
    /// <param name="flagId">The flag ID to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="DocumentFlag"/>, or null if not found.</returns>
    /// <remarks>
    /// LOGIC: Retrieves the flag record from the store. Used for
    /// displaying flag details or checking current status.
    /// </remarks>
    Task<DocumentFlag?> GetFlagAsync(
        Guid flagId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets pending flags for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="DocumentFlag"/> records with
    /// <see cref="FlagStatus.Pending"/> or <see cref="FlagStatus.Acknowledged"/>
    /// status for the specified document.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the flag store for all unresolved flags for
    /// the document, ordered by priority (highest first) then creation date.
    /// </remarks>
    Task<IReadOnlyList<DocumentFlag>> GetPendingFlagsAsync(
        Guid documentId,
        CancellationToken ct = default);
}
