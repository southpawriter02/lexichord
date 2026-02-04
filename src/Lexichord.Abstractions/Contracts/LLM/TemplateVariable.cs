// -----------------------------------------------------------------------
// <copyright file="TemplateVariable.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Metadata about a template variable for documentation and validation.
/// Used for UI display and advanced validation scenarios.
/// </summary>
/// <remarks>
/// <para>
/// Template variables represent placeholders in prompt templates that can be
/// substituted with runtime values. Variables may be required or optional,
/// and optional variables may have default values.
/// </para>
/// <para>
/// Variable names should match the placeholders in the template (without braces).
/// For example, a template containing <c>{{user_input}}</c> would have a variable
/// with <see cref="Name"/> set to <c>"user_input"</c>.
/// </para>
/// </remarks>
/// <param name="Name">Variable name as used in template (without braces).</param>
/// <param name="IsRequired">Whether the variable must be provided for rendering.</param>
/// <param name="Description">Human-readable description of the variable's purpose.</param>
/// <param name="DefaultValue">Default value if not provided (optional variables only).</param>
/// <example>
/// <code>
/// // Create a required variable
/// var required = TemplateVariable.Required("user_input", "The user's question or request");
///
/// // Create an optional variable with a default value
/// var optional = TemplateVariable.Optional("language", "Target language", "English");
///
/// // Direct construction
/// var variable = new TemplateVariable("context", true, "Additional context for the AI");
/// </code>
/// </example>
public record TemplateVariable(
    string Name,
    bool IsRequired,
    string? Description = null,
    string? DefaultValue = null)
{
    /// <summary>
    /// Gets the variable name as used in the template (without braces).
    /// </summary>
    /// <value>The variable name. Never null or whitespace.</value>
    /// <exception cref="ArgumentException">Thrown when the value is null or whitespace.</exception>
    public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
        ? Name
        : throw new ArgumentException("Name cannot be null or whitespace.", nameof(Name));

    /// <summary>
    /// Creates a required variable with the specified name and optional description.
    /// </summary>
    /// <param name="name">The variable name as used in the template.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <returns>A new <see cref="TemplateVariable"/> instance marked as required.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <remarks>
    /// Required variables must be provided when rendering the template.
    /// Rendering will fail with a <see cref="TemplateValidationException"/> if any required
    /// variable is missing.
    /// </remarks>
    /// <example>
    /// <code>
    /// var userInput = TemplateVariable.Required("user_input", "The user's question");
    /// var context = TemplateVariable.Required("context"); // No description
    /// </code>
    /// </example>
    public static TemplateVariable Required(string name, string? description = null)
        => new(name, true, description);

    /// <summary>
    /// Creates an optional variable with the specified name, description, and default value.
    /// </summary>
    /// <param name="name">The variable name as used in the template.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="defaultValue">Default value to use if the variable is not provided.</param>
    /// <returns>A new <see cref="TemplateVariable"/> instance marked as optional.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <remarks>
    /// Optional variables do not cause rendering to fail if not provided.
    /// If a <paramref name="defaultValue"/> is specified, it will be used when the variable
    /// is not provided during rendering.
    /// </remarks>
    /// <example>
    /// <code>
    /// var language = TemplateVariable.Optional("language", "Target language", "English");
    /// var format = TemplateVariable.Optional("format"); // No description or default
    /// </code>
    /// </example>
    public static TemplateVariable Optional(
        string name,
        string? description = null,
        string? defaultValue = null)
        => new(name, false, description, defaultValue);

    /// <summary>
    /// Gets a value indicating whether this variable has a default value.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="DefaultValue"/> is not null; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Only optional variables should have default values. A required variable with a default
    /// value is functionally equivalent to an optional variable.
    /// </remarks>
    public bool HasDefaultValue => DefaultValue is not null;

    /// <summary>
    /// Gets a value indicating whether this variable has a description.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Description"/> is not null or whitespace; otherwise, <see langword="false"/>.
    /// </value>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
}
