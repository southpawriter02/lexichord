// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// Represents a single agent item in the Agent Selector UI (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Wraps <see cref="AgentConfiguration"/> to provide
/// UI-friendly computed properties for display in the Agent Selector dropdown.
/// </para>
/// <para>
/// <strong>Computed Properties:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DefaultPersona"/> — Returns first persona or null.</description></item>
///   <item><description><see cref="TierBadgeText"/> — Returns tier display text (e.g., "PRO", "TEAMS").</description></item>
///   <item><description><see cref="ShowTierBadge"/> — True if tier is above Core.</description></item>
///   <item><description><see cref="IsLocked"/> — True if agent requires higher tier than current.</description></item>
///   <item><description><see cref="CapabilitiesSummary"/> — Returns comma-separated capabilities list.</description></item>
///   <item><description><see cref="AccessibilityLabel"/> — Full accessibility description for screen readers.</description></item>
/// </list>
/// <para>
/// <strong>Usage:</strong> Instantiated by <see cref="AgentSelectorViewModel"/>
/// when loading agents from <see cref="IAgentRegistry"/>.
/// </para>
/// <para>
/// <strong>Related:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AgentSelectorViewModel"/> — Parent ViewModel.</description></item>
///   <item><description><see cref="PersonaItemViewModel"/> — Persona items in <see cref="Personas"/> collection.</description></item>
///   <item><description><see cref="AgentConfiguration"/> (v0.7.1a) — Wrapped configuration.</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
/// </para>
/// </remarks>
public partial class AgentItemViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for this agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Must match the kebab-case pattern from <see cref="AgentConfiguration.AgentId"/>.
    /// </remarks>
    [ObservableProperty]
    private string _agentId = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Displayed prominently in the agent card UI.
    /// </remarks>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the short description of the agent's purpose.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Displayed as secondary text below the agent name.
    /// </remarks>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Gets or sets the Lucide icon name for this agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Used by <see cref="Converters.LucideIconConverter"/> to render the icon.
    /// </remarks>
    [ObservableProperty]
    private string _icon = string.Empty;

    /// <summary>
    /// Gets or sets the capabilities flags for this agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Used to compute <see cref="CapabilitiesSummary"/> for display.
    /// </remarks>
    [ObservableProperty]
    private AgentCapabilities _capabilities;

    /// <summary>
    /// Gets or sets the license tier required to use this agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Compared against <see cref="ILicenseContext.GetCurrentTier"/>
    /// to determine if the agent is locked.
    /// </remarks>
    [ObservableProperty]
    private LicenseTier _requiredTier;

    /// <summary>
    /// Gets or sets whether the user can access this agent with their current license tier.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Set by <see cref="AgentSelectorViewModel"/> using
    /// <see cref="IAgentRegistry.CanAccess"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _canAccess;

    /// <summary>
    /// Gets or sets whether this agent is marked as a favorite by the user.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Persisted via <see cref="ISettingsService"/> in
    /// <see cref="AgentSelectorViewModel"/>.
    /// </remarks>
    [ObservableProperty]
    private bool _isFavorite;

    /// <summary>
    /// Gets or sets whether this agent is currently selected in the dropdown.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Updated by <see cref="AgentSelectorViewModel"/> when
    /// user selects an agent.
    /// </remarks>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Gets the collection of available personas for this agent.
    /// </summary>
    /// <remarks>
    /// <strong>RULE:</strong> Populated by <see cref="AgentSelectorViewModel"/>
    /// from <see cref="AgentConfiguration.Personas"/>.
    /// </remarks>
    public ObservableCollection<PersonaItemViewModel> Personas { get; } = [];

    /// <summary>
    /// Gets the default persona for this agent (first in the list).
    /// </summary>
    /// <value>
    /// The first <see cref="PersonaItemViewModel"/> in <see cref="Personas"/>,
    /// or <see langword="null"/> if no personas are available.
    /// </value>
    /// <remarks>
    /// <strong>LOGIC:</strong> Returns the first persona as the default, following
    /// the convention from <see cref="AgentConfiguration.DefaultPersona"/>.
    /// </remarks>
    public PersonaItemViewModel? DefaultPersona => Personas.Count > 0 ? Personas[0] : null;

    /// <summary>
    /// Gets the display text for the license tier badge.
    /// </summary>
    /// <value>
    /// A short uppercase string representing the tier (e.g., "PRO", "TEAMS", "ENTERPRISE").
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>LOGIC:</strong> Maps <see cref="RequiredTier"/> to display text:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="LicenseTier.Core"/> — Empty string (no badge).</description></item>
    ///   <item><description><see cref="LicenseTier.Writer"/> — "WRITER".</description></item>
    ///   <item><description><see cref="LicenseTier.WriterPro"/> — "PRO".</description></item>
    ///   <item><description><see cref="LicenseTier.Teams"/> — "TEAMS".</description></item>
    ///   <item><description><see cref="LicenseTier.Enterprise"/> — "ENTERPRISE".</description></item>
    /// </list>
    /// </remarks>
    public string TierBadgeText => RequiredTier switch
    {
        LicenseTier.Core => string.Empty,
        LicenseTier.Writer => "WRITER",
        LicenseTier.WriterPro => "PRO",
        LicenseTier.Teams => "TEAMS",
        LicenseTier.Enterprise => "ENTERPRISE",
        _ => string.Empty
    };

    /// <summary>
    /// Gets whether the tier badge should be displayed in the UI.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="RequiredTier"/> is above <see cref="LicenseTier.Core"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <strong>LOGIC:</strong> Core tier agents do not display a badge, as Core is
    /// the baseline tier available to all users.
    /// </remarks>
    public bool ShowTierBadge => RequiredTier > LicenseTier.Core;

    /// <summary>
    /// Gets whether this agent is locked due to insufficient license tier.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the agent requires a higher tier than the user's current tier;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <strong>LOGIC:</strong> An agent is locked if it cannot be accessed AND requires a paid tier.
    /// Core tier agents are never locked.
    /// </remarks>
    public bool IsLocked => !CanAccess && RequiredTier > LicenseTier.Core;

    /// <summary>
    /// Gets a comma-separated summary of the agent's capabilities.
    /// </summary>
    /// <value>
    /// A string like "Chat, Document Context, Style Enforcement" or an empty string if no capabilities.
    /// </value>
    /// <remarks>
    /// <strong>LOGIC:</strong> Uses <see cref="AgentCapabilitiesExtensions.ToDisplayString"/>
    /// to format the capabilities flags.
    /// </remarks>
    public string CapabilitiesSummary => Capabilities.ToDisplayString();

    /// <summary>
    /// Gets the full accessibility label for screen readers.
    /// </summary>
    /// <value>
    /// A concatenated string with agent name, description, capabilities, tier, and lock status.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>LOGIC:</strong> Constructs a descriptive label for accessibility tools:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"{Name}. {Description}."</description></item>
    ///   <item><description>"Capabilities: {CapabilitiesSummary}."</description></item>
    ///   <item><description>"Required tier: {TierBadgeText}." (if ShowTierBadge)</description></item>
    ///   <item><description>"Locked. Upgrade required." (if IsLocked)</description></item>
    ///   <item><description>"Favorite." (if IsFavorite)</description></item>
    ///   <item><description>"Selected." (if IsSelected)</description></item>
    /// </list>
    /// </remarks>
    public string AccessibilityLabel
    {
        get
        {
            var parts = new List<string> { $"{Name}. {Description}." };

            if (!string.IsNullOrWhiteSpace(CapabilitiesSummary))
            {
                parts.Add($"Capabilities: {CapabilitiesSummary}.");
            }

            if (ShowTierBadge)
            {
                parts.Add($"Required tier: {TierBadgeText}.");
            }

            if (IsLocked)
            {
                parts.Add("Locked. Upgrade required.");
            }

            if (IsFavorite)
            {
                parts.Add("Favorite.");
            }

            if (IsSelected)
            {
                parts.Add("Selected.");
            }

            return string.Join(" ", parts);
        }
    }
}
