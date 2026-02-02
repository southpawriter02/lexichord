// =============================================================================
// File: AxiomItemViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model wrapper for displaying a single axiom in the UI.
// =============================================================================
// LOGIC: Wraps an Axiom record for display, providing formatted rule descriptions
//   that convert constraint types into human-readable text. Supports all 14
//   constraint types and conditional (When) clauses.
//
// v0.4.7i: Axiom Viewer (CKVS Phase 1c)
// Dependencies: Axiom, AxiomRule, AxiomCondition (v0.4.6e),
//               AxiomConstraintType, ConditionOperator (v0.4.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model wrapper for displaying a single axiom in the Axiom Viewer.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AxiomItemViewModel"/> provides a display-friendly representation
/// of an <see cref="Axiom"/> for use in the Knowledge Graph Browser UI. It exposes
/// properties for binding and provides human-readable formatting of axiom rules.
/// </para>
/// <para>
/// <b>Rule Formatting:</b> The <see cref="FormattedRules"/> property provides
/// human-readable descriptions of each rule, converting constraint types like
/// <c>Required</c>, <c>OneOf</c>, <c>Pattern</c>, etc. into natural language.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7i as part of the Axiom Viewer.
/// </para>
/// </remarks>
public class AxiomItemViewModel
{
    private readonly Axiom _axiom;
    private readonly Lazy<IReadOnlyList<string>> _formattedRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxiomItemViewModel"/> class.
    /// </summary>
    /// <param name="axiom">The axiom to wrap.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="axiom"/> is null.
    /// </exception>
    public AxiomItemViewModel(Axiom axiom)
    {
        _axiom = axiom ?? throw new ArgumentNullException(nameof(axiom));
        _formattedRules = new Lazy<IReadOnlyList<string>>(() => FormatAllRules());
    }

    #region Properties Mapped from Axiom

    /// <summary>
    /// Gets the unique identifier for this axiom.
    /// </summary>
    public string Id => _axiom.Id;

    /// <summary>
    /// Gets the human-readable display name for the axiom.
    /// </summary>
    public string Name => _axiom.Name;

    /// <summary>
    /// Gets the detailed description of what this axiom enforces.
    /// </summary>
    public string? Description => _axiom.Description;

    /// <summary>
    /// Gets the entity or relationship type this axiom applies to.
    /// </summary>
    public string TargetType => _axiom.TargetType;

    /// <summary>
    /// Gets whether this axiom targets entities, relationships, or claims.
    /// </summary>
    public AxiomTargetKind TargetKind => _axiom.TargetKind;

    /// <summary>
    /// Gets the severity of violations of this axiom.
    /// </summary>
    public AxiomSeverity Severity => _axiom.Severity;

    /// <summary>
    /// Gets the category for grouping related axioms in the UI.
    /// </summary>
    public string? Category => _axiom.Category;

    /// <summary>
    /// Gets the tags for filtering and searching axioms.
    /// </summary>
    public IReadOnlyList<string> Tags => _axiom.Tags;

    /// <summary>
    /// Gets whether this axiom is currently enabled for validation.
    /// </summary>
    public bool IsEnabled => _axiom.IsEnabled;

    /// <summary>
    /// Gets the number of rules in this axiom.
    /// </summary>
    public int RuleCount => _axiom.Rules.Count;

    /// <summary>
    /// Gets the source file path if this axiom was loaded from YAML.
    /// </summary>
    public string? SourceFile => _axiom.SourceFile;

    #endregion

    #region Formatted Display Properties

    /// <summary>
    /// Gets human-readable descriptions of all rules in this axiom.
    /// </summary>
    /// <remarks>
    /// LOGIC: Lazy-evaluated list of formatted rule descriptions. Each rule
    /// is converted to natural language based on its constraint type.
    /// </remarks>
    public IReadOnlyList<string> FormattedRules => _formattedRules.Value;

    /// <summary>
    /// Gets the severity as a display-friendly string.
    /// </summary>
    public string SeverityDisplay => Severity switch
    {
        AxiomSeverity.Error => "Error",
        AxiomSeverity.Warning => "Warning",
        AxiomSeverity.Info => "Info",
        _ => Severity.ToString()
    };

    /// <summary>
    /// Gets the target kind as a display-friendly string.
    /// </summary>
    public string TargetKindDisplay => TargetKind switch
    {
        AxiomTargetKind.Entity => "Entity",
        AxiomTargetKind.Relationship => "Relationship",
        AxiomTargetKind.Claim => "Claim",
        _ => TargetKind.ToString()
    };

    #endregion

    #region Rule Formatting Methods

    /// <summary>
    /// Formats all rules into human-readable descriptions.
    /// </summary>
    /// <returns>List of formatted rule descriptions.</returns>
    private IReadOnlyList<string> FormatAllRules()
    {
        var formatted = new List<string>();
        foreach (var rule in _axiom.Rules)
        {
            formatted.Add(FormatRuleDescription(rule));
        }
        return formatted;
    }

    /// <summary>
    /// Formats a single axiom rule into a human-readable description.
    /// </summary>
    /// <param name="rule">The axiom rule to format.</param>
    /// <returns>Human-readable description of the rule.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Converts constraint types to natural language. The format varies
    /// by constraint type:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>Required</c>: "'property' is required"</item>
    ///   <item><c>OneOf</c>: "'property' must be one of: a, b, c"</item>
    ///   <item><c>Pattern</c>: "'property' must match pattern: ^regex$"</item>
    ///   <item><c>Range</c>: "'property' must be between min and max"</item>
    ///   <item><c>Cardinality</c>: "'property' must have 1-10 items"</item>
    ///   <item><c>NotBoth</c>: "'a' and 'b' cannot both be set"</item>
    ///   <item><c>RequiresTogether</c>: "'a' and 'b' must be set together"</item>
    /// </list>
    /// <para>
    /// If a rule has a custom error message, it is appended to the description.
    /// If a rule has a <c>When</c> condition, the condition is formatted and appended.
    /// </para>
    /// </remarks>
    public static string FormatRuleDescription(AxiomRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var description = rule.Constraint switch
        {
            AxiomConstraintType.Required => FormatRequired(rule),
            AxiomConstraintType.OneOf => FormatOneOf(rule),
            AxiomConstraintType.NotOneOf => FormatNotOneOf(rule),
            AxiomConstraintType.Range => FormatRange(rule),
            AxiomConstraintType.Pattern => FormatPattern(rule),
            AxiomConstraintType.Cardinality => FormatCardinality(rule),
            AxiomConstraintType.NotBoth => FormatNotBoth(rule),
            AxiomConstraintType.RequiresTogether => FormatRequiresTogether(rule),
            AxiomConstraintType.Equals => FormatEquals(rule),
            AxiomConstraintType.NotEquals => FormatNotEquals(rule),
            AxiomConstraintType.Unique => FormatUnique(rule),
            AxiomConstraintType.ReferenceExists => FormatReferenceExists(rule),
            AxiomConstraintType.TypeValid => FormatTypeValid(rule),
            AxiomConstraintType.Custom => FormatCustom(rule),
            _ => $"Unknown constraint: {rule.Constraint}"
        };

        // LOGIC: Append conditional clause if present.
        if (rule.When is not null)
        {
            description += $" (when {FormatCondition(rule.When)})";
        }

        return description;
    }

    /// <summary>
    /// Formats a conditional clause into a human-readable description.
    /// </summary>
    /// <param name="condition">The condition to format.</param>
    /// <returns>Human-readable description of the condition.</returns>
    /// <remarks>
    /// LOGIC: Converts condition operators to natural language:
    /// <list type="bullet">
    ///   <item><c>Equals</c>: "'property' = value"</item>
    ///   <item><c>NotEquals</c>: "'property' ≠ value"</item>
    ///   <item><c>Contains</c>: "'property' contains value"</item>
    ///   <item><c>IsNull</c>: "'property' is null"</item>
    ///   <item><c>IsNotNull</c>: "'property' is not null"</item>
    /// </list>
    /// </remarks>
    public static string FormatCondition(AxiomCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var property = $"'{condition.Property}'";
        var value = FormatValue(condition.Value);

        return condition.Operator switch
        {
            ConditionOperator.Equals => $"{property} = {value}",
            ConditionOperator.NotEquals => $"{property} ≠ {value}",
            ConditionOperator.Contains => $"{property} contains {value}",
            ConditionOperator.StartsWith => $"{property} starts with {value}",
            ConditionOperator.EndsWith => $"{property} ends with {value}",
            ConditionOperator.GreaterThan => $"{property} > {value}",
            ConditionOperator.LessThan => $"{property} < {value}",
            ConditionOperator.IsNull => $"{property} is null",
            ConditionOperator.IsNotNull => $"{property} is not null",
            _ => $"{property} {condition.Operator} {value}"
        };
    }

    #endregion

    #region Constraint Type Formatting Helpers

    private static string FormatRequired(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        return $"'{property}' is required";
    }

    private static string FormatOneOf(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var values = FormatValueList(rule.Values);
        return $"'{property}' must be one of: {values}";
    }

    private static string FormatNotOneOf(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var values = FormatValueList(rule.Values);
        return $"'{property}' must not be one of: {values}";
    }

    private static string FormatRange(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var min = rule.Min is not null ? FormatValue(rule.Min) : "∞";
        var max = rule.Max is not null ? FormatValue(rule.Max) : "∞";

        if (rule.Min is null && rule.Max is not null)
        {
            return $"'{property}' must be at most {max}";
        }
        if (rule.Min is not null && rule.Max is null)
        {
            return $"'{property}' must be at least {min}";
        }
        return $"'{property}' must be between {min} and {max}";
    }

    private static string FormatPattern(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var pattern = rule.Pattern ?? ".*";
        return $"'{property}' must match pattern: {pattern}";
    }

    private static string FormatCardinality(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var min = rule.MinCount ?? 0;
        var max = rule.MaxCount;

        if (max is null)
        {
            return $"'{property}' must have at least {min} item(s)";
        }
        if (min == 0)
        {
            return $"'{property}' must have at most {max} item(s)";
        }
        return $"'{property}' must have {min}-{max} items";
    }

    private static string FormatNotBoth(AxiomRule rule)
    {
        if (rule.Properties is null || rule.Properties.Count < 2)
        {
            return "Two properties cannot both be set";
        }
        var props = string.Join("' and '", rule.Properties);
        return $"'{props}' cannot both be set";
    }

    private static string FormatRequiresTogether(AxiomRule rule)
    {
        if (rule.Properties is null || rule.Properties.Count < 2)
        {
            return "Properties must be set together";
        }
        var props = string.Join("' and '", rule.Properties);
        return $"'{props}' must be set together";
    }

    private static string FormatEquals(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var value = rule.Values?.Count > 0 ? FormatValue(rule.Values[0]) : "value";
        return $"'{property}' must equal {value}";
    }

    private static string FormatNotEquals(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var value = rule.Values?.Count > 0 ? FormatValue(rule.Values[0]) : "value";
        return $"'{property}' must not equal {value}";
    }

    private static string FormatUnique(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        return $"'{property}' must be unique";
    }

    private static string FormatReferenceExists(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        var refType = rule.ReferenceType ?? "entity";
        return $"'{property}' must reference an existing {refType}";
    }

    private static string FormatTypeValid(AxiomRule rule)
    {
        var property = rule.Property ?? "property";
        return $"'{property}' must have a valid type";
    }

    private static string FormatCustom(AxiomRule rule)
    {
        // LOGIC: Custom constraints use the ErrorMessage property for description.
        if (!string.IsNullOrEmpty(rule.ErrorMessage))
        {
            return rule.ErrorMessage;
        }
        var property = rule.Property ?? "property";
        return $"'{property}' must satisfy custom validation";
    }

    #endregion

    #region Value Formatting Helpers

    /// <summary>
    /// Formats a value for display in rule descriptions.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>Formatted string representation.</returns>
    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b.ToString().ToLowerInvariant(),
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Formats a list of values for display in rule descriptions.
    /// </summary>
    /// <param name="values">The values to format.</param>
    /// <returns>Comma-separated formatted string.</returns>
    private static string FormatValueList(IReadOnlyList<object>? values)
    {
        if (values is null || values.Count == 0)
        {
            return "(none)";
        }
        return string.Join(", ", values.Select(FormatValue));
    }

    #endregion
}
