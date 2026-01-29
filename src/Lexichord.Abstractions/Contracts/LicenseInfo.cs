namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Contains information about the current license state.
/// </summary>
/// <remarks>
/// LOGIC: This record provides a snapshot of the current license for UI display.
/// Includes computed properties for common UI scenarios (expiration warning, masked key).
/// 
/// Version: v0.1.6c
/// </remarks>
/// <param name="Tier">The current license tier.</param>
/// <param name="LicenseeName">The licensee name, or null if not activated.</param>
/// <param name="ExpirationDate">When the license expires, or null for perpetual/Core tier.</param>
/// <param name="LicenseKey">The full license key (stored securely), or null if not activated.</param>
/// <param name="IsActivated">Whether a license is currently activated.</param>
public record LicenseInfo(
    LicenseTier Tier,
    string? LicenseeName,
    DateTime? ExpirationDate,
    string? LicenseKey,
    bool IsActivated
)
{
    /// <summary>
    /// Gets whether the license has expired.
    /// </summary>
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;

    /// <summary>
    /// Gets the number of days until expiration, or null if no expiration date.
    /// </summary>
    public int? DaysUntilExpiration => ExpirationDate.HasValue
        ? (int)(ExpirationDate.Value - DateTime.UtcNow).TotalDays
        : null;

    /// <summary>
    /// Gets a masked version of the license key for display (e.g., "****-****-****-ABCD").
    /// </summary>
    public string? MaskedLicenseKey => LicenseKey is not null && LicenseKey.Length >= 4
        ? $"****-****-****-{LicenseKey[^4..]}"
        : null;

    /// <summary>
    /// Gets the default license info for Core (free) tier.
    /// </summary>
    public static LicenseInfo CoreDefault => new(
        LicenseTier.Core,
        null,
        null,
        null,
        false);
}
