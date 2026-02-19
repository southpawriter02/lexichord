// =============================================================================
// File: ValidationWarning.cs
// Project: Lexichord.Abstractions
// Description: Warning details from extraction validation.
// =============================================================================
// LOGIC: Captures non-blocking issues that should be logged but do not
//   prevent synchronization from proceeding.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Details of a validation warning encountered during extraction validation.
/// </summary>
/// <remarks>
/// <para>
/// Warnings are non-blocking issues that do not prevent sync but should be
/// reviewed for data quality:
/// </para>
/// <list type="bullet">
///   <item><b>Code:</b> Machine-readable warning identifier.</item>
///   <item><b>Message:</b> Human-readable description of the issue.</item>
///   <item><b>Context:</b> Optional entity ID to locate the warning.</item>
/// </list>
/// <para>
/// <b>Warning Codes Convention:</b>
/// - WARN-001: Deprecated entity type (will be removed in future)
/// - WARN-002: Optional property missing (recommended but not required)
/// - WARN-003: Low confidence extraction (may need review)
/// - WARN-004: Duplicate entity detected (may be intentional)
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var warning = new ValidationWarning
/// {
///     Code = "WARN-001",
///     Message = "Entity type 'LegacyEndpoint' is deprecated, use 'Endpoint' instead",
///     EntityId = entity.Id
/// };
/// </code>
/// </example>
public record ValidationWarning
{
    /// <summary>
    /// Machine-readable warning code.
    /// </summary>
    /// <value>A unique identifier for the warning type (e.g., "WARN-001").</value>
    /// <remarks>
    /// LOGIC: Used for programmatic handling and documentation references.
    /// Should be stable across versions for tooling integration.
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable warning message.
    /// </summary>
    /// <value>A descriptive message explaining the warning.</value>
    /// <remarks>
    /// LOGIC: Intended for display to users and logging. Provides guidance
    /// on potential issues without blocking the operation.
    /// </remarks>
    public required string Message { get; init; }

    /// <summary>
    /// ID of the entity that triggered the warning.
    /// </summary>
    /// <value>The entity GUID if the warning is entity-specific, null otherwise.</value>
    /// <remarks>
    /// LOGIC: Enables targeted review. When set, the warning relates to
    /// a specific entity in the extraction result.
    /// </remarks>
    public Guid? EntityId { get; init; }
}
