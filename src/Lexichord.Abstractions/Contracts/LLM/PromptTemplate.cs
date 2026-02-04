// -----------------------------------------------------------------------
// <copyright file="PromptTemplate.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Immutable record implementing <see cref="IPromptTemplate"/>.
/// Provides value equality and immutability guarantees.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PromptTemplate"/> is the default implementation of <see cref="IPromptTemplate"/>.
/// It provides a complete, immutable template definition with:
/// </para>
/// <list type="bullet">
///   <item><description>Value equality based on all properties</description></item>
///   <item><description>Immutability for thread-safe sharing across agents</description></item>
///   <item><description>Factory method (<see cref="Create"/>) for validated construction</description></item>
///   <item><description>Computed properties for common operations</description></item>
/// </list>
/// <para>
/// Use the <see cref="Create"/> factory method for validated construction,
/// or the primary constructor for direct instantiation when validation
/// has already been performed.
/// </para>
/// </remarks>
/// <param name="TemplateId">Unique template identifier (kebab-case).</param>
/// <param name="Name">Human-readable display name.</param>
/// <param name="Description">Template purpose description.</param>
/// <param name="SystemPromptTemplate">Mustache template for system prompt.</param>
/// <param name="UserPromptTemplate">Mustache template for user prompt.</param>
/// <param name="RequiredVariables">Variables that must be provided.</param>
/// <param name="OptionalVariables">Variables that may be provided.</param>
/// <example>
/// <code>
/// // Using the factory method (recommended)
/// var template = PromptTemplate.Create(
///     templateId: "translator",
///     name: "Language Translator",
///     systemPrompt: "You are a translator. Translate to {{target_language}}.",
///     userPrompt: "{{source_text}}",
///     requiredVariables: ["target_language", "source_text"],
///     optionalVariables: ["tone"]
/// );
///
/// // Using with-expressions for modifications
/// var verboseTemplate = template with
/// {
///     Description = "Translates text between languages with tone control"
/// };
///
/// // Checking variables
/// if (template.HasVariable("source_text"))
/// {
///     Console.WriteLine($"Template has {template.VariableCount} variables total");
/// }
/// </code>
/// </example>
/// <seealso cref="IPromptTemplate"/>
/// <seealso cref="IPromptRenderer"/>
public record PromptTemplate(
    string TemplateId,
    string Name,
    string Description,
    string SystemPromptTemplate,
    string UserPromptTemplate,
    IReadOnlyList<string> RequiredVariables,
    IReadOnlyList<string> OptionalVariables) : IPromptTemplate
{
    /// <summary>
    /// Gets the unique template identifier (kebab-case).
    /// </summary>
    /// <value>The template ID. Never null or whitespace.</value>
    /// <exception cref="ArgumentException">Thrown when the value is null or whitespace.</exception>
    public string TemplateId { get; init; } = !string.IsNullOrWhiteSpace(TemplateId)
        ? TemplateId
        : throw new ArgumentException("TemplateId cannot be null or whitespace.", nameof(TemplateId));

    /// <summary>
    /// Gets the human-readable display name.
    /// </summary>
    /// <value>The template name. Never null or whitespace.</value>
    /// <exception cref="ArgumentException">Thrown when the value is null or whitespace.</exception>
    public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
        ? Name
        : throw new ArgumentException("Name cannot be null or whitespace.", nameof(Name));

    /// <summary>
    /// Gets the template purpose description.
    /// </summary>
    /// <value>The description. Never null, may be empty.</value>
    public string Description { get; init; } = Description ?? string.Empty;

    /// <summary>
    /// Gets the Mustache template for system prompt.
    /// </summary>
    /// <value>The system prompt template. Never null, may be empty.</value>
    public string SystemPromptTemplate { get; init; } = SystemPromptTemplate ?? string.Empty;

    /// <summary>
    /// Gets the Mustache template for user prompt.
    /// </summary>
    /// <value>The user prompt template. Never null, may be empty.</value>
    public string UserPromptTemplate { get; init; } = UserPromptTemplate ?? string.Empty;

    /// <summary>
    /// Gets the variables that must be provided for rendering.
    /// </summary>
    /// <value>A read-only list of required variable names. Never null.</value>
    public IReadOnlyList<string> RequiredVariables { get; init; } =
        RequiredVariables ?? Array.Empty<string>();

    /// <summary>
    /// Gets the variables that may be provided for rendering.
    /// </summary>
    /// <value>A read-only list of optional variable names. Never null.</value>
    public IReadOnlyList<string> OptionalVariables { get; init; } =
        OptionalVariables ?? Array.Empty<string>();

    /// <summary>
    /// Creates a new <see cref="PromptTemplate"/> with validated parameters.
    /// </summary>
    /// <param name="templateId">Unique identifier for the template (kebab-case).</param>
    /// <param name="name">Human-readable display name.</param>
    /// <param name="systemPrompt">Mustache template for the system prompt.</param>
    /// <param name="userPrompt">Mustache template for the user prompt.</param>
    /// <param name="requiredVariables">Optional list of required variables.</param>
    /// <param name="optionalVariables">Optional list of optional variables.</param>
    /// <returns>A new <see cref="PromptTemplate"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="templateId"/> or <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This factory method provides a convenient way to create templates with
    /// minimal boilerplate. It validates required parameters and provides
    /// sensible defaults for optional ones.
    /// </para>
    /// <para>
    /// The <see cref="Description"/> is set to empty string. Use with-expressions
    /// to add a description if needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Minimal template
    /// var simple = PromptTemplate.Create(
    ///     "assistant", "Assistant", "You are helpful.", "{{input}}");
    ///
    /// // With variables
    /// var withVars = PromptTemplate.Create(
    ///     templateId: "editor",
    ///     name: "Editor",
    ///     systemPrompt: "You are an editor. {{#style}}Follow: {{style}}{{/style}}",
    ///     userPrompt: "{{text}}",
    ///     requiredVariables: ["text"],
    ///     optionalVariables: ["style"]
    /// );
    ///
    /// // Adding description via with-expression
    /// var documented = withVars with { Description = "Edits text with optional style rules" };
    /// </code>
    /// </example>
    public static PromptTemplate Create(
        string templateId,
        string name,
        string systemPrompt,
        string userPrompt,
        IEnumerable<string>? requiredVariables = null,
        IEnumerable<string>? optionalVariables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new PromptTemplate(
            TemplateId: templateId,
            Name: name,
            Description: string.Empty,
            SystemPromptTemplate: systemPrompt ?? string.Empty,
            UserPromptTemplate: userPrompt ?? string.Empty,
            RequiredVariables: requiredVariables?.ToList().AsReadOnly()
                ?? (IReadOnlyList<string>)Array.Empty<string>(),
            OptionalVariables: optionalVariables?.ToList().AsReadOnly()
                ?? (IReadOnlyList<string>)Array.Empty<string>());
    }

    /// <summary>
    /// Gets all variables (required and optional combined).
    /// </summary>
    /// <value>
    /// An enumerable containing all variable names from both
    /// <see cref="RequiredVariables"/> and <see cref="OptionalVariables"/>.
    /// </value>
    /// <remarks>
    /// The required variables appear first, followed by optional variables.
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = PromptTemplate.Create(
    ///     "test", "Test", "{{a}} {{b}}", "{{c}}",
    ///     requiredVariables: ["a", "b"],
    ///     optionalVariables: ["c", "d"]
    /// );
    ///
    /// foreach (var variable in template.AllVariables)
    /// {
    ///     Console.WriteLine(variable); // a, b, c, d
    /// }
    /// </code>
    /// </example>
    public IEnumerable<string> AllVariables => RequiredVariables.Concat(OptionalVariables);

    /// <summary>
    /// Gets the total count of all variables.
    /// </summary>
    /// <value>
    /// The sum of <see cref="RequiredVariables"/> count and
    /// <see cref="OptionalVariables"/> count.
    /// </value>
    /// <example>
    /// <code>
    /// var template = PromptTemplate.Create(
    ///     "test", "Test", "sys", "user",
    ///     requiredVariables: ["a", "b"],
    ///     optionalVariables: ["c"]
    /// );
    ///
    /// Console.WriteLine(template.VariableCount); // 3
    /// </code>
    /// </example>
    public int VariableCount => RequiredVariables.Count + OptionalVariables.Count;

    /// <summary>
    /// Checks if a variable name is defined in this template.
    /// </summary>
    /// <param name="variableName">The variable name to check (case-insensitive).</param>
    /// <returns>
    /// <see langword="true"/> if the variable is in <see cref="RequiredVariables"/>
    /// or <see cref="OptionalVariables"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The comparison is case-insensitive per typical template variable conventions.
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = PromptTemplate.Create(
    ///     "test", "Test", "sys", "user",
    ///     requiredVariables: ["user_input"]
    /// );
    ///
    /// template.HasVariable("user_input");  // true
    /// template.HasVariable("USER_INPUT");  // true (case-insensitive)
    /// template.HasVariable("other");       // false
    /// </code>
    /// </example>
    public bool HasVariable(string variableName)
        => AllVariables.Contains(variableName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this template has any required variables.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="RequiredVariables"/> has any elements;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasRequiredVariables => RequiredVariables.Count > 0;

    /// <summary>
    /// Gets a value indicating whether this template has any optional variables.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="OptionalVariables"/> has any elements;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasOptionalVariables => OptionalVariables.Count > 0;

    /// <summary>
    /// Gets a value indicating whether this template has a system prompt.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="SystemPromptTemplate"/> is not empty;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasSystemPrompt => !string.IsNullOrEmpty(SystemPromptTemplate);

    /// <summary>
    /// Gets a value indicating whether this template has a user prompt.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="UserPromptTemplate"/> is not empty;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasUserPrompt => !string.IsNullOrEmpty(UserPromptTemplate);

    /// <summary>
    /// Gets a value indicating whether this template has a description.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Description"/> is not empty;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasDescription => !string.IsNullOrEmpty(Description);
}
