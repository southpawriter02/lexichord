// -----------------------------------------------------------------------
// <copyright file="StyleRuleContextItem.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// Represents a style rule displayed in the context panel.
/// </summary>
/// <remarks>
/// <para>
/// Style rules are loaded from the active style guide and provide writing
/// guidance to the AI assistant. Each rule contributes to the context token
/// budget and can be individually toggled by the user.
/// </para>
/// <para>
/// The record provides computed properties for UI display including:
/// </para>
/// <list type="bullet">
///   <item><description>Category icons for visual categorization</description></item>
///   <item><description>Severity icons for rule importance</description></item>
///   <item><description>Truncated names for compact display</description></item>
///   <item><description>Tooltip text for full rule details</description></item>
/// </list>
/// </remarks>
/// <param name="Id">Unique identifier for the style rule.</param>
/// <param name="Name">Display name of the rule.</param>
/// <param name="Description">Full description of what the rule enforces.</param>
/// <param name="Category">Category grouping (Grammar, Style, Voice, etc.).</param>
/// <param name="EstimatedTokens">Estimated token count for this rule's content.</param>
/// <param name="IsActive">Whether the rule is currently active in the style guide.</param>
/// <param name="Severity">The severity level of rule violations.</param>
/// <seealso cref="ContextPanelViewModel"/>
/// <seealso cref="ContextSnapshot"/>
public sealed record StyleRuleContextItem(
    string Id,
    string Name,
    string Description,
    string Category,
    int EstimatedTokens,
    bool IsActive,
    ViolationSeverity Severity = ViolationSeverity.Info)
{
    /// <summary>
    /// Maximum length for the short name display.
    /// </summary>
    private const int MaxShortNameLength = 25;

    /// <summary>
    /// Gets the emoji icon representing the rule's category.
    /// </summary>
    /// <value>
    /// An emoji character representing the category:
    /// <list type="bullet">
    ///   <item><description>📝 Grammar</description></item>
    ///   <item><description>🎨 Style</description></item>
    ///   <item><description>🗣️ Voice</description></item>
    ///   <item><description>📋 Formatting</description></item>
    ///   <item><description>📖 Terminology</description></item>
    ///   <item><description>✏️ Punctuation</description></item>
    ///   <item><description>📌 Other/Unknown</description></item>
    /// </list>
    /// </value>
    public string CategoryIcon => Category switch
    {
        "Grammar" => "📝",
        "Style" => "🎨",
        "Voice" => "🗣️",
        "Formatting" => "📋",
        "Terminology" => "📖",
        "Punctuation" => "✏️",
        _ => "📌"
    };

    /// <summary>
    /// Gets the emoji icon representing the rule's severity level.
    /// </summary>
    /// <value>
    /// An emoji character representing the severity:
    /// <list type="bullet">
    ///   <item><description>🔴 Error</description></item>
    ///   <item><description>🟡 Warning</description></item>
    ///   <item><description>🔵 Information</description></item>
    ///   <item><description>⚪ Hint</description></item>
    /// </list>
    /// </value>
    public string SeverityIcon => Severity switch
    {
        ViolationSeverity.Error => "🔴",
        ViolationSeverity.Warning => "🟡",
        ViolationSeverity.Info => "🔵",
        ViolationSeverity.Hint => "⚪",
        _ => "⚪"
    };

    /// <summary>
    /// Gets the full display name combining category and rule name.
    /// </summary>
    /// <value>Format: "Category: Name" (e.g., "Grammar: Active Voice").</value>
    public string DisplayName => $"{Category}: {Name}";

    /// <summary>
    /// Gets a truncated version of the name for compact display.
    /// </summary>
    /// <value>
    /// The name truncated to <see cref="MaxShortNameLength"/> characters
    /// with "..." appended if truncation occurred.
    /// </value>
    public string ShortName => Name.Length <= MaxShortNameLength
        ? Name
        : Name[..(MaxShortNameLength - 3)] + "...";

    /// <summary>
    /// Gets the full tooltip text for hover display.
    /// </summary>
    /// <value>
    /// Multi-line text containing:
    /// <list type="bullet">
    ///   <item><description>Display name (Category: Name)</description></item>
    ///   <item><description>Full description</description></item>
    ///   <item><description>Token count</description></item>
    /// </list>
    /// </value>
    public string TooltipText => $"{DisplayName}\n{Description}\n{EstimatedTokens} tokens";
}
