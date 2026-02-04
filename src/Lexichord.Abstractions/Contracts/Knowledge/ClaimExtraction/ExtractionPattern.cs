// =============================================================================
// File: ExtractionPattern.cs
// Project: Lexichord.Abstractions
// Description: A pattern for extracting claims from text.
// =============================================================================
// LOGIC: Defines configurable patterns for claim extraction. Patterns can
//   be regex-based, template-based, or dependency-based, and specify the
//   predicate and entity types for matched claims.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// A pattern for extracting claims from text.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Defines configurable patterns that identify claim structures
/// in text. Patterns specify how to match subjects, objects, and predicates.
/// </para>
/// <para>
/// <b>Pattern Types:</b>
/// <list type="bullet">
/// <item><description><b>Regex:</b> Regular expression with named groups.</description></item>
/// <item><description><b>Template:</b> Simple pattern with {SUBJECT}/{OBJECT} placeholders.</description></item>
/// <item><description><b>Dependency:</b> Matches based on grammatical relations.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Template pattern example
/// var pattern = new ExtractionPattern
/// {
///     Id = "endpoint-accepts-param",
///     Name = "Endpoint Accepts Parameter",
///     Type = PatternType.Template,
///     Template = "{SUBJECT} accepts {OBJECT}",
///     Predicate = "ACCEPTS",
///     SubjectType = "Endpoint",
///     ObjectType = "Parameter",
///     BaseConfidence = 0.85f
/// };
/// </code>
/// </example>
public record ExtractionPattern
{
    /// <summary>
    /// Gets the unique pattern identifier.
    /// </summary>
    /// <value>A stable ID used for tracking which pattern matched.</value>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name for this pattern.
    /// </summary>
    /// <value>Human-readable name for debugging and logging.</value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of what this pattern extracts.
    /// </summary>
    /// <value>Optional description for documentation.</value>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the pattern type (Regex, Template, or Dependency).
    /// </summary>
    public PatternType Type { get; init; }

    /// <summary>
    /// Gets the regex pattern (for <see cref="PatternType.Regex"/>).
    /// </summary>
    /// <value>
    /// A regex with named groups <c>(?&lt;subject&gt;...)</c> and
    /// <c>(?&lt;object&gt;...)</c>.
    /// </value>
    public string? RegexPattern { get; init; }

    /// <summary>
    /// Gets the template pattern (for <see cref="PatternType.Template"/>).
    /// </summary>
    /// <value>
    /// A template string with {SUBJECT} and {OBJECT} placeholders.
    /// Example: "{SUBJECT} accepts {OBJECT}"
    /// </value>
    public string? Template { get; init; }

    /// <summary>
    /// Gets the dependency pattern (for <see cref="PatternType.Dependency"/>).
    /// </summary>
    public DependencyPattern? DependencyPattern { get; init; }

    /// <summary>
    /// Gets the predicate to assign to matched claims.
    /// </summary>
    /// <value>
    /// The predicate string (e.g., "ACCEPTS", "RETURNS").
    /// Should match values in <c>ClaimPredicate</c>.
    /// </value>
    public required string Predicate { get; init; }

    /// <summary>
    /// Gets the expected entity type for the subject.
    /// </summary>
    /// <value>
    /// The type of entity expected as subject (e.g., "Endpoint", "Parameter").
    /// Use "*" to match any type.
    /// </value>
    public required string SubjectType { get; init; }

    /// <summary>
    /// Gets the expected entity type for the object.
    /// </summary>
    /// <value>
    /// The type of entity expected as object, or "literal" for literal values.
    /// Use "*" to match any type.
    /// </value>
    public required string ObjectType { get; init; }

    /// <summary>
    /// Gets whether the object is a literal value rather than an entity.
    /// </summary>
    /// <value>
    /// If <c>true</c>, the object is treated as a literal value.
    /// </value>
    public bool ObjectIsLiteral { get; init; }

    /// <summary>
    /// Gets the type of literal if <see cref="ObjectIsLiteral"/> is true.
    /// </summary>
    /// <value>
    /// The literal type (e.g., "int", "bool", "string", "float").
    /// </value>
    public string? LiteralType { get; init; }

    /// <summary>
    /// Gets the base confidence for matches from this pattern.
    /// </summary>
    /// <value>A value between 0 and 1. Default is 0.8.</value>
    public float BaseConfidence { get; init; } = 0.8f;

    /// <summary>
    /// Gets the priority of this pattern (higher = matched first).
    /// </summary>
    /// <value>
    /// Priority value for ordering pattern application. Higher priority
    /// patterns are evaluated first.
    /// </value>
    public int Priority { get; init; }

    /// <summary>
    /// Gets whether this pattern is enabled.
    /// </summary>
    /// <value>If <c>false</c>, the pattern is skipped during extraction.</value>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the tags for filtering patterns.
    /// </summary>
    /// <value>Optional tags for categorizing patterns (e.g., "api", "endpoint").</value>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Validates that the pattern is properly configured.
    /// </summary>
    /// <returns><c>true</c> if the pattern is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return Type switch
        {
            PatternType.Regex => !string.IsNullOrEmpty(RegexPattern),
            PatternType.Template => !string.IsNullOrEmpty(Template),
            PatternType.Dependency => DependencyPattern != null,
            _ => false
        };
    }
}
