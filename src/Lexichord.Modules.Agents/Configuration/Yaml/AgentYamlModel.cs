// Copyright (c) 2025 Ryan Witting. Licensed under the MIT License. See LICENSE in root.

using System.Collections.Generic;

namespace Lexichord.Modules.Agents.Configuration.Yaml;

/// <summary>
/// YAML deserialization model for agent configuration files.
/// </summary>
/// <remarks>
/// <para>
/// This internal class is used by <see cref="YamlAgentConfigLoader"/> to deserialize
/// YAML content into a structured format before validation and conversion to
/// <see cref="Abstractions.Agents.AgentConfiguration"/> domain objects.
/// </para>
/// <para>
/// Property names use <c>PascalCase</c> but are mapped to <c>snake_case</c> in YAML
/// via <c>YamlDotNet</c>'s <c>UnderscoredNamingConvention</c>.
/// </para>
/// <para>
/// <strong>Version:</strong> v0.7.1c<br/>
/// <strong>Module:</strong> Lexichord.Modules.Agents
/// </para>
/// </remarks>
internal sealed class AgentYamlModel
{
    /// <summary>
    /// Gets or sets the YAML schema version (currently 1).
    /// </summary>
    /// <remarks>
    /// Used for forward compatibility. Configurations with unsupported schema versions
    /// are rejected during parsing.
    /// </remarks>
    public int? SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the unique agent identifier (kebab-case).
    /// </summary>
    /// <remarks>
    /// Required field. Must match pattern: <c>^[a-z0-9]+(-[a-z0-9]+)*$</c>
    /// </remarks>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the agent.
    /// </summary>
    /// <remarks>
    /// Required field.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the agent's purpose and capabilities.
    /// </summary>
    /// <remarks>
    /// Required field. Supports multi-line YAML strings.
    /// </remarks>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon name (from Lucide icon set).
    /// </summary>
    /// <remarks>
    /// Required field. Examples: "edit-3", "message-square", "search".
    /// </remarks>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt template identifier.
    /// </summary>
    /// <remarks>
    /// Required field. References a prompt template from the template repository.
    /// </remarks>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum license tier required to use this agent.
    /// </summary>
    /// <remarks>
    /// Optional field. Valid values: "Core", "Writer", "WriterPro", "Teams", "Enterprise".
    /// Defaults to "Core" if not specified.
    /// </remarks>
    public string? LicenseTier { get; set; }

    /// <summary>
    /// Gets or sets the list of agent capabilities (flags).
    /// </summary>
    /// <remarks>
    /// Required field. At least one capability must be specified.
    /// Valid values: "Chat", "DocumentContext", "RAGContext", "StyleEnforcement",
    /// "Streaming", "CodeGeneration", "ResearchAssistance", "Summarization",
    /// "StructureAnalysis", "Brainstorming", "Translation".
    /// </remarks>
    public List<string> Capabilities { get; set; } = [];

    /// <summary>
    /// Gets or sets the default LLM options for this agent.
    /// </summary>
    /// <remarks>
    /// Required field.
    /// </remarks>
    public DefaultOptionsYamlModel DefaultOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets custom agent-specific settings.
    /// </summary>
    /// <remarks>
    /// Optional field. Dictionary of key-value pairs for agent-specific configuration.
    /// </remarks>
    public Dictionary<string, object>? CustomSettings { get; set; }

    /// <summary>
    /// Gets or sets the list of persona variants for this agent.
    /// </summary>
    /// <remarks>
    /// Optional field. If empty, the agent uses its default configuration without personas.
    /// </remarks>
    public List<PersonaYamlModel> Personas { get; set; } = [];
}
