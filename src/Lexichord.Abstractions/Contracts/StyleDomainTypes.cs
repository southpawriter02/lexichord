namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a collection of style rules loaded from a YAML configuration.
/// </summary>
/// <remarks>
/// LOGIC: StyleSheet is the "compiled" form of a YAML style definition file.
/// Rules are organized by category and can have multiple severity levels.
/// The Empty static property provides a valid, no-op sheet for initialization.
///
/// Full implementation in v0.2.1b expands this with:
/// - IReadOnlyList&lt;StyleRule&gt; Rules
/// - Version information
/// - Metadata (author, description, extends)
/// </remarks>
public sealed record StyleSheet
{
    /// <summary>
    /// Gets an empty style sheet with no rules.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for initialization before the real sheet is loaded.
    /// Analyzing with Empty returns no violations.
    /// </remarks>
    public static StyleSheet Empty { get; } = new();

    /// <summary>
    /// Gets the rules in this style sheet.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stub property returning empty list.
    /// Full implementation in v0.2.1b provides actual rules.
    /// </remarks>
    public IReadOnlyList<StyleRule> Rules { get; init; } = [];

    /// <summary>
    /// Gets the name of this style sheet.
    /// </summary>
    public string Name { get; init; } = "Default";

    /// <summary>
    /// Gets the version of this style sheet.
    /// </summary>
    public string Version { get; init; } = "1.0.0";
}

/// <summary>
/// Represents a single style rule that checks for a specific pattern.
/// </summary>
/// <remarks>
/// LOGIC: Rules are the atomic unit of style checking.
/// Each rule has an ID, pattern, category, and severity.
/// Full implementation in v0.2.1b expands this with pattern matching logic.
/// </remarks>
/// <param name="Id">Unique rule identifier (e.g., "passive-voice").</param>
/// <param name="Name">Display name for the rule.</param>
/// <param name="Category">Category grouping (vocabulary, grammar, punctuation, etc.).</param>
/// <param name="Severity">Default severity when this rule is violated.</param>
public sealed record StyleRule(
    string Id,
    string Name,
    RuleCategory Category,
    ViolationSeverity Severity)
{
    /// <summary>
    /// Gets the description of what this rule checks for.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the pattern type used for matching.
    /// </summary>
    public PatternType PatternType { get; init; } = PatternType.Literal;

    /// <summary>
    /// Gets the pattern string (regex, literal, or word list reference).
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Gets the suggestion text shown when this rule is violated.
    /// </summary>
    public string? Suggestion { get; init; }
}

/// <summary>
/// Represents a style rule violation found during analysis.
/// </summary>
/// <remarks>
/// LOGIC: Violations are produced by StyleEngine.AnalyzeAsync().
/// They contain location information for highlighting in the editor.
/// Full implementation in v0.2.1b expands with span information.
/// </remarks>
/// <param name="RuleId">The ID of the violated rule.</param>
/// <param name="Message">Human-readable violation message.</param>
/// <param name="Severity">Severity level of this violation.</param>
/// <param name="StartOffset">Character offset where the violation starts.</param>
/// <param name="Length">Length of the violating text.</param>
public sealed record StyleViolation(
    string RuleId,
    string Message,
    ViolationSeverity Severity,
    int StartOffset,
    int Length)
{
    /// <summary>
    /// Gets the suggested replacement text, if available.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Gets the original text that triggered the violation.
    /// </summary>
    public string? MatchedText { get; init; }
}

/// <summary>
/// Categories for organizing style rules.
/// </summary>
/// <remarks>
/// LOGIC: Categories enable filtering and grouping in the UI.
/// They correspond to sections in the YAML style definition.
/// </remarks>
public enum RuleCategory
{
    /// <summary>Word choice and vocabulary rules.</summary>
    Vocabulary,

    /// <summary>Grammar and sentence structure rules.</summary>
    Grammar,

    /// <summary>Punctuation and formatting rules.</summary>
    Punctuation,

    /// <summary>Consistency rules (e.g., capitalization, spelling variants).</summary>
    Consistency,

    /// <summary>Document structure rules (e.g., heading levels).</summary>
    Structure,

    /// <summary>Custom user-defined rules.</summary>
    Custom
}

/// <summary>
/// Severity levels for style violations.
/// </summary>
/// <remarks>
/// LOGIC: Severity controls visual feedback in the editor.
/// - Error: Red underline, blocks "clean" status
/// - Warning: Yellow underline, advisory
/// - Suggestion: Blue underline, optional improvement
/// - Hint: Subtle indicator, informational only
/// </remarks>
public enum ViolationSeverity
{
    /// <summary>Informational hint, minimal visual indicator.</summary>
    Hint = 0,

    /// <summary>Optional suggestion for improvement.</summary>
    Suggestion = 1,

    /// <summary>Advisory warning, should be addressed.</summary>
    Warning = 2,

    /// <summary>Critical error, must be fixed.</summary>
    Error = 3
}

/// <summary>
/// Pattern matching modes for style rules.
/// </summary>
/// <remarks>
/// LOGIC: Different pattern types enable flexible rule definitions.
/// Full implementation in v0.2.1b uses these for pattern compilation.
/// </remarks>
public enum PatternType
{
    /// <summary>Exact literal string match (case-insensitive).</summary>
    Literal,

    /// <summary>Regular expression pattern.</summary>
    Regex,

    /// <summary>Reference to a word list file.</summary>
    WordList,

    /// <summary>Custom analyzer implementation.</summary>
    Custom
}

/// <summary>
/// Result of validating YAML style sheet content.
/// </summary>
/// <param name="IsValid">Whether the YAML is valid.</param>
/// <param name="ErrorMessage">Error message if validation failed.</param>
/// <param name="LineNumber">Line number of the error, if applicable.</param>
public sealed record StyleSheetLoadResult(
    bool IsValid,
    string? ErrorMessage = null,
    int? LineNumber = null)
{
    /// <summary>
    /// Gets a successful validation result.
    /// </summary>
    public static StyleSheetLoadResult Success { get; } = new(true);

    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    public static StyleSheetLoadResult Failure(string message, int? line = null)
        => new(false, message, line);
}

/// <summary>
/// Event args for style-related file changes.
/// </summary>
public sealed class StyleFileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path of the changed file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public required WatcherChangeTypes ChangeType { get; init; }
}

/// <summary>
/// Event args for style watcher errors.
/// </summary>
public sealed class StyleWatcherErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public required Exception Exception { get; init; }
}
