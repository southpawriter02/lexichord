// =============================================================================
// File: ClaimExtractionMethod.cs
// Project: Lexichord.Abstractions
// Description: Methods used to extract claims from text.
// =============================================================================
// LOGIC: Identifies the technique used to extract a claim from source text.
//   Different methods have different accuracy characteristics and are used
//   for confidence scoring and debugging extraction issues.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Method used to extract a claim from source text.
/// </summary>
/// <remarks>
/// <para>
/// Claims can be extracted using various NLU techniques, each with different
/// precision and recall characteristics. This enum identifies which method
/// was used, enabling:
/// </para>
/// <list type="bullet">
///   <item>Confidence calibration based on method reliability.</item>
///   <item>Debugging and improving extraction patterns.</item>
///   <item>Filtering claims by extraction source.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public enum ClaimExtractionMethod
{
    /// <summary>
    /// Extracted using pattern-based rule matching.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses predefined regex or template patterns to identify claims.
    /// High precision, lower recall. See <see cref="ClaimEvidence.PatternId"/>
    /// for the specific pattern that matched.
    /// Example pattern: "{ENDPOINT} accepts {PARAMETER}"
    /// </remarks>
    PatternRule,

    /// <summary>
    /// Extracted using semantic role labeling.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses SRL to identify agent (subject), predicate, and patient
    /// (object) roles in sentences. Medium precision and recall.
    /// Implemented using SpaCy's SRL capabilities (v0.5.6f).
    /// </remarks>
    SemanticRoleLabeling,

    /// <summary>
    /// Extracted using dependency parsing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Analyzes syntactic dependencies to extract subject-verb-object
    /// triples. Uses nsubj/dobj/pobj relations from the dependency tree.
    /// Implemented using SpaCy's dependency parser (v0.5.6f).
    /// </remarks>
    DependencyParsing,

    /// <summary>
    /// Extracted using LLM-based inference.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses a large language model to extract claims from complex
    /// or ambiguous sentences. Higher recall but variable precision.
    /// Reserved for future implementation.
    /// </remarks>
    LLMExtraction,

    /// <summary>
    /// Manually entered by a human reviewer.
    /// </summary>
    /// <remarks>
    /// LOGIC: Claim was created or corrected by a human. Highest confidence
    /// but not scalable. Used for ground truth and training data.
    /// </remarks>
    Manual
}
