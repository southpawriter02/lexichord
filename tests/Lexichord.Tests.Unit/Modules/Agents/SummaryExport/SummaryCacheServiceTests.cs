// -----------------------------------------------------------------------
// <copyright file="SummaryCacheServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.SummaryExport.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.SummaryExport;

/// <summary>
/// Unit tests for <see cref="SummaryCacheService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6c")]
public class SummaryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private readonly Mock<IFileService> _mockFileService;
    private readonly ILogger<SummaryCacheService> _logger;
    private readonly SummaryCacheService _service;

    public SummaryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockFileService = new Mock<IFileService>();
        _logger = NullLogger<SummaryCacheService>.Instance;
        _service = new SummaryCacheService(_memoryCache, _mockFileService.Object, _logger);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    // ── Constructor ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SummaryCacheService(null!, _mockFileService.Object, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("memoryCache");
    }

    [Fact]
    public void Constructor_WithNullFileService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SummaryCacheService(_memoryCache, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SummaryCacheService(_memoryCache, _mockFileService.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── ComputeContentHash ──────────────────────────────────────────────

    [Fact]
    public void ComputeContentHash_WithContent_ReturnsSHA256Hash()
    {
        // Arrange
        const string content = "Test content for hashing";

        // Act
        var hash = _service.ComputeContentHash(content);

        // Assert
        hash.Should().StartWith("sha256:");
        hash.Length.Should().Be(7 + 64); // "sha256:" + 64 hex chars
    }

    [Fact]
    public void ComputeContentHash_WithSameContent_ReturnsSameHash()
    {
        // Arrange
        const string content = "Identical content";

        // Act
        var hash1 = _service.ComputeContentHash(content);
        var hash2 = _service.ComputeContentHash(content);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeContentHash_WithDifferentContent_ReturnsDifferentHash()
    {
        // Act
        var hash1 = _service.ComputeContentHash("Content A");
        var hash2 = _service.ComputeContentHash("Content B");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeContentHash_WithNullContent_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.ComputeContentHash(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("content");
    }

    [Fact]
    public void ComputeContentHash_WithEmptyString_ReturnsValidHash()
    {
        // Act
        var hash = _service.ComputeContentHash(string.Empty);

        // Assert
        hash.Should().StartWith("sha256:");
        hash.Length.Should().Be(71); // "sha256:" + 64 hex chars
    }

    // ── GetAsync Argument Validation ────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.GetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task GetAsync_WhenNotCached_ReturnsNull()
    {
        // Arrange
        _mockFileService
            .Setup(f => f.LoadAsync(It.IsAny<string>(), It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadResult(false, string.Empty));

        // Act
        var result = await _service.GetAsync("/path/to/uncached.md");

        // Assert
        result.Should().BeNull();
    }

    // ── SetAsync Argument Validation ────────────────────────────────────

    [Fact]
    public async Task SetAsync_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var act = () => _service.SetAsync(null!, summary, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public async Task SetAsync_WithNullSummary_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.SetAsync("/path/to/document.md", null!, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("summary");
    }

    // ── ClearAsync Argument Validation ──────────────────────────────────

    [Fact]
    public async Task ClearAsync_WithNullDocumentPath_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.ClearAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    // ── Integration Tests ───────────────────────────────────────────────

    [Fact]
    public async Task SetAndGet_RoundTrip_ReturnsCachedSummary()
    {
        // Arrange
        const string documentPath = "/path/to/document.md";
        const string content = "Document content for caching";
        var summary = CreateTestSummary();

        // Setup file service to return document content
        _mockFileService
            .Setup(f => f.LoadAsync(documentPath, It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadResult(true, documentPath, content));

        _mockFileService
            .Setup(f => f.LoadAsync(It.Is<string>(s => s.Contains(".lexichord")), It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadResult(false, string.Empty));

        _mockFileService
            .Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveResult(true, string.Empty, 0, TimeSpan.Zero));

        // Act - Cache the summary
        await _service.SetAsync(documentPath, summary, null);

        // Act - Retrieve the cached summary
        var cached = await _service.GetAsync(documentPath);

        // Assert
        cached.Should().NotBeNull();
        cached!.Summary.Summary.Should().Be(summary.Summary);
        cached.ContentHash.Should().StartWith("sha256:");
        cached.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task Clear_RemovesFromCache()
    {
        // Arrange
        const string documentPath = "/path/to/document.md";
        const string content = "Document content";
        var summary = CreateTestSummary();

        _mockFileService
            .Setup(f => f.LoadAsync(documentPath, It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadResult(true, documentPath, content));

        _mockFileService
            .Setup(f => f.LoadAsync(It.Is<string>(s => s.Contains(".lexichord")), It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadResult(false, string.Empty));

        _mockFileService
            .Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Encoding?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveResult(true, string.Empty, 0, TimeSpan.Zero));

        // Setup - Cache then clear
        await _service.SetAsync(documentPath, summary, null);
        await _service.ClearAsync(documentPath);

        // Act
        var cached = await _service.GetAsync(documentPath);

        // Assert - Should return null after clearing
        cached.Should().BeNull();
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private static SummarizationResult CreateTestSummary()
    {
        return new SummarizationResult
        {
            Summary = "This is a test summary for caching.",
            Mode = SummarizationMode.BulletPoints,
            OriginalWordCount = 500,
            SummaryWordCount = 50,
            Usage = UsageMetrics.Zero
        };
    }
}
