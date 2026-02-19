// =============================================================================
// File: ValidationError.cs
// Project: Lexichord.Abstractions
// Description: Error details from extraction validation.
// =============================================================================
// LOGIC: Captures detailed information about a validation error including
//   error code, message, affected entity/relationship, and severity level.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: ValidationSeverity
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Details of a validation error encountered during extraction validation.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive error information for debugging and user feedback:
/// </para>
/// <list type="bullet">
///   <item><b>Code:</b> Machine-readable error identifier for programmatic handling.</item>
///   <item><b>Message:</b> Human-readable description of the error.</item>
///   <item><b>Context:</b> Optional entity or relationship ID to locate the error.</item>
///   <item><b>Severity:</b> Impact level determining handling behavior.</item>
/// </list>
/// <para>
/// <b>Error Codes Convention:</b>
/// - VAL-001: Invalid entity type
/// - VAL-002: Missing required property
/// - VAL-003: Invalid relationship reference
/// - VAL-004: Schema constraint violation
/// - VAL-005: Circular relationship detected
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var error = new ValidationError
/// {
///     Code = "VAL-001",
///     Message = "Entity type 'UnknownType' is not registered in the schema",
///     EntityId = entity.Id,
///     Severity = ValidationSeverity.Error
/// };
/// </code>
/// </example>
public record ValidationError
{
    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    /// <value>A unique identifier for the error type (e.g., "VAL-001").</value>
    /// <remarks>
    /// LOGIC: Used for programmatic error handling, localization, and
    /// documentation references. Should be stable across versions.
    /// </remarks>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    /// <value>A descriptive message explaining the error.</value>
    /// <remarks>
    /// LOGIC: Intended for display to users and logging. Should include
    /// enough context to understand and fix the issue without revealing
    /// sensitive internal details.
    /// </remarks>
    public required string Message { get; init; }

    /// <summary>
    /// ID of the entity that caused the error.
    /// </summary>
    /// <value>The entity GUID if the error is entity-specific, null otherwise.</value>
    /// <remarks>
    /// LOGIC: Enables targeted error resolution. When set, the error
    /// relates to a specific entity in the extraction result.
    /// </remarks>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// ID of the relationship that caused the error.
    /// </summary>
    /// <value>The relationship GUID if the error is relationship-specific, null otherwise.</value>
    /// <remarks>
    /// LOGIC: Enables targeted error resolution. When set, the error
    /// relates to a specific relationship in the extraction result.
    /// Mutually exclusive with EntityId in most cases.
    /// </remarks>
    public Guid? RelationshipId { get; init; }

    /// <summary>
    /// Severity level of the error.
    /// </summary>
    /// <value>The error severity determining handling behavior.</value>
    /// <remarks>
    /// LOGIC: Determines how the sync operation handles this error:
    /// - Error: Blocks sync unless auto-correction succeeds.
    /// - Critical: Always blocks sync, cannot be auto-corrected.
    /// Defaults to Error for standard validation failures.
    /// </remarks>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}
