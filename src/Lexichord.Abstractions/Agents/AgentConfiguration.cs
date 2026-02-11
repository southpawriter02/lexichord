// -----------------------------------------------------------------------
// <copyright file="AgentConfiguration.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Defines the complete configuration for a specialized agent including
/// identity, capabilities, default behavior, and available personas.
/// </summary>
/// <param name="AgentId">
/// Unique identifier for this agent. Must be kebab-case (e.g., "editor", "researcher", "co-pilot").
/// </param>
/// <param name="Name">
/// Human-readable display name (e.g., "The Editor", "The Researcher", "Co-pilot").
/// Typically 2-3 words, capitalized.
/// </param>
/// <param name="Description">
/// Brief description of the agent's purpose and specialization (1-2 sentences).
/// Shown in agent selection UI to help users choose the right agent.
/// </param>
/// <param name="Icon">
/// Icon identifier for UI display. Uses Lucide icon names (e.g., "edit-3", "search", "sparkles").
/// </param>
/// <param name="TemplateId">
/// Identifier of the prompt template used by this agent.
/// Corresponds to a template in <see cref="IPromptTemplateRepository"/>.
/// </param>
/// <param name="Capabilities">
/// Flags indicating what features this agent supports.
/// Used by the UI to adapt available features and by the registry for filtering.
/// </param>
/// <param name="DefaultOptions">
/// Default LLM request options (model, temperature, max_tokens).
/// These can be overridden by specific personas.
/// </param>
/// <param name="Personas">
/// Available personality variants for this agent.
/// Each persona can override temperature and system prompt to adjust agent behavior.
/// </param>
/// <param name="RequiredTier">
/// Minimum license tier required to use this agent.
/// Defaults to <see cref="LicenseTier.Core"/> (available to all users).
/// </param>
/// <param name="CustomSettings">
/// Optional dictionary of agent-specific configuration values.
/// Keys are setting names, values can be any serializable type (bool, int, string, etc.).
/// Used for agent-specific features not covered by standard properties.
/// </param>
/// <remarks>
/// <para>
/// AgentConfiguration serves as the canonical definition of an agent's identity,
/// capabilities, and behavior. It is immutable and should be validated before use
/// via the <see cref="Validate"/> method.
/// </para>
/// <para>
/// Configurations can be defined programmatically or loaded from YAML files via
/// <see cref="Lexichord.Modules.Agents.Configuration.IAgentConfigLoader"/> (v0.7.1c).
/// </para>
/// <para>
/// The <see cref="IAgentRegistry"/> (v0.7.1b) uses configurations to manage
/// agent instances with persona switching support.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.1a as part of the Agent Registry (v0.7.1) feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var editorConfig = new AgentConfiguration(
///     AgentId: "editor",
///     Name: "The Editor",
///     Description: "Focused on grammar, clarity, and structure",
///     Icon: "edit-3",
///     TemplateId: "specialist-editor",
///     Capabilities: AgentCapabilities.Chat |
///                   AgentCapabilities.DocumentContext |
///                   AgentCapabilities.StyleEnforcement,
///     DefaultOptions: new ChatOptions(Model: "gpt-4o", Temperature: 0.3, MaxTokens: 2048),
///     Personas: new[]
///     {
///         new AgentPersona("strict", "Strict Editor", "No errors escape notice", null, 0.1),
///         new AgentPersona("friendly", "Friendly Editor", "Gentle suggestions", null, 0.5)
///     },
///     RequiredTier: LicenseTier.WriterPro);
///
/// // Validate configuration
/// var errors = editorConfig.Validate();
/// if (errors.Any())
/// {
///     throw new InvalidOperationException($"Invalid config: {string.Join("; ", errors)}");
/// }
///
/// // Access personas
/// var strictPersona = editorConfig.GetPersona("strict");
/// var defaultPersona = editorConfig.DefaultPersona; // Returns first persona
/// </code>
/// </example>
public partial record AgentConfiguration(
    string AgentId,
    string Name,
    string Description,
    string Icon,
    string TemplateId,
    AgentCapabilities Capabilities,
    ChatOptions DefaultOptions,
    IReadOnlyList<AgentPersona> Personas,
    LicenseTier RequiredTier = LicenseTier.Core,
    IReadOnlyDictionary<string, object>? CustomSettings = null)
{
    /// <summary>
    /// Gets the default persona (first in the list) or null if no personas defined.
    /// </summary>
    /// <value>
    /// The first persona in the <see cref="Personas"/> collection, or <c>null</c> if the collection is empty.
    /// </value>
    /// <remarks>
    /// LOGIC: Returns the first persona as the default choice when no specific persona is requested.
    /// Agents may operate without personas (empty collection), in which case this returns null.
    /// The UI typically selects the default persona automatically when an agent is first chosen.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration(
    ///     ...,
    ///     Personas: new[]
    ///     {
    ///         new AgentPersona("strict", "Strict", "No errors", null, 0.1),
    ///         new AgentPersona("friendly", "Friendly", "Gentle", null, 0.5)
    ///     });
    ///
    /// var defaultPersona = config.DefaultPersona;
    /// // defaultPersona.PersonaId == "strict"
    /// </code>
    /// </example>
    public AgentPersona? DefaultPersona =>
        Personas.Count > 0 ? Personas[0] : null;

    /// <summary>
    /// Gets a persona by ID, or null if not found.
    /// </summary>
    /// <param name="personaId">The unique persona identifier to search for.</param>
    /// <returns>
    /// The <see cref="AgentPersona"/> with matching <see cref="AgentPersona.PersonaId"/>,
    /// or <c>null</c> if no match is found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses LINQ <see cref="Enumerable.FirstOrDefault"/> for efficient single-pass lookup.
    /// Comparison is case-sensitive since PersonaIds are kebab-case by validation rules.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration(..., Personas: [...]);
    ///
    /// var strictPersona = config.GetPersona("strict");
    /// if (strictPersona is not null)
    /// {
    ///     var options = strictPersona.ApplyTo(config.DefaultOptions);
    ///     // Use modified options...
    /// }
    ///
    /// var unknown = config.GetPersona("nonexistent");
    /// // unknown == null
    /// </code>
    /// </example>
    public AgentPersona? GetPersona(string personaId) =>
        Personas.FirstOrDefault(p => p.PersonaId == personaId);

    /// <summary>
    /// Validates that the configuration meets all requirements.
    /// </summary>
    /// <returns>
    /// A read-only list of validation error messages. Returns an empty list if the configuration is valid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Accumulates all validation errors rather than failing on the first error,
    /// providing comprehensive feedback for debugging invalid configurations.
    /// </para>
    /// <para>
    /// Validation rules:
    /// <list type="number">
    ///   <item><description>AgentId is required (not null, empty, or whitespace)</description></item>
    ///   <item><description>AgentId must match kebab-case pattern: <c>^[a-z0-9]+(-[a-z0-9]+)*$</c></description></item>
    ///   <item><description>Name is required (not null, empty, or whitespace)</description></item>
    ///   <item><description>TemplateId is required (not null, empty, or whitespace)</description></item>
    ///   <item><description>Capabilities must not be <see cref="AgentCapabilities.None"/> (at least one capability required)</description></item>
    ///   <item><description>No duplicate PersonaIds in the Personas collection</description></item>
    ///   <item><description>Each persona must pass its own <see cref="AgentPersona.Validate"/> check</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var invalid = new AgentConfiguration(
    ///     AgentId: "Invalid ID", // Fails kebab-case validation
    ///     Name: "Test",
    ///     Description: "Test agent",
    ///     Icon: "test",
    ///     TemplateId: "",  // Fails required validation
    ///     Capabilities: AgentCapabilities.None,  // Fails required validation
    ///     DefaultOptions: new ChatOptions(),
    ///     Personas: new[]
    ///     {
    ///         new AgentPersona("duplicate", "First", "Tag", null, 0.5),
    ///         new AgentPersona("duplicate", "Second", "Tag", null, 0.3) // Duplicate ID
    ///     });
    ///
    /// var errors = invalid.Validate();
    /// // errors contains:
    /// // "AgentId must be kebab-case (lowercase letters, numbers, hyphens)"
    /// // "TemplateId is required"
    /// // "At least one capability is required"
    /// // "Duplicate persona ID: duplicate"
    /// </code>
    /// </example>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(AgentId))
            errors.Add("AgentId is required");
        else if (!AgentIdPattern().IsMatch(AgentId))
            errors.Add("AgentId must be kebab-case (lowercase letters, numbers, hyphens)");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(TemplateId))
            errors.Add("TemplateId is required");

        if (Capabilities == AgentCapabilities.None)
            errors.Add("At least one capability is required");

        // Validate personas
        var personaIds = new HashSet<string>();
        foreach (var persona in Personas)
        {
            if (!personaIds.Add(persona.PersonaId))
                errors.Add($"Duplicate persona ID: {persona.PersonaId}");
            errors.AddRange(persona.Validate());
        }

        return errors;
    }

    /// <summary>
    /// Regular expression pattern for validating kebab-case AgentIds.
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
    /// Valid examples: "co-pilot", "editor", "research-assistant-v2"
    /// Invalid examples: "Co-Pilot", "editor_v1", "-agent", "agent-", "agent--v1"
    /// </remarks>
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex AgentIdPattern();
}
