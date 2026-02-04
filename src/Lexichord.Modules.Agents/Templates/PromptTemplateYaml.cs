// -----------------------------------------------------------------------
// <copyright file="PromptTemplateYaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using YamlDotNet.Serialization;

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Data transfer object for deserializing prompt templates from YAML files.
/// </summary>
/// <remarks>
/// <para>
/// This class serves as the deserialization target for YAML template files.
/// Property names use <see cref="YamlMemberAttribute"/> to map snake_case YAML keys
/// to PascalCase C# properties.
/// </para>
/// <para>
/// <strong>YAML Schema:</strong>
/// </para>
/// <code>
/// template_id: "unique-kebab-case-id"
/// name: "Human-Readable Name"
/// description: "Description of what this template does"
/// category: "editing"  # optional
/// tags:                # optional
///   - writing
///   - assistant
/// system_prompt: |
///   Multi-line system prompt with {{mustache}} variables.
/// user_prompt: "{{user_input}}"
/// required_variables:
///   - user_input
/// optional_variables:  # optional
///   - style_rules
///   - context
/// variable_metadata:   # optional
///   user_input:
///     description: "The user's input text"
///   style_rules:
///     description: "Style guide rules"
///     default: ""
/// </code>
/// </remarks>
/// <seealso cref="VariableMetadataYaml"/>
internal sealed class PromptTemplateYaml
{
    /// <summary>
    /// Gets or sets the unique identifier for the template.
    /// </summary>
    /// <value>
    /// A kebab-case identifier (e.g., "co-pilot-editor").
    /// This field is required for a valid template.
    /// </value>
    [YamlMember(Alias = "template_id")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable display name of the template.
    /// </summary>
    /// <value>
    /// A display name suitable for UI presentation (e.g., "Co-pilot Editor").
    /// This field is required for a valid template.
    /// </value>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the template's purpose.
    /// </summary>
    /// <value>
    /// A brief description explaining what the template does and when to use it.
    /// </value>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category for organizing templates.
    /// </summary>
    /// <value>
    /// A category name for grouping related templates (e.g., "editing", "review", "analysis").
    /// This field is optional.
    /// </value>
    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the tags for filtering and searching templates.
    /// </summary>
    /// <value>
    /// A list of tags for categorization and search (e.g., ["writing", "assistant", "technical"]).
    /// This field is optional; defaults to an empty list if not specified.
    /// </value>
    [YamlMember(Alias = "tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the Mustache template for the system prompt.
    /// </summary>
    /// <value>
    /// A Mustache template string for the system (instruction) prompt.
    /// Can include variables like <c>{{style_rules}}</c> and sections like
    /// <c>{{#context}}...{{/context}}</c>.
    /// </value>
    [YamlMember(Alias = "system_prompt")]
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets the Mustache template for the user prompt.
    /// </summary>
    /// <value>
    /// A Mustache template string for the user prompt.
    /// Typically contains the primary input variable like <c>{{user_input}}</c>.
    /// </value>
    [YamlMember(Alias = "user_prompt")]
    public string? UserPrompt { get; set; }

    /// <summary>
    /// Gets or sets the list of required variable names.
    /// </summary>
    /// <value>
    /// Variables that must be provided when rendering the template.
    /// The template rendering will fail if any required variables are missing.
    /// </value>
    [YamlMember(Alias = "required_variables")]
    public List<string>? RequiredVariables { get; set; }

    /// <summary>
    /// Gets or sets the list of optional variable names.
    /// </summary>
    /// <value>
    /// Variables that may be provided but are not required.
    /// Missing optional variables will render as empty strings or be excluded
    /// from conditional sections.
    /// </value>
    [YamlMember(Alias = "optional_variables")]
    public List<string>? OptionalVariables { get; set; }

    /// <summary>
    /// Gets or sets metadata for each variable.
    /// </summary>
    /// <value>
    /// A dictionary mapping variable names to their metadata including
    /// descriptions and default values.
    /// </value>
    [YamlMember(Alias = "variable_metadata")]
    public Dictionary<string, VariableMetadataYaml>? VariableMetadata { get; set; }
}

/// <summary>
/// Data transfer object for variable metadata within a prompt template.
/// </summary>
/// <remarks>
/// This class provides additional information about template variables,
/// such as descriptions for documentation and default values for optional variables.
/// </remarks>
internal sealed class VariableMetadataYaml
{
    /// <summary>
    /// Gets or sets the description of what the variable represents.
    /// </summary>
    /// <value>
    /// A human-readable description of the variable's purpose and expected content.
    /// </value>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default value for optional variables.
    /// </summary>
    /// <value>
    /// A default value to use when the variable is not provided.
    /// Only applicable to optional variables.
    /// </value>
    [YamlMember(Alias = "default")]
    public string? Default { get; set; }
}
