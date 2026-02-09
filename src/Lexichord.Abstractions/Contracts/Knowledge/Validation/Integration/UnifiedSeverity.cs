// =============================================================================
// File: UnifiedSeverity.cs
// Project: Lexichord.Abstractions
// Description: Unified severity levels across validation and linter sources.
// =============================================================================
// LOGIC: Maps ValidationSeverity (Info=0, Warning=1, Error=2) and
//   ViolationSeverity (Error=0, Warning=1, Info=2, Hint=3) to a single
//   consistent model for UI display and filtering.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Unified severity levels for findings from any source.
/// </summary>
/// <remarks>
/// <para>
/// Provides a single severity model that spans both the CKVS Validation Engine
/// (<see cref="ValidationSeverity"/>) and the style linter
/// (<see cref="ViolationSeverity"/>). The <see cref="IUnifiedFindingAdapter"/>
/// normalizes source-specific severities to this enum.
/// </para>
/// <para>
/// <b>Ordering:</b> Error (most severe) → Hint (least severe). Numeric values
/// follow the same convention as <see cref="ViolationSeverity"/> for consistency.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public enum UnifiedSeverity
{
    /// <summary>Critical error that blocks publication.</summary>
    Error = 0,

    /// <summary>Warning that should be addressed.</summary>
    Warning = 1,

    /// <summary>Informational suggestion.</summary>
    Info = 2,

    /// <summary>Hint or style suggestion.</summary>
    Hint = 3
}
