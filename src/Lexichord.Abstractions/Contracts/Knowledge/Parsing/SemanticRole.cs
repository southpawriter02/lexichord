// =============================================================================
// File: SemanticRole.cs
// Project: Lexichord.Abstractions
// Description: Semantic roles for PropBank-style role labeling.
// =============================================================================
// LOGIC: Defines the semantic roles used in Semantic Role Labeling (SRL).
//   Based on PropBank conventions where ARG0 is typically the agent (doer),
//   ARG1 is the patient/theme (affected), and ARGM-* are modifiers.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// Semantic roles for Semantic Role Labeling (PropBank-style).
/// </summary>
/// <remarks>
/// <para>
/// Semantic Role Labeling (SRL) identifies the predicate-argument structure
/// of a sentence. Each argument is assigned a role relative to the predicate
/// (typically a verb).
/// </para>
/// <para>
/// <b>Core Arguments (ARG0-ARG4):</b>
/// <list type="bullet">
///   <item><see cref="ARG0"/>: Agent — the doer of the action.</item>
///   <item><see cref="ARG1"/>: Patient/Theme — the entity affected.</item>
///   <item><see cref="ARG2"/>-<see cref="ARG4"/>: Verb-specific roles.</item>
/// </list>
/// </para>
/// <para>
/// <b>Modifier Arguments (ARGM-*):</b>
/// <list type="bullet">
///   <item><see cref="ARGM_LOC"/>: Location of the action.</item>
///   <item><see cref="ARGM_TMP"/>: Temporal information.</item>
///   <item><see cref="ARGM_MNR"/>: Manner of the action.</item>
///   <item><see cref="ARGM_CAU"/>: Cause of the action.</item>
///   <item><see cref="ARGM_NEG"/>: Negation marker.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
public enum SemanticRole
{
    /// <summary>
    /// Agent — the entity performing the action.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The endpoint accepts parameters", "endpoint" is ARG0.
    /// </remarks>
    ARG0,

    /// <summary>
    /// Patient/Theme — the entity affected by the action.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The endpoint accepts parameters", "parameters" is ARG1.
    /// </remarks>
    ARG1,

    /// <summary>
    /// Instrument, benefactive, or attribute (verb-specific).
    /// </summary>
    ARG2,

    /// <summary>
    /// Starting point, benefactive, or instrument (verb-specific).
    /// </summary>
    ARG3,

    /// <summary>
    /// Ending point (verb-specific).
    /// </summary>
    ARG4,

    /// <summary>
    /// Direction modifier.
    /// </summary>
    ARGM_DIR,

    /// <summary>
    /// Location modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The server runs in the cloud", "in the cloud" is ARGM_LOC.
    /// </remarks>
    ARGM_LOC,

    /// <summary>
    /// Manner modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The API responds quickly", "quickly" is ARGM_MNR.
    /// </remarks>
    ARGM_MNR,

    /// <summary>
    /// Temporal modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The job runs daily", "daily" is ARGM_TMP.
    /// </remarks>
    ARGM_TMP,

    /// <summary>
    /// Extent modifier.
    /// </summary>
    ARGM_EXT,

    /// <summary>
    /// Purpose modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "Use caching to improve performance", "to improve performance" is ARGM_PRP.
    /// </remarks>
    ARGM_PRP,

    /// <summary>
    /// Cause modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The request failed because of timeout", "because of timeout" is ARGM_CAU.
    /// </remarks>
    ARGM_CAU,

    /// <summary>
    /// Negation modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The endpoint does not accept parameters", "not" is ARGM_NEG.
    /// </remarks>
    ARGM_NEG,

    /// <summary>
    /// Modal modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: In "The API should validate input", "should" is ARGM_MOD.
    /// </remarks>
    ARGM_MOD
}
