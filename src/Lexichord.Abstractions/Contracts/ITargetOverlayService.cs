// <copyright file="ITargetOverlayService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Computes target overlay data from voice profiles for the Resonance Dashboard.
/// </summary>
/// <remarks>
/// <para>LOGIC: Converts voice profile targets into chart-ready data points.</para>
/// <para>Caches computed overlays per profile to avoid redundant computation.</para>
/// <para>Introduced in v0.3.5b.</para>
/// </remarks>
public interface ITargetOverlayService
{
    /// <summary>
    /// Gets the target overlay data for a voice profile.
    /// </summary>
    /// <param name="profile">The voice profile containing target constraints.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Target overlay with normalized data points matching the profile's constraints,
    /// or null if the profile has no defined targets.
    /// </returns>
    /// <remarks>
    /// LOGIC: Computes target values from profile's:
    /// - ReadabilityTarget → Flesch Reading Ease axis
    /// - PassiveVoiceMaxPercent → Clarity axis (inverted)
    /// - WeakWordMaxDensity → Precision axis (inverted)
    /// - MaxSentenceLength → Density axis
    /// Results are cached per profile ID for performance.
    /// </remarks>
    Task<TargetOverlay?> GetOverlayAsync(VoiceProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached overlay for a specific profile.
    /// </summary>
    /// <param name="profileId">The profile ID to invalidate.</param>
    /// <remarks>
    /// LOGIC: Call when profile targets are modified to force recomputation.
    /// </remarks>
    void InvalidateCache(string profileId);

    /// <summary>
    /// Synchronous version for UI binding scenarios.
    /// Uses cached data if available, otherwise computes and caches.
    /// </summary>
    /// <param name="profile">The profile to get overlay for.</param>
    /// <returns>Cached or computed target overlay, or null if profile has no targets.</returns>
    /// <remarks>
    /// LOGIC: v0.3.5d - Provides synchronous access for immediate UI updates.
    /// </remarks>
    TargetOverlay? GetOverlaySync(VoiceProfile profile);

    /// <summary>
    /// Invalidates all cached overlays.
    /// </summary>
    /// <remarks>
    /// LOGIC: Call when axis definitions change or on application settings reset.
    /// </remarks>
    void InvalidateAllCaches();
}
