// -----------------------------------------------------------------------
// <copyright file="ContextStrategyFactory.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// Default factory for creating context strategies from the DI container.
/// Provides license-based filtering and strategy registration.
/// </summary>
/// <remarks>
/// <para>
/// This factory:
/// </para>
/// <list type="bullet">
///   <item><description><b>Registers Strategies:</b> Static dictionary maps strategy IDs to types and license tiers</description></item>
///   <item><description><b>Checks Licenses:</b> Filters strategies based on current license tier via <see cref="ILicenseContext"/></description></item>
///   <item><description><b>Creates Instances:</b> Resolves strategies from DI container with full dependency injection</description></item>
/// </list>
/// <para>
/// <strong>Strategy Registration:</strong>
/// The factory maintains a static registration dictionary (<see cref="_registrations"/>)
/// that maps strategy IDs to their implementation types and minimum license tiers.
/// In v0.7.2a, this dictionary is intentionally empty - concrete strategies will be
/// registered in v0.7.2b.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// The factory is thread-safe and can be registered as a singleton in the DI container.
/// The static <see cref="_registrations"/> dictionary is read-only after initialization.
/// The DI container (IServiceProvider) handles concurrent strategy resolution.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registering in DI container (in v0.7.2b when strategies exist)
/// services.AddSingleton&lt;IContextStrategyFactory, ContextStrategyFactory&gt;();
///
/// // Using the factory
/// var factory = serviceProvider.GetRequiredService&lt;IContextStrategyFactory&gt;();
///
/// // Get available strategies for current tier
/// var available = factory.AvailableStrategyIds;
/// Console.WriteLine($"Available: {string.Join(", ", available)}");
///
/// // Create all strategies
/// var strategies = factory.CreateAllStrategies();
/// foreach (var strategy in strategies)
/// {
///     Console.WriteLine($"Loaded: {strategy.DisplayName} (Priority: {strategy.Priority})");
/// }
/// </code>
/// </example>
public sealed class ContextStrategyFactory : IContextStrategyFactory
{
    private readonly IServiceProvider _services;
    private readonly ILicenseContext _license;
    private readonly ILogger<ContextStrategyFactory> _logger;

    // LOGIC: Static registration of built-in strategies.
    // v0.7.2a: Empty dictionary (no strategies implemented yet)
    // v0.7.2b: Will add concrete implementations for these types:
    //   ["document"] = (typeof(DocumentContextStrategy), LicenseTier.WriterPro),
    //   ["selection"] = (typeof(SelectionContextStrategy), LicenseTier.WriterPro),
    //   ["cursor"] = (typeof(CursorContextStrategy), LicenseTier.WriterPro),
    //   ["heading"] = (typeof(HeadingContextStrategy), LicenseTier.WriterPro),
    //   ["rag"] = (typeof(RAGContextStrategy), LicenseTier.Teams),
    //   ["style"] = (typeof(StyleContextStrategy), LicenseTier.Teams),
    //
    // This dictionary maps strategy ID → (Implementation Type, Minimum License Tier)
    private static readonly Dictionary<string, (Type Type, LicenseTier MinTier)> _registrations = new()
    {
        // NOTE: v0.7.2a intentionally has no registered strategies.
        // This allows the interface layer to be tested and deployed
        // independently of concrete strategy implementations.
        // Concrete strategies will be added in v0.7.2b.
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextStrategyFactory"/> class.
    /// </summary>
    /// <param name="services">Service provider for resolving strategy instances.</param>
    /// <param name="license">License context for tier checking.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: All dependencies are required because:
    /// </remarks>
    /// <list type="bullet">
    ///   <item><description><paramref name="services"/> - Required to resolve strategy instances from DI</description></item>
    ///   <item><description><paramref name="license"/> - Required to filter strategies by license tier</description></item>
    ///   <item><description><paramref name="logger"/> - Required for diagnostic logging</description></item>
    /// </list>
    public ContextStrategyFactory(
        IServiceProvider services,
        ILicenseContext license,
        ILogger<ContextStrategyFactory> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _license = license ?? throw new ArgumentNullException(nameof(license));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> AvailableStrategyIds =>
        _registrations
            .Where(kv => IsAvailable(kv.Key, _license.Tier))
            .Select(kv => kv.Key)
            .ToList();

    /// <inheritdoc />
    public IContextStrategy? CreateStrategy(string strategyId)
    {
        // LOGIC: Check if strategy is registered
        if (!_registrations.TryGetValue(strategyId, out var reg))
        {
            _logger.LogWarning("Unknown strategy ID: {StrategyId}", strategyId);
            return null;
        }

        // LOGIC: Check license tier requirement
        if (!IsAvailable(strategyId, _license.Tier))
        {
            _logger.LogDebug(
                "Strategy {StrategyId} not available for tier {Tier} (requires {MinTier})",
                strategyId, _license.Tier, reg.MinTier);
            return null;
        }

        // LOGIC: Resolve strategy from DI container
        // This allows strategies to have their own dependencies injected
        try
        {
            var strategy = (IContextStrategy)_services.GetRequiredService(reg.Type);
            _logger.LogDebug(
                "Created strategy {StrategyId} ({DisplayName})",
                strategyId, strategy.DisplayName);
            return strategy;
        }
        catch (Exception ex)
        {
            // LOGIC: Log resolution errors and return null (graceful degradation)
            _logger.LogError(ex,
                "Failed to create strategy {StrategyId} (type: {Type})",
                strategyId, reg.Type.Name);
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextStrategy> CreateAllStrategies()
    {
        var result = new List<IContextStrategy>();

        foreach (var strategyId in AvailableStrategyIds)
        {
            var strategy = CreateStrategy(strategyId);
            if (strategy is not null)
            {
                result.Add(strategy);
            }
        }

        _logger.LogDebug("Created {Count} strategies", result.Count);
        return result;
    }

    /// <inheritdoc />
    public bool IsAvailable(string strategyId, LicenseTier tier)
    {
        // LOGIC: Strategy must be registered
        if (!_registrations.TryGetValue(strategyId, out var reg))
            return false;

        // LOGIC: Current tier must meet or exceed minimum requirement
        return tier >= reg.MinTier;
    }
}
