# LCS-DES-072-KG-g: Knowledge Context Formatter

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-072-KG-g |
| **System Breakdown** | LCS-SBD-072-KG |
| **Version** | v0.7.2 |
| **Codename** | Knowledge Context Formatter (CKVS Phase 4a) |
| **Estimated Hours** | 3 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Knowledge Context Formatter** serializes Knowledge Graph entities, relationships, and axioms into a format suitable for prompt injection. It handles token budgeting and supports multiple output formats.

### 1.2 Key Responsibilities

- Format entities for prompt injection
- Format relationships and axioms
- Support multiple output formats (YAML, Markdown, JSON)
- Manage token budget with truncation
- Estimate token counts

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Context/
      Formatting/
        IKnowledgeContextFormatter.cs
        KnowledgeContextFormatter.cs
        TokenEstimator.cs
```

---

## 2. Interface Definitions

### 2.1 Knowledge Context Formatter Interface

```csharp
namespace Lexichord.KnowledgeGraph.Context.Formatting;

/// <summary>
/// Formats knowledge context for prompt injection.
/// </summary>
public interface IKnowledgeContextFormatter
{
    /// <summary>
    /// Formats knowledge data for prompts.
    /// </summary>
    FormattedContext Format(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<Axiom> axioms,
        KnowledgeContextConfig config);

    /// <summary>
    /// Truncates formatted context to token budget.
    /// </summary>
    FormattedContext TruncateToTokenBudget(
        FormattedContext context,
        int maxTokens);

    /// <summary>
    /// Estimates tokens for content.
    /// </summary>
    int EstimateTokens(string content);
}
```

---

## 3. Data Types

### 3.1 Formatted Context

```csharp
/// <summary>
/// Formatted knowledge context.
/// </summary>
public record FormattedContext
{
    /// <summary>Formatted content string.</summary>
    public required string Content { get; init; }

    /// <summary>Token count.</summary>
    public int TokenCount { get; init; }

    /// <summary>Format used.</summary>
    public ContextFormat Format { get; init; }

    /// <summary>Number of entities included.</summary>
    public int EntityCount { get; init; }

    /// <summary>Number of relationships included.</summary>
    public int RelationshipCount { get; init; }

    /// <summary>Number of axioms included.</summary>
    public int AxiomCount { get; init; }

    /// <summary>Whether content was truncated.</summary>
    public bool WasTruncated { get; init; }
}
```

---

## 4. Implementation

### 4.1 Knowledge Context Formatter

```csharp
public class KnowledgeContextFormatter : IKnowledgeContextFormatter
{
    private readonly ITokenEstimator _tokenEstimator;

    public KnowledgeContextFormatter(ITokenEstimator tokenEstimator)
    {
        _tokenEstimator = tokenEstimator;
    }

    public FormattedContext Format(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<Axiom> axioms,
        KnowledgeContextConfig config)
    {
        var content = config.Format switch
        {
            ContextFormat.Yaml => FormatAsYaml(entities, relationships, axioms, config),
            ContextFormat.Markdown => FormatAsMarkdown(entities, relationships, axioms, config),
            ContextFormat.Json => FormatAsJson(entities, relationships, axioms, config),
            _ => FormatAsPlain(entities, relationships, axioms, config)
        };

        return new FormattedContext
        {
            Content = content,
            TokenCount = _tokenEstimator.Estimate(content),
            Format = config.Format,
            EntityCount = entities.Count,
            RelationshipCount = relationships.Count,
            AxiomCount = axioms.Count
        };
    }

    public FormattedContext TruncateToTokenBudget(
        FormattedContext context,
        int maxTokens)
    {
        if (context.TokenCount <= maxTokens)
            return context;

        // Estimate chars per token
        var charsPerToken = (float)context.Content.Length / context.TokenCount;
        var targetChars = (int)(maxTokens * charsPerToken * 0.95f); // 5% buffer

        var truncated = context.Content.Substring(0, Math.Min(targetChars, context.Content.Length));

        // Try to truncate at a clean boundary
        var lastNewline = truncated.LastIndexOf('\n');
        if (lastNewline > targetChars * 0.8f)
        {
            truncated = truncated.Substring(0, lastNewline);
        }

        truncated += "\n\n[Context truncated due to token limit]";

        return context with
        {
            Content = truncated,
            TokenCount = _tokenEstimator.Estimate(truncated),
            WasTruncated = true
        };
    }

    public int EstimateTokens(string content)
    {
        return _tokenEstimator.Estimate(content);
    }

    private string FormatAsYaml(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<Axiom> axioms,
        KnowledgeContextConfig config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Knowledge Context");
        sb.AppendLine();

        if (entities.Count > 0)
        {
            sb.AppendLine("entities:");
            foreach (var entity in entities)
            {
                sb.AppendLine($"  - type: {entity.Type}");
                sb.AppendLine($"    name: \"{EscapeYaml(entity.Name)}\"");

                if (config.IncludeProperties && entity.Properties.Count > 0)
                {
                    sb.AppendLine("    properties:");
                    foreach (var prop in entity.Properties.Take(config.MaxPropertiesPerEntity))
                    {
                        var value = FormatPropertyValue(prop.Value);
                        sb.AppendLine($"      {prop.Key}: {value}");
                    }
                }
            }
            sb.AppendLine();
        }

        if (relationships.Count > 0)
        {
            sb.AppendLine("relationships:");
            foreach (var rel in relationships)
            {
                sb.AppendLine($"  - from: \"{EscapeYaml(rel.FromEntityName)}\"");
                sb.AppendLine($"    type: {rel.Type}");
                sb.AppendLine($"    to: \"{EscapeYaml(rel.ToEntityName)}\"");
            }
            sb.AppendLine();
        }

        if (axioms.Count > 0)
        {
            sb.AppendLine("rules:");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"  - name: \"{EscapeYaml(axiom.Name)}\"");
                sb.AppendLine($"    rule: \"{EscapeYaml(axiom.Description)}\"");
                sb.AppendLine($"    severity: {axiom.Severity}");
            }
        }

        return sb.ToString();
    }

    private string FormatAsMarkdown(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<Axiom> axioms,
        KnowledgeContextConfig config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Knowledge Context");
        sb.AppendLine();

        if (entities.Count > 0)
        {
            sb.AppendLine("### Entities");
            sb.AppendLine();

            var byType = entities.GroupBy(e => e.Type);
            foreach (var group in byType)
            {
                sb.AppendLine($"**{group.Key}**");
                foreach (var entity in group)
                {
                    sb.AppendLine($"- **{entity.Name}**");

                    if (config.IncludeProperties)
                    {
                        foreach (var prop in entity.Properties.Take(config.MaxPropertiesPerEntity))
                        {
                            sb.AppendLine($"  - {prop.Key}: {prop.Value}");
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        if (relationships.Count > 0)
        {
            sb.AppendLine("### Relationships");
            sb.AppendLine();
            foreach (var rel in relationships)
            {
                sb.AppendLine($"- {rel.FromEntityName} **{rel.Type}** {rel.ToEntityName}");
            }
            sb.AppendLine();
        }

        if (axioms.Count > 0)
        {
            sb.AppendLine("### Domain Rules");
            sb.AppendLine();
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"- **{axiom.Name}**: {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    private string FormatAsJson(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<Axiom> axioms,
        KnowledgeContextConfig config)
    {
        var context = new
        {
            entities = entities.Select(e => new
            {
                type = e.Type,
                name = e.Name,
                properties = config.IncludeProperties
                    ? e.Properties.Take(config.MaxPropertiesPerEntity).ToDictionary(p => p.Key, p => p.Value)
                    : null
            }),
            relationships = relationships.Select(r => new
            {
                from = r.FromEntityName,
                type = r.Type,
                to = r.ToEntityName
            }),
            rules = axioms.Select(a => new
            {
                name = a.Name,
                rule = a.Description,
                severity = a.Severity.ToString()
            })
        };

        return JsonSerializer.Serialize(context, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private string FormatAsPlain(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<Axiom> axioms,
        KnowledgeContextConfig config)
    {
        var sb = new StringBuilder();

        sb.AppendLine("KNOWLEDGE CONTEXT:");
        sb.AppendLine();

        foreach (var entity in entities)
        {
            sb.AppendLine($"{entity.Type}: {entity.Name}");
            if (config.IncludeProperties)
            {
                foreach (var prop in entity.Properties.Take(config.MaxPropertiesPerEntity))
                {
                    sb.AppendLine($"  {prop.Key}: {prop.Value}");
                }
            }
        }

        if (relationships.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("RELATIONSHIPS:");
            foreach (var rel in relationships)
            {
                sb.AppendLine($"{rel.FromEntityName} -> {rel.Type} -> {rel.ToEntityName}");
            }
        }

        if (axioms.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("RULES:");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"- {axiom.Name}: {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    private string EscapeYaml(string value)
    {
        return value.Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    private string FormatPropertyValue(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{EscapeYaml(s)}\"";
        if (value is bool b) return b.ToString().ToLower();
        return value.ToString() ?? "null";
    }
}
```

### 4.2 Token Estimator

```csharp
/// <summary>
/// Estimates token counts for content.
/// </summary>
public interface ITokenEstimator
{
    int Estimate(string content);
}

/// <summary>
/// Simple character-based token estimator.
/// </summary>
public class SimpleTokenEstimator : ITokenEstimator
{
    // Average ~4 characters per token for English
    private const float CharsPerToken = 4.0f;

    public int Estimate(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return (int)Math.Ceiling(content.Length / CharsPerToken);
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Empty entities | Return minimal context |
| Serialization fails | Fall back to plain format |
| Token estimation fails | Use character count estimate |

---

## 6. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `Format_Yaml_CorrectOutput` | YAML format correct |
| `Format_Markdown_CorrectOutput` | Markdown format correct |
| `Format_Json_CorrectOutput` | JSON format correct |
| `TruncateToTokenBudget_Truncates` | Truncation works |
| `EstimateTokens_ReasonableEstimate` | Token estimate reasonable |

---

## 7. License Gating

| Tier | Access |
| :--- | :----- |
| All tiers | Full formatting (part of context strategy) |

---

## 8. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
