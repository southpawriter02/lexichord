using YamlDotNet.Serialization;

namespace Lexichord.Modules.Style.Yaml;

/// <summary>
/// DTO for YAML stylesheet deserialization.
/// </summary>
/// <remarks>
/// LOGIC: This class maps YAML snake_case properties to C# PascalCase.
/// YamlDotNet deserializes directly into this mutable class, which is
/// then validated and converted to the immutable StyleSheet domain object.
///
/// This separation allows:
/// - Flexible YAML parsing with helpful error messages
/// - Validation before domain object construction
/// - Clean separation of serialization concerns from domain logic
/// </remarks>
public sealed class YamlStyleSheet
{
    /// <summary>
    /// Display name for the style sheet.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of the style guide.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Semantic version string (e.g., "1.0.0").
    /// </summary>
    [YamlMember(Alias = "version")]
    public string? Version { get; set; }

    /// <summary>
    /// Author or team name.
    /// </summary>
    [YamlMember(Alias = "author")]
    public string? Author { get; set; }

    /// <summary>
    /// Base style sheet to extend ("default" for built-in).
    /// </summary>
    [YamlMember(Alias = "extends")]
    public string? Extends { get; set; }

    /// <summary>
    /// Collection of style rules.
    /// </summary>
    [YamlMember(Alias = "rules")]
    public List<YamlRule>? Rules { get; set; }
}

/// <summary>
/// DTO for individual style rule deserialization.
/// </summary>
/// <remarks>
/// LOGIC: All properties are nullable to allow validation to detect
/// missing required fields with specific error messages.
///
/// Required fields: id, name, description, pattern
/// Optional fields with defaults: category (Terminology), severity (Warning),
///                                 pattern_type (Regex), enabled (true)
/// </remarks>
public sealed class YamlRule
{
    /// <summary>
    /// Unique rule identifier in kebab-case (e.g., "no-jargon").
    /// </summary>
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Detailed explanation of what this rule checks.
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Category for organization (terminology, formatting, syntax).
    /// </summary>
    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    /// <summary>
    /// Severity level (error, warning, info, hint).
    /// </summary>
    [YamlMember(Alias = "severity")]
    public string? Severity { get; set; }

    /// <summary>
    /// The pattern to match.
    /// </summary>
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// How to interpret the pattern (regex, literal, starts_with, etc.).
    /// </summary>
    [YamlMember(Alias = "pattern_type")]
    public string? PatternType { get; set; }

    /// <summary>
    /// Optional suggestion shown to user for fixing violations.
    /// </summary>
    [YamlMember(Alias = "suggestion")]
    public string? Suggestion { get; set; }

    /// <summary>
    /// Whether this rule is enabled (default: true).
    /// </summary>
    [YamlMember(Alias = "enabled")]
    public bool? Enabled { get; set; }
}
