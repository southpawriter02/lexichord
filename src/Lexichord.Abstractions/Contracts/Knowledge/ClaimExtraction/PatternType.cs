// =============================================================================
// File: PatternType.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of extraction pattern types for claim extraction.
// =============================================================================
// LOGIC: Defines the types of patterns that can be used to extract claims
//   from text: regex patterns, template patterns, and dependency patterns.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// Specifies the type of extraction pattern.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Different pattern types offer varying levels of
/// flexibility and specificity for matching claim structures in text.
/// </para>
/// <para>
/// <b>Pattern Types:</b>
/// <list type="bullet">
/// <item><description><b>Regex:</b> Full regular expressions for complex patterns.</description></item>
/// <item><description><b>Template:</b> Simple templates with {SUBJECT}/{OBJECT} placeholders.</description></item>
/// <item><description><b>Dependency:</b> Patterns based on dependency parse relations.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
public enum PatternType
{
    /// <summary>
    /// Regular expression pattern with named capture groups.
    /// </summary>
    /// <remarks>
    /// Uses <c>(?&lt;subject&gt;...)</c> and <c>(?&lt;object&gt;...)</c>
    /// named groups to identify subject and object spans.
    /// </remarks>
    Regex = 0,

    /// <summary>
    /// Template pattern with {SUBJECT} and {OBJECT} placeholders.
    /// </summary>
    /// <remarks>
    /// Simpler than regex patterns. The template is converted to a
    /// regex internally with placeholders replaced by capture groups.
    /// Example: "{SUBJECT} accepts {OBJECT}"
    /// </remarks>
    Template = 1,

    /// <summary>
    /// Pattern based on dependency parse tree relations.
    /// </summary>
    /// <remarks>
    /// Matches claims based on grammatical structure rather than
    /// surface text. Uses dependency relations like nsubj and dobj
    /// to identify subjects and objects.
    /// </remarks>
    Dependency = 2
}
