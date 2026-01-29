namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for license state changes.
/// </summary>
/// <remarks>
/// LOGIC: Raised when the license is activated, deactivated, or expires.
/// Consumers use this to update UI and feature availability.
/// 
/// Version: v0.1.6c
/// </remarks>
public sealed class LicenseChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous license tier.
    /// </summary>
    public required LicenseTier OldTier { get; init; }

    /// <summary>
    /// Gets the new license tier.
    /// </summary>
    public required LicenseTier NewTier { get; init; }

    /// <summary>
    /// Gets the updated license information.
    /// </summary>
    public required LicenseInfo LicenseInfo { get; init; }
}
