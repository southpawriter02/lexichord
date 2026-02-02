// =============================================================================
// File: AxiomYamlModels.cs
// Project: Lexichord.Modules.Knowledge
// Description: YAML deserialization models for axiom definitions.
// =============================================================================
// LOGIC: Internal record types matching the YAML schema for axiom files.
//   - AxiomYamlFile: Root document structure with version and axiom list
//   - AxiomYamlEntry: Single axiom definition
//   - AxiomRuleYamlEntry: Single rule within an axiom
//   - AxiomConditionYamlEntry: Conditional clause for rules
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// =============================================================================

using YamlDotNet.Serialization;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// YAML file structure for axiom definitions.
/// </summary>
/// <remarks>
/// LOGIC: Maps to the root structure of axiom YAML files:
/// <code>
/// axiom_version: "1.0"
/// name: "API Documentation"
/// axioms:
///   - id: "endpoint-method"
///     ...
/// </code>
/// </remarks>
internal record AxiomYamlFile
{
    /// <summary>Axiom schema version (e.g., "1.0").</summary>
    [YamlMember(Alias = "axiom_version")]
    public string AxiomVersion { get; init; } = "1.0";

    /// <summary>Optional name for the axiom collection.</summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; init; }

    /// <summary>Optional description for the axiom collection.</summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; init; }

    /// <summary>List of axiom definitions.</summary>
    [YamlMember(Alias = "axioms")]
    public List<AxiomYamlEntry> Axioms { get; init; } = new();
}

/// <summary>
/// Single axiom entry in YAML.
/// </summary>
/// <remarks>
/// LOGIC: Maps to individual axiom definitions within the axioms list.
/// Example:
/// <code>
/// - id: "endpoint-must-have-method"
///   name: "Endpoint Method Required"
///   target_type: Endpoint
///   severity: error
///   rules:
///     - property: method
///       constraint: required
/// </code>
/// </remarks>
internal record AxiomYamlEntry
{
    /// <summary>Unique axiom identifier.</summary>
    [YamlMember(Alias = "id")]
    public string Id { get; init; } = "";

    /// <summary>Human-readable axiom name.</summary>
    [YamlMember(Alias = "name")]
    public string Name { get; init; } = "";

    /// <summary>Optional description explaining the axiom purpose.</summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; init; }

    /// <summary>Target entity type (e.g., "Endpoint", "Parameter").</summary>
    [YamlMember(Alias = "target_type")]
    public string TargetType { get; init; } = "";

    /// <summary>Target kind: "Entity" or "Relationship".</summary>
    [YamlMember(Alias = "target_kind")]
    public string TargetKind { get; init; } = "Entity";

    /// <summary>Severity: "error", "warning", or "info".</summary>
    [YamlMember(Alias = "severity")]
    public string Severity { get; init; } = "error";

    /// <summary>Optional category for grouping axioms.</summary>
    [YamlMember(Alias = "category")]
    public string? Category { get; init; }

    /// <summary>Tags for filtering and search.</summary>
    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; init; } = new();

    /// <summary>Whether the axiom is enabled.</summary>
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; init; } = true;

    /// <summary>List of rules that define the axiom constraints.</summary>
    [YamlMember(Alias = "rules")]
    public List<AxiomRuleYamlEntry> Rules { get; init; } = new();
}

/// <summary>
/// Single rule entry in YAML.
/// </summary>
/// <remarks>
/// LOGIC: Defines a constraint rule with optional conditions.
/// Properties are mutually exclusive: use <c>property</c> for single-property
/// constraints or <c>properties</c> for multi-property constraints.
/// </remarks>
internal record AxiomRuleYamlEntry
{
    /// <summary>Single property to validate.</summary>
    [YamlMember(Alias = "property")]
    public string? Property { get; init; }

    /// <summary>Multiple properties for combined constraints (e.g., not_both).</summary>
    [YamlMember(Alias = "properties")]
    public List<string>? Properties { get; init; }

    /// <summary>Constraint type (e.g., "required", "one_of", "range").</summary>
    [YamlMember(Alias = "constraint")]
    public string Constraint { get; init; } = "";

    /// <summary>Valid values for one_of/not_one_of constraints.</summary>
    [YamlMember(Alias = "values")]
    public List<object>? Values { get; init; }

    /// <summary>Minimum value for range constraints.</summary>
    [YamlMember(Alias = "min")]
    public object? Min { get; init; }

    /// <summary>Maximum value for range constraints.</summary>
    [YamlMember(Alias = "max")]
    public object? Max { get; init; }

    /// <summary>Regex pattern for pattern constraints.</summary>
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; init; }

    /// <summary>Minimum count for cardinality constraints.</summary>
    [YamlMember(Alias = "min_count")]
    public int? MinCount { get; init; }

    /// <summary>Maximum count for cardinality constraints.</summary>
    [YamlMember(Alias = "max_count")]
    public int? MaxCount { get; init; }

    /// <summary>Conditional clause for conditional rules.</summary>
    [YamlMember(Alias = "when")]
    public AxiomConditionYamlEntry? When { get; init; }

    /// <summary>Custom error message for violations.</summary>
    [YamlMember(Alias = "error_message")]
    public string? ErrorMessage { get; init; }

    /// <summary>Referenced type for reference_exists constraints.</summary>
    [YamlMember(Alias = "reference_type")]
    public string? ReferenceType { get; init; }
}

/// <summary>
/// Condition entry in YAML.
/// </summary>
/// <remarks>
/// LOGIC: Defines when a rule should be applied based on property values.
/// Example:
/// <code>
/// when:
///   property: required
///   operator: equals
///   value: true
/// </code>
/// </remarks>
internal record AxiomConditionYamlEntry
{
    /// <summary>Property to evaluate.</summary>
    [YamlMember(Alias = "property")]
    public string Property { get; init; } = "";

    /// <summary>Comparison operator: "equals", "not_equals", "contains", etc.</summary>
    [YamlMember(Alias = "operator")]
    public string Operator { get; init; } = "equals";

    /// <summary>Value to compare against.</summary>
    [YamlMember(Alias = "value")]
    public object Value { get; init; } = "";
}
