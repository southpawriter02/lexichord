using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Matches file paths against .lexichordignore patterns.
/// </summary>
/// <remarks>
/// LOGIC: Implements gitignore-like pattern matching:
/// - Patterns are processed in order; last match wins
/// - Patterns starting with '!' negate previous matches
/// - Patterns ending with '/' match directories only
/// - '*' matches anything except '/'
/// - '**' matches anything including '/'
/// - '#' at start of line indicates a comment
/// - Empty lines are ignored
///
/// Example patterns:
///   *.log          - Ignore all .log files
///   drafts/        - Ignore the drafts directory
///   !important.log - Don't ignore important.log
///   **/temp/       - Ignore temp dirs at any depth
///
/// Version: v0.2.6d
/// </remarks>
public class IgnorePatternMatcher
{
    private readonly List<(Regex Regex, bool IsNegation)> _patterns = new();
    private readonly ILogger<IgnorePatternMatcher>? _logger;

    /// <summary>
    /// Creates a matcher with no patterns (matches nothing).
    /// </summary>
    public IgnorePatternMatcher()
    {
    }

    /// <summary>
    /// Creates a matcher from pattern lines.
    /// </summary>
    /// <param name="patternLines">Lines from .lexichordignore file.</param>
    /// <param name="logger">Optional logger for pattern errors.</param>
    public IgnorePatternMatcher(
        IEnumerable<string> patternLines,
        ILogger<IgnorePatternMatcher>? logger = null)
    {
        _logger = logger;

        foreach (var line in patternLines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            try
            {
                var isNegation = trimmed.StartsWith('!');
                var pattern = isNegation ? trimmed[1..] : trimmed;
                var regex = ConvertToRegex(pattern);
                _patterns.Add((regex, isNegation));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Invalid ignore pattern: {Pattern}", trimmed);
            }
        }
    }

    /// <summary>
    /// Checks if a file path should be ignored.
    /// </summary>
    /// <param name="relativePath">Path relative to project root.</param>
    /// <returns>True if the file should be ignored.</returns>
    /// <remarks>
    /// LOGIC: Processes patterns in order. Each matching pattern
    /// toggles the ignored state. Final state after all patterns
    /// determines the result.
    /// </remarks>
    public bool IsIgnored(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return false;
        }

        // Normalize path separators
        var normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        var isIgnored = false;

        foreach (var (regex, isNegation) in _patterns)
        {
            if (regex.IsMatch(normalizedPath))
            {
                isIgnored = !isNegation;
            }
        }

        return isIgnored;
    }

    /// <summary>
    /// Gets the number of loaded patterns.
    /// </summary>
    public int PatternCount => _patterns.Count;

    /// <summary>
    /// Converts a gitignore-style pattern to a regex.
    /// </summary>
    private static Regex ConvertToRegex(string pattern)
    {
        var normalized = pattern.Replace('\\', '/').Trim('/');
        var isDirectoryPattern = pattern.EndsWith('/');

        // Build regex pattern
        var regexPattern = new System.Text.StringBuilder();
        var i = 0;

        // Pattern anchoring rules:
        // - Directory pattern (ends with '/') anchors at root: "drafts/" matches only root
        // - Pattern with '/' anchors at root: "src/file" matches only at root
        // - Simple pattern without '/' matches anywhere: "*.log" matches any *.log
        // - Pattern starting with "**/" matches at any level
        var containsSlash = normalized.Contains('/');
        var startsWithRecursive = pattern.StartsWith("**/");

        if (!containsSlash && !startsWithRecursive && !isDirectoryPattern)
        {
            // Simple pattern (no slash, not directory) - match at any directory level
            regexPattern.Append("(^|.*/?)?");
        }
        else
        {
            regexPattern.Append('^');
        }

        while (i < normalized.Length)
        {
            var c = normalized[i];

            switch (c)
            {
                case '*':
                    if (i + 1 < normalized.Length && normalized[i + 1] == '*')
                    {
                        // ** matches any path segment(s)
                        if (i + 2 < normalized.Length && normalized[i + 2] == '/')
                        {
                            // **/ at start or middle
                            regexPattern.Append("(.*/)?");
                            i += 2;
                        }
                        else
                        {
                            // ** at end
                            regexPattern.Append(".*");
                            i += 1;
                        }
                    }
                    else
                    {
                        // * matches anything except /
                        regexPattern.Append("[^/]*");
                    }
                    break;

                case '?':
                    regexPattern.Append("[^/]");
                    break;

                case '.':
                case '(':
                case ')':
                case '+':
                case '^':
                case '$':
                case '|':
                case '{':
                case '}':
                case '[':
                case ']':
                    regexPattern.Append('\\');
                    regexPattern.Append(c);
                    break;

                default:
                    regexPattern.Append(c);
                    break;
            }

            i++;
        }

        // Directory patterns (ending with /) match directories and their contents
        if (pattern.EndsWith('/'))
        {
            regexPattern.Append("(/.*)?");
        }

        regexPattern.Append('$');

        return new Regex(
            regexPattern.ToString(),
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }
}
