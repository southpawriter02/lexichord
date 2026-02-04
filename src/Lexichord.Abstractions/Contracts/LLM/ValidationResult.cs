// -----------------------------------------------------------------------
// <copyright file="ValidationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Result of validating template variables.
/// Provides detailed information about validation failures and warnings.
/// </summary>
/// <remarks>
/// <para>
/// Validation results are returned by <see cref="IPromptRenderer.ValidateVariables"/>
/// to indicate whether a template can be rendered with the provided variables.
/// </para>
/// <para>
/// A result is considered valid (<see cref="IsValid"/> = <see langword="true"/>) when
/// all required variables are present. Warnings do not affect validity but may indicate
/// potential issues such as unused variables.
/// </para>
/// <para>
/// Use the static factory methods <see cref="Success()"/>, <see cref="WithWarnings"/>,
/// and <see cref="Failure(IEnumerable{string})"/> to create instances.
/// </para>
/// </remarks>
/// <param name="IsValid">True if all required variables are present.</param>
/// <param name="MissingVariables">List of required variables not provided.</param>
/// <param name="Warnings">Non-fatal validation warnings.</param>
/// <example>
/// <code>
/// // Check validation before rendering
/// var result = renderer.ValidateVariables(template, variables);
/// if (result.IsValid)
/// {
///     var messages = renderer.RenderMessages(template, variables);
/// }
/// else
/// {
///     Console.WriteLine(result.ErrorMessage);
/// }
///
/// // Or use ThrowIfInvalid for exception-based flow
/// result.ThrowIfInvalid(template.TemplateId);
/// </code>
/// </example>
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> MissingVariables,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Gets the list of required variables that were not provided.
    /// </summary>
    /// <value>A read-only list of missing variable names. Never null.</value>
    public IReadOnlyList<string> MissingVariables { get; init; } =
        MissingVariables ?? Array.Empty<string>();

    /// <summary>
    /// Gets the list of non-fatal validation warnings.
    /// </summary>
    /// <value>A read-only list of warning messages. Never null.</value>
    public IReadOnlyList<string> Warnings { get; init; } =
        Warnings ?? Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result with no warnings.
    /// </summary>
    /// <returns>A new <see cref="ValidationResult"/> with <see cref="IsValid"/> = <see langword="true"/>.</returns>
    /// <remarks>
    /// Use this factory method when validation passes with no issues.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (allRequiredVariablesPresent)
    /// {
    ///     return ValidationResult.Success();
    /// }
    /// </code>
    /// </example>
    public static ValidationResult Success()
        => new(true, Array.Empty<string>(), Array.Empty<string>());

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    /// <param name="warnings">Non-fatal warnings to include in the result.</param>
    /// <returns>A new <see cref="ValidationResult"/> with <see cref="IsValid"/> = <see langword="true"/> and the specified warnings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="warnings"/> is null.</exception>
    /// <remarks>
    /// Use this factory method when validation passes but there are non-critical issues
    /// to report, such as unused variables or deprecated syntax.
    /// </remarks>
    /// <example>
    /// <code>
    /// var warnings = new[] { "Variable 'unused_var' is defined but never used" };
    /// return ValidationResult.WithWarnings(warnings);
    /// </code>
    /// </example>
    public static ValidationResult WithWarnings(IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        return new(true, Array.Empty<string>(), warnings.ToList().AsReadOnly());
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="missingVariables">List of required variables that were not provided.</param>
    /// <returns>A new <see cref="ValidationResult"/> with <see cref="IsValid"/> = <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="missingVariables"/> is null.</exception>
    /// <remarks>
    /// Use this factory method when validation fails due to missing required variables.
    /// </remarks>
    /// <example>
    /// <code>
    /// var missing = template.RequiredVariables
    ///     .Where(v => !variables.ContainsKey(v))
    ///     .ToList();
    ///
    /// if (missing.Count > 0)
    /// {
    ///     return ValidationResult.Failure(missing);
    /// }
    /// </code>
    /// </example>
    public static ValidationResult Failure(IEnumerable<string> missingVariables)
    {
        ArgumentNullException.ThrowIfNull(missingVariables);
        return new(false, missingVariables.ToList().AsReadOnly(), Array.Empty<string>());
    }

    /// <summary>
    /// Creates a failed validation result with warnings.
    /// </summary>
    /// <param name="missingVariables">List of required variables that were not provided.</param>
    /// <param name="warnings">Non-fatal warnings to include in the result.</param>
    /// <returns>A new <see cref="ValidationResult"/> with <see cref="IsValid"/> = <see langword="false"/> and the specified warnings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="missingVariables"/> or <paramref name="warnings"/> is null.</exception>
    /// <remarks>
    /// Use this factory method when validation fails and there are also non-critical warnings.
    /// </remarks>
    /// <example>
    /// <code>
    /// return ValidationResult.Failure(
    ///     missingVariables: new[] { "user_input" },
    ///     warnings: new[] { "Variable 'extra' is not defined in the template" });
    /// </code>
    /// </example>
    public static ValidationResult Failure(
        IEnumerable<string> missingVariables,
        IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(missingVariables);
        ArgumentNullException.ThrowIfNull(warnings);
        return new(false, missingVariables.ToList().AsReadOnly(), warnings.ToList().AsReadOnly());
    }

    /// <summary>
    /// Throws a <see cref="TemplateValidationException"/> if validation failed.
    /// </summary>
    /// <param name="templateId">The template ID to include in the exception message.</param>
    /// <exception cref="TemplateValidationException">Thrown when <see cref="IsValid"/> is <see langword="false"/>.</exception>
    /// <remarks>
    /// Use this method for exception-based flow control when validation failure
    /// should halt execution. For conditional handling, check <see cref="IsValid"/> instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = renderer.ValidateVariables(template, variables);
    /// result.ThrowIfInvalid(template.TemplateId);
    ///
    /// // Only reached if validation passed
    /// var messages = renderer.RenderMessages(template, variables);
    /// </code>
    /// </example>
    public void ThrowIfInvalid(string templateId)
    {
        if (!IsValid)
        {
            throw new TemplateValidationException(templateId, MissingVariables);
        }
    }

    /// <summary>
    /// Gets a formatted error message for display when validation fails.
    /// </summary>
    /// <value>
    /// A human-readable error message listing missing variables, or an empty string if validation passed.
    /// </value>
    /// <remarks>
    /// This message is suitable for display in UI error messages or logging.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!result.IsValid)
    /// {
    ///     Console.WriteLine(result.ErrorMessage);
    ///     // Output: "Missing required variables: user_input, context"
    /// }
    /// </code>
    /// </example>
    public string ErrorMessage => IsValid
        ? string.Empty
        : $"Missing required variables: {string.Join(", ", MissingVariables)}";

    /// <summary>
    /// Gets a value indicating whether there are any warnings.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Warnings"/> contains any items; otherwise, <see langword="false"/>.
    /// </value>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there are any missing variables.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="MissingVariables"/> contains any items; otherwise, <see langword="false"/>.
    /// </value>
    public bool HasMissingVariables => MissingVariables.Count > 0;
}
