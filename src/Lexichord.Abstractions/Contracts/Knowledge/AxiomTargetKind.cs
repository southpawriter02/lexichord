// =============================================================================
// File: AxiomTargetKind.cs
// Project: Lexichord.Abstractions
// Description: Defines the type of knowledge graph element an axiom targets.
// =============================================================================
// LOGIC: Axioms can target different types of knowledge graph elements.
//   This enum identifies whether an axiom applies to entities, relationships,
//   or claims, enabling type-specific validation logic.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Specifies the type of knowledge graph element that an axiom targets.
/// </summary>
/// <remarks>
/// Used by <see cref="Axiom.TargetKind"/> to determine what type of
/// element the axiom's rules should be evaluated against.
/// </remarks>
public enum AxiomTargetKind
{
    /// <summary>
    /// Axiom applies to entities (nodes in the knowledge graph).
    /// </summary>
    Entity = 0,

    /// <summary>
    /// Axiom applies to relationships (edges between entities).
    /// </summary>
    Relationship = 1,

    /// <summary>
    /// Axiom applies to claims (assertions about entities or relationships).
    /// </summary>
    Claim = 2
}
