// -----------------------------------------------------------------------
// <copyright file="AgentPersona.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Defines a personality variant for an agent with specific behavioral overrides.
/// </summary>
/// <param name="PersonaId">
/// Unique identifier within the parent agent. Must be kebab-case (e.g., "strict", "friendly").
/// </param>
/// <param name="DisplayName">
/// Human-readable name shown in UI (e.g., "Strict Editor", "Friendly Editor").
/// </param>
/// <param name="Tagline">
/// Short descriptive phrase conveying the persona's approach (e.g., "No errors escape notice").
/// </param>
/// <param name="SystemPromptOverride">
/// Optional override for the agent's system prompt. When null, uses the agent's default template.
/// When provided, this prompt replaces or augments the base agent's instructions.
/// </param>
/// <param name="Temperature">
/// LLM temperature override. Range: 0.0 (deterministic) to 2.0 (very creative).
/// Lower values produce more focused, consistent outputs; higher values encourage creativity.
/// </param>
/// <param name="VoiceDescription">
/// Optional description of the persona's communication style for UI hints.
/// Example: "Precise and exacting", "Warm and encouraging".
/// </param>
/// <remarks>
/// <para>
/// Personas allow the same agent to behave differently based on user preference.
/// For example, "The Editor" agent might offer both a "Strict Editor" persona
/// (low temperature, precise feedback) and a "Friendly Editor" persona (higher temperature,
/// gentle suggestions).
/// </para>
/// <para>
/// Personas are applied to agents at runtime via <see cref="IAgentRegistry.GetAgentWithPersona"/>,
/// and can be switched without restarting the agent or losing conversation context.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.1a as part of the Agent Registry (v0.7.1) feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define two personas for an editor agent
/// var strict = new AgentPersona(
///     PersonaId: "strict",
///     DisplayName: "Strict Editor",
///     Tagline: "No errors escape notice",
///     SystemPromptOverride: "You are a meticulous editor. Be precise and exacting...",
///     Temperature: 0.1,
///     VoiceDescription: "Precise and exacting");
///
/// var friendly = new AgentPersona(
///     PersonaId: "friendly",
///     DisplayName: "Friendly Editor",
///     Tagline: "Gentle suggestions for improvement",
///     SystemPromptOverride: null, // Uses default template
///     Temperature: 0.5,
///     VoiceDescription: "Warm and encouraging");
///
/// // Validate persona before use
/// var errors = strict.Validate();
/// if (errors.Any())
/// {
///     throw new InvalidOperationException($"Invalid persona: {string.Join("; ", errors)}");
/// }
///
/// // Apply persona to chat options
/// var baseOptions = new ChatOptions(Model: "gpt-4o", Temperature: 0.3);
/// var strictOptions = strict.ApplyTo(baseOptions);
/// // strictOptions.Temperature is now 0.1
/// </code>
/// </example>
public partial record AgentPersona(
    string PersonaId,
    string DisplayName,
    string Tagline,
    string? SystemPromptOverride,
    double Temperature,
    string? VoiceDescription = null)
{
    /// <summary>
    /// Applies this persona's overrides to the given chat options.
    /// </summary>
    /// <param name="baseOptions">The base chat options from the agent configuration.</param>
    /// <returns>
    /// A new <see cref="ChatOptions"/> instance with this persona's temperature applied.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses the record <c>with</c> expression to create a copy of <paramref name="baseOptions"/>
    /// with only the <see cref="Temperature"/> property overridden. All other options
    /// (Model, MaxTokens, TopP, etc.) are preserved from the base configuration.
    /// </remarks>
    /// <example>
    /// <code>
    /// var baseOptions = new ChatOptions(Model: "gpt-4o", Temperature: 0.7, MaxTokens: 2048);
    /// var persona = new AgentPersona("strict", "Strict", "No errors", null, 0.1);
    ///
    /// var modifiedOptions = persona.ApplyTo(baseOptions);
    /// // modifiedOptions.Model == "gpt-4o" (unchanged)
    /// // modifiedOptions.Temperature == 0.1 (overridden)
    /// // modifiedOptions.MaxTokens == 2048 (unchanged)
    /// </code>
    /// </example>
    public ChatOptions ApplyTo(ChatOptions baseOptions) =>
        baseOptions with { Temperature = Temperature };

    /// <summary>
    /// Validates that the persona meets all requirements.
    /// </summary>
    /// <returns>
    /// A read-only list of validation error messages. Returns an empty list if the persona is valid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Accumulates all validation errors rather than failing on the first error,
    /// providing comprehensive feedback for debugging invalid configurations.
    /// </para>
    /// <para>
    /// Validation rules:
    /// <list type="number">
    ///   <item><description>PersonaId is required (not null, empty, or whitespace)</description></item>
    ///   <item><description>PersonaId must match kebab-case pattern: <c>^[a-z0-9]+(-[a-z0-9]+)*$</c></description></item>
    ///   <item><description>DisplayName is required (not null, empty, or whitespace)</description></item>
    ///   <item><description>Temperature must be in range [0.0, 2.0]</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var invalid = new AgentPersona("Invalid ID", "Test", "Tag", null, 3.0);
    /// var errors = invalid.Validate();
    /// // errors contains:
    /// // "PersonaId must be kebab-case: Invalid ID"
    /// // "Temperature must be between 0.0 and 2.0: 3"
    /// </code>
    /// </example>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(PersonaId))
            errors.Add("PersonaId is required");
        else if (!PersonaIdPattern().IsMatch(PersonaId))
            errors.Add($"PersonaId must be kebab-case: {PersonaId}");

        if (string.IsNullOrWhiteSpace(DisplayName))
            errors.Add("DisplayName is required");

        if (Temperature < 0.0 || Temperature > 2.0)
            errors.Add($"Temperature must be between 0.0 and 2.0: {Temperature}");

        return errors;
    }

    /// <summary>
    /// Regular expression pattern for validating kebab-case PersonaIds.
    /// </summary>
    /// <returns>
    /// A compiled regex that matches valid kebab-case identifiers:
    /// lowercase letters and digits, with optional hyphens between segments.
    /// </returns>
    /// <remarks>
    /// LOGIC: Pattern <c>^[a-z0-9]+(-[a-z0-9]+)*$</c> ensures:
    /// <list type="bullet">
    ///   <item><description>Starts with one or more lowercase letters or digits</description></item>
    ///   <item><description>Followed by zero or more groups of (hyphen + letters/digits)</description></item>
    ///   <item><description>No leading/trailing hyphens, no consecutive hyphens</description></item>
    /// </list>
    /// Valid examples: "strict", "friendly-editor", "v2-casual"
    /// Invalid examples: "Strict", "friendly_editor", "-editor", "editor-", "editor--v2"
    /// </remarks>
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex PersonaIdPattern();
}
