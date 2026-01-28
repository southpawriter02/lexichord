namespace Lexichord.Abstractions.Events;

/// <summary>
/// Specifies the scope at which a setting applies.
/// </summary>
/// <remarks>
/// LOGIC: Settings can be scoped to different levels. Handlers may
/// only care about certain scopes.
///
/// Example: UI theme handler cares about User scope; build pipeline
/// handler cares about Application scope.
/// </remarks>
public enum SettingScope
{
    /// <summary>
    /// Application-wide settings (affect all users).
    /// </summary>
    Application = 0,

    /// <summary>
    /// User-specific settings (preferences, customizations).
    /// </summary>
    User = 1,

    /// <summary>
    /// Module-specific settings (module configuration).
    /// </summary>
    Module = 2,

    /// <summary>
    /// Project-specific settings (per-project configuration).
    /// </summary>
    Project = 3,

    /// <summary>
    /// Document-specific settings (per-document overrides).
    /// </summary>
    Document = 4
}
