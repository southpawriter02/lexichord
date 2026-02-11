// Copyright (c) 2025 Ryan Witting. Licensed under the MIT License. See LICENSE in root.

namespace Lexichord.Modules.Agents.Configuration.Yaml;

/// <summary>
/// YAML deserialization model for agent persona variants.
/// </summary>
/// <remarks>
/// <para>
/// This internal class represents a persona definition within an agent configuration
/// YAML file. Personas allow a single agent to have multiple behavioral variants
/// (e.g., "Strict Editor" vs. "Friendly Editor").
/// </para>
/// <para>
/// Each persona can override the agent's default temperature and optionally provide
/// a custom system prompt override.
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
internal sealed class PersonaYamlModel
{
    /// <summary>
    /// Gets or sets the unique persona identifier (kebab-case).
    /// </summary>
    /// <remarks>
    /// Required field. Must match pattern: <c>^[a-z0-9]+(-[a-z0-9]+)*$</c><br/>
    /// Must be unique within the agent's persona list.
    /// </remarks>
    public string PersonaId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the persona.
    /// </summary>
    /// <remarks>
    /// Required field. Shown in the UI when selecting personas.
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tagline describing this persona's behavior.
    /// </summary>
    /// <remarks>
    /// Required field. Brief description shown in the UI (e.g., "No errors escape notice").
    /// </remarks>
    public string Tagline { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the temperature override for this persona (0.0-2.0).
    /// </summary>
    /// <remarks>
    /// Required field. Overrides the agent's default temperature when this persona is active.
    /// </remarks>
    public double Temperature { get; set; }

    /// <summary>
    /// Gets or sets the optional system prompt override for this persona.
    /// </summary>
    /// <remarks>
    /// Optional field. If provided, replaces the agent's base system prompt.
    /// Supports multi-line YAML strings.
    /// </remarks>
    public string? SystemPromptOverride { get; set; }

    /// <summary>
    /// Gets or sets the optional voice description for UI hints.
    /// </summary>
    /// <remarks>
    /// Optional field. Describes the persona's tone (e.g., "Warm but precise", "Exact and exacting").
    /// </remarks>
    public string? VoiceDescription { get; set; }
}
