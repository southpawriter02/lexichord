// =============================================================================
// File: ValidationSeverity.cs
// Project: Lexichord.Abstractions
// Description: Severity levels for validation errors during extraction validation.
// =============================================================================
// LOGIC: Categorizes validation issues by impact level to enable appropriate
//   handling - warnings can be logged, errors block sync, critical issues
//   require immediate attention.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Severity level for validation errors during extraction validation.
/// </summary>
/// <remarks>
/// <para>
/// Categorizes validation issues by their impact on the sync operation:
/// </para>
/// <list type="bullet">
///   <item><b>Warning:</b> Non-blocking issues that should be logged.</item>
///   <item><b>Error:</b> Issues that prevent sync without auto-correction.</item>
///   <item><b>Critical:</b> Severe issues that always block sync.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
public enum ValidationSeverity
{
    /// <summary>
    /// Non-blocking issue that should be logged but does not prevent sync.
    /// </summary>
    /// <remarks>
    /// LOGIC: Warnings are informational. The sync operation continues,
    /// but the warning is included in the result for review.
    /// Examples: Missing optional property, deprecated entity type.
    /// </remarks>
    Warning = 0,

    /// <summary>
    /// Issue that blocks sync unless auto-correction is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Errors indicate problems that should normally be fixed.
    /// If <see cref="DocToGraphSyncOptions.AutoCorrectErrors"/> is true,
    /// the service attempts automatic correction. Otherwise, sync fails.
    /// Examples: Entity type mismatch, invalid relationship reference.
    /// </remarks>
    Error = 1,

    /// <summary>
    /// Severe issue that always blocks sync, regardless of settings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Critical errors cannot be auto-corrected and always
    /// cause sync to fail. Manual intervention is required.
    /// Examples: Circular relationship, schema violation, data corruption.
    /// </remarks>
    Critical = 2
}
