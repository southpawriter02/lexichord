// =============================================================================
// File: FindingSource.cs
// Project: Lexichord.Abstractions
// Description: Identifies the origin of a unified finding.
// =============================================================================
// LOGIC: Differentiates findings from the CKVS validation engine vs. the
//   Lexichord style/grammar linter so the UI can attribute and filter them.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Identifies the origin of a <see cref="UnifiedFinding"/>.
/// </summary>
/// <remarks>
/// <para>
/// Used for filtering, attribution, and severity normalization. Each source
/// has its own severity model that is mapped to <see cref="UnifiedSeverity"/>
/// by the <see cref="IUnifiedFindingAdapter"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public enum FindingSource
{
    /// <summary>From the CKVS Validation Engine (schema, axiom, consistency).</summary>
    Validation,

    /// <summary>From the Lexichord style linter (terminology, formatting, syntax).</summary>
    StyleLinter,

    /// <summary>From a future grammar linter (reserved for extensibility).</summary>
    GrammarLinter
}
