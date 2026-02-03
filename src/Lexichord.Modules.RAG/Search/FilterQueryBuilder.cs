// =============================================================================
// File: FilterQueryBuilder.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IFilterQueryBuilder for SQL query generation.
// =============================================================================
// LOGIC: Translates SearchFilter criteria into parameterized SQL:
//   - Path patterns: Converted to LIKE expressions via GlobToSql conversion
//   - Extensions: Converted to LOWER(file_extension) = ANY(@extensions)
//   - Date range: Converted to modified_at >= @start AND modified_at <= @end
//   - Headings: Converted to heading IS NOT NULL check
//   Uses CTE pattern for efficient filtered vector search.
// =============================================================================
// VERSION: v0.5.5c (Filter Query Builder)
// DEPENDENCIES: v0.5.5a (SearchFilter, DateRange, IFilterValidator)
// =============================================================================

using System.Text;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Builds SQL query components from <see cref="SearchFilter"/> criteria.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FilterQueryBuilder"/> transforms user-defined filter criteria
/// into parameterized SQL that can be safely executed against PostgreSQL.
/// </para>
/// <para>
/// <b>Query Strategy:</b> The builder generates CTEs for efficient filtered search,
/// ensuring the HNSW vector index is leveraged even when filters are applied.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is stateless (except for <see cref="LastResult"/>
/// which is for debugging only) and thread-safe for concurrent use.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c as part of The Filter System feature.
/// </para>
/// </remarks>
public sealed class FilterQueryBuilder : IFilterQueryBuilder
{
    // =========================================================================
    // DEPENDENCIES
    // =========================================================================

    /// <summary>
    /// Logger for diagnostic messages.
    /// </summary>
    private readonly ILogger<FilterQueryBuilder> _logger;

    /// <summary>
    /// Validator for checking filter criteria before processing.
    /// </summary>
    private readonly IFilterValidator _validator;

    // =========================================================================
    // STATE (DEBUG ONLY)
    // =========================================================================

    /// <summary>
    /// Lock object for thread-safe access to <see cref="LastResult"/>.
    /// </summary>
    private readonly object _lastResultLock = new();

    /// <summary>
    /// Backing field for <see cref="LastResult"/>.
    /// </summary>
    private FilterQueryResult? _lastResult;

    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>
    /// Alias for the documents table in generated SQL.
    /// </summary>
    private const string DocumentsAlias = "d";

    /// <summary>
    /// Alias for the chunks table in generated SQL.
    /// </summary>
    private const string ChunksAlias = "c";

    /// <summary>
    /// CTE name for filtered documents.
    /// </summary>
    private const string FilteredDocsCte = "filtered_docs";

    // =========================================================================
    // CONSTRUCTOR
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterQueryBuilder"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="validator">Validator for filter criteria.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="logger"/> or <paramref name="validator"/> is null.
    /// </exception>
    public FilterQueryBuilder(
        ILogger<FilterQueryBuilder> logger,
        IFilterValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));

        _logger.LogDebug("FilterQueryBuilder initialized");
    }

    // =========================================================================
    // IFilterQueryBuilder IMPLEMENTATION
    // =========================================================================

    /// <inheritdoc/>
    public FilterQueryResult? LastResult
    {
        get
        {
            lock (_lastResultLock)
            {
                return _lastResult;
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The build process follows these steps:
    /// <list type="number">
    ///   <item><description>Validate the filter criteria.</description></item>
    ///   <item><description>Build CTE for document-level filters (path, extension, date).</description></item>
    ///   <item><description>Build WHERE clause for chunk-level filters (heading).</description></item>
    ///   <item><description>Generate summary for logging.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public FilterQueryResult Build(SearchFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogDebug(
            "Building filter query. HasCriteria: {HasCriteria}, CriteriaCount: {CriteriaCount}",
            filter.HasCriteria,
            filter.CriteriaCount);

        // LOGIC: If filter has no criteria, return empty result immediately.
        if (!filter.HasCriteria)
        {
            _logger.LogDebug("Filter has no criteria, returning empty result");
            return SetLastResult(FilterQueryResult.Empty);
        }

        // LOGIC: Validate the filter before processing.
        var validationErrors = _validator.Validate(filter);
        if (validationErrors.Count > 0)
        {
            foreach (var error in validationErrors)
            {
                _logger.LogWarning(
                    "Filter validation error: {Code} - {Message} ({Property})",
                    error.Code,
                    error.Message,
                    error.Property);
            }

            // LOGIC: Return empty result to avoid blocking search.
            // Caller can use TryBuild for detailed error handling.
            return SetLastResult(FilterQueryResult.Empty);
        }

        // Build the query components
        var parameters = new Dictionary<string, object>();
        var documentConditions = new List<string>();
        var chunkConditions = new List<string>();
        var summaryParts = new List<string>();

        // LOGIC: Build path pattern conditions
        BuildPathConditions(filter, parameters, documentConditions, summaryParts);

        // LOGIC: Build extension conditions
        BuildExtensionConditions(filter, parameters, documentConditions, summaryParts);

        // LOGIC: Build date range conditions
        BuildDateRangeConditions(filter, parameters, documentConditions, summaryParts);

        // LOGIC: Build heading conditions (chunk-level)
        BuildHeadingConditions(filter, chunkConditions, summaryParts);

        // LOGIC: Generate CTE if there are document-level conditions
        var cteClause = string.Empty;
        var joinClause = string.Empty;
        var whereClause = "1=1";

        if (documentConditions.Count > 0)
        {
            // Build CTE for document filtering
            var docWhere = string.Join(" AND ", documentConditions);
            cteClause = $@"WITH {FilteredDocsCte} AS (
    SELECT id FROM documents
    WHERE {docWhere}
)";

            // Add condition to filter chunks by document
            chunkConditions.Insert(0, $"{ChunksAlias}.document_id IN (SELECT id FROM {FilteredDocsCte})");
        }

        // LOGIC: Combine all chunk conditions
        if (chunkConditions.Count > 0)
        {
            whereClause = string.Join(" AND ", chunkConditions);
        }

        var result = new FilterQueryResult
        {
            WhereClause = whereClause,
            Parameters = parameters,
            CteClause = cteClause,
            JoinClause = joinClause,
            HasFilters = true,
            FilterCount = filter.CriteriaCount,
            Summary = summaryParts.Count > 0 ? string.Join(", ", summaryParts) : "No filters applied"
        };

        _logger.LogDebug(
            "Built filter query: {Summary}. Parameters: {ParameterCount}, CTE: {HasCte}",
            result.Summary,
            parameters.Count,
            !string.IsNullOrEmpty(cteClause));

        return SetLastResult(result);
    }

    /// <inheritdoc/>
    public string ConvertGlobToSql(string globPattern)
    {
        if (string.IsNullOrEmpty(globPattern))
        {
            return "%";
        }

        var result = new StringBuilder(globPattern.Length * 2);

        for (int i = 0; i < globPattern.Length; i++)
        {
            var c = globPattern[i];

            switch (c)
            {
                // LOGIC: Escape SQL wildcards that are literal in glob
                case '%':
                    result.Append(@"\%");
                    break;

                case '_':
                    result.Append(@"\_");
                    break;

                // LOGIC: Handle ** (any path including subdirectories)
                case '*' when i + 1 < globPattern.Length && globPattern[i + 1] == '*':
                    result.Append('%');
                    i++; // Skip the second *
                    break;

                // LOGIC: Handle * (any characters within segment)
                case '*':
                    result.Append('%');
                    break;

                // LOGIC: Handle ? (single character)
                case '?':
                    result.Append('_');
                    break;

                // LOGIC: Pass through all other characters
                default:
                    result.Append(c);
                    break;
            }
        }

        return result.ToString();
    }

    /// <inheritdoc/>
    public bool TryBuild(SearchFilter filter, out IReadOnlyList<FilterValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var validationErrors = _validator.Validate(filter);
        errors = validationErrors;

        if (validationErrors.Count > 0)
        {
            _logger.LogDebug(
                "TryBuild failed with {ErrorCount} validation errors",
                validationErrors.Count);
            return false;
        }

        // Build is guaranteed to succeed after validation
        Build(filter);
        return true;
    }

    // =========================================================================
    // PRIVATE METHODS - CONDITION BUILDERS
    // =========================================================================

    /// <summary>
    /// Builds SQL conditions for path pattern filtering.
    /// </summary>
    /// <param name="filter">The source filter.</param>
    /// <param name="parameters">Dictionary to add parameters to.</param>
    /// <param name="conditions">List to add conditions to.</param>
    /// <param name="summaryParts">List to add summary text to.</param>
    private void BuildPathConditions(
        SearchFilter filter,
        Dictionary<string, object> parameters,
        List<string> conditions,
        List<string> summaryParts)
    {
        if (filter.PathPatterns == null || filter.PathPatterns.Count == 0)
        {
            return;
        }

        var pathConditions = new List<string>();

        for (int i = 0; i < filter.PathPatterns.Count; i++)
        {
            var pattern = filter.PathPatterns[i];
            var sqlPattern = ConvertGlobToSql(pattern);
            var paramName = $"pathPattern{i}";

            parameters[paramName] = sqlPattern;
            pathConditions.Add($"file_path LIKE @{paramName}");

            _logger.LogTrace(
                "Added path pattern: {GlobPattern} -> {SqlPattern}",
                pattern,
                sqlPattern);
        }

        // LOGIC: Multiple path patterns are OR'd together
        if (pathConditions.Count > 1)
        {
            conditions.Add($"({string.Join(" OR ", pathConditions)})");
        }
        else
        {
            conditions.Add(pathConditions[0]);
        }

        var pathSummary = filter.PathPatterns.Count == 1
            ? filter.PathPatterns[0]
            : $"{filter.PathPatterns.Count} patterns";
        summaryParts.Add($"Path: {pathSummary}");
    }

    /// <summary>
    /// Builds SQL conditions for file extension filtering.
    /// </summary>
    /// <param name="filter">The source filter.</param>
    /// <param name="parameters">Dictionary to add parameters to.</param>
    /// <param name="conditions">List to add conditions to.</param>
    /// <param name="summaryParts">List to add summary text to.</param>
    private void BuildExtensionConditions(
        SearchFilter filter,
        Dictionary<string, object> parameters,
        List<string> conditions,
        List<string> summaryParts)
    {
        if (filter.FileExtensions == null || filter.FileExtensions.Count == 0)
        {
            return;
        }

        // LOGIC: Normalize extensions to lowercase for case-insensitive matching
        var normalizedExtensions = filter.FileExtensions
            .Select(e => e.TrimStart('.').ToLowerInvariant())
            .ToArray();

        parameters["extensions"] = normalizedExtensions;
        conditions.Add("LOWER(file_extension) = ANY(@extensions)");

        var extSummary = string.Join(", ", normalizedExtensions);
        summaryParts.Add($"Extensions: {extSummary}");

        _logger.LogTrace(
            "Added extension filter: {Extensions}",
            extSummary);
    }

    /// <summary>
    /// Builds SQL conditions for date range filtering.
    /// </summary>
    /// <param name="filter">The source filter.</param>
    /// <param name="parameters">Dictionary to add parameters to.</param>
    /// <param name="conditions">List to add conditions to.</param>
    /// <param name="summaryParts">List to add summary text to.</param>
    private void BuildDateRangeConditions(
        SearchFilter filter,
        Dictionary<string, object> parameters,
        List<string> conditions,
        List<string> summaryParts)
    {
        if (filter.ModifiedRange == null)
        {
            return;
        }

        var range = filter.ModifiedRange;

        if (range.Start.HasValue)
        {
            parameters["modifiedStart"] = range.Start.Value;
            conditions.Add("modified_at >= @modifiedStart");

            _logger.LogTrace(
                "Added date start filter: {Start}",
                range.Start.Value);
        }

        if (range.End.HasValue)
        {
            parameters["modifiedEnd"] = range.End.Value;
            conditions.Add("modified_at <= @modifiedEnd");

            _logger.LogTrace(
                "Added date end filter: {End}",
                range.End.Value);
        }

        // Build summary
        var dateSummary = BuildDateRangeSummary(range);
        summaryParts.Add($"Modified: {dateSummary}");
    }

    /// <summary>
    /// Builds SQL conditions for heading filtering.
    /// </summary>
    /// <param name="filter">The source filter.</param>
    /// <param name="conditions">List to add conditions to.</param>
    /// <param name="summaryParts">List to add summary text to.</param>
    private void BuildHeadingConditions(
        SearchFilter filter,
        List<string> conditions,
        List<string> summaryParts)
    {
        if (!filter.HasHeadings.HasValue || !filter.HasHeadings.Value)
        {
            return;
        }

        // LOGIC: Filter to chunks that have heading context
        conditions.Add($"{ChunksAlias}.heading IS NOT NULL");
        summaryParts.Add("Has headings");

        _logger.LogTrace("Added heading filter: heading IS NOT NULL");
    }

    // =========================================================================
    // PRIVATE METHODS - HELPERS
    // =========================================================================

    /// <summary>
    /// Builds a human-readable summary of a date range.
    /// </summary>
    /// <param name="range">The date range.</param>
    /// <returns>A summary string (e.g., "last 7 days", "Jan 2026").</returns>
    private static string BuildDateRangeSummary(DateRange range)
    {
        if (!range.HasBounds)
        {
            return "any time";
        }

        // LOGIC: Try to detect common patterns
        if (range.Start.HasValue && !range.End.HasValue)
        {
            var daysAgo = (DateTime.UtcNow - range.Start.Value).TotalDays;

            if (daysAgo <= 1.1)
            {
                return "last 24 hours";
            }
            if (daysAgo <= 7.1)
            {
                return "last 7 days";
            }
            if (daysAgo <= 30.1)
            {
                return "last 30 days";
            }

            return $"since {range.Start.Value:MMM d, yyyy}";
        }

        if (range.Start.HasValue && range.End.HasValue)
        {
            // Check if it's a full month
            if (range.Start.Value.Day == 1 &&
                range.End.Value.Month == range.Start.Value.Month &&
                range.End.Value.Year == range.Start.Value.Year)
            {
                return range.Start.Value.ToString("MMM yyyy");
            }

            return $"{range.Start.Value:MMM d} - {range.End.Value:MMM d, yyyy}";
        }

        if (range.End.HasValue)
        {
            return $"before {range.End.Value:MMM d, yyyy}";
        }

        return "custom range";
    }

    /// <summary>
    /// Sets the <see cref="LastResult"/> property thread-safely.
    /// </summary>
    /// <param name="result">The result to store.</param>
    /// <returns>The same result for chaining.</returns>
    private FilterQueryResult SetLastResult(FilterQueryResult result)
    {
        lock (_lastResultLock)
        {
            _lastResult = result;
        }
        return result;
    }
}
