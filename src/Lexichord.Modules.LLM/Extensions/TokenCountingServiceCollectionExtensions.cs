// -----------------------------------------------------------------------
// <copyright file="TokenCountingServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Extension method for registering token counting services.
//   - Registers TokenizerCache as singleton for efficient tokenizer reuse.
//   - Registers TokenizerFactory as singleton for model-specific tokenization.
//   - Registers LLMTokenCounter as singleton implementing ILLMTokenCounter.
//   - All components are thread-safe and designed for concurrent access.
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.TokenCounting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexichord.Modules.LLM.Extensions;

/// <summary>
/// Extension methods for registering token counting services with the DI container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register the token counting service and its
/// dependencies for LLM operations.
/// </para>
/// <para>
/// <b>Services Registered:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="TokenizerCache"/> - Thread-safe cache for tokenizer instances.</description></item>
///   <item><description><see cref="TokenizerFactory"/> - Creates model-specific tokenizers.</description></item>
///   <item><description><see cref="ILLMTokenCounter"/> - Public interface for token counting.</description></item>
/// </list>
/// <para>
/// All services are registered as singletons since they are thread-safe and
/// benefit from instance reuse across the application lifetime.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs or module registration
/// services.AddTokenCounting();
///
/// // Usage via DI
/// public class MyService
/// {
///     private readonly ILLMTokenCounter _tokenCounter;
///
///     public MyService(ILLMTokenCounter tokenCounter)
///     {
///         _tokenCounter = tokenCounter;
///     }
///
///     public void ProcessText(string text, string model)
///     {
///         var tokens = _tokenCounter.CountTokens(text, model);
///         var cost = _tokenCounter.CalculateCost(model, tokens, estimatedOutput: 500);
///         Console.WriteLine($"Tokens: {tokens}, Estimated cost: ${cost:F4}");
///     }
/// }
/// </code>
/// </example>
public static class TokenCountingServiceCollectionExtensions
{
    /// <summary>
    /// Adds token counting services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="TokenizerCache"/> as singleton - Caches tokenizer instances per model family.</description></item>
    ///   <item><description><see cref="TokenizerFactory"/> as singleton - Creates model-specific tokenizers.</description></item>
    ///   <item><description><see cref="LLMTokenCounter"/> as singleton implementing <see cref="ILLMTokenCounter"/> - Main token counting service.</description></item>
    /// </list>
    /// <para>
    /// <b>Idempotent:</b> This method uses <c>TryAddSingleton</c> to prevent
    /// duplicate registrations if called multiple times.
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> All registered services are thread-safe and
    /// designed for concurrent access across multiple requests.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Typical usage in LLM module registration
    /// public static IServiceCollection AddLLMModule(this IServiceCollection services)
    /// {
    ///     services.AddTokenCounting();
    ///     // ... other registrations
    ///     return services;
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddTokenCounting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        // LOGIC: Register TokenizerCache as singleton for efficient tokenizer reuse.
        // The cache uses ConcurrentDictionary internally for thread safety.
        services.TryAddSingleton<TokenizerCache>();

        // LOGIC: Register TokenizerFactory as singleton.
        // The factory uses Lazy<T> for thread-safe tokenizer initialization.
        services.TryAddSingleton<TokenizerFactory>();

        // LOGIC: Register LLMTokenCounter as the ILLMTokenCounter implementation.
        // This is the main public service for token counting operations.
        services.TryAddSingleton<ILLMTokenCounter, LLMTokenCounter>();

        return services;
    }
}
