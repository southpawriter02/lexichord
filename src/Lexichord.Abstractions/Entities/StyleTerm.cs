using Dapper.Contrib.Extensions;

namespace Lexichord.Abstractions.Entities;

/// <summary>
/// Represents a style term entry in the Terminology Database.
/// </summary>
/// <remarks>
/// LOGIC: This entity maps to the style_terms table created in Migration_002_StyleSchema.
/// Terms are the atomic unit of the Lexicon terminology database, representing
/// words or phrases that should be flagged during writing analysis.
///
/// Design decisions:
/// - ExplicitKey: Using Guid for distributed-friendly ID generation
/// - All timestamps are UTC via TIMESTAMPTZ column type
/// - Severity stored as string for human-readable database values
/// </remarks>
[Table("style_terms")]
public record StyleTerm
{
    /// <summary>
    /// Unique identifier for the term.
    /// </summary>
    [ExplicitKey]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Parent style sheet this term belongs to.
    /// </summary>
    public Guid StyleSheetId { get; init; }

    /// <summary>
    /// The text pattern to match (case-sensitive).
    /// </summary>
    /// <remarks>
    /// Maximum length: 255 characters (enforced by database schema).
    /// </remarks>
    public string Term { get; init; } = string.Empty;

    /// <summary>
    /// Suggested replacement text, or null for "avoid this" rules.
    /// </summary>
    /// <remarks>
    /// Maximum length: 500 characters (enforced by database schema).
    /// </remarks>
    public string? Replacement { get; init; }

    /// <summary>
    /// Grouping category for filtering (General, Terminology, Brand, etc.).
    /// </summary>
    /// <remarks>
    /// Maximum length: 100 characters. Defaults to "General".
    /// </remarks>
    public string Category { get; init; } = "General";

    /// <summary>
    /// Violation severity (Error, Warning, Suggestion).
    /// </summary>
    /// <remarks>
    /// Maximum length: 20 characters. Defaults to "Suggestion".
    /// Stored as string for human-readable database values.
    /// </remarks>
    public string Severity { get; init; } = "Suggestion";

    /// <summary>
    /// Whether this term is currently active for analysis.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Optional notes or rationale for this term.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// When the term was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the term was last updated (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
