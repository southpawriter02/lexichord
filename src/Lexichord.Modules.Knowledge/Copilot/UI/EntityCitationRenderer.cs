// =============================================================================
// File: EntityCitationRenderer.cs
// Project: Lexichord.Modules.Knowledge
// Description: Renders entity citations for Co-pilot responses.
// =============================================================================
// LOGIC: Implements IEntityCitationRenderer by:
//   1. Building EntityCitation records from source entities, checking each
//      against validation findings for verification status.
//   2. Applying type-specific icons and display labels.
//   3. Optionally grouping and sorting by entity type.
//   4. Formatting output in Compact/Detailed/TreeView layouts.
//   5. Detecting used properties by substring matching in generated content.
//   6. Extracting derived claims from post-validation results.
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: IEntityCitationRenderer (v0.6.6h),
//               ValidatedGenerationResult (v0.6.6h),
//               PostValidationResult (v0.6.6g),
//               PostValidationStatus (v0.6.6g),
//               KnowledgeEntity (v0.4.5e)
//
// Spec Deviations:
//   - FormatValidationStatus uses PostValidationStatus (spec's ValidationStatus
//     enum does not exist).
//   - GetValidationIcon accepts PostValidationStatus directly instead of
//     object (simplified from spec's polymorphic approach).
// =============================================================================

using System.Text;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.UI;

/// <summary>
/// Renders entity citations for Co-pilot responses.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IEntityCitationRenderer"/> to transform validated
/// generation results into user-facing citation markup. Stateless and
/// thread-safe for concurrent use across multiple responses.
/// </para>
/// <para>
/// <b>Type Icons:</b> Entities are assigned emoji icons based on their type:
/// Endpoint → 🔗, Parameter → 📝, Response → 📤, Schema → 📋,
/// Entity → 📦, Error → ⚠️, default → 📄.
/// </para>
/// <para>
/// <b>Verification:</b> An entity is considered verified if no error-level
/// validation findings reference it by ID. This is checked against
/// <see cref="PostValidationResult.Findings"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public class EntityCitationRenderer : IEntityCitationRenderer
{
    // ── Type icon mapping ────────────────────────────────────────────
    // LOGIC: Maps entity type names to Unicode emoji icons for visual
    // differentiation in the citation panel. Types not in this map
    // receive the generic 📄 icon.
    private static readonly Dictionary<string, string> TypeIcons = new()
    {
        ["Endpoint"] = "🔗",
        ["Parameter"] = "📝",
        ["Response"] = "📤",
        ["Schema"] = "📋",
        ["Entity"] = "📦",
        ["Error"] = "⚠️"
    };

    private readonly ILogger<EntityCitationRenderer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityCitationRenderer"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    public EntityCitationRenderer(ILogger<EntityCitationRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public CitationMarkup GenerateCitations(
        ValidatedGenerationResult result,
        CitationOptions options)
    {
        _logger.LogInformation(
            "Generating citations for {EntityCount} source entities with format={Format}, max={Max}",
            result.SourceEntities.Count, options.Format, options.MaxCitations);

        var citations = new List<EntityCitation>();

        // Build citations from source entities (up to MaxCitations)
        foreach (var entity in result.SourceEntities.Take(options.MaxCitations))
        {
            // LOGIC: An entity is "verified" if no error-level findings
            // reference it. We check RelatedEntity on each finding; since
            // ValidationFinding doesn't have a RelatedEntity property, we
            // verify using entity name matching in findings messages as a proxy.
            var isVerified = IsEntityVerified(entity, result.PostValidation);

            citations.Add(new EntityCitation
            {
                EntityId = entity.Id,
                EntityType = entity.Type,
                EntityName = entity.Name,
                DisplayLabel = FormatDisplayLabel(entity),
                Confidence = 1.0f, // From verified context
                IsVerified = isVerified,
                TypeIcon = TypeIcons.GetValueOrDefault(entity.Type, "📄")
            });

            _logger.LogDebug(
                "Citation: {EntityType} '{EntityName}' verified={Verified}",
                entity.Type, entity.Name, isVerified);
        }

        // Group by type if requested
        if (options.GroupByType)
        {
            citations = citations
                .OrderBy(c => c.EntityType)
                .ThenBy(c => c.EntityName)
                .ToList();

            _logger.LogDebug("Citations grouped by type, {Count} total", citations.Count);
        }

        // Determine validation status and icon from post-validation
        var validationStatus = FormatValidationStatus(result.PostValidation);
        var icon = GetValidationIcon(result.PostValidation.Status);

        // Format output per requested format
        var formattedMarkup = options.Format switch
        {
            CitationFormat.Compact => FormatCompact(citations, validationStatus, icon),
            CitationFormat.Detailed => FormatDetailed(citations, validationStatus, icon),
            CitationFormat.TreeView => FormatTreeView(citations, validationStatus, icon),
            _ => FormatCompact(citations, validationStatus, icon)
        };

        _logger.LogInformation(
            "Generated {CitationCount} citations, status='{Status}', icon={Icon}",
            citations.Count, validationStatus, icon);

        return new CitationMarkup
        {
            Citations = citations,
            ValidationStatus = validationStatus,
            Icon = icon,
            FormattedMarkup = formattedMarkup
        };
    }

    /// <inheritdoc />
    public EntityCitationDetail GetCitationDetail(
        KnowledgeEntity entity,
        ValidatedGenerationResult result)
    {
        _logger.LogInformation(
            "Getting citation detail for entity '{EntityName}' (type={EntityType})",
            entity.Name, entity.Type);

        // Find properties whose values appear in the generated content
        var usedProperties = new Dictionary<string, object?>();
        foreach (var prop in entity.Properties)
        {
            var propValueStr = prop.Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(propValueStr) &&
                result.Content.Contains(propValueStr, StringComparison.OrdinalIgnoreCase))
            {
                usedProperties[prop.Key] = prop.Value;
            }
        }

        _logger.LogDebug(
            "Entity '{EntityName}': {UsedCount}/{TotalCount} properties used in content",
            entity.Name, usedProperties.Count, entity.Properties.Count);

        // Find derived claims — match claims whose subject entity ID matches
        var derivedClaims = result.PostValidation.ExtractedClaims
            .Where(c => c.Subject.EntityId == entity.Id)
            .ToList();

        _logger.LogDebug(
            "Entity '{EntityName}': {ClaimCount} derived claims found",
            entity.Name, derivedClaims.Count);

        return new EntityCitationDetail
        {
            Entity = entity,
            UsedProperties = usedProperties,
            DerivedClaims = derivedClaims,
            BrowserLink = $"lexichord://graph/entity/{entity.Id}"
        };
    }

    // ─── Private Helpers ─────────────────────────────────────────────

    /// <summary>
    /// Checks whether an entity is verified against validation findings.
    /// </summary>
    /// <remarks>
    /// LOGIC: An entity is verified if no error-level validation findings
    /// mention the entity by name (case-insensitive) in their message.
    /// This is a proxy for entity-specific finding matching since
    /// <see cref="ValidationFinding"/> does not carry a RelatedEntity property.
    /// </remarks>
    private bool IsEntityVerified(
        KnowledgeEntity entity,
        PostValidationResult postValidation)
    {
        // Check if any error-level findings mention this entity's name
        return postValidation.Findings.All(f =>
            f.Severity != ValidationSeverity.Error ||
            !f.Message.Contains(entity.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Formats a display label for an entity based on its type.
    /// </summary>
    /// <remarks>
    /// LOGIC: Endpoint entities show "METHOD path" (e.g., "GET /api/users").
    /// Parameter entities show "name (location)" (e.g., "userId (query)").
    /// All other types show just the entity name.
    /// </remarks>
    private string FormatDisplayLabel(KnowledgeEntity entity)
    {
        return entity.Type switch
        {
            "Endpoint" => $"{entity.Properties.GetValueOrDefault("method")} {entity.Name}",
            "Parameter" => $"{entity.Name} ({entity.Properties.GetValueOrDefault("location")})",
            _ => entity.Name
        };
    }

    /// <summary>
    /// Formats the validation status text from post-validation results.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps PostValidationStatus to human-readable status strings.
    /// Uses the findings and hallucinations counts for warnings/errors.
    /// </remarks>
    private static string FormatValidationStatus(PostValidationResult result)
    {
        return result.Status switch
        {
            PostValidationStatus.Valid => "Validation passed",
            PostValidationStatus.ValidWithWarnings =>
                $"{result.Findings.Count + result.Hallucinations.Count} warning(s)",
            PostValidationStatus.Invalid =>
                $"{result.Findings.Count(f => f.Severity == ValidationSeverity.Error)} error(s)",
            _ => "Validation incomplete"
        };
    }

    /// <summary>
    /// Maps post-validation status to a validation icon.
    /// </summary>
    /// <remarks>
    /// LOGIC: Direct mapping from PostValidationStatus enum values.
    /// Valid → CheckMark, ValidWithWarnings → Warning,
    /// Invalid → Error, Inconclusive → Question.
    /// </remarks>
    private static ValidationIcon GetValidationIcon(PostValidationStatus status)
    {
        return status switch
        {
            PostValidationStatus.Valid => ValidationIcon.CheckMark,
            PostValidationStatus.ValidWithWarnings => ValidationIcon.Warning,
            PostValidationStatus.Invalid => ValidationIcon.Error,
            _ => ValidationIcon.Question
        };
    }

    /// <summary>
    /// Formats citations in compact layout (icon + name inline).
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a tree-like list:
    /// 📚 Based on:
    /// ├── 🔗 GET /api/users ✓
    /// ├── 📝 userId (query) ✓
    /// ✓ Validation passed
    /// </remarks>
    private static string FormatCompact(
        IReadOnlyList<EntityCitation> citations,
        string status,
        ValidationIcon icon)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📚 Based on:");

        foreach (var citation in citations)
        {
            var verifiedMark = citation.IsVerified ? "✓" : "?";
            sb.AppendLine($"├── {citation.TypeIcon} {citation.DisplayLabel} {verifiedMark}");
        }

        var iconChar = icon switch
        {
            ValidationIcon.CheckMark => "✓",
            ValidationIcon.Warning => "⚠",
            ValidationIcon.Error => "✗",
            _ => "?"
        };

        sb.AppendLine($"{iconChar} {status}");
        return sb.ToString();
    }

    /// <summary>
    /// Formats citations in detailed layout grouped by entity type.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a Markdown-style grouped layout:
    /// ## Knowledge Sources
    /// ### Endpoint
    /// - **GET /api/users**
    ///   - Verified: Yes
    /// ### Parameter
    /// - **userId**
    ///   - Verified: Yes
    /// </remarks>
    private static string FormatDetailed(
        IReadOnlyList<EntityCitation> citations,
        string status,
        ValidationIcon icon)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Knowledge Sources\n");

        var byType = citations.GroupBy(c => c.EntityType);
        foreach (var group in byType)
        {
            sb.AppendLine($"### {group.Key}");
            foreach (var citation in group)
            {
                sb.AppendLine($"- **{citation.EntityName}**");
                sb.AppendLine($"  - Verified: {(citation.IsVerified ? "Yes" : "No")}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats citations in hierarchical tree view.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a Unicode box-drawing tree:
    /// Knowledge Sources
    /// ├── Endpoint
    /// │   ├── GET /api/users
    /// │   └── POST /api/orders
    /// └── Parameter
    ///     └── userId (query)
    /// </remarks>
    private static string FormatTreeView(
        IReadOnlyList<EntityCitation> citations,
        string status,
        ValidationIcon icon)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Knowledge Sources");

        var byType = citations.GroupBy(c => c.EntityType);
        var typeList = byType.ToList();

        for (int i = 0; i < typeList.Count; i++)
        {
            var group = typeList[i];
            var isLastGroup = i == typeList.Count - 1;
            var groupPrefix = isLastGroup ? "└── " : "├── ";

            sb.AppendLine($"{groupPrefix}{group.Key}");

            var items = group.ToList();
            for (int j = 0; j < items.Count; j++)
            {
                var item = items[j];
                var isLastItem = j == items.Count - 1;
                var itemPrefix = isLastGroup ? "    " : "│   ";
                itemPrefix += isLastItem ? "└── " : "├── ";

                sb.AppendLine($"{itemPrefix}{item.DisplayLabel}");
            }
        }

        return sb.ToString();
    }
}
