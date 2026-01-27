# Lexichord Specialists Roadmap (v0.7.1 - v0.7.8)

In v0.6.x, we built "The Conductors" — the LLM gateway, prompt templating, and foundational Co-pilot agent. In v0.7.x, we introduce **The Specialists**: purpose-built agents with distinct personalities and capabilities. This phase establishes the Agent Registry, advanced context assembly, and specialized agents for editing, simplification, summarization, and style enforcement.

**Architectural Note:** This version extends `Lexichord.Modules.Agents` with specialized agent implementations. Each agent inherits from the `IAgent` abstraction (v0.6.6a) and defines its own prompt templates, temperature settings, and capabilities. **License Gating:** Basic specialists are WriterPro, advanced orchestration features require Teams tier.

**Total Sub-Parts:** 32 distinct implementation steps.

---

## v0.7.1: The Agent Registry (Persona Management)
**Goal:** Create a configuration-driven system for defining and managing specialized agent personas.

*   **v0.7.1a:** **Agent Configuration Model.** Define agent persona configuration in Abstractions:
    ```csharp
    public record AgentConfiguration(
        string AgentId,
        string Name,
        string Description,
        string Icon,
        string TemplateId,
        AgentCapabilities Capabilities,
        ChatOptions DefaultOptions,
        IReadOnlyDictionary<string, object>? CustomSettings = null
    );

    public record AgentPersona(
        string PersonaId,
        string DisplayName,
        string Tagline,
        string SystemPromptOverride,
        float Temperature,
        string? VoiceDescription
    );
    ```
*   **v0.7.1b:** **Agent Registry Implementation.** Extend `IAgentRegistry` from v0.6.6c:
    ```csharp
    public interface IAgentRegistry
    {
        IReadOnlyList<AgentConfiguration> AvailableAgents { get; }
        IReadOnlyList<AgentPersona> AvailablePersonas { get; }
        IAgent GetAgent(string agentId);
        IAgent GetAgentWithPersona(string agentId, string personaId);
        void RegisterAgent(AgentConfiguration config, Func<IServiceProvider, IAgent> factory);
        void RegisterPersona(AgentPersona persona);
    }
    ```
    *   Scan modules for `[AgentDefinition]` attributes on startup.
    *   Support runtime persona switching without agent restart.
    *   Cache agent instances (singleton per configuration).
*   **v0.7.1c:** **Agent Configuration Files.** Support YAML-based agent definitions:
    ```yaml
    # .lexichord/agents/editor.yaml
    agent_id: "editor"
    name: "The Editor"
    description: "Focused on grammar, clarity, and structure"
    icon: "edit-3"
    template_id: "specialist-editor"
    capabilities: ["Chat", "DocumentContext", "StyleEnforcement"]
    default_options:
      model: "gpt-4o"
      temperature: 0.3
      max_tokens: 2048
    personas:
      - persona_id: "strict"
        display_name: "Strict Editor"
        tagline: "No errors escape notice"
        temperature: 0.1
      - persona_id: "friendly"
        display_name: "Friendly Editor"
        tagline: "Gentle suggestions for improvement"
        temperature: 0.5
    ```
    *   Load from embedded resources (built-in agents).
    *   Load from workspace `.lexichord/agents/` (custom agents).
    *   Hot-reload on file change via `IRobustFileSystemWatcher` (v0.1.2b).
*   **v0.7.1d:** **Agent Selector UI.** Create agent picker in Co-pilot panel:
    *   Dropdown showing available agents with icons.
    *   Persona sub-menu for agents with multiple personas.
    *   "Favorite" agents pinned to quick-access toolbar.
    *   Search/filter for workspaces with many custom agents.
    *   Show license tier badges on locked agents.

---

## v0.7.2: The Context Assembler (Intelligent Context)
**Goal:** Build an advanced system that automatically gathers relevant context based on user activity and cursor position.

*   **v0.7.2a:** **Context Strategy Interface.** Define pluggable context gathering:
    ```csharp
    public interface IContextStrategy
    {
        string StrategyId { get; }
        int Priority { get; }
        Task<ContextFragment?> GatherAsync(ContextGatheringRequest request, CancellationToken ct);
    }

    public record ContextGatheringRequest(
        string? DocumentPath,
        int? CursorPosition,
        string? SelectedText,
        string AgentId,
        IReadOnlyDictionary<string, object>? Hints
    );

    public record ContextFragment(
        string SourceId,
        string Label,
        string Content,
        int TokenEstimate,
        float Relevance
    );
    ```
*   **v0.7.2b:** **Built-in Context Strategies.** Implement core strategies:
    *   **DocumentContextStrategy:** Current document content, respecting token limits.
    *   **SelectionContextStrategy:** Selected text with surrounding paragraph.
    *   **CursorContextStrategy:** Text around cursor (configurable window: 500-2000 chars).
    *   **HeadingContextStrategy:** Current section heading hierarchy.
    *   **RAGContextStrategy:** Semantic search results from v0.5.x.
    *   **StyleContextStrategy:** Active style rules from v0.2.x.
    ```csharp
    public class RAGContextStrategy(
        ISemanticSearchService search,
        IContextExpansionService expansion) : IContextStrategy
    {
        public string StrategyId => "rag";
        public int Priority => 50;

        public async Task<ContextFragment?> GatherAsync(ContextGatheringRequest request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(request.SelectedText)) return null;

            var results = await search.SearchAsync(request.SelectedText, new SearchOptions(TopK: 3), ct);
            if (results.Hits.Count == 0) return null;

            var content = FormatHitsAsContext(results.Hits);
            return new ContextFragment("rag", "Related Documentation", content, EstimateTokens(content), 0.8f);
        }
    }
    ```
*   **v0.7.2c:** **Context Orchestrator.** Implement `IContextOrchestrator` that coordinates strategies:
    ```csharp
    public interface IContextOrchestrator
    {
        Task<AssembledContext> AssembleAsync(ContextGatheringRequest request, ContextBudget budget, CancellationToken ct);
    }

    public record ContextBudget(int MaxTokens, IReadOnlyList<string>? RequiredStrategies, IReadOnlyList<string>? ExcludedStrategies);

    public record AssembledContext(
        IReadOnlyList<ContextFragment> Fragments,
        int TotalTokens,
        IReadOnlyDictionary<string, object> Variables
    );
    ```
    *   Execute strategies in parallel (`Task.WhenAll`).
    *   Sort by priority, then by relevance score.
    *   Trim to fit token budget (drop lowest priority first).
    *   Deduplicate overlapping content.
*   **v0.7.2d:** **Context Preview Panel.** Show assembled context to user:
    *   Collapsible sections per context fragment.
    *   Token count per fragment and total.
    *   Toggle switches to include/exclude specific strategies.
    *   "Refresh Context" button for manual re-gathering.
    *   Highlight which fragments were actually used in last request.

---

## v0.7.3: The Editor Agent (Grammar & Clarity)
**Goal:** Build a specialized agent focused on grammatical correctness, clarity, and structural improvements.

*   **v0.7.3a:** **Editor Prompt Templates.** Create focused prompts for editing tasks:
    ```yaml
    # prompts/specialist-editor.yaml
    template_id: "specialist-editor"
    name: "Editor Specialist"
    system_prompt: |
      You are a meticulous editor focused on grammar, clarity, and structure.
      Your role is to improve writing while preserving the author's voice.

      Guidelines:
      - Fix grammatical errors (subject-verb agreement, tense consistency, punctuation)
      - Improve sentence clarity (remove ambiguity, simplify complex sentences)
      - Enhance structure (logical flow, paragraph transitions)
      - Preserve technical accuracy and domain terminology
      {{#style_rules}}

      Style Rules to Follow:
      {{style_rules}}
      {{/style_rules}}

      When suggesting changes:
      1. Explain what you're changing and why
      2. Show the original and revised text
      3. Rate the severity: Minor, Moderate, or Significant
    user_prompt: |
      Please review and edit the following text:

      {{#selection}}
      Selected text:
      """
      {{selection}}
      """
      {{/selection}}
      {{^selection}}
      Document content:
      """
      {{document_content}}
      """
      {{/selection}}

      {{user_input}}
    ```
*   **v0.7.3b:** **Editor Agent Implementation.** Create `EditorAgent`:
    ```csharp
    [RequiresLicense(LicenseTier.WriterPro)]
    [AgentDefinition("editor", "The Editor", "Grammar, clarity, and structural improvements")]
    public class EditorAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        IContextOrchestrator contextOrchestrator,
        IPromptTemplateRepository templates,
        ILogger<EditorAgent> logger) : BaseAgent(llm, renderer, templates, logger)
    {
        public override string AgentId => "editor";
        public override string Name => "The Editor";
        public override AgentCapabilities Capabilities =>
            AgentCapabilities.Chat | AgentCapabilities.DocumentContext | AgentCapabilities.StyleEnforcement;

        protected override ChatOptions GetDefaultOptions() => new(
            Model: "gpt-4o",
            Temperature: 0.3f,
            MaxTokens: 2048
        );

        protected override async Task<IDictionary<string, object>> PrepareContextAsync(
            AgentRequest request, CancellationToken ct)
        {
            var context = await contextOrchestrator.AssembleAsync(
                new ContextGatheringRequest(request.DocumentPath, null, request.Selection, AgentId, null),
                new ContextBudget(MaxTokens: 4000, RequiredStrategies: ["style"], ExcludedStrategies: null),
                ct);

            return context.Variables;
        }
    }
    ```
*   **v0.7.3c:** **Edit Suggestion Model.** Parse structured edit responses:
    ```csharp
    public record EditSuggestion(
        string OriginalText,
        string RevisedText,
        string Explanation,
        EditSeverity Severity,
        EditCategory Category,
        TextSpan Location
    );

    public enum EditSeverity { Minor, Moderate, Significant }
    public enum EditCategory { Grammar, Clarity, Structure, Style, Consistency }

    public interface IEditSuggestionParser
    {
        IReadOnlyList<EditSuggestion> Parse(string agentResponse);
    }
    ```
    *   Parse markdown-formatted edit blocks from agent response.
    *   Map suggestions to document locations for inline display.
*   **v0.7.3d:** **Edit Review UI.** Display suggestions for user approval:
    *   List view of all suggestions with severity icons.
    *   Click to navigate to location in editor.
    *   "Accept" / "Reject" / "Accept All" actions.
    *   Show diff preview on hover.
    *   Track acceptance rate for analytics.

---

## v0.7.4: The Simplifier Agent (Readability)
**Goal:** Build an agent specialized in reducing complexity and improving readability for broader audiences.

*   **v0.7.4a:** **Simplifier Prompt Templates.** Create focused prompts for simplification:
    ```yaml
    template_id: "specialist-simplifier"
    name: "Simplifier Specialist"
    system_prompt: |
      You are a writing coach specializing in clarity and accessibility.
      Your goal is to make complex content understandable to a wider audience.

      Techniques to apply:
      - Replace jargon with plain language (provide glossary if needed)
      - Break long sentences into shorter ones
      - Use active voice instead of passive
      - Add transitional phrases for flow
      - Simplify nested clauses

      {{#target_reading_level}}
      Target reading level: {{target_reading_level}}
      {{/target_reading_level}}
      {{^target_reading_level}}
      Target reading level: 8th grade (Flesch-Kincaid)
      {{/target_reading_level}}

      Preserve:
      - Technical accuracy (don't oversimplify facts)
      - Key terminology (explain rather than remove)
      - The author's intent and conclusions
    ```
*   **v0.7.4b:** **Simplifier Agent Implementation.** Create `SimplifierAgent`:
    ```csharp
    [RequiresLicense(LicenseTier.WriterPro)]
    [AgentDefinition("simplifier", "The Simplifier", "Reduces complexity for broader audiences")]
    public class SimplifierAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        IContextOrchestrator contextOrchestrator,
        IVoiceMetricsService voiceMetrics,
        IPromptTemplateRepository templates,
        ILogger<SimplifierAgent> logger) : BaseAgent(llm, renderer, templates, logger)
    {
        public override string AgentId => "simplifier";
        public override string Name => "The Simplifier";

        protected override async Task<IDictionary<string, object>> PrepareContextAsync(
            AgentRequest request, CancellationToken ct)
        {
            var context = await base.PrepareContextAsync(request, ct);

            // Add readability metrics for current text
            if (!string.IsNullOrEmpty(request.Selection))
            {
                var metrics = voiceMetrics.Calculate(request.Selection);
                context["current_flesch_kincaid"] = metrics.FleschKincaidGrade;
                context["current_gunning_fog"] = metrics.GunningFogIndex;
            }

            return context;
        }
    }
    ```
*   **v0.7.4c:** **Readability Comparison.** Show before/after metrics:
    *   Calculate Flesch-Kincaid, Gunning Fog for original text.
    *   Calculate same metrics for simplified version.
    *   Display comparison chart: "Grade 14 → Grade 8".
    *   Highlight specific improvements (sentence length reduction, passive → active).
*   **v0.7.4d:** **Audience Presets.** Define target audience configurations:
    ```csharp
    public record AudiencePreset(
        string PresetId,
        string Name,
        string Description,
        int TargetGradeLevel,
        bool AvoidJargon,
        bool PreferActiveVoice,
        int MaxSentenceLength
    );
    ```
    *   Presets: "General Public", "Technical Audience", "Executive Summary", "International (ESL)".
    *   User-defined presets stored in workspace settings.
    *   Quick-select dropdown in Simplifier panel.

---

## v0.7.5: The Tuning Agent (Style Enforcement)
**Goal:** Build an agent that automatically rewrites content to resolve style violations detected by the Linter.

*   **v0.7.5a:** **Linter Integration.** Connect to style linting from v0.2.x:
    ```csharp
    public interface ILinterBridge
    {
        Task<IReadOnlyList<LintViolation>> GetViolationsAsync(string documentPath, CancellationToken ct);
        Task<IReadOnlyList<LintViolation>> GetViolationsForRangeAsync(string documentPath, TextSpan range, CancellationToken ct);
    }

    public record LintViolation(
        string RuleId,
        string Message,
        TextSpan Location,
        LintSeverity Severity,
        string? SuggestedFix,
        string? RuleCategory
    );
    ```
    *   Subscribe to `LintingCompletedEvent` (v0.2.3b) for real-time updates.
    *   Cache violations per document to avoid re-linting.
*   **v0.7.5b:** **Tuning Agent Implementation.** Create `TuningAgent`:
    ```csharp
    [RequiresLicense(LicenseTier.Teams)]
    [AgentDefinition("tuning", "The Tuning Agent", "Automatically resolves style violations")]
    public class TuningAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        ILinterBridge linterBridge,
        IStyleRuleRepository styleRules,
        IPromptTemplateRepository templates,
        ILogger<TuningAgent> logger) : BaseAgent(llm, renderer, templates, logger)
    {
        public override string AgentId => "tuning";
        public override string Name => "The Tuning Agent";
        public override AgentCapabilities Capabilities =>
            AgentCapabilities.Chat | AgentCapabilities.DocumentContext | AgentCapabilities.StyleEnforcement;

        public async Task<TuningResult> TuneSelectionAsync(string documentPath, TextSpan selection, CancellationToken ct)
        {
            // 1. Get violations in selection
            var violations = await linterBridge.GetViolationsForRangeAsync(documentPath, selection, ct);
            if (violations.Count == 0)
                return TuningResult.NoViolations();

            // 2. Get relevant style rules
            var rules = await GetRulesForViolations(violations, ct);

            // 3. Build prompt with violations and rules
            var context = new Dictionary<string, object>
            {
                ["violations"] = FormatViolations(violations),
                ["style_rules"] = FormatRules(rules),
                ["original_text"] = await GetTextForRange(documentPath, selection)
            };

            // 4. Request rewrite
            var response = await InvokeAsync(new AgentRequest("Rewrite to fix all violations", DocumentPath: documentPath), ct);

            return new TuningResult(response.Content, violations, /* diff */);
        }
    }
    ```
*   **v0.7.5c:** **Tuning Prompt Templates.** Create specialized prompts:
    ```yaml
    template_id: "specialist-tuning"
    system_prompt: |
      You are a style enforcement agent. Your task is to rewrite text to comply with style rules.

      VIOLATIONS TO FIX:
      {{violations}}

      STYLE RULES:
      {{style_rules}}

      Requirements:
      1. Fix ALL listed violations
      2. Preserve the original meaning exactly
      3. Make minimal changes beyond violation fixes
      4. Explain each change briefly
    user_prompt: |
      Original text:
      """
      {{original_text}}
      """

      Rewrite this text to fix all style violations while preserving meaning.
    ```
*   **v0.7.5d:** **Batch Tuning Mode.** Process multiple violations at once:
    *   "Tune Document" command processes all violations in file.
    *   Paragraph-by-paragraph processing to manage token limits.
    *   Progress indicator showing violations fixed / total.
    *   Publish `TuningCompletedEvent` with summary statistics.
    *   Undo stack: Single undo reverts entire batch.

---

## v0.7.6: The Summarizer Agent (Metadata Generation)
**Goal:** Build a background agent that generates document metadata, summaries, and tag suggestions.

*   **v0.7.6a:** **Summary Model.** Define summary output structures:
    ```csharp
    public record DocumentSummary(
        string Title,
        string OneLiner,
        string Abstract,
        IReadOnlyList<string> KeyPoints,
        IReadOnlyList<string> SuggestedTags,
        string? TargetAudience,
        int EstimatedReadingMinutes
    );

    public interface ISummarizerAgent
    {
        Task<DocumentSummary> SummarizeAsync(string documentPath, SummaryOptions options, CancellationToken ct);
        Task<string> GenerateAbstractAsync(string content, int maxWords, CancellationToken ct);
        Task<IReadOnlyList<string>> SuggestTagsAsync(string content, int maxTags, CancellationToken ct);
    }
    ```
*   **v0.7.6b:** **Summarizer Implementation.** Create `SummarizerAgent`:
    ```csharp
    [RequiresLicense(LicenseTier.WriterPro)]
    [AgentDefinition("summarizer", "The Summarizer", "Generates metadata and summaries")]
    public class SummarizerAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        IDocumentService documentService,
        IPromptTemplateRepository templates,
        ILogger<SummarizerAgent> logger) : BaseAgent(llm, renderer, templates, logger), ISummarizerAgent
    {
        public async Task<DocumentSummary> SummarizeAsync(string documentPath, SummaryOptions options, CancellationToken ct)
        {
            var content = await documentService.ReadContentAsync(documentPath, ct);

            // Chunk if document exceeds token limit
            if (EstimateTokens(content) > 8000)
            {
                return await SummarizeChunkedAsync(content, options, ct);
            }

            var response = await InvokeAsync(new AgentRequest(
                "Generate a comprehensive document summary",
                DocumentPath: documentPath), ct);

            return ParseSummaryResponse(response.Content);
        }
    }
    ```
*   **v0.7.6c:** **Background Summarization.** Auto-generate summaries on document save:
    *   Subscribe to `DocumentSavedEvent` (v0.1.4c).
    *   Debounce: Wait 5 seconds after last save before summarizing.
    *   Store summary in document frontmatter or sidecar `.meta.yaml` file.
    *   Configurable: Enable/disable auto-summarization in settings.
    ```csharp
    public class BackgroundSummarizerService(
        ISummarizerAgent summarizer,
        IMediator mediator,
        IOptions<SummarizerOptions> options) : IHostedService
    {
        public async Task HandleDocumentSaved(DocumentSavedEvent evt)
        {
            if (!options.Value.AutoSummarize) return;

            await Task.Delay(TimeSpan.FromSeconds(5)); // Debounce
            var summary = await summarizer.SummarizeAsync(evt.DocumentPath, SummaryOptions.Default, default);

            await mediator.Publish(new DocumentSummaryGeneratedEvent(evt.DocumentPath, summary));
        }
    }
    ```
*   **v0.7.6d:** **Summary Panel UI.** Display and edit generated metadata:
    *   Read-only display of generated summary.
    *   "Regenerate" button to request new summary.
    *   Edit mode to manually adjust tags and description.
    *   "Apply to Frontmatter" action to insert into document.
    *   Show generation timestamp and model used.

---

## v0.7.7: The Agent Workflows (Multi-Agent Orchestration)
**Goal:** Enable chaining multiple agents together for complex document processing workflows.

*   **v0.7.7a:** **Workflow Definition Model.** Define multi-step agent workflows:
    ```csharp
    public record AgentWorkflow(
        string WorkflowId,
        string Name,
        string Description,
        IReadOnlyList<WorkflowStep> Steps,
        WorkflowTrigger? Trigger
    );

    public record WorkflowStep(
        string StepId,
        string AgentId,
        string? PersonaId,
        string PromptOverride,
        IReadOnlyDictionary<string, string>? InputMappings,
        IReadOnlyDictionary<string, string>? OutputMappings,
        WorkflowStepCondition? Condition
    );

    public record WorkflowStepCondition(string Expression); // e.g., "violations.Count > 0"

    public enum WorkflowTrigger { Manual, OnSave, OnLint, Scheduled }
    ```
*   **v0.7.7b:** **Workflow Engine.** Implement `IWorkflowEngine`:
    ```csharp
    public interface IWorkflowEngine
    {
        Task<WorkflowResult> ExecuteAsync(AgentWorkflow workflow, WorkflowContext context, CancellationToken ct);
        IAsyncEnumerable<WorkflowStepResult> ExecuteStreamingAsync(AgentWorkflow workflow, WorkflowContext context, CancellationToken ct);
    }

    public record WorkflowContext(
        string DocumentPath,
        string? Selection,
        IReadOnlyDictionary<string, object> Variables
    );

    public record WorkflowResult(
        bool Success,
        IReadOnlyList<WorkflowStepResult> StepResults,
        TimeSpan TotalDuration,
        UsageMetrics TotalUsage
    );
    ```
    *   Execute steps sequentially, passing outputs to next step.
    *   Support conditional step execution.
    *   Aggregate token usage across all steps.
    *   Cancel workflow on any step failure (configurable).
*   **v0.7.7c:** **Built-in Workflows.** Create pre-defined workflows:
    ```yaml
    # workflows/full-review.yaml
    workflow_id: "full-review"
    name: "Full Document Review"
    description: "Editor → Simplifier → Tuning pipeline"
    steps:
      - step_id: "edit"
        agent_id: "editor"
        prompt_override: "Review for grammar and clarity issues"
        output_mappings:
          edit_suggestions: "$.suggestions"
      - step_id: "simplify"
        agent_id: "simplifier"
        condition: "settings.simplify_enabled"
        prompt_override: "Simplify complex sentences"
      - step_id: "tune"
        agent_id: "tuning"
        condition: "violations.Count > 0"
        prompt_override: "Fix remaining style violations"
    ```
*   **v0.7.7d:** **Workflow Builder UI.** Visual workflow composition:
    *   Drag-and-drop agent steps onto canvas.
    *   Connect steps to define flow.
    *   Configure per-step settings (persona, prompt override).
    *   "Run Workflow" button with progress visualization.
    *   Save custom workflows to workspace.

---

## v0.7.8: The Hardening (Quality & Performance)
**Goal:** Ensure the Specialists module is production-ready with comprehensive testing and optimization.

*   **v0.7.8a:** **Agent Test Framework.** Create testing utilities for agents:
    ```csharp
    public class AgentTestHarness
    {
        public AgentTestHarness WithMockLLM(Func<ChatRequest, ChatResponse> responseFactory);
        public AgentTestHarness WithContext(IDictionary<string, object> context);
        public Task<AgentResponse> InvokeAsync(IAgent agent, AgentRequest request);
        public void AssertPromptContains(string expectedText);
        public void AssertTokensUnder(int maxTokens);
    }
    ```
    *   Mock LLM responses for deterministic tests.
    *   Capture and inspect generated prompts.
    *   Verify context assembly correctness.
*   **v0.7.8b:** **Agent Quality Tests.** Create test suites for each specialist:
    *   **Editor:** Test grammar correction accuracy on curated test cases.
    *   **Simplifier:** Test readability improvement (before/after Flesch-Kincaid).
    *   **Tuning:** Test violation resolution rate on linted documents.
    *   **Summarizer:** Test summary accuracy against human-written summaries.
    *   Regression tests for prompt template changes.
*   **v0.7.8c:** **Performance Optimization.** Establish baselines:
    *   Context assembly: <300ms for full context gathering.
    *   Agent invocation (excluding LLM): <50ms overhead.
    *   Workflow execution: Linear scaling with step count.
    *   Memory: Agent instances pooled, not recreated per request.
    *   Implement prompt caching for repeated context patterns.
*   **v0.7.8d:** **Error Handling & Fallbacks.** Implement resilient agent execution:
    *   Agent timeout: Configurable per-agent (default: 60s).
    *   Retry failed agents with exponential backoff.
    *   Graceful degradation: If specialist fails, offer Co-pilot fallback.
    *   Parse errors: Handle malformed agent responses gracefully.
    *   Publish `AgentErrorEvent` with diagnostics for debugging.

---

## Dependencies on Prior Versions

| Component | Source Version | Usage in v0.7.x |
|:----------|:---------------|:----------------|
| `IAgent` | v0.6.6a | Base interface for all specialists |
| `IChatCompletionService` | v0.6.1a | LLM communication |
| `IPromptRenderer` | v0.6.3b | Template rendering |
| `IPromptTemplateRepository` | v0.6.3c | Template storage |
| `IContextInjector` | v0.6.3d | Base context assembly (extended) |
| `IAgentRegistry` | v0.6.6c | Agent discovery (extended) |
| `ILintingOrchestrator` | v0.2.3a | Style violation detection |
| `IStyleRuleRepository` | v0.2.1b | Style rule access |
| `IVoiceMetricsService` | v0.3.3a | Readability calculations |
| `ISemanticSearchService` | v0.4.5a | RAG context gathering |
| `IRobustFileSystemWatcher` | v0.1.2b | Config hot-reload |
| `IEditorService` | v0.1.3a | Document access |
| `ISettingsService` | v0.1.6a | User preferences |
| `ILicenseContext` | v0.0.4c | Feature gating |
| `IMediator` | v0.0.7a | Event publishing |

---

## MediatR Events Introduced

| Event | Description |
|:------|:------------|
| `AgentPersonaSwitchedEvent` | User changed active persona |
| `ContextAssembledEvent` | Context gathering completed with fragment details |
| `EditSuggestionsGeneratedEvent` | Editor agent produced suggestions |
| `EditSuggestionAcceptedEvent` | User accepted an edit suggestion |
| `SimplificationCompletedEvent` | Simplifier finished with metrics |
| `TuningCompletedEvent` | Tuning agent resolved violations |
| `DocumentSummaryGeneratedEvent` | Summarizer produced metadata |
| `WorkflowStartedEvent` | Multi-agent workflow initiated |
| `WorkflowStepCompletedEvent` | Individual workflow step finished |
| `WorkflowCompletedEvent` | Full workflow execution finished |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `YamlDotNet` | 15.x | Agent/workflow configuration parsing (extends v0.2.1a usage) |
| (No new packages) | — | v0.7.x builds on existing dependencies |

---

## License Gating Summary

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| Co-pilot Agent | — | ✓ | ✓ | ✓ |
| Editor Agent | — | ✓ | ✓ | ✓ |
| Simplifier Agent | — | ✓ | ✓ | ✓ |
| Summarizer Agent | — | ✓ | ✓ | ✓ |
| Tuning Agent | — | — | ✓ | ✓ |
| Custom agent personas | — | — | ✓ | ✓ |
| Agent workflows | — | — | ✓ | ✓ |
| Workflow builder UI | — | — | — | ✓ |
| Background summarization | — | ✓ | ✓ | ✓ |
| Batch tuning | — | — | ✓ | ✓ |

---

## Implementation Guide: Sample Workflow for v0.7.3b (Editor Agent)

**LCS-01 (Design Composition)**
*   **Class:** `EditorAgent` extending `BaseAgent` with editing-focused capabilities.
*   **Template:** `specialist-editor` with grammar/clarity/structure guidelines.
*   **Temperature:** Low (0.3) for consistent, precise corrections.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Submit text with known grammar errors. Assert all errors identified.
*   **Test:** Submit correct text. Assert no false positives.
*   **Test:** Submit text with style violations. Assert style rules applied.
*   **Test:** Verify edit suggestions parse correctly from response.

**LCS-03 (Performance Log)**
1.  **Implement `EditorAgent`** (see v0.7.3b code block above).
2.  **Create `BaseAgent` for shared functionality:**
    ```csharp
    public abstract class BaseAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        IPromptTemplateRepository templates,
        ILogger logger) : IAgent
    {
        public abstract string AgentId { get; }
        public abstract string Name { get; }
        public virtual string Description => "";
        public virtual AgentCapabilities Capabilities => AgentCapabilities.Chat;
        public IPromptTemplate Template => templates.GetTemplate(GetTemplateId());

        protected virtual string GetTemplateId() => $"specialist-{AgentId}";
        protected virtual ChatOptions GetDefaultOptions() => new();

        public async Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken ct)
        {
            var context = await PrepareContextAsync(request, ct);
            context["user_input"] = request.UserMessage;

            var messages = renderer.RenderMessages(Template, context);
            var response = await llm.CompleteAsync(new ChatRequest(messages, GetDefaultOptions()), ct);

            return new AgentResponse(response.Content, null,
                new UsageMetrics(response.PromptTokens, response.CompletionTokens, 0));
        }

        protected virtual Task<IDictionary<string, object>> PrepareContextAsync(
            AgentRequest request, CancellationToken ct) => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>());
    }
    ```
3.  **Register:** Add to DI in `AgentsModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IAgent, EditorAgent>();
    services.AddSingleton<IAgent, SimplifierAgent>();
    services.AddSingleton<IAgent, TuningAgent>();
    services.AddSingleton<IAgent, SummarizerAgent>();
    ```

---

## Implementation Guide: Sample Workflow for v0.7.7b (Workflow Engine)

**LCS-01 (Design Composition)**
*   **Interface:** `IWorkflowEngine` orchestrating multi-step agent execution.
*   **Flow:** Load workflow → Execute steps sequentially → Pass outputs → Aggregate results.
*   **Conditions:** Evaluate step conditions using expression engine.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Execute two-step workflow. Assert both agents invoked in order.
*   **Test:** Execute workflow with false condition. Assert step skipped.
*   **Test:** Execute workflow with step failure. Assert workflow aborts (configurable).
*   **Test:** Verify token usage aggregated correctly across steps.

**LCS-03 (Performance Log)**
1.  **Implement `WorkflowEngine`:**
    ```csharp
    public class WorkflowEngine(
        IAgentRegistry agents,
        IExpressionEvaluator evaluator,
        ILogger<WorkflowEngine> logger) : IWorkflowEngine
    {
        public async Task<WorkflowResult> ExecuteAsync(
            AgentWorkflow workflow, WorkflowContext context, CancellationToken ct)
        {
            var stepResults = new List<WorkflowStepResult>();
            var variables = new Dictionary<string, object>(context.Variables);
            var totalUsage = new UsageMetrics(0, 0, 0);
            var stopwatch = Stopwatch.StartNew();

            foreach (var step in workflow.Steps)
            {
                // Check condition
                if (step.Condition is not null &&
                    !evaluator.Evaluate<bool>(step.Condition.Expression, variables))
                {
                    stepResults.Add(WorkflowStepResult.Skipped(step.StepId));
                    continue;
                }

                // Get agent
                var agent = step.PersonaId is not null
                    ? agents.GetAgentWithPersona(step.AgentId, step.PersonaId)
                    : agents.GetAgent(step.AgentId);

                // Execute
                var request = BuildRequest(step, variables, context);
                var response = await agent.InvokeAsync(request, ct);

                // Map outputs
                MapOutputs(step.OutputMappings, response, variables);

                stepResults.Add(new WorkflowStepResult(step.StepId, true, response));
                totalUsage = totalUsage.Add(response.Usage);
            }

            return new WorkflowResult(true, stepResults, stopwatch.Elapsed, totalUsage);
        }
    }
    ```
2.  **Register:** Add to DI in `AgentsModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IWorkflowEngine, WorkflowEngine>();
    ```
