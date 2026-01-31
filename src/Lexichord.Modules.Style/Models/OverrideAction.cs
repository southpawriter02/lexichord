namespace Lexichord.Modules.Style.Models;

/// <summary>
/// Types of override actions available in the Problems Panel context menu.
/// </summary>
/// <remarks>
/// LOGIC: These actions correspond to the context menu options shown
/// when right-clicking a style violation in the Problems Panel.
///
/// Version: v0.3.6c
/// </remarks>
public enum OverrideAction
{
    /// <summary>
    /// Ignore a specific rule for this project.
    /// </summary>
    /// <remarks>
    /// Adds the rule ID to the project's ignored_rules list.
    /// The violation will no longer appear for this project.
    /// </remarks>
    IgnoreRule,

    /// <summary>
    /// Restore a previously ignored rule.
    /// </summary>
    /// <remarks>
    /// Removes the rule ID from the project's ignored_rules list.
    /// The rule will be enforced again for this project.
    /// </remarks>
    RestoreRule,

    /// <summary>
    /// Exclude a term from flagging in this project.
    /// </summary>
    /// <remarks>
    /// Adds the term to the project's terminology.exclusions list.
    /// The term will be allowed in this project even if it would
    /// normally be flagged by global terminology rules.
    /// </remarks>
    ExcludeTerm,

    /// <summary>
    /// Restore a previously excluded term.
    /// </summary>
    /// <remarks>
    /// Removes the term from the project's terminology.exclusions list.
    /// The term will be subject to normal terminology rules again.
    /// </remarks>
    RestoreTerm
}
