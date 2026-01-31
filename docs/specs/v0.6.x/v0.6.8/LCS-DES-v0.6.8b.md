# LCS-DES-068b: Integration Tests

## 1. Metadata & Categorization

| Field              | Value                     |
| :----------------- | :------------------------ |
| **Document ID**    | LCS-DES-068b              |
| **Feature ID**     | AGT-068b                  |
| **Feature Name**   | Integration Tests         |
| **Target Version** | v0.6.8b                   |
| **Module Scope**   | Lexichord.Tests.Agents    |
| **Swimlane**       | Agents                    |
| **License Tier**   | N/A (Test Infrastructure) |
| **Status**         | Draft                     |
| **Last Updated**   | 2026-01-28                |

---

## 2. Executive Summary

### 2.1 The Requirement

Unit tests verify individual components in isolation, but cannot catch integration issues: mismatched data contracts, timing issues, or end-to-end workflow failures.

### 2.2 The Proposed Solution

Create integration tests that verify complete agent workflows using a mock LLM server. Tests cover message send/receive, streaming assembly, context injection, and error scenarios.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**

- `Lexichord.Modules.Agents` (system under test)
- `Lexichord.Tests.Agents.Fixtures` (v0.6.8a)

**NuGet Packages:**

- `xUnit` 2.8.x - Test framework
- `Microsoft.AspNetCore.TestHost` 9.0.x - In-memory test server
- `WireMock.Net` 1.5.x - HTTP mock server

### 3.2 Mock LLM Server

```csharp
namespace Lexichord.Tests.Agents.Fixtures;

/// <summary>
/// In-memory mock LLM server for integration testing.
/// Simulates OpenAI and Anthropic API endpoints.
/// </summary>
public class MockLLMServer : IAsyncDisposable
{
    private readonly WireMockServer _server;
    private readonly List<ChatRequest> _receivedRequests = new();

    public MockLLMServer()
    {
        _server = WireMockServer.Start();
        BaseAddress = new Uri(_server.Url!);
    }

    public Uri BaseAddress { get; }
    public int RequestCount => _receivedRequests.Count;
    public IReadOnlyList<ChatRequest> ReceivedRequests => _receivedRequests;

    public void ConfigureOpenAIResponse(ChatResponse response)
    {
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SerializeOpenAIResponse(response)));
    }

    public void ConfigureStreamingResponse(IEnumerable<StreamingChatToken> tokens)
    {
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(SerializeSSEStream(tokens)));
    }

    public void ConfigureRateLimit(TimeSpan retryAfter)
    {
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", ((int)retryAfter.TotalSeconds).ToString())
                .WithBody("""{"error": {"message": "Rate limit exceeded"}}"""));
    }

    public void ConfigureTimeout(TimeSpan delay)
    {
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(delay)
                .WithBody("""{"error": {"message": "Timeout"}}"""));
    }

    public void ConfigureAuthenticationError()
    {
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("""{"error": {"message": "Invalid API key"}}"""));
    }

    public ValueTask DisposeAsync()
    {
        _server.Stop();
        return ValueTask.CompletedTask;
    }

    private static string SerializeOpenAIResponse(ChatResponse response) => $$"""
        {
            "id": "chatcmpl-{{Guid.NewGuid():N}}",
            "choices": [{"message": {"content": "{{response.Content}}"}, "finish_reason": "stop"}],
            "usage": {"prompt_tokens": {{response.PromptTokens}}, "completion_tokens": {{response.CompletionTokens}}}
        }
        """;

    private static string SerializeSSEStream(IEnumerable<StreamingChatToken> tokens)
    {
        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            sb.AppendLine($$"""data: {"choices":[{"delta":{"content":"{{token.Text}}"}}]}""");
            sb.AppendLine();
        }
        sb.AppendLine("data: [DONE]");
        return sb.ToString();
    }
}
```

---

## 4. Implementation Logic

### 4.1 End-to-End Workflow Tests

```csharp
[Trait("Category", "Integration")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8b")]
public class AgentWorkflowTests : IAsyncLifetime
{
    private MockLLMServer _llmServer = null!;
    private ServiceProvider _serviceProvider = null!;
    private IAgent _agent = null!;

    public async Task InitializeAsync()
    {
        _llmServer = new MockLLMServer();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());

        // Configure services to use mock server
        services.Configure<OpenAIOptions>(o => o.BaseAddress = _llmServer.BaseAddress);
        services.AddHttpClient("OpenAI", c => c.BaseAddress = _llmServer.BaseAddress);

        // Register all Agents module services
        services.AddAgentsModule();

        // Override vault with test key
        var vault = Substitute.For<ISecureVault>();
        vault.GetSecretAsync("openai:api-key").Returns("test-api-key");
        services.AddSingleton(vault);

        _serviceProvider = services.BuildServiceProvider();
        _agent = _serviceProvider.GetRequiredService<IAgentRegistry>().GetAgent("copilot");
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _llmServer.DisposeAsync();
    }

    [Fact]
    public async Task SendMessage_ReceiveResponse_VerifyTokenCounts()
    {
        // Arrange
        var expectedResponse = new ChatResponse("The answer is 42.", 75, 15, TimeSpan.FromMilliseconds(200), "stop");
        _llmServer.ConfigureOpenAIResponse(expectedResponse);

        var request = new AgentRequest(UserMessage: "What is the meaning of life?");

        // Act
        var response = await _agent.InvokeAsync(request, CancellationToken.None);

        // Assert
        response.Content.Should().Be("The answer is 42.");
        response.Metrics.PromptTokens.Should().Be(75);
        response.Metrics.CompletionTokens.Should().Be(15);
        response.Metrics.TotalTokens.Should().Be(90);
        _llmServer.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ConversationFlow_MultipleMessages_MaintainsHistory()
    {
        // Arrange
        _llmServer.ConfigureOpenAIResponse(new ChatResponse("Hello!", 10, 5, TimeSpan.FromMilliseconds(100), "stop"));
        await _agent.InvokeAsync(new AgentRequest("Hi"), CancellationToken.None);

        _llmServer.ConfigureOpenAIResponse(new ChatResponse("Nice!", 25, 8, TimeSpan.FromMilliseconds(100), "stop"));
        await _agent.InvokeAsync(new AgentRequest("How are you?"), CancellationToken.None);

        _llmServer.ConfigureOpenAIResponse(new ChatResponse("Goodbye!", 40, 7, TimeSpan.FromMilliseconds(100), "stop"));

        // Act
        var response = await _agent.InvokeAsync(new AgentRequest("Bye!"), CancellationToken.None);

        // Assert
        _llmServer.RequestCount.Should().Be(3);
        var lastRequest = _llmServer.ReceivedRequests.Last();
        lastRequest.Messages.Should().HaveCount(5); // System + 2 turns + current
    }
}
```

### 4.2 Streaming Integration Tests

```csharp
[Trait("Category", "Integration")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8b")]
public class StreamingIntegrationTests : IAsyncLifetime
{
    private MockLLMServer _llmServer = null!;
    private ServiceProvider _serviceProvider = null!;
    private IStreamingChatHandler _handler = null!;

    public async Task InitializeAsync()
    {
        _llmServer = new MockLLMServer();
        var services = ConfigureTestServices(_llmServer);
        _serviceProvider = services.BuildServiceProvider();
        _handler = _serviceProvider.GetRequiredService<IStreamingChatHandler>();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _llmServer.DisposeAsync();
    }

    [Fact]
    public async Task StreamingResponse_AllTokensReceived_AssembledCorrectly()
    {
        // Arrange
        var tokens = new[]
        {
            new StreamingChatToken("Hello", 0),
            new StreamingChatToken(" there", 1),
            new StreamingChatToken("!", 2)
        };
        _llmServer.ConfigureStreamingResponse(tokens);

        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Greet me")],
            new ChatOptions { Streaming = true });

        // Act
        var receivedTokens = new List<StreamingChatToken>();
        await foreach (var token in _handler.StreamAsync(request, CancellationToken.None))
        {
            receivedTokens.Add(token);
        }

        // Assert
        receivedTokens.Should().HaveCount(3);
        string.Concat(receivedTokens.Select(t => t.Text)).Should().Be("Hello there!");
    }

    [Fact]
    public async Task StreamingResponse_Cancellation_StopsGracefully()
    {
        // Arrange
        var tokens = Enumerable.Range(0, 100)
            .Select(i => new StreamingChatToken($"Token{i} ", i))
            .ToArray();
        _llmServer.ConfigureStreamingResponse(tokens);

        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Long response")],
            new ChatOptions { Streaming = true });
        using var cts = new CancellationTokenSource();

        // Act
        var receivedTokens = new List<StreamingChatToken>();
        await foreach (var token in _handler.StreamAsync(request, cts.Token))
        {
            receivedTokens.Add(token);
            if (receivedTokens.Count >= 10) cts.Cancel();
        }

        // Assert
        receivedTokens.Count.Should().BeLessThan(20, "Should stop shortly after cancellation");
    }

    [Fact]
    public async Task StreamingResponse_FirstTokenLatency_UnderThreshold()
    {
        // Arrange
        var tokens = new[] { new StreamingChatToken("Quick", 0) };
        _llmServer.ConfigureStreamingResponse(tokens);

        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Test")],
            new ChatOptions { Streaming = true });

        // Act
        var stopwatch = Stopwatch.StartNew();
        await foreach (var _ in _handler.StreamAsync(request, CancellationToken.None))
        {
            stopwatch.Stop();
            break; // Only measure first token
        }

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "First token should arrive quickly");
    }
}
```

### 4.3 Context Injection Tests

```csharp
[Trait("Category", "Integration")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8b")]
public class ContextInjectionIntegrationTests : IAsyncLifetime
{
    private MockLLMServer _llmServer = null!;
    private ServiceProvider _serviceProvider = null!;
    private IAgent _agent = null!;
    private InMemoryStyleRuleRepository _styleRules = null!;
    private InMemorySemanticSearchService _ragService = null!;

    public async Task InitializeAsync()
    {
        _llmServer = new MockLLMServer();
        _styleRules = new InMemoryStyleRuleRepository();
        _ragService = new InMemorySemanticSearchService();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<OpenAIOptions>(o => o.BaseAddress = _llmServer.BaseAddress);
        services.AddAgentsModule();
        services.AddSingleton<IStyleRuleRepository>(_styleRules);
        services.AddSingleton<ISemanticSearchService>(_ragService);

        _serviceProvider = services.BuildServiceProvider();
        _agent = _serviceProvider.GetRequiredService<IAgentRegistry>().GetAgent("copilot");
    }

    [Fact]
    public async Task ContextInjection_StyleRulesIncluded_InSystemPrompt()
    {
        // Arrange
        _styleRules.Add(new StyleRule("Use active voice", Severity.Required));
        _styleRules.Add(new StyleRule("Avoid jargon", Severity.Preferred));

        _llmServer.ConfigureOpenAIResponse(new ChatResponse("Improved text", 50, 10, TimeSpan.FromMilliseconds(100), "stop"));

        var request = new AgentRequest(UserMessage: "Improve this text", IncludeStyleRules: true);

        // Act
        await _agent.InvokeAsync(request, CancellationToken.None);

        // Assert
        var sentRequest = _llmServer.ReceivedRequests.Single();
        var systemMessage = sentRequest.Messages.First(m => m.Role == ChatRole.System);
        systemMessage.Content.Should().Contain("Use active voice");
        systemMessage.Content.Should().Contain("Avoid jargon");
    }

    [Fact]
    public async Task ContextInjection_RAGChunksIncluded_InContext()
    {
        // Arrange
        _ragService.Add(new SearchResult("First relevant chunk", 0.95));
        _ragService.Add(new SearchResult("Second relevant chunk", 0.85));
        _ragService.Add(new SearchResult("Third relevant chunk", 0.75));

        _llmServer.ConfigureOpenAIResponse(new ChatResponse("Response with context", 80, 20, TimeSpan.FromMilliseconds(100), "stop"));

        var request = new AgentRequest(UserMessage: "Answer based on context", IncludeRAGContext: true, MaxRAGChunks: 3);

        // Act
        await _agent.InvokeAsync(request, CancellationToken.None);

        // Assert
        var sentRequest = _llmServer.ReceivedRequests.Single();
        var systemMessage = sentRequest.Messages.First(m => m.Role == ChatRole.System);
        systemMessage.Content.Should().Contain("First relevant chunk");
        systemMessage.Content.Should().Contain("Second relevant chunk");
        systemMessage.Content.Should().Contain("Third relevant chunk");
    }
}
```

### 4.4 Error Scenario Tests

```csharp
[Trait("Category", "Integration")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8b")]
public class ErrorScenarioTests : IAsyncLifetime
{
    private MockLLMServer _llmServer = null!;
    private ServiceProvider _serviceProvider = null!;
    private IAgent _agent = null!;

    [Fact]
    public async Task APITimeout_RetriesAndFails_ThrowsAgentException()
    {
        // Arrange
        _llmServer.ConfigureTimeout(TimeSpan.FromSeconds(35)); // Longer than timeout

        var request = new AgentRequest("Test message");

        // Act & Assert
        var exception = await _agent.Invoking(a => a.InvokeAsync(request, CancellationToken.None))
            .Should().ThrowAsync<AgentException>();

        exception.Which.UserMessage.Should().Contain("request timed out");
        exception.Which.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimit_QueuesAndRetries_EventuallySucceeds()
    {
        // Arrange - First request gets rate limited, retry succeeds
        var callCount = 0;
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create().WithCallback(req =>
            {
                callCount++;
                if (callCount == 1)
                    return new ResponseMessage
                    {
                        StatusCode = 429,
                        Headers = new Dictionary<string, WireMockList<string>> { ["Retry-After"] = new(["1"]) }
                    };
                return new ResponseMessage { StatusCode = 200, Body = TestChatResponses.OpenAIJsonResponse };
            }));

        var request = new AgentRequest("Test message");

        // Act
        var response = await _agent.InvokeAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task InvalidAPIKey_ThrowsAuthenticationException()
    {
        // Arrange
        _llmServer.ConfigureAuthenticationError();
        var request = new AgentRequest("Test message");

        // Act & Assert
        var exception = await _agent.Invoking(a => a.InvokeAsync(request, CancellationToken.None))
            .Should().ThrowAsync<AuthenticationException>();

        exception.Which.UserMessage.Should().Contain("API key");
        exception.Which.IsRecoverable.Should().BeFalse();
    }
}
```

---

## 5. Observability & Logging

| Level   | Source          | Message Template                          |
| :------ | :-------------- | :---------------------------------------- |
| Debug   | MockLLMServer   | `Received request: {Method} {Path}`       |
| Info    | TestFixture     | `Test scenario: {Scenario} configured`    |
| Warning | IntegrationTest | `Retry detected after {DelayMs}ms`        |
| Error   | IntegrationTest | `Unexpected error in {TestName}: {Error}` |

---

## 6. Acceptance Criteria (QA)

| #   | Category        | Criterion                                                   |
| :-- | :-------------- | :---------------------------------------------------------- |
| 1   | **Workflows**   | All critical agent workflows verified end-to-end            |
| 2   | **Streaming**   | Streaming tests verify complete token assembly              |
| 3   | **Context**     | Context injection tests verify RAG and style rule inclusion |
| 4   | **Errors**      | All error scenarios (timeout, rate limit, auth) are tested  |
| 5   | **Isolation**   | Tests use mock server, no real API calls                    |
| 6   | **Performance** | Integration test suite runs in < 60 seconds                 |

---

## 7. Deliverable Checklist

| #   | Deliverable                                   | Status |
| :-- | :-------------------------------------------- | :----- |
| 1   | `MockLLMServer` with configurable responses   | [ ]    |
| 2   | `AgentWorkflowTests.cs` with 8+ tests         | [ ]    |
| 3   | `StreamingIntegrationTests.cs` with 6+ tests  | [ ]    |
| 4   | `ContextInjectionIntegrationTests.cs` with 5+ | [ ]    |
| 5   | `ErrorScenarioTests.cs` with 6+ tests         | [ ]    |
| 6   | In-memory test data providers                 | [ ]    |
| 7   | Integration test documentation                | [ ]    |

---
