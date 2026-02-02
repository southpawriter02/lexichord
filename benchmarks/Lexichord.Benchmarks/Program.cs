// =============================================================================
// File: Program.cs
// Project: Lexichord.Benchmarks
// Description: Entry point for BenchmarkDotNet performance benchmark suite.
// =============================================================================
// v0.4.8c: Performance benchmarks for RAG module components.
//   - Chunking strategy throughput
//   - Token counter performance
//   - Vector search latency
//   - Memory usage profiling
// =============================================================================

using BenchmarkDotNet.Running;

namespace Lexichord.Benchmarks;

/// <summary>
/// Entry point for the Lexichord benchmark suite.
/// </summary>
/// <remarks>
/// <para>
/// This program uses BenchmarkDotNet's <see cref="BenchmarkSwitcher"/> to enable
/// running specific benchmark categories from the command line.
/// </para>
/// <para><b>Usage:</b></para>
/// <list type="bullet">
///   <item>Run all: <c>dotnet run -c Release</c></item>
///   <item>Run specific: <c>dotnet run -c Release -- --filter "*Chunking*"</c></item>
///   <item>With memory: <c>dotnet run -c Release -- --memory</c></item>
/// </list>
/// </remarks>
public static class Program
{
    /// <summary>
    /// Main entry point for the benchmark application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to BenchmarkDotNet.</param>
    public static void Main(string[] args)
    {
        // LOGIC: BenchmarkSwitcher scans the assembly for all benchmark classes
        // and provides command-line filtering, export options, and job configuration.
        var summary = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}
