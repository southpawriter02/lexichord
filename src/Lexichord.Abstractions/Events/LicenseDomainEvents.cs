using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a license is successfully activated.
/// </summary>
/// <remarks>
/// LOGIC: Allows other parts of the application to react to license activation,
/// such as enabling features or updating UI elements.
/// 
/// Version: v0.1.6c
/// </remarks>
/// <param name="Tier">The activated license tier.</param>
/// <param name="LicenseeName">The name of the licensee.</param>
public record LicenseActivatedEvent(
    Contracts.LicenseTier Tier,
    string LicenseeName
) : INotification;

/// <summary>
/// Published when a license is deactivated.
/// </summary>
/// <remarks>
/// LOGIC: Allows other parts of the application to react to license deactivation,
/// such as disabling features or showing upgrade prompts.
/// 
/// Version: v0.1.6c
/// </remarks>
/// <param name="PreviousTier">The tier before deactivation.</param>
public record LicenseDeactivatedEvent(
    Contracts.LicenseTier PreviousTier
) : INotification;

/// <summary>
/// Published when license validation fails.
/// </summary>
/// <remarks>
/// LOGIC: Enables logging, analytics, and fraud detection for failed validation attempts.
/// 
/// Version: v0.1.6c
/// </remarks>
/// <param name="ErrorCode">The error code indicating why validation failed.</param>
/// <param name="ErrorMessage">Human-readable error message.</param>
public record LicenseValidationFailedEvent(
    Contracts.LicenseErrorCode ErrorCode,
    string ErrorMessage
) : INotification;
