// -----------------------------------------------------------------------
// <copyright file="ContextGatheringRequest.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Context;

/// <summary>
/// Encapsulates all information needed to gather context for an agent request.
/// Passed to each <see cref="IContextStrategy"/> during context assembly.
/// </summary>
/// <remarks>
/// <para>
/// This request object provides strategies with:
/// </para>
/// <list type="bullet">
///   <item><description><b>Document Information:</b> Path and cursor position</description></item>
///   <item><description><b>Selection:</b> Currently selected text</description></item>
///   <item><description><b>Agent Identity:</b> Which agent is requesting context</description></item>
///   <item><description><b>Customization Hints:</b> Optional strategy-specific parameters</description></item>
/// </list>
/// <para>
/// <strong>Design Pattern:</strong>
/// The request uses an immutable record pattern with computed properties for
/// convenience checks (<see cref="HasDocument"/>, <see cref="HasSelection"/>, <see cref="HasCursor"/>).
/// Hints provide extensibility for strategy-specific parameters without polluting
/// the core interface with every possible parameter.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <param name="DocumentPath">Path to the currently active document, if any.</param>
/// <param name="CursorPosition">Current cursor position in the document (character offset from start).</param>
/// <param name="SelectedText">Currently selected text in the document, if any.</param>
/// <param name="AgentId">Identifier of the agent requesting context.</param>
/// <param name="Hints">Optional key-value hints for strategy customization.</param>
/// <example>
/// <code>
/// // Creating a request with document and selection
/// var request = new ContextGatheringRequest(
///     DocumentPath: "/docs/chapter1.md",
///     CursorPosition: 1234,
///     SelectedText: "The old man and the sea",
///     AgentId: "editor",
///     Hints: new Dictionary&lt;string, object&gt;
///     {
///         ["IncludeHeadings"] = true,
///         ["MaxSearchResults"] = 5
///     });
///
/// // Checking prerequisites in a strategy
/// if (!request.HasDocument) return null;
///
/// // Accessing hints with type safety
/// var maxResults = request.GetHint("MaxSearchResults", 3);
/// </code>
/// </example>
public record ContextGatheringRequest(
    string? DocumentPath,
    int? CursorPosition,
    string? SelectedText,
    string AgentId,
    IReadOnlyDictionary<string, object>? Hints)
{
    /// <summary>
    /// Creates a minimal request with only the agent ID.
    /// Useful for testing or when no document context is available.
    /// </summary>
    /// <param name="agentId">Identifier of the agent requesting context.</param>
    /// <returns>A new <see cref="ContextGatheringRequest"/> with all optional fields null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agentId"/> is null.</exception>
    /// <remarks>
    /// LOGIC: Factory method for the common case of creating a minimal request
    /// without document or editor state. Used primarily in testing scenarios
    /// or when an agent needs context but has no active document.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ContextGatheringRequest.Empty("test-agent");
    /// // request.DocumentPath is null
    /// // request.SelectedText is null
    /// // request.Hints is null
    /// </code>
    /// </example>
    public static ContextGatheringRequest Empty(string agentId)
    {
        ArgumentNullException.ThrowIfNull(agentId);
        return new(null, null, null, agentId, null);
    }

    /// <summary>
    /// Gets a hint value with type checking, returning default if not found.
    /// </summary>
    /// <typeparam name="T">Expected type of the hint value.</typeparam>
    /// <param name="key">Hint key to look up.</param>
    /// <param name="defaultValue">Value to return if hint not found or wrong type.</param>
    /// <returns>The hint value if found and correct type; otherwise, <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Type-safe hint access with graceful fallback. If the hint exists
    /// but is the wrong type, returns default rather than throwing. This allows
    /// strategies to specify default behavior without fragile type checking.
    /// </para>
    /// <para>
    /// <strong>Type Safety:</strong>
    /// The generic parameter <typeparamref name="T"/> ensures compile-time type
    /// checking at the call site. Runtime type checking via <c>is T</c> provides
    /// additional safety against configuration errors.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Accessing typed hints with defaults
    /// var maxResults = request.GetHint("MaxSearchResults", 3);
    /// var includeHeadings = request.GetHint("IncludeHeadings", false);
    /// var contextRadius = request.GetHint("ContextRadius", 500);
    ///
    /// // Wrong type returns default (no exception)
    /// var hints = new Dictionary&lt;string, object&gt; { ["MaxResults"] = "not-a-number" };
    /// var req = new ContextGatheringRequest(null, null, null, "agent", hints);
    /// var max = req.GetHint("MaxResults", 10); // Returns 10, not "not-a-number"
    /// </code>
    /// </example>
    public T GetHint<T>(string key, T defaultValue = default!)
    {
        // LOGIC: Return default if no hints dictionary exists
        if (Hints is null) return defaultValue;

        // LOGIC: Return default if key not found
        if (!Hints.TryGetValue(key, out var value)) return defaultValue;

        // LOGIC: Return typed value if correct type, otherwise default
        // This provides graceful degradation if hints are misconfigured
        return value is T typed ? typed : defaultValue;
    }

    /// <summary>
    /// Gets a value indicating whether a document path is available.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="DocumentPath"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Convenience property for strategies that require document context.
    /// Checks both null and empty string to handle edge cases where an empty
    /// path might be passed.
    /// </remarks>
    public bool HasDocument => !string.IsNullOrEmpty(DocumentPath);

    /// <summary>
    /// Gets a value indicating whether text is selected.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="SelectedText"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Convenience property for strategies that need selected text.
    /// Uses <see cref="string.IsNullOrEmpty"/> rather than just null check to
    /// avoid processing empty selections.
    /// </remarks>
    public bool HasSelection => !string.IsNullOrEmpty(SelectedText);

    /// <summary>
    /// Gets a value indicating whether cursor position is known.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="CursorPosition"/> has a value; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Convenience property for strategies that need cursor position
    /// (e.g., cursor context strategy). Uses <see cref="Nullable{T}.HasValue"/>
    /// to distinguish between "no position" (null) and "position zero" (0).
    /// </remarks>
    public bool HasCursor => CursorPosition.HasValue;
}
