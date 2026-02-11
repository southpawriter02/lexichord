// Copyright (c) 2025 Ryan Witting. Licensed under the MIT License. See LICENSE in root.

namespace Lexichord.Modules.Agents.Configuration.Yaml;

/// <summary>
/// YAML deserialization model for default LLM options in agent configurations.
/// </summary>
/// <remarks>
/// <para>
/// This internal class represents the <c>default_options</c> section of an agent
/// configuration YAML file. It is converted to <see cref="Abstractions.Contracts.LLM.ChatOptions"/>
/// during processing.
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
internal sealed class DefaultOptionsYamlModel
{
    /// <summary>
    /// Gets or sets the OpenAI model name (e.g., "gpt-4o", "gpt-4-turbo").
    /// </summary>
    /// <remarks>
    /// Required field. Defaults to "gpt-4o" if not specified in YAML.
    /// </remarks>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Gets or sets the LLM temperature parameter (0.0-2.0).
    /// </summary>
    /// <remarks>
    /// Required field. Controls response randomness:<br/>
    /// - 0.0: Deterministic, focused responses<br/>
    /// - 0.5: Balanced creativity and consistency<br/>
    /// - 1.0+: Highly creative, unpredictable responses<br/>
    /// Defaults to 0.5 if not specified in YAML.
    /// </remarks>
    public double Temperature { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the maximum number of tokens in the response.
    /// </summary>
    /// <remarks>
    /// Required field. Must be a positive integer. Values over 128,000 generate warnings.
    /// Defaults to 2048 if not specified in YAML.
    /// </remarks>
    public int MaxTokens { get; set; } = 2048;
}
