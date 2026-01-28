using Dapper.Contrib.Extensions;

namespace Lexichord.Abstractions.Entities;

/// <summary>
/// System setting entity for key-value configuration storage.
/// </summary>
/// <remarks>
/// LOGIC: SystemSettings provides a flexible key-value store for application configuration
/// that may change at runtime without requiring a restart.
///
/// Common use cases:
/// - Feature flags
/// - Application version tracking
/// - Maintenance mode status
/// - Dynamic configuration
///
/// Mapping:
/// - Table: "SystemSettings"
/// - Primary Key: Key (string, explicit/natural key)
/// </remarks>
[Table("SystemSettings")]
public record SystemSetting
{
    /// <summary>
    /// Setting key (primary key).
    /// </summary>
    /// <remarks>
    /// [ExplicitKey] indicates this is the PK (natural key, not auto-generated).
    /// Keys should follow the pattern: "namespace:setting_name"
    /// Examples: "app:version", "system:maintenance_mode", "feature:dark_mode"
    /// </remarks>
    [ExplicitKey]
    public required string Key { get; init; }

    /// <summary>
    /// Setting value as text.
    /// </summary>
    /// <remarks>
    /// Values are stored as text and parsed by consuming code.
    /// For complex values, use JSON serialization.
    /// </remarks>
    public required string Value { get; init; }

    /// <summary>
    /// Human-readable description of the setting.
    /// </summary>
    /// <remarks>Helps administrators understand the purpose of each setting.</remarks>
    public string? Description { get; init; }

    /// <summary>
    /// When the setting was last updated.
    /// </summary>
    [Computed]
    public DateTimeOffset UpdatedAt { get; init; }
}
