// =============================================================================
// File: AxiomEvaluator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Implementation of axiom rule evaluation logic.
// =============================================================================
// LOGIC: Evaluates axiom rules against entity/relationship properties.
//   Supports all constraint types defined in AxiomConstraintType:
//   - Required: Property must be non-null and non-empty
//   - OneOf/NotOneOf: Value must (not) match one of allowed values
//   - Range: Numeric value within [Min, Max] bounds
//   - Pattern: String matches regex pattern
//   - Cardinality: Collection count within [MinCount, MaxCount]
//   - NotBoth: At most one of specified properties has value
//   - RequiresTogether: All specified properties present or none
//   - Equals/NotEquals: Property equals/not-equals expected value
//
// v0.4.6h: Axiom Query API (CKVS Phase 1e)
// Dependencies: Axiom data model (v0.4.6e), KnowledgeRecords (v0.4.5e)
// =============================================================================

using System.Collections;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Evaluates axiom rules against entities and relationships.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AxiomEvaluator"/> implements the core rule evaluation logic.
/// It is stateless and thread-safe - the same instance can be used concurrently
/// from multiple threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6h as part of the Axiom Query API.
/// </para>
/// </remarks>
public sealed class AxiomEvaluator : IAxiomEvaluator
{
    private readonly ILogger<AxiomEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AxiomEvaluator"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public AxiomEvaluator(ILogger<AxiomEvaluator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IReadOnlyList<AxiomViolation> Evaluate(Axiom axiom, KnowledgeEntity entity)
    {
        ArgumentNullException.ThrowIfNull(axiom);
        ArgumentNullException.ThrowIfNull(entity);

        _logger.LogDebug(
            "Evaluating axiom {AxiomId} against entity {EntityType}:{EntityId}",
            axiom.Id, entity.Type, entity.Id);

        // Build property dictionary including built-in properties
        var properties = BuildEntityProperties(entity);

        return EvaluateRules(axiom, properties, entity.Id, null);
    }

    /// <inheritdoc/>
    public IReadOnlyList<AxiomViolation> Evaluate(
        Axiom axiom,
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity)
    {
        ArgumentNullException.ThrowIfNull(axiom);
        ArgumentNullException.ThrowIfNull(relationship);
        ArgumentNullException.ThrowIfNull(fromEntity);
        ArgumentNullException.ThrowIfNull(toEntity);

        _logger.LogDebug(
            "Evaluating axiom {AxiomId} against relationship {RelType}:{RelId}",
            axiom.Id, relationship.Type, relationship.Id);

        // Build merged property dictionary
        var properties = BuildRelationshipProperties(relationship, fromEntity, toEntity);

        return EvaluateRules(axiom, properties, null, relationship.Id);
    }

    /// <inheritdoc/>
    public AxiomViolation? EvaluateRule(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(properties);

        try
        {
            return rule.Constraint switch
            {
                AxiomConstraintType.Required => EvaluateRequired(rule, properties),
                AxiomConstraintType.OneOf => EvaluateOneOf(rule, properties),
                AxiomConstraintType.NotOneOf => EvaluateNotOneOf(rule, properties),
                AxiomConstraintType.Range => EvaluateRange(rule, properties),
                AxiomConstraintType.Pattern => EvaluatePattern(rule, properties),
                AxiomConstraintType.Cardinality => EvaluateCardinality(rule, properties),
                AxiomConstraintType.NotBoth => EvaluateNotBoth(rule, properties),
                AxiomConstraintType.RequiresTogether => EvaluateRequiresTogether(rule, properties),
                AxiomConstraintType.Equals => EvaluateEquals(rule, properties),
                AxiomConstraintType.NotEquals => EvaluateNotEquals(rule, properties),
                AxiomConstraintType.Unique => null, // Unique requires graph context, handled by store
                AxiomConstraintType.ReferenceExists => null, // Reference checks require graph context
                AxiomConstraintType.TypeValid => null, // Type validation handled by schema registry
                AxiomConstraintType.Custom => null, // Custom validation not yet implemented
                _ => null // Unknown constraints pass silently
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error evaluating rule constraint {Constraint} for property {Property}",
                rule.Constraint, rule.Property);
            return null;
        }
    }

    #region Private Evaluation Methods

    /// <summary>
    /// Evaluates all rules in an axiom against the given properties.
    /// </summary>
    private IReadOnlyList<AxiomViolation> EvaluateRules(
        Axiom axiom,
        IReadOnlyDictionary<string, object?> properties,
        Guid? entityId,
        Guid? relationshipId)
    {
        var violations = new List<AxiomViolation>();

        foreach (var rule in axiom.Rules)
        {
            // Check condition first
            if (rule.When != null && !EvaluateCondition(rule.When, properties))
            {
                _logger.LogTrace(
                    "Skipping rule for {Property} - condition not met",
                    rule.Property ?? string.Join(", ", rule.Properties ?? Array.Empty<string>()));
                continue;
            }

            var violation = EvaluateRule(rule, properties);
            if (violation != null)
            {
                // Add context to the violation
                violations.Add(violation with
                {
                    Axiom = axiom,
                    ViolatedRule = rule,
                    EntityId = entityId,
                    RelationshipId = relationshipId,
                    Severity = axiom.Severity
                });
            }
        }

        return violations;
    }

    /// <summary>
    /// Builds a property dictionary from an entity.
    /// </summary>
    private static Dictionary<string, object?> BuildEntityProperties(KnowledgeEntity entity)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Add all entity properties
        foreach (var prop in entity.Properties)
        {
            properties[prop.Key] = prop.Value;
        }

        // Add built-in properties
        properties["id"] = entity.Id;
        properties["name"] = entity.Name;
        properties["type"] = entity.Type;

        return properties;
    }

    /// <summary>
    /// Builds a merged property dictionary from a relationship and its endpoints.
    /// </summary>
    private static Dictionary<string, object?> BuildRelationshipProperties(
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Add relationship properties
        foreach (var prop in relationship.Properties)
        {
            properties[prop.Key] = prop.Value;
        }

        // Add from entity properties with prefix
        foreach (var prop in fromEntity.Properties)
        {
            properties[$"from_{prop.Key}"] = prop.Value;
        }

        // Add to entity properties with prefix
        foreach (var prop in toEntity.Properties)
        {
            properties[$"to_{prop.Key}"] = prop.Value;
        }

        // Add built-in properties
        properties["id"] = relationship.Id;
        properties["type"] = relationship.Type;
        properties["from_id"] = relationship.FromEntityId;
        properties["to_id"] = relationship.ToEntityId;
        properties["from_name"] = fromEntity.Name;
        properties["to_name"] = toEntity.Name;
        properties["from_type"] = fromEntity.Type;
        properties["to_type"] = toEntity.Type;

        return properties;
    }

    #endregion

    #region Constraint Evaluators

    private AxiomViolation? EvaluateRequired(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property))
            return null;

        var hasValue = properties.TryGetValue(rule.Property, out var value) && !IsEmpty(value);

        if (!hasValue)
        {
            return CreateViolation(
                rule,
                rule.Property,
                null,
                "non-null value",
                rule.ErrorMessage ?? $"Property '{rule.Property}' is required");
        }

        return null;
    }

    private AxiomViolation? EvaluateOneOf(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property) || rule.Values == null || rule.Values.Count == 0)
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue) || actualValue == null)
            return null; // Missing values handled by Required constraint

        var matches = rule.Values.Any(v => ValuesEqual(actualValue, v));

        if (!matches)
        {
            return CreateViolation(
                rule,
                rule.Property,
                actualValue,
                rule.Values,
                rule.ErrorMessage ?? $"Property '{rule.Property}' must be one of: {string.Join(", ", rule.Values)}");
        }

        return null;
    }

    private AxiomViolation? EvaluateNotOneOf(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property) || rule.Values == null || rule.Values.Count == 0)
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue) || actualValue == null)
            return null;

        var matches = rule.Values.Any(v => ValuesEqual(actualValue, v));

        if (matches)
        {
            return CreateViolation(
                rule,
                rule.Property,
                actualValue,
                $"not one of: {string.Join(", ", rule.Values)}",
                rule.ErrorMessage ?? $"Property '{rule.Property}' must not be one of: {string.Join(", ", rule.Values)}");
        }

        return null;
    }

    private AxiomViolation? EvaluateRange(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property))
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue) || actualValue == null)
            return null;

        var numericValue = ConvertToDouble(actualValue);
        if (numericValue == null)
        {
            _logger.LogTrace(
                "Cannot evaluate Range constraint - property '{Property}' value '{Value}' is not numeric",
                rule.Property, actualValue);
            return null;
        }

        var min = rule.Min != null ? ConvertToDouble(rule.Min) : null;
        var max = rule.Max != null ? ConvertToDouble(rule.Max) : null;

        if (min.HasValue && numericValue.Value < min.Value)
        {
            return CreateViolation(
                rule,
                rule.Property,
                actualValue,
                $">= {rule.Min}",
                rule.ErrorMessage ?? $"Property '{rule.Property}' must be at least {rule.Min}");
        }

        if (max.HasValue && numericValue.Value > max.Value)
        {
            return CreateViolation(
                rule,
                rule.Property,
                actualValue,
                $"<= {rule.Max}",
                rule.ErrorMessage ?? $"Property '{rule.Property}' must be at most {rule.Max}");
        }

        return null;
    }

    private AxiomViolation? EvaluatePattern(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property) || string.IsNullOrEmpty(rule.Pattern))
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue) || actualValue == null)
            return null;

        var stringValue = actualValue.ToString();
        if (stringValue == null)
            return null;

        try
        {
            if (!Regex.IsMatch(stringValue, rule.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1)))
            {
                return CreateViolation(
                    rule,
                    rule.Property,
                    actualValue,
                    $"matches pattern: {rule.Pattern}",
                    rule.ErrorMessage ?? $"Property '{rule.Property}' must match pattern: {rule.Pattern}");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            _logger.LogWarning(
                "Pattern evaluation timeout for property '{Property}' with pattern '{Pattern}'",
                rule.Property, rule.Pattern);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid regex pattern '{Pattern}' for property '{Property}'",
                rule.Pattern, rule.Property);
        }

        return null;
    }

    private AxiomViolation? EvaluateCardinality(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property))
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue) || actualValue == null)
            return null;

        var count = GetCollectionCount(actualValue);
        if (count == null)
        {
            _logger.LogTrace(
                "Cannot evaluate Cardinality constraint - property '{Property}' is not a collection",
                rule.Property);
            return null;
        }

        if (rule.MinCount.HasValue && count.Value < rule.MinCount.Value)
        {
            return CreateViolation(
                rule,
                rule.Property,
                count,
                $"at least {rule.MinCount} items",
                rule.ErrorMessage ?? $"Property '{rule.Property}' must have at least {rule.MinCount} items");
        }

        if (rule.MaxCount.HasValue && count.Value > rule.MaxCount.Value)
        {
            return CreateViolation(
                rule,
                rule.Property,
                count,
                $"at most {rule.MaxCount} items",
                rule.ErrorMessage ?? $"Property '{rule.Property}' must have at most {rule.MaxCount} items");
        }

        return null;
    }

    private AxiomViolation? EvaluateNotBoth(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (rule.Properties == null || rule.Properties.Count < 2)
            return null;

        var presentProperties = rule.Properties
            .Where(p => properties.TryGetValue(p, out var v) && !IsEmpty(v))
            .ToList();

        if (presentProperties.Count > 1)
        {
            return CreateViolation(
                rule,
                string.Join(", ", rule.Properties),
                string.Join(", ", presentProperties),
                "at most one property should have a value",
                rule.ErrorMessage ?? $"Properties '{string.Join("' and '", presentProperties)}' cannot both have values");
        }

        return null;
    }

    private AxiomViolation? EvaluateRequiresTogether(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (rule.Properties == null || rule.Properties.Count < 2)
            return null;

        var presentCount = rule.Properties
            .Count(p => properties.TryGetValue(p, out var v) && !IsEmpty(v));

        // Either all must be present or none
        if (presentCount > 0 && presentCount < rule.Properties.Count)
        {
            var missing = rule.Properties
                .Where(p => !properties.TryGetValue(p, out var v) || IsEmpty(v))
                .ToList();

            return CreateViolation(
                rule,
                string.Join(", ", rule.Properties),
                $"only {presentCount} of {rule.Properties.Count} present",
                "all properties must be present together or none",
                rule.ErrorMessage ?? $"Properties {string.Join(", ", rule.Properties)} must all be present together. Missing: {string.Join(", ", missing)}");
        }

        return null;
    }

    private AxiomViolation? EvaluateEquals(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property) || rule.Values == null || rule.Values.Count == 0)
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue))
            return null;

        var expectedValue = rule.Values[0];
        if (!ValuesEqual(actualValue, expectedValue))
        {
            return CreateViolation(
                rule,
                rule.Property,
                actualValue,
                expectedValue,
                rule.ErrorMessage ?? $"Property '{rule.Property}' must equal '{expectedValue}'");
        }

        return null;
    }

    private AxiomViolation? EvaluateNotEquals(
        AxiomRule rule,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (string.IsNullOrEmpty(rule.Property) || rule.Values == null || rule.Values.Count == 0)
            return null;

        if (!properties.TryGetValue(rule.Property, out var actualValue))
            return null;

        var forbiddenValue = rule.Values[0];
        if (ValuesEqual(actualValue, forbiddenValue))
        {
            return CreateViolation(
                rule,
                rule.Property,
                actualValue,
                $"not equal to '{forbiddenValue}'",
                rule.ErrorMessage ?? $"Property '{rule.Property}' must not equal '{forbiddenValue}'");
        }

        return null;
    }

    #endregion

    #region Condition Evaluation

    /// <summary>
    /// Evaluates a condition clause against properties.
    /// </summary>
    private bool EvaluateCondition(
        AxiomCondition condition,
        IReadOnlyDictionary<string, object?> properties)
    {
        properties.TryGetValue(condition.Property, out var actualValue);

        return condition.Operator switch
        {
            ConditionOperator.Equals => ValuesEqual(actualValue, condition.Value),
            ConditionOperator.NotEquals => !ValuesEqual(actualValue, condition.Value),
            ConditionOperator.GreaterThan => CompareNumeric(actualValue, condition.Value) > 0,
            ConditionOperator.LessThan => CompareNumeric(actualValue, condition.Value) < 0,
            ConditionOperator.Contains => ContainsString(actualValue, condition.Value),
            ConditionOperator.StartsWith => StartsWithString(actualValue, condition.Value),
            ConditionOperator.EndsWith => EndsWithString(actualValue, condition.Value),
            ConditionOperator.IsNull => actualValue == null || IsEmpty(actualValue),
            ConditionOperator.IsNotNull => actualValue != null && !IsEmpty(actualValue),
            _ => true // Unknown operators pass
        };
    }

    #endregion

    #region Helper Methods

    private static AxiomViolation CreateViolation(
        AxiomRule rule,
        string propertyName,
        object? actualValue,
        object? expectedValue,
        string message)
    {
        return new AxiomViolation
        {
            Axiom = null!, // Will be set by caller
            ViolatedRule = rule,
            PropertyName = propertyName,
            ActualValue = actualValue,
            ExpectedValue = expectedValue,
            Message = message
        };
    }

    private static bool IsEmpty(object? value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            ICollection c => c.Count == 0,
            IEnumerable e => !e.Cast<object>().Any(),
            _ => false
        };
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Try direct equality first
        if (a.Equals(b)) return true;

        // Try string comparison (case-insensitive)
        var strA = a.ToString();
        var strB = b.ToString();
        if (string.Equals(strA, strB, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try numeric comparison
        var numA = ConvertToDouble(a);
        var numB = ConvertToDouble(b);
        if (numA.HasValue && numB.HasValue)
            return Math.Abs(numA.Value - numB.Value) < 0.0001;

        return false;
    }

    private static double? ConvertToDouble(object? value)
    {
        return value switch
        {
            null => null,
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal m => (double)m,
            short s => s,
            byte b => b,
            _ when double.TryParse(value.ToString(), out var result) => result,
            _ => null
        };
    }

    private static int? GetCollectionCount(object? value)
    {
        return value switch
        {
            null => null,
            ICollection c => c.Count,
            IEnumerable e => e.Cast<object>().Count(),
            _ => null
        };
    }

    private static int? CompareNumeric(object? a, object? b)
    {
        var numA = ConvertToDouble(a);
        var numB = ConvertToDouble(b);

        if (!numA.HasValue || !numB.HasValue)
            return null;

        return numA.Value.CompareTo(numB.Value);
    }

    private static bool ContainsString(object? value, object? substring)
    {
        var strValue = value?.ToString();
        var strSubstring = substring?.ToString();

        if (strValue == null || strSubstring == null)
            return false;

        return strValue.Contains(strSubstring, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWithString(object? value, object? prefix)
    {
        var strValue = value?.ToString();
        var strPrefix = prefix?.ToString();

        if (strValue == null || strPrefix == null)
            return false;

        return strValue.StartsWith(strPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndsWithString(object? value, object? suffix)
    {
        var strValue = value?.ToString();
        var strSuffix = suffix?.ToString();

        if (strValue == null || strSuffix == null)
            return false;

        return strValue.EndsWith(strSuffix, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
