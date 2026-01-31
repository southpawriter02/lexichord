// <copyright file="IProfileSelectorViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Windows.Input;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// ViewModel contract for the voice profile selector widget.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4d - Defines the contract for the status bar widget that displays
/// the current voice profile and allows profile switching via context menu.</para>
/// <para>The selector shows a compact display with the profile name and provides
/// a rich tooltip with full profile details.</para>
/// </remarks>
public interface IProfileSelectorViewModel
{
    /// <summary>
    /// Gets the display name of the currently active voice profile.
    /// </summary>
    /// <example>"Technical", "Marketing", "Academic"</example>
    string CurrentProfileName { get; }

    /// <summary>
    /// Gets the rich tooltip content displaying full profile configuration.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: The tooltip includes:</para>
    /// <list type="bullet">
    ///   <item>Profile name and description</item>
    ///   <item>Target grade level and tolerance</item>
    ///   <item>Max sentence length</item>
    ///   <item>Passive voice settings</item>
    ///   <item>Adverb/weasel word flags</item>
    /// </list>
    /// </remarks>
    string ProfileTooltip { get; }

    /// <summary>
    /// Gets a value indicating whether a profile is currently active.
    /// </summary>
    /// <remarks>Always true after initialization (defaults to Technical profile).</remarks>
    bool HasActiveProfile { get; }

    /// <summary>
    /// Gets all available profiles for the context menu.
    /// </summary>
    IReadOnlyList<VoiceProfile> AvailableProfiles { get; }

    /// <summary>
    /// Gets the command to select a specific profile.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: The command parameter is the profile ID (Guid) to select.</para>
    /// <para>Execution triggers IVoiceProfileService.SetActiveProfileAsync and
    /// publishes ProfileChangedEvent via MediatR.</para>
    /// </remarks>
    ICommand SelectProfileCommand { get; }

    /// <summary>
    /// Refreshes the profile list and current selection from the service.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RefreshAsync(CancellationToken ct = default);
}
