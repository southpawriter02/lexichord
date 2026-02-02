// =============================================================================
// File: AxiomViolation.cs
// Project: Lexichord.Abstractions
// Description: Represents a detected violation of an axiom rule.
// =============================================================================
// LOGIC: When validation finds that an entity/relationship violates an axiom
//   rule, this record captures all the context needed for display and
//   potential auto-fix suggestions.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A violation detected when validating against an <see cref="Axiom"/>.
/// </summary>
/// <remarks>
/// <para>
/// Violations capture complete context about what failed, where, and how to fix it.
/// They are aggregated in <see cref="AxiomValidationResult"/> and can be displayed
/// in the UI with location highlighting and quick-fix suggestions.
/// </para>
/// <example>
/// Creating a violation:
/// <code>
/// var violation = new AxiomViolation
/// {
///     Axiom = axiom,
///     ViolatedRule = axiom.Rules[0],
///     EntityId = entityId,
///     PropertyName = "method",
///     ActualValue = null,
///     ExpectedValue = "non-null value",
///     Message = "Endpoint missing required 'method' property",
///     Severity = AxiomSeverity.Error
/// };
/// </code>
/// </example>
/// </remarks>
public record AxiomViolation
{
    /// <summary>
    /// Unique identifier for this violation instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The axiom that was violated.
    /// </summary>
    public required Axiom Axiom { get; init; }

    /// <summary>
    /// The specific rule within the axiom that failed.
    /// </summary>
    public required AxiomRule ViolatedRule { get; init; }

    /// <summary>
    /// Entity ID that caused the violation, if the axiom targets entities.
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Relationship ID that caused the violation, if the axiom targets relationships.
    /// </summary>
    public Guid? RelationshipId { get; init; }

    /// <summary>
    /// Claim ID that caused the violation, if the axiom targets claims.
    /// </summary>
    public Guid? ClaimId { get; init; }

    /// <summary>
    /// The property name where the violation occurred.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// The actual value that caused the violation.
    /// </summary>
    public object? ActualValue { get; init; }

    /// <summary>
    /// The expected value(s) based on the constraint.
    /// </summary>
    /// <remarks>
    /// For range constraints, this might be "[min, max]".
    /// For one_of constraints, this is the list of allowed values.
    /// </remarks>
    public object? ExpectedValue { get; init; }

    /// <summary>
    /// Human-readable violation message for display in the UI.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity of the violation, inherited from the axiom.
    /// </summary>
    public AxiomSeverity Severity { get; init; }

    /// <summary>
    /// Document ID where the violation occurred, if applicable.
    /// </summary>
    /// <remarks>
    /// Used when validating knowledge extracted from documents.
    /// </remarks>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Text location in the document where the violation occurred.
    /// </summary>
    /// <remarks>
    /// Enables precise highlighting in the editor when the violation
    /// relates to a specific text span.
    /// </remarks>
    public TextSpan? Location { get; init; }

    /// <summary>
    /// Suggested fix for the violation, if available.
    /// </summary>
    public AxiomFix? SuggestedFix { get; init; }

    /// <summary>
    /// Timestamp when the violation was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}
