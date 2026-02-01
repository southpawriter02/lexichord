// =============================================================================
// File: OpenAIEmbeddingServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Comprehensive unit tests for v0.4.4b OpenAI embedding service.
//              Tests embedding generation, batch processing, error handling, and retry logic.
// =============================================================================

using System.Net;
using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.RAG.Embedding;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace Lexichord.Tests.Unit.Modules;

/// <summary>
/// Unit tests for <see cref="OpenAIEmbeddingService"/>.
/// </summary>
/// <remarks>
/// Introduced in v0.4.4b as part of the Embedding Service implementation.
/// Tests the production-ready OpenAI Embeddings API client with retry logic.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4b")]
public class OpenAIEmbeddingServiceTests
{
    private const string TestApiKey = "sk-test-key-12345";
    private const string TestModel = "text-embedding-3-small";
    private const int TestDimensions = 1536;

    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly Mock<ISecureVault> _mockVault = new();
    private readonly Mock<ILogger<OpenAIEmbeddingService>> _mockLogger = new();
    private readonly EmbeddingOptions _defaultOptions = EmbeddingOptions.Default;

    #region Constructor Tests

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);

        // Act
        var act = () => new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Constructor_WithNullHttpFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);

        // Act & Assert
        var act = () => new OpenAIEmbeddingService(
            null!,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClientFactory");
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Constructor_WithNullVault_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);

        // Act & Assert
        var act = () => new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            null!,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("vault");
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Constructor_WithNullOptionsAccessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("optionsAccessor");
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);

        // Act & Assert
        var act = () => new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Constructor_WithInvalidOptions_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = EmbeddingOptions.Default with { Model = "" };
        var optionsAccessor = Options.Create(invalidOptions);

        // Act & Assert
        var act = () => new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void ModelName_Property_ReturnsConfiguredValue()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { Model = "text-embedding-3-large" };
        var optionsAccessor = Options.Create(options);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act
        var modelName = service.ModelName;

        // Assert
        modelName.Should().Be("text-embedding-3-large");
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Dimensions_Property_ReturnsConfiguredValue()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { Dimensions = 3072 };
        var optionsAccessor = Options.Create(options);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act
        var dimensions = service.Dimensions;

        // Assert
        dimensions.Should().Be(3072);
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void MaxTokens_Property_ReturnsConfiguredValue()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxTokens = 4000 };
        var optionsAccessor = Options.Create(options);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act
        var maxTokens = service.MaxTokens;

        // Assert
        maxTokens.Should().Be(4000);
    }

    #endregion

    #region EmbedAsync Tests

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedAsync_EmptyText_ThrowsArgumentException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act & Assert
        var act = () => service.EmbedAsync("", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedAsync_NullText_ThrowsArgumentException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act & Assert
        var act = () => service.EmbedAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedAsync_ValidText_ReturnsEmbedding()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Mock vault to return API key
        _mockVault.Setup(v => v.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestApiKey);

        // Mock successful API response
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CreateSuccessfulEmbeddingResponse())
        };

        var mockHttpClient = new Mock<HttpClient>();
        mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        _mockHttpClientFactory.Setup(f => f.CreateClient())
            .Returns(mockHttpClient.Object);

        // Act
        var embedding = await service.EmbedAsync("test text", CancellationToken.None);

        // Assert
        embedding.Should().NotBeNull();
        embedding.Should().HaveCount(TestDimensions);
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedAsync_NoApiKey_ThrowsEmbeddingException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        _mockVault.Setup(v => v.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var act = () => service.EmbedAsync("test", CancellationToken.None);
        await act.Should().ThrowAsync<EmbeddingException>();
    }

    #endregion

    #region EmbedBatchAsync Tests

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedBatchAsync_MultipleTexts_ReturnsCorrectCount()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        _mockVault.Setup(v => v.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestApiKey);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CreateSuccessfulEmbeddingResponse(3))
        };

        var mockHttpClient = new Mock<HttpClient>();
        mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        _mockHttpClientFactory.Setup(f => f.CreateClient())
            .Returns(mockHttpClient.Object);

        var texts = new[] { "text1", "text2", "text3" };

        // Act
        var embeddings = await service.EmbedBatchAsync(texts, CancellationToken.None);

        // Assert
        embeddings.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedBatchAsync_ExceedsMaxBatch_ThrowsArgumentException()
    {
        // Arrange
        var options = EmbeddingOptions.Default with { MaxBatchSize = 10 };
        var optionsAccessor = Options.Create(options);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        var texts = Enumerable.Range(0, 11).Select(i => $"text{i}").ToList();

        // Act & Assert
        var act = () => service.EmbedBatchAsync(texts, CancellationToken.None);
        await act.Should().ThrowAsync<EmbeddingException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedBatchAsync_NullTexts_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act & Assert
        var act = () => service.EmbedBatchAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedBatchAsync_EmptyTexts_ThrowsArgumentException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act & Assert
        var act = () => service.EmbedBatchAsync(Array.Empty<string>(), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedBatchAsync_ContainsNullText_ThrowsArgumentException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        var texts = new string[] { "text1", null!, "text3" };

        // Act & Assert
        var act = () => service.EmbedBatchAsync(texts, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedAsync_Unauthorized_FailsImmediately()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        _mockVault.Setup(v => v.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestApiKey);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\": {\"message\": \"Invalid API key\"}}")
        };

        var mockHttpClient = new Mock<HttpClient>();
        mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        _mockHttpClientFactory.Setup(f => f.CreateClient())
            .Returns(mockHttpClient.Object);

        // Act & Assert
        var act = () => service.EmbedAsync("test", CancellationToken.None);
        await act.Should().ThrowAsync<EmbeddingException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public async Task EmbedAsync_RateLimited_RetriesWithBackoff()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        _mockVault.Setup(v => v.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestApiKey);

        // First call returns rate limit, second returns success
        var mockResponses = new Queue<HttpResponseMessage>();
        mockResponses.Enqueue(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\": {\"message\": \"Rate limit exceeded\"}}")
        });
        mockResponses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CreateSuccessfulEmbeddingResponse())
        });

        var mockHttpClient = new Mock<HttpClient>();
        mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(mockResponses.Dequeue()));

        _mockHttpClientFactory.Setup(f => f.CreateClient())
            .Returns(mockHttpClient.Object);

        // Act
        var embedding = await service.EmbedAsync("test", CancellationToken.None);

        // Assert
        embedding.Should().NotBeNull();
        embedding.Should().HaveCount(TestDimensions);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Service_CanBeDisposed()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act & Assert
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.4b")]
    public void Service_CanBeDisposedMultipleTimes()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultOptions);
        var service = new OpenAIEmbeddingService(
            _mockHttpClientFactory.Object,
            _mockVault.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Act & Assert
        service.Dispose();
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private string CreateSuccessfulEmbeddingResponse(int count = 1)
    {
        var embeddings = Enumerable.Range(0, count).Select(i =>
            new
            {
                index = i,
                embedding = Enumerable.Range(0, TestDimensions)
                    .Select(j => (float)(i + j) * 0.001f)
                    .ToArray(),
                object = "embedding"
            }).ToList();

        var response = new
        {
            @object = "list",
            data = embeddings,
            model = TestModel,
            usage = new
            {
                prompt_tokens = 8,
                total_tokens = 8
            }
        };

        return JsonSerializer.Serialize(response);
    }

    #endregion
}
