// -----------------------------------------------------------------------
// <copyright file="SummarizerServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Modules.Agents.Summarizer;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Summarizer Agent services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides extension methods for registering all services
/// required by the Summarizer Agent feature:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AddSummarizerAgentPipeline"/> (v0.7.6a) — Summarizer Agent pipeline interface and IAgent forwarding</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6a as part of the Summarizer Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="ISummarizerAgent"/>
/// <seealso cref="SummarizerAgent"/>
public static class SummarizerServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Summarizer Agent Pipeline services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="ISummarizerAgent"/> → <see cref="SummarizerAgent"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the agent is stateless—all state is
    ///     passed via request/result. Injected services (IChatCompletionService, etc.)
    ///     are singletons or thread-safe.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IAgent"/> → forwarded to the same <see cref="SummarizerAgent"/> instance (Singleton)
    ///     <para>
    ///     Enables agent discovery via <see cref="IAgentRegistry"/> while sharing the
    ///     same instance used by <see cref="ISummarizerAgent"/> consumers.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IChatCompletionService"/> (v0.6.1a) — LLM communication</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IPromptRenderer"/> (v0.6.3b) — Template rendering</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IPromptTemplateRepository"/> (v0.6.3c) — Template storage</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.Context.IContextOrchestrator"/> (v0.7.2c) — Context assembly</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IFileService"/> (v0.1.4a) — Document access</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License verification</description></item>
    ///   <item><description><see cref="MediatR.IMediator"/> — Event publishing</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.6a as part of the Summarizer Agent Summarization Modes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddSummarizerAgentPipeline();         // v0.7.6a
    /// }
    ///
    /// // Resolve and use via DI
    /// var agent = serviceProvider.GetRequiredService&lt;ISummarizerAgent&gt;();
    /// var options = agent.ParseCommand("Summarize in 5 bullets");
    /// var result = await agent.SummarizeContentAsync(content, options, ct);
    /// </code>
    /// </example>
    public static IServiceCollection AddSummarizerAgentPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register SummarizerAgent as Singleton implementing ISummarizerAgent.
        // Singleton lifetime is appropriate because:
        // 1. The agent is stateless—all state is passed via request/result
        // 2. Injected services (IChatCompletionService, etc.) are singletons or thread-safe
        // 3. No per-request state is maintained between invocations
        services.AddSingleton<ISummarizerAgent, SummarizerAgent>();

        // LOGIC: Forward IAgent to the same SummarizerAgent instance.
        // This enables the agent to be discovered via IAgentRegistry while sharing
        // the same instance used by ISummarizerAgent consumers.
        services.AddSingleton<IAgent>(sp =>
            (IAgent)sp.GetRequiredService<ISummarizerAgent>());

        return services;
    }
}
