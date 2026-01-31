namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Writes configuration overrides to project-level configuration files.
/// </summary>
/// <remarks>
/// LOGIC: This interface provides project-level configuration modification capabilities
/// for the Override UI feature (v0.3.6c). It enables users to ignore rules and exclude
/// terms for specific projects via context menu actions.
///
/// Key behaviors:
/// - All write operations are atomic (temp file + rename)
/// - Automatically creates .lexichord/ directory and style.yaml if needed
/// - Thread-safe via internal write synchronization
/// - YAML formatting is preserved where possible
///
/// File location: {workspace}/.lexichord/style.yaml
///
/// Thread Safety:
/// - Write operations are synchronized via internal lock
/// - Read operations (IsRuleIgnored, IsTermExcluded) are thread-safe
///
/// Dependencies:
/// - IWorkspaceService for workspace root path detection
/// - YamlDotNet for YAML serialization
///
/// Version: v0.3.6c
/// </remarks>
/// <example>
/// <code>
/// // Ignore a specific rule for the current project
/// await _writer.IgnoreRuleAsync("TERM-001");
///
/// // Exclude a term from flagging
/// await _writer.ExcludeTermAsync("whitelist");
///
/// // Check if rule is currently ignored
/// if (_writer.IsRuleIgnored("TERM-001"))
/// {
///     // Rule is ignored for this project
/// }
/// </code>
/// </example>
public interface IProjectConfigurationWriter
{
    /// <summary>
    /// Adds a rule to the project's ignored rules list.
    /// </summary>
    /// <param name="ruleId">The rule ID to ignore (e.g., "TERM-001", "PASSIVE-*").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successfully written; false if operation failed.</returns>
    /// <remarks>
    /// LOGIC: Adds the rule ID to the ignored_rules list in style.yaml.
    /// If the rule is already ignored, this is a no-op returning true.
    /// Creates the configuration file if it doesn't exist.
    ///
    /// The ignored_rules list is maintained in alphabetical order for readability.
    /// </remarks>
    Task<bool> IgnoreRuleAsync(string ruleId, CancellationToken ct = default);

    /// <summary>
    /// Removes a rule from the project's ignored rules list.
    /// </summary>
    /// <param name="ruleId">The rule ID to restore.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successfully written; false if operation failed.</returns>
    /// <remarks>
    /// LOGIC: Removes the rule ID from the ignored_rules list.
    /// If the rule is not in the list, this is a no-op returning true.
    /// Case-insensitive matching is used.
    /// </remarks>
    Task<bool> RestoreRuleAsync(string ruleId, CancellationToken ct = default);

    /// <summary>
    /// Adds a term to the project's exclusions list.
    /// </summary>
    /// <param name="term">The term pattern to exclude from flagging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successfully written; false if operation failed.</returns>
    /// <remarks>
    /// LOGIC: Adds the term to the terminology.exclusions list in style.yaml.
    /// Excluded terms will not trigger style violations in this project,
    /// even if they are flagged by global terminology rules.
    ///
    /// Creates the configuration file if it doesn't exist.
    /// The exclusions list is maintained in alphabetical order.
    /// </remarks>
    Task<bool> ExcludeTermAsync(string term, CancellationToken ct = default);

    /// <summary>
    /// Removes a term from the project's exclusions list.
    /// </summary>
    /// <param name="term">The term pattern to restore.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successfully written; false if operation failed.</returns>
    /// <remarks>
    /// LOGIC: Removes the term from the terminology.exclusions list.
    /// If the term is not in the list, this is a no-op returning true.
    /// Case-insensitive matching is used.
    /// </remarks>
    Task<bool> RestoreTermAsync(string term, CancellationToken ct = default);

    /// <summary>
    /// Checks if a rule is currently ignored in the project configuration.
    /// </summary>
    /// <param name="ruleId">The rule ID to check.</param>
    /// <returns>True if the rule is in the project's ignored rules list.</returns>
    /// <remarks>
    /// LOGIC: Reads the current project configuration and checks
    /// if the rule ID exists in the ignored_rules list.
    /// Returns false if no project configuration exists.
    ///
    /// Note: This is an exact match check. Use IConflictResolver.IsRuleIgnored
    /// for wildcard pattern matching.
    /// </remarks>
    bool IsRuleIgnored(string ruleId);

    /// <summary>
    /// Checks if a term is currently excluded in the project configuration.
    /// </summary>
    /// <param name="term">The term to check.</param>
    /// <returns>True if the term is in the project's exclusions list.</returns>
    /// <remarks>
    /// LOGIC: Reads the current project configuration and checks
    /// if the term exists in the terminology.exclusions list.
    /// Returns false if no project configuration exists.
    /// Case-insensitive matching is used.
    /// </remarks>
    bool IsTermExcluded(string term);

    /// <summary>
    /// Creates the project configuration file if it doesn't exist.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Absolute path to the configuration file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace is open.</exception>
    /// <remarks>
    /// LOGIC: Ensures both the .lexichord/ directory and style.yaml file exist.
    /// If the file doesn't exist, creates it with default content:
    /// - version: 1
    /// - empty ignored_rules list
    /// - empty terminology.exclusions list
    ///
    /// If the file already exists, returns the path without modification.
    /// </remarks>
    Task<string> EnsureConfigurationFileAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the path to the project configuration file.
    /// </summary>
    /// <returns>
    /// Absolute path to {workspace}/.lexichord/style.yaml,
    /// or null if no workspace is open.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns the expected path to the configuration file.
    /// The file may not exist - use EnsureConfigurationFileAsync to create it.
    /// Returns null when IWorkspaceService.IsWorkspaceOpen is false.
    /// </remarks>
    string? GetConfigurationFilePath();
}
