namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the hierarchical sources for style configuration.
/// </summary>
/// <remarks>
/// LOGIC: Configuration is loaded and merged from multiple sources in priority order.
/// Higher-numbered sources override lower-numbered sources:
///
/// 1. System (lowest priority): Embedded application defaults
/// 2. User: User-specific settings from appsettings or user config file
/// 3. Project (highest priority): Project-specific settings from .lexichord/style.yaml
///
/// Design Decisions:
/// - System defaults are always available (embedded)
/// - User settings persist across workspaces
/// - Project settings are workspace-specific and source-controllable
/// - Project settings require Writer Pro license
///
/// Version: v0.3.6a
/// </remarks>
public enum ConfigurationSource
{
    /// <summary>
    /// System-wide defaults embedded in the application.
    /// </summary>
    /// <remarks>
    /// LOGIC: These are the baseline settings that apply when no other
    /// configuration is present. They provide sensible defaults for all users.
    /// </remarks>
    System = 0,

    /// <summary>
    /// User-specific settings stored in the user's profile.
    /// </summary>
    /// <remarks>
    /// LOGIC: Overrides system defaults. Applied across all workspaces
    /// for the current user. Stored in appsettings or dedicated user config.
    /// </remarks>
    User = 1,

    /// <summary>
    /// Project-specific settings stored in the workspace.
    /// </summary>
    /// <remarks>
    /// LOGIC: Overrides both system and user settings. Stored in
    /// {workspace}/.lexichord/style.yaml. Requires Writer Pro license.
    /// Can be committed to version control for team sharing.
    /// </remarks>
    Project = 2
}
