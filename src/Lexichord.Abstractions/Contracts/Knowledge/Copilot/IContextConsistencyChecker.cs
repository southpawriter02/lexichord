// =============================================================================
// File: IContextConsistencyChecker.cs
// Project: Lexichord.Abstractions
// Description: Interface for checking knowledge context internal consistency.
// =============================================================================
// LOGIC: Defines the contract for consistency checking of knowledge context.
//   This is a sub-component of the pre-generation validator, responsible for
//   detecting conflicts within the context (duplicate entities, property
//   conflicts, dangling relationships) and between the request and context.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: AgentRequest (v0.6.6a), KnowledgeContext (v0.6.6e),
//               ContextIssue (v0.6.6f)
// =============================================================================

using Lexichord.Abstractions.Agents;

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Checks knowledge context for internal consistency.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IContextConsistencyChecker"/> performs structural analysis
/// of the knowledge context to detect issues that could lead to contradictory
/// or misleading LLM generation. It checks for:
/// </para>
/// <list type="bullet">
///   <item>Duplicate entities (same name, different data).</item>
///   <item>Conflicting property values across same-type entities.</item>
///   <item>Dangling relationship references (endpoints not in context).</item>
///   <item>Request references to entities not in the context.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. All methods
/// are synchronous since they operate on in-memory data only.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
public interface IContextConsistencyChecker
{
    /// <summary>
    /// Checks context entities and relationships for internal conflicts.
    /// </summary>
    /// <param name="context">The knowledge context to check.</param>
    /// <returns>
    /// A list of <see cref="ContextIssue"/> instances for each conflict
    /// found. Empty list if the context is consistent.
    /// </returns>
    /// <remarks>
    /// LOGIC:
    /// <list type="number">
    ///   <item>Detect duplicate entity names (case-insensitive).</item>
    ///   <item>Detect conflicting property values across same-type entities.</item>
    ///   <item>Validate relationship endpoint existence in entity set.</item>
    /// </list>
    /// </remarks>
    IReadOnlyList<ContextIssue> CheckConsistency(KnowledgeContext context);

    /// <summary>
    /// Checks if the user's request is consistent with the context.
    /// </summary>
    /// <param name="request">The user's agent request.</param>
    /// <param name="context">The knowledge context to check against.</param>
    /// <returns>
    /// A list of <see cref="ContextIssue"/> instances for each inconsistency
    /// found. Empty list if the request is consistent with context.
    /// </returns>
    /// <remarks>
    /// LOGIC:
    /// <list type="number">
    ///   <item>Extract terms from the request query.</item>
    ///   <item>Detect entity-like references not present in context.</item>
    ///   <item>Check for empty or ambiguous requests.</item>
    /// </list>
    /// </remarks>
    IReadOnlyList<ContextIssue> CheckRequestConsistency(
        AgentRequest request,
        KnowledgeContext context);
}
