// -----------------------------------------------------------------------
// <copyright file="IPromptRenderer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Renders prompt templates with variable substitution.
/// </summary>
/// <remarks>
/// <para>
/// The prompt renderer is responsible for transforming templates and variables
/// into ready-to-send <see cref="ChatMessage"/> arrays for LLM submission.
/// It supports Mustache-style variable substitution with <c>{{variable}}</c> syntax.
/// </para>
/// <para>
/// Implementations may use different templating engines:
/// </para>
/// <list type="bullet">
///   <item><description>Mustache (default implementation in v0.6.3b)</description></item>
///   <item><description>Handlebars (extended syntax support)</description></item>
///   <item><description>Liquid (Ruby-style templates)</description></item>
///   <item><description>Custom engines for specific requirements</description></item>
/// </list>
/// <para>
/// All implementations must be thread-safe for concurrent rendering across
/// multiple agents and requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic rendering workflow
/// var template = PromptTemplate.Create(
///     templateId: "translator",
///     name: "Translator",
///     systemPrompt: "You are a translator. Translate to {{target_language}}.",
///     userPrompt: "{{source_text}}",
///     requiredVariables: ["target_language", "source_text"]
/// );
///
/// var variables = new Dictionary&lt;string, object&gt;
/// {
///     ["target_language"] = "Spanish",
///     ["source_text"] = "Hello, world!"
/// };
///
/// // Option 1: Validate first, then render
/// var validation = renderer.ValidateVariables(template, variables);
/// if (validation.IsValid)
/// {
///     var messages = renderer.RenderMessages(template, variables);
///     var response = await chatService.CompleteAsync(new ChatRequest(messages));
/// }
///
/// // Option 2: Let RenderMessages throw on validation failure
/// try
/// {
///     var messages = renderer.RenderMessages(template, variables);
/// }
/// catch (TemplateValidationException ex)
/// {
///     logger.LogError("Missing variables: {Missing}", ex.MissingVariables);
/// }
/// </code>
/// </example>
/// <seealso cref="IPromptTemplate"/>
/// <seealso cref="ValidationResult"/>
/// <seealso cref="TemplateValidationException"/>
public interface IPromptRenderer
{
    /// <summary>
    /// Renders a template string with the provided variables.
    /// </summary>
    /// <param name="template">The template string with <c>{{variable}}</c> placeholders.</param>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <returns>The rendered string with all placeholders substituted.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="template"/> or <paramref name="variables"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs simple variable substitution without validation.
    /// It does not check for required variables - it renders whatever is provided.
    /// </para>
    /// <para>
    /// Variable values are converted to strings using <see cref="object.ToString"/>.
    /// Null values are rendered as empty strings.
    /// </para>
    /// <para>
    /// For validation, use <see cref="ValidateVariables"/> before rendering.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = "Hello, {{name}}! You have {{count}} messages.";
    /// var variables = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["name"] = "Alice",
    ///     ["count"] = 5
    /// };
    /// var result = renderer.Render(template, variables);
    /// // Result: "Hello, Alice! You have 5 messages."
    /// </code>
    /// </example>
    string Render(string template, IDictionary<string, object> variables);

    /// <summary>
    /// Renders a complete prompt template into a <see cref="ChatMessage"/> array.
    /// </summary>
    /// <param name="template">The prompt template to render.</param>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <returns>
    /// Array of <see cref="ChatMessage"/> ready for LLM submission.
    /// Typically contains [System, User] message pair.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="template"/> or <paramref name="variables"/> is null.
    /// </exception>
    /// <exception cref="TemplateValidationException">
    /// Thrown when required variables are missing. Validate first to avoid exceptions.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method validates required variables before rendering.
    /// If validation fails, a <see cref="TemplateValidationException"/> is thrown
    /// with details about the missing variables.
    /// </para>
    /// <para>
    /// The returned array typically contains:
    /// </para>
    /// <list type="number">
    ///   <item><description>System message with rendered <see cref="IPromptTemplate.SystemPromptTemplate"/></description></item>
    ///   <item><description>User message with rendered <see cref="IPromptTemplate.UserPromptTemplate"/></description></item>
    /// </list>
    /// <para>
    /// If <see cref="IPromptTemplate.SystemPromptTemplate"/> is empty,
    /// only the User message is returned.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var messages = renderer.RenderMessages(template, variables);
    /// // messages[0] = ChatMessage with Role=System, Content="You are a translator..."
    /// // messages[1] = ChatMessage with Role=User, Content="Hello, world!"
    ///
    /// var response = await chatService.CompleteAsync(new ChatRequest(messages));
    /// </code>
    /// </example>
    ChatMessage[] RenderMessages(
        IPromptTemplate template,
        IDictionary<string, object> variables);

    /// <summary>
    /// Validates that all required variables are present.
    /// </summary>
    /// <param name="template">The prompt template to validate against.</param>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether validation passed
    /// and listing any missing variables.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="template"/> or <paramref name="variables"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Call this method before <see cref="RenderMessages"/> to get validation errors
    /// without throwing exceptions. This is useful for UI feedback or logging.
    /// </para>
    /// <para>
    /// Validation considers the following as "missing" for required variables:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Variable not in dictionary</description></item>
    ///   <item><description>Variable value is null</description></item>
    ///   <item><description>Variable value is an empty string</description></item>
    /// </list>
    /// <para>
    /// The result can be used to throw a <see cref="TemplateValidationException"/>
    /// via <see cref="ValidationResult.ThrowIfInvalid"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var validation = renderer.ValidateVariables(template, variables);
    ///
    /// if (!validation.IsValid)
    /// {
    ///     // Log the specific missing variables
    ///     logger.LogWarning(
    ///         "Template {TemplateId} validation failed. Missing: {Missing}",
    ///         template.TemplateId,
    ///         string.Join(", ", validation.MissingVariables));
    ///
    ///     // Or throw with detailed exception
    ///     validation.ThrowIfInvalid(template.TemplateId);
    /// }
    ///
    /// // Safe to render now
    /// var messages = renderer.RenderMessages(template, variables);
    /// </code>
    /// </example>
    ValidationResult ValidateVariables(
        IPromptTemplate template,
        IDictionary<string, object> variables);
}
