// -----------------------------------------------------------------------
// <copyright file="PerformanceBenchmarks.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Performance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Performance;

/// <summary>
/// BenchmarkDotNet performance benchmarks for v0.6.8c Performance Optimization.
/// </summary>
/// <remarks>
/// <para>
/// Measures latency and throughput of:
/// </para>
/// <list type="bullet">
///   <item><description>Conversation memory trimming at various sizes</description></item>
///   <item><description>Memory estimation calculations</description></item>
///   <item><description>Context cache hit vs miss performance</description></item>
///   <item><description>Cache invalidation cost</description></item>
/// </list>
/// <para>
/// Run benchmarks with: <c>dotnet run -c Release -- --filter *PerformanceBenchmarks*</c>
/// </para>
/// <para>
/// <b>Performance Targets (from LCS-DES-068c):</b>
/// </para>
/// <list type="bullet">
///   <item><description>Context assembly cache hit: &lt;5ms (P95)</description></item>
///   <item><description>Conversation trim (100 messages): &lt;10ms (P95)</description></item>
///   <item><description>Memory estimation: &lt;1ms (P95)</description></item>
/// </list>
/// </remarks>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PerformanceBenchmarks
{
    private ConversationMemoryManager _memoryManager = null!;
    private CachedContextAssembler _cachedAssembler = null!;
    private List<ChatMessage> _smallConversation = null!;
    private List<ChatMessage> _mediumConversation = null!;
    private List<ChatMessage> _largeConversation = null!;

    /// <summary>
    /// Sets up benchmark dependencies with mock services.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Set up memory manager
        var tokenCounterMock = new Mock<ITokenCounter>();
        tokenCounterMock
            .Setup(tc => tc.CountTokens(It.IsAny<string>()))
            .Returns((string s) => s.Length / 4);

        _memoryManager = new ConversationMemoryManager(
            NullLogger<ConversationMemoryManager>.Instance,
            tokenCounterMock.Object,
            Options.Create(new PerformanceOptions()));

        // Set up cached context assembler with a mock injector
        var injectorMock = new Mock<IContextInjector>();
        injectorMock
            .Setup(i => i.AssembleContextAsync(It.IsAny<ContextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object>
            {
                ["style_rules"] = "Use active voice. Be concise.",
                ["context"] = "Relevant context from RAG search results."
            });

        _cachedAssembler = new CachedContextAssembler(
            injectorMock.Object,
            NullLogger<CachedContextAssembler>.Instance,
            Options.Create(new PerformanceOptions()));

        // Create conversation datasets of varying sizes
        _smallConversation = CreateConversation(10);
        _mediumConversation = CreateConversation(50);
        _largeConversation = CreateConversation(200);

        // Warm up the cache with one entry
        _cachedAssembler.GetOrCreateAsync(
            "warmup.md",
            new ContextRequest("warmup.md", null, null, true, true, 3),
            CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Cleans up resources after benchmarks complete.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _cachedAssembler.Dispose();
    }

    #region Conversation Trimming Benchmarks

    /// <summary>
    /// Benchmarks trimming a 10-message conversation.
    /// </summary>
    [Benchmark(Description = "TrimToLimit (10 messages, no trim needed)")]
    public void TrimSmallConversation_NoTrim()
    {
        var copy = new List<ChatMessage>(_smallConversation);
        _memoryManager.TrimToLimit(copy, 50);
    }

    /// <summary>
    /// Benchmarks trimming a 50-message conversation to 20 messages.
    /// </summary>
    [Benchmark(Description = "TrimToLimit (50 → 20 messages)")]
    public void TrimMediumConversation()
    {
        var copy = new List<ChatMessage>(_mediumConversation);
        _memoryManager.TrimToLimit(copy, 20);
    }

    /// <summary>
    /// Benchmarks trimming a 200-message conversation to 50 messages.
    /// Target: &lt;10ms at P95.
    /// </summary>
    [Benchmark(Description = "TrimToLimit (200 → 50 messages)")]
    public void TrimLargeConversation()
    {
        var copy = new List<ChatMessage>(_largeConversation);
        _memoryManager.TrimToLimit(copy, 50);
    }

    #endregion

    #region Memory Estimation Benchmarks

    /// <summary>
    /// Benchmarks memory estimation on a 50-message conversation.
    /// Target: &lt;1ms at P95.
    /// </summary>
    [Benchmark(Description = "MemoryEstimation (50 messages)")]
    public void EstimateMemory_MediumConversation()
    {
        var copy = new List<ChatMessage>(_mediumConversation);
        _memoryManager.TrimToLimit(copy, 1000); // No trim, just estimate
    }

    /// <summary>
    /// Benchmarks memory estimation on a 200-message conversation.
    /// </summary>
    [Benchmark(Description = "MemoryEstimation (200 messages)")]
    public void EstimateMemory_LargeConversation()
    {
        var copy = new List<ChatMessage>(_largeConversation);
        _memoryManager.TrimToLimit(copy, 1000); // No trim, just estimate
    }

    #endregion

    #region Context Cache Benchmarks

    /// <summary>
    /// Benchmarks a context cache hit.
    /// Target: &lt;5ms at P95.
    /// </summary>
    [Benchmark(Description = "ContextCache Hit")]
    public async Task ContextCacheHit()
    {
        await _cachedAssembler.GetOrCreateAsync(
            "warmup.md",
            new ContextRequest("warmup.md", null, null, true, true, 3),
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmarks a context cache miss (requires assembly).
    /// </summary>
    [Benchmark(Description = "ContextCache Miss")]
    public async Task ContextCacheMiss()
    {
        // Invalidate first to ensure a miss
        _cachedAssembler.Invalidate("miss-test.md");
        await _cachedAssembler.GetOrCreateAsync(
            "miss-test.md",
            new ContextRequest("miss-test.md", null, null, true, true, 3),
            CancellationToken.None);
    }

    /// <summary>
    /// Benchmarks cache invalidation cost.
    /// </summary>
    [Benchmark(Description = "ContextCache Invalidation")]
    public void ContextCacheInvalidation()
    {
        _cachedAssembler.Invalidate("nonexistent.md");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a conversation of the specified size with alternating user/assistant messages.
    /// </summary>
    /// <param name="messageCount">The total number of messages to generate.</param>
    /// <returns>A list of chat messages with a system message and alternating turns.</returns>
    private static List<ChatMessage> CreateConversation(int messageCount)
    {
        var messages = new List<ChatMessage>(messageCount)
        {
            ChatMessage.System("You are a helpful writing assistant.")
        };

        for (var i = 1; i < messageCount; i++)
        {
            messages.Add(i % 2 == 1
                ? ChatMessage.User($"User message number {i} with some content to make it realistic.")
                : ChatMessage.Assistant($"Assistant response number {i} with detailed and helpful content."));
        }

        return messages;
    }

    #endregion
}
