// =============================================================================
// File: ClaimChangeType.cs
// Project: Lexichord.Abstractions
// Description: Types of changes that can occur to claims.
// =============================================================================
// LOGIC: Enumerates the different ways a claim can change between versions.
//   Used to categorize modifications and generate appropriate descriptions.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Types of claim changes.
/// </summary>
/// <remarks>
/// <para>
/// Categorizes how claims differ between versions:
/// </para>
/// <list type="bullet">
///   <item><b>Structural:</b> Added, Removed — claim presence changes.</item>
///   <item><b>Content:</b> SubjectChanged, PredicateChanged, ObjectChanged
///     — core triple modifications.</item>
///   <item><b>Metadata:</b> ConfidenceChanged, ValidationChanged,
///     EvidenceChanged — attribute updates.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public enum ClaimChangeType
{
    /// <summary>
    /// Claim was added in the new version.
    /// </summary>
    Added = 0,

    /// <summary>
    /// Claim was removed from the old version.
    /// </summary>
    Removed = 1,

    /// <summary>
    /// Claim subject (entity) changed.
    /// </summary>
    /// <remarks>
    /// Detected when subject surface form or entity ID differs.
    /// </remarks>
    SubjectChanged = 2,

    /// <summary>
    /// Claim predicate (relationship type) changed.
    /// </summary>
    /// <remarks>
    /// Typically indicates a different assertion about the same entities.
    /// </remarks>
    PredicateChanged = 3,

    /// <summary>
    /// Claim object (value or entity) changed.
    /// </summary>
    /// <remarks>
    /// Detected when object type, value, or entity differs.
    /// </remarks>
    ObjectChanged = 4,

    /// <summary>
    /// Claim confidence changed significantly.
    /// </summary>
    /// <remarks>
    /// Only reported when change exceeds IgnoreConfidenceChangeBelow threshold.
    /// </remarks>
    ConfidenceChanged = 5,

    /// <summary>
    /// Claim validation status changed.
    /// </summary>
    /// <remarks>
    /// Examples: Pending → Valid, Valid → Invalid.
    /// </remarks>
    ValidationChanged = 6,

    /// <summary>
    /// Claim evidence (source text location) changed.
    /// </summary>
    /// <remarks>
    /// Indicates the claim was found in a different location or sentence.
    /// </remarks>
    EvidenceChanged = 7
}
