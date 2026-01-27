# LCS-DES-048a: Unit Test Suite

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-048a                             |
| **Version**      | v0.4.8a                                  |
| **Title**        | Unit Test Suite                          |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG.Tests`            |
| **License Tier** | N/A (Development)                        |

---

## 1. Overview

### 1.1 Purpose

This specification defines the comprehensive unit test suite for the RAG module, covering chunking strategies, embedding services, search functionality, token counting, and ingestion pipelines. The goal is to achieve ≥80% code coverage.

### 1.2 Goals

- Create tests for all chunking strategies
- Create tests for embedding service with mocked HTTP
- Create tests for search scoring and filtering
- Create tests for token counting edge cases
- Create tests for ingestion pipeline logic
- Generate coverage reports with Coverlet
- Achieve ≥80% line coverage

### 1.3 Non-Goals

- Performance testing (covered in v0.4.8c)
- Integration with real databases (covered in v0.4.8b)
- UI testing

---

## 2. Test Project Structure

### 2.1 Project Configuration

```xml
<!-- tests/Lexichord.Modules.RAG.Tests/Lexichord.Modules.RAG.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.x" />
    <PackageReference Include="xunit" Version="2.x" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.x" />
    <PackageReference Include="FluentAssertions" Version="6.x" />
    <PackageReference Include="Moq" Version="4.x" />
    <PackageReference Include="coverlet.collector" Version="6.x" />
    <PackageReference Include="RichardSzalay.MockHttp" Version="7.x" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Lexichord.Modules.RAG\Lexichord.Modules.RAG.csproj" />
  </ItemGroup>
</Project>
```

### 2.2 Directory Structure

```
tests/Lexichord.Modules.RAG.Tests/
├── Chunking/
│   ├── FixedSizeChunkingStrategyTests.cs
│   ├── ParagraphChunkingStrategyTests.cs
│   └── MarkdownHeaderChunkingStrategyTests.cs
├── Embedding/
│   ├── OpenAIEmbeddingServiceTests.cs
│   ├── TiktokenTokenCounterTests.cs
│   └── EmbeddingPipelineTests.cs
├── Search/
│   ├── PgVectorSearchServiceTests.cs
│   ├── QueryPreprocessorTests.cs
│   └── SearchLicenseGuardTests.cs
├── Ingestion/
│   ├── IngestionServiceTests.cs
│   ├── FileHashServiceTests.cs
│   └── IngestionQueueTests.cs
├── Fixtures/
│   └── TestDataFixtures.cs
└── _Imports.cs
```

---

## 3. Test Specifications

### 3.1 Chunking Strategy Tests

```csharp
namespace Lexichord.Modules.RAG.Tests.Chunking;

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class FixedSizeChunkingStrategyTests
{
    private readonly FixedSizeChunkingStrategy _sut = new();

    [Fact]
    public void Mode_ReturnsFixedSize()
    {
        _sut.Mode.Should().Be(ChunkingMode.FixedSize);
    }

    [Fact]
    public void Split_EmptyContent_ReturnsEmptyList()
    {
        var chunks = _sut.Split("", new ChunkingOptions { MaxChunkSize = 100 });
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void Split_ContentSmallerThanChunkSize_ReturnsSingleChunk()
    {
        var content = "Hello world";
        var options = new ChunkingOptions { MaxChunkSize = 100 };

        var chunks = _sut.Split(content, options);

        chunks.Should().ContainSingle();
        chunks[0].Content.Should().Be(content);
    }

    [Theory]
    [InlineData(100, 0, 10)]   // No overlap
    [InlineData(100, 50, 19)]  // 50% overlap
    [InlineData(100, 25, 13)]  // 25% overlap
    public void Split_WithOverlap_ReturnsCorrectChunkCount(
        int chunkSize, int overlap, int minExpectedChunks)
    {
        var content = new string('a', 1000);
        var options = new ChunkingOptions
        {
            MaxChunkSize = chunkSize,
            Overlap = overlap
        };

        var chunks = _sut.Split(content, options);

        chunks.Should().HaveCountGreaterOrEqualTo(minExpectedChunks);
    }

    [Fact]
    public void Split_RespectsWordBoundaries()
    {
        var content = "word1 word2 word3 word4 word5";
        var options = new ChunkingOptions { MaxChunkSize = 12, Overlap = 0 };

        var chunks = _sut.Split(content, options);

        // No chunk should split mid-word
        foreach (var chunk in chunks)
        {
            chunk.Content.Should().NotStartWith(" ");
            chunk.Content.Trim().Should().NotContain("ord"); // partial word
        }
    }

    [Fact]
    public void Split_SetsCorrectOffsets()
    {
        var content = "First chunk content. Second chunk content.";
        var options = new ChunkingOptions { MaxChunkSize = 20, Overlap = 0 };

        var chunks = _sut.Split(content, options);

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            chunk.Metadata.Index.Should().Be(i);
            chunk.StartOffset.Should().BeGreaterOrEqualTo(0);
            chunk.EndOffset.Should().BeGreaterThan(chunk.StartOffset);
        }
    }

    [Fact]
    public void Split_OverlappingChunks_ShareContent()
    {
        var content = "AAAAAAAAAA BBBBBBBBBB CCCCCCCCCC";
        var options = new ChunkingOptions { MaxChunkSize = 15, Overlap = 5 };

        var chunks = _sut.Split(content, options);

        // With overlap, consecutive chunks should share some content
        if (chunks.Count > 1)
        {
            var firstEnd = chunks[0].Content[^5..];
            var secondStart = chunks[1].Content[..5];
            // They should have some overlap
            chunks[0].EndOffset.Should().BeGreaterThan(chunks[1].StartOffset - options.Overlap);
        }
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class ParagraphChunkingStrategyTests
{
    private readonly ParagraphChunkingStrategy _sut = new();

    [Fact]
    public void Split_SingleParagraph_ReturnsSingleChunk()
    {
        var content = "This is a single paragraph with no breaks.";

        var chunks = _sut.Split(content, new ChunkingOptions());

        chunks.Should().ContainSingle();
    }

    [Fact]
    public void Split_MultipleParagraphs_SplitsOnDoubleNewline()
    {
        var content = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";

        var chunks = _sut.Split(content, new ChunkingOptions());

        chunks.Should().HaveCount(3);
    }

    [Fact]
    public void Split_ShortParagraphs_MergesWithNext()
    {
        var content = "Short.\n\nAnother short.\n\nThis is a longer paragraph.";
        var options = new ChunkingOptions { MinChunkSize = 50 };

        var chunks = _sut.Split(content, options);

        // Short paragraphs should be merged
        chunks.Should().HaveCountLessThan(3);
    }

    [Fact]
    public void Split_LongParagraph_FallsBackToFixedSize()
    {
        var longParagraph = new string('a', 3000);
        var options = new ChunkingOptions { MaxChunkSize = 1000 };

        var chunks = _sut.Split(longParagraph, options);

        chunks.Should().HaveCountGreaterThan(1);
        foreach (var chunk in chunks)
        {
            chunk.Content.Length.Should().BeLessThanOrEqualTo(1000);
        }
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class MarkdownHeaderChunkingStrategyTests
{
    private readonly MarkdownHeaderChunkingStrategy _sut = new();

    [Fact]
    public void Split_NoHeaders_ReturnsSingleChunk()
    {
        var content = "Just plain text without any markdown headers.";

        var chunks = _sut.Split(content, new ChunkingOptions());

        chunks.Should().ContainSingle();
    }

    [Fact]
    public void Split_WithHeaders_SplitsAtHeaders()
    {
        var content = @"# Header 1
Content under header 1.

## Header 2
Content under header 2.

## Header 3
Content under header 3.";

        var chunks = _sut.Split(content, new ChunkingOptions());

        chunks.Should().HaveCount(3);
    }

    [Fact]
    public void Split_CapturesHeadingInMetadata()
    {
        var content = @"# Introduction
Some intro text.

## Background
Background info.";

        var chunks = _sut.Split(content, new ChunkingOptions());

        chunks[0].Metadata.Heading.Should().Be("Introduction");
        chunks[1].Metadata.Heading.Should().Be("Background");
    }

    [Fact]
    public void Split_TracksHeadingLevel()
    {
        var content = @"# H1
## H2
### H3";

        var chunks = _sut.Split(content, new ChunkingOptions());

        chunks[0].Metadata.Level.Should().Be(1);
        chunks[1].Metadata.Level.Should().Be(2);
        chunks[2].Metadata.Level.Should().Be(3);
    }
}
```

### 3.2 Embedding Service Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class OpenAIEmbeddingServiceTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly Mock<ISecureVault> _vaultMock;
    private readonly OpenAIEmbeddingService _sut;

    public OpenAIEmbeddingServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _vaultMock = new Mock<ISecureVault>();
        _vaultMock.Setup(v => v.GetSecretAsync("openai:api-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-api-key");

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.openai.com/");

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("OpenAI")).Returns(httpClient);

        _sut = new OpenAIEmbeddingService(
            httpFactory.Object,
            _vaultMock.Object,
            NullLogger<OpenAIEmbeddingService>.Instance);
    }

    [Fact]
    public void ModelName_ReturnsExpectedModel()
    {
        _sut.ModelName.Should().Be("text-embedding-3-small");
    }

    [Fact]
    public void Dimensions_Returns1536()
    {
        _sut.Dimensions.Should().Be(1536);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsEmbedding()
    {
        // Arrange
        var embedding = CreateMockEmbedding(1536);
        _mockHttp.When("https://api.openai.com/v1/embeddings")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                data = new[] { new { embedding = embedding } }
            }));

        // Act
        var result = await _sut.EmbedAsync("Hello world");

        // Assert
        result.Should().HaveCount(1536);
    }

    [Fact]
    public async Task EmbedAsync_NoApiKey_ThrowsException()
    {
        _vaultMock.Setup(v => v.GetSecretAsync("openai:api-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await _sut.Invoking(s => s.EmbedAsync("test"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task EmbedBatchAsync_ProcessesMultipleTexts()
    {
        var texts = new List<string> { "Text 1", "Text 2", "Text 3" };
        var embeddings = texts.Select(_ => CreateMockEmbedding(1536)).ToList();

        _mockHttp.When("https://api.openai.com/v1/embeddings")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                data = embeddings.Select((e, i) => new { embedding = e, index = i })
            }));

        var results = await _sut.EmbedBatchAsync(texts);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(e => e.Should().HaveCount(1536));
    }

    [Fact]
    public async Task EmbedAsync_RateLimit_RetriesWithBackoff()
    {
        var callCount = 0;
        _mockHttp.When("https://api.openai.com/v1/embeddings")
            .Respond(req =>
            {
                callCount++;
                if (callCount < 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        data = new[] { new { embedding = CreateMockEmbedding(1536) } }
                    }))
                };
            });

        var result = await _sut.EmbedAsync("test");

        callCount.Should().Be(3);
        result.Should().HaveCount(1536);
    }

    private static float[] CreateMockEmbedding(int dimensions)
    {
        return Enumerable.Range(0, dimensions).Select(i => (float)i / dimensions).ToArray();
    }
}
```

### 3.3 Search Service Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class SearchServiceTests
{
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IEmbeddingService> _embedderMock;
    private readonly Mock<IQueryPreprocessor> _preprocessorMock;
    private readonly PgVectorSearchService _sut;

    public SearchServiceTests()
    {
        _chunkRepoMock = new Mock<IChunkRepository>();
        _embedderMock = new Mock<IEmbeddingService>();
        _preprocessorMock = new Mock<IQueryPreprocessor>();

        _embedderMock.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        _preprocessorMock.Setup(p => p.Process(It.IsAny<string>(), It.IsAny<SearchOptions>()))
            .Returns<string, SearchOptions>((q, _) => q.Trim().ToLower());

        _sut = new PgVectorSearchService(
            _chunkRepoMock.Object,
            _embedderMock.Object,
            _preprocessorMock.Object,
            NullLogger<PgVectorSearchService>.Instance);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyResult()
    {
        var result = await _sut.SearchAsync("", new SearchOptions());

        result.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_PreprocessesQuery()
    {
        await _sut.SearchAsync("  TEST QUERY  ", new SearchOptions());

        _preprocessorMock.Verify(p => p.Process("  TEST QUERY  ", It.IsAny<SearchOptions>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_EmbedsQuery()
    {
        await _sut.SearchAsync("test query", new SearchOptions());

        _embedderMock.Verify(e => e.EmbedAsync("test query", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_RespectsTopK()
    {
        var options = new SearchOptions { TopK = 5 };

        await _sut.SearchAsync("test", options);

        _chunkRepoMock.Verify(r => r.SearchByVectorAsync(
            It.IsAny<float[]>(),
            5,
            It.IsAny<float>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_RespectsMinScore()
    {
        var options = new SearchOptions { MinScore = 0.8f };

        await _sut.SearchAsync("test", options);

        _chunkRepoMock.Verify(r => r.SearchByVectorAsync(
            It.IsAny<float[]>(),
            It.IsAny<int>(),
            0.8f,
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_AppliesDocumentFilter()
    {
        var docId = Guid.NewGuid();
        var options = new SearchOptions { DocumentFilter = docId };

        await _sut.SearchAsync("test", options);

        _chunkRepoMock.Verify(r => r.SearchByVectorAsync(
            It.IsAny<float[]>(),
            It.IsAny<int>(),
            It.IsAny<float>(),
            docId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ReturnsDurationInResult()
    {
        var result = await _sut.SearchAsync("test", new SearchOptions());

        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
```

### 3.4 Token Counter Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class TiktokenTokenCounterTests
{
    private readonly TiktokenTokenCounter _sut = new();

    [Theory]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    [InlineData("Hello", 1)]
    [InlineData("Hello world", 2)]
    public void CountTokens_BasicCases_ReturnsExpected(string? text, int expected)
    {
        var result = _sut.CountTokens(text!);
        result.Should().BeCloseTo(expected, 1);
    }

    [Fact]
    public void CountTokens_LongText_CountsAccurately()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 1000));
        var result = _sut.CountTokens(text);

        // ~1000 words should be ~1000-1500 tokens
        result.Should().BeInRange(900, 1500);
    }

    [Fact]
    public void TruncateToTokenLimit_UnderLimit_ReturnsUnchanged()
    {
        var (text, truncated) = _sut.TruncateToTokenLimit("Hello", 100);

        text.Should().Be("Hello");
        truncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateToTokenLimit_OverLimit_Truncates()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 10000));
        var (text, truncated) = _sut.TruncateToTokenLimit(longText, 100);

        _sut.CountTokens(text).Should().BeLessThanOrEqualTo(100);
        truncated.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TruncateToTokenLimit_InvalidLimit_Throws(int limit)
    {
        _sut.Invoking(s => s.TruncateToTokenLimit("test", limit))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Encode_Decode_RoundTrip()
    {
        var original = "Hello world, this is a test!";
        var tokens = _sut.Encode(original);
        var decoded = _sut.Decode(tokens);

        decoded.Should().Be(original);
    }

    [Theory]
    [InlineData("Unicode: 你好世界")]
    [InlineData("Emoji: 🎉🚀✨")]
    [InlineData("Special: @#$%^&*()")]
    public void Encode_Decode_HandlesSpecialCharacters(string text)
    {
        var tokens = _sut.Encode(text);
        var decoded = _sut.Decode(tokens);

        decoded.Should().Be(text);
    }
}
```

---

## 4. Coverage Configuration

### 4.1 runsettings File

```xml
<!-- tests/coverage.runsettings -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <Exclude>[*Tests*]*,[*]*.Migrations.*</Exclude>
          <Include>[Lexichord.Modules.RAG]*</Include>
          <ExcludeByAttribute>
            GeneratedCodeAttribute,CompilerGeneratedAttribute
          </ExcludeByAttribute>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### 4.2 Coverage Script

```bash
#!/bin/bash
# scripts/run-coverage.sh

dotnet test tests/Lexichord.Modules.RAG.Tests \
  --filter Category=Unit \
  --settings tests/coverage.runsettings \
  --results-directory ./coverage

# Generate HTML report
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:Html

echo "Coverage report: ./coverage/report/index.html"
```

---

## 5. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Information | "Test run complete: {Passed}/{Total} passed" | After test run |
| Debug | "Coverage: {Percent}% ({Covered}/{Total} lines)" | After coverage |

---

## 6. File Locations

| File | Path |
| :--- | :--- |
| Project | `tests/Lexichord.Modules.RAG.Tests/Lexichord.Modules.RAG.Tests.csproj` |
| Chunking tests | `tests/Lexichord.Modules.RAG.Tests/Chunking/` |
| Embedding tests | `tests/Lexichord.Modules.RAG.Tests/Embedding/` |
| Search tests | `tests/Lexichord.Modules.RAG.Tests/Search/` |
| Coverage settings | `tests/coverage.runsettings` |

---

## 7. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | All chunking strategies have tests | [ ] |
| 2 | Embedding service tested with mock HTTP | [ ] |
| 3 | Search scoring logic verified | [ ] |
| 4 | Token counting edge cases covered | [ ] |
| 5 | Ingestion pipeline tested | [ ] |
| 6 | Code coverage ≥ 80% | [ ] |
| 7 | All tests pass | [ ] |

---

## 8. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
