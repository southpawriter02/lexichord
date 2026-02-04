// -----------------------------------------------------------------------
// <copyright file="TemplateValidationException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Exception thrown when template rendering fails due to missing required variables.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by <see cref="IPromptRenderer.RenderMessages"/> when
/// required template variables are not provided. It can also be thrown explicitly
/// via <see cref="ValidationResult.ThrowIfInvalid"/>.
/// </para>
/// <para>
/// The exception provides detailed information about which template failed and
/// which required variables were missing, enabling callers to provide meaningful
/// error messages or take corrective action.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var messages = renderer.RenderMessages(template, variables);
/// }
/// catch (TemplateValidationException ex)
/// {
///     Console.WriteLine($"Template '{ex.TemplateId}' failed validation.");
///     Console.WriteLine($"Missing variables: {string.Join(", ", ex.MissingVariables)}");
/// }
/// </code>
/// </example>
public class TemplateValidationException : Exception
{
    /// <summary>
    /// Gets the identifier of the template that failed validation.
    /// </summary>
    /// <value>The template ID, or null if not specified.</value>
    public string? TemplateId { get; }

    /// <summary>
    /// Gets the list of required variables that were not provided.
    /// </summary>
    /// <value>A read-only list of missing variable names. Never null.</value>
    public IReadOnlyList<string> MissingVariables { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateValidationException"/> class.
    /// </summary>
    public TemplateValidationException()
        : base("Template validation failed.")
    {
        MissingVariables = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateValidationException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public TemplateValidationException(string message)
        : base(message)
    {
        MissingVariables = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateValidationException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TemplateValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        MissingVariables = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateValidationException"/> class
    /// with the template ID and list of missing variables.
    /// </summary>
    /// <param name="templateId">The identifier of the template that failed validation.</param>
    /// <param name="missingVariables">The list of required variables that were not provided.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="templateId"/> is null.</exception>
    /// <remarks>
    /// This is the primary constructor for creating validation exceptions with full context.
    /// The exception message is automatically built from the template ID and missing variables.
    /// </remarks>
    /// <example>
    /// <code>
    /// throw new TemplateValidationException("my-template", new[] { "user_input", "context" });
    /// // Message: "Template 'my-template' validation failed. Missing required variables: user_input, context"
    /// </code>
    /// </example>
    public TemplateValidationException(
        string templateId,
        IReadOnlyList<string> missingVariables)
        : base(BuildMessage(templateId, missingVariables))
    {
        ArgumentNullException.ThrowIfNull(templateId);
        TemplateId = templateId;
        MissingVariables = missingVariables ?? Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateValidationException"/> class
    /// with the template ID, list of missing variables, and inner exception.
    /// </summary>
    /// <param name="templateId">The identifier of the template that failed validation.</param>
    /// <param name="missingVariables">The list of required variables that were not provided.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="templateId"/> is null.</exception>
    public TemplateValidationException(
        string templateId,
        IReadOnlyList<string> missingVariables,
        Exception innerException)
        : base(BuildMessage(templateId, missingVariables), innerException)
    {
        ArgumentNullException.ThrowIfNull(templateId);
        TemplateId = templateId;
        MissingVariables = missingVariables ?? Array.Empty<string>();
    }

    /// <summary>
    /// Builds a formatted error message from the template ID and missing variables.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="missingVariables">The list of missing variables.</param>
    /// <returns>A formatted error message.</returns>
    private static string BuildMessage(string templateId, IReadOnlyList<string>? missingVariables)
    {
        var vars = missingVariables is { Count: > 0 }
            ? string.Join(", ", missingVariables)
            : "(none)";
        return $"Template '{templateId}' validation failed. Missing required variables: {vars}";
    }
}
