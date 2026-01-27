# Lexichord Conductors Roadmap (v0.6.1 - v0.6.8)

In v0.5.x, we built "The Retrieval" — advanced search with hybrid ranking, citations, and contextual expansion. In v0.6.x, we introduce **The Conductors**: the AI agents that transform Lexichord from a smart search tool into an intelligent writing assistant. This phase establishes the LLM Gateway, prompt templating, streaming UI, and the foundational "Co-pilot" agent.

**Architectural Note:** This version introduces `Lexichord.Modules.Agents` as the orchestration layer for AI interactions. The module depends on `Lexichord.Modules.RAG` (v0.4.x/v0.5.x) for context retrieval and `Lexichord.Abstractions` for provider-agnostic interfaces. **License Gating:** Basic chat is WriterPro, streaming and advanced context assembly require Teams tier.

**Total Sub-Parts:** 32 distinct implementation steps.

---

## v0.6.1: The Gateway (LLM Provider Abstraction)
**Goal:** Create a unified interface for communicating with multiple LLM providers (OpenAI, Anthropic, local models).

*   **v0.6.1a:** **Chat Completion Abstractions.** Define core interfaces in `Lexichord.Abstractions`:
    ```csharp
    public interface IChatCompletionService
    {
        string ProviderName { get; }
        Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default);
        IAsyncEnumerable<StreamingChatToken> StreamAsync(ChatRequest request, CancellationToken ct = default);
    }

    public record ChatRequest(
        IReadOnlyList<ChatMessage> Messages,
        ChatOptions Options,
        CancellationToken CancellationToken = default
    );

    public record ChatMessage(ChatRole Role, string Content, string? Name = null);
    public enum ChatRole { System, User, Assistant, Tool }

    public record ChatResponse(
        string Content,
        int PromptTokens,
        int CompletionTokens,
        TimeSpan Duration,
        string? FinishReason
    );
    ```
*   **v0.6.1b:** **Chat Options Model.** Define configuration for LLM calls:
    ```csharp
    public record ChatOptions(
        string Model = "gpt-4o-mini",
        float Temperature = 0.7f,
        int MaxTokens = 2048,
        float TopP = 1.0f,
        float FrequencyPenalty = 0.0f,
        float PresencePenalty = 0.0f,
        IReadOnlyList<string>? StopSequences = null
    );
    ```
    *   Model-specific defaults loaded from configuration.
    *   Validation: Temperature 0-2, MaxTokens > 0, TopP 0-1.
*   **v0.6.1c:** **Provider Registry.** Implement `ILLMProviderRegistry` for dynamic provider selection:
    ```csharp
    public interface ILLMProviderRegistry
    {
        IReadOnlyList<LLMProviderInfo> AvailableProviders { get; }
        IChatCompletionService GetProvider(string providerName);
        IChatCompletionService GetDefaultProvider();
        void SetDefaultProvider(string providerName);
    }

    public record LLMProviderInfo(
        string Name,
        string DisplayName,
        IReadOnlyList<string> SupportedModels,
        bool IsConfigured,
        bool SupportsStreaming
    );
    ```
    *   Register providers via DI with named registrations.
    *   Default provider stored in `ISettingsService` (v0.1.6a).
*   **v0.6.1d:** **API Key Management UI.** Extend Settings dialog (v0.1.6a) with LLM configuration:
    *   Provider selection dropdown.
    *   API key input with "Test Connection" button.
    *   Keys stored via `ISecureVault` (v0.0.6a) with provider-prefixed keys (`openai:api-key`, `anthropic:api-key`).
    *   Show connection status indicator per provider.

---

## v0.6.2: The Providers (OpenAI & Anthropic Connectors)
**Goal:** Implement concrete LLM provider integrations with proper error handling and retry logic.

*   **v0.6.2a:** **OpenAI Connector.** Implement `OpenAIChatCompletionService`:
    *   Use `HttpClient` to POST to `https://api.openai.com/v1/chat/completions`.
    *   Support models: `gpt-4o`, `gpt-4o-mini`, `gpt-4-turbo`, `gpt-3.5-turbo`.
    *   Parse response JSON using `System.Text.Json`.
    *   Handle streaming via Server-Sent Events (SSE) with `data: [DONE]` terminator.
    ```csharp
    [RequiresLicense(LicenseTier.WriterPro)]
    public class OpenAIChatCompletionService(
        IHttpClientFactory httpFactory,
        ISecureVault vault,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIChatCompletionService> logger) : IChatCompletionService
    {
        public string ProviderName => "OpenAI";
        // Implementation...
    }
    ```
*   **v0.6.2b:** **Anthropic Connector.** Implement `AnthropicChatCompletionService`:
    *   Use Messages API: `https://api.anthropic.com/v1/messages`.
    *   Support models: `claude-3-5-sonnet-20241022`, `claude-3-opus-20240229`, `claude-3-haiku-20240307`.
    *   Map `ChatMessage` to Anthropic's format (separate system prompt handling).
    *   Handle streaming via SSE with event types: `message_start`, `content_block_delta`, `message_stop`.
    *   Set required headers: `anthropic-version`, `x-api-key`.
*   **v0.6.2c:** **Retry Policy Implementation.** Configure Polly for resilient API calls:
    ```csharp
    var retryPolicy = Policy
        .Handle<HttpRequestException>()
        .OrResult<HttpResponseMessage>(r =>
            r.StatusCode == HttpStatusCode.TooManyRequests ||
            r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, delay, attempt, ctx) =>
                logger.LogWarning("Retry {Attempt} after {Delay}ms: {Reason}",
                    attempt, delay.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString())
        );
    ```
    *   Circuit breaker: Open after 5 consecutive failures, half-open after 30 seconds.
    *   Timeout policy: 60 seconds per request (streaming), 30 seconds (non-streaming).
*   **v0.6.2d:** **Token Counting Service.** Implement `ITokenCounter` for cost estimation:
    ```csharp
    public interface ITokenCounter
    {
        int CountTokens(string text, string model);
        int CountTokens(IEnumerable<ChatMessage> messages, string model);
        int EstimateResponseTokens(int promptTokens, int maxTokens);
    }
    ```
    *   Use `Microsoft.ML.Tokenizers` for GPT models (cl100k_base encoding).
    *   Anthropic tokenization approximation (character-based until official tokenizer).
    *   Display token count in UI before sending request.

---

## v0.6.3: The Template Engine (Dynamic Prompt Assembly)
**Goal:** Build a flexible prompt templating system that injects context, style rules, and user instructions.

*   **v0.6.3a:** **Template Abstractions.** Define prompt template interfaces:
    ```csharp
    public interface IPromptTemplate
    {
        string TemplateId { get; }
        string Name { get; }
        string Description { get; }
        string SystemPromptTemplate { get; }
        string UserPromptTemplate { get; }
        IReadOnlyList<string> RequiredVariables { get; }
        IReadOnlyList<string> OptionalVariables { get; }
    }

    public interface IPromptRenderer
    {
        string Render(string template, IDictionary<string, object> variables);
        ChatMessage[] RenderMessages(IPromptTemplate template, IDictionary<string, object> variables);
    }
    ```
*   **v0.6.3b:** **Mustache Renderer.** Implement template rendering using Mustache syntax:
    *   Support variables: `{{variable}}`, `{{#section}}...{{/section}}`, `{{^inverted}}...{{/inverted}}`.
    *   Support partials: `{{> partial_name}}` for reusable template fragments.
    *   HTML escaping disabled by default (use `{{{raw}}}` syntax awareness).
    *   Use `Stubble.Core` NuGet package for rendering.
    ```csharp
    public class MustachePromptRenderer(IStubbleRenderer stubble) : IPromptRenderer
    {
        public string Render(string template, IDictionary<string, object> variables)
        {
            return stubble.Render(template, variables);
        }
    }
    ```
*   **v0.6.3c:** **Template Repository.** Implement `IPromptTemplateRepository`:
    *   Load built-in templates from embedded resources (YAML format).
    *   Support user-defined templates from workspace `.lexichord/prompts/` directory.
    *   Hot-reload on file change (using `IRobustFileSystemWatcher` from v0.1.2b).
    *   Template validation on load (check required variables).
    ```yaml
    # Example: .lexichord/prompts/editor.yaml
    template_id: "co-pilot-editor"
    name: "Co-pilot Editor"
    description: "General writing assistance"
    system_prompt: |
      You are a writing assistant for technical documentation.
      {{#style_rules}}
      Follow these style rules:
      {{style_rules}}
      {{/style_rules}}
      {{#context}}
      Reference context:
      {{context}}
      {{/context}}
    user_prompt: "{{user_input}}"
    required_variables: ["user_input"]
    optional_variables: ["style_rules", "context"]
    ```
*   **v0.6.3d:** **Context Injection Service.** Implement `IContextInjector` for automatic context assembly:
    ```csharp
    public interface IContextInjector
    {
        Task<IDictionary<string, object>> AssembleContextAsync(ContextRequest request, CancellationToken ct);
    }

    public record ContextRequest(
        string? CurrentDocumentPath,
        int? CursorPosition,
        string? SelectedText,
        bool IncludeStyleRules,
        bool IncludeRAGContext,
        int MaxRAGChunks = 3
    );
    ```
    *   Fetch style rules from `IStyleRuleRepository` (v0.2.1b) if enabled.
    *   Fetch relevant chunks from `ISemanticSearchService` (v0.4.5a) based on selection or cursor context.
    *   Format context for injection into templates.

---

## v0.6.4: The Chat Interface (Co-pilot Foundation)
**Goal:** Build the user-facing chat panel for conversational AI assistance.

*   **v0.6.4a:** **Chat Panel View.** Create `CoPilotView.axaml`:
    *   Message list (ItemsControl with virtualization) displaying conversation history.
    *   Input area with multi-line TextBox and Send button.
    *   Model selector dropdown (from `ILLMProviderRegistry`).
    *   Context indicator showing what's being sent (document name, style guide).
    *   Register in `ShellRegion.Right` via `IRegionManager` (v0.1.1b).
    ```csharp
    public partial class CoPilotViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<ChatMessageViewModel> _messages = [];
        [ObservableProperty] private string _inputText = string.Empty;
        [ObservableProperty] private bool _isGenerating;
        [ObservableProperty] private string _selectedModel = "gpt-4o-mini";

        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendAsync(CancellationToken ct);
    }
    ```
*   **v0.6.4b:** **Message Rendering.** Create `ChatMessageView` UserControl:
    *   Different styling for User/Assistant/System messages.
    *   Markdown rendering in assistant responses using `Markdig` (from v0.1.3b).
    *   Code block syntax highlighting with language detection.
    *   Copy button for code blocks and full responses.
    *   Timestamp display (relative: "2 minutes ago").
*   **v0.6.4c:** **Conversation Management.** Implement conversation state handling:
    *   Store conversation history in memory (current session).
    *   "New Conversation" button to clear history.
    *   "Export Conversation" to Markdown file.
    *   Conversation title auto-generated from first user message.
    *   Maximum history length configurable (default: 50 messages).
*   **v0.6.4d:** **Context Panel.** Add collapsible panel showing injected context:
    *   "Style Rules Active" indicator with rule count.
    *   "RAG Context" showing retrieved chunk summaries.
    *   "Document Context" showing current file info.
    *   Toggle switches to enable/disable context sources per-conversation.

---

## v0.6.5: The Stream (Real-time Response Rendering)
**Goal:** Implement streaming token display for natural, responsive AI interactions.

*   **v0.6.5a:** **Streaming Token Model.** Define streaming data structures:
    ```csharp
    public record StreamingChatToken(
        string Text,
        int Index,
        bool IsComplete,
        string? FinishReason
    );

    public interface IStreamingChatHandler
    {
        Task OnTokenReceived(StreamingChatToken token);
        Task OnStreamComplete(ChatResponse fullResponse);
        Task OnStreamError(Exception error);
    }
    ```
*   **v0.6.5b:** **SSE Parser.** Implement Server-Sent Events parsing for streaming:
    *   Parse `data:` lines from HTTP response stream.
    *   Handle OpenAI format: `{"choices":[{"delta":{"content":"token"}}]}`.
    *   Handle Anthropic format: `{"type":"content_block_delta","delta":{"text":"token"}}`.
    *   Detect stream end (`[DONE]` for OpenAI, `message_stop` for Anthropic).
    ```csharp
    public async IAsyncEnumerable<StreamingChatToken> ParseSSEStreamAsync(
        Stream responseStream,
        string provider,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(responseStream);
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line?.StartsWith("data: ") == true)
            {
                var json = line[6..];
                if (json == "[DONE]") yield break;
                yield return ParseToken(json, provider);
            }
        }
    }
    ```
*   **v0.6.5c:** **Streaming UI Handler.** Implement real-time message updates:
    *   Add tokens to message content incrementally.
    *   Throttle UI updates (batch tokens every 50ms) for performance.
    *   Show typing indicator during initial connection.
    *   Smooth scroll to keep latest content visible.
    *   Cancel button to abort in-progress generation.
    ```csharp
    public class StreamingChatHandler(CoPilotViewModel viewModel) : IStreamingChatHandler
    {
        private readonly StringBuilder _buffer = new();
        private readonly Timer _uiUpdateTimer;

        public async Task OnTokenReceived(StreamingChatToken token)
        {
            _buffer.Append(token.Text);
            // UI update batched via timer
        }
    }
    ```
*   **v0.6.5d:** **License Gating.** Streaming requires Teams tier:
    ```csharp
    if (licenseContext.Tier < LicenseTier.Teams && request.Stream)
    {
        logger.LogInformation("Streaming downgraded to batch for {Tier} license", licenseContext.Tier);
        return await CompleteAsync(request, ct); // Fallback to non-streaming
    }
    ```
    *   WriterPro users see full response after generation completes.
    *   Teams+ users see real-time streaming.

---

## v0.6.6: The Co-pilot Agent (Conversational Assistant)
**Goal:** Wire together all components into a functional conversational writing assistant.

*   **v0.6.6a:** **Agent Abstractions.** Define the agent contract in Abstractions:
    ```csharp
    public interface IAgent
    {
        string AgentId { get; }
        string Name { get; }
        string Description { get; }
        IPromptTemplate Template { get; }
        AgentCapabilities Capabilities { get; }
        Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken ct = default);
    }

    [Flags]
    public enum AgentCapabilities
    {
        None = 0,
        Chat = 1,
        DocumentContext = 2,
        RAGContext = 4,
        StyleEnforcement = 8,
        Streaming = 16
    }

    public record AgentRequest(
        string UserMessage,
        IReadOnlyList<ChatMessage>? History = null,
        string? DocumentPath = null,
        string? Selection = null
    );

    public record AgentResponse(
        string Content,
        IReadOnlyList<Citation>? Citations,
        UsageMetrics Usage
    );

    public record UsageMetrics(int PromptTokens, int CompletionTokens, decimal EstimatedCost);
    ```
*   **v0.6.6b:** **Co-pilot Implementation.** Create the foundational writing assistant agent:
    ```csharp
    [RequiresLicense(LicenseTier.WriterPro)]
    public class CoPilotAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        IContextInjector contextInjector,
        IPromptTemplateRepository templates,
        ILogger<CoPilotAgent> logger) : IAgent
    {
        public string AgentId => "co-pilot";
        public string Name => "Co-pilot";
        public string Description => "General writing assistant with document and style awareness";
        public AgentCapabilities Capabilities =>
            AgentCapabilities.Chat | AgentCapabilities.DocumentContext |
            AgentCapabilities.RAGContext | AgentCapabilities.StyleEnforcement |
            AgentCapabilities.Streaming;

        public async Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken ct)
        {
            // 1. Assemble context
            var context = await contextInjector.AssembleContextAsync(
                new ContextRequest(request.DocumentPath, null, request.Selection, true, true), ct);

            // 2. Render prompt
            var template = templates.GetTemplate("co-pilot-editor");
            context["user_input"] = request.UserMessage;
            var messages = renderer.RenderMessages(template, context);

            // 3. Add conversation history
            var fullMessages = (request.History ?? []).Concat(messages).ToList();

            // 4. Call LLM
            var response = await llm.CompleteAsync(new ChatRequest(fullMessages, new ChatOptions()), ct);

            // 5. Extract citations if RAG context was used
            var citations = ExtractCitations(response.Content, context);

            return new AgentResponse(response.Content, citations,
                new UsageMetrics(response.PromptTokens, response.CompletionTokens, CalculateCost(response)));
        }
    }
    ```
*   **v0.6.6c:** **Agent Registry.** Implement `IAgentRegistry` for agent discovery:
    ```csharp
    public interface IAgentRegistry
    {
        IReadOnlyList<IAgent> AvailableAgents { get; }
        IAgent GetAgent(string agentId);
        IAgent GetDefaultAgent();
    }
    ```
    *   Scan loaded modules for `IAgent` implementations.
    *   Filter by license tier (show upgrade prompts for locked agents).
    *   Default agent configurable in settings.
*   **v0.6.6d:** **Usage Tracking.** Implement token and cost tracking:
    *   Store per-conversation usage metrics.
    *   Display running total in chat panel footer.
    *   Monthly usage summary in Settings (Teams+ feature).
    *   Publish `AgentInvocationEvent` with metrics for telemetry.
    ```csharp
    public record AgentInvocationEvent(
        string AgentId,
        string Model,
        int PromptTokens,
        int CompletionTokens,
        TimeSpan Duration,
        bool Streamed
    ) : INotification;
    ```

---

## v0.6.7: The Document Bridge (Editor Integration)
**Goal:** Seamlessly integrate the Co-pilot with the active document in the editor.

*   **v0.6.7a:** **Selection Context.** Send selected text to Co-pilot:
    *   Right-click context menu: "Ask Co-pilot about selection".
    *   Keyboard shortcut: `Ctrl+Shift+A` (configurable via `IKeyBindingService` from v0.1.5b).
    *   Selection passed as `AgentRequest.Selection`.
    *   Pre-fill user input: "Explain this:" or "Improve this:" based on selection length.
*   **v0.6.7b:** **Inline Suggestions.** Display agent responses inline in editor:
    *   "Insert at Cursor" button in chat response.
    *   "Replace Selection" when response relates to selected text.
    *   Preview mode: Show proposed text as ghost overlay before accepting.
    *   Undo support: Single `Ctrl+Z` reverts inserted AI text.
    ```csharp
    public interface IEditorInsertionService
    {
        Task InsertAtCursorAsync(string text);
        Task ReplaceSelectionAsync(string text);
        Task ShowPreviewAsync(string text, TextSpan location);
        Task AcceptPreviewAsync();
        Task RejectPreviewAsync();
    }
    ```
*   **v0.6.7c:** **Document-Aware Prompting.** Enhance context based on cursor position:
    *   Detect current section heading (using Markdown AST from v0.1.3b).
    *   Include 500 chars before and after cursor as "local context".
    *   Detect if user is in code block, table, or list for specialized prompts.
    *   Auto-suggest relevant agent based on context (e.g., "Code" section → Code helper).
*   **v0.6.7d:** **Quick Actions Panel.** Add floating toolbar near cursor:
    *   Shows on text selection (configurable delay: 500ms).
    *   Actions: "Improve", "Simplify", "Expand", "Fix Grammar".
    *   Each action maps to a pre-defined prompt template.
    *   Dismiss on click outside or Escape key.
    ```csharp
    public record QuickAction(string Id, string Label, string Icon, string PromptTemplate);

    public class QuickActionsService(IAgentRegistry agents, IPromptTemplateRepository templates)
    {
        public IReadOnlyList<QuickAction> GetActionsForContext(EditorContext context);
    }
    ```

---

## v0.6.8: The Hardening (Reliability & Performance)
**Goal:** Ensure the Agents module is production-ready with comprehensive testing, error handling, and optimization.

*   **v0.6.8a:** **Unit Test Suite.** Create comprehensive tests for all components:
    *   `ChatCompletionServiceTests`: Mock HTTP responses, verify request format, handle errors.
    *   `PromptRendererTests`: Template variable substitution, missing variable handling.
    *   `AgentTests`: Context assembly, response parsing, citation extraction.
    *   `StreamingParserTests`: SSE parsing for both providers, edge cases (empty chunks, errors).
*   **v0.6.8b:** **Integration Tests.** Test end-to-end agent workflows:
    *   Send message → Receive response → Verify token counts.
    *   Streaming: Verify all tokens received and assembled correctly.
    *   Context injection: RAG search executes, style rules included.
    *   Error scenarios: API timeout, rate limit, invalid API key.
*   **v0.6.8c:** **Performance Optimization.** Establish baselines and optimize:
    *   First token latency (streaming): <500ms after request sent.
    *   Context assembly: <200ms for style rules + 3 RAG chunks.
    *   Template rendering: <10ms for typical prompt.
    *   Memory: Conversation history capped to prevent unbounded growth.
    *   Implement request coalescing for rapid sequential queries.
*   **v0.6.8d:** **Error Handling & Recovery.** Implement resilient error management:
    *   API errors: User-friendly messages ("OpenAI is temporarily unavailable").
    *   Rate limits: Queue requests with estimated wait time display.
    *   Network errors: Retry with exponential backoff, offline indicator.
    *   Invalid responses: Graceful degradation, log for debugging.
    *   Token limit exceeded: Auto-truncate history, warn user.
    ```csharp
    public class ResilientChatService(
        IChatCompletionService inner,
        IAsyncPolicy<ChatResponse> policy,
        ILogger<ResilientChatService> logger) : IChatCompletionService
    {
        public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct)
        {
            try
            {
                return await policy.ExecuteAsync(() => inner.CompleteAsync(request, ct));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chat completion failed after retries");
                throw new AgentException("Unable to complete request. Please try again.", ex);
            }
        }
    }
    ```

---

## Dependencies on Prior Versions

| Component | Source Version | Usage in v0.6.x |
|:----------|:---------------|:----------------|
| `ISecureVault` | v0.0.6a | Store LLM API keys securely |
| `IMediator` | v0.0.7a | Publish agent events |
| `IRegionManager` | v0.1.1b | Register Co-pilot panel |
| `IRobustFileSystemWatcher` | v0.1.2b | Hot-reload prompt templates |
| `IEditorService` | v0.1.3a | Insert AI responses into editor |
| `Markdig` | v0.1.3b | Render Markdown in chat responses |
| `IKeyBindingService` | v0.1.5b | Keyboard shortcuts for agent actions |
| `ISettingsService` | v0.1.6a | Store provider preferences, model selection |
| `IStyleRuleRepository` | v0.2.1b | Inject style rules into prompts |
| `ISemanticSearchService` | v0.4.5a | Fetch RAG context for prompts |
| `ICitationService` | v0.5.2a | Attribute sources in agent responses |
| `ILicenseContext` | v0.0.4c | Gate agent features by tier |
| `Polly` | v0.0.5d | Retry policies for LLM API calls |

---

## MediatR Events Introduced

| Event | Description |
|:------|:------------|
| `AgentInvocationStartedEvent` | Agent request initiated with context summary |
| `AgentInvocationCompletedEvent` | Agent response received with metrics |
| `AgentInvocationFailedEvent` | Agent request failed with error details |
| `StreamingTokenReceivedEvent` | Token received during streaming (for telemetry) |
| `ConversationStartedEvent` | New conversation initiated |
| `ConversationExportedEvent` | Conversation exported to file |
| `QuickActionExecutedEvent` | User triggered inline quick action |
| `LLMProviderChangedEvent` | User switched default LLM provider |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Stubble.Core` | 1.10.x | Mustache template rendering |
| `Microsoft.ML.Tokenizers` | 0.22.x | Token counting (shared with v0.4.4c) |
| `System.Linq.Async` | 6.0.x | Async LINQ for streaming enumeration |

---

## License Gating Summary

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| LLM API configuration | — | ✓ | ✓ | ✓ |
| Basic chat (non-streaming) | — | ✓ | ✓ | ✓ |
| Streaming responses | — | — | ✓ | ✓ |
| Document context injection | — | ✓ | ✓ | ✓ |
| RAG context injection | — | ✓ | ✓ | ✓ |
| Style rule injection | — | ✓ | ✓ | ✓ |
| Custom prompt templates | — | — | ✓ | ✓ |
| Usage analytics | — | — | ✓ | ✓ |
| Quick actions toolbar | — | ✓ | ✓ | ✓ |
| Multiple LLM providers | — | — | — | ✓ |

---

## Implementation Guide: Sample Workflow for v0.6.2a (OpenAI Connector)

**LCS-01 (Design Composition)**
*   **Interface:** `IChatCompletionService` with `CompleteAsync` and `StreamAsync`.
*   **Transport:** `HttpClient` posting JSON to OpenAI Chat Completions API.
*   **Authentication:** Bearer token from `ISecureVault.GetSecretAsync("openai:api-key")`.

**LCS-02 (Rehearsal Strategy)**
*   **Test (Unit):** Mock HTTP response with canned completion. Assert response parsed correctly.
*   **Test (Streaming):** Mock SSE stream, verify tokens emitted in order.
*   **Test (Error):** Mock 429 response, verify retry policy activates.
*   **Test (Token Count):** Verify prompt/completion token counts extracted from response.

**LCS-03 (Performance Log)**
1.  **Implement `OpenAIChatCompletionService`:**
    ```csharp
    [RequiresLicense(LicenseTier.WriterPro)]
    public class OpenAIChatCompletionService(
        IHttpClientFactory httpFactory,
        ISecureVault vault,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIChatCompletionService> logger) : IChatCompletionService
    {
        public string ProviderName => "OpenAI";

        public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct)
        {
            var apiKey = await vault.GetSecretAsync("openai:api-key")
                ?? throw new InvalidOperationException("OpenAI API key not configured");

            using var client = httpFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = request.Options.Model,
                messages = request.Messages.Select(m => new { role = m.Role.ToString().ToLower(), content = m.Content }),
                temperature = request.Options.Temperature,
                max_tokens = request.Options.MaxTokens,
                stream = false
            };

            var response = await client.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions", payload, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(ct);

            return new ChatResponse(
                result.Choices[0].Message.Content,
                result.Usage.PromptTokens,
                result.Usage.CompletionTokens,
                TimeSpan.Zero, // Set from Stopwatch
                result.Choices[0].FinishReason
            );
        }

        public async IAsyncEnumerable<StreamingChatToken> StreamAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            // SSE implementation with stream: true
        }
    }
    ```
2.  **Register:** Add to DI in `AgentsModule.RegisterServices()`:
    ```csharp
    services.AddHttpClient("OpenAI");
    services.AddSingleton<IChatCompletionService, OpenAIChatCompletionService>();
    services.AddSingleton<ILLMProviderRegistry, LLMProviderRegistry>();
    ```

---

## Implementation Guide: Sample Workflow for v0.6.3b (Mustache Renderer)

**LCS-01 (Design Composition)**
*   **Interface:** `IPromptRenderer` with `Render` and `RenderMessages` methods.
*   **Library:** Stubble.Core for Mustache template processing.
*   **Configuration:** Disable HTML escaping for prompt content.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Render template with all variables provided. Assert output matches expected.
*   **Test:** Render template with missing optional variable. Assert section omitted.
*   **Test:** Render template with missing required variable. Assert exception thrown.
*   **Test:** Render template with list variable. Assert iteration works correctly.

**LCS-03 (Performance Log)**
1.  **Implement `MustachePromptRenderer`:**
    ```csharp
    public class MustachePromptRenderer : IPromptRenderer
    {
        private readonly StubbleVisitorRenderer _renderer;

        public MustachePromptRenderer()
        {
            _renderer = new StubbleBuilder()
                .Configure(settings => settings.SetIgnoreCaseOnKeyLookup(true))
                .Build();
        }

        public string Render(string template, IDictionary<string, object> variables)
        {
            return _renderer.Render(template, variables);
        }

        public ChatMessage[] RenderMessages(IPromptTemplate template, IDictionary<string, object> variables)
        {
            var systemContent = Render(template.SystemPromptTemplate, variables);
            var userContent = Render(template.UserPromptTemplate, variables);

            return
            [
                new ChatMessage(ChatRole.System, systemContent),
                new ChatMessage(ChatRole.User, userContent)
            ];
        }
    }
    ```
2.  **Register:** Add to DI in `AgentsModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IPromptRenderer, MustachePromptRenderer>();
    ```

---

## Implementation Guide: Sample Workflow for v0.6.6b (Co-pilot Agent)

**LCS-01 (Design Composition)**
*   **Interface:** `IAgent` with full capabilities for chat, document, RAG, and style context.
*   **Flow:** Assemble context → Render prompt → Call LLM → Extract citations → Return response.
*   **Dependencies:** All v0.6.1-v0.6.5 components integrated.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Invoke with plain message, no context. Assert LLM called with system + user messages.
*   **Test:** Invoke with document path. Assert context injector fetched document content.
*   **Test:** Invoke with selection. Assert selection included in context variables.
*   **Test:** Invoke with RAG enabled. Assert semantic search executed, citations extracted.

**LCS-03 (Performance Log)**
1.  **Implement full agent flow** (see v0.6.6b code block above).
2.  **Citation extraction:**
    ```csharp
    private IReadOnlyList<Citation>? ExtractCitations(string content, IDictionary<string, object> context)
    {
        if (!context.TryGetValue("rag_chunks", out var chunksObj) || chunksObj is not List<SearchHit> hits)
            return null;

        var citations = new List<Citation>();
        foreach (var hit in hits)
        {
            // Check if response references this chunk's content
            if (content.Contains(hit.Chunk.Content[..Math.Min(50, hit.Chunk.Content.Length)]))
            {
                citations.Add(_citationService.CreateCitation(hit));
            }
        }
        return citations.Count > 0 ? citations : null;
    }
    ```
3.  **Register:** Add to DI in `AgentsModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IAgent, CoPilotAgent>();
    services.AddSingleton<IAgentRegistry, AgentRegistry>();
    ```
