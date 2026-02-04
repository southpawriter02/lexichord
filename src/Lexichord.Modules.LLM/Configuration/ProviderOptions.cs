// -----------------------------------------------------------------------
// <copyright file="ProviderOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Configuration;

/// <summary>
/// Configuration options for an individual LLM provider.
/// </summary>
/// <remarks>
/// <para>
/// This class is designed for binding to configuration sections in <c>appsettings.json</c>.
/// Each provider (OpenAI, Anthropic, etc.) has its own configuration section.
/// </para>
/// <para>
/// Example configuration:
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
/// </remarks>
public class ProviderOptions
{
    /// <summary>
    /// Gets or sets the base URL for the provider's API.
    /// </summary>
    /// <value>The API base URL. Defaults to an empty string.</value>
    /// <remarks>
    /// Include the version path if applicable (e.g., "https://api.openai.com/v1").
    /// </remarks>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default model identifier for this provider.
    /// </summary>
    /// <value>The default model ID. Defaults to an empty string.</value>
    /// <remarks>
    /// This model is used when no specific model is requested in <see cref="Abstractions.Contracts.LLM.ChatOptions"/>.
    /// </remarks>
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    /// <value>The retry count. Defaults to 3.</value>
    /// <remarks>
    /// Retries use exponential backoff with jitter. Set to 0 to disable retries.
    /// </remarks>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    /// <value>The timeout in seconds. Defaults to 30.</value>
    /// <remarks>
    /// For streaming requests, this is the timeout for the initial connection.
    /// Individual token delivery has a separate timeout.
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets a value indicating whether this provider is properly configured.
    /// </summary>
    /// <value>True if both BaseUrl and DefaultModel are specified; otherwise, false.</value>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(DefaultModel);
}
