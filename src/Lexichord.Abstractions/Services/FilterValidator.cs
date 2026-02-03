// =============================================================================
// File: FilterValidator.cs
// Project: Lexichord.Abstractions
// Description: Validates SearchFilter criteria for correctness (v0.5.5a).
// =============================================================================
// LOGIC: Implements IFilterValidator with validation rules:
//   - Path patterns: Non-empty, no null bytes, no ".." traversal
//   - Extensions: Non-empty, no path separators (/ or \)
//   - Date range: Start <= End when both provided
//   Thread-safe and stateless; suitable for singleton registration in DI.
//   Collects all errors rather than failing on the first one.
// =============================================================================
// VERSION: v0.5.5a (Filter Model)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Services;

/// <summary>
/// Validates search filter criteria for correctness.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FilterValidator"/> implements <see cref="IFilterValidator"/> with
/// comprehensive validation for <see cref="SearchFilter"/> instances. The validator
/// checks for security issues (path traversal, null byte injection) and structural
/// correctness (non-empty values, valid date ranges).
/// </para>
/// <para>
/// <b>Security Validations:</b>
/// <list type="bullet">
///   <item><description>Path traversal: Rejects patterns containing ".." to prevent directory escape.</description></item>
///   <item><description>Null bytes: Rejects patterns containing <c>\0</c> to prevent null byte injection.</description></item>
///   <item><description>Extension abuse: Rejects extensions containing path separators.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Structural Validations:</b>
/// <list type="bullet">
///   <item><description>Empty values: Rejects null, empty, or whitespace-only strings.</description></item>
///   <item><description>Date range: Rejects ranges where Start is after End.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and thread-safe.
/// It can be registered as a singleton in dependency injection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5a as part of The Filter System feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddSingleton&lt;IFilterValidator, FilterValidator&gt;();
///
/// // Use in service
/// public class SearchService
/// {
///     private readonly IFilterValidator _validator;
///
///     public SearchService(IFilterValidator validator)
///     {
///         _validator = validator;
///     }
///
///     public async Task&lt;SearchResult&gt; SearchAsync(string query, SearchFilter filter)
///     {
///         var errors = _validator.Validate(filter);
///         if (errors.Count > 0)
///             throw new ValidationException(errors);
///
///         // Proceed with validated filter...
///     }
/// }
/// </code>
/// </example>
public sealed class FilterValidator : IFilterValidator
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Validates all criteria in the filter and collects all errors.
    /// The validation order is:
    /// <list type="number">
    ///   <item><description>Path patterns (if present)</description></item>
    ///   <item><description>File extensions (if present)</description></item>
    ///   <item><description>Modified date range (if present)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Tags and HasHeadings are not validated as they have no format constraints.
    /// </para>
    /// </remarks>
    public IReadOnlyList<FilterValidationError> Validate(SearchFilter filter)
    {
        var errors = new List<FilterValidationError>();

        // Validate path patterns
        if (filter.PathPatterns is not null)
        {
            for (int i = 0; i < filter.PathPatterns.Count; i++)
            {
                var pattern = filter.PathPatterns[i];
                var error = ValidatePattern(pattern);
                if (error is not null)
                {
                    // Update property to include index
                    errors.Add(error with { Property = $"PathPatterns[{i}]" });
                }
            }
        }

        // Validate file extensions
        if (filter.FileExtensions is not null)
        {
            foreach (var ext in filter.FileExtensions)
            {
                if (string.IsNullOrWhiteSpace(ext))
                {
                    errors.Add(new FilterValidationError(
                        Code: "ExtensionEmpty",
                        Message: "File extension cannot be empty.",
                        Property: "FileExtensions"));
                }
                else if (ext.Contains('/') || ext.Contains('\\'))
                {
                    errors.Add(new FilterValidationError(
                        Code: "ExtensionInvalid",
                        Message: $"File extension '{ext}' contains path separators.",
                        Property: "FileExtensions"));
                }
            }
        }

        // Validate date range
        if (filter.ModifiedRange is not null && !filter.ModifiedRange.IsValid)
        {
            errors.Add(new FilterValidationError(
                Code: "DateRangeInvalid",
                Message: "Start date cannot be after end date.",
                Property: "ModifiedRange"));
        }

        return errors;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Validates a single path pattern for:
    /// <list type="bullet">
    ///   <item><description>Emptiness: Pattern cannot be null, empty, or whitespace.</description></item>
    ///   <item><description>Null bytes: Pattern cannot contain <c>\0</c>.</description></item>
    ///   <item><description>Path traversal: Pattern cannot contain "..".</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method is useful for real-time validation in UI controls.
    /// </para>
    /// </remarks>
    public FilterValidationError? ValidatePattern(string pattern)
    {
        // Check for empty/whitespace
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new FilterValidationError(
                Code: "PatternEmpty",
                Message: "Path pattern cannot be empty.",
                Property: "PathPatterns");
        }

        // Check for null bytes (security: prevents null byte injection)
        if (pattern.Contains('\0'))
        {
            return new FilterValidationError(
                Code: "PatternNullByte",
                Message: "Path pattern contains null byte.",
                Property: "PathPatterns");
        }

        // Check for path traversal (security: prevents directory escape)
        if (pattern.Contains(".."))
        {
            return new FilterValidationError(
                Code: "PatternTraversal",
                Message: "Path pattern cannot contain '..' for path traversal.",
                Property: "PathPatterns");
        }

        // Pattern is valid
        return null;
    }
}
