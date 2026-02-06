// -----------------------------------------------------------------------
// <copyright file="ConversationMetadata.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Models;

/// <summary>
/// Metadata about a conversation for context tracking.
/// </summary>
/// <remarks>
/// Tracks contextual information about where and how a conversation
/// was created, enabling better context restoration and analytics.
/// </remarks>
/// <param name="DocumentPath">Path to the document that was open when conversation started.</param>
/// <param name="SelectedModel">The AI model used for this conversation.</param>
/// <param name="TotalTokens">Cumulative token count across all messages.</param>
public record ConversationMetadata(
    string? DocumentPath,
    string? SelectedModel,
    int TotalTokens)
{
    /// <summary>
    /// Gets the provider name extracted from the model identifier.
    /// </summary>
    public string? ProviderName =>
        SelectedModel?.Split('/').FirstOrDefault();

    /// <summary>
    /// Gets the model name without provider prefix.
    /// </summary>
    public string? ModelName =>
        SelectedModel?.Split('/').LastOrDefault() ?? SelectedModel;

    /// <summary>
    /// Gets whether this conversation has document context.
    /// </summary>
    public bool HasDocumentContext => !string.IsNullOrEmpty(DocumentPath);

    /// <summary>
    /// Gets the document filename without path.
    /// </summary>
    public string? DocumentName =>
        DocumentPath is not null ? Path.GetFileName(DocumentPath) : null;

    /// <summary>
    /// Default metadata with no context.
    /// </summary>
    public static ConversationMetadata Default => new(null, null, 0);

    /// <summary>
    /// Creates metadata with document context.
    /// </summary>
    /// <param name="path">The document path.</param>
    /// <returns>Metadata with document path set.</returns>
    public static ConversationMetadata ForDocument(string path) =>
        new(path, null, 0);

    /// <summary>
    /// Creates metadata with model context.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>Metadata with model set.</returns>
    public static ConversationMetadata ForModel(string model) =>
        new(null, model, 0);

    /// <summary>
    /// Creates a copy with updated token count.
    /// </summary>
    /// <param name="tokens">The new total token count.</param>
    /// <returns>Updated metadata.</returns>
    public ConversationMetadata WithTokens(int tokens) =>
        this with { TotalTokens = tokens };

    /// <summary>
    /// Creates a copy with added tokens.
    /// </summary>
    /// <param name="additionalTokens">Tokens to add to the total.</param>
    /// <returns>Updated metadata.</returns>
    public ConversationMetadata AddTokens(int additionalTokens) =>
        this with { TotalTokens = TotalTokens + additionalTokens };
}
