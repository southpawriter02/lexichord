// -----------------------------------------------------------------------
// <copyright file="SimplificationValidation.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the result of validating a <see cref="SimplificationRequest"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Request validation occurs before invoking the LLM to catch
/// configuration errors early and provide actionable feedback. Validation checks:
/// </para>
/// <list type="bullet">
///   <item><description>Text is not null, empty, or whitespace</description></item>
///   <item><description>Text length is within acceptable bounds (≤50,000 characters)</description></item>
///   <item><description>Target is not null and has valid parameters</description></item>
///   <item><description>Strategy is a valid enum value</description></item>
///   <item><description>Timeout is positive and reasonable (≤5 minutes)</description></item>
/// </list>
/// <para>
/// <b>UI Integration:</b>
/// Validation results are checked before displaying the simplification preview.
/// Errors are shown as validation messages in the UI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <param name="IsValid">
/// <c>true</c> if the request passed all validation checks; otherwise, <c>false</c>.
/// </param>
/// <param name="Errors">
/// Collection of validation error messages. Empty if <paramref name="IsValid"/> is <c>true</c>.
/// Each error is a user-facing message that can be displayed in the UI.
/// </param>
/// <param name="Warnings">
/// Collection of validation warning messages. Warnings do not prevent processing
/// but indicate potential issues (e.g., text is very long, target may be difficult to achieve).
/// </param>
/// <example>
/// <code>
/// // Validating a request before processing
/// var validation = pipeline.ValidateRequest(request);
///
/// if (!validation.IsValid)
/// {
///     foreach (var error in validation.Errors)
///     {
///         Console.WriteLine($"Error: {error}");
///     }
///     return; // Don't proceed
/// }
///
/// // Check for warnings
/// if (validation.HasWarnings)
/// {
///     foreach (var warning in validation.Warnings)
///     {
///         Console.WriteLine($"Warning: {warning}");
///     }
/// }
///
/// // Proceed with simplification
/// var result = await pipeline.SimplifyAsync(request, ct);
/// </code>
/// </example>
/// <seealso cref="SimplificationRequest"/>
/// <seealso cref="ISimplificationPipeline"/>
public record SimplificationValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Gets a value indicating whether there are any warnings.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Warnings"/> contains one or more items; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for quickly checking warning presence.
    /// </remarks>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Gets the total number of issues (errors + warnings).
    /// </summary>
    /// <value>
    /// The sum of <see cref="Errors"/> count and <see cref="Warnings"/> count.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for displaying a total issue count in the UI.
    /// </remarks>
    public int TotalIssueCount => Errors.Count + Warnings.Count;

    /// <summary>
    /// Creates a valid result with no errors or warnings.
    /// </summary>
    /// <returns>A <see cref="SimplificationValidation"/> indicating the request is valid.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for the happy path when validation succeeds.
    /// </remarks>
    public static SimplificationValidation Valid() =>
        new(
            IsValid: true,
            Errors: Array.Empty<string>(),
            Warnings: Array.Empty<string>());

    /// <summary>
    /// Creates a valid result with warnings.
    /// </summary>
    /// <param name="warnings">The warning messages.</param>
    /// <returns>A <see cref="SimplificationValidation"/> indicating the request is valid but has warnings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="warnings"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for when validation passes but there are concerns.
    /// </remarks>
    public static SimplificationValidation ValidWithWarnings(IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        return new(
            IsValid: true,
            Errors: Array.Empty<string>(),
            Warnings: warnings.ToList().AsReadOnly());
    }

    /// <summary>
    /// Creates an invalid result with errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A <see cref="SimplificationValidation"/> indicating the request is invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for when validation fails.
    /// </remarks>
    public static SimplificationValidation Invalid(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new(
            IsValid: false,
            Errors: errors.ToList().AsReadOnly(),
            Warnings: Array.Empty<string>());
    }

    /// <summary>
    /// Creates an invalid result with errors and warnings.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <param name="warnings">The warning messages.</param>
    /// <returns>A <see cref="SimplificationValidation"/> indicating the request is invalid.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> or <paramref name="warnings"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for comprehensive validation results with both errors and warnings.
    /// </remarks>
    public static SimplificationValidation InvalidWithWarnings(
        IEnumerable<string> errors,
        IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(warnings);
        return new(
            IsValid: false,
            Errors: errors.ToList().AsReadOnly(),
            Warnings: warnings.ToList().AsReadOnly());
    }
}
