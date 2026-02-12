// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Agents;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// Represents a single persona option within an agent in the Agent Selector UI (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Wraps <see cref="AgentPersona"/> to provide UI-friendly
/// computed properties for display in the persona selection submenu.
/// </para>
/// <para>
/// <strong>Computed Properties:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="TemperatureLabel"/> — Returns temperature-based label (e.g., "Focused", "Creative").</description></item>
///   <item><description><see cref="AccessibilityLabel"/> — Full accessibility description for screen readers.</description></item>
/// </list>
/// <para>
/// <strong>Usage:</strong> Instantiated by <see cref="AgentSelectorViewModel"/>
/// when loading personas from <see cref="AgentConfiguration.Personas"/>.
/// </para>
/// <para>
/// <strong>Related:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AgentItemViewModel"/> — Parent agent that contains this persona.</description></item>
///   <item><description><see cref="AgentSelectorViewModel"/> — Manages persona selection.</description></item>
///   <item><description><see cref="AgentPersona"/> (v0.7.1a) — Wrapped persona configuration.</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
/// </para>
/// </remarks>
public partial class PersonaItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for this persona within its agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Must match the kebab-case pattern from <see cref="AgentPersona.PersonaId"/>.
    /// </remarks>
    [ObservableProperty]
    private string _personaId = string.Empty;

    /// <summary>
    /// Gets or sets the display name of this persona.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Displayed prominently in the persona selection UI.
    /// </remarks>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>
    /// Gets or sets the short tagline describing this persona's personality.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Displayed as secondary text below the persona name.
    /// </remarks>
    [ObservableProperty]
    private string _tagline = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of this persona's voice and tone.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Used for detailed tooltip or expanded UI hints.
    /// May be <see langword="null"/> if not specified.
    /// </remarks>
    [ObservableProperty]
    private string? _voiceDescription;

    /// <summary>
    /// Gets or sets the temperature override for this persona.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>RULE:</strong> Valid range is 0.0 to 2.0.
    /// </para>
    /// <list type="bullet">
    ///   <item><description>&lt; 0.3 — "Focused" (deterministic, precise).</description></item>
    ///   <item><description>0.3 - 0.6 — "Balanced" (moderate creativity).</description></item>
    ///   <item><description>0.6 - 0.9 — "Creative" (more varied responses).</description></item>
    ///   <item><description>&gt;= 0.9 — "Experimental" (highly creative, unpredictable).</description></item>
    /// </list>
    /// </remarks>
    [ObservableProperty]
    private double _temperature;

    /// <summary>
    /// Gets or sets whether this persona is currently selected.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Updated by <see cref="AgentSelectorViewModel"/> when
    /// user switches personas.
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets the temperature-based label for this persona.
    /// </summary>
    /// <value>
    /// A short string describing the temperature mode:
    /// "Focused", "Balanced", "Creative", or "Experimental".
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>LOGIC:</strong> Maps temperature ranges to user-friendly labels:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>&lt; 0.3 — "Focused".</description></item>
    ///   <item><description>0.3 - 0.6 — "Balanced".</description></item>
    ///   <item><description>0.6 - 0.9 — "Creative".</description></item>
    ///   <item><description>&gt;= 0.9 — "Experimental".</description></item>
    /// </list>
    /// </remarks>
    public string TemperatureLabel => Temperature switch
    {
        < 0.3 => "Focused",
        < 0.6 => "Balanced",
        < 0.9 => "Creative",
        _ => "Experimental"
    };

    /// <summary>
    /// Gets the full accessibility label for screen readers.
    /// </summary>
    /// <value>
    /// A concatenated string with persona name, tagline, temperature label, and selection status.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>LOGIC:</strong> Constructs a descriptive label for accessibility tools:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"{DisplayName}. {Tagline}."</description></item>
    ///   <item><description>"{TemperatureLabel} mode."</description></item>
    ///   <item><description>"{VoiceDescription}." (if not null/empty)</description></item>
    ///   <item><description>"Selected." (if IsSelected)</description></item>
    /// </list>
    /// </remarks>
    public string AccessibilityLabel
    {
        get
        {
            var parts = new List<string>
            {
                $"{DisplayName}. {Tagline}.",
                $"{TemperatureLabel} mode."
            };

            if (!string.IsNullOrWhiteSpace(VoiceDescription))
            {
                parts.Add($"{VoiceDescription}.");
            }

            if (IsSelected)
            {
                parts.Add("Selected.");
            }

            return string.Join(" ", parts);
        }
    }
}
