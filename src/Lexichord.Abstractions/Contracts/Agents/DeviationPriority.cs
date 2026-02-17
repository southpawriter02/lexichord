// -----------------------------------------------------------------------
// <copyright file="DeviationPriority.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Priority levels for style deviations, used for review ordering and UI display.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Priority is mapped from <see cref="ViolationSeverity"/> with additional
/// context from rule characteristics. Higher priority deviations should be addressed first.
/// </para>
/// <para>
/// <b>Mapping from ViolationSeverity:</b>
/// <list type="bullet">
///   <item><description>Error → Critical</description></item>
///   <item><description>Warning → High</description></item>
///   <item><description>Info → Normal</description></item>
///   <item><description>Hint → Low</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Style Deviation Scanner feature.
/// </para>
/// </remarks>
public enum DeviationPriority
{
    /// <summary>
    /// Minor style suggestion that can be safely ignored.
    /// </summary>
    /// <remarks>
    /// Maps from <see cref="ViolationSeverity.Hint"/>. These are optional improvements
    /// that don't significantly impact document quality.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Standard style violation that should be addressed.
    /// </summary>
    /// <remarks>
    /// Maps from <see cref="ViolationSeverity.Info"/>. These are informational
    /// deviations that improve consistency when fixed.
    /// </remarks>
    Normal = 1,

    /// <summary>
    /// Significant style issue that should be addressed before publishing.
    /// </summary>
    /// <remarks>
    /// Maps from <see cref="ViolationSeverity.Warning"/>. These deviations
    /// may affect readability or brand consistency.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical violation that must be fixed immediately.
    /// </summary>
    /// <remarks>
    /// Maps from <see cref="ViolationSeverity.Error"/>. These are severe deviations
    /// that significantly impact document quality or violate mandatory guidelines.
    /// </remarks>
    Critical = 3
}
