// =============================================================================
// File: GraphParameterExtensions.cs
// Project: Lexichord.Modules.Knowledge
// Description: Extension methods for converting objects to graph query parameters.
// =============================================================================
// LOGIC: The Neo4j driver expects query parameters as Dictionary<string, object?>.
//   This extension converts anonymous objects (e.g., new { id, name }) to the
//   required dictionary format using reflection. Also handles the case where
//   the parameter is already a dictionary.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: System.Reflection
// =============================================================================

using System.Reflection;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Extension methods for converting parameter objects to Neo4j-compatible dictionaries.
/// </summary>
/// <remarks>
/// LOGIC: Neo4j.Driver expects parameters as <c>Dictionary&lt;string, object?&gt;</c>.
/// This extension handles conversion from anonymous objects, strongly-typed objects,
/// and pre-built dictionaries to ensure consistent parameter passing.
/// </remarks>
internal static class GraphParameterExtensions
{
    /// <summary>
    /// Converts an object to a dictionary of parameters for Cypher queries.
    /// </summary>
    /// <param name="parameters">
    /// The parameter object. Can be:
    /// <list type="bullet">
    ///   <item><c>null</c> — returns null.</item>
    ///   <item><c>Dictionary&lt;string, object?&gt;</c> — returned as-is.</item>
    ///   <item>Anonymous object or POCO — properties extracted via reflection.</item>
    /// </list>
    /// </param>
    /// <returns>
    /// A dictionary mapping parameter names to values, or <c>null</c> if
    /// <paramref name="parameters"/> is null.
    /// </returns>
    /// <remarks>
    /// LOGIC: Reflection-based conversion for anonymous objects. The performance
    /// impact is minimal because:
    /// 1. Parameter objects are typically small (2-5 properties).
    /// 2. Query execution time dominates over parameter conversion.
    /// 3. PropertyInfo[] is cached per type by the runtime.
    /// </remarks>
    public static Dictionary<string, object?>? ToDictionary(this object? parameters)
    {
        // LOGIC: Null parameters are valid (many Cypher queries have no parameters).
        if (parameters is null)
            return null;

        // LOGIC: If already a dictionary, return directly (no conversion needed).
        if (parameters is Dictionary<string, object?> dict)
            return dict;

        // LOGIC: If it's a non-nullable dictionary variant, convert the values.
        if (parameters is IDictionary<string, object> typedDict)
            return typedDict.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

        // LOGIC: Convert anonymous object or POCO properties to dictionary via reflection.
        // This handles the common case: new { id = "123", name = "Widget" }
        var properties = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new Dictionary<string, object?>(properties.Length);

        foreach (var property in properties)
        {
            result[property.Name] = property.GetValue(parameters);
        }

        return result;
    }
}
