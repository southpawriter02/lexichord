// =============================================================================
// File: AxiomEntity.cs
// Project: Lexichord.Modules.Knowledge
// Description: Database entity for axiom storage.
// =============================================================================
// LOGIC: Maps between the Axiom domain record and the PostgreSQL axioms table.
//   - Column names use snake_case convention for PostgreSQL.
//   - Rules and Tags are stored as JSONB and serialized/deserialized.
//   - Version column supports optimistic concurrency.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Database entity for axiom storage.
/// </summary>
/// <remarks>
/// <para>
/// This internal record maps directly to the <c>Axioms</c> database table.
/// It is used only within the repository layer and should not be exposed
/// to other modules.
/// </para>
/// <para>
/// <b>Column Mapping:</b> Properties use PascalCase but are mapped to
/// snake_case columns via Dapper column aliases in SQL queries.
/// </para>
/// </remarks>
internal sealed record AxiomEntity
{
    /// <summary>
    /// Gets the unique axiom identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable axiom name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional axiom description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the target entity type this axiom applies to.
    /// </summary>
    public required string TargetType { get; init; }

    /// <summary>
    /// Gets the target kind (Entity, Relationship, Claim).
    /// </summary>
    public required string TargetKind { get; init; }

    /// <summary>
    /// Gets the JSON-serialized rules array.
    /// </summary>
    public required string RulesJson { get; init; }

    /// <summary>
    /// Gets the severity level as a string.
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets the optional category for grouping.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the JSON-serialized tags array.
    /// </summary>
    public required string TagsJson { get; init; }

    /// <summary>
    /// Gets whether the axiom is enabled for validation.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// Gets the optional source file path.
    /// </summary>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Gets the schema version for forward compatibility.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the version number for optimistic concurrency.
    /// </summary>
    public required int Version { get; init; }
}
