using System.Text.RegularExpressions;

namespace Lexichord.Host.Services;

/// <summary>
/// Matches file paths against glob-style ignore patterns.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): Converts glob patterns to Regex for matching:
/// - "**" matches any directory depth
/// - "*" matches any characters within a path segment
/// - Patterns are case-insensitive
/// - Forward slashes are used internally for consistency
/// </remarks>
internal sealed class IgnorePatternMatcher
{
    private readonly List<Regex> _patterns;

    /// <summary>
    /// Creates a new ignore pattern matcher.
    /// </summary>
    /// <param name="patterns">Glob patterns to match against.</param>
    public IgnorePatternMatcher(IEnumerable<string> patterns)
    {
        _patterns = patterns
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(ConvertGlobToRegex)
            .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToList();
    }

    /// <summary>
    /// Checks if a relative path should be ignored.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <returns>True if the path matches any ignore pattern.</returns>
    public bool IsIgnored(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return false;

        // Normalize to forward slashes for consistent matching
        var normalizedPath = relativePath.Replace('\\', '/');

        return _patterns.Any(p => p.IsMatch(normalizedPath));
    }

    /// <summary>
    /// Converts a glob pattern to a Regex pattern.
    /// </summary>
    /// <remarks>
    /// LOGIC: Conversion rules:
    /// - "**" → ".*" (match any characters including /)
    /// - "*" → "[^/]*" (match any characters except /)
    /// - "?" → "[^/]" (match single character except /)
    /// - "." → "\." (escape dots)
    /// - Pattern anchored at start if no leading wildcard
    /// </remarks>
    private static string ConvertGlobToRegex(string glob)
    {
        var pattern = glob.Replace('\\', '/');

        // Escape regex special characters except * and ?
        pattern = Regex.Escape(pattern);

        // Convert escaped glob wildcards back to regex
        pattern = pattern.Replace("\\*\\*", ".*");     // ** matches anything
        pattern = pattern.Replace("\\*", "[^/]*");     // * matches within segment
        pattern = pattern.Replace("\\?", "[^/]");      // ? matches single char

        // Anchor pattern appropriately
        if (!pattern.StartsWith(".*"))
        {
            // Match at path segment boundary or start
            pattern = "(^|/)" + pattern;
        }

        // Allow matching at end or before path separator
        if (!pattern.EndsWith(".*"))
        {
            pattern = pattern + "($|/)";
        }

        return pattern;
    }
}
