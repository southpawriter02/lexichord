// =============================================================================
// File: ContextModificationType.cs
// Project: Lexichord.Abstractions
// Description: Types of suggested modifications to knowledge context.
// =============================================================================
// LOGIC: When pre-generation validation finds issues, it may suggest
//   modifications to the context to resolve them. This enum categorizes
//   the type of modification suggested.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Type of suggested modification to knowledge context.
/// </summary>
/// <remarks>
/// <para>
/// Used in <see cref="ContextModification"/> to indicate what change
/// should be made to the knowledge context to resolve a validation issue.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
public enum ContextModificationType
{
    /// <summary>
    /// An entity should be added to the context for completeness.
    /// </summary>
    AddEntity = 0,

    /// <summary>
    /// An entity should be removed due to conflicts or irrelevance.
    /// </summary>
    RemoveEntity = 1,

    /// <summary>
    /// An entity's properties should be updated to resolve inconsistency.
    /// </summary>
    UpdateEntity = 2,

    /// <summary>
    /// The entire context should be refreshed (e.g., stale data detected).
    /// </summary>
    RefreshContext = 3
}
