using System.Text.RegularExpressions;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Analyzes regex patterns for potential ReDoS vulnerabilities.
/// </summary>
/// <remarks>
/// LOGIC: Uses heuristic detection to identify dangerous patterns
/// before execution, preventing CPU exhaustion attacks.
///
/// Detection targets:
/// 1. Nested quantifiers: (a+)+ or (a*)*
/// 2. Overlapping alternation: (a|a)+
/// 3. Excessive backtracking patterns
///
/// Version: v0.2.3c
/// </remarks>
internal static class PatternComplexityAnalyzer
{
    /// <summary>
    /// Pattern complexity levels.
    /// </summary>
    public enum ComplexityLevel
    {
        /// <summary>Pattern is safe to execute.</summary>
        Safe,

        /// <summary>Pattern may be slow with certain inputs.</summary>
        Suspicious,

        /// <summary>Pattern is likely vulnerable to ReDoS.</summary>
        Dangerous
    }

    /// <summary>
    /// Result of complexity analysis.
    /// </summary>
    /// <param name="Level">The complexity level.</param>
    /// <param name="Reason">Explanation if not safe.</param>
    public readonly record struct AnalysisResult(ComplexityLevel Level, string? Reason = null)
    {
        public bool IsSafe => Level == ComplexityLevel.Safe;
        public bool IsDangerous => Level == ComplexityLevel.Dangerous;
    }

    // Patterns that detect nested quantifiers like (a+)+
    private static readonly Regex NestedQuantifiersPattern = new(
        @"\([^)]*[+*]\)[+*?]|\([^)]*[+*]\)\{",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    // Patterns that detect overlapping alternation like (a|a)+
    private static readonly Regex OverlappingAlternationPattern = new(
        @"\(([^|)]+)\|(\1)\)[+*]",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    // Patterns with multiple adjacent quantifiers
    private static readonly Regex AdjacentQuantifiersPattern = new(
        @"[+*?]\s*[+*?]",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    /// <summary>
    /// Analyzes a regex pattern for potential ReDoS vulnerabilities.
    /// </summary>
    /// <param name="pattern">The regex pattern to analyze.</param>
    /// <returns>Analysis result with complexity level and reason.</returns>
    public static AnalysisResult Analyze(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return new AnalysisResult(ComplexityLevel.Safe);
        }

        try
        {
            // Check for nested quantifiers (most dangerous)
            if (NestedQuantifiersPattern.IsMatch(pattern))
            {
                return new AnalysisResult(
                    ComplexityLevel.Dangerous,
                    "Pattern contains nested quantifiers which may cause exponential backtracking.");
            }

            // Check for overlapping alternation
            if (OverlappingAlternationPattern.IsMatch(pattern))
            {
                return new AnalysisResult(
                    ComplexityLevel.Dangerous,
                    "Pattern contains overlapping alternation which may cause exponential backtracking.");
            }

            // Check for adjacent quantifiers (suspicious but not always dangerous)
            if (AdjacentQuantifiersPattern.IsMatch(pattern))
            {
                return new AnalysisResult(
                    ComplexityLevel.Suspicious,
                    "Pattern contains adjacent quantifiers which may cause slow matching.");
            }

            // Pattern appears safe
            return new AnalysisResult(ComplexityLevel.Safe);
        }
        catch (RegexMatchTimeoutException)
        {
            // If our analysis patterns time out, treat the input as suspicious
            return new AnalysisResult(
                ComplexityLevel.Suspicious,
                "Pattern is too complex to analyze.");
        }
    }

    /// <summary>
    /// Quick check if a pattern should be rejected outright.
    /// </summary>
    /// <param name="pattern">The regex pattern to check.</param>
    /// <returns>True if the pattern should be blocked.</returns>
    public static bool ShouldBlock(string pattern)
    {
        return Analyze(pattern).IsDangerous;
    }
}
