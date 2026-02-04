// -----------------------------------------------------------------------
// <copyright file="ChatRoleExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Extension methods for <see cref="ChatRole"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides utility methods for working with chat roles,
/// particularly for converting roles to provider-specific string representations.
/// </para>
/// </remarks>
public static class ChatRoleExtensions
{
    /// <summary>
    /// Converts a <see cref="ChatRole"/> to a provider-specific role string.
    /// </summary>
    /// <param name="role">The chat role to convert.</param>
    /// <param name="provider">The provider name (e.g., "openai", "anthropic").</param>
    /// <returns>The provider-specific role string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="provider"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Different LLM providers use slightly different role names. This method
    /// maps the generic <see cref="ChatRole"/> to the appropriate string for each provider.
    /// </para>
    /// <para>
    /// Known provider mappings:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Role</term>
    ///     <description>OpenAI / Anthropic</description>
    ///   </listheader>
    ///   <item>
    ///     <term>System</term>
    ///     <description>"system"</description>
    ///   </item>
    ///   <item>
    ///     <term>User</term>
    ///     <description>"user"</description>
    ///   </item>
    ///   <item>
    ///     <term>Assistant</term>
    ///     <description>"assistant"</description>
    ///   </item>
    ///   <item>
    ///     <term>Tool</term>
    ///     <description>OpenAI: "tool", Anthropic: "tool_result"</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var role = ChatRole.Tool;
    /// var openAiRole = role.ToProviderString("openai");   // "tool"
    /// var anthropicRole = role.ToProviderString("anthropic"); // "tool_result"
    /// </code>
    /// </example>
    public static string ToProviderString(this ChatRole role, string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider, nameof(provider));

        return (role, provider.ToLowerInvariant()) switch
        {
            (ChatRole.System, "openai") => "system",
            (ChatRole.System, "anthropic") => "system",
            (ChatRole.User, _) => "user",
            (ChatRole.Assistant, "openai") => "assistant",
            (ChatRole.Assistant, "anthropic") => "assistant",
            (ChatRole.Tool, "openai") => "tool",
            (ChatRole.Tool, "anthropic") => "tool_result",
            _ => role.ToString().ToLowerInvariant()
        };
    }

    /// <summary>
    /// Parses a provider role string to a <see cref="ChatRole"/>.
    /// </summary>
    /// <param name="roleString">The role string from a provider response.</param>
    /// <returns>The corresponding <see cref="ChatRole"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="roleString"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="roleString"/> is empty, whitespace, or unrecognized.
    /// </exception>
    /// <remarks>
    /// This method handles common role string variations from different providers.
    /// </remarks>
    /// <example>
    /// <code>
    /// var role = ChatRoleExtensions.FromProviderString("assistant"); // ChatRole.Assistant
    /// var role = ChatRoleExtensions.FromProviderString("tool_result"); // ChatRole.Tool
    /// </code>
    /// </example>
    public static ChatRole FromProviderString(string roleString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleString, nameof(roleString));

        return roleString.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            "tool_result" => ChatRole.Tool,
            "function" => ChatRole.Tool, // Legacy OpenAI term
            _ => throw new ArgumentException($"Unrecognized role string: '{roleString}'.", nameof(roleString))
        };
    }

    /// <summary>
    /// Gets the default display name for a <see cref="ChatRole"/>.
    /// </summary>
    /// <param name="role">The chat role.</param>
    /// <returns>A human-readable display name for the role.</returns>
    /// <example>
    /// <code>
    /// var name = ChatRole.Assistant.GetDisplayName(); // "AI Assistant"
    /// </code>
    /// </example>
    public static string GetDisplayName(this ChatRole role)
    {
        return role switch
        {
            ChatRole.System => "System",
            ChatRole.User => "User",
            ChatRole.Assistant => "AI Assistant",
            ChatRole.Tool => "Tool Result",
            _ => role.ToString()
        };
    }

    /// <summary>
    /// Gets whether the role represents AI-generated content.
    /// </summary>
    /// <param name="role">The chat role to check.</param>
    /// <returns>True if the role is <see cref="ChatRole.Assistant"/>; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// if (message.Role.IsAiGenerated())
    /// {
    ///     // Apply AI content styling
    /// }
    /// </code>
    /// </example>
    public static bool IsAiGenerated(this ChatRole role) => role == ChatRole.Assistant;

    /// <summary>
    /// Gets whether the role represents human input.
    /// </summary>
    /// <param name="role">The chat role to check.</param>
    /// <returns>True if the role is <see cref="ChatRole.User"/>; otherwise, false.</returns>
    public static bool IsHumanInput(this ChatRole role) => role == ChatRole.User;

    /// <summary>
    /// Gets whether the role is a metadata/control role (System or Tool).
    /// </summary>
    /// <param name="role">The chat role to check.</param>
    /// <returns>True if the role is System or Tool; otherwise, false.</returns>
    public static bool IsMetadata(this ChatRole role)
        => role == ChatRole.System || role == ChatRole.Tool;
}
