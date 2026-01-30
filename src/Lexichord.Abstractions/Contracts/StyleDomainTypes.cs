using System.Text.RegularExpressions;

namespace Lexichord.Abstractions.Contracts;

#region Enums

/// <summary>
/// Categories for organizing style rules.
/// </summary>
/// <remarks>
/// LOGIC: Categories enable filtering and grouping in the UI.
/// They correspond to sections in the YAML style definition.
/// </remarks>
public enum RuleCategory
{
    /// <summary>Word choice and terminology rules (e.g., jargon, preferred terms).</summary>
    Terminology = 0,

    /// <summary>Formatting and whitespace rules (e.g., line length, spacing).</summary>
    Formatting = 1,

    /// <summary>Syntax and grammar rules (e.g., passive voice, sentence structure).</summary>
    Syntax = 2
}

/// <summary>
/// Severity levels for style violations.
/// </summary>
/// <remarks>
/// LOGIC: Severity controls visual feedback in the editor.
/// Ordered by importance (Error is most severe).
/// - Error: Red underline, blocks "clean" status
/// - Warning: Yellow underline, advisory
/// - Info: Blue underline, informational
/// - Hint: Subtle indicator, optional improvement
/// </remarks>
public enum ViolationSeverity
{
    /// <summary>Critical error, must be fixed.</summary>
    Error = 0,

    /// <summary>Advisory warning, should be addressed.</summary>
    Warning = 1,

    /// <summary>Informational message.</summary>
    Info = 2,

    /// <summary>Optional hint for improvement.</summary>
    Hint = 3
}

/// <summary>
/// Pattern matching modes for style rules.
/// </summary>
/// <remarks>
/// LOGIC: Different pattern types enable flexible rule definitions.
/// Each type has different matching semantics and performance characteristics.
/// </remarks>
public enum PatternType
{
    /// <summary>Regular expression pattern (most powerful, lazy compiled).</summary>
    Regex = 0,

    /// <summary>Exact literal string match (case-sensitive).</summary>
    Literal = 1,

    /// <summary>Literal string match ignoring case.</summary>
    LiteralIgnoreCase = 2,

    /// <summary>Match text that starts with the pattern.</summary>
    StartsWith = 3,

    /// <summary>Match text that ends with the pattern.</summary>
    EndsWith = 4,

    /// <summary>Match text that contains the pattern.</summary>
    Contains = 5
}

#endregion

#region StyleRule

/// <summary>
/// Represents a single style rule that checks for a specific pattern.
/// </summary>
/// <remarks>
/// LOGIC: Rules are the atomic unit of style checking.
/// Each rule has an ID, pattern, category, and severity.
///
/// Design Decisions:
/// - Immutable record for thread-safety
/// - Lazy regex compilation with 100ms timeout for safety
/// - Pattern caching via Lazy&lt;Regex&gt; field
///
/// YAML Representation:
/// <code>
/// - id: no-jargon
///   name: Avoid Jargon
///   description: Technical jargon reduces accessibility
///   category: terminology
///   severity: warning
///   pattern: \b(leverage|synergy|paradigm)\b
///   pattern_type: regex
///   suggestion: Use plain language
/// </code>
/// </remarks>
/// <param name="Id">Unique rule identifier (e.g., "no-jargon").</param>
/// <param name="Name">Display name for the rule.</param>
/// <param name="Description">Detailed explanation of what this rule checks.</param>
/// <param name="Category">Category grouping for organization.</param>
/// <param name="DefaultSeverity">Default severity when this rule is violated.</param>
/// <param name="Pattern">The pattern to match (regex, literal, etc.).</param>
/// <param name="PatternType">How to interpret the pattern.</param>
/// <param name="Suggestion">Optional fix suggestion shown to user.</param>
/// <param name="IsEnabled">Whether this rule is active.</param>
public sealed record StyleRule(
    string Id,
    string Name,
    string Description,
    RuleCategory Category,
    ViolationSeverity DefaultSeverity,
    string Pattern,
    PatternType PatternType,
    string? Suggestion,
    bool IsEnabled = true)
{
    /// <summary>
    /// Timeout for regex pattern matching to prevent ReDoS attacks.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Lazily compiled regex for performance.
    /// </summary>
    /// <remarks>
    /// LOGIC: Regex compilation is expensive. We defer it until first use
    /// and cache the result. The Lazy ensures thread-safe initialization.
    /// </remarks>
    private Lazy<Regex?>? _compiledPattern;

    /// <summary>
    /// Gets the compiled regex pattern, or null if not a regex pattern type.
    /// </summary>
    private Regex? CompiledPattern
    {
        get
        {
            if (_compiledPattern is null)
            {
                _compiledPattern = new Lazy<Regex?>(() =>
                {
                    if (PatternType == PatternType.Regex)
                    {
                        try
                        {
                            return new Regex(
                                Pattern,
                                RegexOptions.Compiled | RegexOptions.Multiline,
                                RegexTimeout);
                        }
                        catch (ArgumentException)
                        {
                            // Invalid regex pattern - return null
                            return null;
                        }
                    }
                    return null;
                });
            }
            return _compiledPattern.Value;
        }
    }

    /// <summary>
    /// Finds all violations of this rule in the given content.
    /// </summary>
    /// <param name="content">The text content to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of violations found.</returns>
    /// <remarks>
    /// LOGIC: This is the core pattern matching logic.
    /// 1. If disabled, return empty immediately
    /// 2. Based on PatternType, use appropriate matching strategy
    /// 3. For each match, compute line/column positions
    /// 4. Return violations sorted by position
    /// </remarks>
    public Task<IReadOnlyList<StyleViolation>> FindViolationsAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrEmpty(content) || string.IsNullOrEmpty(Pattern))
        {
            return Task.FromResult<IReadOnlyList<StyleViolation>>([]);
        }

        var violations = new List<StyleViolation>();
        var matches = FindMatches(content);

        foreach (var (startOffset, length) in matches)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var endOffset = startOffset + length;
            var matchedText = content.Substring(startOffset, length);

            // Compute line and column positions
            var (startLine, startColumn) = ComputePosition(content, startOffset);
            var (endLine, endColumn) = ComputePosition(content, endOffset);

            var violation = new StyleViolation(
                Rule: this,
                Message: $"{Name}: {Description}",
                StartOffset: startOffset,
                EndOffset: endOffset,
                StartLine: startLine,
                StartColumn: startColumn,
                EndLine: endLine,
                EndColumn: endColumn,
                MatchedText: matchedText,
                Suggestion: Suggestion,
                Severity: DefaultSeverity);

            violations.Add(violation);
        }

        return Task.FromResult<IReadOnlyList<StyleViolation>>(violations);
    }

    /// <summary>
    /// Finds all pattern matches in the content.
    /// </summary>
    /// <returns>Enumerable of (startOffset, length) tuples.</returns>
    private IEnumerable<(int StartOffset, int Length)> FindMatches(string content)
    {
        return PatternType switch
        {
            PatternType.Regex => FindRegexMatches(content),
            PatternType.Literal => FindLiteralMatches(content, StringComparison.Ordinal),
            PatternType.LiteralIgnoreCase => FindLiteralMatches(content, StringComparison.OrdinalIgnoreCase),
            PatternType.StartsWith => FindStartsWithMatches(content),
            PatternType.EndsWith => FindEndsWithMatches(content),
            PatternType.Contains => FindLiteralMatches(content, StringComparison.Ordinal),
            _ => []
        };
    }

    private IEnumerable<(int, int)> FindRegexMatches(string content)
    {
        var regex = CompiledPattern;
        if (regex is null) yield break;

        MatchCollection? matches = null;
        try
        {
            matches = regex.Matches(content);
        }
        catch (RegexMatchTimeoutException)
        {
            // Pattern timed out - return no matches for safety
            yield break;
        }

        foreach (Match match in matches)
        {
            yield return (match.Index, match.Length);
        }
    }

    private IEnumerable<(int, int)> FindLiteralMatches(string content, StringComparison comparison)
    {
        var index = 0;
        while ((index = content.IndexOf(Pattern, index, comparison)) >= 0)
        {
            yield return (index, Pattern.Length);
            index += Pattern.Length;
        }
    }

    private IEnumerable<(int, int)> FindStartsWithMatches(string content)
    {
        // Match at start of each line
        var lines = content.Split('\n');
        var offset = 0;
        foreach (var line in lines)
        {
            if (line.StartsWith(Pattern, StringComparison.OrdinalIgnoreCase))
            {
                yield return (offset, Pattern.Length);
            }
            offset += line.Length + 1; // +1 for newline
        }
    }

    private IEnumerable<(int, int)> FindEndsWithMatches(string content)
    {
        // Match at end of each line
        var lines = content.Split('\n');
        var offset = 0;
        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');
            if (trimmedLine.EndsWith(Pattern, StringComparison.OrdinalIgnoreCase))
            {
                var matchStart = offset + trimmedLine.Length - Pattern.Length;
                yield return (matchStart, Pattern.Length);
            }
            offset += line.Length + 1;
        }
    }

    /// <summary>
    /// Computes line and column from character offset.
    /// </summary>
    /// <remarks>
    /// LOGIC: Lines and columns are 1-indexed for editor compatibility.
    /// We scan through the content counting newlines.
    /// </remarks>
    private static (int Line, int Column) ComputePosition(string content, int offset)
    {
        var line = 1;
        var column = 1;

        for (var i = 0; i < offset && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    /// <summary>
    /// Creates a disabled copy of this rule.
    /// </summary>
    public StyleRule Disable() => this with { IsEnabled = false };

    /// <summary>
    /// Creates an enabled copy of this rule.
    /// </summary>
    public StyleRule Enable() => this with { IsEnabled = true };

    /// <summary>
    /// Creates a copy with different severity.
    /// </summary>
    public StyleRule WithSeverity(ViolationSeverity severity) =>
        this with { DefaultSeverity = severity };

    /// <summary>
    /// Creates a copy with different pattern.
    /// </summary>
    public StyleRule WithPattern(string pattern, PatternType patternType) =>
        this with { Pattern = pattern, PatternType = patternType };
}

#endregion

#region StyleViolation

/// <summary>
/// Represents a style rule violation found during analysis.
/// </summary>
/// <remarks>
/// LOGIC: Violations are produced by StyleRule.FindViolationsAsync().
/// They contain complete location information for highlighting in the editor.
///
/// Design Decisions:
/// - Immutable record for thread-safety
/// - Contains reference to original Rule for context
/// - Both offset-based and line/column positions for flexibility
/// </remarks>
/// <param name="Rule">The rule that was violated.</param>
/// <param name="Message">Human-readable violation message.</param>
/// <param name="StartOffset">Character offset where the violation starts (0-indexed).</param>
/// <param name="EndOffset">Character offset where the violation ends (exclusive).</param>
/// <param name="StartLine">Starting line number (1-indexed).</param>
/// <param name="StartColumn">Starting column number (1-indexed).</param>
/// <param name="EndLine">Ending line number.</param>
/// <param name="EndColumn">Ending column number.</param>
/// <param name="MatchedText">The text that triggered the violation.</param>
/// <param name="Suggestion">Optional suggested replacement.</param>
/// <param name="Severity">Severity of this specific violation.</param>
public sealed record StyleViolation(
    StyleRule Rule,
    string Message,
    int StartOffset,
    int EndOffset,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    string MatchedText,
    string? Suggestion,
    ViolationSeverity Severity,
    bool IsFuzzyMatch = false,
    int? FuzzyRatio = null)
{
    /// <summary>
    /// Gets the length of the violating text.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Gets the surrounding context with the match highlighted.
    /// </summary>
    /// <param name="content">The full document content.</param>
    /// <param name="contextChars">Number of characters before/after to include.</param>
    /// <returns>String with match highlighted in square brackets.</returns>
    /// <remarks>
    /// LOGIC: Provides context for displaying violations.
    /// Format: "...prefix[matched]suffix..."
    /// </remarks>
    public string GetSurroundingContext(string content, int contextChars = 20)
    {
        if (string.IsNullOrEmpty(content) || StartOffset < 0 || EndOffset > content.Length)
        {
            return $"[{MatchedText}]";
        }

        var prefixStart = Math.Max(0, StartOffset - contextChars);
        var suffixEnd = Math.Min(content.Length, EndOffset + contextChars);

        var prefix = content[prefixStart..StartOffset];
        var suffix = content[EndOffset..suffixEnd];

        var hasLeadingEllipsis = prefixStart > 0;
        var hasTrailingEllipsis = suffixEnd < content.Length;

        return $"{(hasLeadingEllipsis ? "..." : "")}{prefix}[{MatchedText}]{suffix}{(hasTrailingEllipsis ? "..." : "")}";
    }

    /// <summary>
    /// Creates a copy with different severity.
    /// </summary>
    public StyleViolation WithSeverity(ViolationSeverity severity) =>
        this with { Severity = severity };

    /// <summary>
    /// Creates a copy with different message.
    /// </summary>
    public StyleViolation WithMessage(string message) =>
        this with { Message = message };
}

#endregion

#region StyleSheet

/// <summary>
/// Represents a collection of style rules forming a complete style guide.
/// </summary>
/// <remarks>
/// LOGIC: A StyleSheet is the aggregate root for style governance.
/// It contains:
/// - Metadata about the style guide (name, version, author)
/// - Collection of StyleRules
/// - Inheritance information (extends another sheet)
///
/// Design Decisions:
/// - Immutable to allow safe sharing across threads
/// - Rules are exposed as IReadOnlyList to prevent modification
/// - Helper methods for common filtering operations
/// - Static Empty instance for default/null object pattern
///
/// YAML Representation:
/// <code>
/// name: Corporate Style Guide
/// version: "2.0"
/// author: Documentation Team
/// extends: default
/// rules:
///   - id: no-jargon
///     name: Avoid Jargon
///     ...
/// </code>
/// </remarks>
/// <param name="Name">Display name for the style sheet.</param>
/// <param name="Rules">Collection of style rules.</param>
/// <param name="Description">Optional description of the style guide.</param>
/// <param name="Version">Optional semantic version.</param>
/// <param name="Author">Optional author or team name.</param>
/// <param name="Extends">Optional base style sheet to extend.</param>
public sealed record StyleSheet(
    string Name,
    IReadOnlyList<StyleRule> Rules,
    string? Description = null,
    string? Version = null,
    string? Author = null,
    string? Extends = null)
{
    /// <summary>
    /// Empty style sheet for use as default/null object.
    /// </summary>
    /// <remarks>
    /// LOGIC: Using a static empty instance avoids null checks
    /// throughout the codebase. Any code expecting a StyleSheet
    /// can safely operate on Empty without special cases.
    /// </remarks>
    public static StyleSheet Empty { get; } = new(
        Name: "Empty",
        Rules: Array.Empty<StyleRule>(),
        Description: "No rules configured");

    /// <summary>
    /// Gets all enabled rules.
    /// </summary>
    /// <remarks>
    /// LOGIC: Most operations only care about enabled rules.
    /// This filters once and caches the result in the caller.
    /// </remarks>
    public IReadOnlyList<StyleRule> GetEnabledRules() =>
        Rules.Where(r => r.IsEnabled).ToList().AsReadOnly();

    /// <summary>
    /// Gets rules filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Rules in the specified category.</returns>
    public IReadOnlyList<StyleRule> GetRulesByCategory(RuleCategory category) =>
        Rules.Where(r => r.Category == category && r.IsEnabled).ToList().AsReadOnly();

    /// <summary>
    /// Gets rules filtered by severity.
    /// </summary>
    /// <param name="severity">The minimum severity to include.</param>
    /// <returns>Rules at or above the specified severity.</returns>
    public IReadOnlyList<StyleRule> GetRulesBySeverity(ViolationSeverity severity) =>
        Rules.Where(r => r.DefaultSeverity <= severity && r.IsEnabled).ToList().AsReadOnly();

    /// <summary>
    /// Finds a rule by its unique identifier.
    /// </summary>
    /// <param name="ruleId">The rule ID to search for.</param>
    /// <returns>The rule if found, null otherwise.</returns>
    public StyleRule? FindRuleById(string ruleId) =>
        Rules.FirstOrDefault(r => r.Id.Equals(ruleId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets whether this sheet extends another.
    /// </summary>
    public bool HasBaseSheet => !string.IsNullOrEmpty(Extends);

    /// <summary>
    /// Gets the total number of enabled rules.
    /// </summary>
    public int EnabledRuleCount => Rules.Count(r => r.IsEnabled);

    /// <summary>
    /// Creates a new StyleSheet by merging this one with a base sheet.
    /// </summary>
    /// <param name="baseSheet">The base sheet to merge with.</param>
    /// <returns>A new StyleSheet with merged rules.</returns>
    /// <remarks>
    /// LOGIC: Rules from this sheet override rules from base sheet
    /// with the same ID. This enables "extends: default" behavior.
    ///
    /// Merge priority (highest wins):
    /// 1. Rules from this sheet
    /// 2. Rules from base sheet (if not overridden)
    /// </remarks>
    public StyleSheet MergeWith(StyleSheet baseSheet)
    {
        var thisRuleIds = new HashSet<string>(
            Rules.Select(r => r.Id),
            StringComparer.OrdinalIgnoreCase);

        var mergedRules = Rules.ToList();

        // LOGIC: Add base rules that aren't overridden
        foreach (var baseRule in baseSheet.Rules)
        {
            if (!thisRuleIds.Contains(baseRule.Id))
            {
                mergedRules.Add(baseRule);
            }
        }

        return this with
        {
            Rules = mergedRules.AsReadOnly(),
            Extends = null // Already merged
        };
    }

    /// <summary>
    /// Creates a copy with a rule disabled.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to disable.</param>
    /// <returns>A new StyleSheet with the rule disabled.</returns>
    public StyleSheet DisableRule(string ruleId)
    {
        var newRules = Rules.Select(r =>
            r.Id.Equals(ruleId, StringComparison.OrdinalIgnoreCase)
                ? r.Disable()
                : r).ToList();

        return this with { Rules = newRules.AsReadOnly() };
    }

    /// <summary>
    /// Creates a copy with a rule's severity changed.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to modify.</param>
    /// <param name="severity">The new severity.</param>
    /// <returns>A new StyleSheet with the modified rule.</returns>
    public StyleSheet SetRuleSeverity(string ruleId, ViolationSeverity severity)
    {
        var newRules = Rules.Select(r =>
            r.Id.Equals(ruleId, StringComparison.OrdinalIgnoreCase)
                ? r.WithSeverity(severity)
                : r).ToList();

        return this with { Rules = newRules.AsReadOnly() };
    }
}

#endregion

#region Supporting Types

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

#endregion

#region Terminology CRUD Types

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
/// <remarks>
/// LOGIC: Provides a functional approach to error handling without exceptions.
/// Enables railway-oriented programming for service layer operations.
/// </remarks>
public sealed record Result<T>
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Throws if the result is a failure.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? Error { get; }

    private readonly T? _value;

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static Result<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// Types of changes to the lexicon.
/// </summary>
/// <remarks>
/// LOGIC: Used in LexiconChangedEvent to indicate what type of
/// modification occurred, enabling event handlers to respond appropriately.
/// </remarks>
public enum LexiconChangeType
{
    /// <summary>A new term was added to the lexicon.</summary>
    Created = 0,

    /// <summary>An existing term was modified.</summary>
    Updated = 1,

    /// <summary>A term was soft-deleted (deactivated).</summary>
    Deleted = 2,

    /// <summary>A previously deleted term was reactivated.</summary>
    Reactivated = 3
}

/// <summary>
/// Command for creating a new terminology entry.
/// </summary>
/// <param name="Term">The term pattern to match (literal or regex).</param>
/// <param name="Replacement">Optional suggested replacement text.</param>
/// <param name="Category">Rule category for organization.</param>
/// <param name="Severity">Violation severity level.</param>
/// <param name="Notes">Optional notes about the term.</param>
/// <param name="StyleSheetId">The style sheet this term belongs to.</param>
/// <remarks>
/// LOGIC: Immutable command record ensures thread-safety and clear intent.
/// All validation is performed by the service layer.
/// </remarks>
public sealed record CreateTermCommand(
    string Term,
    string? Replacement = null,
    string Category = "General",
    string Severity = "Suggestion",
    string? Notes = null,
    bool MatchCase = false,
    Guid? StyleSheetId = null);

/// <summary>
/// Command for updating an existing terminology entry.
/// </summary>
/// <param name="Id">The unique identifier of the term to update.</param>
/// <param name="Term">The updated term pattern.</param>
/// <param name="Replacement">Optional updated replacement text.</param>
/// <param name="Category">Updated rule category.</param>
/// <param name="Severity">Updated violation severity level.</param>
/// <param name="Notes">Optional updated notes.</param>
/// <remarks>
/// LOGIC: All fields are required to ensure explicit update intent.
/// Partial updates should fetch the current state first.
/// </remarks>
public sealed record UpdateTermCommand(
    Guid Id,
    string Term,
    string? Replacement = null,
    string Category = "General",
    string Severity = "Suggestion",
    string? Notes = null,
    bool MatchCase = false);

/// <summary>
/// Statistics about the terminology database.
/// </summary>
/// <param name="TotalCount">Total number of terms (active and inactive).</param>
/// <param name="ActiveCount">Number of active terms.</param>
/// <param name="InactiveCount">Number of inactive (soft-deleted) terms.</param>
/// <param name="CategoryCounts">Breakdown of terms by category.</param>
/// <param name="SeverityCounts">Breakdown of terms by severity.</param>
/// <remarks>
/// LOGIC: Provides aggregated view of the lexicon for dashboard display
/// and monitoring purposes.
/// </remarks>
public sealed record TermStatistics(
    int TotalCount,
    int ActiveCount,
    int InactiveCount,
    IReadOnlyDictionary<string, int> CategoryCounts,
    IReadOnlyDictionary<string, int> SeverityCounts);

#endregion

#region Pattern Testing (v0.2.5c)

/// <summary>
/// Result of testing a pattern against sample text.
/// </summary>
/// <param name="IsValid">Whether the pattern is valid and could be tested.</param>
/// <param name="Matches">List of matches found in the sample text.</param>
/// <param name="Error">Error message if the pattern is invalid.</param>
/// <param name="TimedOut">Whether the pattern matching timed out (ReDoS protection).</param>
/// <remarks>
/// LOGIC: Used by the Term Editor Dialog to provide real-time feedback
/// when testing patterns against sample text.
/// 
/// Version: v0.2.5c
/// </remarks>
public sealed record PatternTestResult(
    bool IsValid,
    IReadOnlyList<PatternMatch> Matches,
    string? Error = null,
    bool TimedOut = false)
{
    /// <summary>
    /// Creates a successful test result with matches.
    /// </summary>
    public static PatternTestResult Success(IReadOnlyList<PatternMatch> matches) =>
        new(true, matches);

    /// <summary>
    /// Creates a failed test result with an error message.
    /// </summary>
    public static PatternTestResult Failure(string error) =>
        new(false, Array.Empty<PatternMatch>(), error);

    /// <summary>
    /// Creates a timed-out test result.
    /// </summary>
    public static PatternTestResult Timeout() =>
        new(false, Array.Empty<PatternMatch>(), "Pattern matching timed out (pattern may be too complex)", true);
}

/// <summary>
/// Represents a single pattern match in sample text.
/// </summary>
/// <param name="Start">Starting character offset (0-indexed).</param>
/// <param name="Length">Length of the matched text.</param>
/// <param name="MatchedText">The actual text that was matched.</param>
/// <remarks>
/// LOGIC: Simple record for UI display of matched regions.
/// Used with TermEditorViewModel pattern testing feature.
/// 
/// Version: v0.2.5c
/// </remarks>
public sealed record PatternMatch(
    int Start,
    int Length,
    string MatchedText);

#endregion
