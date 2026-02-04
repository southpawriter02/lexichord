// -----------------------------------------------------------------------
// <copyright file="AnthropicParameterMapper.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Maps <see cref="ChatRequest"/> to Anthropic API request format.
/// </summary>
/// <remarks>
/// <para>
/// This mapper converts Lexichord's provider-agnostic <see cref="ChatRequest"/>
/// and <see cref="ChatOptions"/> into the JSON format expected by Anthropic's Messages API.
/// </para>
/// <para>
/// <b>Anthropic API Differences:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Temperature range: 0.0 to 1.0 (not 0.0 to 2.0)</description></item>
///   <item><description>System messages: Separate "system" field (not in messages array)</description></item>
///   <item><description>Stop sequences: "stop_sequences" (not "stop")</description></item>
///   <item><description>No frequency/presence penalty support</description></item>
/// </list>
/// </remarks>
internal static class AnthropicParameterMapper
{
    /// <summary>
    /// The provider name used for role string conversion.
    /// </summary>
    private const string ProviderName = "anthropic";

    /// <summary>
    /// Anthropic's maximum temperature value.
    /// </summary>
    private const double AnthropicMaxTemperature = 1.0;

    /// <summary>
    /// Standard temperature maximum (OpenAI scale) for remapping.
    /// </summary>
    private const double StandardMaxTemperature = 2.0;

    /// <summary>
    /// Converts a <see cref="ChatRequest"/> to an Anthropic-compatible JSON request body.
    /// </summary>
    /// <param name="request">The chat request to convert.</param>
    /// <returns>A <see cref="JsonObject"/> representing the Anthropic API request body.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The resulting JSON structure follows the Anthropic Messages API format:
    /// </para>
    /// <code>
    /// {
    ///   "model": "claude-3-haiku-20240307",
    ///   "max_tokens": 2048,
    ///   "system": "You are a helpful assistant.",
    ///   "messages": [...],
    ///   "temperature": 0.35,
    ///   "top_p": 1.0,
    ///   "stop_sequences": ["```", "---"]
    /// }
    /// </code>
    /// <para>
    /// <b>Temperature Mapping:</b>
    /// </para>
    /// <para>
    /// Anthropic uses a 0.0-1.0 temperature range, while Lexichord uses 0.0-2.0
    /// (following OpenAI's convention). This mapper automatically scales the temperature
    /// by dividing by 2.0, so a Lexichord temperature of 1.4 becomes 0.7 for Anthropic.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ChatRequest.WithSystemPrompt(
    ///     "You are a helpful assistant.",
    ///     "Hello!",
    ///     new ChatOptions(Temperature: 1.4)); // Will become 0.7 for Anthropic
    ///
    /// var jsonBody = AnthropicParameterMapper.ToRequestBody(request);
    /// </code>
    /// </example>
    public static JsonObject ToRequestBody(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var options = request.Options ?? new ChatOptions();

        var body = new JsonObject
        {
            ["model"] = options.Model ?? "claude-3-haiku-20240307",
            ["messages"] = MapMessages(request.Messages)
        };

        // LOGIC: max_tokens is required for Anthropic API.
        body["max_tokens"] = options.MaxTokens ?? 2048;

        // LOGIC: Extract system message to separate field (Anthropic convention).
        var systemMessage = request.Messages.FirstOrDefault(m => m.Role == ChatRole.System);
        if (systemMessage is not null)
        {
            body["system"] = systemMessage.Content;
        }

        // LOGIC: Map temperature from 0-2 scale to 0-1 scale.
        if (options.Temperature.HasValue)
        {
            body["temperature"] = ClampTemperature(options.Temperature.Value);
        }

        if (options.TopP.HasValue)
        {
            body["top_p"] = options.TopP.Value;
        }

        // NOTE: Anthropic does not support frequency_penalty or presence_penalty.
        // These parameters are intentionally omitted.

        // LOGIC: Anthropic uses "stop_sequences" instead of "stop".
        if (options.StopSequences.HasValue && !options.StopSequences.Value.IsDefaultOrEmpty)
        {
            body["stop_sequences"] = MapStopSequences(options.StopSequences.Value);
        }

        return body;
    }

    /// <summary>
    /// Maps messages to Anthropic format, excluding system messages.
    /// </summary>
    /// <param name="messages">The messages to convert.</param>
    /// <returns>A JSON array of message objects.</returns>
    /// <remarks>
    /// Anthropic requires system messages to be sent separately in the "system" field,
    /// not in the messages array. This method filters out system messages.
    /// </remarks>
    private static JsonArray MapMessages(ImmutableArray<ChatMessage> messages)
    {
        var array = new JsonArray();

        // LOGIC: Anthropic's messages array should not include system messages.
        // System content is sent via the separate "system" field.
        foreach (var message in messages.Where(m => m.Role != ChatRole.System))
        {
            var messageObj = new JsonObject
            {
                ["role"] = message.Role.ToProviderString(ProviderName),
                ["content"] = message.Content
            };

            array.Add(messageObj);
        }

        return array;
    }

    /// <summary>
    /// Maps stop sequences to a JSON array.
    /// </summary>
    /// <param name="stopSequences">The stop sequences to convert.</param>
    /// <returns>A JSON array of stop sequence strings.</returns>
    private static JsonArray MapStopSequences(ImmutableArray<string> stopSequences)
    {
        var array = new JsonArray();

        foreach (var sequence in stopSequences)
        {
            array.Add(JsonValue.Create(sequence));
        }

        return array;
    }

    /// <summary>
    /// Clamps and scales temperature from the standard 0-2 range to Anthropic's 0-1 range.
    /// </summary>
    /// <param name="temperature">The temperature in 0-2 scale.</param>
    /// <returns>The scaled temperature in 0-1 scale.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following transformation:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Input: 0.0-2.0 (standard/OpenAI scale)</description></item>
    ///   <item><description>Output: 0.0-1.0 (Anthropic scale)</description></item>
    ///   <item><description>Formula: output = min(1.0, input / 2.0)</description></item>
    /// </list>
    /// <para>
    /// Examples:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>0.0 → 0.0 (deterministic)</description></item>
    ///   <item><description>0.7 → 0.35 (balanced)</description></item>
    ///   <item><description>1.4 → 0.7 (creative)</description></item>
    ///   <item><description>2.0 → 1.0 (maximum randomness)</description></item>
    /// </list>
    /// </remarks>
    public static double ClampTemperature(double temperature)
    {
        // LOGIC: Scale from 0-2 to 0-1, then clamp to valid range.
        var scaled = temperature / StandardMaxTemperature;
        return Math.Clamp(scaled, 0.0, AnthropicMaxTemperature);
    }

    /// <summary>
    /// Extracts token usage from an Anthropic API response usage object.
    /// </summary>
    /// <param name="usage">The usage JSON object from the response.</param>
    /// <returns>A tuple containing input tokens and output tokens.</returns>
    /// <remarks>
    /// Anthropic uses "input_tokens" and "output_tokens" instead of
    /// "prompt_tokens" and "completion_tokens".
    /// </remarks>
    public static (int InputTokens, int OutputTokens) ParseUsage(JsonObject? usage)
    {
        if (usage is null)
        {
            return (0, 0);
        }

        var inputTokens = usage["input_tokens"]?.GetValue<int>() ?? 0;
        var outputTokens = usage["output_tokens"]?.GetValue<int>() ?? 0;

        return (inputTokens, outputTokens);
    }

    /// <summary>
    /// Adds streaming configuration to the request body.
    /// </summary>
    /// <param name="body">The request body to modify.</param>
    /// <param name="enableStreaming">Whether to enable streaming.</param>
    /// <returns>The modified request body.</returns>
    public static JsonObject WithStreaming(JsonObject body, bool enableStreaming = true)
    {
        ArgumentNullException.ThrowIfNull(body, nameof(body));

        body["stream"] = enableStreaming;
        return body;
    }
}
