// =============================================================================
// File: AxiomYamlParser.cs
// Project: Lexichord.Modules.Knowledge
// Description: Parses axiom YAML files into Axiom domain records.
// =============================================================================
// LOGIC: Transforms YAML content into validated Axiom and AxiomRule records.
//   - Uses YamlDotNet with underscored naming convention
//   - Maps string constraint types to AxiomConstraintType enum
//   - Maps string condition operators to ConditionOperator enum
//   - Captures YAML syntax errors with line/column information
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// Dependencies: Axiom (v0.4.6e), YamlDotNet
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Knowledge.Axioms;

/// <summary>
/// Parses axiom YAML files into Axiom records.
/// </summary>
/// <remarks>
/// LOGIC: The parser uses YamlDotNet for deserialization with the following configuration:
/// - Underscored naming convention (axiom_version → AxiomVersion)
/// - Ignore unmatched properties for forward compatibility
/// - Capture syntax errors with precise location information
/// </remarks>
internal sealed class AxiomYamlParser
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxiomYamlParser"/> class.
    /// </summary>
    public AxiomYamlParser()
    {
        // LOGIC: Configure deserializer with underscored naming convention.
        // This allows YAML keys like "target_type" to map to C# properties "TargetType".
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses YAML content into Axiom records.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <param name="sourcePath">Source file path for error reporting.</param>
    /// <returns>Parse result with axioms and any errors encountered.</returns>
    public AxiomParseResult Parse(string yaml, string sourcePath)
    {
        var errors = new List<AxiomLoadError>();
        var axioms = new List<Axiom>();

        try
        {
            var file = _deserializer.Deserialize<AxiomYamlFile>(yaml);

            // LOGIC: Handle empty or null axiom list.
            if (file?.Axioms == null || file.Axioms.Count == 0)
            {
                errors.Add(new AxiomLoadError
                {
                    FilePath = sourcePath,
                    Code = "EMPTY_FILE",
                    Message = "No axioms found in file",
                    Severity = LoadErrorSeverity.Warning
                });
                return new AxiomParseResult(axioms, errors);
            }

            // LOGIC: Parse each axiom entry, isolating errors to individual entries.
            foreach (var entry in file.Axioms)
            {
                try
                {
                    var axiom = MapToAxiom(entry, file.Name);
                    axioms.Add(axiom);
                }
                catch (Exception ex)
                {
                    errors.Add(new AxiomLoadError
                    {
                        FilePath = sourcePath,
                        Code = "AXIOM_PARSE_ERROR",
                        Message = $"Failed to parse axiom '{entry.Id}': {ex.Message}",
                        AxiomId = entry.Id,
                        Severity = LoadErrorSeverity.Error
                    });
                }
            }
        }
        catch (YamlException ex)
        {
            // LOGIC: Capture YAML syntax errors with precise location.
            errors.Add(new AxiomLoadError
            {
                FilePath = sourcePath,
                Line = ex.Start.Line,
                Column = ex.Start.Column,
                Code = "YAML_SYNTAX_ERROR",
                Message = ex.Message,
                Severity = LoadErrorSeverity.Error
            });
        }
        catch (Exception ex)
        {
            // LOGIC: Capture unexpected parsing errors.
            errors.Add(new AxiomLoadError
            {
                FilePath = sourcePath,
                Code = "PARSE_ERROR",
                Message = $"Unexpected error parsing YAML: {ex.Message}",
                Severity = LoadErrorSeverity.Error
            });
        }

        return new AxiomParseResult(axioms, errors);
    }

    /// <summary>
    /// Maps a YAML entry to an Axiom domain record.
    /// </summary>
    /// <param name="entry">The YAML entry to map.</param>
    /// <param name="fileCategory">Optional category from file-level name.</param>
    /// <returns>The mapped Axiom record.</returns>
    private static Axiom MapToAxiom(AxiomYamlEntry entry, string? fileCategory)
    {
        return new Axiom
        {
            Id = entry.Id,
            Name = entry.Name,
            Description = entry.Description,
            TargetType = entry.TargetType,
            TargetKind = ParseTargetKind(entry.TargetKind),
            Severity = ParseSeverity(entry.Severity),
            Category = entry.Category ?? fileCategory,
            Tags = entry.Tags,
            IsEnabled = entry.Enabled,
            Rules = entry.Rules.Select(MapToRule).ToList()
        };
    }

    /// <summary>
    /// Maps a YAML rule entry to an AxiomRule domain record.
    /// </summary>
    /// <param name="entry">The YAML rule entry to map.</param>
    /// <returns>The mapped AxiomRule record.</returns>
    private static AxiomRule MapToRule(AxiomRuleYamlEntry entry)
    {
        return new AxiomRule
        {
            Property = entry.Property,
            Properties = entry.Properties,
            Constraint = ParseConstraintType(entry.Constraint),
            Values = entry.Values,
            Min = entry.Min,
            Max = entry.Max,
            Pattern = entry.Pattern,
            MinCount = entry.MinCount,
            MaxCount = entry.MaxCount,
            When = entry.When != null ? MapToCondition(entry.When) : null,
            ErrorMessage = entry.ErrorMessage,
            ReferenceType = entry.ReferenceType
        };
    }

    /// <summary>
    /// Maps a YAML condition entry to an AxiomCondition domain record.
    /// </summary>
    /// <param name="entry">The YAML condition entry to map.</param>
    /// <returns>The mapped AxiomCondition record.</returns>
    private static AxiomCondition MapToCondition(AxiomConditionYamlEntry entry)
    {
        return new AxiomCondition
        {
            Property = entry.Property,
            Operator = ParseConditionOperator(entry.Operator),
            Value = entry.Value
        };
    }

    /// <summary>
    /// Parses target kind from string.
    /// </summary>
    /// <param name="value">String value (e.g., "Entity", "Relationship").</param>
    /// <returns>Parsed enum value, defaulting to Entity.</returns>
    private static AxiomTargetKind ParseTargetKind(string value) =>
        Enum.TryParse<AxiomTargetKind>(value, ignoreCase: true, out var result)
            ? result
            : AxiomTargetKind.Entity;

    /// <summary>
    /// Parses severity from string.
    /// </summary>
    /// <param name="value">String value (e.g., "error", "warning", "info").</param>
    /// <returns>Parsed enum value, defaulting to Error.</returns>
    private static AxiomSeverity ParseSeverity(string value) =>
        Enum.TryParse<AxiomSeverity>(value, ignoreCase: true, out var result)
            ? result
            : AxiomSeverity.Error;

    /// <summary>
    /// Parses constraint type from string.
    /// </summary>
    /// <param name="value">String constraint type (e.g., "required", "one_of").</param>
    /// <returns>Parsed enum value.</returns>
    /// <exception cref="ArgumentException">Thrown for unknown constraint types.</exception>
    /// <remarks>
    /// LOGIC: Supports both underscored (one_of) and camelCase (oneOf) variants.
    /// </remarks>
    private static AxiomConstraintType ParseConstraintType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "required" => AxiomConstraintType.Required,
            "one_of" or "oneof" => AxiomConstraintType.OneOf,
            "not_one_of" or "notoneof" => AxiomConstraintType.NotOneOf,
            "range" => AxiomConstraintType.Range,
            "pattern" => AxiomConstraintType.Pattern,
            "cardinality" => AxiomConstraintType.Cardinality,
            "not_both" or "notboth" => AxiomConstraintType.NotBoth,
            "requires_together" or "requirestogether" => AxiomConstraintType.RequiresTogether,
            "equals" => AxiomConstraintType.Equals,
            "not_equals" or "notequals" => AxiomConstraintType.NotEquals,
            "unique" => AxiomConstraintType.Unique,
            "reference_exists" or "referenceexists" => AxiomConstraintType.ReferenceExists,
            "type_valid" or "typevalid" => AxiomConstraintType.TypeValid,
            "custom" => AxiomConstraintType.Custom,
            _ => throw new ArgumentException($"Unknown constraint type: {value}")
        };
    }

    /// <summary>
    /// Parses condition operator from string.
    /// </summary>
    /// <param name="value">String operator (e.g., "equals", "contains").</param>
    /// <returns>Parsed enum value, defaulting to Equals.</returns>
    /// <remarks>
    /// LOGIC: Supports symbolic operators (==, !=, &gt;, &lt;) in addition to names.
    /// </remarks>
    private static ConditionOperator ParseConditionOperator(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "equals" or "=" or "==" => ConditionOperator.Equals,
            "not_equals" or "notequals" or "!=" or "<>" => ConditionOperator.NotEquals,
            "contains" => ConditionOperator.Contains,
            "starts_with" or "startswith" => ConditionOperator.StartsWith,
            "ends_with" or "endswith" => ConditionOperator.EndsWith,
            "greater_than" or "greaterthan" or ">" or "gt" => ConditionOperator.GreaterThan,
            "less_than" or "lessthan" or "<" or "lt" => ConditionOperator.LessThan,
            "is_null" or "isnull" => ConditionOperator.IsNull,
            "is_not_null" or "isnotnull" => ConditionOperator.IsNotNull,
            _ => ConditionOperator.Equals
        };
    }
}

/// <summary>
/// Result of parsing an axiom YAML file.
/// </summary>
/// <param name="Axioms">Successfully parsed axioms.</param>
/// <param name="Errors">Errors encountered during parsing.</param>
internal record AxiomParseResult(
    IReadOnlyList<Axiom> Axioms,
    IReadOnlyList<AxiomLoadError> Errors);
