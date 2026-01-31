using Lexichord.Abstractions.Events;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides merged configuration from system, user, and project sources.
/// </summary>
/// <remarks>
/// LOGIC: This interface defines the contract for hierarchical configuration loading.
/// Configuration is merged from three sources in priority order:
///
/// 1. System (lowest): Embedded application defaults
/// 2. User: User-specific settings from profile/appsettings
/// 3. Project (highest): Workspace-specific settings from .lexichord/style.yaml
///
/// Merge Strategy:
/// - Higher sources override lower sources for scalar values
/// - Lists are concatenated and deduplicated
/// - Null values in higher sources don't override non-null lower values
///
/// License Gating:
/// - Project configuration requires Writer Pro license
/// - Core users only see merged System + User configuration
///
/// Caching:
/// - Results are cached for performance (5 second TTL)
/// - Use InvalidateCache() when underlying files change
///
/// Thread Safety:
/// - Implementations must be thread-safe
/// - GetEffectiveConfiguration() may be called concurrently
///
/// Version: v0.3.6a
/// </remarks>
/// <example>
/// <code>
/// public class MyService(ILayeredConfigurationProvider configProvider)
/// {
///     public void DoWork()
///     {
///         var config = configProvider.GetEffectiveConfiguration();
///         
///         if (config.PassiveVoiceThreshold &lt; 15)
///         {
///             // Strict passive voice checking
///         }
///     }
/// }
/// </code>
/// </example>
public interface ILayeredConfigurationProvider
{
    /// <summary>
    /// Gets the fully merged effective configuration.
    /// </summary>
    /// <returns>
    /// The merged configuration from all applicable sources.
    /// </returns>
    /// <remarks>
    /// LOGIC: Merges System → User → Project (if licensed and available).
    /// Results are cached for 5 seconds to avoid repeated file I/O.
    /// Never returns null; returns StyleConfiguration.Defaults at minimum.
    /// </remarks>
    StyleConfiguration GetEffectiveConfiguration();

    /// <summary>
    /// Gets configuration from a specific source only.
    /// </summary>
    /// <param name="source">The configuration source to load.</param>
    /// <returns>
    /// Configuration from the specified source, or null if not available.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns the raw configuration from a single source without merging.
    /// Useful for debugging or displaying "source of truth" for a setting.
    /// Returns null if:
    /// - Source is Project but no workspace is open
    /// - Source is Project but user lacks Writer Pro license
    /// - Configuration file doesn't exist or is invalid
    /// </remarks>
    StyleConfiguration? GetConfiguration(ConfigurationSource source);

    /// <summary>
    /// Invalidates the configuration cache.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears the internal cache and raises ConfigurationChanged event.
    /// Call this when:
    /// - A configuration file is modified (file watcher notification)
    /// - Workspace is opened or closed
    /// - License tier changes
    ///
    /// Next call to GetEffectiveConfiguration() will reload from disk.
    /// </remarks>
    void InvalidateCache();

    /// <summary>
    /// Occurs when the effective configuration changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after cache invalidation or when configuration is reloaded.
    /// Subscribers should re-evaluate configuration-dependent state.
    /// Common subscribers: LintingOrchestrator, VoiceProfileService.
    /// </remarks>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}
