// -----------------------------------------------------------------------
// <copyright file="IContextStrategyFactory.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Factory for creating context strategy instances.
/// Allows runtime registration, instantiation, and license-based availability checking.
/// </summary>
/// <remarks>
/// <para>
/// The factory provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Strategy Discovery:</b> List all available strategy IDs for the current license tier</description></item>
///   <item><description><b>Instance Creation:</b> Create strategies via DI container with proper dependency injection</description></item>
///   <item><description><b>License Gating:</b> Check strategy availability for different license tiers</description></item>
/// </list>
/// <para>
/// <strong>License Tier Mapping:</strong>
/// The factory enforces license requirements defined in v0.7.2 specification:
/// </para>
/// <list type="bullet">
///   <item><description><b>Core:</b> No context strategies (baseline tier)</description></item>
///   <item><description><b>WriterPro:</b> document, selection, cursor, heading (editing-focused strategies)</description></item>
///   <item><description><b>Teams:</b> All WriterPro + rag, style (advanced strategies for teams)</description></item>
///   <item><description><b>Enterprise:</b> All strategies + custom strategy registration</description></item>
/// </list>
/// <para>
/// <strong>Design Pattern:</strong>
/// The factory uses a registry pattern with static strategy registration.
/// Concrete strategies are registered at compile-time and instantiated at
/// runtime via the DI container. This provides type safety while allowing
/// runtime filtering based on license tier.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Injecting factory in a service
/// public class ContextOrchestrator
/// {
///     private readonly IContextStrategyFactory _factory;
///
///     public ContextOrchestrator(IContextStrategyFactory factory)
///     {
///         _factory = factory;
///     }
///
///     public async Task&lt;List&lt;ContextFragment&gt;&gt; AssembleContextAsync(
///         ContextGatheringRequest request,
///         ContextBudget budget,
///         CancellationToken ct)
///     {
///         // Get all available strategies for current tier
///         var strategies = _factory.CreateAllStrategies();
///
///         // Execute strategies in parallel
///         var tasks = strategies
///             .Where(s => budget.ShouldExecute(s.StrategyId))
///             .Select(s => s.GatherAsync(request, ct));
///
///         var fragments = await Task.WhenAll(tasks);
///         return fragments.Where(f => f is not null).ToList()!;
///     }
/// }
///
/// // Checking availability for UI
/// var factory = serviceProvider.GetRequiredService&lt;IContextStrategyFactory&gt;();
///
/// if (!factory.IsAvailable("rag", LicenseTier.WriterPro))
/// {
///     ShowUpgradePrompt("RAG search requires Teams tier or higher");
/// }
/// </code>
/// </example>
public interface IContextStrategyFactory
{
    /// <summary>
    /// Gets all registered strategy IDs available for the current license tier.
    /// </summary>
    /// <value>
    /// A read-only list of strategy IDs that can be created with the current license.
    /// </value>
    /// <remarks>
    /// <para>
    /// LOGIC: This list is dynamically filtered based on <see cref="Abstractions.Contracts.ILicenseContext.CurrentTier"/>.
    /// The list may change at runtime if the license tier changes (e.g., upgrade, downgrade, expiration).
    /// </para>
    /// <para>
    /// <strong>Implementation Note:</strong>
    /// The property should query the current license tier each time it's accessed,
    /// not cache the result. This ensures UI elements always reflect the latest
    /// license state without requiring manual refresh.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Displaying available strategies in UI
    /// var available = factory.AvailableStrategyIds;
    /// Console.WriteLine($"Available strategies for {licenseContext.CurrentTier} tier:");
    /// foreach (var id in available)
    /// {
    ///     Console.WriteLine($"  - {id}");
    /// }
    ///
    /// // Example output for WriterPro tier:
    /// // Available strategies for WriterPro tier:
    /// //   - document
    /// //   - selection
    /// //   - cursor
    /// //   - heading
    /// </code>
    /// </example>
    IReadOnlyList<string> AvailableStrategyIds { get; }

    /// <summary>
    /// Creates a strategy instance by ID.
    /// </summary>
    /// <param name="strategyId">Strategy ID to instantiate (e.g., "document", "selection", "rag").</param>
    /// <returns>
    /// A new strategy instance resolved from the DI container,
    /// or <c>null</c> if the strategy is not registered or not available for the current license tier.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Checks if the strategy ID is registered in the static registry</description></item>
    ///   <item><description>Checks if the strategy is available for the current license tier</description></item>
    ///   <item><description>Resolves the strategy type from the DI container (with dependencies)</description></item>
    ///   <item><description>Returns <c>null</c> if any check fails (with appropriate logging)</description></item>
    /// </list>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// Returns <c>null</c> rather than throwing exceptions for missing or unavailable strategies.
    /// This allows callers to handle missing strategies gracefully without try-catch blocks.
    /// Detailed logging is provided at Debug level for troubleshooting.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// The DI container handles concurrent resolution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating a specific strategy
    /// var docStrategy = factory.CreateStrategy("document");
    /// if (docStrategy is not null)
    /// {
    ///     var fragment = await docStrategy.GatherAsync(request, ct);
    ///     // ... process fragment
    /// }
    /// else
    /// {
    ///     _logger.LogWarning("Document strategy not available (tier: {Tier})",
    ///         licenseContext.CurrentTier);
    /// }
    ///
    /// // Handling unavailable strategy
    /// var ragStrategy = factory.CreateStrategy("rag");
    /// if (ragStrategy is null)
    /// {
    ///     // RAG not available - possibly due to license tier or not registered
    ///     // Fall back to other strategies or skip
    /// }
    /// </code>
    /// </example>
    IContextStrategy? CreateStrategy(string strategyId);

    /// <summary>
    /// Creates all available strategy instances for the current license tier.
    /// </summary>
    /// <returns>
    /// A read-only list of all strategies that are registered and available
    /// for the current license tier.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Internally calls <see cref="CreateStrategy"/> for each ID in
    /// <see cref="AvailableStrategyIds"/>. Strategies that fail to create
    /// (e.g., DI resolution errors) are silently skipped with debug logging.
    /// This ensures the method never throws exceptions due to individual
    /// strategy creation failures.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong>
    /// This method creates new instances each time it's called. For scenarios
    /// where strategies are used repeatedly, consider caching the result or
    /// using a singleton scope in the DI container.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Orchestrator initialization - load all strategies once</description></item>
    ///   <item><description>UI display - show all available strategies</description></item>
    ///   <item><description>Configuration validation - verify all expected strategies are present</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Loading all strategies in orchestrator
    /// var strategies = factory.CreateAllStrategies();
    /// _logger.LogInformation("Loaded {Count} context strategies", strategies.Count);
    ///
    /// foreach (var strategy in strategies)
    /// {
    ///     _logger.LogDebug("  - {StrategyId} ({DisplayName}) - Priority: {Priority}",
    ///         strategy.StrategyId, strategy.DisplayName, strategy.Priority);
    /// }
    ///
    /// // Example output:
    /// // Loaded 4 context strategies
    /// //   - document (Document Content) - Priority: 100
    /// //   - selection (Selected Text) - Priority: 80
    /// //   - cursor (Cursor Context) - Priority: 60
    /// //   - heading (Heading Hierarchy) - Priority: 40
    /// </code>
    /// </example>
    IReadOnlyList<IContextStrategy> CreateAllStrategies();

    /// <summary>
    /// Checks if a strategy is available for a specific license tier.
    /// </summary>
    /// <param name="strategyId">Strategy ID to check.</param>
    /// <param name="tier">License tier to check against.</param>
    /// <returns>
    /// <c>true</c> if the strategy is registered and the tier meets the minimum requirement;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method checks both registration and license requirements:
    /// </para>
    /// <list type="number">
    ///   <item><description>Is the strategy ID registered in the factory?</description></item>
    ///   <item><description>Does the specified tier meet the strategy's minimum tier requirement?</description></item>
    /// </list>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// This method is useful for UI elements that need to show what strategies
    /// would be available at different tier levels (e.g., upgrade prompts,
    /// tier comparison tables, feature availability indicators).
    /// </para>
    /// <para>
    /// <strong>Performance:</strong>
    /// This method does not create strategy instances - it only checks the
    /// static registration. It's safe to call frequently from UI code.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Showing upgrade prompt in UI
    /// if (!factory.IsAvailable("rag", currentTier))
    /// {
    ///     if (factory.IsAvailable("rag", LicenseTier.Teams))
    ///     {
    ///         ShowUpgradePrompt(
    ///             "RAG search requires Teams tier or higher. " +
    ///             "Upgrade to unlock semantic search capabilities.");
    ///     }
    /// }
    ///
    /// // Building a tier comparison table
    /// var strategies = new[] { "document", "selection", "rag", "style" };
    /// var tiers = new[] { LicenseTier.Core, LicenseTier.WriterPro, LicenseTier.Teams };
    ///
    /// foreach (var tier in tiers)
    /// {
    ///     Console.WriteLine($"\n{tier} tier strategies:");
    ///     foreach (var strategyId in strategies)
    ///     {
    ///         var available = factory.IsAvailable(strategyId, tier);
    ///         Console.WriteLine($"  {strategyId}: {(available ? "✓" : "✗")}");
    ///     }
    /// }
    /// </code>
    /// </example>
    bool IsAvailable(string strategyId, LicenseTier tier);
}
