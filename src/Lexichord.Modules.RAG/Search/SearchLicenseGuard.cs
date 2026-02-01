// =============================================================================
// File: SearchLicenseGuard.cs
// Project: Lexichord.Modules.RAG
// Description: License validation helper for semantic search operations.
// =============================================================================
// LOGIC: Centralizes license tier checking for semantic search features.
//   - EnsureSearchAuthorized() throws FeatureNotLicensedException if unlicensed.
//   - TryAuthorizeSearchAsync() returns bool and publishes SearchDeniedEvent.
//   - IsSearchAvailable provides quick property check without exceptions.
//   - Required tier: WriterPro or higher.
//
//   This guard is injected into PgVectorSearchService to enforce license gating
//   before any search operations execute. The guard approach avoids duplicating
//   license checks across multiple search-related services.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Guard class for license-gated semantic search operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SearchLicenseGuard"/> provides centralized license tier validation
/// for all semantic search features. It checks the current user's license tier
/// against the <see cref="LicenseTier.WriterPro"/> requirement and either throws
/// an exception or publishes a denial event.
/// </para>
/// <para>
/// <b>Usage Patterns:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="EnsureSearchAuthorized"/>: Synchronous, throws
///     <see cref="FeatureNotLicensedException"/> if not authorized.
///     Used in service methods that should fail fast on license denial.
///   </description></item>
///   <item><description>
///     <see cref="TryAuthorizeSearchAsync"/>: Asynchronous, publishes
///     <see cref="SearchDeniedEvent"/> and returns <c>false</c> if not authorized.
///     Used in UI code that needs to show upgrade prompts without exceptions.
///   </description></item>
///   <item><description>
///     <see cref="IsSearchAvailable"/>: Property check without side effects.
///     Used for UI element visibility (e.g., disabling search input).
///   </description></item>
/// </list>
/// <para>
/// <b>License Tiers:</b>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Tier</term>
///     <description>Access</description>
///   </listheader>
///   <item><term>Core</term><description>Blocked (upgrade prompt)</description></item>
///   <item><term>WriterPro</term><description>Full access</description></item>
///   <item><term>Teams</term><description>Full access</description></item>
///   <item><term>Enterprise</term><description>Full access</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe and suitable for singleton lifetime.
/// License tier is read from <see cref="ILicenseContext"/> on each check.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5b.
/// </para>
/// </remarks>
public sealed class SearchLicenseGuard
{
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<SearchLicenseGuard> _logger;

    /// <summary>
    /// The feature name used in exception messages and denial events.
    /// </summary>
    private const string FeatureName = "Semantic Search";

    /// <summary>
    /// The minimum license tier required for semantic search.
    /// </summary>
    private static readonly LicenseTier RequiredTier = LicenseTier.WriterPro;

    /// <summary>
    /// Creates a new <see cref="SearchLicenseGuard"/> instance.
    /// </summary>
    /// <param name="licenseContext">The license context for tier checks.</param>
    /// <param name="mediator">The MediatR mediator for publishing denial events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public SearchLicenseGuard(
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<SearchLicenseGuard> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the user's current license tier.
    /// </summary>
    /// <value>The active <see cref="LicenseTier"/> from <see cref="ILicenseContext"/>.</value>
    public LicenseTier CurrentTier => _licenseContext.GetCurrentTier();

    /// <summary>
    /// Gets whether semantic search is available for the current license.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current tier is <see cref="LicenseTier.WriterPro"/> or higher;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Quick property check without side effects.
    /// Suitable for UI element visibility binding.
    /// </remarks>
    public bool IsSearchAvailable => _licenseContext.GetCurrentTier() >= RequiredTier;

    /// <summary>
    /// Validates that the current license tier authorizes semantic search.
    /// Throws if not authorized.
    /// </summary>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the current license tier is below <see cref="LicenseTier.WriterPro"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Synchronous validation intended for use at the start of search operations.
    /// The exception includes the required tier for UI upgrade prompts.
    /// </para>
    /// </remarks>
    public void EnsureSearchAuthorized()
    {
        var currentTier = _licenseContext.GetCurrentTier();

        if (currentTier < RequiredTier)
        {
            _logger.LogDebug(
                "Search blocked: current tier {CurrentTier}, required {RequiredTier}",
                currentTier, RequiredTier);

            throw new FeatureNotLicensedException(
                $"{FeatureName} requires {RequiredTier} license or higher. Current tier: {currentTier}.",
                RequiredTier);
        }
    }

    /// <summary>
    /// Attempts to authorize semantic search, publishing a denial event on failure.
    /// </summary>
    /// <param name="ct">Cancellation token for the event publishing operation.</param>
    /// <returns>
    /// <c>true</c> if the current license tier authorizes semantic search;
    /// <c>false</c> if the search is denied and a <see cref="SearchDeniedEvent"/>
    /// has been published.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Non-throwing alternative to <see cref="EnsureSearchAuthorized"/>.
    /// Used by UI code that needs to show upgrade prompts without exception handling.
    /// The <see cref="SearchDeniedEvent"/> enables handlers to:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Display an "Upgrade to WriterPro" modal.</description></item>
    ///   <item><description>Log the denial for telemetry.</description></item>
    ///   <item><description>Track upgrade conversion opportunities.</description></item>
    /// </list>
    /// </remarks>
    public async Task<bool> TryAuthorizeSearchAsync(CancellationToken ct = default)
    {
        if (IsSearchAvailable)
            return true;

        var currentTier = _licenseContext.GetCurrentTier();

        _logger.LogDebug(
            "Search denied: current tier {CurrentTier}, required {RequiredTier}. Publishing denial event.",
            currentTier, RequiredTier);

        await _mediator.Publish(new SearchDeniedEvent
        {
            CurrentTier = currentTier,
            RequiredTier = RequiredTier,
            FeatureName = FeatureName,
            Timestamp = DateTimeOffset.UtcNow
        }, ct);

        return false;
    }
}
