// =============================================================================
// File: AxiomValidatorService.cs
// Project: Lexichord.Modules.Knowledge
// Description: IValidator implementation for axiom-based entity validation.
// =============================================================================
// LOGIC: Bridges the v0.6.5e validation pipeline (IValidator) with the v0.4.6h
//   axiom evaluation system (IAxiomStore + IAxiomEvaluator). When invoked via
//   ValidateAsync, extracts entities from context metadata and validates each
//   against all applicable axioms.
//
// Validation steps per entity:
//   1. Retrieve all axioms from IAxiomStore
//   2. Match axioms to entity via AxiomMatcher (TargetType + TargetKind)
//   3. Evaluate each matching axiom's rules via IAxiomEvaluator
//   4. Convert AxiomViolation instances to ValidationFinding instances
//   5. Map AxiomSeverity → ValidationSeverity for pipeline compatibility
//
// v0.6.5g: Axiom Validator (CKVS Phase 3a)
// Dependencies: IValidator (v0.6.5e), IAxiomStore (v0.4.6h),
//               IAxiomEvaluator (v0.4.6h), AxiomMatcher (v0.6.5g),
//               KnowledgeEntity (v0.4.5e), ValidationFinding (v0.6.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Axiom;

/// <summary>
/// Axiom validator that integrates with the v0.6.5e validation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AxiomValidatorService"/> implements
/// <see cref="IAxiomValidatorService"/> (which extends <see cref="IValidator"/>),
/// enabling it to participate in the validation orchestrator pipeline while also
/// exposing direct entity validation methods.
/// </para>
/// <para>
/// <b>Entity Extraction:</b> When invoked via <see cref="ValidateAsync"/>,
/// entities are extracted from <c>context.Metadata["entities"]</c>. If no
/// entities are present, the validator returns an empty findings list.
/// </para>
/// <para>
/// <b>Axiom Evaluation:</b> Uses the existing <see cref="IAxiomEvaluator"/>
/// (v0.4.6h) for rule evaluation. <see cref="AxiomViolation"/> instances are
/// mapped to <see cref="ValidationFinding"/> with appropriate severity and
/// finding code mapping.
/// </para>
/// <para>
/// <b>License Requirement:</b> <see cref="LicenseTier.Teams"/> or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5g as part of the Axiom Validator.
/// </para>
/// </remarks>
public sealed class AxiomValidatorService : IAxiomValidatorService
{
    private readonly IAxiomStore _axiomStore;
    private readonly IAxiomEvaluator _axiomEvaluator;
    private readonly ILogger<AxiomValidatorService> _logger;

    /// <inheritdoc />
    public string Id => "axiom-validator";

    /// <inheritdoc />
    public string DisplayName => "Axiom Validator";

    /// <inheritdoc />
    /// <remarks>
    /// Axiom validation is supported in all modes. Performance depends on
    /// the number of loaded axioms and matching rules.
    /// </remarks>
    public ValidationMode SupportedModes => ValidationMode.All;

    /// <inheritdoc />
    public LicenseTier RequiredLicenseTier => LicenseTier.Teams;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxiomValidatorService"/> class.
    /// </summary>
    /// <param name="axiomStore">The axiom store for retrieving loaded axioms.</param>
    /// <param name="axiomEvaluator">The evaluator for checking axiom rules against entities.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="axiomStore"/>, <paramref name="axiomEvaluator"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    public AxiomValidatorService(
        IAxiomStore axiomStore,
        IAxiomEvaluator axiomEvaluator,
        ILogger<AxiomValidatorService> logger)
    {
        _axiomStore = axiomStore ?? throw new ArgumentNullException(nameof(axiomStore));
        _axiomEvaluator = axiomEvaluator ?? throw new ArgumentNullException(nameof(axiomEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "AxiomValidatorService: Initialized — Id={Id}, Modes={Modes}, Tier={Tier}",
            Id, SupportedModes, RequiredLicenseTier);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Extracts entities from <c>context.Metadata["entities"]</c> and validates
    /// each against its applicable axioms. If no entities key is present, returns empty.
    /// </remarks>
    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "AxiomValidatorService: ValidateAsync called for document '{DocumentId}'",
            context.DocumentId);

        // LOGIC: Extract entities from context metadata.
        if (!context.Metadata.TryGetValue("entities", out var entitiesObj) ||
            entitiesObj is not IReadOnlyList<KnowledgeEntity> entities)
        {
            _logger.LogDebug(
                "AxiomValidatorService: No entities found in context metadata for document '{DocumentId}' — skipping",
                context.DocumentId);
            return Array.Empty<ValidationFinding>();
        }

        _logger.LogDebug(
            "AxiomValidatorService: Validating {Count} entities from document '{DocumentId}'",
            entities.Count, context.DocumentId);

        return await ValidateEntitiesAsync(entities, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ValidationFinding>> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "AxiomValidatorService: Validating entity '{EntityName}' (Type={EntityType}, Id={EntityId})",
            entity.Name, entity.Type, entity.Id);

        var findings = new List<ValidationFinding>();

        // LOGIC: Step 1 — Retrieve all axioms from the store.
        IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom> allAxioms;
        try
        {
            allAxioms = _axiomStore.GetAllAxioms();
        }
        catch (FeatureNotLicensedException ex)
        {
            _logger.LogDebug(
                "AxiomValidatorService: Axiom store not accessible (license: {Message}) — skipping validation",
                ex.Message);
            return Task.FromResult<IReadOnlyList<ValidationFinding>>(findings);
        }

        // LOGIC: Step 2 — Match axioms to entity via AxiomMatcher.
        var matchingAxioms = AxiomMatcher.FindMatchingAxioms(entity, allAxioms);

        if (matchingAxioms.Count == 0)
        {
            _logger.LogDebug(
                "AxiomValidatorService: No applicable axioms for entity type '{EntityType}'",
                entity.Type);
            return Task.FromResult<IReadOnlyList<ValidationFinding>>(findings);
        }

        _logger.LogDebug(
            "AxiomValidatorService: Found {AxiomCount} applicable axioms for entity '{EntityName}' (Type={EntityType})",
            matchingAxioms.Count, entity.Name, entity.Type);

        // LOGIC: Step 3 — Evaluate each matching axiom's rules via IAxiomEvaluator.
        foreach (var axiom in matchingAxioms)
        {
            ct.ThrowIfCancellationRequested();

            var violations = _axiomEvaluator.Evaluate(axiom, entity);

            // LOGIC: Step 4 — Convert AxiomViolation → ValidationFinding.
            foreach (var violation in violations)
            {
                var finding = ConvertViolationToFinding(violation);
                findings.Add(finding);

                _logger.LogDebug(
                    "AxiomValidatorService: Axiom violation — Axiom={AxiomId}, Rule={Constraint}, Property={Property}, Severity={Severity}",
                    axiom.Id,
                    violation.ViolatedRule.Constraint,
                    violation.PropertyName ?? "(multi-property)",
                    violation.Severity);
            }
        }

        _logger.LogDebug(
            "AxiomValidatorService: Entity '{EntityName}' validation complete — {Count} finding(s)",
            entity.Name, findings.Count);

        return Task.FromResult<IReadOnlyList<ValidationFinding>>(findings);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationFinding>> ValidateEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "AxiomValidatorService: Validating batch of {Count} entities",
            entities.Count);

        var findings = new List<ValidationFinding>();
        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();
            var entityFindings = await ValidateEntityAsync(entity, ct);
            findings.AddRange(entityFindings);
        }

        _logger.LogDebug(
            "AxiomValidatorService: Batch validation complete — {Count} total finding(s) across {EntityCount} entities",
            findings.Count, entities.Count);

        return findings;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom>> GetApplicableAxiomsAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "AxiomValidatorService: Getting applicable axioms for entity type '{EntityType}'",
            entity.Type);

        IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom> allAxioms;
        try
        {
            allAxioms = _axiomStore.GetAllAxioms();
        }
        catch (FeatureNotLicensedException ex)
        {
            _logger.LogDebug(
                "AxiomValidatorService: Axiom store not accessible (license: {Message})",
                ex.Message);
            return Task.FromResult<IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom>>(Array.Empty<Lexichord.Abstractions.Contracts.Knowledge.Axiom>());
        }

        var matchingAxioms = AxiomMatcher.FindMatchingAxioms(entity, allAxioms);

        _logger.LogDebug(
            "AxiomValidatorService: Found {Count} applicable axioms for entity type '{EntityType}'",
            matchingAxioms.Count, entity.Type);

        return Task.FromResult<IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom>>(matchingAxioms);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Converts an <see cref="AxiomViolation"/> to a <see cref="ValidationFinding"/>.
    /// </summary>
    /// <param name="violation">The axiom violation to convert.</param>
    /// <returns>An equivalent <see cref="ValidationFinding"/>.</returns>
    /// <remarks>
    /// LOGIC: Maps AxiomSeverity → ValidationSeverity and AxiomConstraintType
    /// to the appropriate AxiomFindingCodes constant. The property path is set
    /// to the violation's PropertyName for UI highlighting.
    /// </remarks>
    private ValidationFinding ConvertViolationToFinding(AxiomViolation violation)
    {
        var severity = MapSeverity(violation.Severity);
        var code = MapConstraintToCode(violation.ViolatedRule.Constraint);
        var suggestedFix = violation.SuggestedFix?.Description;

        return new ValidationFinding(
            ValidatorId: Id,
            Severity: severity,
            Code: code,
            Message: violation.Message,
            PropertyPath: violation.PropertyName,
            SuggestedFix: suggestedFix);
    }

    /// <summary>
    /// Maps <see cref="AxiomSeverity"/> to <see cref="ValidationSeverity"/>.
    /// </summary>
    /// <param name="severity">The axiom severity to map.</param>
    /// <returns>The equivalent validation severity.</returns>
    /// <remarks>
    /// LOGIC: Direct mapping — both enums have the same three levels:
    ///   AxiomSeverity.Error (0)   → ValidationSeverity.Error (2)
    ///   AxiomSeverity.Warning (1) → ValidationSeverity.Warning (1)
    ///   AxiomSeverity.Info (2)    → ValidationSeverity.Info (0)
    /// Note: The numeric values differ, so explicit mapping is required.
    /// </remarks>
    internal static Lexichord.Abstractions.Contracts.Knowledge.Validation.ValidationSeverity MapSeverity(AxiomSeverity severity) => severity switch
    {
        AxiomSeverity.Error => Lexichord.Abstractions.Contracts.Knowledge.Validation.ValidationSeverity.Error,
        AxiomSeverity.Warning => Lexichord.Abstractions.Contracts.Knowledge.Validation.ValidationSeverity.Warning,
        AxiomSeverity.Info => Lexichord.Abstractions.Contracts.Knowledge.Validation.ValidationSeverity.Info,
        _ => Lexichord.Abstractions.Contracts.Knowledge.Validation.ValidationSeverity.Warning
    };

    /// <summary>
    /// Maps an <see cref="AxiomConstraintType"/> to the appropriate finding code.
    /// </summary>
    /// <param name="constraint">The constraint type to map.</param>
    /// <returns>The corresponding <see cref="AxiomFindingCodes"/> constant.</returns>
    /// <remarks>
    /// LOGIC: Maps each constraint type to its specific finding code for
    /// programmatic filtering. Unmapped types fall through to the generic
    /// AXIOM_VIOLATION code.
    /// </remarks>
    internal static string MapConstraintToCode(AxiomConstraintType constraint) => constraint switch
    {
        AxiomConstraintType.Required => AxiomFindingCodes.RequiredViolation,
        AxiomConstraintType.OneOf => AxiomFindingCodes.PropertyConstraint,
        AxiomConstraintType.NotOneOf => AxiomFindingCodes.PropertyConstraint,
        AxiomConstraintType.Range => AxiomFindingCodes.RangeViolation,
        AxiomConstraintType.Pattern => AxiomFindingCodes.PatternViolation,
        AxiomConstraintType.Cardinality => AxiomFindingCodes.CardinalityViolation,
        AxiomConstraintType.NotBoth => AxiomFindingCodes.MutualExclusionViolation,
        AxiomConstraintType.RequiresTogether => AxiomFindingCodes.DependencyViolation,
        AxiomConstraintType.Equals => AxiomFindingCodes.EqualityViolation,
        AxiomConstraintType.NotEquals => AxiomFindingCodes.InequalityViolation,
        AxiomConstraintType.ReferenceExists => AxiomFindingCodes.RelationshipInvalid,
        _ => AxiomFindingCodes.AxiomViolation
    };
}
