// =============================================================================
// File: AxiomSchemaValidator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Validates parsed axioms against the schema registry.
// =============================================================================
// LOGIC: Verifies that axiom target types exist in the schema registry.
//   - Validates entity types for AxiomTargetKind.Entity axioms
//   - Validates relationship types for AxiomTargetKind.Relationship axioms
//   - Reports unknown target types as warnings (non-blocking)
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// Dependencies: ISchemaRegistry (v0.4.5f), Axiom (v0.4.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Validates parsed axioms against the schema registry.
/// </summary>
/// <remarks>
/// LOGIC: The validator checks that target types referenced by axioms exist
/// in the schema registry. Unknown types are flagged as warnings to allow
/// loading axioms for types that may be added later.
/// </remarks>
internal sealed class AxiomSchemaValidator
{
    private readonly ISchemaRegistry _schemaRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxiomSchemaValidator"/> class.
    /// </summary>
    /// <param name="schemaRegistry">The schema registry for type validation.</param>
    public AxiomSchemaValidator(ISchemaRegistry schemaRegistry)
    {
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
    }

    /// <summary>
    /// Validates a collection of axioms against the schema registry.
    /// </summary>
    /// <param name="axioms">The axioms to validate.</param>
    /// <returns>Validation result with errors and invalid axiom IDs.</returns>
    public AxiomSchemaValidationResult Validate(IReadOnlyList<Axiom> axioms)
    {
        var errors = new List<AxiomLoadError>();
        var invalidAxiomIds = new HashSet<string>();
        var unknownTargetTypes = new HashSet<string>();

        foreach (var axiom in axioms)
        {
            // LOGIC: Validate target type exists in schema registry.
            var targetExists = axiom.TargetKind switch
            {
                AxiomTargetKind.Entity => _schemaRegistry.GetEntityType(axiom.TargetType) != null,
                AxiomTargetKind.Relationship => _schemaRegistry.GetRelationshipType(axiom.TargetType) != null,
                _ => true // Unknown kinds pass through for future compatibility
            };

            if (!targetExists)
            {
                unknownTargetTypes.Add(axiom.TargetType);
                errors.Add(new AxiomLoadError
                {
                    Code = "UNKNOWN_TARGET_TYPE",
                    Message = $"Target type '{axiom.TargetType}' not found in schema registry",
                    AxiomId = axiom.Id,
                    Severity = LoadErrorSeverity.Warning
                });

                // LOGIC: Unknown target types are warnings, not errors.
                // The axiom is still valid but may not apply to any entities.
                // We don't add to invalidAxiomIds because the axiom structure is valid.
            }

            // LOGIC: Validate required fields are present.
            if (string.IsNullOrWhiteSpace(axiom.Id))
            {
                errors.Add(new AxiomLoadError
                {
                    Code = "MISSING_ID",
                    Message = "Axiom is missing required 'id' field",
                    Severity = LoadErrorSeverity.Error
                });
                continue; // Can't track by ID if missing
            }

            if (string.IsNullOrWhiteSpace(axiom.Name))
            {
                errors.Add(new AxiomLoadError
                {
                    Code = "MISSING_NAME",
                    Message = "Axiom is missing required 'name' field",
                    AxiomId = axiom.Id,
                    Severity = LoadErrorSeverity.Error
                });
                invalidAxiomIds.Add(axiom.Id);
            }

            if (string.IsNullOrWhiteSpace(axiom.TargetType))
            {
                errors.Add(new AxiomLoadError
                {
                    Code = "MISSING_TARGET_TYPE",
                    Message = "Axiom is missing required 'target_type' field",
                    AxiomId = axiom.Id,
                    Severity = LoadErrorSeverity.Error
                });
                invalidAxiomIds.Add(axiom.Id);
            }

            if (axiom.Rules.Count == 0)
            {
                errors.Add(new AxiomLoadError
                {
                    Code = "NO_RULES",
                    Message = "Axiom has no rules defined",
                    AxiomId = axiom.Id,
                    Severity = LoadErrorSeverity.Warning
                });
            }

            // LOGIC: Validate each rule has required constraint information.
            foreach (var rule in axiom.Rules)
            {
                if (rule.Constraint == AxiomConstraintType.Required ||
                    rule.Constraint == AxiomConstraintType.OneOf ||
                    rule.Constraint == AxiomConstraintType.NotOneOf ||
                    rule.Constraint == AxiomConstraintType.Range ||
                    rule.Constraint == AxiomConstraintType.Pattern ||
                    rule.Constraint == AxiomConstraintType.Equals ||
                    rule.Constraint == AxiomConstraintType.NotEquals)
                {
                    if (string.IsNullOrWhiteSpace(rule.Property) &&
                        (rule.Properties == null || rule.Properties.Count == 0))
                    {
                        errors.Add(new AxiomLoadError
                        {
                            Code = "MISSING_PROPERTY",
                            Message = $"Rule with constraint '{rule.Constraint}' requires a property",
                            AxiomId = axiom.Id,
                            Severity = LoadErrorSeverity.Error
                        });
                        invalidAxiomIds.Add(axiom.Id);
                    }
                }
            }
        }

        return new AxiomSchemaValidationResult(
            errors,
            invalidAxiomIds.ToList(),
            unknownTargetTypes.ToList());
    }
}

/// <summary>
/// Result of schema validation.
/// </summary>
/// <param name="Errors">Validation errors and warnings.</param>
/// <param name="InvalidAxiomIds">IDs of axioms with blocking errors.</param>
/// <param name="UnknownTargetTypes">Target types not found in schema registry.</param>
internal record AxiomSchemaValidationResult(
    IReadOnlyList<AxiomLoadError> Errors,
    IReadOnlyList<string> InvalidAxiomIds,
    IReadOnlyList<string> UnknownTargetTypes);
