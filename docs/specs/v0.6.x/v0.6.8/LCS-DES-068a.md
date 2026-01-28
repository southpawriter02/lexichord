# LCS-DES-068a: Unit Test Suite

## 1. Metadata & Categorization

| Field              | Value                     |
| :----------------- | :------------------------ |
| **Document ID**    | LCS-DES-068a              |
| **Feature ID**     | AGT-068a                  |
| **Feature Name**   | Unit Test Suite           |
| **Target Version** | v0.6.8a                   |
| **Module Scope**   | Lexichord.Tests.Agents    |
| **Swimlane**       | Agents                    |
| **License Tier**   | N/A (Test Infrastructure) |
| **Status**         | Draft                     |
| **Last Updated**   | 2026-01-28                |

---

## 2. Executive Summary

### 2.1 The Requirement

The Agents module (v0.6.1–v0.6.7) lacks comprehensive unit test coverage. Without tests, refactoring carries high regression risk, and production bugs are discovered too late.

### 2.2 The Proposed Solution

Create a comprehensive unit test suite covering all Agents module components: chat completion services, prompt rendering, agents, and streaming. Tests use mocked dependencies to ensure fast, deterministic execution.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**

- `Lexichord.Modules.Agents` (system under test)
- `Lexichord.Abstractions` (interfaces)

**NuGet Packages:**

- `xUnit` 2.8.x - Test framework
- `NSubstitute` 4.4.x - Mocking
- `FluentAssertions` 6.12.x - Assertion library
- `Moq` 4.20.x - HTTP mock support

### 3.2 Test Project Structure

```text
Lexichord.Tests.Agents/
├── Unit/
│   ├── ChatCompletion/
│   │   ├── OpenAIConnectorTests.cs
│   │   ├── AnthropicConnectorTests.cs
│   │   ├── TokenCounterTests.cs
│   │   └── ChatOptionsValidationTests.cs
│   ├── Templates/
│   │   ├── MustacheRendererTests.cs
│   │   ├── TemplateRepositoryTests.cs
│   │   ├── ContextInjectorTests.cs
│   │   └── TemplateValidationTests.cs
│   ├── Agents/
│   │   ├── CoPilotAgentTests.cs
│   │   ├── AgentRegistryTests.cs
│   │   ├── CitationExtractionTests.cs
│   │   └── UsageTrackingTests.cs
│   └── Streaming/
│       ├── OpenAISSEParserTests.cs
│       ├── AnthropicSSEParserTests.cs
│       ├── StreamingHandlerTests.cs
│       └── TokenAssemblyTests.cs
└── Fixtures/
    ├── MockHttpMessageHandler.cs
    ├── TestChatResponses.cs
    ├── TestPromptTemplates.cs
    └── TestStreamingTokens.cs
```

---

## 4. Data Contract (The API)

### 4.1 Test Fixtures

```csharp
namespace Lexichord.Tests.Agents.Fixtures;

/// <summary>
/// Provides pre-configured mock HTTP responses for LLM provider tests.
/// </summary>
public static class TestChatResponses
{
    public static ChatResponse SuccessfulCompletion => new(
        Content: "Test response content",
        PromptTokens: 50,
        CompletionTokens: 25,
        Duration: TimeSpan.FromMilliseconds(150),
        FinishReason: "stop"
    );

    public static string OpenAIJsonResponse => """
        {
            "id": "chatcmpl-test123",
            "object": "chat.completion",
            "choices": [{
                "index": 0,
                "message": { "role": "assistant", "content": "Test response content" },
                "finish_reason": "stop"
            }],
            "usage": { "prompt_tokens": 50, "completion_tokens": 25 }
        }
        """;

    public static string AnthropicJsonResponse => """
        {
            "id": "msg_test123",
            "type": "message",
            "role": "assistant",
            "content": [{ "type": "text", "text": "Test response content" }],
            "stop_reason": "end_turn",
            "usage": { "input_tokens": 50, "output_tokens": 25 }
        }
        """;
}

/// <summary>
/// Configurable HTTP handler for testing HTTP-based services.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public void EnqueueResponse(HttpStatusCode status, string content)
    {
        _responses.Enqueue(new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        });
    }

    public void EnqueueStreamingResponse(IEnumerable<string> sseLines)
    {
        var content = string.Join("\n", sseLines.Select(l => $"data: {l}")) + "\ndata: [DONE]\n";
        _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/event-stream")
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        return Task.FromResult(_responses.Dequeue());
    }

    public List<HttpRequestMessage> ReceivedRequests { get; } = new();
}
```

---

## 5. Implementation Logic

### 5.1 ChatCompletionService Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8a")]
public class OpenAIConnectorTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly ISecureVault _vault = Substitute.For<ISecureVault>();
    private readonly ILogger<OpenAIChatCompletionService> _logger = Substitute.For<ILogger<OpenAIChatCompletionService>>();

    [Fact]
    public async Task CompleteAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK, TestChatResponses.OpenAIJsonResponse);
        _vault.GetSecretAsync("openai:api-key").Returns("test-api-key");

        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.openai.com/") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("OpenAI").Returns(httpClient);

        var service = new OpenAIChatCompletionService(factory, _vault, Options.Create(new OpenAIOptions()), _logger);
        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Hello")], new ChatOptions());

        // Act
        var response = await service.CompleteAsync(request, CancellationToken.None);

        // Assert
        response.Content.Should().Be("Test response content");
        response.PromptTokens.Should().Be(50);
        response.CompletionTokens.Should().Be(25);
    }

    [Fact]
    public async Task CompleteAsync_MissingApiKey_ThrowsInvalidOperation()
    {
        // Arrange
        _vault.GetSecretAsync("openai:api-key").Returns((string?)null);
        var factory = Substitute.For<IHttpClientFactory>();
        var service = new OpenAIChatCompletionService(factory, _vault, Options.Create(new OpenAIOptions()), _logger);
        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Hello")], new ChatOptions());

        // Act & Assert
        await service.Invoking(s => s.CompleteAsync(request, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*not configured*");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CompleteAsync_RetryableError_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        // Arrange
        _handler.EnqueueResponse(statusCode, """{"error": {"message": "Rate limited"}}""");
        _vault.GetSecretAsync("openai:api-key").Returns("test-api-key");

        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.openai.com/") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("OpenAI").Returns(httpClient);

        var service = new OpenAIChatCompletionService(factory, _vault, Options.Create(new OpenAIOptions()), _logger);
        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Hello")], new ChatOptions());

        // Act & Assert
        await service.Invoking(s => s.CompleteAsync(request, CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CompleteAsync_SetsAuthorizationHeader()
    {
        // Arrange
        _handler.EnqueueResponse(HttpStatusCode.OK, TestChatResponses.OpenAIJsonResponse);
        _vault.GetSecretAsync("openai:api-key").Returns("sk-test-key-123");

        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.openai.com/") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("OpenAI").Returns(httpClient);

        var service = new OpenAIChatCompletionService(factory, _vault, Options.Create(new OpenAIOptions()), _logger);
        var request = new ChatRequest([new ChatMessage(ChatRole.User, "Hello")], new ChatOptions());

        // Act
        await service.CompleteAsync(request, CancellationToken.None);

        // Assert
        _handler.ReceivedRequests.Should().ContainSingle();
        _handler.ReceivedRequests[0].Headers.Authorization.Should().NotBeNull();
        _handler.ReceivedRequests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
        _handler.ReceivedRequests[0].Headers.Authorization!.Parameter.Should().Be("sk-test-key-123");
    }
}
```

### 5.2 PromptRenderer Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8a")]
public class MustacheRendererTests
{
    private readonly MustachePromptRenderer _renderer = new();

    [Fact]
    public void Render_SimpleVariable_ReplacesCorrectly()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, object> { ["name"] = "World" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void Render_MissingVariable_LeavesEmpty()
    {
        // Arrange
        var template = "Hello, {{name}}! Your score is {{score}}.";
        var variables = new Dictionary<string, object> { ["name"] = "Alice" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, Alice! Your score is .");
    }

    [Fact]
    public void Render_ConditionalSection_RendersWhenTrue()
    {
        // Arrange
        var template = "{{#showDetails}}Details: {{content}}{{/showDetails}}";
        var variables = new Dictionary<string, object>
        {
            ["showDetails"] = true,
            ["content"] = "Secret info"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Details: Secret info");
    }

    [Fact]
    public void Render_InvertedSection_RendersWhenFalse()
    {
        // Arrange
        var template = "{{^hasItems}}No items found{{/hasItems}}";
        var variables = new Dictionary<string, object> { ["hasItems"] = false };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("No items found");
    }

    [Fact]
    public void RenderMessages_ValidTemplate_ReturnsSystemAndUserMessages()
    {
        // Arrange
        var template = Substitute.For<IPromptTemplate>();
        template.SystemPromptTemplate.Returns("You are a helpful assistant. {{#context}}Context: {{context}}{{/context}}");
        template.UserPromptTemplate.Returns("{{user_input}}");

        var variables = new Dictionary<string, object>
        {
            ["context"] = "Relevant info",
            ["user_input"] = "Help me"
        };

        // Act
        var messages = _renderer.RenderMessages(template, variables);

        // Assert
        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[0].Content.Should().Contain("Context: Relevant info");
        messages[1].Role.Should().Be(ChatRole.User);
        messages[1].Content.Should().Be("Help me");
    }
}
```

### 5.3 Streaming Parser Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Module", "Agents")]
[Trait("SubPart", "v0.6.8a")]
public class OpenAISSEParserTests
{
    private readonly SSEStreamParser _parser = new();

    [Fact]
    public async Task ParseSSEStreamAsync_ValidStream_YieldsTokens()
    {
        // Arrange
        var sseContent = """
            data: {"choices":[{"delta":{"content":"Hello"}}]}

            data: {"choices":[{"delta":{"content":" World"}}]}

            data: [DONE]

            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseContent));

        // Act
        var tokens = new List<StreamingChatToken>();
        await foreach (var token in _parser.ParseSSEStreamAsync(stream, "OpenAI", CancellationToken.None))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().HaveCount(2);
        tokens[0].Text.Should().Be("Hello");
        tokens[1].Text.Should().Be(" World");
    }

    [Fact]
    public async Task ParseSSEStreamAsync_EmptyDelta_SkipsToken()
    {
        // Arrange
        var sseContent = """
            data: {"choices":[{"delta":{}}]}

            data: {"choices":[{"delta":{"content":"Content"}}]}

            data: [DONE]

            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseContent));

        // Act
        var tokens = new List<StreamingChatToken>();
        await foreach (var token in _parser.ParseSSEStreamAsync(stream, "OpenAI", CancellationToken.None))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().ContainSingle();
        tokens[0].Text.Should().Be("Content");
    }

    [Fact]
    public async Task ParseSSEStreamAsync_Cancelled_StopsIterating()
    {
        // Arrange
        var sseContent = """
            data: {"choices":[{"delta":{"content":"First"}}]}

            data: {"choices":[{"delta":{"content":"Second"}}]}

            data: {"choices":[{"delta":{"content":"Third"}}]}

            data: [DONE]

            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseContent));
        using var cts = new CancellationTokenSource();

        // Act
        var tokens = new List<StreamingChatToken>();
        await foreach (var token in _parser.ParseSSEStreamAsync(stream, "OpenAI", cts.Token))
        {
            tokens.Add(token);
            if (tokens.Count == 1) cts.Cancel();
        }

        // Assert
        tokens.Should().HaveCount(1);
    }
}
```

---

## 6. Observability & Logging

| Level   | Source          | Message Template                              |
| :------ | :-------------- | :-------------------------------------------- |
| Debug   | TestRunner      | `Starting test: {TestName}`                   |
| Info    | TestRunner      | `Test completed: {TestName} in {ElapsedMs}ms` |
| Warning | MockHttpHandler | `Unexpected request to {Url}`                 |
| Error   | TestRunner      | `Test failed: {TestName} - {Error}`           |

---

## 7. Acceptance Criteria (QA)

| #   | Category          | Criterion                                                   |
| :-- | :---------------- | :---------------------------------------------------------- |
| 1   | **Coverage**      | Unit test coverage ≥ 90% for Agents module                  |
| 2   | **Isolation**     | All tests run without network or external dependencies      |
| 3   | **Speed**         | Complete unit test suite runs in < 30 seconds               |
| 4   | **Determinism**   | Tests produce consistent results across 10 consecutive runs |
| 5   | **Documentation** | Each test class has XML doc explaining test scope           |
| 6   | **Traits**        | All tests tagged with Category, Module, SubPart traits      |

---

## 8. Deliverable Checklist

| #   | Deliverable                                       | Status |
| :-- | :------------------------------------------------ | :----- |
| 1   | `OpenAIConnectorTests.cs` with 10+ tests          | [ ]    |
| 2   | `AnthropicConnectorTests.cs` with 10+ tests       | [ ]    |
| 3   | `MustacheRendererTests.cs` with 8+ tests          | [ ]    |
| 4   | `CoPilotAgentTests.cs` with 12+ tests             | [ ]    |
| 5   | `SSEParserTests.cs` (OpenAI + Anthropic) with 10+ | [ ]    |
| 6   | `MockHttpMessageHandler` fixture                  | [ ]    |
| 7   | Test data fixtures                                | [ ]    |
| 8   | 90%+ coverage report                              | [ ]    |

---
