// -----------------------------------------------------------------------
// <copyright file="ProviderNotFoundException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Exception thrown when a requested LLM provider is not registered in the provider registry.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates that the requested provider name does not match any
/// registered provider in the <see cref="ILLMProviderRegistry"/>. This is different
/// from <see cref="ProviderNotConfiguredException"/>, which indicates that a provider
/// is registered but lacks the necessary API key configuration.
/// </para>
/// <para>
/// Common causes include:
/// </para>
/// <list type="bullet">
///   <item><description>Typo in the provider name</description></item>
///   <item><description>Provider module not loaded or registered</description></item>
///   <item><description>Attempting to use a provider before initialization</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var provider = registry.GetProvider("unknown-provider");
/// }
/// catch (ProviderNotFoundException ex)
/// {
///     Console.WriteLine($"Provider '{ex.ProviderName}' is not registered.");
///     Console.WriteLine($"Available providers: {string.Join(", ", registry.AvailableProviders.Select(p => p.Name))}");
/// }
/// </code>
/// </example>
public class ProviderNotFoundException : Exception
{
    /// <summary>
    /// The default message format used when only a provider name is specified.
    /// </summary>
    private const string DefaultMessageFormat = "LLM provider '{0}' is not registered. Ensure the provider is registered via AddChatCompletionProvider.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotFoundException"/> class.
    /// </summary>
    public ProviderNotFoundException()
        : base("The requested LLM provider is not registered.")
    {
        ProviderName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotFoundException"/> class
    /// with the name of the missing provider.
    /// </summary>
    /// <param name="providerName">The name of the provider that was not found.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providerName"/> is null.</exception>
    public ProviderNotFoundException(string providerName)
        : base(string.Format(DefaultMessageFormat, providerName ?? throw new ArgumentNullException(nameof(providerName))))
    {
        ProviderName = providerName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotFoundException"/> class
    /// with the name of the missing provider and a custom error message.
    /// </summary>
    /// <param name="providerName">The name of the provider that was not found.</param>
    /// <param name="message">The custom error message.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providerName"/> is null.</exception>
    public ProviderNotFoundException(string providerName, string message)
        : base(message)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotFoundException"/> class
    /// with a custom error message and inner exception.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public ProviderNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderNotFoundException"/> class
    /// with the name of the missing provider, a custom error message, and inner exception.
    /// </summary>
    /// <param name="providerName">The name of the provider that was not found.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providerName"/> is null.</exception>
    public ProviderNotFoundException(string providerName, string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
    }

    /// <summary>
    /// Gets the name of the provider that was not found.
    /// </summary>
    /// <value>
    /// The provider name that was requested but not found in the registry,
    /// or an empty string if not specified.
    /// </value>
    public string ProviderName { get; }

    /// <summary>
    /// Gets a value indicating whether this exception has a specific provider name.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ProviderName"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasProviderName => !string.IsNullOrEmpty(ProviderName);
}
