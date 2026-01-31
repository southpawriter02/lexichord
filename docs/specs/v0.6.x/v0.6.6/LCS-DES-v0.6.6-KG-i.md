# LCS-DES-066-KG-i: Knowledge-Aware Prompts

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-066-KG-i |
| **System Breakdown** | LCS-SBD-066-KG |
| **Version** | v0.6.6 |
| **Codename** | Knowledge-Aware Prompts (CKVS Phase 3b) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Knowledge-Aware Prompts** module provides prompt templates that incorporate Knowledge Graph context. These templates guide the LLM to use verified facts, follow axioms, and maintain consistency with canonical knowledge.

### 1.2 Key Responsibilities

- Define prompt templates with knowledge placeholders
- Inject knowledge context into prompts
- Include axiom rules in system prompts
- Format entity data for LLM consumption
- Support different prompt strategies

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Copilot/
      Prompts/
        IKnowledgePromptBuilder.cs
        KnowledgePromptBuilder.cs
        PromptTemplates/
          copilot-knowledge-aware.yaml
          copilot-validation.yaml
```

---

## 2. Interface Definitions

### 2.1 Knowledge Prompt Builder Interface

```csharp
namespace Lexichord.KnowledgeGraph.Copilot.Prompts;

/// <summary>
/// Builds prompts with knowledge graph context.
/// </summary>
public interface IKnowledgePromptBuilder
{
    /// <summary>
    /// Builds a knowledge-aware prompt.
    /// </summary>
    KnowledgePrompt BuildPrompt(
        CopilotRequest request,
        KnowledgeContext context,
        PromptOptions options);

    /// <summary>
    /// Gets available prompt templates.
    /// </summary>
    IReadOnlyList<PromptTemplate> GetTemplates();

    /// <summary>
    /// Registers a custom prompt template.
    /// </summary>
    void RegisterTemplate(PromptTemplate template);
}
```

---

## 3. Data Types

### 3.1 Knowledge Prompt

```csharp
/// <summary>
/// A prompt ready for LLM submission.
/// </summary>
public record KnowledgePrompt
{
    /// <summary>System prompt with rules and context.</summary>
    public required string SystemPrompt { get; init; }

    /// <summary>User prompt with request.</summary>
    public required string UserPrompt { get; init; }

    /// <summary>Token count estimate.</summary>
    public int EstimatedTokens { get; init; }

    /// <summary>Template used.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Entities included in context.</summary>
    public IReadOnlyList<Guid> IncludedEntityIds { get; init; } = [];

    /// <summary>Axioms included.</summary>
    public IReadOnlyList<Guid> IncludedAxiomIds { get; init; } = [];
}
```

### 3.2 Prompt Template

```csharp
/// <summary>
/// A prompt template definition.
/// </summary>
public record PromptTemplate
{
    /// <summary>Template ID.</summary>
    public required string Id { get; init; }

    /// <summary>Template name.</summary>
    public required string Name { get; init; }

    /// <summary>Template description.</summary>
    public string? Description { get; init; }

    /// <summary>System prompt template (Handlebars).</summary>
    public required string SystemTemplate { get; init; }

    /// <summary>User prompt template (Handlebars).</summary>
    public required string UserTemplate { get; init; }

    /// <summary>Default options.</summary>
    public PromptOptions? DefaultOptions { get; init; }

    /// <summary>Required context elements.</summary>
    public PromptRequirements Requirements { get; init; } = new();
}

/// <summary>
/// Requirements for a prompt template.
/// </summary>
public record PromptRequirements
{
    public bool RequiresEntities { get; init; } = true;
    public bool RequiresRelationships { get; init; } = false;
    public bool RequiresAxioms { get; init; } = false;
    public bool RequiresClaims { get; init; } = false;
    public int MinEntities { get; init; } = 0;
}
```

### 3.3 Prompt Options

```csharp
/// <summary>
/// Options for prompt building.
/// </summary>
public record PromptOptions
{
    /// <summary>Template ID to use.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Maximum tokens for context.</summary>
    public int MaxContextTokens { get; init; } = 2000;

    /// <summary>Whether to include axioms.</summary>
    public bool IncludeAxioms { get; init; } = true;

    /// <summary>Whether to include relationships.</summary>
    public bool IncludeRelationships { get; init; } = true;

    /// <summary>Context format in prompt.</summary>
    public ContextFormat ContextFormat { get; init; } = ContextFormat.Yaml;

    /// <summary>Strictness level for grounding.</summary>
    public GroundingLevel GroundingLevel { get; init; } = GroundingLevel.Moderate;

    /// <summary>Additional instructions.</summary>
    public string? AdditionalInstructions { get; init; }
}

public enum GroundingLevel
{
    /// <summary>Strict: only facts from context.</summary>
    Strict,

    /// <summary>Moderate: prefer context, allow inferences.</summary>
    Moderate,

    /// <summary>Flexible: use context as guidance.</summary>
    Flexible
}
```

---

## 4. Implementation

### 4.1 Knowledge Prompt Builder

```csharp
public class KnowledgePromptBuilder : IKnowledgePromptBuilder
{
    private readonly Dictionary<string, PromptTemplate> _templates = new();
    private readonly IContextFormatter _contextFormatter;
    private readonly IHandlebars _handlebars;

    public KnowledgePromptBuilder(IContextFormatter contextFormatter)
    {
        _contextFormatter = contextFormatter;
        _handlebars = Handlebars.Create();

        // Register helpers
        _handlebars.RegisterHelper("each", EachHelper);

        // Load default templates
        LoadDefaultTemplates();
    }

    public KnowledgePrompt BuildPrompt(
        CopilotRequest request,
        KnowledgeContext context,
        PromptOptions options)
    {
        var templateId = options.TemplateId ?? "copilot-knowledge-aware";
        var template = _templates.GetValueOrDefault(templateId)
            ?? throw new ArgumentException($"Template not found: {templateId}");

        // Prepare template data
        var data = new Dictionary<string, object>
        {
            ["query"] = request.Query,
            ["entities"] = FormatEntities(context.Entities, options),
            ["relationships"] = FormatRelationships(context.Relationships, options),
            ["axioms"] = FormatAxioms(context.Axioms, options),
            ["groundingInstructions"] = GetGroundingInstructions(options.GroundingLevel),
            ["additionalInstructions"] = options.AdditionalInstructions ?? ""
        };

        // Compile and execute templates
        var systemTemplate = _handlebars.Compile(template.SystemTemplate);
        var userTemplate = _handlebars.Compile(template.UserTemplate);

        var systemPrompt = systemTemplate(data);
        var userPrompt = userTemplate(data);

        return new KnowledgePrompt
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            EstimatedTokens = EstimateTokens(systemPrompt + userPrompt),
            TemplateId = templateId,
            IncludedEntityIds = context.Entities.Select(e => e.Id).ToList(),
            IncludedAxiomIds = context.Axioms?.Select(a => a.Id).ToList() ?? []
        };
    }

    public IReadOnlyList<PromptTemplate> GetTemplates()
    {
        return _templates.Values.ToList();
    }

    public void RegisterTemplate(PromptTemplate template)
    {
        _templates[template.Id] = template;
    }

    private void LoadDefaultTemplates()
    {
        RegisterTemplate(new PromptTemplate
        {
            Id = "copilot-knowledge-aware",
            Name = "Knowledge-Aware Co-pilot",
            Description = "Standard knowledge-grounded generation",
            SystemTemplate = @"You are a technical writing assistant with access to a verified knowledge base.
Use the provided KNOWLEDGE CONTEXT to ensure accuracy in your responses.

{{groundingInstructions}}

DOMAIN RULES TO FOLLOW:
{{#each axioms}}
- {{this.Name}}: {{this.Description}}
{{/each}}

{{additionalInstructions}}",
            UserTemplate = @"KNOWLEDGE CONTEXT:
{{entities}}

{{#if relationships}}
RELATIONSHIPS:
{{relationships}}
{{/if}}

USER REQUEST:
{{query}}

Please generate content that is consistent with the knowledge context above.
Use the exact names and terminology from the knowledge entities.",
            Requirements = new PromptRequirements
            {
                RequiresEntities = true,
                RequiresAxioms = true
            }
        });

        RegisterTemplate(new PromptTemplate
        {
            Id = "copilot-strict",
            Name = "Strict Knowledge-Only",
            Description = "Only use facts from knowledge base",
            SystemTemplate = @"You are a technical writing assistant.
You may ONLY state facts that are explicitly present in the KNOWLEDGE CONTEXT.
If information is not in the context, respond with ""I don't have verified information about that.""

Do not make inferences or add information beyond what is provided.

{{#each axioms}}
- Rule: {{this.Name}} - {{this.Description}}
{{/each}}",
            UserTemplate = @"VERIFIED KNOWLEDGE:
{{entities}}

QUESTION:
{{query}}

Respond using ONLY the verified knowledge above. Do not add any information not present in the context.",
            Requirements = new PromptRequirements
            {
                RequiresEntities = true,
                MinEntities = 1
            }
        });

        RegisterTemplate(new PromptTemplate
        {
            Id = "copilot-documentation",
            Name = "Documentation Generator",
            Description = "Generate documentation from knowledge",
            SystemTemplate = @"You are a technical documentation writer.
Generate clear, accurate documentation based on the provided knowledge.

Writing style:
- Use active voice
- Be concise and precise
- Use consistent terminology from the knowledge base
- Include all required properties
- Follow the domain rules

{{#each axioms}}
Domain Rule: {{this.Name}}
- {{this.Description}}
{{/each}}",
            UserTemplate = @"ENTITIES TO DOCUMENT:
{{entities}}

{{#if relationships}}
ENTITY RELATIONSHIPS:
{{relationships}}
{{/if}}

DOCUMENTATION REQUEST:
{{query}}

Generate professional documentation for the entities above.",
            Requirements = new PromptRequirements
            {
                RequiresEntities = true,
                RequiresRelationships = true
            }
        });
    }

    private string FormatEntities(
        IReadOnlyList<KnowledgeEntity> entities,
        PromptOptions options)
    {
        return _contextFormatter.FormatContext(
            entities, null, null,
            new ContextFormatOptions { Format = options.ContextFormat });
    }

    private string FormatRelationships(
        IReadOnlyList<KnowledgeRelationship>? relationships,
        PromptOptions options)
    {
        if (relationships == null || !options.IncludeRelationships)
            return "";

        var sb = new StringBuilder();
        foreach (var rel in relationships)
        {
            sb.AppendLine($"- {rel.FromEntityName} --[{rel.Type}]--> {rel.ToEntityName}");
        }
        return sb.ToString();
    }

    private string FormatAxioms(
        IReadOnlyList<Axiom>? axioms,
        PromptOptions options)
    {
        if (axioms == null || !options.IncludeAxioms)
            return "";

        return string.Join("\n", axioms.Select(a => new
        {
            Name = a.Name,
            Description = a.Description
        }));
    }

    private string GetGroundingInstructions(GroundingLevel level)
    {
        return level switch
        {
            GroundingLevel.Strict => @"STRICT GROUNDING RULES:
1. ONLY state facts explicitly present in the knowledge context
2. Do NOT make inferences beyond what is stated
3. If asked about something not in the context, say ""I don't have verified information about that""
4. Use exact terminology from the knowledge entities",

            GroundingLevel.Moderate => @"GROUNDING RULES:
1. Prefer facts from the knowledge context
2. You may make reasonable inferences clearly marked as such
3. If information is uncertain, indicate the level of confidence
4. Use terminology consistent with the knowledge entities",

            GroundingLevel.Flexible => @"GROUNDING GUIDANCE:
1. Use the knowledge context as primary reference
2. You may supplement with general knowledge where appropriate
3. Prioritize accuracy over completeness
4. Maintain consistency with the knowledge base terminology",

            _ => ""
        };
    }

    private int EstimateTokens(string text)
    {
        return text.Length / 4; // Rough estimate
    }

    private void EachHelper(
        EncodedTextWriter output,
        BlockHelperOptions options,
        Context context,
        Arguments arguments)
    {
        // Handlebars each helper implementation
        var enumerable = arguments[0] as IEnumerable;
        if (enumerable == null) return;

        foreach (var item in enumerable)
        {
            options.Template(output, item);
        }
    }
}
```

### 4.2 YAML Template Files

```yaml
# prompts/copilot-knowledge-aware.yaml
id: copilot-knowledge-aware
name: "Knowledge-Aware Co-pilot"
version: "1.0"
description: "Standard knowledge-grounded generation prompt"

system_prompt: |
  You are a technical writing assistant with access to a verified knowledge base.
  Use the provided KNOWLEDGE CONTEXT to ensure accuracy in your responses.

  IMPORTANT RULES:
  1. Only state facts that are in the knowledge context or explicitly asked about
  2. Use the exact names and terminology from the knowledge entities
  3. If information is not in the context, say "I don't have information about..."
  4. Reference the knowledge entities by their canonical names

  AXIOMS TO FOLLOW:
  {{#each axioms}}
  - {{this.name}}: {{this.description}}
  {{/each}}

user_prompt: |
  KNOWLEDGE CONTEXT:
  {{#each entities}}
  - {{this.Type}}: {{this.Name}}
    {{#each this.Properties}}
    - {{@key}}: {{this}}
    {{/each}}
  {{/each}}

  RELATIONSHIPS:
  {{#each relationships}}
  - {{this.FromEntity.Name}} --[{{this.Type}}]--> {{this.ToEntity.Name}}
  {{/each}}

  USER REQUEST:
  {{query}}

  Please generate content that is consistent with the knowledge context above.

requirements:
  requires_entities: true
  requires_axioms: true
  min_entities: 1

defaults:
  grounding_level: moderate
  include_relationships: true
  context_format: yaml
```

---

## 5. Template Variables

| Variable | Type | Description |
| :------- | :--- | :---------- |
| `{{query}}` | string | User's request |
| `{{entities}}` | formatted | Knowledge entities |
| `{{relationships}}` | formatted | Entity relationships |
| `{{axioms}}` | list | Domain axioms/rules |
| `{{groundingInstructions}}` | string | Grounding level rules |
| `{{additionalInstructions}}` | string | Custom instructions |

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Template not found | Use default template |
| Missing required context | Return error |
| Template compilation fails | Log error, use fallback |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `BuildPrompt_IncludesEntities` | Entities in prompt |
| `BuildPrompt_IncludesAxioms` | Axioms in system prompt |
| `BuildPrompt_RespectsGroundingLevel` | Correct instructions |
| `BuildPrompt_EstimatesTokens` | Token count reasonable |

---

## 8. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Default templates |
| Teams | All templates |
| Enterprise | Custom templates |

---

## 9. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
