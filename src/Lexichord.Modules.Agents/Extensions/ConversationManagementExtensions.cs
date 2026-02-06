// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Chat.Contracts;
using Lexichord.Modules.Agents.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering conversation management services.
/// </summary>
internal static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers conversation management services (v0.6.4c).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="IConversationManager"/> as a scoped service</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddConversationManagement(this IServiceCollection services)
    {
        // LOGIC: Register as scoped to maintain conversation state per user session.
        services.AddScoped<IConversationManager, ConversationManager>();

        return services;
    }
}
