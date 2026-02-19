// =============================================================================
// File: DocToGraphValidationContext.cs
// Project: Lexichord.Abstractions
// Description: Context for extraction validation operations.
// =============================================================================
// LOGIC: Provides configuration and context for validation including the
//   source document ID, validation strictness, and allowed entity types.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Context for extraction validation operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides configuration for <see cref="IExtractionValidator.ValidateAsync"/>:
/// </para>
/// <list type="bullet">
///   <item><b>DocumentId:</b> Links validation to its source document.</item>
///   <item><b>StrictMode:</b> Controls validation strictness level.</item>
///   <item><b>AllowedEntityTypes:</b> Optional whitelist for entity types.</item>
/// </list>
/// <para>
/// <b>Validation Modes:</b>
/// - Strict: Rejects any schema deviation, recommended for production.
/// - Lenient (default): Allows minor deviations, useful for development.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new DocToGraphValidationContext
/// {
///     DocumentId = document.Id,
///     StrictMode = true,
///     AllowedEntityTypes = ["Endpoint", "Parameter", "Component"]
/// };
/// var result = await validator.ValidateAsync(extraction, context, ct);
/// </code>
/// </example>
public record DocToGraphValidationContext
{
    /// <summary>
    /// ID of the document being validated.
    /// </summary>
    /// <value>The document GUID for context in error messages.</value>
    /// <remarks>
    /// LOGIC: Included in error messages for traceability. Optional
    /// if validation is performed outside a document sync context.
    /// </remarks>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Whether to use strict validation mode.
    /// </summary>
    /// <value>True for strict mode, false for lenient mode (default).</value>
    /// <remarks>
    /// LOGIC: Strict mode:
    /// - All entity types must be registered in schema
    /// - All property names must match schema definitions
    /// - Property types must exactly match
    /// Lenient mode:
    /// - Unrecognized types generate warnings instead of errors
    /// - Extra properties are preserved
    /// - Minor type mismatches are coerced
    /// </remarks>
    public bool StrictMode { get; init; } = false;

    /// <summary>
    /// Whitelist of allowed entity types.
    /// </summary>
    /// <value>
    /// A set of entity type names to allow. Null means all types allowed.
    /// </value>
    /// <remarks>
    /// LOGIC: When specified, only entities with types in this set are
    /// considered valid. Used to filter extractions to specific entity
    /// categories (e.g., only API-related types).
    /// </remarks>
    public IReadOnlySet<string>? AllowedEntityTypes { get; init; }

    /// <summary>
    /// Workspace ID for schema lookup.
    /// </summary>
    /// <value>The workspace GUID for workspace-specific schema rules.</value>
    /// <remarks>
    /// LOGIC: Some workspaces may have custom schema extensions or
    /// restrictions. When set, validation considers workspace-specific
    /// rules in addition to global schema.
    /// </remarks>
    public Guid? WorkspaceId { get; init; }

    /// <summary>
    /// Creates a default validation context.
    /// </summary>
    /// <returns>A context with lenient validation settings.</returns>
    public static DocToGraphValidationContext Default => new();

    /// <summary>
    /// Creates a strict validation context.
    /// </summary>
    /// <param name="documentId">Optional document ID.</param>
    /// <returns>A context with strict validation settings.</returns>
    public static DocToGraphValidationContext Strict(Guid? documentId = null)
        => new() { DocumentId = documentId, StrictMode = true };
}
