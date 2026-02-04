// -----------------------------------------------------------------------
// <copyright file="AnthropicOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Providers.Anthropic;

/// <summary>
/// Configuration options for the Anthropic chat completion provider.
/// </summary>
/// <remarks>
/// <para>
/// This record defines the configuration settings required for connecting to the
/// Anthropic Messages API. Settings can be bound from configuration sources
/// (e.g., appsettings.json) using the <c>LLM:Providers:Anthropic</c> section path.
/// </para>
/// <para>
/// <b>Configuration Example (appsettings.json):</b>
/// </para>
/// <code>
/// {
///   "LLM": {
///     "Providers": {
///       "Anthropic": {
///         "BaseUrl": "https://api.anthropic.com/v1",
///         "DefaultModel": "claude-3-haiku-20240307",
///         "ApiVersion": "2024-01-01",
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
/// <para>
/// <b>Anthropic API Differences from OpenAI:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Authentication uses <c>x-api-key</c> header (not Bearer token)</description></item>
///   <item><description>Requires <c>anthropic-version</c> header</description></item>
///   <item><description>System messages sent via separate "system" field</description></item>
///   <item><description>Response content is array of content blocks</description></item>
/// </list>
/// </remarks>
/// <param name="BaseUrl">
/// The base URL for the Anthropic API. Defaults to the standard Anthropic endpoint.
/// This can be overridden to use a proxy or compatible API endpoint.
/// </param>
/// <param name="DefaultModel">
/// The default model to use when not specified in the request.
/// Defaults to "claude-3-haiku-20240307" for cost-effectiveness with good performance.
/// </param>
/// <param name="ApiVersion">
/// The Anthropic API version to use. This is sent via the <c>anthropic-version</c> header.
/// Defaults to "2024-01-01".
/// </param>
/// <param name="MaxRetries">
/// Maximum number of retry attempts for transient failures.
/// Retries use exponential backoff with jitter.
/// </param>
/// <param name="TimeoutSeconds">
/// Request timeout in seconds. Should be long enough to accommodate
/// lengthy completions, especially for large context windows (200K tokens).
/// </param>
/// <example>
/// <code>
/// // Binding configuration in DI registration
/// services.Configure&lt;AnthropicOptions&gt;(
///     configuration.GetSection("LLM:Providers:Anthropic"));
///
/// // Using options in a service
/// public class MyService
/// {
///     private readonly AnthropicOptions _options;
///
///     public MyService(IOptions&lt;AnthropicOptions&gt; options)
///     {
///         _options = options.Value;
///     }
///
///     public void Configure()
///     {
///         var endpoint = _options.MessagesEndpoint;
///         // endpoint = "https://api.anthropic.com/v1/messages"
///     }
/// }
/// </code>
/// </example>
public record AnthropicOptions(
    string BaseUrl = "https://api.anthropic.com/v1",
    string DefaultModel = "claude-3-haiku-20240307",
    string ApiVersion = "2024-01-01",
    int MaxRetries = 3,
    int TimeoutSeconds = 30)
{
    /// <summary>
    /// The vault key used to retrieve the Anthropic API key from secure storage.
    /// </summary>
    /// <value>The constant string "anthropic:api-key".</value>
    /// <remarks>
    /// <para>
    /// The API key is stored in <see cref="Lexichord.Abstractions.Contracts.Security.ISecureVault"/>
    /// using this key pattern. This follows the Lexichord convention of
    /// <c>{provider}:api-key</c> for all LLM provider API keys.
    /// </para>
    /// </remarks>
    public const string VaultKey = "anthropic:api-key";

    /// <summary>
    /// Gets the full URL for the Messages API endpoint.
    /// </summary>
    /// <value>The base URL combined with the messages path.</value>
    /// <remarks>
    /// <para>
    /// This computed property constructs the full endpoint URL by appending
    /// <c>/messages</c> to the <see cref="BaseUrl"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new AnthropicOptions(BaseUrl: "https://api.anthropic.com/v1");
    /// var endpoint = options.MessagesEndpoint;
    /// // endpoint = "https://api.anthropic.com/v1/messages"
    /// </code>
    /// </example>
    public string MessagesEndpoint => $"{BaseUrl}/messages";

    /// <summary>
    /// The name of the named HTTP client registered for Anthropic requests.
    /// </summary>
    /// <value>The constant string "Anthropic".</value>
    /// <remarks>
    /// <para>
    /// This constant is used when registering and resolving the HTTP client
    /// via <see cref="System.Net.Http.IHttpClientFactory"/>. The named client
    /// is configured with appropriate timeout and resilience policies.
    /// </para>
    /// </remarks>
    public const string HttpClientName = "Anthropic";

    /// <summary>
    /// The list of supported Anthropic Claude models.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This list includes the primary Claude models available through the Anthropic API.
    /// It is used for provider registration and model validation.
    /// </para>
    /// <para>
    /// <b>Model Capabilities:</b>
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Model</term>
    ///     <description>Context Window</description>
    ///   </listheader>
    ///   <item>
    ///     <term>claude-3-5-sonnet-20241022</term>
    ///     <description>200,000 tokens</description>
    ///   </item>
    ///   <item>
    ///     <term>claude-3-opus-20240229</term>
    ///     <description>200,000 tokens</description>
    ///   </item>
    ///   <item>
    ///     <term>claude-3-sonnet-20240229</term>
    ///     <description>200,000 tokens</description>
    ///   </item>
    ///   <item>
    ///     <term>claude-3-haiku-20240307</term>
    ///     <description>200,000 tokens</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public static IReadOnlyList<string> SupportedModels { get; } = new[]
    {
        "claude-3-5-sonnet-20241022",
        "claude-3-opus-20240229",
        "claude-3-sonnet-20240229",
        "claude-3-haiku-20240307"
    };
}
