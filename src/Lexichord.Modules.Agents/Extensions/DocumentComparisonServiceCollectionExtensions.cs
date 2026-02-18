// -----------------------------------------------------------------------
// <copyright file="DocumentComparisonServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: DI extension for registering Document Comparison services (v0.7.6d).
//   Registers IDocumentComparer as singleton (stateless service).
//
//   Registration:
//     - IDocumentComparer -> DocumentComparer (Singleton)
//     - ComparisonViewModel (Transient)
//
//   Introduced in: v0.7.6d
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.DocumentComparison;
using Lexichord.Modules.Agents.DocumentComparison;
using Lexichord.Modules.Agents.DocumentComparison.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Document Comparison services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides a fluent API for registering all Document Comparison
/// related services in the dependency injection container.
/// </para>
/// <para>
/// <b>Lifetime Choices:</b>
/// <list type="bullet">
/// <item><description><see cref="IDocumentComparer"/>: Singleton - stateless service with all state per-invocation</description></item>
/// <item><description><see cref="ComparisonViewModel"/>: Transient - new instance per comparison view</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d</para>
/// </remarks>
public static class DocumentComparisonServiceCollectionExtensions
{
    /// <summary>
    /// Adds Document Comparison services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Registers:
    /// <list type="bullet">
    /// <item><description><see cref="IDocumentComparer"/> as <see cref="DocumentComparer"/> (Singleton)</description></item>
    /// <item><description><see cref="ComparisonViewModel"/> (Transient)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <code>
    /// services.AddDocumentComparisonPipeline();
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDocumentComparisonPipeline(
        this IServiceCollection services)
    {
        // LOGIC: Register the main comparer service as singleton (stateless)
        services.AddSingleton<IDocumentComparer, DocumentComparer>();

        // LOGIC: Register ViewModel as transient (new instance per view)
        services.AddTransient<ComparisonViewModel>();

        return services;
    }
}
