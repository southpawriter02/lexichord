// -----------------------------------------------------------------------
// <copyright file="OpenAIParameterMapper.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.LLM.Providers.OpenAI;

/// <summary>
/// Maps <see cref="ChatRequest"/> to OpenAI API request format.
/// </summary>
/// <remarks>
/// <para>
/// This mapper converts Lexichord's provider-agnostic <see cref="ChatRequest"/>
/// and <see cref="ChatOptions"/> into the JSON format expected by OpenAI's Chat Completions API.
/// </para>
/// <para>
/// <b>OpenAI API Reference:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Endpoint: POST /v1/chat/completions</description></item>
///   <item><description>Temperature range: 0.0 to 2.0</description></item>
///   <item><description>Stop sequences: "stop" array (max 4)</description></item>
///   <item><description>Penalty range: -2.0 to 2.0</description></item>
/// </list>
/// </remarks>
internal static class OpenAIParameterMapper
{
    /// <summary>
    /// The provider name used for role string conversion.
    /// </summary>
    private const string ProviderName = "openai";

    /// <summary>
    /// Converts a <see cref="ChatRequest"/> to an OpenAI-compatible JSON request body.
    /// </summary>
    /// <param name="request">The chat request to convert.</param>
    /// <returns>A <see cref="JsonObject"/> representing the OpenAI API request body.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The resulting JSON structure follows the OpenAI Chat Completions API format:
    /// </para>
    /// <code>
    /// {
    ///   "model": "gpt-4o-mini",
    ///   "messages": [...],
    ///   "temperature": 0.7,
    ///   "max_tokens": 2048,
    ///   "top_p": 1.0,
    ///   "frequency_penalty": 0.0,
    ///   "presence_penalty": 0.0,
    ///   "stop": ["```", "---"]
    /// }
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ChatRequest.FromUserMessage("Hello!", new ChatOptions(Temperature: 0.9));
    /// var jsonBody = OpenAIParameterMapper.ToRequestBody(request);
    /// var json = jsonBody.ToJsonString();
    /// </code>
    /// </example>
    public static JsonObject ToRequestBody(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var options = request.Options ?? new ChatOptions();

        var body = new JsonObject
        {
            ["model"] = options.Model ?? "gpt-4o-mini",
            ["messages"] = MapMessages(request.Messages)
        };

        // LOGIC: Only include parameters that have explicit values.
        // OpenAI will use its own defaults for omitted parameters.
        if (options.Temperature.HasValue)
        {
            body["temperature"] = options.Temperature.Value;
        }

        if (options.MaxTokens.HasValue)
        {
            body["max_tokens"] = options.MaxTokens.Value;
        }

        if (options.TopP.HasValue)
        {
            body["top_p"] = options.TopP.Value;
        }

        if (options.FrequencyPenalty.HasValue)
        {
            body["frequency_penalty"] = options.FrequencyPenalty.Value;
        }

        if (options.PresencePenalty.HasValue)
        {
            body["presence_penalty"] = options.PresencePenalty.Value;
        }

        // LOGIC: OpenAI uses "stop" for stop sequences.
        if (options.StopSequences.HasValue && !options.StopSequences.Value.IsDefaultOrEmpty)
        {
            body["stop"] = MapStopSequences(options.StopSequences.Value);
        }

        return body;
    }

    /// <summary>
    /// Maps an immutable array of messages to OpenAI message format.
    /// </summary>
    /// <param name="messages">The messages to convert.</param>
    /// <returns>A JSON array of message objects.</returns>
    private static JsonArray MapMessages(ImmutableArray<ChatMessage> messages)
    {
        var array = new JsonArray();

        foreach (var message in messages)
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
    /// Extracts chat options from an OpenAI API response usage object.
    /// </summary>
    /// <param name="usage">The usage JSON object from the response.</param>
    /// <returns>A tuple containing prompt tokens and completion tokens.</returns>
    /// <remarks>
    /// This method parses the token usage information returned in OpenAI responses.
    /// </remarks>
    public static (int PromptTokens, int CompletionTokens) ParseUsage(JsonObject? usage)
    {
        if (usage is null)
        {
            return (0, 0);
        }

        var promptTokens = usage["prompt_tokens"]?.GetValue<int>() ?? 0;
        var completionTokens = usage["completion_tokens"]?.GetValue<int>() ?? 0;

        return (promptTokens, completionTokens);
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

        if (enableStreaming)
        {
            // LOGIC: Request usage in streaming mode (requires OpenAI API version support).
            body["stream_options"] = new JsonObject
            {
                ["include_usage"] = true
            };
        }

        return body;
    }
}
