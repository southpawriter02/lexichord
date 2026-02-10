// =============================================================================
// File: PreGenerationValidator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Validates knowledge context and request before LLM generation.
// =============================================================================
// LOGIC: Orchestrates pre-generation validation by:
//   1. Checking for empty context (warning, not blocking).
//   2. Running consistency checks via IContextConsistencyChecker.
//   3. Performing request-context alignment checks.
//   4. Validating axiom compliance for context entities.
//   5. Aggregating all issues and determining proceed/block.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: IPreGenerationValidator (v0.6.6f),
//               IContextConsistencyChecker (v0.6.6f),
//               KnowledgeContext (v0.6.6e), AgentRequest (v0.6.6a),
//               Axiom/AxiomRule/AxiomConstraintType (v0.4.6e)
// =============================================================================

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Validation;

/// <summary>
/// Validates knowledge context and user request before LLM generation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PreGenerationValidator"/> orchestrates the full pre-generation
/// validation pipeline. It combines consistency checking, request validation,
/// and axiom compliance checking to produce a single
/// <see cref="PreValidationResult"/> that determines if generation can proceed.
/// </para>
/// <para>
/// <b>Validation Pipeline:</b>
/// <list type="number">
///   <item>Empty context check → warning (generation proceeds without knowledge).</item>
///   <item>Context consistency → duplicates, property conflicts, relationships.</item>
///   <item>Request-context alignment → missing references, ambiguous queries.</item>
///   <item>Axiom compliance → entity property validation against rules.</item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b>
/// <list type="bullet">
///   <item>Null arguments → <see cref="ArgumentNullException"/>.</item>
///   <item>Consistency checker failure → caught, logged, generation allowed.</item>
///   <item>Axiom check failure → caught, logged as warning issue.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
public class PreGenerationValidator : IPreGenerationValidator
{
    private readonly IContextConsistencyChecker _consistencyChecker;
    private readonly ILogger<PreGenerationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreGenerationValidator"/> class.
    /// </summary>
    /// <param name="consistencyChecker">The context consistency checker.</param>
    /// <param name="logger">Logger for validation diagnostics.</param>
    public PreGenerationValidator(
        IContextConsistencyChecker consistencyChecker,
        ILogger<PreGenerationValidator> logger)
    {
        _consistencyChecker = consistencyChecker ??
            throw new ArgumentNullException(nameof(consistencyChecker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<PreValidationResult> ValidateAsync(
        AgentRequest request,
        KnowledgeContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug(
            "Starting pre-generation validation for request with " +
            "{EntityCount} entities in context",
            context.Entities.Count);

        var issues = new List<ContextIssue>();

        // Step 1: Check for empty context
        if (context.Entities.Count == 0)
        {
            _logger.LogDebug("Context has no entities, adding empty context warning");

            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.EmptyContext,
                Message = "No knowledge context available for this request",
                Severity = ContextIssueSeverity.Warning,
                Resolution = "Generation will proceed without domain knowledge"
            });
        }

        // Step 2: Check context consistency (duplicate entities, property conflicts)
        try
        {
            var consistencyIssues = _consistencyChecker.CheckConsistency(context);
            issues.AddRange(consistencyIssues);

            _logger.LogDebug(
                "Consistency check returned {Count} issues",
                consistencyIssues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Consistency check failed, allowing generation with warning");

            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.StaleContext,
                Message = "Unable to verify context consistency",
                Severity = ContextIssueSeverity.Warning,
                Resolution = "Context may contain undetected inconsistencies"
            });
        }

        // Step 3: Check request against context (missing references, ambiguity)
        try
        {
            var requestIssues = _consistencyChecker.CheckRequestConsistency(
                request, context);
            issues.AddRange(requestIssues);

            _logger.LogDebug(
                "Request consistency check returned {Count} issues",
                requestIssues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Request consistency check failed, allowing generation with warning");
        }

        // Step 4: Check axiom compliance of context entities
        if (context.Axioms?.Count > 0)
        {
            var axiomIssues = CheckAxiomCompliance(context);
            issues.AddRange(axiomIssues);

            _logger.LogDebug(
                "Axiom compliance check returned {Count} issues",
                axiomIssues.Count);
        }

        // Step 5: Determine if generation can proceed
        var blockingIssues = issues
            .Where(i => i.Severity == ContextIssueSeverity.Error)
            .ToList();
        var canProceed = blockingIssues.Count == 0;

        _logger.LogDebug(
            "Pre-validation complete: {IssueCount} issues, canProceed={CanProceed}",
            issues.Count, canProceed);

        if (!canProceed)
        {
            _logger.LogInformation(
                "Pre-validation blocked generation: {BlockingCount} blocking issues",
                blockingIssues.Count);
        }

        return Task.FromResult(new PreValidationResult
        {
            CanProceed = canProceed,
            Issues = issues,
            UserMessage = canProceed
                ? null
                : $"Cannot generate: {string.Join("; ", blockingIssues.Select(i => i.Message))}"
        });
    }

    /// <inheritdoc />
    public async Task<bool> CanProceedAsync(
        AgentRequest request,
        KnowledgeContext context,
        CancellationToken ct = default)
    {
        var result = await ValidateAsync(request, context, ct);
        return result.CanProceed;
    }

    /// <summary>
    /// Checks context entities for axiom rule compliance.
    /// </summary>
    /// <param name="context">The knowledge context with entities and axioms.</param>
    /// <returns>Issues for any axiom violations found.</returns>
    /// <remarks>
    /// LOGIC: For each entity in the context, finds axioms that target its type
    /// and checks if the entity satisfies each axiom's rules. Uses
    /// <see cref="AxiomConstraintType.Required"/> to check property existence
    /// and simple equality checks for other constraint types.
    ///
    /// Axiom severity mapping:
    /// - <see cref="AxiomSeverity.Error"/> → <see cref="ContextIssueSeverity.Error"/> (blocks generation)
    /// - <see cref="AxiomSeverity.Warning"/> or <see cref="AxiomSeverity.Info"/>
    ///   → <see cref="ContextIssueSeverity.Warning"/>
    /// </remarks>
    private IReadOnlyList<ContextIssue> CheckAxiomCompliance(KnowledgeContext context)
    {
        var issues = new List<ContextIssue>();

        foreach (var entity in context.Entities)
        {
            // Find axioms that target this entity's type
            var applicableAxioms = context.Axioms?
                .Where(a => string.Equals(a.TargetType, entity.Type,
                    StringComparison.OrdinalIgnoreCase))
                .ToList() ?? [];

            foreach (var axiom in applicableAxioms)
            {
                if (!EntitySatisfiesAxiom(entity, axiom))
                {
                    _logger.LogDebug(
                        "Entity '{EntityName}' violates axiom '{AxiomName}'",
                        entity.Name, axiom.Name);

                    issues.Add(new ContextIssue
                    {
                        Code = ContextIssueCodes.AxiomViolation,
                        Message = $"Entity '{entity.Name}' violates axiom '{axiom.Name}'",
                        Severity = axiom.Severity == AxiomSeverity.Error
                            ? ContextIssueSeverity.Error
                            : ContextIssueSeverity.Warning,
                        RelatedEntity = entity,
                        RelatedAxiom = axiom,
                        Resolution = $"Axiom rule: {axiom.Description}"
                    });
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Checks if an entity satisfies all rules of an axiom.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="axiom">The axiom containing rules to check.</param>
    /// <returns><c>true</c> if all rules pass; <c>false</c> if any rule fails.</returns>
    /// <remarks>
    /// LOGIC: Iterates through axiom rules and checks each against the entity's
    /// properties. Supports <see cref="AxiomConstraintType.Required"/> (property
    /// must exist) and <see cref="AxiomConstraintType.Equals"/> (property must
    /// match expected value). Unknown constraint types pass by default to avoid
    /// false positives.
    /// </remarks>
    private static bool EntitySatisfiesAxiom(KnowledgeEntity entity, Axiom axiom)
    {
        // If no rules defined, axiom is trivially satisfied
        if (axiom.Rules.Count == 0) return true;

        foreach (var rule in axiom.Rules)
        {
            // Skip rules without a property target
            if (string.IsNullOrEmpty(rule.Property)) continue;

            var hasProperty = entity.Properties.ContainsKey(rule.Property);
            var value = hasProperty ? entity.Properties[rule.Property] : null;

            var satisfied = rule.Constraint switch
            {
                // Required: property must exist and be non-null
                AxiomConstraintType.Required => hasProperty && value != null,

                // Equals: property value must match the first expected value
                AxiomConstraintType.Equals =>
                    hasProperty && Equals(value?.ToString(),
                        rule.Values?.FirstOrDefault()?.ToString()),

                // Unknown constraint types pass by default
                _ => true
            };

            if (!satisfied) return false;
        }

        return true;
    }
}
