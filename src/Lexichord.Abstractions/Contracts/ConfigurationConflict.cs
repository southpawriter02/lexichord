namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a detected conflict between configuration layers.
/// </summary>
/// <remarks>
/// LOGIC: Conflicts occur when the same setting has different values
/// in different configuration layers. The higher priority layer wins,
/// but conflicts are logged for visibility.
///
/// Hierarchy (highest to lowest priority):
/// - Project (requires Writer Pro)
/// - User
/// - System
///
/// Version: v0.3.6b
/// </remarks>
/// <param name="Key">The configuration key that has conflicting values.</param>
/// <param name="HigherSource">The higher-priority configuration source.</param>
/// <param name="LowerSource">The lower-priority configuration source.</param>
/// <param name="HigherValue">The value from the higher-priority source.</param>
/// <param name="LowerValue">The value from the lower-priority source.</param>
public record ConfigurationConflict(
    string Key,
    ConfigurationSource HigherSource,
    ConfigurationSource LowerSource,
    object? HigherValue,
    object? LowerValue)
{
    /// <summary>
    /// Gets a human-readable description of the conflict.
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides context for debugging and logging purposes.
    /// Format: "Key: 'LowerValue' (LowerSource) overridden by 'HigherValue' (HigherSource)"
    /// </remarks>
    public string Description =>
        $"{Key}: '{LowerValue}' ({LowerSource}) overridden by '{HigherValue}' ({HigherSource})";

    /// <summary>
    /// Gets whether this conflict represents a meaningful difference.
    /// </summary>
    /// <remarks>
    /// LOGIC: A conflict is significant if the higher and lower values
    /// are actually different. Non-significant conflicts occur when
    /// both layers happen to have the same value.
    /// </remarks>
    public bool IsSignificant => !Equals(HigherValue, LowerValue);
}
