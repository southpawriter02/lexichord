// =============================================================================
// File: VectorSearchBenchmarks.cs
// Project: Lexichord.Benchmarks
// Description: Performance benchmarks for vector similarity search.
// =============================================================================
// v0.4.8c: Measures in-memory cosine similarity computation latency.
//   - 100 vectors (small corpus)
//   - 1000 vectors (medium corpus)
//   - 10000 vectors (large corpus)
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Lexichord.Benchmarks.Search;

/// <summary>
/// Performance benchmarks for vector similarity search operations.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure in-memory cosine similarity calculations.
/// Target baseline: CosineSimilarity_TopK (10K corpus) &lt; 200ms.
/// </para>
/// <para>
/// Note: This benchmarks the computational cost of similarity calculations,
/// not the full database query path. For database benchmarks, see integration tests.
/// </para>
/// </remarks>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class VectorSearchBenchmarks
{
    private float[][] _corpus = null!;
    private float[] _queryVector = null!;
    private const int VectorDimensions = 1536;

    /// <summary>
    /// Corpus size parameter for the benchmark (100, 1000, 10000 vectors).
    /// </summary>
    [Params(100, 1000, 10000)]
    public int CorpusSize { get; set; }

    /// <summary>
    /// One-time setup that generates the test corpus and query vector.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _corpus = new float[CorpusSize][];

        for (int i = 0; i < CorpusSize; i++)
        {
            _corpus[i] = GenerateNormalizedVector(random);
        }

        _queryVector = GenerateNormalizedVector(random);
    }

    /// <summary>
    /// Benchmark computing top-K results from the corpus.
    /// </summary>
    [Benchmark]
    public List<(int Index, float Score)> CosineSimilarity_TopK()
    {
        // LOGIC: Full corpus scan with sorting for top-K results.
        var results = new List<(int, float)>(CorpusSize);

        for (int i = 0; i < CorpusSize; i++)
        {
            var score = ComputeCosineSimilarity(_queryVector, _corpus[i]);
            results.Add((i, score));
        }

        return results.OrderByDescending(r => r.Item2).Take(10).ToList();
    }

    /// <summary>
    /// Benchmark computing results with threshold filtering.
    /// </summary>
    [Benchmark]
    public List<(int Index, float Score)> CosineSimilarity_WithThreshold()
    {
        // LOGIC: Early filtering reduces result set before sorting.
        var results = new List<(int, float)>();
        const float threshold = 0.7f;

        for (int i = 0; i < CorpusSize; i++)
        {
            var score = ComputeCosineSimilarity(_queryVector, _corpus[i]);
            if (score >= threshold)
            {
                results.Add((i, score));
            }
        }

        return results.OrderByDescending(r => r.Item2).Take(10).ToList();
    }

    /// <summary>
    /// Computes cosine similarity between two vectors.
    /// </summary>
    private static float ComputeCosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;

        for (int i = 0; i < VectorDimensions; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }

    /// <summary>
    /// Generates a random normalized vector (unit length).
    /// </summary>
    private static float[] GenerateNormalizedVector(Random random)
    {
        var vector = new float[VectorDimensions];
        float magnitude = 0;

        for (int i = 0; i < VectorDimensions; i++)
        {
            vector[i] = (float)(random.NextDouble() * 2 - 1);
            magnitude += vector[i] * vector[i];
        }

        magnitude = MathF.Sqrt(magnitude);
        for (int i = 0; i < VectorDimensions; i++)
        {
            vector[i] /= magnitude;
        }

        return vector;
    }
}
