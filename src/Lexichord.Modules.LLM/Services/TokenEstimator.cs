// -----------------------------------------------------------------------
// <copyright file="TokenEstimator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Configuration;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Services;

/// <summary>
/// Estimates token usage for chat completion requests.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TokenEstimator"/> calculates expected token counts for requests
/// to help manage context window limits before sending requests to providers.
/// </para>
/// <para>
/// <b>Token Counting Methodology:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Uses <see cref="ITokenCounter"/> for accurate content tokenization.</description></item>
///   <item><description>Adds per-message overhead for role markers and formatting.</description></item>
///   <item><description>Includes request overhead for assistant response priming.</description></item>
/// </list>
/// <para>
/// <b>Overhead Constants:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="MessageOverhead"/>: 4 tokens per message (role markers).</description></item>
///   <item><description><see cref="RequestOverhead"/>: 3 tokens for assistant response priming.</description></item>
///   <item><description><see cref="NameOverhead"/>: 1 additional token when message has a name.</description></item>
/// </list>
/// </remarks>
public class TokenEstimator
{
    private readonly ITokenCounter _tokenCounter;
    private readonly ModelRegistry _modelRegistry;
    private readonly ILogger<TokenEstimator> _logger;

    /// <summary>
    /// Per-message token overhead for role markers (e.g., &lt;|start|&gt;role&lt;|end|&gt;).
    /// </summary>
    public const int MessageOverhead = 4;

    /// <summary>
    /// Request-level token overhead for assistant response priming.
    /// </summary>
    public const int RequestOverhead = 3;

    /// <summary>
    /// Additional token overhead when a message includes a name.
    /// </summary>
    public const int NameOverhead = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenEstimator"/> class.
    /// </summary>
    /// <param name="tokenCounter">The token counter for content tokenization.</param>
    /// <param name="modelRegistry">The model registry for context window lookup.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public TokenEstimator(
        ITokenCounter tokenCounter,
        ModelRegistry modelRegistry,
        ILogger<TokenEstimator> logger)
    {
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Estimates the total tokens for a chat request.
    /// </summary>
    /// <param name="request">The chat request to estimate.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="TokenEstimate"/> containing prompt token count, available response tokens,
    /// and whether the request would exceed the context window.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The estimation process:
    /// </para>
    /// <list type="number">
    ///   <item><description>Count tokens for each message content using <see cref="ITokenCounter"/>.</description></item>
    ///   <item><description>Add <see cref="MessageOverhead"/> per message.</description></item>
    ///   <item><description>Add <see cref="NameOverhead"/> for messages with names.</description></item>
    ///   <item><description>Add <see cref="RequestOverhead"/> for the request.</description></item>
    ///   <item><description>Look up model context window and max output tokens.</description></item>
    ///   <item><description>Calculate available response tokens.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ChatRequest.FromUserMessage("Hello, world!");
    /// var estimate = await estimator.EstimateAsync(request);
    ///
    /// if (estimate.WouldExceedContext)
    /// {
    ///     // Reduce prompt size
    /// }
    /// else if (estimate.AvailableResponseTokens &lt; 100)
    /// {
    ///     // Warn about limited response space
    /// }
    /// </code>
    /// </example>
    public async Task<TokenEstimate> EstimateAsync(
        ChatRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var promptTokens = 0;
        var messageIndex = 0;

        foreach (var message in request.Messages)
        {
            // LOGIC: Count content tokens using the configured token counter.
            var contentTokens = _tokenCounter.CountTokens(message.Content ?? string.Empty);
            promptTokens += contentTokens;
            promptTokens += MessageOverhead;

            // NOTE: ChatMessage currently doesn't have a Name property.
            // If added in the future, uncomment the following to add overhead:
            // if (!string.IsNullOrEmpty(message.Name))
            // {
            //     promptTokens += _tokenCounter.CountTokens(message.Name);
            //     promptTokens += NameOverhead;
            // }

            LLMLogEvents.MessageTokenCount(
                _logger,
                messageIndex,
                message.Role.ToString(),
                contentTokens);

            messageIndex++;
        }

        // LOGIC: Add request-level overhead for assistant priming.
        promptTokens += RequestOverhead;

        // LOGIC: Determine model context limits.
        var model = request.Options?.Model ?? "gpt-4o-mini";
        var modelInfo = await _modelRegistry.GetModelInfoAsync(model, ct);

        var contextWindow = modelInfo?.ContextWindow ?? ModelDefaults.DefaultContextWindow;
        var maxOutput = modelInfo?.MaxOutputTokens ?? ModelDefaults.DefaultMaxOutputTokens;

        // LOGIC: Calculate available response tokens.
        // Available = min(requested_max_tokens, model_max_output, context_window - prompt_tokens)
        var requestedMaxTokens = request.Options?.MaxTokens ?? maxOutput;
        var availableForResponse = Math.Min(
            requestedMaxTokens,
            Math.Min(maxOutput, contextWindow - promptTokens));

        LLMLogEvents.TokenEstimation(
            _logger,
            promptTokens,
            Math.Max(0, availableForResponse),
            contextWindow);

        // LOGIC: Check if the prompt alone exceeds context window.
        var wouldExceed = promptTokens >= contextWindow;
        if (wouldExceed)
        {
            LLMLogEvents.ContextWindowExceeded(_logger, promptTokens, contextWindow);
        }

        return new TokenEstimate(
            EstimatedPromptTokens: promptTokens,
            AvailableResponseTokens: Math.Max(0, availableForResponse),
            ContextWindow: contextWindow,
            WouldExceedContext: wouldExceed);
    }

    /// <summary>
    /// Estimates tokens for a simple text prompt without conversation context.
    /// </summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <param name="model">The target model for context window limits. Defaults to "gpt-4o-mini".</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="TokenEstimate"/> for the text as a single user message.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that wraps the text in a <see cref="ChatRequest"/>
    /// and calls <see cref="EstimateAsync(ChatRequest, CancellationToken)"/>.
    /// </remarks>
    public async Task<TokenEstimate> EstimateTextAsync(
        string text,
        string model = "gpt-4o-mini",
        CancellationToken ct = default)
    {
        var request = ChatRequest.FromUserMessage(
            text ?? string.Empty,
            new ChatOptions(Model: model));

        return await EstimateAsync(request, ct);
    }

    /// <summary>
    /// Gets the raw token count for a piece of text without overhead calculations.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <returns>The number of tokens in the text.</returns>
    /// <remarks>
    /// This method provides direct access to the token counter without message
    /// or request overhead. Useful for comparing raw content sizes.
    /// </remarks>
    public int CountTokens(string text)
    {
        return _tokenCounter.CountTokens(text ?? string.Empty);
    }

    /// <summary>
    /// Calculates how many tokens are available for a response given a prompt token count.
    /// </summary>
    /// <param name="promptTokens">The number of tokens in the prompt.</param>
    /// <param name="model">The target model.</param>
    /// <param name="maxRequestedTokens">The maximum tokens requested for the response.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// The number of tokens available for the response, considering model limits.
    /// </returns>
    public async Task<int> CalculateAvailableResponseTokensAsync(
        int promptTokens,
        string model,
        int? maxRequestedTokens = null,
        CancellationToken ct = default)
    {
        var modelInfo = await _modelRegistry.GetModelInfoAsync(model, ct);
        var contextWindow = modelInfo?.ContextWindow ?? ModelDefaults.DefaultContextWindow;
        var maxOutput = modelInfo?.MaxOutputTokens ?? ModelDefaults.DefaultMaxOutputTokens;

        var remainingInContext = contextWindow - promptTokens;
        var effectiveMax = Math.Min(maxOutput, remainingInContext);

        if (maxRequestedTokens.HasValue)
        {
            effectiveMax = Math.Min(effectiveMax, maxRequestedTokens.Value);
        }

        return Math.Max(0, effectiveMax);
    }
}
