// -----------------------------------------------------------------------
// <copyright file="RenderedPrompt.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Result of rendering a prompt template.
/// Contains both raw strings and ready-to-send messages.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RenderedPrompt"/> record is produced by <see cref="IPromptRenderer.RenderMessages"/>
/// and contains all the information needed to submit a prompt to an LLM:
/// </para>
/// <list type="bullet">
///   <item><description>Raw rendered strings for logging and debugging</description></item>
///   <item><description>Ready-to-use <see cref="ChatMessage"/> array for API submission</description></item>
///   <item><description>Performance metrics for monitoring render times</description></item>
/// </list>
/// <para>
/// Use <see cref="EstimatedTokens"/> for quick token estimation, or use
/// <c>ILLMTokenCounter</c> (v0.6.2d) for accurate model-specific counts.
/// </para>
/// </remarks>
/// <param name="SystemPrompt">The rendered system prompt string after variable substitution.</param>
/// <param name="UserPrompt">The rendered user prompt string after variable substitution.</param>
/// <param name="Messages">Ready-to-send ChatMessage array for LLM submission.</param>
/// <param name="RenderDuration">Time taken to render the template.</param>
/// <example>
/// <code>
/// // Render a template and access the results
/// var rendered = renderer.RenderMessages(template, variables);
///
/// // Access the raw strings for logging
/// logger.LogDebug("System prompt: {SystemPrompt}", rendered.SystemPrompt);
///
/// // Use the messages for LLM submission
/// var response = await chatService.CompleteAsync(new ChatRequest(rendered.Messages));
///
/// // Check performance metrics
/// if (!rendered.WasFastRender)
/// {
///     logger.LogWarning("Slow render: {Duration}ms", rendered.RenderDuration.TotalMilliseconds);
/// }
/// </code>
/// </example>
public record RenderedPrompt(
    string SystemPrompt,
    string UserPrompt,
    ChatMessage[] Messages,
    TimeSpan RenderDuration)
{
    /// <summary>
    /// Gets the rendered system prompt string after variable substitution.
    /// </summary>
    /// <value>The rendered system prompt. Never null.</value>
    public string SystemPrompt { get; init; } = SystemPrompt ?? string.Empty;

    /// <summary>
    /// Gets the rendered user prompt string after variable substitution.
    /// </summary>
    /// <value>The rendered user prompt. Never null.</value>
    public string UserPrompt { get; init; } = UserPrompt ?? string.Empty;

    /// <summary>
    /// Gets the ready-to-send ChatMessage array for LLM submission.
    /// </summary>
    /// <value>The rendered messages. Never null.</value>
    /// <remarks>
    /// Typically contains a [System, User] message pair, but may contain
    /// additional messages depending on the template structure.
    /// </remarks>
    public ChatMessage[] Messages { get; init; } = Messages ?? Array.Empty<ChatMessage>();

    /// <summary>
    /// Gets the approximate token count for the rendered prompts.
    /// Uses character-based estimation (4 characters ≈ 1 token).
    /// </summary>
    /// <value>The estimated token count.</value>
    /// <remarks>
    /// <para>
    /// This is a rough approximation based on the typical ratio of 4 characters
    /// per token. Actual token counts vary by model and content.
    /// </para>
    /// <para>
    /// For accurate token counting, use <c>ILLMTokenCounter</c> from v0.6.2d
    /// which provides model-specific tokenization.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rendered = renderer.RenderMessages(template, variables);
    ///
    /// // Quick estimation for budget checking
    /// if (rendered.EstimatedTokens > 4000)
    /// {
    ///     logger.LogWarning("Large prompt: ~{Tokens} tokens", rendered.EstimatedTokens);
    /// }
    /// </code>
    /// </example>
    public int EstimatedTokens => (SystemPrompt.Length + UserPrompt.Length) / 4;

    /// <summary>
    /// Gets the total character count across all prompts.
    /// </summary>
    /// <value>The sum of <see cref="SystemPrompt"/> and <see cref="UserPrompt"/> lengths.</value>
    /// <example>
    /// <code>
    /// var rendered = renderer.RenderMessages(template, variables);
    /// logger.LogDebug("Total prompt size: {Chars} characters", rendered.TotalCharacters);
    /// </code>
    /// </example>
    public int TotalCharacters => SystemPrompt.Length + UserPrompt.Length;

    /// <summary>
    /// Gets a value indicating whether rendering completed quickly (under 10ms).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="RenderDuration"/> is less than 10 milliseconds;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Most template renders should complete in under 10ms. Slower renders may indicate
    /// complex templates, large variable values, or performance issues.
    /// </remarks>
    /// <example>
    /// <code>
    /// var rendered = renderer.RenderMessages(template, variables);
    /// if (!rendered.WasFastRender)
    /// {
    ///     logger.LogWarning(
    ///         "Slow render for template {TemplateId}: {Duration}ms",
    ///         template.TemplateId,
    ///         rendered.RenderDuration.TotalMilliseconds);
    /// }
    /// </code>
    /// </example>
    public bool WasFastRender => RenderDuration.TotalMilliseconds < 10;

    /// <summary>
    /// Gets the number of messages in the <see cref="Messages"/> array.
    /// </summary>
    /// <value>The count of messages.</value>
    /// <remarks>
    /// Standard renders produce 2 messages (System, User).
    /// Multi-turn templates may produce additional messages.
    /// </remarks>
    public int MessageCount => Messages.Length;

    /// <summary>
    /// Gets a value indicating whether this prompt contains a system message.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="SystemPrompt"/> is not empty;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasSystemPrompt => !string.IsNullOrEmpty(SystemPrompt);

    /// <summary>
    /// Gets a value indicating whether this prompt contains a user message.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="UserPrompt"/> is not empty;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasUserPrompt => !string.IsNullOrEmpty(UserPrompt);
}
