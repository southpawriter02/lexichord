// -----------------------------------------------------------------------
// <copyright file="OpenAIOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Providers.OpenAI;

/// <summary>
/// Configuration options for the OpenAI chat completion provider.
/// </summary>
/// <remarks>
/// <para>
/// This record defines the configuration settings required for connecting to the
/// OpenAI Chat Completions API. Settings can be bound from configuration sources
/// (e.g., appsettings.json) using the <c>LLM:Providers:OpenAI</c> section path.
/// </para>
/// <para>
/// <b>Configuration Example (appsettings.json):</b>
/// </para>
/// <code>
/// {
///   "LLM": {
///     "Providers": {
///       "OpenAI": {
///         "BaseUrl": "https://api.openai.com/v1",
///         "DefaultModel": "gpt-4o-mini",
///         "MaxRetries": 3,
///         "TimeoutSeconds": 30
///       }
///     }
///   }
/// }
/// </code>
/// <para>
/// <b>API Key Storage:</b> The API key is not stored in configuration for security.
/// Instead, it is retrieved from <see cref="Lexichord.Abstractions.Contracts.Security.ISecureVault"/>
/// using the key pattern defined in <see cref="VaultKey"/>.
/// </para>
/// </remarks>
/// <param name="BaseUrl">
/// The base URL for the OpenAI API. Defaults to the standard OpenAI endpoint.
/// This can be overridden to use a proxy or compatible API endpoint.
/// </param>
/// <param name="DefaultModel">
/// The default model to use when not specified in the request.
/// Defaults to "gpt-4o-mini" for cost-effectiveness with good performance.
/// </param>
/// <param name="MaxRetries">
/// Maximum number of retry attempts for transient failures.
/// Retries use exponential backoff with jitter.
/// </param>
/// <param name="TimeoutSeconds">
/// Request timeout in seconds. Should be long enough to accommodate
/// lengthy completions, especially for large context windows.
/// </param>
/// <example>
/// <code>
/// // Binding configuration in DI registration
/// services.Configure&lt;OpenAIOptions&gt;(
///     configuration.GetSection("LLM:Providers:OpenAI"));
///
/// // Using options in a service
/// public class MyService
/// {
///     private readonly OpenAIOptions _options;
///
///     public MyService(IOptions&lt;OpenAIOptions&gt; options)
///     {
///         _options = options.Value;
///     }
///
///     public void Configure()
///     {
///         var endpoint = _options.CompletionsEndpoint;
///         // endpoint = "https://api.openai.com/v1/chat/completions"
///     }
/// }
/// </code>
/// </example>
public record OpenAIOptions(
    string BaseUrl = "https://api.openai.com/v1",
    string DefaultModel = "gpt-4o-mini",
    int MaxRetries = 3,
    int TimeoutSeconds = 30)
{
    /// <summary>
    /// The vault key used to retrieve the OpenAI API key from secure storage.
    /// </summary>
    /// <value>The constant string "openai:api-key".</value>
    /// <remarks>
    /// <para>
    /// The API key is stored in <see cref="Lexichord.Abstractions.Contracts.Security.ISecureVault"/>
    /// using this key pattern. This follows the Lexichord convention of
    /// <c>{provider}:api-key</c> for all LLM provider API keys.
    /// </para>
    /// </remarks>
    public const string VaultKey = "openai:api-key";

    /// <summary>
    /// Gets the full URL for the Chat Completions API endpoint.
    /// </summary>
    /// <value>The base URL combined with the chat/completions path.</value>
    /// <remarks>
    /// <para>
    /// This computed property constructs the full endpoint URL by appending
    /// <c>/chat/completions</c> to the <see cref="BaseUrl"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new OpenAIOptions(BaseUrl: "https://api.openai.com/v1");
    /// var endpoint = options.CompletionsEndpoint;
    /// // endpoint = "https://api.openai.com/v1/chat/completions"
    /// </code>
    /// </example>
    public string CompletionsEndpoint => $"{BaseUrl}/chat/completions";

    /// <summary>
    /// The name of the named HTTP client registered for OpenAI requests.
    /// </summary>
    /// <value>The constant string "OpenAI".</value>
    /// <remarks>
    /// <para>
    /// This constant is used when registering and resolving the HTTP client
    /// via <see cref="System.Net.Http.IHttpClientFactory"/>. The named client
    /// is configured with appropriate timeout and resilience policies.
    /// </para>
    /// </remarks>
    public const string HttpClientName = "OpenAI";

    /// <summary>
    /// The list of supported OpenAI models.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This list includes the primary models available through the OpenAI API.
    /// It is used for provider registration and model validation.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> SupportedModels { get; } = new[]
    {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "gpt-3.5-turbo"
    };
}
