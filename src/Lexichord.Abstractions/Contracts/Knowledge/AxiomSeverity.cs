// =============================================================================
// File: AxiomSeverity.cs
// Project: Lexichord.Abstractions
// Description: Defines severity levels for axiom violations.
// =============================================================================
// LOGIC: Axioms can have different severity levels that determine how the
//   system handles violations. Errors block operations (like save/publish),
//   warnings are advisory, and info provides suggestions.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Severity level of an axiom violation.
/// </summary>
/// <remarks>
/// <para>
/// Determines how the system handles violations of the axiom:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Error"/>: Blocks save/publish operations until resolved.</description></item>
///   <item><description><see cref="Warning"/>: Advisory issue that should be addressed but doesn't block.</description></item>
///   <item><description><see cref="Info"/>: Informational suggestion for improvement.</description></item>
/// </list>
/// </remarks>
public enum AxiomSeverity
{
    /// <summary>
    /// Must fix before save/publish. Blocks the operation until resolved.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Should fix but doesn't block operations. Displayed as a warning in UI.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Informational suggestion. Lowest priority, displayed as a hint.
    /// </summary>
    Info = 2
}
