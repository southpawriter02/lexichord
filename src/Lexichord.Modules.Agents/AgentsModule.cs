// -----------------------------------------------------------------------
// <copyright file="AgentsModule.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Extensions;
using Lexichord.Modules.Agents.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents;

/// <summary>
/// The Agents module provides AI agent orchestration and prompt templating capabilities.
/// </summary>
/// <remarks>
/// <para>
/// This module serves as the foundation for AI agent interactions in Lexichord,
/// providing the following capabilities:
/// </para>
/// <list type="bullet">
///   <item><description><strong>v0.6.3b:</strong> Mustache-based prompt template rendering via <see cref="MustachePromptRenderer"/></description></item>
///   <item><description><strong>v0.6.3c:</strong> Template repository for built-in and custom prompts (future)</description></item>
///   <item><description><strong>v0.6.3d:</strong> Context injection from style rules and RAG (future)</description></item>
/// </list>
/// <para>
/// The module is part of the v0.6.x "Conductors" release series, which transforms
/// Lexichord from a smart search tool into an intelligent writing assistant with
/// LLM-powered features.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // The module is automatically loaded by the ModuleLoader from the Modules directory.
/// // Services can then be resolved via DI:
///
/// var renderer = serviceProvider.GetRequiredService&lt;IPromptRenderer&gt;();
///
/// var template = PromptTemplate.Create(
///     templateId: "assistant",
///     name: "Assistant",
///     systemPrompt: "You are a helpful writing assistant.",
///     userPrompt: "{{user_input}}",
///     requiredVariables: ["user_input"]
/// );
///
/// var messages = renderer.RenderMessages(template, new Dictionary&lt;string, object&gt;
/// {
///     ["user_input"] = "Help me improve this paragraph."
/// });
/// </code>
/// </example>
/// <seealso cref="IModule"/>
/// <seealso cref="MustachePromptRenderer"/>
/// <seealso cref="IPromptRenderer"/>
public class AgentsModule : IModule
{
    /// <inheritdoc />
    /// <remarks>
    /// Module metadata for the Agents module:
    /// <list type="bullet">
    ///   <item><description><strong>Id:</strong> "agents"</description></item>
    ///   <item><description><strong>Name:</strong> "Agents"</description></item>
    ///   <item><description><strong>Version:</strong> 0.6.3 (The Template Engine)</description></item>
    ///   <item><description><strong>Author:</strong> Lexichord Team</description></item>
    /// </list>
    /// </remarks>
    public ModuleInfo Info => new(
        Id: "agents",
        Name: "Agents",
        Version: new Version(0, 6, 3),
        Author: "Lexichord Team",
        Description: "AI agent orchestration with Mustache-based prompt templating");

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="IPromptRenderer"/> → <see cref="MustachePromptRenderer"/> (Singleton)
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="MustacheRendererOptions"/> via IOptions pattern
    ///   </description></item>
    /// </list>
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Register Mustache prompt renderer services (v0.6.3b).
        // The renderer is registered as a singleton for thread-safe reuse.
        services.AddMustacheRenderer();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Performs post-DI-build initialization:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Verifies the prompt renderer is available</description></item>
    ///   <item><description>Logs module initialization status</description></item>
    /// </list>
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<AgentsModule>>();

        logger.LogInformation(
            "Initializing {ModuleName} module v{Version}",
            Info.Name,
            Info.Version);

        // LOGIC: Verify the prompt renderer is available.
        var renderer = provider.GetService<IPromptRenderer>();
        if (renderer is not null)
        {
            logger.LogDebug(
                "Prompt renderer registered successfully: {RendererType}",
                renderer.GetType().Name);
        }
        else
        {
            logger.LogWarning("Prompt renderer is not registered. Template rendering will not be available.");
        }

        logger.LogInformation(
            "{ModuleName} module initialized successfully",
            Info.Name);

        await Task.CompletedTask;
    }
}
