// -----------------------------------------------------------------------
// <copyright file="ValidationStatus.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Validation status for a fix suggestion, indicating whether the fix resolves
/// the original violation without introducing new issues.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The validation status is determined by running the suggested fix
/// through the linting system to verify:
/// <list type="bullet">
///   <item><description>The original violation is resolved</description></item>
///   <item><description>No new violations are introduced</description></item>
///   <item><description>Semantic meaning is preserved</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public enum ValidationStatus
{
    /// <summary>
    /// Fix is valid — resolves the violation without introducing new issues.
    /// </summary>
    /// <remarks>
    /// The fix successfully addresses the original style violation and passes
    /// all validation checks. It can be safely applied to the document.
    /// </remarks>
    Valid = 0,

    /// <summary>
    /// Fix resolves the violation but has warnings that should be reviewed.
    /// </summary>
    /// <remarks>
    /// The fix addresses the original violation but may have minor issues such as:
    /// <list type="bullet">
    ///   <item><description>Significant change in text length</description></item>
    ///   <item><description>Possible semantic shift (low similarity score)</description></item>
    ///   <item><description>Introduction of informational-level violations</description></item>
    /// </list>
    /// User review is recommended before applying.
    /// </remarks>
    ValidWithWarnings = 1,

    /// <summary>
    /// Fix is invalid — doesn't resolve the violation or introduces new issues.
    /// </summary>
    /// <remarks>
    /// The fix should not be applied because:
    /// <list type="bullet">
    ///   <item><description>The original violation is still present</description></item>
    ///   <item><description>New style violations were introduced</description></item>
    ///   <item><description>The semantic meaning was significantly altered</description></item>
    /// </list>
    /// Regeneration with different parameters or user guidance is recommended.
    /// </remarks>
    Invalid = 2,

    /// <summary>
    /// Validation could not be completed due to an error.
    /// </summary>
    /// <remarks>
    /// The validation process encountered an error such as:
    /// <list type="bullet">
    ///   <item><description>Linting service unavailable</description></item>
    ///   <item><description>Timeout during validation</description></item>
    ///   <item><description>Invalid text or document state</description></item>
    /// </list>
    /// The fix may still be valid; manual review is required.
    /// </remarks>
    ValidationFailed = 3
}
