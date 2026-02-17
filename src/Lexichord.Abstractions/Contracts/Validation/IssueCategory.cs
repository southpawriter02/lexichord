// =============================================================================
// File: IssueCategory.cs
// Project: Lexichord.Abstractions
// Description: Categories for unified validation issues from different sources.
// =============================================================================
// LOGIC: Provides a unified categorization scheme that spans style linting,
//   grammar checking, and CKVS knowledge validation. This enables consistent
//   filtering and grouping in the Tuning Agent UI regardless of issue source.
//
// v0.7.5e: Unified Issue Model (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Categories for unified validation issues, spanning style, grammar, and knowledge validation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This enum provides a unified categorization scheme for issues from
/// multiple validation sources:
/// <list type="bullet">
///   <item><description>Style Linter: Maps <see cref="RuleCategory"/> to Style</description></item>
///   <item><description>Grammar Linter: Maps to Grammar</description></item>
///   <item><description>CKVS Validation: Maps <see cref="FindingCategory"/> to Knowledge</description></item>
///   <item><description>Document Structure: Structure-related issues</description></item>
///   <item><description>Custom: User-defined validation rules</description></item>
/// </list>
/// </para>
/// <para>
/// <b>UI Usage:</b> Used for grouping and filtering in the Tuning Agent review panel,
/// Quick Fix panel, and validation summary views.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
public enum IssueCategory
{
    /// <summary>
    /// Style guide violations from the style linting engine.
    /// </summary>
    /// <remarks>
    /// Includes terminology preferences, formatting rules, and voice/tone guidelines.
    /// Sourced from <see cref="StyleRule"/> violations processed by the Style Deviation Scanner.
    /// </remarks>
    Style = 0,

    /// <summary>
    /// Grammar and spelling issues from the grammar linter.
    /// </summary>
    /// <remarks>
    /// Includes grammatical errors, spelling mistakes, punctuation issues, and
    /// readability concerns detected by grammar checking services.
    /// </remarks>
    Grammar = 1,

    /// <summary>
    /// Knowledge validation findings from the CKVS Validation Engine.
    /// </summary>
    /// <remarks>
    /// Includes schema violations, axiom contradictions, consistency issues,
    /// and fact-checking results from the Claim Knowledge Validation System.
    /// </remarks>
    Knowledge = 2,

    /// <summary>
    /// Document structure issues such as heading hierarchy or section organization.
    /// </summary>
    /// <remarks>
    /// Includes structural problems like improper heading levels, missing sections,
    /// or document outline violations that affect document organization.
    /// </remarks>
    Structure = 3,

    /// <summary>
    /// User-defined custom validation rules.
    /// </summary>
    /// <remarks>
    /// Includes issues from user-created rules, project-specific guidelines,
    /// or plugin-provided validators that don't fit other categories.
    /// </remarks>
    Custom = 4
}
