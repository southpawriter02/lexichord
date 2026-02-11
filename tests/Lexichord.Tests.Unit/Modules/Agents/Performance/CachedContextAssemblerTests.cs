// -----------------------------------------------------------------------
// <copyright file="CachedContextAssemblerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Performance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Performance;

/// <summary>
/// Unit tests for <see cref="CachedContextAssembler"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate:
/// </para>
/// <list type="bullet">
///   <item><description>Cache miss triggers context assembly</description></item>
///   <item><description>Cache hit returns cached context without reassembly</description></item>
///   <item><description>Cache invalidation clears entries for a document</description></item>
///   <item><description>Cache hit ratio tracking</description></item>
///   <item><description>Argument validation</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.8c")]
public class CachedContextAssemblerTests : IDisposable
{
    private readonly Mock<IContextInjector> _injectorMock;
    private readonly CachedContextAssembler _assembler;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public CachedContextAssemblerTests()
    {
        _injectorMock = new Mock<IContextInjector>();

        _assembler = new CachedContextAssembler(
            _injectorMock.Object,
            NullLogger<CachedContextAssembler>.Instance,
            Options.Create(new PerformanceOptions
            {
                ContextCacheDuration = TimeSpan.FromMinutes(5) // Long duration for tests
            }));
    }

    /// <summary>
    /// Creates a standard context request for testing.
    /// </summary>
    private static ContextRequest CreateTestRequest(
        string? docPath = "test.md",
        bool includeStyle = true,
        bool includeRag = true,
        int maxChunks = 3)
        => new(docPath, null, null, includeStyle, includeRag, maxChunks);

    /// <summary>
    /// Creates a mock context result.
    /// </summary>
    private static IDictionary<string, object> CreateTestContext(string content = "test context")
        => new Dictionary<string, object>
        {
            ["style_rules"] = "Use active voice",
            ["context"] = content
        };

    #region Cache Miss Tests

    /// <summary>
    /// Verifies that first access triggers context assembly.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_CacheMiss_AssemblesContext()
    {
        // Arrange
        var expectedContext = CreateTestContext();
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContext);

        // Act
        var result = await _assembler.GetOrCreateAsync(
            "doc.md",
            CreateTestRequest("doc.md"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("style_rules");
        _injectorMock.Verify(
            i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that cache miss increments miss counter.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_CacheMiss_IncrementsMissCounter()
    {
        // Arrange
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestContext());

        // Act
        await _assembler.GetOrCreateAsync("doc1.md", CreateTestRequest("doc1.md"), CancellationToken.None);

        // Assert — 1 miss, 0 hits
        _assembler.CacheHitRatio.Should().Be(0.0);
    }

    #endregion

    #region Cache Hit Tests

    /// <summary>
    /// Verifies that second access returns cached context.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_SameKeyTwice_ReturnsCachedOnSecondCall()
    {
        // Arrange
        var expectedContext = CreateTestContext("cached content");
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContext);

        var request = CreateTestRequest("doc.md");

        // Act
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None);
        var result = await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None);

        // Assert — inner was only called once
        result.Should().BeSameAs(expectedContext);
        _injectorMock.Verify(
            i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that cache hit increments hit counter.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_CacheHit_IncrementsHitCounter()
    {
        // Arrange
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestContext());

        var request = CreateTestRequest("doc.md");

        // Act
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None);
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None);

        // Assert — 1 miss, 1 hit = 50% ratio
        _assembler.CacheHitRatio.Should().Be(0.5);
    }

    /// <summary>
    /// Verifies that different request params create separate cache entries.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_DifferentParams_CreatesSeparateCacheEntries()
    {
        // Arrange
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestContext());

        // Act — same document, different request configurations
        await _assembler.GetOrCreateAsync(
            "doc.md",
            CreateTestRequest("doc.md", includeStyle: true, includeRag: true),
            CancellationToken.None);

        await _assembler.GetOrCreateAsync(
            "doc.md",
            CreateTestRequest("doc.md", includeStyle: false, includeRag: true),
            CancellationToken.None);

        // Assert — both calls should have been cache misses (different keys)
        _injectorMock.Verify(
            i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region Invalidation Tests

    /// <summary>
    /// Verifies that invalidation causes a cache miss on next access.
    /// </summary>
    [Fact]
    public async Task Invalidate_ThenAccess_CausesNewAssembly()
    {
        // Arrange
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestContext());

        var request = CreateTestRequest("doc.md");

        // Populate cache
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None);

        // Act — invalidate and access again
        _assembler.Invalidate("doc.md");
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None);

        // Assert — inner should have been called twice (miss, invalidate, miss)
        _injectorMock.Verify(
            i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that invalidating a non-existent path does not throw.
    /// </summary>
    [Fact]
    public void Invalidate_NonExistentPath_DoesNotThrow()
    {
        // Act
        var action = () => _assembler.Invalidate("nonexistent.md");

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that invalidation only affects the specified document.
    /// </summary>
    [Fact]
    public async Task Invalidate_OnlyAffectsSpecifiedDocument()
    {
        // Arrange
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestContext());

        // Populate cache for two documents
        await _assembler.GetOrCreateAsync("doc1.md", CreateTestRequest("doc1.md"), CancellationToken.None);
        await _assembler.GetOrCreateAsync("doc2.md", CreateTestRequest("doc2.md"), CancellationToken.None);

        // Act — invalidate only doc1
        _assembler.Invalidate("doc1.md");

        // Access both again
        await _assembler.GetOrCreateAsync("doc1.md", CreateTestRequest("doc1.md"), CancellationToken.None);
        await _assembler.GetOrCreateAsync("doc2.md", CreateTestRequest("doc2.md"), CancellationToken.None);

        // Assert — doc1 should have 2 assembly calls (miss, invalidate, miss), doc2 should have 1 (hit on second access)
        _injectorMock.Verify(
            i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3)); // 2 initial misses + 1 re-assembly for doc1
    }

    #endregion

    #region CacheHitRatio Tests

    /// <summary>
    /// Verifies that cache hit ratio is 0 when no requests have been made.
    /// </summary>
    [Fact]
    public void CacheHitRatio_NoRequests_ReturnsZero()
    {
        // Assert
        _assembler.CacheHitRatio.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies that cache hit ratio is accurate after mixed hits and misses.
    /// </summary>
    [Fact]
    public async Task CacheHitRatio_MixedHitsAndMisses_ReturnsAccurateRatio()
    {
        // Arrange
        _injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestContext());

        var request = CreateTestRequest("doc.md");

        // Act — 1 miss + 3 hits = 75% ratio
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None); // miss
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None); // hit
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None); // hit
        await _assembler.GetOrCreateAsync("doc.md", request, CancellationToken.None); // hit

        // Assert
        _assembler.CacheHitRatio.Should().Be(0.75);
    }

    #endregion

    #region Argument Validation Tests

    /// <summary>
    /// Verifies that null document path throws.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _assembler.GetOrCreateAsync(null!, CreateTestRequest(), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    /// <summary>
    /// Verifies that null request throws.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _assembler.GetOrCreateAsync("doc.md", null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    /// <summary>
    /// Verifies that null document path for invalidate throws.
    /// </summary>
    [Fact]
    public void Invalidate_NullPath_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _assembler.Invalidate(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws for null inner injector.
    /// </summary>
    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new CachedContextAssembler(
            null!,
            NullLogger<CachedContextAssembler>.Instance,
            Options.Create(new PerformanceOptions()));

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("inner");
    }

    /// <summary>
    /// Verifies that constructor throws for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new CachedContextAssembler(
            _injectorMock.Object,
            null!,
            Options.Create(new PerformanceOptions()));

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructor throws for null options.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new CachedContextAssembler(
            _injectorMock.Object,
            NullLogger<CachedContextAssembler>.Instance,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    /// <summary>
    /// Disposes the assembler to clean up the memory cache.
    /// </summary>
    public void Dispose()
    {
        _assembler.Dispose();
    }
}
