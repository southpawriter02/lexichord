// =============================================================================
// File: ClaimObjectType.cs
// Project: Lexichord.Abstractions
// Description: Type discriminator for claim objects (entity vs literal).
// =============================================================================
// LOGIC: Discriminates between claim objects that reference entities in the
//   knowledge graph versus literal values (strings, numbers, booleans).
//   Used by ClaimObject to determine which property contains the value.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Type of claim object (entity reference or literal value).
/// </summary>
/// <remarks>
/// <para>
/// A claim's object can be either an entity reference (linking to another
/// node in the knowledge graph) or a literal value (string, number, boolean).
/// This enum discriminates between these two cases.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <list type="bullet">
///   <item><see cref="Entity"/>: Object is a <see cref="ClaimEntity"/> reference.</item>
///   <item><see cref="Literal"/>: Object is a primitive value with type metadata.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public enum ClaimObjectType
{
    /// <summary>
    /// Object is an entity reference.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim object references another entity in the knowledge
    /// graph. See <see cref="ClaimObject.Entity"/> for the reference.
    /// Example: "GET /users ACCEPTS limit" where limit is an entity.
    /// </remarks>
    Entity,

    /// <summary>
    /// Object is a literal value.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim object is a primitive value (string, int, bool, etc.).
    /// See <see cref="ClaimObject.LiteralValue"/> and
    /// <see cref="ClaimObject.LiteralType"/> for the value and type.
    /// Example: "Rate limit HAS_VALUE 100" where 100 is a literal.
    /// </remarks>
    Literal
}
