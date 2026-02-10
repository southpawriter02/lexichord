// =============================================================================
// File: ContextIssueSeverity.cs
// Project: Lexichord.Abstractions
// Description: Severity levels for pre-generation validation context issues.
// =============================================================================
// LOGIC: Determines how the system handles issues found during pre-generation
//   validation. Error-level issues block generation entirely, warnings allow
//   generation but notify the user, and info issues are purely advisory.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Severity level of a pre-generation validation issue.
/// </summary>
/// <remarks>
/// <para>
/// Controls whether generation proceeds or is blocked based on the issues
/// found during pre-validation of knowledge context and user request.
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Error"/>: Blocks LLM generation until resolved.</description></item>
///   <item><description><see cref="Warning"/>: Allows generation but warns the user.</description></item>
///   <item><description><see cref="Info"/>: Informational, no action required.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
public enum ContextIssueSeverity
{
    /// <summary>
    /// Blocks LLM generation. Must be resolved before the Co-pilot can proceed.
    /// </summary>
    Error = 0,

    /// <summary>
    /// Allows generation but warns the user about potential issues.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Informational message. No action required, displayed for awareness.
    /// </summary>
    Info = 2
}
