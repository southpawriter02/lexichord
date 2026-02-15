// -----------------------------------------------------------------------
// <copyright file="ShowUpgradeModalEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a license upgrade modal should be displayed.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This MediatR notification is published when the user attempts
/// to use a feature that requires a higher license tier. The event is consumed
/// by the shell or dialog service which displays the upgrade modal with
/// appropriate messaging.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="FeatureName"/> - Display name of the feature requiring upgrade</description></item>
///   <item><description><see cref="RequiredTier"/> - The minimum license tier required</description></item>
///   <item><description><see cref="Message"/> - Descriptive message explaining the upgrade benefit</description></item>
///   <item><description><see cref="Timestamp"/> - When the upgrade prompt was triggered</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <param name="FeatureName">Display name of the feature requiring upgrade (e.g., "AI Rewriting").</param>
/// <param name="RequiredTier">The minimum license tier required to access the feature.</param>
/// <param name="Message">Descriptive message explaining the upgrade benefit to the user.</param>
/// <param name="Timestamp">When the upgrade prompt was triggered.</param>
/// <seealso cref="LicenseTier"/>
public record ShowUpgradeModalEvent(
    string FeatureName,
    LicenseTier RequiredTier,
    string Message,
    DateTime Timestamp
) : INotification
{
    /// <summary>
    /// Creates a new upgrade modal event with the current timestamp.
    /// </summary>
    /// <param name="featureName">Display name of the feature requiring upgrade.</param>
    /// <param name="requiredTier">The minimum license tier required.</param>
    /// <param name="message">Descriptive message explaining the upgrade benefit.</param>
    /// <returns>A new <see cref="ShowUpgradeModalEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory method that automatically sets the
    /// <see cref="Timestamp"/> to <see cref="DateTime.UtcNow"/>.
    /// </remarks>
    public static ShowUpgradeModalEvent Create(
        string featureName,
        LicenseTier requiredTier,
        string message) =>
        new(featureName, requiredTier, message, DateTime.UtcNow);

    /// <summary>
    /// Creates an upgrade modal event for Editor Agent features.
    /// </summary>
    /// <returns>A new <see cref="ShowUpgradeModalEvent"/> for the Editor Agent feature.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method with pre-configured messaging for Editor Agent
    /// features. Uses <see cref="LicenseTier.WriterPro"/> as the required tier.
    /// </remarks>
    public static ShowUpgradeModalEvent ForEditorAgent() =>
        Create(
            featureName: "AI Rewriting",
            requiredTier: LicenseTier.WriterPro,
            message: "Unlock AI-powered rewriting to transform your text with a single click. " +
                     "Rewrite formally, simplify complex text, expand ideas, or provide custom instructions.");
}
