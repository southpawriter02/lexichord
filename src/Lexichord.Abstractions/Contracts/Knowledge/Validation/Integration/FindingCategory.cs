// =============================================================================
// File: FindingCategory.cs
// Project: Lexichord.Abstractions
// Description: Category grouping for unified findings.
// =============================================================================
// LOGIC: Maps validator IDs (schema, axiom, consistency) and linter rule
//   categories (Terminology, Formatting, Syntax) to common groupings for
//   unified display and filtering.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Category for grouping <see cref="UnifiedFinding"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Categories span both validation and linter domains. The first three
/// (Schema, Axiom, Consistency) correspond to CKVS validators; the remaining
/// (Style, Grammar, Spelling) correspond to linter rule types.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public enum FindingCategory
{
    /// <summary>Schema validation issues (missing fields, type mismatches).</summary>
    Schema,

    /// <summary>Axiom/rule violations (business rule contradictions).</summary>
    Axiom,

    /// <summary>Consistency/contradiction issues across the document.</summary>
    Consistency,

    /// <summary>Style guide violations (terminology, formatting, syntax).</summary>
    Style,

    /// <summary>Grammar issues (future grammar linter).</summary>
    Grammar,

    /// <summary>Spelling issues (future spell checker).</summary>
    Spelling
}
