// =============================================================================
// File: ContextConsistencyChecker.cs
// Project: Lexichord.Modules.Knowledge
// Description: Checks knowledge context for internal consistency before
//              LLM generation.
// =============================================================================
// LOGIC: Performs three categories of consistency checks on knowledge context:
//   1. Entity-level: duplicate names, conflicting property values.
//   2. Relationship-level: dangling references (endpoints not in context).
//   3. Request-level: missing entity references, ambiguous/empty requests.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: IContextConsistencyChecker (v0.6.6f),
//               KnowledgeContext (v0.6.6e), AgentRequest (v0.6.6a)
// =============================================================================

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Validation;

/// <summary>
/// Checks knowledge context for internal consistency.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ContextConsistencyChecker"/> performs structural analysis
/// of the knowledge context to detect issues that could mislead the LLM.
/// All checks operate on in-memory data and are synchronous.
/// </para>
/// <para>
/// <b>Checks performed by <see cref="CheckConsistency"/>:</b>
/// <list type="number">
///   <item>Duplicate entity detection (case-insensitive name matching).</item>
///   <item>Property conflict detection for same-type entities.</item>
///   <item>Relationship endpoint validation (both endpoints must exist).</item>
/// </list>
/// </para>
/// <para>
/// <b>Checks performed by <see cref="CheckRequestConsistency"/>:</b>
/// <list type="number">
///   <item>Empty or whitespace-only request detection.</item>
///   <item>Entity-like term reference detection (not in context).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
public class ContextConsistencyChecker : IContextConsistencyChecker
{
    /// <summary>
    /// Property names that indicate boolean-like values, where conflicting
    /// values across entities of the same type are likely real conflicts.
    /// </summary>
    private static readonly string[] BooleanLikeProperties =
        ["required", "optional", "deprecated", "enabled", "disabled"];

    private readonly ILogger<ContextConsistencyChecker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextConsistencyChecker"/> class.
    /// </summary>
    /// <param name="logger">Logger for consistency check diagnostics.</param>
    public ContextConsistencyChecker(ILogger<ContextConsistencyChecker> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextIssue> CheckConsistency(KnowledgeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var issues = new List<ContextIssue>();

        _logger.LogDebug(
            "Checking consistency of context with {EntityCount} entities, " +
            "{RelationshipCount} relationships",
            context.Entities.Count,
            context.Relationships?.Count ?? 0);

        // 1. Check for duplicate entities (same name, case-insensitive)
        var duplicates = context.Entities
            .GroupBy(e => e.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1);

        foreach (var dup in duplicates)
        {
            _logger.LogDebug(
                "Found {Count} duplicate entities with name '{Name}'",
                dup.Count(), dup.Key);

            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.ConflictingEntities,
                Message = $"Multiple entities with name '{dup.Key}' in context",
                Severity = ContextIssueSeverity.Warning,
                Resolution = "Consider which entity is most relevant"
            });
        }

        // 2. Check for conflicting property values across same-type entities
        issues.AddRange(CheckPropertyConflicts(context.Entities));

        // 3. Check relationship consistency (endpoints must exist in context)
        if (context.Relationships != null)
        {
            issues.AddRange(CheckRelationshipConsistency(
                context.Entities, context.Relationships));
        }

        _logger.LogDebug(
            "Consistency check complete: {IssueCount} issues found",
            issues.Count);

        return issues;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextIssue> CheckRequestConsistency(
        AgentRequest request,
        KnowledgeContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var issues = new List<ContextIssue>();

        _logger.LogDebug(
            "Checking request consistency against {EntityCount} context entities",
            context.Entities.Count);

        // 1. Check for empty or ambiguous request
        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.AmbiguousRequest,
                Message = "Request is empty or unclear",
                Severity = ContextIssueSeverity.Error
            });

            // No point checking further if the request is empty
            return issues;
        }

        // 2. Check if request references entities not in context
        var requestTerms = ExtractTerms(request.UserMessage);
        var contextEntityNames = context.Entities
            .Select(e => e.Name.ToLowerInvariant())
            .ToHashSet();

        // Look for potential entity references in the request
        foreach (var term in requestTerms)
        {
            if (LooksLikeEntityReference(term) && !contextEntityNames.Contains(term))
            {
                _logger.LogDebug(
                    "Request references '{Term}' which is not in context", term);

                issues.Add(new ContextIssue
                {
                    Code = ContextIssueCodes.MissingRequiredEntity,
                    Message = $"Request mentions '{term}' but no matching entity in context",
                    Severity = ContextIssueSeverity.Info,
                    Resolution = "Entity may need to be added to knowledge graph"
                });
            }
        }

        _logger.LogDebug(
            "Request consistency check complete: {IssueCount} issues found",
            issues.Count);

        return issues;
    }

    /// <summary>
    /// Checks for conflicting property values across entities of the same type.
    /// </summary>
    /// <param name="entities">The entities to check.</param>
    /// <returns>Issues for any detected property conflicts.</returns>
    /// <remarks>
    /// LOGIC: Groups entities by type, then for each type with 2+ entities,
    /// checks if any shared properties have conflicting values. Only boolean-like
    /// properties (required, optional, deprecated, etc.) are flagged as conflicts
    /// since different values for those are most likely errors.
    /// </remarks>
    private IEnumerable<ContextIssue> CheckPropertyConflicts(
        IReadOnlyList<KnowledgeEntity> entities)
    {
        // Group entities by type — only types with 2+ entities can conflict
        var byType = entities.GroupBy(e => e.Type);

        foreach (var typeGroup in byType)
        {
            var entitiesOfType = typeGroup.ToList();
            if (entitiesOfType.Count < 2) continue;

            // Collect all property names used by any entity of this type
            var allProps = entitiesOfType
                .SelectMany(e => e.Properties.Keys)
                .Distinct();

            foreach (var prop in allProps)
            {
                // Gather values from entities that have this property
                var values = entitiesOfType
                    .Where(e => e.Properties.ContainsKey(prop))
                    .Select(e => (Entity: e, Value: e.Properties[prop]))
                    .ToList();

                // Check if there are distinct values that look like conflicts
                if (values.Count > 1)
                {
                    var distinctValues = values
                        .Select(v => v.Value?.ToString())
                        .Distinct()
                        .ToList();

                    if (distinctValues.Count > 1 && IsLikelyConflict(prop, distinctValues))
                    {
                        _logger.LogDebug(
                            "Property '{Property}' has {Count} conflicting values " +
                            "across {Type} entities",
                            prop, distinctValues.Count, typeGroup.Key);

                        yield return new ContextIssue
                        {
                            Code = ContextIssueCodes.ConflictingEntities,
                            Message = $"Property '{prop}' has conflicting values " +
                                      $"across {typeGroup.Key} entities",
                            Severity = ContextIssueSeverity.Warning
                        };
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks that all relationship endpoints exist in the entity set.
    /// </summary>
    /// <param name="entities">The context entities.</param>
    /// <param name="relationships">The relationships to validate.</param>
    /// <returns>Issues for any dangling relationship endpoints.</returns>
    /// <remarks>
    /// LOGIC: For each relationship, checks that both FromEntityId and ToEntityId
    /// are present in the entity set. Missing endpoints produce a warning since
    /// they may cause the LLM to reference non-existent entities.
    /// </remarks>
    private IEnumerable<ContextIssue> CheckRelationshipConsistency(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships)
    {
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        foreach (var rel in relationships)
        {
            // Check source entity exists
            if (!entityIds.Contains(rel.FromEntityId))
            {
                _logger.LogDebug(
                    "Relationship '{Type}' references missing source entity: {Id}",
                    rel.Type, rel.FromEntityId);

                yield return new ContextIssue
                {
                    Code = ContextIssueCodes.MissingRequiredEntity,
                    Message = $"Relationship '{rel.Type}' references missing source entity",
                    Severity = ContextIssueSeverity.Warning
                };
            }

            // Check target entity exists
            if (!entityIds.Contains(rel.ToEntityId))
            {
                _logger.LogDebug(
                    "Relationship '{Type}' references missing target entity: {Id}",
                    rel.Type, rel.ToEntityId);

                yield return new ContextIssue
                {
                    Code = ContextIssueCodes.MissingRequiredEntity,
                    Message = $"Relationship '{rel.Type}' references missing target entity",
                    Severity = ContextIssueSeverity.Warning
                };
            }
        }
    }

    /// <summary>
    /// Extracts meaningful terms from a query string.
    /// </summary>
    /// <param name="query">The query to extract terms from.</param>
    /// <returns>Lowercased terms with length > 2.</returns>
    /// <remarks>
    /// LOGIC: Splits on common delimiters (space, comma, period, slash, hyphen,
    /// underscore) and filters out short terms (≤2 chars) which are unlikely to
    /// be meaningful entity references.
    /// </remarks>
    private static IReadOnlyList<string> ExtractTerms(string query)
    {
        return query.ToLowerInvariant()
            .Split([' ', ',', '.', '/', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToList();
    }

    /// <summary>
    /// Heuristic check for whether a term looks like an entity reference.
    /// </summary>
    /// <param name="term">The term to check.</param>
    /// <returns><c>true</c> if the term has entity-like characteristics.</returns>
    /// <remarks>
    /// LOGIC: Checks for patterns commonly found in entity names:
    /// path-like starts (/), snake_case (contains _), PascalCase (starts uppercase).
    /// </remarks>
    private static bool LooksLikeEntityReference(string term)
    {
        return term.StartsWith('/') ||   // Path-like (e.g., /api/users)
               term.Contains('_') ||     // Snake_case (e.g., user_id)
               char.IsUpper(term[0]);    // PascalCase (e.g., UserService)
    }

    /// <summary>
    /// Determines if conflicting property values are likely a real conflict.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="values">The distinct values found.</param>
    /// <returns><c>true</c> if the property is boolean-like and has different values.</returns>
    /// <remarks>
    /// LOGIC: Only boolean-like properties are flagged as conflicts. Properties
    /// like "description" or "name" naturally vary between entities and are not
    /// considered conflicts.
    /// </remarks>
    private static bool IsLikelyConflict(string propertyName, List<string?> values)
    {
        return BooleanLikeProperties.Any(p =>
            propertyName.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
