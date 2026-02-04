// -----------------------------------------------------------------------
// <copyright file="ChatRole.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Defines the role of a participant in a chat conversation.
/// </summary>
/// <remarks>
/// <para>
/// Chat roles follow the standard conversation model used by most LLM APIs:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="System"/>: Instructions that guide model behavior</description></item>
///   <item><description><see cref="User"/>: Human user input</description></item>
///   <item><description><see cref="Assistant"/>: AI model responses</description></item>
///   <item><description><see cref="Tool"/>: Function/tool call results</description></item>
/// </list>
/// <para>
/// The integer values correspond to typical ordering in conversation history.
/// </para>
/// </remarks>
public enum ChatRole
{
    /// <summary>
    /// System instruction message that guides the model's behavior.
    /// </summary>
    /// <remarks>
    /// System messages are typically placed at the beginning of the conversation
    /// and define the persona, constraints, or context for the AI assistant.
    /// Not all providers support system messages in the same way.
    /// </remarks>
    System = 0,

    /// <summary>
    /// User message representing human input.
    /// </summary>
    /// <remarks>
    /// User messages contain the questions, prompts, or requests from the human
    /// participant in the conversation.
    /// </remarks>
    User = 1,

    /// <summary>
    /// Assistant message representing AI model output.
    /// </summary>
    /// <remarks>
    /// Assistant messages contain the AI's responses. When providing conversation
    /// history, these represent previous AI outputs.
    /// </remarks>
    Assistant = 2,

    /// <summary>
    /// Tool response message containing function call results.
    /// </summary>
    /// <remarks>
    /// Tool messages are used when the model invokes a function and the result
    /// needs to be provided back. The exact format varies by provider.
    /// </remarks>
    Tool = 3
}
