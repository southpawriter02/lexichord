namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines error codes for license validation failures.
/// </summary>
/// <remarks>
/// LOGIC: These codes enable programmatic error handling and localized error messages.
/// Each code maps to a specific validation failure scenario.
/// 
/// Version: v0.1.6c
/// </remarks>
public enum LicenseErrorCode
{
    /// <summary>No error (validation succeeded).</summary>
    None = 0,

    /// <summary>License key format is invalid (wrong length, characters).</summary>
    InvalidFormat,

    /// <summary>License key checksum validation failed.</summary>
    InvalidSignature,

    /// <summary>License has expired.</summary>
    Expired,

    /// <summary>License has been revoked by the server.</summary>
    Revoked,

    /// <summary>Network error during server validation.</summary>
    NetworkError,

    /// <summary>Server returned an error response.</summary>
    ServerError,

    /// <summary>License is already activated on another device.</summary>
    AlreadyActivated,

    /// <summary>Maximum device activation limit reached.</summary>
    ActivationLimitReached
}
