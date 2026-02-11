// -----------------------------------------------------------------------
// <copyright file="IntegrationTestBase.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Shared base class for v0.6.8b integration tests. Pre-configures
//   Moq mocks for all 9 CoPilotAgent dependencies with sensible defaults:
//   - MockLLMServer: batch response "Hello from Co-pilot." (50/25 tokens)
//   - MockStreamingHandler: captures OnTokenReceived/OnStreamComplete calls
//   - MockPromptRenderer: returns [System, User] ChatMessage pair
//   - MockContextInjector: returns empty context dictionary (overridable)
//   - MockTemplateRepository: returns stub IPromptTemplate "co-pilot-editor"
//   - MockCitationService: returns empty citation list
//   - MockLicenseContext: returns WriterPro tier (streaming off by default)
//   - MockSettingsService: returns sensible defaults for all keys
//   - Logger: NullLogger<CoPilotAgent>
//
//   Subclasses can override any mock setup before calling CreateAgent().
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Abstractions;
using Lexichord.Modules.Agents.Chat.Agents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Integration.Fixtures;

/// <summary>
/// Shared base class for CoPilotAgent integration tests.
/// </summary>
/// <remarks>
/// <para>
/// Provides pre-configured mocks for all 9 <see cref="CoPilotAgent"/> dependencies.
/// Subclasses can override mock setups before calling <see cref="CreateAgent"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8b as part of the Integration Tests.
/// </para>
/// </remarks>
public abstract class IntegrationTestBase : IDisposable
{
    // ── Mock Infrastructure ────────────────────────────────────────────

    /// <summary>
    /// Configurable mock LLM server wrapping <see cref="IChatCompletionService"/>.
    /// </summary>
    protected MockLLMServer LLMServer { get; }

    /// <summary>
    /// Mock streaming chat handler that captures token events.
    /// </summary>
    protected Mock<IStreamingChatHandler> MockStreamingHandler { get; }

    /// <summary>
    /// Mock prompt renderer that returns a [System, User] message pair.
    /// </summary>
    protected Mock<IPromptRenderer> MockPromptRenderer { get; }

    /// <summary>
    /// Mock context injector returning a configurable dictionary.
    /// </summary>
    protected Mock<IContextInjector> MockContextInjector { get; }

    /// <summary>
    /// Mock template repository returning a stub IPromptTemplate.
    /// </summary>
    protected Mock<IPromptTemplateRepository> MockTemplateRepository { get; }

    /// <summary>
    /// Mock citation service for creating citations from search hits.
    /// </summary>
    protected Mock<ICitationService> MockCitationService { get; }

    /// <summary>
    /// Mock license context with configurable tier.
    /// </summary>
    protected Mock<ILicenseContext> MockLicenseContext { get; }

    /// <summary>
    /// Mock settings service with configurable settings.
    /// </summary>
    protected Mock<ISettingsService> MockSettingsService { get; }

    /// <summary>
    /// Mock prompt template used by the agent.
    /// </summary>
    protected Mock<IPromptTemplate> MockTemplate { get; }

    /// <summary>
    /// Logger instance (null logger for tests).
    /// </summary>
    protected ILogger<CoPilotAgent> Logger { get; }

    /// <summary>
    /// Tokens received by the streaming handler, in order.
    /// </summary>
    protected List<Lexichord.Modules.Agents.Chat.Models.StreamingChatToken> ReceivedTokens { get; }

    /// <summary>
    /// The ChatResponse received by OnStreamComplete, if any.
    /// </summary>
    protected ChatResponse? StreamCompleteResponse { get; private set; }

    /// <summary>
    /// The Exception received by OnStreamError, if any.
    /// </summary>
    protected Exception? StreamError { get; private set; }

    // ── Constructor ────────────────────────────────────────────────────

    protected IntegrationTestBase()
    {
        ReceivedTokens = new List<Lexichord.Modules.Agents.Chat.Models.StreamingChatToken>();
        Logger = NullLogger<CoPilotAgent>.Instance;

        // 1. LLM Server — default: batch response
        LLMServer = new MockLLMServer();
        LLMServer.ConfigureResponse("Hello from Co-pilot.", promptTokens: 50, completionTokens: 25);

        // 2. Streaming Handler — captures token events
        MockStreamingHandler = new Mock<IStreamingChatHandler>();
        MockStreamingHandler
            .Setup(h => h.OnTokenReceived(It.IsAny<Lexichord.Modules.Agents.Chat.Models.StreamingChatToken>()))
            .Callback<Lexichord.Modules.Agents.Chat.Models.StreamingChatToken>(t => ReceivedTokens.Add(t))
            .Returns(Task.CompletedTask);
        MockStreamingHandler
            .Setup(h => h.OnStreamComplete(It.IsAny<ChatResponse>()))
            .Callback<ChatResponse>(r => StreamCompleteResponse = r)
            .Returns(Task.CompletedTask);
        MockStreamingHandler
            .Setup(h => h.OnStreamError(It.IsAny<Exception>()))
            .Callback<Exception>(e => StreamError = e)
            .Returns(Task.CompletedTask);

        // 3. Prompt Template stub
        MockTemplate = new Mock<IPromptTemplate>();
        MockTemplate.Setup(t => t.TemplateId).Returns("co-pilot-editor");
        MockTemplate.Setup(t => t.Name).Returns("Co-pilot Editor");
        MockTemplate.Setup(t => t.Description).Returns("Test template");
        MockTemplate.Setup(t => t.SystemPromptTemplate).Returns("You are a writing assistant.");
        MockTemplate.Setup(t => t.UserPromptTemplate).Returns("{{user_input}}");
        MockTemplate.Setup(t => t.RequiredVariables).Returns(new List<string> { "user_input" });
        MockTemplate.Setup(t => t.OptionalVariables).Returns(new List<string> { "style_rules", "context" });

        // 4. Template Repository — returns the mock template
        MockTemplateRepository = new Mock<IPromptTemplateRepository>();
        MockTemplateRepository
            .Setup(r => r.GetTemplate("co-pilot-editor"))
            .Returns(MockTemplate.Object);

        // 5. Prompt Renderer — returns [System, User] pair
        MockPromptRenderer = new Mock<IPromptRenderer>();
        MockPromptRenderer
            .Setup(r => r.RenderMessages(
                It.IsAny<IPromptTemplate>(),
                It.IsAny<IDictionary<string, object>>()))
            .Returns((IPromptTemplate _, IDictionary<string, object> vars) =>
                new[]
                {
                    ChatMessage.System("You are a writing assistant."),
                    ChatMessage.User(vars.ContainsKey("user_input")
                        ? vars["user_input"].ToString()!
                        : "default input"),
                });

        // 6. Context Injector — returns empty dictionary by default
        MockContextInjector = new Mock<IContextInjector>();
        MockContextInjector
            .Setup(c => c.AssembleContextAsync(
                It.IsAny<ContextRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>());

        // 7. Citation Service — returns empty list by default
        MockCitationService = new Mock<ICitationService>();
        MockCitationService
            .Setup(c => c.CreateCitations(It.IsAny<IEnumerable<SearchHit>>()))
            .Returns(new List<Citation>());

        // 8. License Context — WriterPro tier by default (no streaming)
        MockLicenseContext = new Mock<ILicenseContext>();
        MockLicenseContext
            .Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        // 9. Settings Service — sensible defaults
        MockSettingsService = new Mock<ISettingsService>();
        MockSettingsService
            .Setup(s => s.Get("Agent:Temperature", It.IsAny<double>()))
            .Returns(0.7);
        MockSettingsService
            .Setup(s => s.Get("Agent:MaxResponseTokens", It.IsAny<int>()))
            .Returns(2000);
        MockSettingsService
            .Setup(s => s.Get("Agent:MaxContextItems", It.IsAny<int>()))
            .Returns(10);
        MockSettingsService
            .Setup(s => s.Get("Agent:EnableStreaming", It.IsAny<bool>()))
            .Returns(true);
    }

    // ── Factory ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="CoPilotAgent"/> instance with all current mock configurations.
    /// </summary>
    /// <returns>A fully-configured CoPilotAgent for testing.</returns>
    protected CoPilotAgent CreateAgent()
    {
        return new CoPilotAgent(
            LLMServer.Service,
            MockStreamingHandler.Object,
            MockPromptRenderer.Object,
            MockContextInjector.Object,
            MockTemplateRepository.Object,
            MockCitationService.Object,
            MockLicenseContext.Object,
            MockSettingsService.Object,
            Logger);
    }

    /// <summary>
    /// Enables streaming mode by setting license tier to Teams.
    /// </summary>
    protected void EnableStreaming()
    {
        MockLicenseContext
            .Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.Teams);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        LLMServer.Dispose();
        GC.SuppressFinalize(this);
    }
}
