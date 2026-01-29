using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Yaml;

/// <summary>
/// Validates YAML style sheet content against the schema.
/// </summary>
/// <remarks>
/// LOGIC: Provides detailed, user-friendly error messages for YAML validation.
/// Checks required fields, valid patterns, enum values, and uniqueness constraints.
///
/// Design Decisions:
/// - Separate from deserialization to collect ALL errors before failing
/// - Line-contextual messages where possible
/// - Lenient on optional fields with sensible defaults
/// </remarks>
public static class YamlSchemaValidator
{
    /// <summary>
    /// Pattern for valid rule IDs: kebab-case starting with letter.
    /// </summary>
    private static readonly Regex ValidIdPattern = new(
        @"^[a-z][a-z0-9-]*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Valid category values (case-insensitive).
    /// </summary>
    private static readonly HashSet<string> ValidCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "terminology", "formatting", "syntax"
    };

    /// <summary>
    /// Valid severity values (case-insensitive).
    /// </summary>
    private static readonly HashSet<string> ValidSeverities = new(StringComparer.OrdinalIgnoreCase)
    {
        "error", "warning", "info", "hint"
    };

    /// <summary>
    /// Valid pattern type values (case-insensitive).
    /// </summary>
    private static readonly HashSet<string> ValidPatternTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "regex", "literal", "literal_ignore_case", "starts_with", "ends_with", "contains"
    };

    /// <summary>
    /// Validates a deserialized YAML style sheet.
    /// </summary>
    /// <param name="sheet">The deserialized DTO.</param>
    /// <returns>List of validation errors (empty if valid).</returns>
    public static List<ValidationError> Validate(YamlStyleSheet? sheet)
    {
        var errors = new List<ValidationError>();

        if (sheet is null)
        {
            errors.Add(new ValidationError("YAML content is empty or invalid", null, null, null));
            return errors;
        }

        // Required: name
        if (string.IsNullOrWhiteSpace(sheet.Name))
        {
            errors.Add(new ValidationError("Missing required field 'name'", null, "name", null));
        }

        // Validate rules
        if (sheet.Rules is null || sheet.Rules.Count == 0)
        {
            errors.Add(new ValidationError("Style sheet must have at least one rule", null, "rules", null));
        }
        else
        {
            ValidateRules(sheet.Rules, errors);
        }

        return errors;
    }

    private static void ValidateRules(List<YamlRule> rules, List<ValidationError> errors)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var ruleContext = $"rules[{i}]";

            // Required: id
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Missing required field 'id'",
                    null, "id", null));
            }
            else
            {
                // Validate kebab-case format
                if (!ValidIdPattern.IsMatch(rule.Id))
                {
                    errors.Add(new ValidationError(
                        $"{ruleContext}: 'id' must be kebab-case (e.g., 'no-passive-voice'), got '{rule.Id}'",
                        null, "id", rule.Id));
                }

                // Check for duplicates
                if (!seenIds.Add(rule.Id))
                {
                    errors.Add(new ValidationError(
                        $"{ruleContext}: Duplicate rule id '{rule.Id}'",
                        null, "id", rule.Id));
                }
            }

            // Required: name
            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Missing required field 'name'",
                    null, "name", null));
            }

            // Required: description
            if (string.IsNullOrWhiteSpace(rule.Description))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Missing required field 'description'",
                    null, "description", null));
            }

            // Required: pattern
            if (string.IsNullOrWhiteSpace(rule.Pattern))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Missing required field 'pattern'",
                    null, "pattern", null));
            }

            // Optional: category (validate if present)
            if (!string.IsNullOrWhiteSpace(rule.Category) && !ValidCategories.Contains(rule.Category))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Invalid category '{rule.Category}'. Valid values: terminology, formatting, syntax",
                    null, "category", rule.Category));
            }

            // Optional: severity (validate if present)
            if (!string.IsNullOrWhiteSpace(rule.Severity) && !ValidSeverities.Contains(rule.Severity))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Invalid severity '{rule.Severity}'. Valid values: error, warning, info, hint",
                    null, "severity", rule.Severity));
            }

            // Optional: pattern_type (validate if present)
            if (!string.IsNullOrWhiteSpace(rule.PatternType) && !ValidPatternTypes.Contains(rule.PatternType))
            {
                errors.Add(new ValidationError(
                    $"{ruleContext}: Invalid pattern_type '{rule.PatternType}'. Valid values: regex, literal, literal_ignore_case, starts_with, ends_with, contains",
                    null, "pattern_type", rule.PatternType));
            }

            // Validate regex pattern if pattern_type is regex (or default)
            if ((string.IsNullOrWhiteSpace(rule.PatternType) || 
                 rule.PatternType.Equals("regex", StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrWhiteSpace(rule.Pattern))
            {
                try
                {
                    _ = new Regex(rule.Pattern);
                }
                catch (ArgumentException ex)
                {
                    errors.Add(new ValidationError(
                        $"{ruleContext}: Invalid regex pattern: {ex.Message}",
                        null, "pattern", rule.Pattern));
                }
            }
        }
    }
}

/// <summary>
/// Represents a schema validation error.
/// </summary>
/// <param name="Message">Human-readable error description.</param>
/// <param name="Line">Line number in YAML (if available).</param>
/// <param name="Field">Field name that failed validation.</param>
/// <param name="Value">The invalid value (if applicable).</param>
public sealed record ValidationError(
    string Message,
    int? Line,
    string? Field,
    string? Value);
