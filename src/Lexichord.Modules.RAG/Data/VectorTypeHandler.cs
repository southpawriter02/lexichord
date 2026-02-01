// =============================================================================
// File: VectorTypeHandler.cs
// Project: Lexichord.Modules.RAG
// Description: Custom Dapper type handler for pgvector's VECTOR type.
// =============================================================================
// LOGIC: Enables transparent mapping between .NET float[] and PostgreSQL VECTOR.
//   - SetValue: Converts float[] to Pgvector.Vector for INSERT/UPDATE parameters.
//   - Parse: Converts returned Vector data back to float[].
//   - Handles null values bidirectionally (null -> DBNull, DBNull/null -> null).
// =============================================================================

using System.Data;
using Dapper;
using Pgvector;

namespace Lexichord.Modules.RAG.Data;

/// <summary>
/// Dapper type handler for mapping between .NET <see cref="float"/>[] arrays 
/// and PostgreSQL pgvector's VECTOR type.
/// </summary>
/// <remarks>
/// <para>
/// This handler must be registered with Dapper's <see cref="SqlMapper"/> before
/// any repository operations involving vector columns. Registration is performed
/// in <see cref="RAGModule.RegisterServices"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This handler is stateless and thread-safe. It can be
/// registered as a singleton with Dapper.
/// </para>
/// <para>
/// <b>Embedding Dimensions:</b> OpenAI's text-embedding-3-small produces 1536-dimensional
/// vectors. The handler does not validate dimensionality - that responsibility lies
/// with the database schema constraint.
/// </para>
/// </remarks>
public sealed class VectorTypeHandler : SqlMapper.TypeHandler<float[]>
{
    /// <summary>
    /// Sets the database parameter value for a vector embedding.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The float array to store, or null.</param>
    /// <remarks>
    /// Converts the float array to a <see cref="Vector"/> instance for proper
    /// pgvector serialization. Null values are converted to <see cref="DBNull.Value"/>.
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, float[]? value)
    {
        // LOGIC: Pgvector's Vector type handles serialization to the wire format.
        // DBNull.Value is required for NULL database values.
        parameter.Value = value is null ? DBNull.Value : new Vector(value);
    }

    /// <summary>
    /// Parses a database value into a float array.
    /// </summary>
    /// <param name="value">The database value to parse.</param>
    /// <returns>
    /// The parsed float array, or null if the database value is null/DBNull.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when <paramref name="value"/> is not a <see cref="Vector"/>, 
    /// <see cref="float"/>[], null, or <see cref="DBNull"/>.
    /// </exception>
    /// <remarks>
    /// Handles multiple input formats:
    /// <list type="bullet">
    ///   <item><description>null or DBNull: Returns null</description></item>
    ///   <item><description>Vector: Extracts the underlying float array</description></item>
    ///   <item><description>float[]: Returns as-is (for compatibility)</description></item>
    /// </list>
    /// </remarks>
    public override float[]? Parse(object value)
    {
        // LOGIC: Pattern matching handles all expected types from Npgsql + pgvector.
        // The float[] case provides compatibility with raw array returns.
        return value switch
        {
            null or DBNull => null,
            Vector vector => vector.ToArray(),
            float[] array => array,
            _ => throw new InvalidCastException(
                $"Cannot convert {value.GetType().FullName} to float[]. " +
                $"Expected Vector, float[], null, or DBNull.")
        };
    }
}
