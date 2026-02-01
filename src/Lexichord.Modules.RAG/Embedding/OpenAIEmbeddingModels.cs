// =============================================================================
// File: OpenAIEmbeddingModels.cs
// Project: Lexichord.Modules.RAG
// Description: Internal DTOs for OpenAI Embeddings API request/response models.
// =============================================================================
// LOGIC: Serialization-focused records for OpenAI API communication.
//   - OpenAIEmbeddingRequest: Maps to the /v1/embeddings endpoint request payload.
//   - OpenAIEmbeddingResponse: Maps the response envelope with metadata and embeddings.
//   - OpenAIEmbeddingData: Represents individual embedding vectors from the response.
//   - OpenAIUsage: Token usage statistics from the API response.
//   - All use System.Text.Json.Serialization for modern JSON handling.
//   - JsonPropertyName attributes handle snake_case API field names (e.g., "encoding_format").
// =============================================================================

using System.Text.Json.Serialization;

namespace Lexichord.Modules.RAG.Embedding;

/// <summary>
/// Request payload for the OpenAI Embeddings API.
/// </summary>
/// <remarks>
/// <para>
/// Maps to POST /v1/embeddings endpoint. Supports both single strings and
/// arrays of strings for batch operations.
/// </para>
/// </remarks>
internal record OpenAIEmbeddingRequest
{
    /// <summary>
    /// The model identifier (e.g., "text-embedding-3-small").
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// The input text(s) to embed. Can be a single string or array of strings.
    /// </summary>
    [JsonPropertyName("input")]
    public IReadOnlyList<string> Input { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The number of dimensions for the embedding (optional, for reducing dimensions).
    /// </summary>
    [JsonPropertyName("dimensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Dimensions { get; init; }

    /// <summary>
    /// The format for the embedding response (default "float").
    /// </summary>
    [JsonPropertyName("encoding_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncodingFormat { get; init; }
}

/// <summary>
/// Response from the OpenAI Embeddings API.
/// </summary>
/// <remarks>
/// <para>
/// Returned from POST /v1/embeddings endpoint. Contains the embeddings array,
/// model information, and token usage statistics.
/// </para>
/// </remarks>
internal record OpenAIEmbeddingResponse
{
    /// <summary>
    /// The type of object returned (typically "list").
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    /// <summary>
    /// Array of embedding objects, one per input text.
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<OpenAIEmbeddingData> Data { get; init; } = Array.Empty<OpenAIEmbeddingData>();

    /// <summary>
    /// The model identifier used to generate the embeddings.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Token usage information for the API call.
    /// </summary>
    [JsonPropertyName("usage")]
    public OpenAIUsage? Usage { get; init; }
}

/// <summary>
/// Represents a single embedding vector in the API response.
/// </summary>
/// <remarks>
/// <para>
/// Each OpenAIEmbeddingData corresponds to one input text. The index allows
/// matching embeddings to their source texts in batch operations.
/// </para>
/// </remarks>
internal record OpenAIEmbeddingData
{
    /// <summary>
    /// The type of object (typically "embedding").
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    /// <summary>
    /// The embedding vector as an array of floats.
    /// </summary>
    [JsonPropertyName("embedding")]
    public IReadOnlyList<float> Embedding { get; init; } = Array.Empty<float>();

    /// <summary>
    /// The index of this embedding in the input batch.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; init; }
}

/// <summary>
/// Token usage statistics from the OpenAI API response.
/// </summary>
/// <remarks>
/// <para>
/// Provides visibility into token consumption for billing and quota tracking.
/// </para>
/// </remarks>
internal record OpenAIUsage
{
    /// <summary>
    /// The number of tokens in the input text.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    /// <summary>
    /// The total number of tokens processed (input + output, if applicable).
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; init; }
}
