// =============================================================================
// File: UnifiedFindingAdapter.cs
// Project: Lexichord.Modules.Knowledge
// Description: Adapts validation and linter findings to unified format.
// =============================================================================
// LOGIC: Stateless adapter that maps source-specific types to the unified
//   model. Each source has its own severity mapping and category mapping:
//
//   ValidationFinding → UnifiedFinding:
//     - ValidatorId "schema.*" → Schema, "axiom.*" → Axiom,
//       "consistency.*" → Consistency, default → Schema
//     - ValidationSeverity: Error→Error, Warning→Warning, Info→Info
//     - PropertyPath carried over directly
//     - SuggestedFix (string?) → UnifiedFix with Confidence=0.5
//
//   StyleViolation → UnifiedFinding:
//     - RuleCategory: Terminology→Style, Formatting→Style, Syntax→Style
//     - ViolationSeverity: Error→Error, Warning→Warning, Info→Info, Hint→Hint
//     - Location = "Line {StartLine}, Col {StartColumn}"
//     - Suggestion → UnifiedFix with Confidence=0.7
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Integration;

/// <summary>
/// Adapts validation and linter findings to the <see cref="UnifiedFinding"/> format.
/// </summary>
/// <remarks>
/// <para>
/// Stateless singleton. Thread-safe — no mutable state.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public sealed class UnifiedFindingAdapter : IUnifiedFindingAdapter
{
    // =========================================================================
    // Fields
    // =========================================================================

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<UnifiedFindingAdapter> _logger;

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedFindingAdapter"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is null.</exception>
    public UnifiedFindingAdapter(ILogger<UnifiedFindingAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Log construction for DI verification during startup.
        _logger.LogDebug("[v0.6.5j] UnifiedFindingAdapter constructed.");
    }

    // =========================================================================
    // IUnifiedFindingAdapter — FromValidationFinding
    // =========================================================================

    /// <inheritdoc />
    public UnifiedFinding FromValidationFinding(ValidationFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        // LOGIC: Map ValidatorId prefix to FindingCategory.
        var category = MapValidatorIdToCategory(finding.ValidatorId);

        // LOGIC: Normalize ValidationSeverity → UnifiedSeverity.
        var severity = NormalizeSeverity(finding.Severity);

        // LOGIC: Create UnifiedFix from SuggestedFix string, if present.
        var findingId = Guid.NewGuid();
        UnifiedFix? suggestedFix = null;
        if (!string.IsNullOrEmpty(finding.SuggestedFix))
        {
            suggestedFix = new UnifiedFix
            {
                Source = FindingSource.Validation,
                Description = finding.SuggestedFix,
                ReplacementText = finding.SuggestedFix,
                Confidence = 0.5f,
                CanAutoApply = false,
                FindingId = findingId
            };
        }

        _logger.LogTrace(
            "[v0.6.5j] Adapted ValidationFinding: Code={Code}, Severity={Severity}, Category={Category}.",
            finding.Code, severity, category);

        return new UnifiedFinding
        {
            Id = findingId,
            Source = FindingSource.Validation,
            Severity = severity,
            Code = finding.Code,
            Message = finding.Message,
            PropertyPath = finding.PropertyPath,
            Category = category,
            SuggestedFix = suggestedFix,
            OriginalValidationFinding = finding,
            OriginalStyleViolation = null
        };
    }

    // =========================================================================
    // IUnifiedFindingAdapter — FromStyleViolation
    // =========================================================================

    /// <inheritdoc />
    public UnifiedFinding FromStyleViolation(StyleViolation violation)
    {
        ArgumentNullException.ThrowIfNull(violation);

        // LOGIC: All style linter categories map to FindingCategory.Style.
        var category = FindingCategory.Style;

        // LOGIC: Normalize ViolationSeverity → UnifiedSeverity.
        var severity = NormalizeSeverity(violation.Severity);

        // LOGIC: Build a human-readable location string from line/column.
        var propertyPath = $"Line {violation.StartLine}, Col {violation.StartColumn}";

        // LOGIC: Create UnifiedFix from Suggestion, if present.
        var findingId = Guid.NewGuid();
        UnifiedFix? suggestedFix = null;
        if (!string.IsNullOrEmpty(violation.Suggestion))
        {
            suggestedFix = new UnifiedFix
            {
                Source = FindingSource.StyleLinter,
                Description = $"Replace '{violation.MatchedText}' with '{violation.Suggestion}'",
                ReplacementText = violation.Suggestion,
                Confidence = 0.7f,
                CanAutoApply = true,
                FindingId = findingId
            };
        }

        _logger.LogTrace(
            "[v0.6.5j] Adapted StyleViolation: RuleId={RuleId}, Severity={Severity}, Location={Location}.",
            violation.Rule.Id, severity, propertyPath);

        return new UnifiedFinding
        {
            Id = findingId,
            Source = FindingSource.StyleLinter,
            Severity = severity,
            Code = violation.Rule.Id,
            Message = violation.Message,
            PropertyPath = propertyPath,
            Category = category,
            SuggestedFix = suggestedFix,
            OriginalValidationFinding = null,
            OriginalStyleViolation = violation
        };
    }

    // =========================================================================
    // IUnifiedFindingAdapter — NormalizeSeverity
    // =========================================================================

    /// <inheritdoc />
    public UnifiedSeverity NormalizeSeverity(ValidationSeverity severity)
    {
        // LOGIC: ValidationSeverity has 3 levels: Info(0), Warning(1), Error(2).
        // Map directly to the unified model.
        return severity switch
        {
            ValidationSeverity.Error => UnifiedSeverity.Error,
            ValidationSeverity.Warning => UnifiedSeverity.Warning,
            ValidationSeverity.Info => UnifiedSeverity.Info,
            _ => UnifiedSeverity.Info
        };
    }

    /// <inheritdoc />
    public UnifiedSeverity NormalizeSeverity(ViolationSeverity severity)
    {
        // LOGIC: ViolationSeverity has 4 levels: Error(0), Warning(1), Info(2), Hint(3).
        // Map directly — same numeric values as UnifiedSeverity.
        return severity switch
        {
            ViolationSeverity.Error => UnifiedSeverity.Error,
            ViolationSeverity.Warning => UnifiedSeverity.Warning,
            ViolationSeverity.Info => UnifiedSeverity.Info,
            ViolationSeverity.Hint => UnifiedSeverity.Hint,
            _ => UnifiedSeverity.Info
        };
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Maps a ValidatorId to a <see cref="FindingCategory"/>.
    /// </summary>
    /// <param name="validatorId">The validator identifier (e.g., "schema", "axiom.core").</param>
    /// <returns>The corresponding <see cref="FindingCategory"/>.</returns>
    /// <remarks>
    /// LOGIC: Uses StartsWith matching to support hierarchical validator IDs.
    /// Default fallback is Schema for unknown validators.
    /// </remarks>
    private static FindingCategory MapValidatorIdToCategory(string validatorId)
    {
        if (string.IsNullOrEmpty(validatorId))
            return FindingCategory.Schema;

        var lower = validatorId.ToLowerInvariant();

        if (lower.StartsWith("schema", StringComparison.Ordinal))
            return FindingCategory.Schema;

        if (lower.StartsWith("axiom", StringComparison.Ordinal))
            return FindingCategory.Axiom;

        if (lower.StartsWith("consistency", StringComparison.Ordinal))
            return FindingCategory.Consistency;

        return FindingCategory.Schema;
    }
}
