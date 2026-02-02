// =============================================================================
// File: ChunkingBenchmarks.cs
// Project: Lexichord.Benchmarks
// Description: Performance benchmarks for text chunking strategies.
// =============================================================================
// v0.4.8c: Measures throughput and memory usage for chunking different sizes.
//   - FixedSizeChunkingStrategy: Baseline strategy
//   - ParagraphChunkingStrategy: Paragraph-aware splitting
//   - MarkdownHeaderChunkingStrategy: Header-structure-aware splitting
// =============================================================================

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Chunking;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Benchmarks.Chunking;

/// <summary>
/// Performance benchmarks for text chunking strategies.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the throughput of each chunking strategy across
/// different document sizes (1KB, 10KB, 100KB). Target baselines:
/// </para>
/// <list type="bullet">
///   <item>FixedSize_100KB: &lt; 100ms</item>
///   <item>Paragraph_100KB: &lt; 150ms</item>
///   <item>Markdown_100KB: &lt; 200ms</item>
/// </list>
/// </remarks>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[RankColumn]
public class ChunkingBenchmarks
{
    private string _smallDoc = null!;   // 1KB
    private string _mediumDoc = null!;  // 10KB
    private string _largeDoc = null!;   // 100KB

    private FixedSizeChunkingStrategy _fixedSizeStrategy = null!;
    private ParagraphChunkingStrategy _paragraphStrategy = null!;
    private MarkdownHeaderChunkingStrategy _markdownStrategy = null!;
    private ChunkingOptions _options = null!;

    /// <summary>
    /// One-time setup that generates test documents and initializes strategies.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // LOGIC: Generate deterministic documents for reproducible benchmarks.
        _smallDoc = GenerateMarkdownDocument(1024);
        _mediumDoc = GenerateMarkdownDocument(10 * 1024);
        _largeDoc = GenerateMarkdownDocument(100 * 1024);

        // LOGIC: Initialize strategies with null loggers for benchmarking.
        _fixedSizeStrategy = new FixedSizeChunkingStrategy(
            NullLogger<FixedSizeChunkingStrategy>.Instance);
        _paragraphStrategy = new ParagraphChunkingStrategy(
            NullLogger<ParagraphChunkingStrategy>.Instance,
            _fixedSizeStrategy);
        _markdownStrategy = new MarkdownHeaderChunkingStrategy(
            NullLogger<MarkdownHeaderChunkingStrategy>.Instance,
            _paragraphStrategy,
            _fixedSizeStrategy);

        _options = ChunkingOptions.Default;
    }

    // =========================================================================
    // Fixed Size Strategy Benchmarks
    // =========================================================================

    /// <summary>Baseline benchmark for 1KB document with fixed-size chunking.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FixedSize")]
    public IReadOnlyList<TextChunk> FixedSize_1KB() =>
        _fixedSizeStrategy.Split(_smallDoc, _options);

    /// <summary>Fixed-size chunking on 10KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("FixedSize")]
    public IReadOnlyList<TextChunk> FixedSize_10KB() =>
        _fixedSizeStrategy.Split(_mediumDoc, _options);

    /// <summary>Fixed-size chunking on 100KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("FixedSize")]
    public IReadOnlyList<TextChunk> FixedSize_100KB() =>
        _fixedSizeStrategy.Split(_largeDoc, _options);

    // =========================================================================
    // Paragraph Strategy Benchmarks
    // =========================================================================

    /// <summary>Paragraph-aware chunking on 1KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("Paragraph")]
    public IReadOnlyList<TextChunk> Paragraph_1KB() =>
        _paragraphStrategy.Split(_smallDoc, _options);

    /// <summary>Paragraph-aware chunking on 10KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("Paragraph")]
    public IReadOnlyList<TextChunk> Paragraph_10KB() =>
        _paragraphStrategy.Split(_mediumDoc, _options);

    /// <summary>Paragraph-aware chunking on 100KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("Paragraph")]
    public IReadOnlyList<TextChunk> Paragraph_100KB() =>
        _paragraphStrategy.Split(_largeDoc, _options);

    // =========================================================================
    // Markdown Strategy Benchmarks
    // =========================================================================

    /// <summary>Markdown header-aware chunking on 1KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("Markdown")]
    public IReadOnlyList<TextChunk> Markdown_1KB() =>
        _markdownStrategy.Split(_smallDoc, _options);

    /// <summary>Markdown header-aware chunking on 10KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("Markdown")]
    public IReadOnlyList<TextChunk> Markdown_10KB() =>
        _markdownStrategy.Split(_mediumDoc, _options);

    /// <summary>Markdown header-aware chunking on 100KB document.</summary>
    [Benchmark]
    [BenchmarkCategory("Markdown")]
    public IReadOnlyList<TextChunk> Markdown_100KB() =>
        _markdownStrategy.Split(_largeDoc, _options);

    // =========================================================================
    // Document Generation
    // =========================================================================

    /// <summary>
    /// Generates a deterministic Markdown document of the specified size.
    /// </summary>
    /// <param name="targetBytes">Approximate target size in bytes.</param>
    /// <returns>Generated Markdown content.</returns>
    private static string GenerateMarkdownDocument(int targetBytes)
    {
        var sb = new StringBuilder();
        var sectionCount = 0;
        var random = new Random(42); // Deterministic seed for reproducibility

        while (sb.Length < targetBytes)
        {
            sectionCount++;
            sb.AppendLine($"## Section {sectionCount}");
            sb.AppendLine();

            var paragraphs = random.Next(2, 5);
            for (int p = 0; p < paragraphs; p++)
            {
                var sentences = random.Next(3, 8);
                for (int s = 0; s < sentences; s++)
                {
                    sb.Append(GenerateSentence(random));
                    sb.Append(' ');
                }
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a random sentence from a word pool.
    /// </summary>
    /// <param name="random">Random number generator.</param>
    /// <returns>A sentence string.</returns>
    private static string GenerateSentence(Random random)
    {
        var words = new[]
        {
            "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing",
            "technical", "documentation", "writing", "platform", "enables",
            "semantic", "search", "vector", "embedding", "retrieval"
        };
        var length = random.Next(5, 15);
        return string.Join(" ", Enumerable.Range(0, length).Select(_ => words[random.Next(words.Length)]));
    }
}
