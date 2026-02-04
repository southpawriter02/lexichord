// =============================================================================
// File: ClaimPredicate.cs
// Project: Lexichord.Abstractions
// Description: Standard predicate constants for technical documentation claims.
// =============================================================================
// LOGIC: Defines a canonical set of predicates for claims extracted from
//   technical documentation. Predicates are standardized strings that describe
//   the relationship between a claim's subject and object. Includes inverse
//   mapping for bidirectional navigation.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure constants)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Standard predicates for claims in technical documentation.
/// </summary>
/// <remarks>
/// <para>
/// This static class defines a canonical set of predicate strings for claims.
/// Using standardized predicates enables:
/// </para>
/// <list type="bullet">
///   <item>Consistent claim structure across documents.</item>
///   <item>Efficient querying by predicate type.</item>
///   <item>Inverse relationship navigation.</item>
///   <item>Pattern-based extraction matching.</item>
/// </list>
/// <para>
/// <b>Predicate Categories:</b>
/// <list type="bullet">
///   <item><b>Structural:</b> CONTAINS, BELONGS_TO, REFERENCES — hierarchical relationships.</item>
///   <item><b>Functional:</b> ACCEPTS, RETURNS, REQUIRES — API behavior.</item>
///   <item><b>Descriptive:</b> HAS_PROPERTY, HAS_DEFAULT, HAS_TYPE — attributes.</item>
///   <item><b>Lifecycle:</b> IS_DEPRECATED, REPLACED_BY — versioning.</item>
/// </list>
/// </para>
/// <para>
/// <b>Inverse Relationships:</b> Many predicates have inverses. For example,
/// if A CONTAINS B, then B BELONGS_TO A. Use <see cref="GetInverse"/> to
/// retrieve the inverse predicate.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public static class ClaimPredicate
{
    // =========================================================================
    // Structural Predicates
    // =========================================================================

    /// <summary>
    /// The subject contains the object as a child element.
    /// </summary>
    /// <remarks>Inverse: <see cref="BELONGS_TO"/>.</remarks>
    public const string CONTAINS = "CONTAINS";

    /// <summary>
    /// The subject belongs to (is contained by) the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="CONTAINS"/>.</remarks>
    public const string BELONGS_TO = "BELONGS_TO";

    /// <summary>
    /// The subject references the object (non-hierarchical link).
    /// </summary>
    /// <remarks>Inverse: <see cref="REFERENCED_BY"/>.</remarks>
    public const string REFERENCES = "REFERENCES";

    /// <summary>
    /// The subject is referenced by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="REFERENCES"/>.</remarks>
    public const string REFERENCED_BY = "REFERENCED_BY";

    // =========================================================================
    // Functional Predicates (API Behavior)
    // =========================================================================

    /// <summary>
    /// The subject accepts the object as input (parameter, header, body).
    /// </summary>
    /// <remarks>Inverse: <see cref="ACCEPTED_BY"/>.</remarks>
    public const string ACCEPTS = "ACCEPTS";

    /// <summary>
    /// The subject is accepted by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="ACCEPTS"/>.</remarks>
    public const string ACCEPTED_BY = "ACCEPTED_BY";

    /// <summary>
    /// The subject returns the object as output.
    /// </summary>
    /// <remarks>Inverse: <see cref="RETURNED_BY"/>.</remarks>
    public const string RETURNS = "RETURNS";

    /// <summary>
    /// The subject is returned by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="RETURNS"/>.</remarks>
    public const string RETURNED_BY = "RETURNED_BY";

    /// <summary>
    /// The subject requires the object (dependency).
    /// </summary>
    /// <remarks>Inverse: <see cref="REQUIRED_BY"/>.</remarks>
    public const string REQUIRES = "REQUIRES";

    /// <summary>
    /// The subject is required by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="REQUIRES"/>.</remarks>
    public const string REQUIRED_BY = "REQUIRED_BY";

    /// <summary>
    /// The subject calls/invokes the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="CALLED_BY"/>.</remarks>
    public const string CALLS = "CALLS";

    /// <summary>
    /// The subject is called by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="CALLS"/>.</remarks>
    public const string CALLED_BY = "CALLED_BY";

    /// <summary>
    /// The subject throws the object (exception).
    /// </summary>
    /// <remarks>Inverse: <see cref="THROWN_BY"/>.</remarks>
    public const string THROWS = "THROWS";

    /// <summary>
    /// The subject is thrown by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="THROWS"/>.</remarks>
    public const string THROWN_BY = "THROWN_BY";

    // =========================================================================
    // Descriptive Predicates (Attributes)
    // =========================================================================

    /// <summary>
    /// The subject has the object as a property/attribute.
    /// </summary>
    public const string HAS_PROPERTY = "HAS_PROPERTY";

    /// <summary>
    /// The subject has the object as its default value.
    /// </summary>
    public const string HAS_DEFAULT = "HAS_DEFAULT";

    /// <summary>
    /// The subject has the object as its data type.
    /// </summary>
    public const string HAS_TYPE = "HAS_TYPE";

    /// <summary>
    /// The subject has the object as a constraint (min, max, pattern).
    /// </summary>
    public const string HAS_CONSTRAINT = "HAS_CONSTRAINT";

    /// <summary>
    /// The subject has the object as a description/documentation.
    /// </summary>
    public const string HAS_DESCRIPTION = "HAS_DESCRIPTION";

    /// <summary>
    /// The subject has the object as an example value.
    /// </summary>
    public const string HAS_EXAMPLE = "HAS_EXAMPLE";

    // =========================================================================
    // Lifecycle Predicates (Versioning)
    // =========================================================================

    /// <summary>
    /// The subject is deprecated (object is deprecation message or version).
    /// </summary>
    public const string IS_DEPRECATED = "IS_DEPRECATED";

    /// <summary>
    /// The subject is replaced by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="REPLACES"/>.</remarks>
    public const string REPLACED_BY = "REPLACED_BY";

    /// <summary>
    /// The subject replaces the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="REPLACED_BY"/>.</remarks>
    public const string REPLACES = "REPLACES";

    /// <summary>
    /// The subject is an alias for the object.
    /// </summary>
    public const string ALIAS_OF = "ALIAS_OF";

    /// <summary>
    /// The subject extends/inherits from the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="EXTENDED_BY"/>.</remarks>
    public const string EXTENDS = "EXTENDS";

    /// <summary>
    /// The subject is extended by the object.
    /// </summary>
    /// <remarks>Inverse: <see cref="EXTENDS"/>.</remarks>
    public const string EXTENDED_BY = "EXTENDED_BY";

    // =========================================================================
    // Collections and Lookup
    // =========================================================================

    /// <summary>
    /// All standard predicates as a readonly list.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        CONTAINS, BELONGS_TO, REFERENCES, REFERENCED_BY,
        ACCEPTS, ACCEPTED_BY, RETURNS, RETURNED_BY,
        REQUIRES, REQUIRED_BY, CALLS, CALLED_BY,
        THROWS, THROWN_BY,
        HAS_PROPERTY, HAS_DEFAULT, HAS_TYPE, HAS_CONSTRAINT,
        HAS_DESCRIPTION, HAS_EXAMPLE,
        IS_DEPRECATED, REPLACED_BY, REPLACES, ALIAS_OF,
        EXTENDS, EXTENDED_BY
    };

    private static readonly Dictionary<string, string> InverseMap = new(StringComparer.Ordinal)
    {
        [CONTAINS] = BELONGS_TO,
        [BELONGS_TO] = CONTAINS,
        [REFERENCES] = REFERENCED_BY,
        [REFERENCED_BY] = REFERENCES,
        [ACCEPTS] = ACCEPTED_BY,
        [ACCEPTED_BY] = ACCEPTS,
        [RETURNS] = RETURNED_BY,
        [RETURNED_BY] = RETURNS,
        [REQUIRES] = REQUIRED_BY,
        [REQUIRED_BY] = REQUIRES,
        [CALLS] = CALLED_BY,
        [CALLED_BY] = CALLS,
        [THROWS] = THROWN_BY,
        [THROWN_BY] = THROWS,
        [REPLACED_BY] = REPLACES,
        [REPLACES] = REPLACED_BY,
        [EXTENDS] = EXTENDED_BY,
        [EXTENDED_BY] = EXTENDS
    };

    /// <summary>
    /// Gets the inverse predicate for a given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to find the inverse of.</param>
    /// <returns>
    /// The inverse predicate if one exists, or null if the predicate
    /// has no defined inverse (e.g., HAS_PROPERTY).
    /// </returns>
    /// <remarks>
    /// LOGIC: Enables bidirectional relationship navigation. If claim
    /// "A CONTAINS B" exists, we can infer "B BELONGS_TO A".
    /// </remarks>
    /// <example>
    /// <code>
    /// var inverse = ClaimPredicate.GetInverse(ClaimPredicate.CONTAINS);
    /// // Returns "BELONGS_TO"
    /// </code>
    /// </example>
    public static string? GetInverse(string predicate)
    {
        return InverseMap.TryGetValue(predicate, out var inverse) ? inverse : null;
    }
}
