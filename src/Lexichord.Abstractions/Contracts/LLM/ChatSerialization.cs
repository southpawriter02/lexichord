// -----------------------------------------------------------------------
// <copyright file="ChatSerialization.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Provides JSON serialization utilities for chat completion types.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides consistent JSON serialization/deserialization
/// for chat requests and responses. It uses camelCase property naming and
/// handles nullable types appropriately.
/// </para>
/// <para>
/// The serializer is configured with sensible defaults for LLM API compatibility:
/// case-insensitive property matching, camelCase naming, and ignoring null values.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Serialize a request
/// var request = ChatRequest.FromUserMessage("Hello!");
/// var json = ChatSerialization.ToJson(request);
///
/// // Deserialize a request
/// var restored = ChatSerialization.FromJson&lt;ChatRequest&gt;(json);
///
/// // Using custom options
/// var options = ChatSerialization.CreateOptions(writeIndented: true);
/// var prettyJson = JsonSerializer.Serialize(request, options);
/// </code>
/// </example>
public static class ChatSerialization
{
    /// <summary>
    /// The default JSON serializer options used by this class.
    /// </summary>
    /// <remarks>
    /// These options are configured for compatibility with most LLM APIs:
    /// camelCase property names, case-insensitive matching, and null value exclusion.
    /// </remarks>
    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions();

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <example>
    /// <code>
    /// var message = ChatMessage.User("Hello!");
    /// var json = ChatSerialization.ToJson(message);
    /// // Result: {"role":"User","content":"Hello!"}
    /// </code>
    /// </example>
    public static string ToJson<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        return JsonSerializer.Serialize(value, DefaultOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the target type.</exception>
    /// <example>
    /// <code>
    /// var json = "{\"role\":\"User\",\"content\":\"Hello!\"}";
    /// var message = ChatSerialization.FromJson&lt;ChatMessage&gt;(json);
    /// </code>
    /// </example>
    public static T FromJson<T>(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));
        return JsonSerializer.Deserialize<T>(json, DefaultOptions)
            ?? throw new JsonException($"Failed to deserialize JSON to type {typeof(T).Name}.");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">When successful, contains the deserialized object.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// var json = "{\"role\":\"User\",\"content\":\"Hello!\"}";
    /// if (ChatSerialization.TryFromJson&lt;ChatMessage&gt;(json, out var message))
    /// {
    ///     Console.WriteLine(message.Content);
    /// }
    /// </code>
    /// </example>
    public static bool TryFromJson<T>(string json, out T? result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return result is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new <see cref="JsonSerializerOptions"/> instance with the standard configuration.
    /// </summary>
    /// <param name="writeIndented">Whether to format the JSON with indentation.</param>
    /// <returns>A configured <see cref="JsonSerializerOptions"/> instance.</returns>
    /// <remarks>
    /// Use this method when you need to customize options beyond the defaults,
    /// such as enabling indented output for debugging.
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = ChatSerialization.CreateOptions(writeIndented: true);
    /// var prettyJson = JsonSerializer.Serialize(request, options);
    /// </code>
    /// </example>
    public static JsonSerializerOptions CreateOptions(bool writeIndented = false)
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = writeIndented,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Serializes a chat request to provider-agnostic JSON format.
    /// </summary>
    /// <param name="request">The chat request to serialize.</param>
    /// <returns>A JSON string suitable for logging or debugging.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <remarks>
    /// This method produces a standardized JSON format that can be used for
    /// logging, debugging, or storage. Provider implementations should use
    /// their own serialization for API calls.
    /// </remarks>
    public static string SerializeRequest(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        return ToJson(request);
    }

    /// <summary>
    /// Serializes a chat response to JSON format.
    /// </summary>
    /// <param name="response">The chat response to serialize.</param>
    /// <returns>A JSON string representation of the response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    /// <remarks>
    /// This method is useful for logging, caching, or storing responses.
    /// </remarks>
    public static string SerializeResponse(ChatResponse response)
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));
        return ToJson(response);
    }
}
