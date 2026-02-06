// -----------------------------------------------------------------------
// <copyright file="ConversationExportOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Models;

/// <summary>
/// Options for customizing conversation export.
/// </summary>
/// <param name="IncludeMetadata">Whether to include metadata header.</param>
/// <param name="IncludeTimestamps">Whether to include message timestamps.</param>
/// <param name="IncludeSystemMessages">Whether to include system messages.</param>
/// <param name="UseEmoji">Whether to use emoji for role indicators.</param>
public record ConversationExportOptions(
    bool IncludeMetadata = true,
    bool IncludeTimestamps = false,
    bool IncludeSystemMessages = false,
    bool UseEmoji = true)
{
    /// <summary>
    /// Default export options.
    /// </summary>
    public static ConversationExportOptions Default => new();

    /// <summary>
    /// Minimal export with just messages.
    /// </summary>
    public static ConversationExportOptions Minimal => new(
        IncludeMetadata: false,
        IncludeTimestamps: false,
        IncludeSystemMessages: false,
        UseEmoji: false);

    /// <summary>
    /// Full export with all details.
    /// </summary>
    public static ConversationExportOptions Full => new(
        IncludeMetadata: true,
        IncludeTimestamps: true,
        IncludeSystemMessages: true,
        UseEmoji: true);
}
