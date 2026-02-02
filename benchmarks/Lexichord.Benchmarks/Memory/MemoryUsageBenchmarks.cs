// =============================================================================
// File: MemoryUsageBenchmarks.cs
// Project: Lexichord.Benchmarks
// Description: Performance benchmarks for memory allocation patterns.
// =============================================================================
// v0.4.8c: Measures memory usage for document metadata and index status.
//   - Document record allocations
//   - Index status allocations
//   - Scalability at 100, 500, 1000 documents
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Benchmarks.Memory;

/// <summary>
/// Memory allocation benchmarks for Document records.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure memory usage when creating document metadata.
/// Target baseline: 1000 documents &lt; 10MB allocated.
/// </para>
/// </remarks>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class MemoryUsageBenchmarks
{
    /// <summary>
    /// Number of documents to allocate in the benchmark.
    /// </summary>
    [Params(100, 500, 1000)]
    public int DocumentCount { get; set; }

    /// <summary>
    /// Benchmark allocating Document records with typical metadata.
    /// </summary>
    [Benchmark]
    public List<Document> DocumentMetadataAllocation()
    {
        // LOGIC: Allocate documents to measure per-document memory overhead.
        var docs = new List<Document>(DocumentCount);

        for (int i = 0; i < DocumentCount; i++)
        {
            docs.Add(new Document(
                Id: Guid.NewGuid(),
                ProjectId: Guid.Empty,
                FilePath: $"/workspace/documents/document_{i}.md",
                Title: $"Document Title {i}",
                Hash: Guid.NewGuid().ToString("N"),
                Status: DocumentStatus.Indexed,
                IndexedAt: DateTime.UtcNow,
                FailureReason: null));
        }

        return docs;
    }

    /// <summary>
    /// Benchmark allocating TextChunk records with embeddings.
    /// </summary>
    [Benchmark]
    public List<Chunk> ChunkWithEmbeddingAllocation()
    {
        // LOGIC: Embeddings dominate memory for chunks (1536 floats = 6KB per chunk).
        var chunks = new List<Chunk>(DocumentCount);

        for (int i = 0; i < DocumentCount; i++)
        {
            var embedding = new float[1536];
            chunks.Add(new Chunk(
                Id: Guid.NewGuid(),
                DocumentId: Guid.NewGuid(),
                Content: $"This is the content of chunk {i} in the document.",
                Embedding: embedding,
                ChunkIndex: i,
                StartOffset: i * 100,
                EndOffset: (i + 1) * 100));
        }

        return chunks;
    }

    /// <summary>
    /// Benchmark creating document file paths (string allocation).
    /// </summary>
    [Benchmark]
    public List<string> FilePathAllocation()
    {
        // LOGIC: File paths are a significant part of Document record size.
        var paths = new List<string>(DocumentCount);

        for (int i = 0; i < DocumentCount; i++)
        {
            paths.Add($"/Users/developer/projects/lexichord/workspace/documents/subfolder/document_{i:D5}.md");
        }

        return paths;
    }
}
