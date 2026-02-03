// =============================================================================
// File: DateRangeOption.cs
// Project: Lexichord.Modules.RAG
// Description: Enum defining date range filter options for the search filter panel.
// =============================================================================
// LOGIC: Defines the available date range presets for filtering search results
//        by document modification date. These options map to DateRange factory
//        methods in SearchFilter (v0.5.5a).
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

namespace Lexichord.Modules.RAG.Enums;

/// <summary>
/// Defines the available date range filter options.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DateRangeOption"/> provides preset time ranges for filtering
/// documents by modification date in the search filter panel. Each option
/// corresponds to a <see cref="Abstractions.Contracts.DateRange"/> factory method
/// or custom date input.
/// </para>
/// <para>
/// <b>Option Mapping:</b>
/// <list type="table">
///   <listheader>
///     <term>Option</term>
///     <description>DateRange Factory</description>
///   </listheader>
///   <item>
///     <term><see cref="AnyTime"/></term>
///     <description>No date filtering (returns null)</description>
///   </item>
///   <item>
///     <term><see cref="LastDay"/></term>
///     <description><c>DateRange.LastDays(1)</c></description>
///   </item>
///   <item>
///     <term><see cref="Last7Days"/></term>
///     <description><c>DateRange.LastDays(7)</c></description>
///   </item>
///   <item>
///     <term><see cref="Last30Days"/></term>
///     <description><c>DateRange.LastDays(30)</c></description>
///   </item>
///   <item>
///     <term><see cref="Custom"/></term>
///     <description>User-specified start and end dates</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Date range filtering requires WriterPro tier or higher.
/// Core tier users will see the date range section with a lock icon.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
public enum DateRangeOption
{
    /// <summary>
    /// No date filtering applied. All documents regardless of modification date.
    /// </summary>
    AnyTime,

    /// <summary>
    /// Documents modified in the last 24 hours.
    /// </summary>
    /// <remarks>Maps to <c>DateRange.LastDays(1)</c>.</remarks>
    LastDay,

    /// <summary>
    /// Documents modified in the last 7 days (one week).
    /// </summary>
    /// <remarks>Maps to <c>DateRange.LastDays(7)</c>.</remarks>
    Last7Days,

    /// <summary>
    /// Documents modified in the last 30 days (approximately one month).
    /// </summary>
    /// <remarks>Maps to <c>DateRange.LastDays(30)</c>.</remarks>
    Last30Days,

    /// <summary>
    /// Custom date range with user-specified start and end dates.
    /// </summary>
    /// <remarks>
    /// When selected, the UI should display date picker controls for
    /// entering custom start and end dates.
    /// </remarks>
    Custom
}
