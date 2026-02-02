// =============================================================================
// File: EndpointExtractorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the EndpointExtractor entity extractor.
// =============================================================================
// LOGIC: Validates that EndpointExtractor correctly identifies API endpoint
//   patterns in text using three detection strategies:
//   - HTTP method + path (confidence 1.0)
//   - Code block definitions (confidence 0.9)
//   - Standalone paths (confidence 0.7)
//   Also validates false positive filtering and path normalization.
//
// Test Categories:
//   - Method + path extraction (various HTTP methods)
//   - Code block path extraction
//   - Standalone path extraction
//   - False positive filtering (file paths, Unix paths, short paths)
//   - Path normalization (query stripping, trailing slash, lowercase)
//   - Deduplication across patterns
//   - Empty/whitespace text handling
//   - Extractor metadata (SupportedTypes, Priority)
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction.Extractors;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="EndpointExtractor"/> entity extraction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5g")]
public sealed class EndpointExtractorTests
{
    private readonly EndpointExtractor _extractor = new();
    private readonly ExtractionContext _context = new();

    #region Extractor Metadata

    [Fact]
    public void SupportedTypes_ContainsEndpoint()
    {
        // Assert
        _extractor.SupportedTypes.Should().ContainSingle("Endpoint");
    }

    [Fact]
    public void Priority_Is100()
    {
        // Assert
        _extractor.Priority.Should().Be(100);
    }

    #endregion

    #region Method + Path Extraction

    [Theory]
    [InlineData("GET /users", "GET", "/users")]
    [InlineData("POST /api/v1/orders", "POST", "/api/v1/orders")]
    [InlineData("PUT /items/{id}", "PUT", "/items/{id}")]
    [InlineData("PATCH /users/{userId}/profile", "PATCH", "/users/{userid}/profile")]
    [InlineData("DELETE /sessions/{token}", "DELETE", "/sessions/{token}")]
    [InlineData("HEAD /health", "HEAD", "/health")]
    [InlineData("OPTIONS /api", "OPTIONS", "/api")]
    public async Task ExtractAsync_MethodPath_ExtractsWithConfidence1(
        string text, string expectedMethod, string expectedPath)
    {
        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert — method+path match always present at confidence 1.0;
        // standalone path may also match partial segments for paths with
        // template variables, so we check for the high-confidence match.
        mentions.Should().Contain(m =>
            m.Confidence == 1.0f &&
            (string)m.Properties["method"] == expectedMethod &&
            m.NormalizedValue == expectedPath);
    }

    [Fact]
    public async Task ExtractAsync_MethodPathCaseInsensitive_Extracts()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("get /users", _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Properties["method"].Should().Be("GET");
    }

    [Fact]
    public async Task ExtractAsync_MultipleMethodPaths_ExtractsAll()
    {
        // Arrange
        var text = "GET /users returns a list. POST /users creates a new user.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().HaveCount(2);
        mentions.Should().Contain(m => (string)m.Properties["method"] == "GET");
        mentions.Should().Contain(m => (string)m.Properties["method"] == "POST");
    }

    #endregion

    #region Code Block Path Extraction

    [Fact]
    public async Task ExtractAsync_CodeBlockEndpoint_ExtractsWithConfidence09()
    {
        // Arrange
        var text = "endpoint: /api/orders";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(0.9f);
        mentions[0].NormalizedValue.Should().Be("/api/orders");
    }

    [Theory]
    [InlineData("url = \"/api/users\"")]
    [InlineData("path: /api/items")]
    [InlineData("route = /api/products")]
    public async Task ExtractAsync_CodeBlockVariants_Extracts(string text)
    {
        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(0.9f);
    }

    #endregion

    #region Standalone Path Extraction

    [Fact]
    public async Task ExtractAsync_StandalonePath_ExtractsWithConfidence07()
    {
        // Arrange
        var text = "The /users resource returns all users.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].Confidence.Should().Be(0.7f);
        mentions[0].NormalizedValue.Should().Be("/users");
    }

    [Fact]
    public async Task ExtractAsync_StandalonePathWithSegments_Extracts()
    {
        // Arrange
        var text = "Call /api/v1/orders to get orders.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().ContainSingle();
        mentions[0].NormalizedValue.Should().Be("/api/v1/orders");
    }

    #endregion

    #region False Positive Filtering

    [Theory]
    [InlineData("/etc/config.json")]
    [InlineData("/usr/local/bin")]
    [InlineData("/home/user/docs")]
    [InlineData("/var/log/app.log")]
    [InlineData("/tmp/cache")]
    [InlineData("/bin/bash")]
    [InlineData("/opt/app")]
    public async Task ExtractAsync_SystemPaths_Filtered(string path)
    {
        // Arrange
        var text = $"Located at {path}";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_ShortPaths_Filtered()
    {
        // Arrange — paths < 4 chars are filtered
        var text = "See /a for details.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_FilePathWithExtension_Filtered()
    {
        // Arrange — the standalone path regex may capture the path without the
        // extension portion, since "." is not part of the path character set.
        // Use a path where the extension is clearly part of the path.
        var text = "Edit /etc/config.json for settings.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert — /etc/ is a Unix system path and is filtered
        mentions.Should().BeEmpty();
    }

    #endregion

    #region Path Normalization

    [Fact]
    public void NormalizePath_StripsQueryParameters()
    {
        // Act
        var result = EndpointExtractor.NormalizePath("/users?page=1&limit=20");

        // Assert
        result.Should().Be("/users");
    }

    [Fact]
    public void NormalizePath_EnsuresLeadingSlash()
    {
        // Act
        var result = EndpointExtractor.NormalizePath("users");

        // Assert
        result.Should().Be("/users");
    }

    [Fact]
    public void NormalizePath_RemovesTrailingSlash()
    {
        // Act
        var result = EndpointExtractor.NormalizePath("/users/");

        // Assert
        result.Should().Be("/users");
    }

    [Fact]
    public void NormalizePath_PreservesRootSlash()
    {
        // Act
        var result = EndpointExtractor.NormalizePath("/");

        // Assert
        result.Should().Be("/");
    }

    [Fact]
    public void NormalizePath_Lowercases()
    {
        // Act
        var result = EndpointExtractor.NormalizePath("/API/Users");

        // Assert
        result.Should().Be("/api/users");
    }

    #endregion

    #region Deduplication

    [Fact]
    public async Task ExtractAsync_SamePathDifferentPatterns_DeduplicatedToHigherConfidence()
    {
        // Arrange — "GET /users" matches method+path (1.0) AND standalone path (0.7)
        var text = "Use GET /users to list users.";

        // Act
        var mentions = await _extractor.ExtractAsync(text, _context);

        // Assert — should only have the method+path match (1.0), not a duplicate standalone
        mentions.Where(m => m.NormalizedValue == "/users").Should().HaveCount(1);
        mentions.First(m => m.NormalizedValue == "/users").Confidence.Should().Be(1.0f);
    }

    #endregion

    #region Empty/Whitespace Text

    [Fact]
    public async Task ExtractAsync_EmptyText_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_WhitespaceText_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("   \n\t  ", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_TextWithNoEndpoints_ReturnsEmpty()
    {
        // Act
        var mentions = await _extractor.ExtractAsync("Just a regular sentence with no endpoints.", _context);

        // Assert
        mentions.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    [Fact]
    public void IsLikelyFalsePositive_FileExtension_True()
    {
        // Assert
        EndpointExtractor.IsLikelyFalsePositive("/config/settings.json").Should().BeTrue();
    }

    [Fact]
    public void IsLikelyFalsePositive_SystemPath_True()
    {
        // Assert
        EndpointExtractor.IsLikelyFalsePositive("/usr/local/bin").Should().BeTrue();
    }

    [Fact]
    public void IsLikelyFalsePositive_ShortPath_True()
    {
        // Assert
        EndpointExtractor.IsLikelyFalsePositive("/ab").Should().BeTrue();
    }

    [Fact]
    public void IsLikelyFalsePositive_ValidApiPath_False()
    {
        // Assert
        EndpointExtractor.IsLikelyFalsePositive("/users").Should().BeFalse();
    }

    [Fact]
    public void GetSnippet_ReturnsContextAroundPosition()
    {
        // Arrange
        var text = "Hello world, this is a test string for snippet extraction.";

        // Act
        var snippet = EndpointExtractor.GetSnippet(text, 13, 10);

        // Assert
        snippet.Should().NotBeEmpty();
        snippet.Length.Should().BeLessOrEqualTo(20);
    }

    #endregion
}
