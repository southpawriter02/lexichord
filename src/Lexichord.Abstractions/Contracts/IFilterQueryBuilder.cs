// =============================================================================
// File: IFilterQueryBuilder.cs
// Project: Lexichord.Abstractions
// Description: Interface and records for filter query building (v0.5.5c).
// =============================================================================
// LOGIC: Defines the contract for translating SearchFilter criteria into SQL:
//   - IFilterQueryBuilder: Interface for building SQL from SearchFilter
//   - FilterQueryResult: Contains SQL clauses and parameters for execution
//   - GlobToSqlConverter: Internal utility for glob-to-LIKE conversion
//   The builder generates parameterized SQL to prevent injection attacks.
//   Queries use CTE pattern to preserve HNSW index performance.
// =============================================================================
// VERSION: v0.5.5c (Filter Query Builder)
// DEPENDENCIES: v0.5.5a (SearchFilter, DateRange, IFilterValidator)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Result of building a filter query from <see cref="SearchFilter"/> criteria.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FilterQueryResult"/> encapsulates the SQL components needed to
/// apply filter criteria to search queries. The result includes:
/// <list type="bullet">
///   <item><description><see cref="WhereClause"/>: SQL WHERE conditions for filtering.</description></item>
///   <item><description><see cref="Parameters"/>: Named parameters for safe SQL execution.</description></item>
///   <item><description><see cref="JoinClause"/>: Optional JOIN for cross-table filtering.</description></item>
///   <item><description><see cref="CteClause"/>: Optional CTE for efficient filtered lookups.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Query Strategy:</b> Filters are applied via CTE to ensure the HNSW vector
/// index is still used for similarity search. The CTE pre-filters documents,
/// and the main query performs vector search only on matching document chunks.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c as part of The Filter System feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Build filter query
/// var filter = SearchFilter.ForPath("docs/**") with
/// {
///     FileExtensions = new[] { "md" },
///     ModifiedRange = DateRange.LastDays(7)
/// };
///
/// var result = queryBuilder.Build(filter);
///
/// // Use in search query
/// var sql = $@"
///     {result.CteClause}
///     SELECT c.*, c.embedding &lt;=&gt; @vector AS distance
///     FROM chunks c
///     {result.JoinClause}
///     WHERE {result.WhereClause}
///     ORDER BY c.embedding &lt;=&gt; @vector
///     LIMIT @topK";
///
/// var chunks = await connection.QueryAsync(sql, result.Parameters);
/// </code>
/// </example>
public record FilterQueryResult
{
    /// <summary>
    /// SQL WHERE clause conditions (without the WHERE keyword).
    /// </summary>
    /// <value>
    /// A string containing SQL conditions to be placed after WHERE.
    /// Returns "1=1" (always true) when no filters are applied, allowing
    /// seamless AND concatenation.
    /// </value>
    /// <remarks>
    /// <para>
    /// The clause is designed for easy composition:
    /// <code>
    /// WHERE {result.WhereClause} AND c.embedding &lt;=&gt; @vector &lt; @threshold
    /// </code>
    /// </para>
    /// <para>
    /// When <see cref="HasFilters"/> is <c>false</c>, this returns "1=1" to
    /// maintain valid SQL syntax without filtering.
    /// </para>
    /// </remarks>
    public required string WhereClause { get; init; }

    /// <summary>
    /// Named SQL parameters for the WHERE clause.
    /// </summary>
    /// <value>
    /// A dictionary of parameter names (without @) to their values.
    /// All filter values are parameterized to prevent SQL injection.
    /// </value>
    /// <remarks>
    /// <para>
    /// Parameter naming convention:
    /// <list type="bullet">
    ///   <item><description><c>pathPattern0</c>, <c>pathPattern1</c>: Path LIKE patterns.</description></item>
    ///   <item><description><c>extensions</c>: Array of file extensions.</description></item>
    ///   <item><description><c>modifiedStart</c>: Date range lower bound.</description></item>
    ///   <item><description><c>modifiedEnd</c>: Date range upper bound.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public required IReadOnlyDictionary<string, object> Parameters { get; init; }

    /// <summary>
    /// Optional SQL JOIN clause for cross-table filtering.
    /// </summary>
    /// <value>
    /// A JOIN clause string (e.g., "INNER JOIN documents d ON c.document_id = d.id"),
    /// or an empty string if no join is needed.
    /// </value>
    /// <remarks>
    /// <para>
    /// When filtering by document-level properties (path, extension, modified date),
    /// a JOIN to the documents table is required. The join uses table alias <c>d</c>
    /// for documents and expects <c>c</c> for chunks.
    /// </para>
    /// </remarks>
    public string JoinClause { get; init; } = string.Empty;

    /// <summary>
    /// Optional SQL CTE (Common Table Expression) for efficient filtering.
    /// </summary>
    /// <value>
    /// A WITH clause string (e.g., "WITH filtered_docs AS (...)"),
    /// or an empty string if no CTE is needed.
    /// </value>
    /// <remarks>
    /// <para>
    /// The CTE pattern is used to pre-filter documents before vector search,
    /// ensuring the HNSW index is leveraged efficiently. The CTE produces
    /// a list of matching document IDs that the main query joins against.
    /// </para>
    /// <para>
    /// <b>Performance Note:</b> Using a CTE avoids inline subqueries that
    /// could cause the query planner to skip the HNSW index.
    /// </para>
    /// </remarks>
    public string CteClause { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether any filter criteria were applied.
    /// </summary>
    /// <value>
    /// <c>true</c> if the source <see cref="SearchFilter"/> had criteria;
    /// <c>false</c> if the filter was empty.
    /// </value>
    public bool HasFilters { get; init; }

    /// <summary>
    /// Gets the count of active filter criteria.
    /// </summary>
    /// <value>
    /// The number of distinct filter types applied (0-5).
    /// </value>
    public int FilterCount { get; init; }

    /// <summary>
    /// Summary of applied filters for logging and debugging.
    /// </summary>
    /// <value>
    /// A human-readable string describing the applied filters,
    /// e.g., "Path: docs/**, Extensions: md, txt, Modified: last 7 days".
    /// </value>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Returns an empty result representing no filtering.
    /// </summary>
    /// <value>
    /// A <see cref="FilterQueryResult"/> with "1=1" WHERE clause and no parameters.
    /// </value>
    public static FilterQueryResult Empty => new()
    {
        WhereClause = "1=1",
        Parameters = new Dictionary<string, object>(),
        HasFilters = false,
        FilterCount = 0,
        Summary = "No filters applied"
    };
}

/// <summary>
/// Builds SQL query components from <see cref="SearchFilter"/> criteria.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IFilterQueryBuilder"/> transforms user-defined filter criteria into
/// parameterized SQL that can be safely executed against the database. The builder
/// handles:
/// <list type="bullet">
///   <item><description>Glob pattern conversion to SQL LIKE expressions.</description></item>
///   <item><description>Extension array conversion to ANY() clauses.</description></item>
///   <item><description>Date range conversion to BETWEEN conditions.</description></item>
///   <item><description>Heading filter conversion to IS NOT NULL checks.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Security:</b> All filter values are parameterized. The builder validates
/// filters via <see cref="IFilterValidator"/> before processing. Invalid filters
/// result in an empty query (no filtering) with logged warnings.
/// </para>
/// <para>
/// <b>Query Strategy:</b> The builder generates CTEs for efficient filtered search:
/// <code>
/// WITH filtered_docs AS (
///     SELECT id FROM documents
///     WHERE file_path LIKE @pattern
///       AND file_extension = ANY(@extensions)
///       AND modified_at >= @start
/// )
/// SELECT c.*, c.embedding &lt;=&gt; @vector AS distance
/// FROM chunks c
/// WHERE c.document_id IN (SELECT id FROM filtered_docs)
/// ORDER BY c.embedding &lt;=&gt; @vector
/// LIMIT @topK;
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be stateless and thread-safe,
/// suitable for singleton registration in dependency injection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c as part of The Filter System feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Inject the builder
/// public class SearchService
/// {
///     private readonly IFilterQueryBuilder _queryBuilder;
///     private readonly IDbConnection _connection;
///
///     public async Task&lt;IEnumerable&lt;Chunk&gt;&gt; SearchAsync(
///         string query, SearchFilter filter, int topK)
///     {
///         // Build the filter query
///         var filterResult = _queryBuilder.Build(filter);
///
///         // Combine with vector search
///         var sql = $@"
///             {filterResult.CteClause}
///             SELECT c.* FROM chunks c
///             {filterResult.JoinClause}
///             WHERE {filterResult.WhereClause}
///             ORDER BY c.embedding &lt;=&gt; @vector
///             LIMIT @topK";
///
///         // Merge parameters
///         var parameters = new DynamicParameters(filterResult.Parameters);
///         parameters.Add("vector", embedding);
///         parameters.Add("topK", topK);
///
///         return await _connection.QueryAsync&lt;Chunk&gt;(sql, parameters);
///     }
/// }
/// </code>
/// </example>
public interface IFilterQueryBuilder
{
    /// <summary>
    /// Builds SQL query components from filter criteria.
    /// </summary>
    /// <param name="filter">
    /// The filter criteria to convert. Cannot be null.
    /// An empty filter (no criteria) returns <see cref="FilterQueryResult.Empty"/>.
    /// </param>
    /// <returns>
    /// A <see cref="FilterQueryResult"/> containing the SQL components.
    /// Never returns null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The builder validates the filter before processing. If validation fails,
    /// the method logs warnings and returns an empty result to avoid blocking
    /// search operations.
    /// </para>
    /// <para>
    /// <b>Empty Filter Handling:</b> When the filter has no criteria
    /// (<see cref="SearchFilter.HasCriteria"/> is <c>false</c>), the method
    /// returns <see cref="FilterQueryResult.Empty"/> which applies no filtering.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var filter = SearchFilter.ForPath("docs/**");
    /// var result = builder.Build(filter);
    ///
    /// Console.WriteLine($"WHERE: {result.WhereClause}");
    /// Console.WriteLine($"Filters: {result.Summary}");
    /// </code>
    /// </example>
    FilterQueryResult Build(SearchFilter filter);

    /// <summary>
    /// Converts a glob pattern to a SQL LIKE pattern.
    /// </summary>
    /// <param name="globPattern">
    /// The glob pattern (e.g., "docs/**/*.md").
    /// </param>
    /// <returns>
    /// A SQL LIKE-compatible pattern (e.g., "docs/%/%.md").
    /// </returns>
    /// <remarks>
    /// <para>
    /// Glob-to-SQL conversion rules:
    /// <list type="bullet">
    ///   <item><description><c>**</c> → <c>%</c> (matches any path including subdirectories).</description></item>
    ///   <item><description><c>*</c> → <c>%</c> (matches any characters within a segment).</description></item>
    ///   <item><description><c>?</c> → <c>_</c> (matches a single character).</description></item>
    ///   <item><description><c>%</c> → <c>\%</c> (escaped literal percent).</description></item>
    ///   <item><description><c>_</c> → <c>\_</c> (escaped literal underscore).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> This method does not validate the pattern. Use
    /// <see cref="IFilterValidator.ValidatePattern"/> for validation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = builder.ConvertGlobToSql("docs/**/*.md");
    /// // Returns: "docs/%/%.md"
    ///
    /// var escaped = builder.ConvertGlobToSql("file_with_underscore.txt");
    /// // Returns: "file\_with\_underscore.txt"
    /// </code>
    /// </example>
    string ConvertGlobToSql(string globPattern);

    /// <summary>
    /// Validates and builds a filter, returning validation errors if any.
    /// </summary>
    /// <param name="filter">The filter criteria to validate and build.</param>
    /// <param name="errors">
    /// When this method returns, contains validation errors if the filter is invalid;
    /// otherwise, an empty collection.
    /// </param>
    /// <returns>
    /// <c>true</c> if the filter is valid and the result was built successfully;
    /// <c>false</c> if validation failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method when you need to report validation errors to users
    /// rather than silently falling back to no filtering.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var filter = new SearchFilter(PathPatterns: new[] { "../secret" });
    ///
    /// if (!builder.TryBuild(filter, out var errors))
    /// {
    ///     foreach (var error in errors)
    ///     {
    ///         Console.WriteLine($"Error: {error.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    bool TryBuild(SearchFilter filter, out IReadOnlyList<FilterValidationError> errors);

    /// <summary>
    /// Gets the last successfully built <see cref="FilterQueryResult"/>.
    /// </summary>
    /// <value>
    /// The result from the most recent successful <see cref="Build"/> call,
    /// or <see cref="FilterQueryResult.Empty"/> if no build has occurred.
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>Note:</b> This property is primarily for debugging and testing.
    /// Production code should use the return value of <see cref="Build"/> directly.
    /// </para>
    /// </remarks>
    FilterQueryResult? LastResult { get; }
}
