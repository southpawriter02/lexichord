namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Reasons why a content region should be excluded from linting.
/// </summary>
/// <remarks>
/// LOGIC: ExclusionReason categorizes why a region is skipped:
/// - Code blocks contain programming syntax, not prose
/// - Frontmatter contains metadata, not user content
/// - Future: Could add HTML blocks, math blocks, etc.
///
/// Version: v0.2.7c
/// </remarks>
public enum ExclusionReason
{
    /// <summary>
    /// Fenced code block (``` or ~~~ delimited).
    /// </summary>
    /// <remarks>
    /// LOGIC: Most common code block format in Markdown.
    /// Content between fences is programming code.
    /// </remarks>
    FencedCodeBlock,

    /// <summary>
    /// Inline code span (`backticks`).
    /// </summary>
    /// <remarks>
    /// LOGIC: Single or multiple backtick delimited spans.
    /// Used for variable names, commands, short snippets.
    /// </remarks>
    InlineCode,

    /// <summary>
    /// YAML frontmatter (--- delimited).
    /// </summary>
    /// <remarks>
    /// LOGIC: Document metadata at start of file.
    /// Implemented in v0.2.7c.
    /// </remarks>
    Frontmatter,

    /// <summary>
    /// TOML frontmatter (+++ delimited).
    /// </summary>
    /// <remarks>
    /// LOGIC: Document metadata in TOML format (Hugo style).
    /// Implemented in v0.2.7c.
    /// </remarks>
    TomlFrontmatter,

    /// <summary>
    /// JSON frontmatter ({ on first line).
    /// </summary>
    /// <remarks>
    /// LOGIC: Document metadata in JSON format.
    /// Less common but supported for completeness.
    /// Implemented in v0.2.7c.
    /// </remarks>
    JsonFrontmatter,

    /// <summary>
    /// Indented code block (4+ spaces).
    /// </summary>
    /// <remarks>
    /// LOGIC: CommonMark legacy code block format.
    /// Less common but still valid Markdown.
    /// </remarks>
    IndentedCodeBlock
}
