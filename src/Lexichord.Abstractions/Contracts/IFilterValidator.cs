// =============================================================================
// File: IFilterValidator.cs
// Project: Lexichord.Abstractions
// Description: Interface and error record for filter validation (v0.5.5a).
// =============================================================================
// LOGIC: Defines the validation contract for SearchFilter:
//   - FilterValidationError: Immutable error record with code, message, property
//   - IFilterValidator: Interface for validating filters and individual patterns
//   The validator checks for security issues (path traversal, null bytes) and
//   structural correctness (date range validity, non-empty values).
// =============================================================================
// VERSION: v0.5.5a (Filter Model)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a filter validation error.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FilterValidationError"/> captures a specific validation failure
/// when checking <see cref="SearchFilter"/> criteria. Each error includes:
/// <list type="bullet">
///   <item><description><see cref="Code"/>: A unique identifier for programmatic handling.</description></item>
///   <item><description><see cref="Message"/>: A human-readable description of the issue.</description></item>
///   <item><description><see cref="Property"/>: The property or path that failed validation.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Error Codes:</b>
/// <list type="table">
///   <listheader>
///     <term>Code</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term>PatternEmpty</term>
///     <description>Path pattern is null, empty, or whitespace.</description>
///   </item>
///   <item>
///     <term>PatternNullByte</term>
///     <description>Path pattern contains null byte (security risk).</description>
///   </item>
///   <item>
///     <term>PatternTraversal</term>
///     <description>Path pattern contains ".." for directory traversal (security risk).</description>
///   </item>
///   <item>
///     <term>ExtensionEmpty</term>
///     <description>File extension is null, empty, or whitespace.</description>
///   </item>
///   <item>
///     <term>ExtensionInvalid</term>
///     <description>File extension contains path separators.</description>
///   </item>
///   <item>
///     <term>DateRangeInvalid</term>
///     <description>Date range has start date after end date.</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5a as part of The Filter System feature.
/// </para>
/// </remarks>
/// <param name="Code">
/// Error code for programmatic handling. Use this for conditional logic
/// rather than parsing the message.
/// </param>
/// <param name="Message">
/// Human-readable error message suitable for display to users.
/// </param>
/// <param name="Property">
/// The property that failed validation. May include array indices
/// (e.g., "PathPatterns[0]") for collection items.
/// </param>
/// <example>
/// <code>
/// var errors = validator.Validate(filter);
/// foreach (var error in errors)
/// {
///     if (error.Code == "PatternTraversal")
///     {
///         // Handle security-related error specially
///         logger.LogWarning("Security: {Message}", error.Message);
///     }
///     else
///     {
///         // Display to user
///         Console.WriteLine($"{error.Property}: {error.Message}");
///     }
/// }
/// </code>
/// </example>
public record FilterValidationError(
    string Code,
    string Message,
    string Property);

/// <summary>
/// Validates search filter criteria for correctness.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IFilterValidator"/> provides validation for <see cref="SearchFilter"/>
/// instances before they are used in search queries. The validator checks for:
/// <list type="bullet">
///   <item><description>Security issues: Path traversal attacks, null byte injection.</description></item>
///   <item><description>Structural correctness: Non-empty values, valid date ranges.</description></item>
///   <item><description>Format compliance: Extensions without path separators.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Validation Flow:</b>
/// <list type="number">
///   <item><description>Check each path pattern for emptiness, null bytes, and ".." traversal.</description></item>
///   <item><description>Check each file extension for emptiness and path separators.</description></item>
///   <item><description>Check date range for valid bounds (Start &lt;= End).</description></item>
/// </list>
/// </para>
/// <para>
/// The validator collects all errors rather than failing on the first one,
/// allowing comprehensive feedback to users.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be stateless and thread-safe,
/// suitable for singleton registration in dependency injection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5a as part of The Filter System feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate a filter before search
/// var filter = new SearchFilter(
///     PathPatterns: new[] { "docs/**", "../secrets" },
///     FileExtensions: new[] { "md", "" });
///
/// var errors = validator.Validate(filter);
/// if (errors.Count > 0)
/// {
///     foreach (var error in errors)
///     {
///         Console.WriteLine($"Error: {error.Message}");
///     }
///     return; // Don't proceed with invalid filter
/// }
///
/// // Filter is valid, proceed with search
/// var results = await searchService.SearchAsync(query, filter);
/// </code>
/// </example>
public interface IFilterValidator
{
    /// <summary>
    /// Validates a <see cref="SearchFilter"/> and returns any validation errors.
    /// </summary>
    /// <param name="filter">The filter to validate. Cannot be null.</param>
    /// <returns>
    /// A list of validation errors, or an empty list if the filter is valid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The validator checks all criteria in the filter and collects all errors
    /// rather than failing on the first one. This allows comprehensive feedback.
    /// </para>
    /// <para>
    /// An empty filter (one with no criteria set) is considered valid.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var filter = SearchFilter.ForPath("docs/**");
    /// var errors = validator.Validate(filter);
    ///
    /// if (errors.Count == 0)
    ///     Console.WriteLine("Filter is valid");
    /// </code>
    /// </example>
    IReadOnlyList<FilterValidationError> Validate(SearchFilter filter);

    /// <summary>
    /// Validates a single glob pattern.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <returns>
    /// A <see cref="FilterValidationError"/> if the pattern is invalid,
    /// or <c>null</c> if the pattern is valid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method for real-time validation as users type patterns.
    /// The <see cref="FilterValidationError.Property"/> will be set to "PathPatterns".
    /// </para>
    /// <para>
    /// <b>Validation Rules:</b>
    /// <list type="bullet">
    ///   <item><description>Pattern cannot be null, empty, or whitespace.</description></item>
    ///   <item><description>Pattern cannot contain null bytes (<c>\0</c>).</description></item>
    ///   <item><description>Pattern cannot contain ".." for path traversal.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Real-time validation in UI
    /// var error = validator.ValidatePattern(userInput);
    /// if (error is not null)
    /// {
    ///     ShowErrorTooltip(error.Message);
    /// }
    /// </code>
    /// </example>
    FilterValidationError? ValidatePattern(string pattern);
}
