namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Result of license key validation.
/// </summary>
/// <remarks>
/// LOGIC: Encapsulates all validation outcome data including success/failure status,
/// tier information for valid keys, and error details for invalid keys.
/// 
/// Version: v0.1.6c
/// </remarks>
/// <param name="IsValid">Whether the license key is valid.</param>
/// <param name="Tier">The license tier for valid keys.</param>
/// <param name="LicenseeName">The name of the licensee (individual or organization).</param>
/// <param name="ExpirationDate">When the license expires, or null for perpetual licenses.</param>
/// <param name="ErrorMessage">Human-readable error message for invalid keys.</param>
/// <param name="ErrorCode">Programmatic error code for invalid keys.</param>
public record LicenseValidationResult(
    bool IsValid,
    LicenseTier Tier,
    string? LicenseeName = null,
    DateTime? ExpirationDate = null,
    string? ErrorMessage = null,
    LicenseErrorCode ErrorCode = LicenseErrorCode.None
)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="tier">The validated license tier.</param>
    /// <param name="licenseeName">The licensee name.</param>
    /// <param name="expirationDate">Optional expiration date.</param>
    /// <returns>A success result.</returns>
    public static LicenseValidationResult Success(
        LicenseTier tier,
        string licenseeName,
        DateTime? expirationDate = null) =>
        new(true, tier, licenseeName, expirationDate);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">Human-readable error message.</param>
    /// <returns>A failure result.</returns>
    public static LicenseValidationResult Failure(
        LicenseErrorCode errorCode,
        string errorMessage) =>
        new(false, LicenseTier.Core, ErrorMessage: errorMessage, ErrorCode: errorCode);
}
