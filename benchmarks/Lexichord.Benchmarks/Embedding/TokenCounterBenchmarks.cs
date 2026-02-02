// =============================================================================
// File: TokenCounterBenchmarks.cs
// Project: Lexichord.Benchmarks
// Description: Performance benchmarks for token counting operations.
// =============================================================================
// v0.4.8c: Measures tiktoken encoding/decoding performance.
//   - Short text (single sentence)
//   - Medium text (1000 words)
//   - Long text (10000 words)
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lexichord.Modules.RAG.Embedding;

namespace Lexichord.Benchmarks.Embedding;

/// <summary>
/// Performance benchmarks for tiktoken-based token counting.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure the throughput of token counting and encoding
/// operations. Target baseline: CountTokens_10000Words &lt; 50ms.
/// </para>
/// </remarks>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class TokenCounterBenchmarks
{
    private TiktokenTokenCounter _counter = null!;
    private string _shortText = null!;
    private string _mediumText = null!;
    private string _longText = null!;

    /// <summary>
    /// One-time setup that initializes the token counter and test strings.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _counter = new TiktokenTokenCounter();
        _shortText = "Hello world, this is a test of the token counting system.";
        _mediumText = string.Join(" ", Enumerable.Repeat("word", 1000));
        _longText = string.Join(" ", Enumerable.Repeat("word", 10000));
    }

    // =========================================================================
    // CountTokens Benchmarks
    // =========================================================================

    /// <summary>Baseline: count tokens in a short phrase.</summary>
    [Benchmark(Baseline = true)]
    public int CountTokens_Short() => _counter.CountTokens(_shortText);

    /// <summary>Count tokens in 1000 words.</summary>
    [Benchmark]
    public int CountTokens_1000Words() => _counter.CountTokens(_mediumText);

    /// <summary>Count tokens in 10000 words.</summary>
    [Benchmark]
    public int CountTokens_10000Words() => _counter.CountTokens(_longText);

    // =========================================================================
    // Encode Benchmarks
    // =========================================================================

    /// <summary>Encode a short phrase to token IDs.</summary>
    [Benchmark]
    public IReadOnlyList<int> Encode_Short() => _counter.Encode(_shortText);

    /// <summary>Encode 1000 words to token IDs.</summary>
    [Benchmark]
    public IReadOnlyList<int> Encode_1000Words() => _counter.Encode(_mediumText);

    // =========================================================================
    // Truncate Benchmarks
    // =========================================================================

    /// <summary>Truncate long text to 1000 tokens.</summary>
    [Benchmark]
    public (string, bool) Truncate_LongText() =>
        _counter.TruncateToTokenLimit(_longText, 1000);

    /// <summary>Truncate medium text (no truncation needed).</summary>
    [Benchmark]
    public (string, bool) Truncate_NoOp() =>
        _counter.TruncateToTokenLimit(_shortText, 1000);
}
