// =============================================================================
// File: KnowledgePromptBuilder.cs
// Project: Lexichord.Modules.Knowledge
// Description: Builds LLM prompts with knowledge graph context injected.
// =============================================================================
// LOGIC: Orchestrates prompt construction by:
//   1. Selecting a template (default or specified by PromptOptions.TemplateId)
//   2. Formatting entities via IKnowledgeContextFormatter
//   3. Formatting relationships as "FromName --[TYPE]--> ToName" lines
//   4. Formatting axioms as "- Name: Description" lines
//   5. Selecting grounding instructions based on GroundingLevel
//   6. Rendering templates via IPromptRenderer (Mustache {{variable}} syntax)
//   7. Estimating token count (~4 chars/token)
//
// Three built-in templates are loaded on construction:
//   - copilot-knowledge-aware: Standard knowledge-grounded generation
//   - copilot-strict: Only facts from knowledge base
//   - copilot-documentation: Generate documentation from knowledge
//
// v0.6.6i: Knowledge-Aware Prompts (CKVS Phase 3b)
// Dependencies:
//   - IKnowledgeContextFormatter (v0.6.6e)
//   - IPromptRenderer (v0.6.3a)
//   - AgentRequest (v0.6.6a)
//   - KnowledgeContext (v0.6.6e)
//   - Axiom (v0.4.6e)
//   - KnowledgeEntity, KnowledgeRelationship (v0.4.5e)
// =============================================================================

using System.Text;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Prompts;

/// <summary>
/// Builds LLM prompts with knowledge graph context injected.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KnowledgePromptBuilder"/> is the central service for
/// constructing knowledge-aware prompts. It maintains a registry of prompt
/// templates, formats knowledge context data into template variables, and
/// renders the final prompts via the Mustache renderer.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Template registration is not thread-safe. Templates
/// should be registered during application startup. The <see cref="BuildPrompt"/>
/// method is safe to call concurrently after templates are registered.
/// </para>
/// <para>
/// <b>Built-in Templates:</b>
/// <list type="bullet">
///   <item><c>copilot-knowledge-aware</c> — Standard knowledge-grounded generation (default)</item>
///   <item><c>copilot-strict</c> — Only use facts from the knowledge base</item>
///   <item><c>copilot-documentation</c> — Generate documentation from knowledge</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6i as part of Knowledge-Aware Prompts.
/// </para>
/// </remarks>
public sealed class KnowledgePromptBuilder : IKnowledgePromptBuilder
{
    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    private readonly Dictionary<string, KnowledgePromptTemplate> _templates = new();
    private readonly IKnowledgeContextFormatter _contextFormatter;
    private readonly IPromptRenderer _promptRenderer;
    private readonly ILogger<KnowledgePromptBuilder> _logger;

    /// <summary>
    /// Default template ID used when <see cref="PromptOptions.TemplateId"/> is null.
    /// </summary>
    private const string DefaultTemplateId = "copilot-knowledge-aware";

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of <see cref="KnowledgePromptBuilder"/>.
    /// </summary>
    /// <param name="contextFormatter">Formatter for converting knowledge entities to text.</param>
    /// <param name="promptRenderer">Mustache template renderer for variable substitution.</param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public KnowledgePromptBuilder(
        IKnowledgeContextFormatter contextFormatter,
        IPromptRenderer promptRenderer,
        ILogger<KnowledgePromptBuilder> logger)
    {
        _contextFormatter = contextFormatter ?? throw new ArgumentNullException(nameof(contextFormatter));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Load built-in templates on construction so they are immediately
        // available. Custom templates can be added via RegisterTemplate().
        LoadDefaultTemplates();

        _logger.LogDebug(
            "KnowledgePromptBuilder initialized with {TemplateCount} default templates",
            _templates.Count);
    }

    // -------------------------------------------------------------------------
    // IKnowledgePromptBuilder Implementation
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public KnowledgePrompt BuildPrompt(
        AgentRequest request,
        KnowledgeContext context,
        PromptOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        // LOGIC: Resolve template ID — use default if not specified.
        var templateId = options.TemplateId ?? DefaultTemplateId;

        _logger.LogDebug(
            "Building knowledge prompt with template '{TemplateId}', " +
            "{EntityCount} entities, {AxiomCount} axioms, grounding={GroundingLevel}",
            templateId,
            context.Entities.Count,
            context.Axioms?.Count ?? 0,
            options.GroundingLevel);

        // LOGIC: Look up the template; throw if not found.
        if (!_templates.TryGetValue(templateId, out var template))
        {
            _logger.LogError("Prompt template not found: '{TemplateId}'", templateId);
            throw new ArgumentException($"Template not found: {templateId}", nameof(options));
        }

        // LOGIC: Build template variables by formatting each context element.
        var variables = new Dictionary<string, object>
        {
            ["query"] = request.UserMessage,
            ["entities"] = FormatEntities(context.Entities, options),
            ["relationships"] = FormatRelationships(context.Relationships, context.Entities, options),
            ["axioms"] = FormatAxioms(context.Axioms, options),
            ["groundingInstructions"] = GetGroundingInstructions(options.GroundingLevel),
            ["additionalInstructions"] = options.AdditionalInstructions ?? ""
        };

        // LOGIC: Render both templates via the Mustache renderer.
        var systemPrompt = _promptRenderer.Render(template.SystemTemplate, variables);
        var userPrompt = _promptRenderer.Render(template.UserTemplate, variables);

        var estimatedTokens = EstimateTokens(systemPrompt + userPrompt);

        _logger.LogInformation(
            "Built knowledge prompt '{TemplateId}': " +
            "systemLength={SystemLength}, userLength={UserLength}, " +
            "estimatedTokens={EstimatedTokens}, entities={EntityCount}, axioms={AxiomCount}",
            templateId,
            systemPrompt.Length,
            userPrompt.Length,
            estimatedTokens,
            context.Entities.Count,
            context.Axioms?.Count ?? 0);

        return new KnowledgePrompt
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            EstimatedTokens = estimatedTokens,
            TemplateId = templateId,
            IncludedEntityIds = context.Entities.Select(e => e.Id).ToList(),
            IncludedAxiomIds = context.Axioms?.Select(a => a.Id).ToList() ?? []
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<KnowledgePromptTemplate> GetTemplates()
    {
        _logger.LogDebug("Returning {TemplateCount} registered templates", _templates.Count);
        return _templates.Values.ToList();
    }

    /// <inheritdoc />
    public void RegisterTemplate(KnowledgePromptTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var isOverwrite = _templates.ContainsKey(template.Id);
        _templates[template.Id] = template;

        _logger.LogInformation(
            "{Action} prompt template '{TemplateId}' ('{TemplateName}')",
            isOverwrite ? "Overwrote" : "Registered",
            template.Id,
            template.Name);
    }

    // -------------------------------------------------------------------------
    // Private Methods — Context Formatting
    // -------------------------------------------------------------------------

    /// <summary>
    /// Formats knowledge entities into text using the configured context formatter.
    /// </summary>
    /// <param name="entities">Entities to format.</param>
    /// <param name="options">Options controlling the output format.</param>
    /// <returns>Formatted entity text.</returns>
    private string FormatEntities(
        IReadOnlyList<KnowledgeEntity> entities,
        PromptOptions options)
    {
        // LOGIC: Delegate to IKnowledgeContextFormatter for consistent formatting
        // across the system (Markdown, YAML, JSON, Plain).
        return _contextFormatter.FormatContext(
            entities,
            null,
            null,
            new ContextFormatOptions { Format = options.ContextFormat });
    }

    /// <summary>
    /// Formats relationships as human-readable lines.
    /// </summary>
    /// <param name="relationships">Relationships to format (may be null).</param>
    /// <param name="entities">Entity list for resolving names from IDs.</param>
    /// <param name="options">Options controlling relationship inclusion.</param>
    /// <returns>Formatted relationship text, or empty string if none.</returns>
    private string FormatRelationships(
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<KnowledgeEntity> entities,
        PromptOptions options)
    {
        if (relationships == null || relationships.Count == 0 || !options.IncludeRelationships)
        {
            return "";
        }

        // LOGIC: Build a lookup from entity ID → name for resolving relationship
        // endpoints. KnowledgeRelationship only has FromEntityId/ToEntityId, not
        // name properties — we resolve names from the entity list.
        var entityNameById = entities.ToDictionary(e => e.Id, e => e.Name);

        var sb = new StringBuilder();
        foreach (var rel in relationships)
        {
            var fromName = entityNameById.GetValueOrDefault(rel.FromEntityId, rel.FromEntityId.ToString());
            var toName = entityNameById.GetValueOrDefault(rel.ToEntityId, rel.ToEntityId.ToString());
            sb.AppendLine($"- {fromName} --[{rel.Type}]--> {toName}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats axioms as a bulleted list of name-description pairs.
    /// </summary>
    /// <param name="axioms">Axioms to format (may be null).</param>
    /// <param name="options">Options controlling axiom inclusion.</param>
    /// <returns>Formatted axiom text, or empty string if none.</returns>
    private static string FormatAxioms(
        IReadOnlyList<Axiom>? axioms,
        PromptOptions options)
    {
        if (axioms == null || axioms.Count == 0 || !options.IncludeAxioms)
        {
            return "";
        }

        // LOGIC: Format each axiom as "- Name: Description" for readability in
        // the system prompt. Axioms without descriptions use name only.
        var sb = new StringBuilder();
        foreach (var axiom in axioms)
        {
            if (!string.IsNullOrEmpty(axiom.Description))
            {
                sb.AppendLine($"- {axiom.Name}: {axiom.Description}");
            }
            else
            {
                sb.AppendLine($"- {axiom.Name}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    // -------------------------------------------------------------------------
    // Private Methods — Grounding Instructions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns grounding instructions text appropriate for the specified level.
    /// </summary>
    /// <param name="level">The grounding strictness level.</param>
    /// <returns>Multi-line instruction text for the system prompt.</returns>
    private static string GetGroundingInstructions(GroundingLevel level)
    {
        return level switch
        {
            GroundingLevel.Strict => """
                STRICT GROUNDING RULES:
                1. ONLY state facts explicitly present in the knowledge context
                2. Do NOT make inferences beyond what is stated
                3. If asked about something not in the context, say "I don't have verified information about that"
                4. Use exact terminology from the knowledge entities
                """,

            GroundingLevel.Moderate => """
                GROUNDING RULES:
                1. Prefer facts from the knowledge context
                2. You may make reasonable inferences clearly marked as such
                3. If information is uncertain, indicate the level of confidence
                4. Use terminology consistent with the knowledge entities
                """,

            GroundingLevel.Flexible => """
                GROUNDING GUIDANCE:
                1. Use the knowledge context as primary reference
                2. You may supplement with general knowledge where appropriate
                3. Prioritize accuracy over completeness
                4. Maintain consistency with the knowledge base terminology
                """,

            _ => ""
        };
    }

    // -------------------------------------------------------------------------
    // Private Methods — Token Estimation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Estimates the token count for a given text.
    /// </summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <returns>Approximate token count (~4 characters per token).</returns>
    private static int EstimateTokens(string text)
    {
        // LOGIC: Rough estimate using ~4 characters per token. This matches the
        // estimation approach used in KnowledgeContextFormatter (v0.6.6e) and
        // RenderedPrompt (v0.6.3a). Sufficient for budget checking.
        return text.Length / 4;
    }

    // -------------------------------------------------------------------------
    // Private Methods — Default Templates
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads the three built-in prompt templates.
    /// </summary>
    private void LoadDefaultTemplates()
    {
        // LOGIC: Template variables use Mustache {{variable}} syntax. Since Mustache
        // does not support {{#each}} iteration, entities/relationships/axioms are
        // pre-formatted into strings by the format methods above.

        RegisterTemplate(new KnowledgePromptTemplate
        {
            Id = "copilot-knowledge-aware",
            Name = "Knowledge-Aware Co-pilot",
            Description = "Standard knowledge-grounded generation",
            SystemTemplate = """
                You are a technical writing assistant with access to a verified knowledge base.
                Use the provided KNOWLEDGE CONTEXT to ensure accuracy in your responses.

                {{groundingInstructions}}

                DOMAIN RULES TO FOLLOW:
                {{axioms}}

                {{additionalInstructions}}
                """,
            UserTemplate = """
                KNOWLEDGE CONTEXT:
                {{entities}}

                {{#relationships}}
                RELATIONSHIPS:
                {{relationships}}
                {{/relationships}}

                USER REQUEST:
                {{query}}

                Please generate content that is consistent with the knowledge context above.
                Use the exact names and terminology from the knowledge entities.
                """,
            Requirements = new PromptRequirements
            {
                RequiresEntities = true,
                RequiresAxioms = true
            }
        });

        RegisterTemplate(new KnowledgePromptTemplate
        {
            Id = "copilot-strict",
            Name = "Strict Knowledge-Only",
            Description = "Only use facts from knowledge base",
            SystemTemplate = """
                You are a technical writing assistant.
                You may ONLY state facts that are explicitly present in the KNOWLEDGE CONTEXT.
                If information is not in the context, respond with "I don't have verified information about that."

                Do not make inferences or add information beyond what is provided.

                {{axioms}}
                """,
            UserTemplate = """
                VERIFIED KNOWLEDGE:
                {{entities}}

                QUESTION:
                {{query}}

                Respond using ONLY the verified knowledge above. Do not add any information not present in the context.
                """,
            Requirements = new PromptRequirements
            {
                RequiresEntities = true,
                MinEntities = 1
            }
        });

        RegisterTemplate(new KnowledgePromptTemplate
        {
            Id = "copilot-documentation",
            Name = "Documentation Generator",
            Description = "Generate documentation from knowledge",
            SystemTemplate = """
                You are a technical documentation writer.
                Generate clear, accurate documentation based on the provided knowledge.

                Writing style:
                - Use active voice
                - Be concise and precise
                - Use consistent terminology from the knowledge base
                - Include all required properties
                - Follow the domain rules

                {{axioms}}
                """,
            UserTemplate = """
                ENTITIES TO DOCUMENT:
                {{entities}}

                {{#relationships}}
                ENTITY RELATIONSHIPS:
                {{relationships}}
                {{/relationships}}

                DOCUMENTATION REQUEST:
                {{query}}

                Generate professional documentation for the entities above.
                """,
            Requirements = new PromptRequirements
            {
                RequiresEntities = true,
                RequiresRelationships = true
            }
        });
    }
}
