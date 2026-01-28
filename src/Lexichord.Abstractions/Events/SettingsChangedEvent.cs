namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when application or module settings are changed.
/// </summary>
/// <remarks>
/// LOGIC: This event enables reactive configuration updates across modules.
/// Instead of polling for changes, modules subscribe to settings they care about.
///
/// **When to Publish:**
/// - After a setting value is persisted.
/// - Include both old and new values for diffing.
///
/// **Expected Handlers:**
/// - Theme Manager: React to appearance settings.
/// - Module Loaders: React to enabled/disabled modules.
/// - Agents: React to API key or model changes.
/// - Cache Invalidators: Clear cached data on relevant changes.
///
/// **Handler Responsibilities:**
/// - Handlers SHOULD filter by SettingKey or Scope before processing.
/// - Handlers MAY perform validation on NewValue.
/// - Handlers SHOULD NOT throw on invalid values (log warning instead).
///
/// **Setting Key Conventions:**
/// - Use dot notation: "module.documents.autoSaveInterval"
/// - Use lowercase with dots as separators
/// - Prefix with module name for module-specific settings
/// </remarks>
/// <example>
/// Publishing the event:
/// <code>
/// await _mediator.Publish(new SettingsChangedEvent
/// {
///     SettingKey = "appearance.theme",
///     OldValue = "Dark",
///     NewValue = "Light",
///     Scope = SettingScope.User,
///     ChangedBy = currentUser.Id,
///     CorrelationId = correlationId
/// });
/// </code>
/// </example>
public record SettingsChangedEvent : DomainEventBase
{
    /// <summary>
    /// The unique key identifying the setting that changed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Follows dot-notation convention for hierarchical keys.
    /// Examples: "appearance.theme", "editor.autoSave.enabled",
    /// "module.rag.embeddingModel".
    /// </remarks>
    public required string SettingKey { get; init; }

    /// <summary>
    /// The previous value of the setting (null if newly created).
    /// </summary>
    /// <remarks>
    /// LOGIC: String representation allows generic handling without
    /// knowing the actual type. Handlers can parse as needed.
    /// </remarks>
    public string? OldValue { get; init; }

    /// <summary>
    /// The new value of the setting (null if deleted).
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// The scope at which this setting applies.
    /// </summary>
    /// <remarks>
    /// LOGIC: Handlers can filter by scope. A handler for user preferences
    /// might ignore Application-scoped changes.
    /// </remarks>
    public required SettingScope Scope { get; init; }

    /// <summary>
    /// The module ID if this is a module-specific setting.
    /// </summary>
    /// <remarks>
    /// LOGIC: Only set when Scope is Module. Allows handlers to
    /// filter for their specific module's settings.
    /// </remarks>
    public string? ModuleId { get; init; }

    /// <summary>
    /// Identifier of the user or system that changed the setting.
    /// </summary>
    /// <remarks>
    /// LOGIC: "system" for programmatic changes, user ID for user changes.
    /// Useful for audit logging.
    /// </remarks>
    public required string ChangedBy { get; init; }

    /// <summary>
    /// Optional reason for the change.
    /// </summary>
    /// <remarks>
    /// LOGIC: Useful for audit logging and understanding why a
    /// change was made. Example: "User preference", "Migration script".
    /// </remarks>
    public string? Reason { get; init; }
}
