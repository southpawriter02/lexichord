// =============================================================================
// File: ClaimObject.cs
// Project: Lexichord.Abstractions
// Description: The object of a claim, which can be an entity or a literal value.
// =============================================================================
// LOGIC: Represents the object (O) in a subject-predicate-object claim triple.
//   The object can be either an entity reference (linking to the knowledge graph)
//   or a literal value (string, number, boolean). Factory methods provide
//   convenient construction for each scenario.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: ClaimEntity (v0.5.6e), ClaimObjectType (v0.5.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// The object of a claim, which can be an entity reference or a literal value.
/// </summary>
/// <remarks>
/// <para>
/// In a claim triple (subject-predicate-object), the object can be:
/// </para>
/// <list type="bullet">
///   <item><b>Entity:</b> A reference to another entity in the knowledge graph.
///     Example: "GET /users ACCEPTS limit" where limit is an entity.</item>
///   <item><b>Literal:</b> A primitive value (string, int, bool, decimal).
///     Example: "Rate limit HAS_VALUE 100" where 100 is a literal.</item>
/// </list>
/// <para>
/// <b>Type Discrimination:</b> Use <see cref="Type"/> to determine whether
/// to access <see cref="Entity"/> or <see cref="LiteralValue"/>.
/// </para>
/// <para>
/// <b>Factory Methods:</b> Use the static factory methods to create objects:
/// <list type="bullet">
///   <item><see cref="FromEntity"/> — Entity reference objects.</item>
///   <item><see cref="FromString"/> — String literal objects.</item>
///   <item><see cref="FromInt"/> — Integer literal objects.</item>
///   <item><see cref="FromDecimal"/> — Decimal literal objects.</item>
///   <item><see cref="FromBool"/> — Boolean literal objects.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Entity object
/// var entityObject = ClaimObject.FromEntity(parameterEntity);
///
/// // Literal objects
/// var stringObject = ClaimObject.FromString("application/json");
/// var intObject = ClaimObject.FromInt(100, unit: "requests/minute");
/// var boolObject = ClaimObject.FromBool(true);
/// var decimalObject = ClaimObject.FromDecimal(99.9m, unit: "percent");
/// </code>
/// </example>
public record ClaimObject
{
    /// <summary>
    /// Type of the object (entity or literal).
    /// </summary>
    /// <value>Discriminates between entity references and literal values.</value>
    public ClaimObjectType Type { get; init; }

    /// <summary>
    /// Entity reference (when <see cref="Type"/> is <see cref="ClaimObjectType.Entity"/>).
    /// </summary>
    /// <value>
    /// The <see cref="ClaimEntity"/> reference, or null for literal objects.
    /// </value>
    public ClaimEntity? Entity { get; init; }

    /// <summary>
    /// Literal value (when <see cref="Type"/> is <see cref="ClaimObjectType.Literal"/>).
    /// </summary>
    /// <value>
    /// The string representation of the literal value, or null for entity objects.
    /// </value>
    /// <remarks>
    /// LOGIC: All literal values are stored as strings. The <see cref="LiteralType"/>
    /// property indicates the original type for parsing.
    /// </remarks>
    public string? LiteralValue { get; init; }

    /// <summary>
    /// Data type of the literal value.
    /// </summary>
    /// <value>
    /// The type name (e.g., "string", "int", "bool", "decimal").
    /// Null for entity objects.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to parse <see cref="LiteralValue"/> back to its original type.
    /// Standard type names follow C# primitive type naming.
    /// </remarks>
    public string? LiteralType { get; init; }

    /// <summary>
    /// Unit of measurement for numeric literals.
    /// </summary>
    /// <value>
    /// The unit string (e.g., "ms", "bytes", "requests/second"), or null.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides context for numeric values. Extracted from surrounding
    /// text during claim extraction.
    /// </remarks>
    public string? Unit { get; init; }

    /// <summary>
    /// Converts the object to a canonical string form.
    /// </summary>
    /// <returns>
    /// For entities: the normalized form. For literals: the value with optional unit.
    /// </returns>
    /// <remarks>
    /// LOGIC: Used for claim deduplication and comparison. The canonical form
    /// is consistent regardless of surface form variations.
    /// </remarks>
    public string ToCanonicalForm()
    {
        return Type switch
        {
            ClaimObjectType.Entity => Entity?.NormalizedForm ?? string.Empty,
            ClaimObjectType.Literal => Unit is not null
                ? $"{LiteralValue} {Unit}"
                : LiteralValue ?? string.Empty,
            _ => string.Empty
        };
    }

    // =========================================================================
    // Factory Methods
    // =========================================================================

    /// <summary>
    /// Creates an object from an entity reference.
    /// </summary>
    /// <param name="entity">The entity reference.</param>
    /// <returns>A new <see cref="ClaimObject"/> of type <see cref="ClaimObjectType.Entity"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="entity"/> is null.</exception>
    public static ClaimObject FromEntity(ClaimEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ClaimObject
        {
            Type = ClaimObjectType.Entity,
            Entity = entity
        };
    }

    /// <summary>
    /// Creates an object from a string literal.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new <see cref="ClaimObject"/> of type <see cref="ClaimObjectType.Literal"/>.</returns>
    public static ClaimObject FromString(string value)
    {
        return new ClaimObject
        {
            Type = ClaimObjectType.Literal,
            LiteralValue = value,
            LiteralType = "string"
        };
    }

    /// <summary>
    /// Creates an object from an integer literal.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <returns>A new <see cref="ClaimObject"/> of type <see cref="ClaimObjectType.Literal"/>.</returns>
    public static ClaimObject FromInt(int value, string? unit = null)
    {
        return new ClaimObject
        {
            Type = ClaimObjectType.Literal,
            LiteralValue = value.ToString(),
            LiteralType = "int",
            Unit = unit
        };
    }

    /// <summary>
    /// Creates an object from a decimal literal.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <returns>A new <see cref="ClaimObject"/> of type <see cref="ClaimObjectType.Literal"/>.</returns>
    public static ClaimObject FromDecimal(decimal value, string? unit = null)
    {
        return new ClaimObject
        {
            Type = ClaimObjectType.Literal,
            LiteralValue = value.ToString(),
            LiteralType = "decimal",
            Unit = unit
        };
    }

    /// <summary>
    /// Creates an object from a boolean literal.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A new <see cref="ClaimObject"/> of type <see cref="ClaimObjectType.Literal"/>.</returns>
    public static ClaimObject FromBool(bool value)
    {
        return new ClaimObject
        {
            Type = ClaimObjectType.Literal,
            LiteralValue = value.ToString().ToLowerInvariant(),
            LiteralType = "bool"
        };
    }
}
