# LCS-DES-038d: Design Specification — Benchmark Baseline

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `TST-038d` | Sub-part of TST-038 |
| **Feature Name** | `Performance Benchmark Baseline` | Performance verification tests |
| **Target Version** | `v0.3.8d` | Fourth sub-part of v0.3.8 |
| **Module Scope** | `Lexichord.Tests.Performance` | Performance test project |
| **Swimlane** | `Governance` | Part of Style vertical |
| **License Tier** | `Core` | Testing available to all |
| **Feature Gate Key** | N/A | No gating for tests |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-26` | |
| **Parent Document** | [LCS-DES-038-INDEX](./LCS-DES-038-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-038 §3.4](./LCS-SBD-038.md#34-v038d-benchmark-baseline) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord's v0.3.x analysis features must maintain acceptable performance as the codebase evolves. Without performance baselines:

- Unintentional performance regressions could degrade user experience
- No objective data exists to guide optimization efforts
- CI/CD cannot prevent slow code from reaching production
- Memory usage patterns remain unknown

> **Goal:** Establish that analyzing 10,000 words completes in less than 200ms for readability, with CI builds failing if performance regresses by more than 10%.

### 2.2 The Proposed Solution

Implement a comprehensive performance testing infrastructure that:

1. Uses BenchmarkDotNet for accurate profiling with statistical analysis
2. Creates CI-integrated threshold tests that fail builds on regression
3. Tracks memory allocation alongside execution time
4. Establishes documented baselines for all analysis operations
5. Provides throughput metrics (words/second) for capacity planning
6. Stores baseline data in JSON for version-controlled comparison

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IReadabilityService` | v0.3.3c | Readability analysis performance |
| `IFuzzyScanner` | v0.3.1c | Fuzzy scanning performance |
| `IPassiveVoiceDetector` | v0.3.4b | Passive voice detection performance |
| `IVoiceScanner` | v0.3.4c | Weasel word scanning performance |
| `ILintingOrchestrator` | v0.2.3a | Full pipeline performance |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `xunit` | 2.9.x | Test framework |
| `FluentAssertions` | 6.x | Fluent assertions |
| `BenchmarkDotNet` | 0.14.x | Performance benchmarking (NEW) |
| `BenchmarkDotNet.Diagnostics.Windows` | 0.14.x | Windows diagnostics (optional) |

### 3.2 Licensing Behavior

No licensing required. Tests run in development/CI environments only.

---

## 4. Data Contract (The API)

### 4.1 Baseline Configuration

```csharp
namespace Lexichord.Tests.Performance;

/// <summary>
/// Performance baseline configuration.
/// Defines acceptable performance thresholds for CI enforcement.
/// </summary>
public sealed class PerformanceBaseline
{
    /// <summary>
    /// Maximum acceptable time in milliseconds for each operation at given word counts.
    /// </summary>
    public required Dictionary<string, OperationThresholds> Operations { get; init; }

    /// <summary>
    /// Percentage threshold for regression detection (e.g., 1.10 = 10% slower triggers failure).
    /// </summary>
    public double RegressionThreshold { get; init; } = 1.10;

    /// <summary>
    /// Date when baseline was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Hardware/configuration notes for the baseline.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Thresholds for a single operation at various input sizes.
/// </summary>
public sealed class OperationThresholds
{
    /// <summary>Threshold for 1,000 words in milliseconds.</summary>
    public int Words1K { get; init; }

    /// <summary>Threshold for 10,000 words in milliseconds.</summary>
    public int Words10K { get; init; }

    /// <summary>Threshold for 50,000 words in milliseconds.</summary>
    public int Words50K { get; init; }

    /// <summary>Maximum memory allocation in megabytes for 10K words.</summary>
    public int MaxMemoryMB { get; init; }
}
```

### 4.2 Baseline JSON Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "PerformanceBaseline",
  "type": "object",
  "properties": {
    "regressionThreshold": {
      "type": "number",
      "description": "Percentage threshold for regression (1.10 = 10%)"
    },
    "lastUpdated": {
      "type": "string",
      "format": "date-time"
    },
    "notes": {
      "type": "string"
    },
    "operations": {
      "type": "object",
      "additionalProperties": {
        "type": "object",
        "properties": {
          "words1K": { "type": "integer" },
          "words10K": { "type": "integer" },
          "words50K": { "type": "integer" },
          "maxMemoryMB": { "type": "integer" }
        }
      }
    }
  }
}
```

### 4.3 Default Baseline Values

```json
{
  "regressionThreshold": 1.10,
  "lastUpdated": "2026-01-26T00:00:00Z",
  "notes": "Initial baseline - Developer workstation (Apple M2, 16GB RAM)",
  "operations": {
    "readability": {
      "words1K": 20,
      "words10K": 200,
      "words50K": 1000,
      "maxMemoryMB": 50
    },
    "fuzzyScanning": {
      "words1K": 30,
      "words10K": 300,
      "words50K": 1500,
      "maxMemoryMB": 75
    },
    "passiveVoice": {
      "words1K": 10,
      "words10K": 100,
      "words50K": 500,
      "maxMemoryMB": 25
    },
    "weaselWords": {
      "words1K": 10,
      "words10K": 100,
      "words50K": 500,
      "maxMemoryMB": 25
    },
    "fullPipeline": {
      "words1K": 50,
      "words10K": 500,
      "words50K": 2500,
      "maxMemoryMB": 100
    }
  }
}
```

---

## 5. Implementation Logic

### 5.1 Test Text Generation

```csharp
namespace Lexichord.Tests.Performance;

/// <summary>
/// Generates reproducible test text of specified word counts.
/// Uses a seeded random generator for consistent benchmarks.
/// </summary>
public static class LoremIpsumGenerator
{
    private static readonly string[] Words = new[]
    {
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur",
        "adipiscing", "elit", "sed", "do", "eiusmod", "tempor",
        "incididunt", "ut", "labore", "et", "dolore", "magna",
        "aliqua", "enim", "ad", "minim", "veniam", "quis",
        "nostrud", "exercitation", "ullamco", "laboris", "nisi",
        "aliquip", "ex", "ea", "commodo", "consequat", "duis",
        "aute", "irure", "in", "reprehenderit", "voluptate",
        "velit", "esse", "cillum", "fugiat", "nulla", "pariatur",
        "excepteur", "sint", "occaecat", "cupidatat", "non",
        "proident", "sunt", "culpa", "qui", "officia", "deserunt",
        "mollit", "anim", "id", "est", "laborum"
    };

    private static readonly string[] Sentences = new[]
    {
        "The quick brown fox jumps over the lazy dog.",
        "Pack my box with five dozen liquor jugs.",
        "How vexingly quick daft zebras jump.",
        "The five boxing wizards jump quickly.",
        "Sphinx of black quartz, judge my vow."
    };

    /// <summary>
    /// Generates text with approximately the specified word count.
    /// Uses a seeded random generator for reproducibility.
    /// </summary>
    public static string Generate(int wordCount, int seed = 42)
    {
        var random = new Random(seed);
        var sb = new StringBuilder();
        var wordsAdded = 0;
        var sentenceLength = 0;

        while (wordsAdded < wordCount)
        {
            var word = Words[random.Next(Words.Length)];

            if (sentenceLength == 0)
            {
                word = char.ToUpper(word[0]) + word[1..];
            }

            sb.Append(word);
            wordsAdded++;
            sentenceLength++;

            if (sentenceLength >= random.Next(8, 15) || wordsAdded >= wordCount)
            {
                sb.Append(". ");
                sentenceLength = 0;
            }
            else
            {
                sb.Append(' ');
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates text with passive voice constructions for testing.
    /// </summary>
    public static string GenerateWithPassive(int wordCount, double passiveRatio = 0.2)
    {
        var random = new Random(42);
        var sb = new StringBuilder();
        var wordsAdded = 0;

        while (wordsAdded < wordCount)
        {
            string sentence;
            if (random.NextDouble() < passiveRatio)
            {
                sentence = GeneratePassiveSentence(random);
            }
            else
            {
                sentence = GenerateActiveSentence(random);
            }

            sb.Append(sentence).Append(' ');
            wordsAdded += sentence.Split(' ').Length;
        }

        return sb.ToString().TrimEnd();
    }

    private static string GeneratePassiveSentence(Random random)
    {
        var passiveTemplates = new[]
        {
            "The code was written by the developer.",
            "The report was submitted yesterday.",
            "The feature was implemented last week.",
            "Tests were run by the CI system.",
            "Bugs were fixed in the release."
        };
        return passiveTemplates[random.Next(passiveTemplates.Length)];
    }

    private static string GenerateActiveSentence(Random random)
    {
        return Sentences[random.Next(Sentences.Length)];
    }
}
```

### 5.2 Threshold Decision Logic

```text
SHOULD CI FAIL FOR PERFORMANCE?
│
├── Measure actual execution time
├── Load baseline threshold for operation
│
├── Calculate ratio: actual / baseline
│   │
│   ├── ratio <= 1.0 (faster or equal)
│   │   └── PASS (performance improved or maintained)
│   │
│   ├── ratio > 1.0 AND ratio <= 1.10 (up to 10% slower)
│   │   └── PASS with WARNING (minor regression)
│   │
│   └── ratio > 1.10 (more than 10% slower)
│       └── FAIL BUILD (significant regression)
│
└── Also check memory allocation thresholds
```

---

## 6. Test Scenarios

### 6.1 BenchmarkDotNet Benchmarks

```csharp
namespace Lexichord.Tests.Performance;

/// <summary>
/// Performance benchmarks using BenchmarkDotNet.
/// Run with: dotnet run -c Release -- --filter "*"
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RPlotExporter]
[HtmlExporter]
public class AnalysisBenchmarks
{
    private string _text1K = null!;
    private string _text10K = null!;
    private string _text50K = null!;

    private IReadabilityService _readabilityService = null!;
    private IFuzzyScanner _fuzzyScanner = null!;
    private IPassiveVoiceDetector _passiveDetector = null!;
    private IVoiceScanner _weaselScanner = null!;
    private ILintingOrchestrator _orchestrator = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test texts
        _text1K = LoremIpsumGenerator.Generate(1_000);
        _text10K = LoremIpsumGenerator.Generate(10_000);
        _text50K = LoremIpsumGenerator.Generate(50_000);

        // Initialize services with production configuration
        var serviceProvider = BenchmarkServiceProvider.Create();
        _readabilityService = serviceProvider.GetRequiredService<IReadabilityService>();
        _fuzzyScanner = serviceProvider.GetRequiredService<IFuzzyScanner>();
        _passiveDetector = serviceProvider.GetRequiredService<IPassiveVoiceDetector>();
        _weaselScanner = serviceProvider.GetRequiredService<IVoiceScanner>();
        _orchestrator = serviceProvider.GetRequiredService<ILintingOrchestrator>();
    }

    #region Readability Benchmarks

    [Benchmark]
    [BenchmarkCategory("Readability")]
    public ReadabilityMetrics Readability_1K()
        => _readabilityService.Analyze(_text1K);

    [Benchmark]
    [BenchmarkCategory("Readability")]
    public ReadabilityMetrics Readability_10K()
        => _readabilityService.Analyze(_text10K);

    [Benchmark]
    [BenchmarkCategory("Readability")]
    public ReadabilityMetrics Readability_50K()
        => _readabilityService.Analyze(_text50K);

    #endregion

    #region Fuzzy Scanning Benchmarks

    [Benchmark]
    [BenchmarkCategory("FuzzyScanning")]
    public IReadOnlyList<FuzzyMatch> FuzzyScanning_1K()
        => _fuzzyScanner.Scan(_text1K);

    [Benchmark]
    [BenchmarkCategory("FuzzyScanning")]
    public IReadOnlyList<FuzzyMatch> FuzzyScanning_10K()
        => _fuzzyScanner.Scan(_text10K);

    [Benchmark]
    [BenchmarkCategory("FuzzyScanning")]
    public IReadOnlyList<FuzzyMatch> FuzzyScanning_50K()
        => _fuzzyScanner.Scan(_text50K);

    #endregion

    #region Passive Voice Benchmarks

    [Benchmark]
    [BenchmarkCategory("PassiveVoice")]
    public IReadOnlyList<PassiveVoiceMatch> PassiveVoice_1K()
        => _passiveDetector.Analyze(_text1K);

    [Benchmark]
    [BenchmarkCategory("PassiveVoice")]
    public IReadOnlyList<PassiveVoiceMatch> PassiveVoice_10K()
        => _passiveDetector.Analyze(_text10K);

    [Benchmark]
    [BenchmarkCategory("PassiveVoice")]
    public IReadOnlyList<PassiveVoiceMatch> PassiveVoice_50K()
        => _passiveDetector.Analyze(_text50K);

    #endregion

    #region Weasel Word Benchmarks

    [Benchmark]
    [BenchmarkCategory("WeaselWords")]
    public IReadOnlyList<WeaselWordMatch> WeaselWords_1K()
        => _weaselScanner.Scan(_text1K);

    [Benchmark]
    [BenchmarkCategory("WeaselWords")]
    public IReadOnlyList<WeaselWordMatch> WeaselWords_10K()
        => _weaselScanner.Scan(_text10K);

    [Benchmark]
    [BenchmarkCategory("WeaselWords")]
    public IReadOnlyList<WeaselWordMatch> WeaselWords_50K()
        => _weaselScanner.Scan(_text50K);

    #endregion

    #region Full Pipeline Benchmarks

    [Benchmark]
    [BenchmarkCategory("FullPipeline")]
    public AnalysisResult FullPipeline_1K()
        => _orchestrator.AnalyzeAsync(_text1K).GetAwaiter().GetResult();

    [Benchmark]
    [BenchmarkCategory("FullPipeline")]
    public AnalysisResult FullPipeline_10K()
        => _orchestrator.AnalyzeAsync(_text10K).GetAwaiter().GetResult();

    [Benchmark]
    [BenchmarkCategory("FullPipeline")]
    public AnalysisResult FullPipeline_50K()
        => _orchestrator.AnalyzeAsync(_text50K).GetAwaiter().GetResult();

    #endregion
}
```

### 6.2 CI Threshold Tests

```csharp
[Trait("Category", "Performance")]
[Trait("Version", "v0.3.8d")]
public class PerformanceThresholdTests
{
    private readonly IReadabilityService _readabilityService;
    private readonly IFuzzyScanner _fuzzyScanner;
    private readonly IPassiveVoiceDetector _passiveDetector;
    private readonly IVoiceScanner _weaselScanner;
    private readonly ILintingOrchestrator _orchestrator;
    private readonly PerformanceBaseline _baseline;

    public PerformanceThresholdTests()
    {
        var serviceProvider = TestServiceProvider.Create();
        _readabilityService = serviceProvider.GetRequiredService<IReadabilityService>();
        _fuzzyScanner = serviceProvider.GetRequiredService<IFuzzyScanner>();
        _passiveDetector = serviceProvider.GetRequiredService<IPassiveVoiceDetector>();
        _weaselScanner = serviceProvider.GetRequiredService<IVoiceScanner>();
        _orchestrator = serviceProvider.GetRequiredService<ILintingOrchestrator>();
        _baseline = PerformanceBaselineLoader.Load();
    }

    #region Readability Performance

    [Fact]
    public void Readability_10000Words_CompletesUnder200ms()
    {
        // Arrange
        var text = LoremIpsumGenerator.Generate(10_000);
        var threshold = _baseline.Operations["readability"].Words10K;
        var sw = Stopwatch.StartNew();

        // Act
        var result = _readabilityService.Analyze(text);

        // Assert
        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"10,000 word readability analysis must complete in < {threshold}ms");

        // Verify result is valid
        result.WordCount.Should().BeGreaterThan(9_500,
            because: "tokenization should preserve most words");
    }

    [Fact]
    public void Readability_1000Words_CompletesUnder20ms()
    {
        var text = LoremIpsumGenerator.Generate(1_000);
        var threshold = _baseline.Operations["readability"].Words1K;
        var sw = Stopwatch.StartNew();

        var result = _readabilityService.Analyze(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"1,000 word readability analysis must complete in < {threshold}ms");
    }

    [Fact]
    public void Readability_50000Words_CompletesUnder1000ms()
    {
        var text = LoremIpsumGenerator.Generate(50_000);
        var threshold = _baseline.Operations["readability"].Words50K;
        var sw = Stopwatch.StartNew();

        var result = _readabilityService.Analyze(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"50,000 word readability analysis must complete in < {threshold}ms");
    }

    #endregion

    #region Fuzzy Scanning Performance

    [Fact]
    public void FuzzyScanning_10000Words_CompletesUnder300ms()
    {
        var text = LoremIpsumGenerator.Generate(10_000);
        var threshold = _baseline.Operations["fuzzyScanning"].Words10K;
        var sw = Stopwatch.StartNew();

        var result = _fuzzyScanner.Scan(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"10,000 word fuzzy scanning must complete in < {threshold}ms");
    }

    [Fact]
    public void FuzzyScanning_1000Words_CompletesUnder30ms()
    {
        var text = LoremIpsumGenerator.Generate(1_000);
        var threshold = _baseline.Operations["fuzzyScanning"].Words1K;
        var sw = Stopwatch.StartNew();

        var result = _fuzzyScanner.Scan(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"1,000 word fuzzy scanning must complete in < {threshold}ms");
    }

    #endregion

    #region Passive Voice Performance

    [Fact]
    public void PassiveVoice_10000Words_CompletesUnder100ms()
    {
        var text = LoremIpsumGenerator.GenerateWithPassive(10_000);
        var threshold = _baseline.Operations["passiveVoice"].Words10K;
        var sw = Stopwatch.StartNew();

        var result = _passiveDetector.Analyze(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"10,000 word passive voice detection must complete in < {threshold}ms");
    }

    [Fact]
    public void PassiveVoice_1000Words_CompletesUnder10ms()
    {
        var text = LoremIpsumGenerator.GenerateWithPassive(1_000);
        var threshold = _baseline.Operations["passiveVoice"].Words1K;
        var sw = Stopwatch.StartNew();

        var result = _passiveDetector.Analyze(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"1,000 word passive voice detection must complete in < {threshold}ms");
    }

    #endregion

    #region Full Pipeline Performance

    [Fact]
    public async Task FullPipeline_10000Words_CompletesUnder500ms()
    {
        var text = LoremIpsumGenerator.Generate(10_000);
        var threshold = _baseline.Operations["fullPipeline"].Words10K;
        var sw = Stopwatch.StartNew();

        var result = await _orchestrator.AnalyzeAsync(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"10,000 word full pipeline must complete in < {threshold}ms");

        // Verify all analysis components ran
        result.ReadabilityMetrics.Should().NotBeNull();
        result.VoiceMetrics.Should().NotBeNull();
    }

    [Fact]
    public async Task FullPipeline_1000Words_CompletesUnder50ms()
    {
        var text = LoremIpsumGenerator.Generate(1_000);
        var threshold = _baseline.Operations["fullPipeline"].Words1K;
        var sw = Stopwatch.StartNew();

        var result = await _orchestrator.AnalyzeAsync(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"1,000 word full pipeline must complete in < {threshold}ms");
    }

    [Fact]
    public async Task FullPipeline_50000Words_CompletesUnder2500ms()
    {
        var text = LoremIpsumGenerator.Generate(50_000);
        var threshold = _baseline.Operations["fullPipeline"].Words50K;
        var sw = Stopwatch.StartNew();

        var result = await _orchestrator.AnalyzeAsync(text);

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeLessThan(threshold,
            because: $"50,000 word full pipeline must complete in < {threshold}ms");
    }

    #endregion

    #region Memory Allocation Tests

    [Fact]
    public void MemoryAllocation_10000Words_Under50MB()
    {
        var text = LoremIpsumGenerator.Generate(10_000);
        var threshold = _baseline.Operations["fullPipeline"].MaxMemoryMB;

        // Force GC to get clean baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(true);

        // Run full pipeline
        var result = _orchestrator.AnalyzeAsync(text).GetAwaiter().GetResult();

        // Measure allocation
        var finalMemory = GC.GetTotalMemory(false);
        var allocatedMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        allocatedMB.Should().BeLessThan(threshold,
            because: $"10,000 word analysis should allocate < {threshold}MB");
    }

    [Fact]
    public void MemoryAllocation_50000Words_Under250MB()
    {
        var text = LoremIpsumGenerator.Generate(50_000);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(true);

        var result = _orchestrator.AnalyzeAsync(text).GetAwaiter().GetResult();

        var finalMemory = GC.GetTotalMemory(false);
        var allocatedMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        allocatedMB.Should().BeLessThan(250,
            because: "50,000 word analysis should allocate < 250MB");
    }

    #endregion

    #region Throughput Tests

    [Fact]
    public void Throughput_Readability_AtLeast50000WordsPerSecond()
    {
        var text = LoremIpsumGenerator.Generate(10_000);
        var iterations = 10;
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            _readabilityService.Analyze(text);
        }

        sw.Stop();
        var totalWords = 10_000 * iterations;
        var wordsPerSecond = totalWords / (sw.ElapsedMilliseconds / 1000.0);

        wordsPerSecond.Should().BeGreaterThan(50_000,
            because: "readability analysis should process at least 50,000 words/second");
    }

    #endregion

    #region Regression Detection

    [Theory]
    [InlineData(1_000)]
    [InlineData(10_000)]
    public void Performance_NotRegressed_FromBaseline(int wordCount)
    {
        var text = LoremIpsumGenerator.Generate(wordCount);
        var thresholdKey = wordCount switch
        {
            1_000 => "Words1K",
            10_000 => "Words10K",
            50_000 => "Words50K",
            _ => throw new ArgumentException($"Unsupported word count: {wordCount}")
        };

        var operations = new Dictionary<string, Func<object>>
        {
            ["readability"] = () => _readabilityService.Analyze(text),
            ["fuzzyScanning"] = () => _fuzzyScanner.Scan(text),
            ["passiveVoice"] = () => _passiveDetector.Analyze(text),
        };

        foreach (var (opName, operation) in operations)
        {
            var threshold = GetThreshold(_baseline.Operations[opName], thresholdKey);
            var allowedMax = (int)(threshold * _baseline.RegressionThreshold);

            var sw = Stopwatch.StartNew();
            var _ = operation();
            sw.Stop();

            sw.ElapsedMilliseconds.Should().BeLessThan(allowedMax,
                because: $"{opName} at {wordCount} words should not regress > 10% from baseline ({threshold}ms)");
        }
    }

    private static int GetThreshold(OperationThresholds thresholds, string key) =>
        key switch
        {
            "Words1K" => thresholds.Words1K,
            "Words10K" => thresholds.Words10K,
            "Words50K" => thresholds.Words50K,
            _ => throw new ArgumentException($"Unknown threshold key: {key}")
        };

    #endregion
}
```

### 6.3 Baseline Loader

```csharp
namespace Lexichord.Tests.Performance;

/// <summary>
/// Loads performance baseline from JSON file.
/// </summary>
public static class PerformanceBaselineLoader
{
    private const string BaselineFileName = "baseline.json";

    /// <summary>
    /// Loads the performance baseline from the Baselines directory.
    /// Falls back to default values if file not found.
    /// </summary>
    public static PerformanceBaseline Load()
    {
        var baselineDir = FindBaselinesDirectory();
        var baselinePath = Path.Combine(baselineDir, BaselineFileName);

        if (File.Exists(baselinePath))
        {
            var json = File.ReadAllText(baselinePath);
            return JsonSerializer.Deserialize<PerformanceBaseline>(json)
                ?? GetDefaultBaseline();
        }

        return GetDefaultBaseline();
    }

    /// <summary>
    /// Saves a new baseline to disk.
    /// </summary>
    public static void Save(PerformanceBaseline baseline)
    {
        var baselineDir = FindBaselinesDirectory();
        var baselinePath = Path.Combine(baselineDir, BaselineFileName);

        var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(baselinePath, json);
    }

    private static string FindBaselinesDirectory()
    {
        // Search up from current directory for Baselines folder
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var baselinesPath = Path.Combine(dir.FullName, "Baselines");
            if (Directory.Exists(baselinesPath))
                return baselinesPath;

            dir = dir.Parent;
        }

        // Fallback to current directory
        return Directory.GetCurrentDirectory();
    }

    private static PerformanceBaseline GetDefaultBaseline() => new()
    {
        RegressionThreshold = 1.10,
        LastUpdated = DateTimeOffset.UtcNow,
        Notes = "Default baseline values",
        Operations = new Dictionary<string, OperationThresholds>
        {
            ["readability"] = new()
            {
                Words1K = 20,
                Words10K = 200,
                Words50K = 1000,
                MaxMemoryMB = 50
            },
            ["fuzzyScanning"] = new()
            {
                Words1K = 30,
                Words10K = 300,
                Words50K = 1500,
                MaxMemoryMB = 75
            },
            ["passiveVoice"] = new()
            {
                Words1K = 10,
                Words10K = 100,
                Words50K = 500,
                MaxMemoryMB = 25
            },
            ["weaselWords"] = new()
            {
                Words1K = 10,
                Words10K = 100,
                Words50K = 500,
                MaxMemoryMB = 25
            },
            ["fullPipeline"] = new()
            {
                Words1K = 50,
                Words10K = 500,
                Words50K = 2500,
                MaxMemoryMB = 100
            }
        }
    };
}
```

---

## 7. UI/UX Specifications

**Not applicable.** This is a test-only specification with no user-facing UI components.

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Benchmark {Name} completed: {ElapsedMs}ms, {AllocatedBytes} bytes"` |
| Info | `"Performance test suite completed: {PassCount}/{TotalCount} passed"` |
| Warning | `"Performance threshold exceeded: {Operation} took {ActualMs}ms (threshold: {ThresholdMs}ms)"` |
| Error | `"Performance regression detected: {Operation} regressed {RegressionPercent}%"` |
| Info | `"Baseline updated: {Operation} from {OldMs}ms to {NewMs}ms"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Baseline tampering | Low | Version control tracks changes |
| Resource exhaustion | Low | Reasonable test sizes (max 50K words) |
| CI timeout | Medium | Individual test timeouts |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | 10,000 words | Analyzing readability | Completes in < 200ms |
| 2 | 10,000 words | Scanning fuzzy matches | Completes in < 300ms |
| 3 | 10,000 words | Detecting passive voice | Completes in < 100ms |
| 4 | 10,000 words | Running full pipeline | Completes in < 500ms |
| 5 | 10,000 words | Running full pipeline | Allocates < 50MB |

### 10.2 CI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 6 | Performance > baseline + 10% | CI runs tests | Build fails |
| 7 | Performance <= baseline + 10% | CI runs tests | Build succeeds |
| 8 | Performance improved | Developer request | Baseline can be updated |

### 10.3 Tooling Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | BenchmarkDotNet installed | Running benchmarks | Generates HTML report |
| 10 | Baseline file exists | Loading baseline | Values loaded correctly |
| 11 | No baseline file | Loading baseline | Default values used |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `AnalysisBenchmarks.cs` with BenchmarkDotNet | [ ] |
| 2 | `PerformanceThresholdTests.cs` for CI | [ ] |
| 3 | `LoremIpsumGenerator.cs` test utility | [ ] |
| 4 | `PerformanceBaseline.cs` record | [ ] |
| 5 | `PerformanceBaselineLoader.cs` utility | [ ] |
| 6 | `Baselines/baseline.json` initial baseline | [ ] |
| 7 | BenchmarkDotNet NuGet package reference | [ ] |
| 8 | Test trait configuration | [ ] |
| 9 | CI filter for `Category=Performance` | [ ] |

---

## 12. Verification Commands

```bash
# Run all performance threshold tests (CI)
dotnet test --filter "Category=Performance&Version=v0.3.8d" --logger "console;verbosity=detailed"

# Run BenchmarkDotNet benchmarks (detailed analysis)
dotnet run --project tests/Lexichord.Tests.Performance -c Release -- --filter "*"

# Run specific benchmark category
dotnet run --project tests/Lexichord.Tests.Performance -c Release -- --filter "*Readability*"

# Run benchmarks with memory diagnostics
dotnet run --project tests/Lexichord.Tests.Performance -c Release -- --memory

# Export benchmark results to JSON
dotnet run --project tests/Lexichord.Tests.Performance -c Release -- --exporters json

# Run with coverage
dotnet test --filter "Category=Performance" --collect:"XPlat Code Coverage"
```

---

## 13. CI Pipeline Configuration

### 13.1 GitHub Actions Example

```yaml
name: Performance Tests

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  performance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run Performance Tests
        run: |
          dotnet test --configuration Release \
            --filter "Category=Performance" \
            --logger "trx;LogFileName=performance-results.trx" \
            --no-build

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: performance-results
          path: '**/TestResults/*.trx'

      - name: Run Benchmarks (Optional)
        if: github.event_name == 'push' && github.ref == 'refs/heads/main'
        run: |
          dotnet run --project tests/Lexichord.Tests.Performance \
            --configuration Release \
            -- --filter "*" --exporters json html

      - name: Upload Benchmark Results
        uses: actions/upload-artifact@v4
        if: github.event_name == 'push' && github.ref == 'refs/heads/main'
        with:
          name: benchmark-results
          path: 'BenchmarkDotNet.Artifacts/**/*'
```

### 13.2 Test Filter Commands

```bash
# Run all v0.3.8 tests
dotnet test --filter "Version~v0.3.8"

# Run only performance tests
dotnet test --filter "Category=Performance"

# Run only v0.3.8d performance tests
dotnet test --filter "Category=Performance&Version=v0.3.8d"

# Run benchmarks only (no threshold tests)
dotnet test --filter "Category=Benchmark"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-26 | Lead Architect | Initial draft |
