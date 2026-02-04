// -----------------------------------------------------------------------
// <copyright file="IPromptTemplate.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Defines a reusable prompt template for LLM interactions.
/// Templates use Mustache-style variable substitution.
/// </summary>
/// <remarks>
/// <para>
/// Prompt templates provide a structured, type-safe approach to prompt definition.
/// They separate prompt structure from runtime values, enabling:
/// </para>
/// <list type="bullet">
///   <item><description>Validation of required variables before expensive LLM calls</description></item>
///   <item><description>Consistent prompt patterns across different agents</description></item>
///   <item><description>Easy prompt auditing and version control</description></item>
///   <item><description>Reusable prompt patterns across multiple use cases</description></item>
/// </list>
/// <para>
/// Templates define both system and user prompts with placeholders using
/// Mustache syntax (<c>{{variable}}</c>). Required variables must be provided
/// for rendering; optional variables default to empty strings if not provided.
/// </para>
/// <para>
/// Template IDs should be kebab-case and unique within the repository
/// (e.g., "co-pilot-editor", "style-checker").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a template implementation
/// public class TranslatorTemplate : IPromptTemplate
/// {
///     public string TemplateId => "translator";
///     public string Name => "Language Translator";
///     public string Description => "Translates text between languages";
///     public string SystemPromptTemplate => "You are a translator. Translate to {{target_language}}.";
///     public string UserPromptTemplate => "{{source_text}}";
///     public IReadOnlyList&lt;string&gt; RequiredVariables => new[] { "target_language", "source_text" };
///     public IReadOnlyList&lt;string&gt; OptionalVariables => Array.Empty&lt;string&gt;();
/// }
///
/// // Using the template with a renderer
/// var template = new TranslatorTemplate();
/// var variables = new Dictionary&lt;string, object&gt;
/// {
///     ["target_language"] = "Spanish",
///     ["source_text"] = "Hello, world!"
/// };
/// var messages = renderer.RenderMessages(template, variables);
/// </code>
/// </example>
/// <seealso cref="IPromptRenderer"/>
/// <seealso cref="PromptTemplate"/>
public interface IPromptTemplate
{
    /// <summary>
    /// Gets the unique identifier for the template.
    /// </summary>
    /// <value>
    /// The template ID in kebab-case format (e.g., "co-pilot-editor").
    /// Must be unique within the template repository.
    /// </value>
    /// <remarks>
    /// Template IDs are used for:
    /// <list type="bullet">
    ///   <item><description>Loading templates from repositories</description></item>
    ///   <item><description>Logging and diagnostics</description></item>
    ///   <item><description>Exception messages in <see cref="TemplateValidationException"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>co-pilot-editor</example>
    string TemplateId { get; }

    /// <summary>
    /// Gets the human-readable display name for the template.
    /// </summary>
    /// <value>
    /// A user-friendly name shown in UI template selectors
    /// (e.g., "Co-pilot Editor").
    /// </value>
    /// <remarks>
    /// The name should be concise but descriptive enough
    /// to help users select the appropriate template.
    /// </remarks>
    /// <example>Co-pilot Editor</example>
    string Name { get; }

    /// <summary>
    /// Gets the description of the template's purpose and intended use case.
    /// </summary>
    /// <value>
    /// A detailed description that helps users understand when to use this template.
    /// </value>
    /// <remarks>
    /// Descriptions should explain:
    /// <list type="bullet">
    ///   <item><description>What the template does</description></item>
    ///   <item><description>When to use it</description></item>
    ///   <item><description>Any special requirements or limitations</description></item>
    /// </list>
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets the Mustache template for the system prompt.
    /// </summary>
    /// <value>
    /// The system prompt template string containing <c>{{variable}}</c> placeholders.
    /// May be empty if no system prompt is needed.
    /// </value>
    /// <remarks>
    /// <para>
    /// The system prompt establishes the AI's role, behavior, and context.
    /// It supports Mustache syntax including:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Variable substitution: <c>{{variable}}</c></description></item>
    ///   <item><description>Sections (conditional): <c>{{#var}}...{{/var}}</c></description></item>
    ///   <item><description>Inverted sections: <c>{{^var}}...{{/var}}</c></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// You are a helpful {{role}}.
    ///
    /// {{#style_rules}}
    /// Follow these style guidelines:
    /// {{style_rules}}
    /// {{/style_rules}}
    /// </code>
    /// </example>
    string SystemPromptTemplate { get; }

    /// <summary>
    /// Gets the Mustache template for the user prompt.
    /// </summary>
    /// <value>
    /// The user prompt template string containing <c>{{variable}}</c> placeholders.
    /// </value>
    /// <remarks>
    /// The user prompt contains the actual request or content to process.
    /// It typically includes the user's input or selected text.
    /// </remarks>
    /// <example>
    /// <code>
    /// Please review the following text:
    ///
    /// {{selected_text}}
    /// </code>
    /// </example>
    string UserPromptTemplate { get; }

    /// <summary>
    /// Gets the list of variables that must be provided for rendering.
    /// </summary>
    /// <value>
    /// A read-only list of required variable names (without braces).
    /// Rendering fails if any required variable is missing.
    /// </value>
    /// <remarks>
    /// <para>
    /// Required variables are validated by <see cref="IPromptRenderer.ValidateVariables"/>
    /// before rendering. If any required variable is missing, a
    /// <see cref="TemplateValidationException"/> is thrown.
    /// </para>
    /// <para>
    /// Variable names should be snake_case (e.g., "user_input", "target_language").
    /// </para>
    /// </remarks>
    IReadOnlyList<string> RequiredVariables { get; }

    /// <summary>
    /// Gets the list of variables that may be provided for rendering.
    /// </summary>
    /// <value>
    /// A read-only list of optional variable names (without braces).
    /// Missing optional variables render as empty strings.
    /// </value>
    /// <remarks>
    /// <para>
    /// Optional variables allow templates to have conditional sections
    /// that are only included when the variable is provided.
    /// </para>
    /// <para>
    /// Use Mustache sections (<c>{{#var}}...{{/var}}</c>) to conditionally
    /// include content based on optional variables.
    /// </para>
    /// </remarks>
    IReadOnlyList<string> OptionalVariables { get; }
}
