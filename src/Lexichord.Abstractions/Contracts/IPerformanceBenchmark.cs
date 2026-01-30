namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Interface for running performance benchmarks.
/// </summary>
/// <remarks>
/// LOGIC: IPerformanceBenchmark provides standardized performance
/// testing for the linting system. It measures scan duration,
/// memory usage, and UI responsiveness under various load conditions.
///
/// Thread Safety:
/// - Benchmark methods can be called from any thread
/// - Results are thread-safe snapshots
///
/// Version: v0.2.7d
/// </remarks>
public interface IPerformanceBenchmark
{
    /// <summary>
    /// Runs a linting benchmark with the specified content.
    /// </summary>
    /// <param name="content">Content to lint.</param>
    /// <param name="iterations">Number of iterations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Benchmark results.</returns>
    /// <remarks>
    /// LOGIC: Runs the linting pipeline multiple times and
    /// collects performance statistics. Each iteration starts
    /// fresh to avoid cache effects skewing results.
    /// </remarks>
    Task<BenchmarkResult> RunLintingBenchmarkAsync(
        string content,
        int iterations,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a typing simulation benchmark.
    /// </summary>
    /// <param name="baseContent">Initial document content.</param>
    /// <param name="charactersToType">Number of characters to simulate.</param>
    /// <param name="typingIntervalMs">Milliseconds between keystrokes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Typing benchmark results.</returns>
    /// <remarks>
    /// LOGIC: Simulates user typing to measure UI responsiveness
    /// while the linting system is active. Measures frame times
    /// during the typing simulation.
    /// </remarks>
    Task<TypingBenchmarkResult> RunTypingBenchmarkAsync(
        string baseContent,
        int charactersToType,
        int typingIntervalMs = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a scrolling benchmark.
    /// </summary>
    /// <param name="content">Document content.</param>
    /// <param name="scrollEvents">Number of scroll events to simulate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scrolling benchmark results.</returns>
    /// <remarks>
    /// LOGIC: Simulates scrolling through a large document to
    /// measure rendering performance and violation display updates.
    /// </remarks>
    Task<ScrollBenchmarkResult> RunScrollBenchmarkAsync(
        string content,
        int scrollEvents,
        CancellationToken cancellationToken = default);
}
