// -----------------------------------------------------------------------
// <copyright file="ContextResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Templates.Providers;

/// <summary>
/// Represents the result of a context provider operation.
/// </summary>
/// <remarks>
/// <para>
/// This record encapsulates the outcome of an <see cref="IContextProvider.GetContextAsync"/>
/// call, including success state, context data, error information, and timing diagnostics.
/// </para>
/// <para>
/// <strong>Factory Methods:</strong>
/// Use the static factory methods to create instances with appropriate state:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Ok"/> - Provider succeeded with data.</description></item>
///   <item><description><see cref="Empty"/> - Provider succeeded but produced no data.</description></item>
///   <item><description><see cref="Failure"/> - Provider failed with an error message.</description></item>
///   <item><description><see cref="Timeout"/> - Provider exceeded its timeout limit.</description></item>
/// </list>
/// <para>
/// <strong>Immutability:</strong>
/// This is an immutable record. All properties are set at construction time and cannot be modified.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Success with data
/// var data = new Dictionary&lt;string, object&gt; { ["document_path"] = "/path/to/file.md" };
/// var result = ContextResult.Ok("Document", data, TimeSpan.FromMilliseconds(5));
///
/// // Empty result (valid but no data)
/// var empty = ContextResult.Empty("StyleRules", TimeSpan.FromMilliseconds(2));
///
/// // Failure result
/// var error = ContextResult.Failure("RAG", "Search service unavailable");
///
/// // Timeout result
/// var timeout = ContextResult.Timeout("RAG");
///
/// // Checking results
/// if (result.Success &amp;&amp; result.Data != null)
/// {
///     foreach (var kv in result.Data)
///     {
///         context[kv.Key] = kv.Value;
///     }
/// }
/// </code>
/// </example>
/// <param name="Success">Indicates whether the provider operation completed successfully.</param>
/// <param name="Data">The context variables produced, or <c>null</c> on failure.</param>
/// <param name="Error">An error message describing the failure, or <c>null</c> on success.</param>
/// <param name="Duration">The time taken to execute the provider operation.</param>
/// <param name="ProviderName">The name of the provider that produced this result.</param>
/// <seealso cref="IContextProvider"/>
public record ContextResult(
    bool Success,
    IDictionary<string, object>? Data,
    string? Error,
    TimeSpan Duration,
    string ProviderName)
{
    /// <summary>
    /// Creates a successful result with context data.
    /// </summary>
    /// <param name="providerName">The name of the provider producing this result.</param>
    /// <param name="data">The context variables to include in the result.</param>
    /// <param name="duration">The time taken to produce the context.</param>
    /// <returns>A new <see cref="ContextResult"/> indicating success with data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> or <paramref name="data"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// LOGIC: Use this factory when the provider successfully retrieved context variables.
    /// The data dictionary is stored as-is and will be merged into the final context.
    /// </remarks>
    public static ContextResult Ok(
        string providerName,
        IDictionary<string, object> data,
        TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        return new ContextResult(
            Success: true,
            Data: data,
            Error: null,
            Duration: duration,
            ProviderName: providerName);
    }

    /// <summary>
    /// Creates a successful result with no data.
    /// </summary>
    /// <param name="providerName">The name of the provider producing this result.</param>
    /// <param name="duration">The time taken to determine no data was available.</param>
    /// <returns>A new <see cref="ContextResult"/> indicating success with an empty data set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// LOGIC: Use this factory when the provider executed successfully but had no context
    /// to contribute. This is different from an error - it indicates the provider
    /// checked its sources and found nothing applicable.
    /// </remarks>
    /// <example>
    /// <code>
    /// // No style rules are enabled
    /// var enabledRules = styleSheet.GetEnabledRules();
    /// if (enabledRules.Count == 0)
    /// {
    ///     return ContextResult.Empty(ProviderName, elapsed);
    /// }
    /// </code>
    /// </example>
    public static ContextResult Empty(string providerName, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        return new ContextResult(
            Success: true,
            Data: new Dictionary<string, object>(),
            Error: null,
            Duration: duration,
            ProviderName: providerName);
    }

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    /// <param name="providerName">The name of the provider that failed.</param>
    /// <param name="errorMessage">A message describing the failure.</param>
    /// <returns>A new <see cref="ContextResult"/> indicating failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> or <paramref name="errorMessage"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> or <paramref name="errorMessage"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Use this factory when the provider encountered a recoverable error.
    /// The error message is logged as a warning but does not fail the entire
    /// context assembly operation.
    /// </para>
    /// <para>
    /// Common error scenarios:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>External service unavailable</description></item>
    ///   <item><description>File not found or access denied</description></item>
    ///   <item><description>Invalid configuration</description></item>
    /// </list>
    /// </remarks>
    public static ContextResult Failure(string providerName, string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));

        return new ContextResult(
            Success: false,
            Data: null,
            Error: errorMessage,
            Duration: TimeSpan.Zero,
            ProviderName: providerName);
    }

    /// <summary>
    /// Creates a failure result indicating a timeout.
    /// </summary>
    /// <param name="providerName">The name of the provider that timed out.</param>
    /// <returns>A new <see cref="ContextResult"/> indicating timeout failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// LOGIC: Use this factory when the provider exceeded its configured timeout.
    /// Timeouts are logged as warnings and the provider's contribution is skipped.
    /// This allows context assembly to complete with partial data rather than
    /// blocking on a slow provider.
    /// </remarks>
    public static ContextResult Timeout(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        return new ContextResult(
            Success: false,
            Data: null,
            Error: "Operation timed out",
            Duration: TimeSpan.Zero,
            ProviderName: providerName);
    }

    /// <summary>
    /// Gets the number of variables in the data dictionary.
    /// </summary>
    /// <value>
    /// The count of variables if <see cref="Data"/> is not null; otherwise, 0.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for logging and diagnostics to summarize provider output.
    /// </remarks>
    public int VariableCount => Data?.Count ?? 0;

    /// <summary>
    /// Gets a value indicating whether this result contains any data.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Success"/> is true and <see cref="Data"/> contains at least one entry;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: A shorthand for checking if this result will contribute anything
    /// to the final context dictionary.
    /// </remarks>
    public bool HasData => Success && Data is { Count: > 0 };

    /// <summary>
    /// Gets a value indicating whether this result represents a timeout.
    /// </summary>
    /// <value>
    /// <c>true</c> if this result was created via <see cref="Timeout"/>; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to distinguish timeout failures from other error types for logging purposes.
    /// </remarks>
    public bool IsTimeout => !Success && Error == "Operation timed out";
}
