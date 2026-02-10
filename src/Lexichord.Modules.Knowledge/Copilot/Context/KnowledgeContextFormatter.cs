// =============================================================================
// File: KnowledgeContextFormatter.cs
// Project: Lexichord.Modules.Knowledge
// Description: Formats knowledge context for LLM prompt injection.
// =============================================================================
// LOGIC: Implements multi-format context formatting (Markdown, YAML, JSON,
//   Plain) for knowledge graph data. Transforms entities, relationships, and
//   axioms into structured text suitable for Co-pilot prompt injection.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: IKnowledgeContextFormatter (v0.6.6e), KnowledgeEntity (v0.4.5e),
//               KnowledgeRelationship (v0.4.5e), Axiom (v0.4.6e)
// =============================================================================

using System.Text;
using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Context;

/// <summary>
/// Formats knowledge context for LLM prompt injection.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KnowledgeContextFormatter"/> transforms structured knowledge
/// graph data into formatted strings suitable for inclusion in LLM prompts.
/// It supports four output formats:
/// <list type="bullet">
///   <item><see cref="ContextFormat.Markdown"/>: Headers and bullet lists.</item>
///   <item><see cref="ContextFormat.Yaml"/>: Structured YAML.</item>
///   <item><see cref="ContextFormat.Json"/>: Indented JSON.</item>
///   <item><see cref="ContextFormat.Plain"/>: Plain text.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Stateless and thread-safe for concurrent use.
/// </para>
/// <para>
/// <b>Token Estimation:</b> Uses the industry-standard heuristic of
/// ~4 characters per token for GPT-style tokenization.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
internal sealed class KnowledgeContextFormatter : IKnowledgeContextFormatter
{
    private readonly ILogger<KnowledgeContextFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeContextFormatter"/> class.
    /// </summary>
    /// <param name="logger">Logger for formatting operations.</param>
    public KnowledgeContextFormatter(ILogger<KnowledgeContextFormatter> logger)
    {
        _logger = logger;
        _logger.LogDebug("KnowledgeContextFormatter initialized");
    }

    /// <inheritdoc />
    public string FormatContext(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        _logger.LogDebug(
            "Formatting context: {EntityCount} entities, {RelCount} relationships, {AxiomCount} axioms, Format={Format}",
            entities.Count,
            relationships?.Count ?? 0,
            axioms?.Count ?? 0,
            options.Format);

        if (entities.Count == 0)
        {
            _logger.LogDebug("No entities to format, returning empty string");
            return string.Empty;
        }

        var result = options.Format switch
        {
            ContextFormat.Markdown => FormatAsMarkdown(entities, relationships, axioms, options),
            ContextFormat.Yaml => FormatAsYaml(entities, relationships, axioms, options),
            ContextFormat.Json => FormatAsJson(entities, relationships, axioms, options),
            _ => FormatAsPlain(entities, relationships, axioms, options)
        };

        _logger.LogDebug(
            "Formatted context: {Length} chars, ~{Tokens} tokens",
            result.Length, EstimateTokens(result));

        return result;
    }

    /// <inheritdoc />
    public int EstimateTokens(string formattedContext)
    {
        // GPT-style tokenization: ~4 chars per token
        return formattedContext.Length / 4;
    }

    /// <summary>
    /// Formats context as Markdown with headers and bullet lists.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a Markdown document with:
    ///   - ## Knowledge Entities section with ### per entity
    ///   - Properties as bullet list items (limited by MaxPropertiesPerEntity)
    ///   - ## Relationships section with arrow notation
    ///   - ## Domain Rules section for axioms
    /// </remarks>
    private static string FormatAsMarkdown(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Knowledge Entities");
        foreach (var entity in entities)
        {
            sb.AppendLine($"### {entity.Type}: {entity.Name}");
            if (options.IncludeEntityIds)
            {
                sb.AppendLine($"- **Id**: {entity.Id}");
            }
            var props = entity.Properties.Take(options.MaxPropertiesPerEntity);
            foreach (var prop in props)
            {
                sb.AppendLine($"- **{prop.Key}**: {prop.Value}");
            }
            sb.AppendLine();
        }

        if (relationships?.Count > 0)
        {
            sb.AppendLine("## Relationships");
            foreach (var rel in relationships)
            {
                // KnowledgeRelationship uses entity IDs, not names.
                // Format with short ID prefix for readability.
                sb.AppendLine($"- {rel.FromEntityId.ToString()[..8]}... --[{rel.Type}]--> {rel.ToEntityId.ToString()[..8]}...");
            }
            sb.AppendLine();
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine("## Domain Rules");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"- **{axiom.Name}**: {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats context as structured YAML.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a YAML document with entities as a list of type/name/properties
    /// mappings. Relationships and axioms are included as separate top-level keys.
    /// </remarks>
    private static string FormatAsYaml(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("entities:");
        foreach (var entity in entities)
        {
            sb.AppendLine($"  - type: {entity.Type}");
            sb.AppendLine($"    name: {entity.Name}");
            if (options.IncludeEntityIds)
            {
                sb.AppendLine($"    id: {entity.Id}");
            }
            var props = entity.Properties.Take(options.MaxPropertiesPerEntity).ToList();
            if (props.Count > 0)
            {
                sb.AppendLine("    properties:");
                foreach (var prop in props)
                {
                    sb.AppendLine($"      {prop.Key}: {prop.Value}");
                }
            }
        }

        if (relationships?.Count > 0)
        {
            sb.AppendLine("relationships:");
            foreach (var rel in relationships)
            {
                sb.AppendLine($"  - from: {rel.FromEntityId}");
                sb.AppendLine($"    type: {rel.Type}");
                sb.AppendLine($"    to: {rel.ToEntityId}");
            }
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine("axioms:");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"  - name: {axiom.Name}");
                sb.AppendLine($"    rule: {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats context as indented JSON.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses System.Text.Json for serialization with indented output.
    /// Anonymous types are used to shape the JSON structure without requiring
    /// dedicated DTOs.
    /// </remarks>
    private static string FormatAsJson(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var context = new
        {
            entities = entities.Select(e => new
            {
                type = e.Type,
                name = e.Name,
                properties = e.Properties
                    .Take(options.MaxPropertiesPerEntity)
                    .ToDictionary(p => p.Key, p => p.Value?.ToString() ?? "")
            }),
            relationships = relationships?.Select(r => new
            {
                from = r.FromEntityId,
                type = r.Type,
                to = r.ToEntityId
            }),
            axioms = axioms?.Select(a => new
            {
                name = a.Name,
                rule = a.Description
            })
        };
        return JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Formats context as plain text without markup.
    /// </summary>
    /// <remarks>
    /// LOGIC: Simple indented format with entity type and name on the first line,
    /// followed by indented property key-value pairs.
    /// </remarks>
    private static string FormatAsPlain(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var sb = new StringBuilder();
        foreach (var entity in entities)
        {
            sb.AppendLine($"{entity.Type}: {entity.Name}");
            foreach (var prop in entity.Properties.Take(options.MaxPropertiesPerEntity))
            {
                sb.AppendLine($"  {prop.Key}: {prop.Value}");
            }
        }

        if (relationships?.Count > 0)
        {
            sb.AppendLine();
            foreach (var rel in relationships)
            {
                sb.AppendLine($"{rel.FromEntityId} --[{rel.Type}]--> {rel.ToEntityId}");
            }
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine();
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"Rule: {axiom.Name} - {axiom.Description}");
            }
        }

        return sb.ToString();
    }
}
