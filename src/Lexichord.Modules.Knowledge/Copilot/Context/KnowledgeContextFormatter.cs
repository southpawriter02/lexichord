// =============================================================================
// File: KnowledgeContextFormatter.cs
// Project: Lexichord.Modules.Knowledge
// Description: Formats knowledge context for LLM prompt injection.
// =============================================================================
// LOGIC: Implements multi-format context formatting (Markdown, YAML, JSON,
//   Plain) for knowledge graph data. Transforms entities, relationships, and
//   axioms into structured text suitable for Co-pilot prompt injection.
//   v0.7.2g enhancements: FormatWithMetadata() returning FormattedContext with
//   metadata, TruncateToTokenBudget() for token budget enforcement, ITokenCounter
//   integration for accurate token estimation, YAML header/escaping/severity,
//   entity name resolution for relationships, JSON null suppression, property
//   type-aware formatting, Plain text section headers, Markdown entity grouping.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// v0.7.2g: Knowledge Context Formatter enhancement (CKVS Phase 4a)
// Dependencies: IKnowledgeContextFormatter (v0.6.6e), KnowledgeEntity (v0.4.5e),
//               KnowledgeRelationship (v0.4.5e), Axiom (v0.4.6e),
//               FormattedContext (v0.7.2g), ITokenCounter (v0.6.1b, optional)
// =============================================================================

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
///   <item><see cref="ContextFormat.Markdown"/>: Headers, entity grouping, and bullet lists.</item>
///   <item><see cref="ContextFormat.Yaml"/>: Structured YAML with header comment, escaping, and severity.</item>
///   <item><see cref="ContextFormat.Json"/>: Indented JSON with null suppression and severity.</item>
///   <item><see cref="ContextFormat.Plain"/>: Plain text with section headers.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Stateless and thread-safe for concurrent use.
/// </para>
/// <para>
/// <b>Token Estimation:</b> When an <see cref="ITokenCounter"/> is available,
/// uses accurate tokenizer-based counting. Otherwise falls back to the
/// industry-standard heuristic of ~4 characters per token.
/// </para>
/// <para>
/// <b>Entity Name Resolution:</b> Since <see cref="KnowledgeRelationship"/>
/// only contains entity GUIDs (not names), the formatter builds an entity ID
/// → name lookup dictionary from the provided entities. Unknown IDs fall back
/// to a short GUID prefix for readability.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// Enhanced in v0.7.2g with metadata-rich output, token budgeting, and
/// improved formatting.
/// </para>
/// </remarks>
internal sealed class KnowledgeContextFormatter : IKnowledgeContextFormatter
{
    private readonly ILogger<KnowledgeContextFormatter> _logger;
    private readonly ITokenCounter? _tokenCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeContextFormatter"/> class.
    /// </summary>
    /// <param name="logger">Logger for formatting operations.</param>
    /// <param name="tokenCounter">
    /// Optional token counter for accurate token estimation (v0.7.2g).
    /// When null, falls back to the ~4 characters per token heuristic.
    /// </param>
    public KnowledgeContextFormatter(
        ILogger<KnowledgeContextFormatter> logger,
        ITokenCounter? tokenCounter = null)
    {
        _logger = logger;
        _tokenCounter = tokenCounter;

        _logger.LogDebug(
            "KnowledgeContextFormatter initialized. TokenCounter={TokenCounterAvailable}",
            _tokenCounter is not null);
    }

    /// <inheritdoc />
    public string FormatContext(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        // LOGIC: Delegate to FormatWithMetadata for consistent output, return just the content.
        // v0.7.2g: Unified formatting pipeline via FormatWithMetadata.
        return FormatWithMetadata(entities, relationships, axioms, options).Content;
    }

    /// <inheritdoc />
    public int EstimateTokens(string formattedContext)
    {
        // LOGIC: Use ITokenCounter when available for accurate estimation.
        // Fallback to the GPT-style heuristic (~4 chars per token) when unavailable.
        // v0.7.2g: Added ITokenCounter integration.
        if (_tokenCounter is not null)
        {
            var count = _tokenCounter.CountTokens(formattedContext);
            _logger.LogTrace(
                "Token estimation via ITokenCounter: {Count} tokens for {Length} chars",
                count, formattedContext.Length);
            return count;
        }

        return formattedContext.Length / 4;
    }

    /// <inheritdoc />
    public FormattedContext FormatWithMetadata(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        _logger.LogDebug(
            "FormatWithMetadata: {EntityCount} entities, {RelCount} relationships, " +
            "{AxiomCount} axioms, Format={Format}",
            entities.Count,
            relationships?.Count ?? 0,
            axioms?.Count ?? 0,
            options.Format);

        if (entities.Count == 0)
        {
            _logger.LogDebug("No entities to format, returning FormattedContext.Empty");
            return FormattedContext.Empty;
        }

        // LOGIC: Build entity ID → name lookup for relationship name resolution.
        // KnowledgeRelationship only contains FromEntityId/ToEntityId GUIDs.
        // This dictionary allows resolving entity names for display.
        var entityNameById = new Dictionary<Guid, string>();
        foreach (var entity in entities)
        {
            entityNameById[entity.Id] = entity.Name;
        }

        _logger.LogTrace(
            "Built entity name lookup with {Count} entries", entityNameById.Count);

        var content = options.Format switch
        {
            ContextFormat.Markdown => FormatAsMarkdown(entities, relationships, axioms, options, entityNameById),
            ContextFormat.Yaml => FormatAsYaml(entities, relationships, axioms, options, entityNameById),
            ContextFormat.Json => FormatAsJson(entities, relationships, axioms, options, entityNameById),
            _ => FormatAsPlain(entities, relationships, axioms, options, entityNameById)
        };

        var tokenCount = EstimateTokens(content);

        _logger.LogDebug(
            "FormatWithMetadata complete: {Length} chars, ~{Tokens} tokens",
            content.Length, tokenCount);

        return new FormattedContext
        {
            Content = content,
            TokenCount = tokenCount,
            Format = options.Format,
            EntityCount = entities.Count,
            RelationshipCount = relationships?.Count ?? 0,
            AxiomCount = axioms?.Count ?? 0
        };
    }

    /// <inheritdoc />
    public FormattedContext TruncateToTokenBudget(FormattedContext context, int maxTokens)
    {
        _logger.LogDebug(
            "TruncateToTokenBudget: currentTokens={CurrentTokens}, maxTokens={MaxTokens}",
            context.TokenCount, maxTokens);

        // LOGIC: If already within budget, return unchanged.
        if (context.TokenCount <= maxTokens)
        {
            _logger.LogDebug("Content within token budget, no truncation needed");
            return context;
        }

        // LOGIC: Estimate the character-to-token ratio from the current content,
        // then calculate the target character count with a 5% safety buffer.
        // This approach adapts to the actual content density rather than using
        // a fixed chars-per-token assumption.
        var charsPerToken = (float)context.Content.Length / context.TokenCount;
        var targetChars = (int)(maxTokens * charsPerToken * 0.95f); // 5% buffer

        var truncated = context.Content[..Math.Min(targetChars, context.Content.Length)];

        // LOGIC: Try to truncate at a clean boundary (newline) to avoid
        // cutting mid-line. Only use the newline boundary if it's within
        // 80% of the target length (don't lose too much content).
        var lastNewline = truncated.LastIndexOf('\n');
        if (lastNewline > targetChars * 0.8f)
        {
            truncated = truncated[..lastNewline];
        }

        truncated += "\n\n[Context truncated due to token limit]";

        var truncatedTokenCount = EstimateTokens(truncated);

        _logger.LogInformation(
            "Content truncated from {OriginalTokens} to {TruncatedTokens} tokens " +
            "(budget={MaxTokens})",
            context.TokenCount, truncatedTokenCount, maxTokens);

        return context with
        {
            Content = truncated,
            TokenCount = truncatedTokenCount,
            WasTruncated = true
        };
    }

    /// <summary>
    /// Formats context as Markdown with entity grouping by type, headers, and bullet lists.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a Markdown document with:
    ///   - ## Knowledge Entities section with entities grouped by type
    ///   - Entity type headers as **TypeName** bold text
    ///   - Properties as indented bullet list items (limited by MaxPropertiesPerEntity)
    ///   - ## Relationships section with entity name resolution ({from} **{type}** {to})
    ///   - ## Domain Rules section for axioms with severity display
    /// v0.7.2g: Added entity grouping by type, entity name resolution for
    /// relationships, and axiom severity display.
    /// </remarks>
    private static string FormatAsMarkdown(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options,
        Dictionary<Guid, string> entityNameById)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Knowledge Entities");
        sb.AppendLine();

        // LOGIC: Group entities by type for structured display.
        // v0.7.2g: Entity grouping by type replaces flat listing.
        var byType = entities.GroupBy(e => e.Type);
        foreach (var group in byType)
        {
            sb.AppendLine($"**{group.Key}**");
            foreach (var entity in group)
            {
                sb.AppendLine($"- **{entity.Name}**");
                if (options.IncludeEntityIds)
                {
                    sb.AppendLine($"  - Id: {entity.Id}");
                }
                var props = entity.Properties.Take(options.MaxPropertiesPerEntity);
                foreach (var prop in props)
                {
                    sb.AppendLine($"  - {prop.Key}: {prop.Value}");
                }
            }
            sb.AppendLine();
        }

        if (relationships?.Count > 0)
        {
            sb.AppendLine("## Relationships");
            foreach (var rel in relationships)
            {
                // LOGIC: Resolve entity names for relationship display.
                // v0.7.2g: Uses entity name lookup instead of raw GUID prefixes.
                var fromName = ResolveEntityName(rel.FromEntityId, entityNameById);
                var toName = ResolveEntityName(rel.ToEntityId, entityNameById);
                sb.AppendLine($"- {fromName} **{rel.Type}** {toName}");
            }
            sb.AppendLine();
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine("## Domain Rules");
            foreach (var axiom in axioms)
            {
                // LOGIC: Include severity for axiom display.
                // v0.7.2g: Axiom severity display.
                sb.AppendLine($"- **{axiom.Name}** ({axiom.Severity}): {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats context as structured YAML with header comment, escaping, and severity.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a YAML document with:
    ///   - # Knowledge Context header comment
    ///   - Entities as a list of type/name/properties mappings with YAML string escaping
    ///   - Property values formatted type-aware (strings quoted, bools lowercase, null literal)
    ///   - Relationships with resolved entity names and YAML escaping
    ///   - Axioms/rules with severity field
    /// v0.7.2g: Added header comment, EscapeYaml for string values,
    /// FormatPropertyValue for type-aware formatting, axiom severity field,
    /// entity name resolution for relationships.
    /// </remarks>
    private static string FormatAsYaml(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options,
        Dictionary<Guid, string> entityNameById)
    {
        var sb = new StringBuilder();

        // LOGIC: Header comment for YAML context identification.
        // v0.7.2g: Added header comment.
        sb.AppendLine("# Knowledge Context");
        sb.AppendLine();

        sb.AppendLine("entities:");
        foreach (var entity in entities)
        {
            sb.AppendLine($"  - type: {entity.Type}");
            // LOGIC: Quote and escape entity names for YAML safety.
            // v0.7.2g: YAML string escaping.
            sb.AppendLine($"    name: \"{EscapeYaml(entity.Name)}\"");
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
                    // LOGIC: Type-aware property value formatting.
                    // v0.7.2g: FormatPropertyValue for strings, bools, nulls.
                    sb.AppendLine($"      {prop.Key}: {FormatPropertyValue(prop.Value)}");
                }
            }
        }

        if (relationships?.Count > 0)
        {
            sb.AppendLine("relationships:");
            foreach (var rel in relationships)
            {
                // LOGIC: Resolve entity names and escape for YAML safety.
                // v0.7.2g: Entity name resolution for relationships.
                var fromName = ResolveEntityName(rel.FromEntityId, entityNameById);
                var toName = ResolveEntityName(rel.ToEntityId, entityNameById);
                sb.AppendLine($"  - from: \"{EscapeYaml(fromName)}\"");
                sb.AppendLine($"    type: {rel.Type}");
                sb.AppendLine($"    to: \"{EscapeYaml(toName)}\"");
            }
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine("rules:");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"  - name: \"{EscapeYaml(axiom.Name)}\"");
                sb.AppendLine($"    rule: \"{EscapeYaml(axiom.Description ?? string.Empty)}\"");
                // LOGIC: Include axiom severity in YAML output.
                // v0.7.2g: Axiom severity field.
                sb.AppendLine($"    severity: {axiom.Severity}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats context as indented JSON with null suppression and severity.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses System.Text.Json for serialization with:
    ///   - Indented output for readability
    ///   - JsonIgnoreCondition.WhenWritingNull for compact output
    ///   - Entity names resolved for relationships
    ///   - Axiom severity field included
    /// v0.7.2g: Added WhenWritingNull, axiom severity, entity name resolution.
    /// </remarks>
    private static string FormatAsJson(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options,
        Dictionary<Guid, string> entityNameById)
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
            // LOGIC: Resolve entity names for relationship display in JSON.
            // v0.7.2g: Entity name resolution for relationships.
            relationships = relationships?.Select(r => new
            {
                from = ResolveEntityName(r.FromEntityId, entityNameById),
                type = r.Type,
                to = ResolveEntityName(r.ToEntityId, entityNameById)
            }),
            // LOGIC: Include axiom severity in JSON output.
            // v0.7.2g: Axiom severity field and rename to "rules".
            rules = axioms?.Select(a => new
            {
                name = a.Name,
                rule = a.Description,
                severity = a.Severity.ToString()
            })
        };

        // LOGIC: WhenWritingNull suppresses null collections for compact output.
        // v0.7.2g: JSON null suppression.
        return JsonSerializer.Serialize(context, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Formats context as plain text with section headers.
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a plain text document with:
    ///   - KNOWLEDGE CONTEXT: header
    ///   - Entity type and name on the first line
    ///   - Indented property key-value pairs
    ///   - RELATIONSHIPS: section header with entity name resolution
    ///   - RULES: section header for axioms
    /// v0.7.2g: Added KNOWLEDGE CONTEXT header, RELATIONSHIPS and RULES
    /// section headers, entity name resolution.
    /// </remarks>
    private static string FormatAsPlain(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options,
        Dictionary<Guid, string> entityNameById)
    {
        var sb = new StringBuilder();

        // LOGIC: Top-level header for plain text context identification.
        // v0.7.2g: Added KNOWLEDGE CONTEXT header.
        sb.AppendLine("KNOWLEDGE CONTEXT:");
        sb.AppendLine();

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
            // LOGIC: Section header for relationships.
            // v0.7.2g: Added RELATIONSHIPS section header and entity name resolution.
            sb.AppendLine("RELATIONSHIPS:");
            foreach (var rel in relationships)
            {
                var fromName = ResolveEntityName(rel.FromEntityId, entityNameById);
                var toName = ResolveEntityName(rel.ToEntityId, entityNameById);
                sb.AppendLine($"{fromName} -> {rel.Type} -> {toName}");
            }
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine();
            // LOGIC: Section header for axioms/rules.
            // v0.7.2g: Added RULES section header.
            sb.AppendLine("RULES:");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"- {axiom.Name}: {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a string value for safe inclusion in YAML output.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>The escaped string with quotes and newlines handled.</returns>
    /// <remarks>
    /// LOGIC: Escapes double quotes and newline characters that would break
    /// YAML string parsing. Used for entity names, axiom descriptions, and
    /// relationship entity names.
    /// v0.7.2g: Introduced for YAML safety.
    /// </remarks>
    private static string EscapeYaml(string value)
    {
        return value.Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    /// <summary>
    /// Formats a property value with type-aware representation.
    /// </summary>
    /// <param name="value">The property value to format.</param>
    /// <returns>
    /// A formatted string: <c>"null"</c> for nulls, quoted strings with YAML escaping,
    /// lowercase booleans, or the default <c>ToString()</c> representation.
    /// </returns>
    /// <remarks>
    /// LOGIC: Type-aware formatting for YAML property values:
    ///   - null → "null" literal
    ///   - string → quoted with YAML escaping
    ///   - bool → lowercase (true/false) for YAML convention
    ///   - other → ToString() or "null" fallback
    /// v0.7.2g: Introduced for type-aware property formatting.
    /// </remarks>
    private static string FormatPropertyValue(object? value)
    {
        if (value is null) return "null";
        if (value is string s) return $"\"{EscapeYaml(s)}\"";
        if (value is bool b) return b.ToString().ToLowerInvariant();
        return value.ToString() ?? "null";
    }

    /// <summary>
    /// Resolves an entity ID to its display name.
    /// </summary>
    /// <param name="entityId">The entity GUID to resolve.</param>
    /// <param name="entityNameById">The entity ID → name lookup dictionary.</param>
    /// <returns>
    /// The entity name if found in the lookup, otherwise the first 8 characters
    /// of the GUID followed by "..." for readability.
    /// </returns>
    /// <remarks>
    /// LOGIC: KnowledgeRelationship only contains FromEntityId/ToEntityId GUIDs,
    /// not entity names. This method resolves GUIDs to human-readable names using
    /// the lookup dictionary built from the provided entities. Falls back to a
    /// short GUID prefix when the entity is not in the current context set.
    /// v0.7.2g: Introduced for entity name resolution in relationships.
    /// </remarks>
    private static string ResolveEntityName(Guid entityId, Dictionary<Guid, string> entityNameById)
    {
        return entityNameById.TryGetValue(entityId, out var name)
            ? name
            : $"{entityId.ToString()[..8]}...";
    }
}
