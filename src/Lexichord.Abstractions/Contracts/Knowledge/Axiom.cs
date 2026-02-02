// =============================================================================
// File: Axiom.cs
// Project: Lexichord.Abstractions
// Description: Defines a domain rule (axiom) for knowledge graph validation.
// =============================================================================
// LOGIC: Axioms are foundational rules that govern the structure and content
//   of knowledge graph elements. They are evaluated during entity/relationship
//   creation and modification to ensure consistency and correctness.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A domain rule that defines constraints for knowledge graph elements.
/// </summary>
/// <remarks>
/// <para>
/// Axioms are the fundamental building blocks for validating knowledge graph
/// content. Each axiom targets a specific entity or relationship type and
/// contains one or more rules that must all pass for the axiom to be satisfied.
/// </para>
/// <para>
/// Axioms can be loaded from YAML files (by the Axiom Loader) or created
/// programmatically. They support categorization, tagging, and conditional
/// enable/disable for flexible validation workflows.
/// </para>
/// <example>
/// Creating an axiom programmatically:
/// <code>
/// var axiom = new Axiom
/// {
///     Id = "endpoint-must-have-method",
///     Name = "Endpoint Method Required",
///     Description = "Every endpoint must specify an HTTP method",
///     TargetType = "Endpoint",
///     TargetKind = AxiomTargetKind.Entity,
///     Severity = AxiomSeverity.Error,
///     Rules = new[]
///     {
///         new AxiomRule
///         {
///             Property = "method",
///             Constraint = AxiomConstraintType.Required
///         }
///     }
/// };
/// </code>
/// </example>
/// </remarks>
public record Axiom
{
    /// <summary>
    /// Unique identifier for this axiom.
    /// </summary>
    /// <remarks>
    /// Should be a kebab-case string like "endpoint-must-have-method".
    /// Used for referencing axioms in configurations and logs.
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name for the axiom.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description of what this axiom enforces.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The entity or relationship type this axiom applies to.
    /// </summary>
    /// <remarks>
    /// Must match a type defined in the Schema Registry.
    /// </remarks>
    public required string TargetType { get; init; }

    /// <summary>
    /// Whether this axiom targets entities, relationships, or claims.
    /// </summary>
    /// <value>Defaults to <see cref="AxiomTargetKind.Entity"/>.</value>
    public AxiomTargetKind TargetKind { get; init; } = AxiomTargetKind.Entity;

    /// <summary>
    /// The rules that define this axiom's constraints.
    /// </summary>
    /// <remarks>
    /// All rules must pass for the axiom to be satisfied. Rules are evaluated
    /// in order, and evaluation stops at the first failure.
    /// </remarks>
    public required IReadOnlyList<AxiomRule> Rules { get; init; }

    /// <summary>
    /// Severity of violations of this axiom.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="AxiomSeverity.Error"/>.
    /// Errors block save/publish; warnings are advisory.
    /// </value>
    public AxiomSeverity Severity { get; init; } = AxiomSeverity.Error;

    /// <summary>
    /// Category for grouping related axioms in the UI.
    /// </summary>
    /// <example>"API Documentation", "Data Quality", "Security"</example>
    public string? Category { get; init; }

    /// <summary>
    /// Tags for filtering and searching axioms.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this axiom is currently enabled for validation.
    /// </summary>
    /// <value>Defaults to <c>true</c>. Disabled axioms are skipped during validation.</value>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Source file path if this axiom was loaded from YAML.
    /// </summary>
    /// <remarks>
    /// Used for error reporting and to support reload functionality.
    /// </remarks>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Timestamp when the axiom was created or loaded.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Schema version of the axiom definition format.
    /// </summary>
    /// <value>Defaults to "1.0". Used for compatibility checking.</value>
    public string SchemaVersion { get; init; } = "1.0";
}
