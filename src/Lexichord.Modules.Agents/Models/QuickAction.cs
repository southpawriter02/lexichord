// -----------------------------------------------------------------------
// <copyright file="QuickAction.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Models;

/// <summary>
/// Defines a quick action for the floating toolbar.
/// </summary>
/// <remarks>
/// <para>
/// Quick actions are pre-defined or custom AI operations that can be
/// invoked with a single click or keyboard shortcut from the quick
/// actions panel. Each action is associated with a prompt template
/// and target agent.
/// </para>
/// <para>
/// Actions may be filtered by content type (e.g., code-specific actions
/// only appear when the cursor is in a code block) and by license tier
/// (e.g., custom actions require Teams tier).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
/// <param name="ActionId">Unique identifier for the action (e.g., "improve", "simplify").</param>
/// <param name="Name">Display name shown in the toolbar button.</param>
/// <param name="Description">Tooltip description explaining the action's purpose.</param>
/// <param name="Icon">Icon name from the icon set (e.g., "IconSparkle", "IconMinimize").</param>
/// <param name="PromptTemplateId">The prompt template ID to use when executing this action.</param>
/// <param name="AgentId">Optional specific agent to use; null means use the default agent.</param>
/// <param name="KeyboardShortcut">Optional keyboard shortcut (e.g., "1", "2") for numbered access.</param>
/// <param name="RequiresSelection">Whether text selection is required to enable this action.</param>
/// <param name="SupportedContentTypes">
/// Content types this action applies to. Null or empty means the action applies to all content types.
/// </param>
/// <param name="MinimumLicenseTier">Minimum license tier required to use this action.</param>
/// <param name="Order">Display order in the toolbar (lower values appear first).</param>
/// <example>
/// <code>
/// var improveAction = new QuickAction(
///     ActionId: "improve",
///     Name: "Improve",
///     Description: "Enhance writing quality and clarity",
///     Icon: "IconSparkle",
///     PromptTemplateId: "quick-improve",
///     KeyboardShortcut: "1",
///     RequiresSelection: true,
///     SupportedContentTypes: new[] { ContentBlockType.Prose },
///     MinimumLicenseTier: LicenseTier.WriterPro,
///     Order: 0);
/// </code>
/// </example>
public record QuickAction(
    string ActionId,
    string Name,
    string Description,
    string Icon,
    string PromptTemplateId,
    string? AgentId = null,
    string? KeyboardShortcut = null,
    bool RequiresSelection = true,
    IReadOnlyList<ContentBlockType>? SupportedContentTypes = null,
    LicenseTier MinimumLicenseTier = LicenseTier.WriterPro,
    int Order = 100)
{
    /// <summary>
    /// Gets whether this action supports all content types.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true when <see cref="SupportedContentTypes"/> is null or empty,
    /// indicating that the action is universally applicable regardless of the
    /// document content type at the cursor position.
    /// </remarks>
    public bool AppliesToAllContentTypes =>
        SupportedContentTypes is null || SupportedContentTypes.Count == 0;

    /// <summary>
    /// Checks if this action applies to the specified content type.
    /// </summary>
    /// <param name="contentType">The content type to check.</param>
    /// <returns>
    /// <c>true</c> if this action applies to the content type (or applies to all types);
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Universal actions (no content type filter) return true for any type.
    /// Targeted actions only return true if the specified type is in the
    /// <see cref="SupportedContentTypes"/> list.
    /// </remarks>
    public bool AppliesTo(ContentBlockType contentType)
    {
        return AppliesToAllContentTypes || SupportedContentTypes!.Contains(contentType);
    }
}
