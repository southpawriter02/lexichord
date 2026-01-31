# LCS-DES-048c: Performance Benchmarks

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-048c                             |
| **Version**      | v0.4.8c                                  |
| **Title**        | Performance Benchmarks                   |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Benchmarks`                   |
| **License Tier** | N/A (Development)                        |

---

## 1. Overview

### 1.1 Purpose

This specification defines the performance benchmark suite for the RAG module using BenchmarkDotNet. The benchmarks establish baseline metrics for chunking, token counting, vector search, and indexing throughput.

### 1.2 Goals

- Establish chunking performance baseline (< 100ms for 100KB)
- Establish search latency baseline (< 200ms for 10K chunks)
- Establish indexing throughput baseline (> 10 docs/minute)
- Measure memory usage for document metadata
- Generate reproducible benchmark reports
- Identify performance bottlenecks

### 1.3 Non-Goals

- Load testing under production conditions
- Network latency benchmarks (API-dependent)
- UI rendering performance

---

## 2. Benchmark Project Structure

### 2.1 Project Configuration

```xml
<!-- benchmarks/Lexichord.Benchmarks/Lexichord.Benchmarks.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.x" />
    <PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.14.x"
                      Condition="'$(OS)' == 'Windows_NT'" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Lexichord.Modules.RAG\Lexichord.Modules.RAG.csproj" />
  </ItemGroup>
</Project>
```

### 2.2 Entry Point

```csharp
// benchmarks/Lexichord.Benchmarks/Program.cs
using BenchmarkDotNet.Running;
using Lexichord.Benchmarks;

var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

### 2.3 Directory Structure

```
benchmarks/Lexichord.Benchmarks/
├── Program.cs
├── Chunking/
│   └── ChunkingBenchmarks.cs
├── Embedding/
│   └── TokenCounterBenchmarks.cs
├── Search/
│   └── VectorSearchBenchmarks.cs
├── Pipeline/
│   └── IndexingBenchmarks.cs
└── Data/
    └── SampleDocuments/
        ├── small_1kb.md
        ├── medium_10kb.md
        ├── large_100kb.md
        └── corpus_10k_chunks.json
```

---

## 3. Benchmark Specifications

### 3.1 Chunking Benchmarks

```csharp
namespace Lexichord.Benchmarks.Chunking;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

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

    [GlobalSetup]
    public void Setup()
    {
        _smallDoc = GenerateMarkdownDocument(1024);
        _mediumDoc = GenerateMarkdownDocument(10 * 1024);
        _largeDoc = GenerateMarkdownDocument(100 * 1024);

        _fixedSizeStrategy = new FixedSizeChunkingStrategy();
        _paragraphStrategy = new ParagraphChunkingStrategy();
        _markdownStrategy = new MarkdownHeaderChunkingStrategy();

        _options = new ChunkingOptions
        {
            MaxChunkSize = 1000,
            Overlap = 100,
            MinChunkSize = 200
        };
    }

    // Fixed Size Strategy Benchmarks
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FixedSize")]
    public IReadOnlyList<TextChunk> FixedSize_1KB() =>
        _fixedSizeStrategy.Split(_smallDoc, _options);

    [Benchmark]
    [BenchmarkCategory("FixedSize")]
    public IReadOnlyList<TextChunk> FixedSize_10KB() =>
        _fixedSizeStrategy.Split(_mediumDoc, _options);

    [Benchmark]
    [BenchmarkCategory("FixedSize")]
    public IReadOnlyList<TextChunk> FixedSize_100KB() =>
        _fixedSizeStrategy.Split(_largeDoc, _options);

    // Paragraph Strategy Benchmarks
    [Benchmark]
    [BenchmarkCategory("Paragraph")]
    public IReadOnlyList<TextChunk> Paragraph_1KB() =>
        _paragraphStrategy.Split(_smallDoc, _options);

    [Benchmark]
    [BenchmarkCategory("Paragraph")]
    public IReadOnlyList<TextChunk> Paragraph_10KB() =>
        _paragraphStrategy.Split(_mediumDoc, _options);

    [Benchmark]
    [BenchmarkCategory("Paragraph")]
    public IReadOnlyList<TextChunk> Paragraph_100KB() =>
        _paragraphStrategy.Split(_largeDoc, _options);

    // Markdown Strategy Benchmarks
    [Benchmark]
    [BenchmarkCategory("Markdown")]
    public IReadOnlyList<TextChunk> Markdown_1KB() =>
        _markdownStrategy.Split(_smallDoc, _options);

    [Benchmark]
    [BenchmarkCategory("Markdown")]
    public IReadOnlyList<TextChunk> Markdown_10KB() =>
        _markdownStrategy.Split(_mediumDoc, _options);

    [Benchmark]
    [BenchmarkCategory("Markdown")]
    public IReadOnlyList<TextChunk> Markdown_100KB() =>
        _markdownStrategy.Split(_largeDoc, _options);

    private static string GenerateMarkdownDocument(int targetBytes)
    {
        var sb = new StringBuilder();
        var sectionCount = 0;
        var random = new Random(42); // Deterministic

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

    private static string GenerateSentence(Random random)
    {
        var words = new[] { "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing" };
        var length = random.Next(5, 15);
        return string.Join(" ", Enumerable.Range(0, length).Select(_ => words[random.Next(words.Length)]));
    }
}
```

### 3.2 Token Counter Benchmarks

```csharp
namespace Lexichord.Benchmarks.Embedding;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class TokenCounterBenchmarks
{
    private TiktokenTokenCounter _counter = null!;
    private string _shortText = null!;
    private string _mediumText = null!;
    private string _longText = null!;

    [GlobalSetup]
    public void Setup()
    {
        _counter = new TiktokenTokenCounter();
        _shortText = "Hello world, this is a test.";
        _mediumText = string.Join(" ", Enumerable.Repeat("word", 1000));
        _longText = string.Join(" ", Enumerable.Repeat("word", 10000));
    }

    [Benchmark(Baseline = true)]
    public int CountTokens_Short() => _counter.CountTokens(_shortText);

    [Benchmark]
    public int CountTokens_1000Words() => _counter.CountTokens(_mediumText);

    [Benchmark]
    public int CountTokens_10000Words() => _counter.CountTokens(_longText);

    [Benchmark]
    public IReadOnlyList<int> Encode_Short() => _counter.Encode(_shortText);

    [Benchmark]
    public IReadOnlyList<int> Encode_1000Words() => _counter.Encode(_mediumText);

    [Benchmark]
    public (string, bool) Truncate_LongText() =>
        _counter.TruncateToTokenLimit(_longText, 1000);
}
```

### 3.3 Vector Search Benchmarks

```csharp
namespace Lexichord.Benchmarks.Search;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class VectorSearchBenchmarks
{
    private float[][] _corpus = null!;
    private float[] _queryVector = null!;
    private const int VectorDimensions = 1536;

    [Params(100, 1000, 10000)]
    public int CorpusSize { get; set; }

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

    [Benchmark]
    public List<(int Index, float Score)> CosineSimilarity_TopK()
    {
        var results = new List<(int, float)>(CorpusSize);

        for (int i = 0; i < CorpusSize; i++)
        {
            var score = ComputeCosineSimilarity(_queryVector, _corpus[i]);
            results.Add((i, score));
        }

        return results.OrderByDescending(r => r.Item2).Take(10).ToList();
    }

    [Benchmark]
    public List<(int Index, float Score)> CosineSimilarity_WithThreshold()
    {
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
```

### 3.4 Memory Usage Benchmarks

```csharp
namespace Lexichord.Benchmarks.Memory;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class MemoryUsageBenchmarks
{
    [Params(100, 500, 1000)]
    public int DocumentCount { get; set; }

    [Benchmark]
    public List<Document> DocumentMetadataAllocation()
    {
        var docs = new List<Document>(DocumentCount);

        for (int i = 0; i < DocumentCount; i++)
        {
            docs.Add(new Document
            {
                Id = Guid.NewGuid(),
                FilePath = $"/workspace/documents/document_{i}.md",
                FileHash = Guid.NewGuid().ToString("N"),
                Title = $"Document Title {i}",
                IndexedAt = DateTimeOffset.UtcNow,
                ChunkCount = 10
            });
        }

        return docs;
    }

    [Benchmark]
    public List<IndexedDocumentInfo> IndexStatusAllocation()
    {
        var infos = new List<IndexedDocumentInfo>(DocumentCount);

        for (int i = 0; i < DocumentCount; i++)
        {
            infos.Add(new IndexedDocumentInfo
            {
                Id = Guid.NewGuid(),
                FilePath = $"/workspace/documents/document_{i}.md",
                Status = IndexingStatus.Indexed,
                ChunkCount = 10,
                IndexedAt = DateTimeOffset.UtcNow,
                EstimatedSizeBytes = 80 * 1024
            });
        }

        return infos;
    }
}
```

---

## 4. Expected Baselines

### 4.1 Target Metrics

| Benchmark | Target | Notes |
| :-------- | :----- | :---- |
| FixedSize_100KB | < 100ms | Primary chunking strategy |
| Paragraph_100KB | < 150ms | More parsing overhead |
| Markdown_100KB | < 200ms | Markdig parsing |
| CountTokens_10000Words | < 50ms | tiktoken encoding |
| CosineSimilarity_TopK (10K) | < 200ms | In-memory comparison |
| DocumentMetadata (1000) | < 10MB | Metadata only |

### 4.2 Sample Results Format

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.2428/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.100

| Method           | CorpusSize | Mean      | Error    | StdDev   | Allocated |
|----------------- |----------- |----------:|---------:|---------:|----------:|
| FixedSize_1KB    | N/A        |   0.45 ms | 0.008 ms | 0.007 ms |    2.1 KB |
| FixedSize_10KB   | N/A        |   3.21 ms | 0.012 ms | 0.011 ms |   18.4 KB |
| FixedSize_100KB  | N/A        |  28.67 ms | 0.134 ms | 0.125 ms |  178.2 KB |
| CosineSimilarity | 100        |   0.12 ms | 0.002 ms | 0.002 ms |    1.2 KB |
| CosineSimilarity | 1000       |   1.18 ms | 0.008 ms | 0.007 ms |    1.2 KB |
| CosineSimilarity | 10000      |  11.82 ms | 0.045 ms | 0.042 ms |    1.2 KB |
```

---

## 5. Running Benchmarks

### 5.1 Command Line

```bash
# Run all benchmarks
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks

# Run specific category
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --filter "*Chunking*"

# Run with memory diagnoser
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --memory

# Export results
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --exporters json html
```

### 5.2 CI Integration

```yaml
# .github/workflows/benchmarks.yml
name: Performance Benchmarks

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Run benchmarks
        run: |
          dotnet run -c Release --project benchmarks/Lexichord.Benchmarks \
            -- --filter "*" --exporters json

      - name: Upload results
        uses: actions/upload-artifact@v4
        with:
          name: benchmark-results
          path: BenchmarkDotNet.Artifacts/
```

---

## 6. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Information | "Benchmark complete: {Name} = {Mean}ms (±{StdDev})" | After each benchmark |
| Information | "Memory: {Allocated} allocated" | With memory diagnoser |

---

## 7. File Locations

| File | Path |
| :--- | :--- |
| Project | `benchmarks/Lexichord.Benchmarks/Lexichord.Benchmarks.csproj` |
| Chunking benchmarks | `benchmarks/Lexichord.Benchmarks/Chunking/ChunkingBenchmarks.cs` |
| Token benchmarks | `benchmarks/Lexichord.Benchmarks/Embedding/TokenCounterBenchmarks.cs` |
| Search benchmarks | `benchmarks/Lexichord.Benchmarks/Search/VectorSearchBenchmarks.cs` |
| Results | `BenchmarkDotNet.Artifacts/results/` |

---

## 8. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Chunking 100KB < 100ms | [ ] |
| 2 | Token counting 10K words < 50ms | [ ] |
| 3 | Vector search 10K chunks < 200ms | [ ] |
| 4 | Memory for 1K docs < 50MB | [ ] |
| 5 | Benchmark reports generated | [ ] |
| 6 | CI integration configured | [ ] |
| 7 | Baselines documented | [ ] |

---

## 9. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
