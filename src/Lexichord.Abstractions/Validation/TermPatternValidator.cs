using System.Text.RegularExpressions;

namespace Lexichord.Abstractions.Validation;

/// <summary>
/// Validates term patterns for safety and correctness.
/// </summary>
/// <remarks>
/// LOGIC: Ensures patterns are safe to use in document analysis.
/// - Validates pattern length (max 500 characters)
/// - Detects regex patterns by metacharacter presence
/// - Validates regex compilation with timeout to prevent ReDoS
///
/// Security considerations:
/// - 100ms regex timeout prevents catastrophic backtracking
/// - Pattern length limit prevents memory exhaustion
/// </remarks>
public static class TermPatternValidator
{
    /// <summary>
    /// Maximum allowed pattern length.
    /// </summary>
    public const int MaxPatternLength = 500;

    /// <summary>
    /// Timeout for regex compilation and matching.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Regex metacharacters that indicate a pattern is a regex.
    /// </summary>
    private static readonly char[] RegexMetaCharacters = { '*', '+', '?', '^', '$', '[', ']', '(', ')', '{', '}', '|', '\\', '.' };

    /// <summary>
    /// Validates a term pattern for safety and correctness.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <returns>Result indicating success or failure with error message.</returns>
    public static Contracts.Result<bool> Validate(string? pattern)
    {
        // Empty pattern validation
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Contracts.Result<bool>.Failure("Pattern cannot be empty.");
        }

        // Length validation
        if (pattern.Length > MaxPatternLength)
        {
            return Contracts.Result<bool>.Failure($"Pattern must be {MaxPatternLength} characters or less.");
        }

        // Check if pattern looks like regex
        if (LooksLikeRegex(pattern))
        {
            return ValidateRegexPattern(pattern);
        }

        // Plain text pattern is valid
        return Contracts.Result<bool>.Success(true);
    }

    /// <summary>
    /// Determines if a pattern appears to be a regular expression.
    /// </summary>
    /// <param name="pattern">The pattern to check.</param>
    /// <returns>True if the pattern contains regex metacharacters.</returns>
    public static bool LooksLikeRegex(string pattern)
    {
        return pattern.IndexOfAny(RegexMetaCharacters) >= 0;
    }

    /// <summary>
    /// Validates a regex pattern for safety.
    /// </summary>
    /// <param name="pattern">The regex pattern to validate.</param>
    /// <returns>Result indicating success or failure with error message.</returns>
    private static Contracts.Result<bool> ValidateRegexPattern(string pattern)
    {
        try
        {
            // Attempt to compile the regex with timeout
            var regex = new Regex(pattern, RegexOptions.Compiled, RegexTimeout);

            // Test the regex against a sample string to catch runtime issues
            _ = regex.IsMatch("test sample text for validation");

            return Contracts.Result<bool>.Success(true);
        }
        catch (ArgumentException ex)
        {
            return Contracts.Result<bool>.Failure($"Invalid regex pattern: {ex.Message}");
        }
        catch (RegexMatchTimeoutException)
        {
            return Contracts.Result<bool>.Failure(
                "Regex pattern is too complex and may cause performance issues.");
        }
    }
}
