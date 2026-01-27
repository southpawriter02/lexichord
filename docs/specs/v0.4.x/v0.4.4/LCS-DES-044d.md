# LCS-DES-044d: Embedding Pipeline

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-044d                             |
| **Version**      | v0.4.4d                                  |
| **Title**        | Embedding Pipeline                       |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines the `DocumentIndexingPipeline`, which orchestrates the complete document-to-vector indexing flow. It integrates chunking, token validation, embedding generation, and database storage into a cohesive pipeline.

### 1.2 Goals

- Orchestrate the full indexing workflow
- Integrate chunking, token counting, embedding, and storage
- Support batch embedding for efficiency
- Handle partial failures gracefully
- Publish MediatR events for progress tracking
- Enforce license gating at WriterPro tier

### 1.3 Non-Goals

- UI for indexing progress (v0.4.7)
- Embedding caching (v0.4.8)
- Incremental/delta indexing (future)

---

## 2. Pipeline Flow

### 2.1 Overview

```text
┌─────────────┐     ┌──────────┐     ┌───────────┐     ┌─────────┐     ┌─────────┐
│   Document  │ ──► │  Chunk   │ ──► │  Validate │ ──► │  Embed  │ ──► │  Store  │
│   Content   │     │  (Split) │     │  (Tokens) │     │ (Batch) │     │   (DB)  │
└─────────────┘     └──────────┘     └───────────┘     └─────────┘     └─────────┘
                                                                            │
                                                                            ▼
                                                                    ┌───────────────┐
                                                                    │ Publish Event │
                                                                    └───────────────┘
```

### 2.2 Detailed Flow

```text
IndexDocumentAsync(filePath, content):
│
├── 1. LICENSE CHECK
│   ├── Check ILicenseContext.Tier >= WriterPro
│   └── If not licensed → Throw FeatureNotLicensedException
│
├── 2. DOCUMENT RECORD
│   ├── Get existing document by path OR create new
│   ├── Set status = Indexing
│   ├── Set IndexedAt = now
│   └── Upsert to database
│
├── 3. CLEAR OLD CHUNKS
│   └── Delete all chunks for this document ID
│
├── 4. CHUNK CONTENT
│   ├── Select chunking strategy (auto-detect or configured)
│   ├── Split content → TextChunk[]
│   └── Log chunk count
│
├── 5. VALIDATE TOKENS
│   │
│   └── FOR EACH chunk:
│       ├── TruncateToTokenLimit(chunk.Content, MaxTokens)
│       ├── If truncated → Log warning
│       └── Collect validated texts
│
├── 6. GENERATE EMBEDDINGS (Batched)
│   │
│   └── FOR batches of 100:
│       ├── EmbedBatchAsync(batch)
│       ├── Collect embeddings
│       └── Log progress
│
├── 7. STORE CHUNKS
│   ├── Create Chunk entities with:
│   │   ├── DocumentId
│   │   ├── Content (validated)
│   │   ├── ChunkIndex
│   │   ├── Embedding vector
│   │   └── Metadata JSON
│   └── BulkInsertAsync(chunks)
│
├── 8. UPDATE DOCUMENT
│   ├── Status = Indexed
│   ├── ChunkCount = count
│   └── UpsertAsync(document)
│
├── 9. PUBLISH SUCCESS EVENT
│   └── DocumentIndexedEvent { DocumentId, FilePath, ChunkCount, Duration }
│
└── RETURN IndexingResult { Success, DocumentId, ChunkCount, Duration }

ON ERROR:
├── Log error
├── Update document status = Failed, ErrorMessage
├── Publish DocumentIndexingFailedEvent
└── RETURN IndexingResult { Success = false, ErrorMessage }
```

---

## 3. Implementation

### 3.1 Class Definition

```csharp
namespace Lexichord.Modules.RAG.Indexing;

/// <summary>
/// Orchestrates the complete document indexing pipeline:
/// Chunk → Validate → Embed → Store
/// </summary>
public sealed class DocumentIndexingPipeline
{
    private readonly ChunkingStrategyFactory _chunkingFactory;
    private readonly ITokenCounter _tokenCounter;
    private readonly IEmbeddingService _embedder;
    private readonly IChunkRepository _chunkRepo;
    private readonly IDocumentRepository _docRepo;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<DocumentIndexingPipeline> _logger;

    public DocumentIndexingPipeline(
        ChunkingStrategyFactory chunkingFactory,
        ITokenCounter tokenCounter,
        IEmbeddingService embedder,
        IChunkRepository chunkRepo,
        IDocumentRepository docRepo,
        IMediator mediator,
        ILicenseContext licenseContext,
        IOptions<EmbeddingOptions> options,
        ILogger<DocumentIndexingPipeline> logger)
    {
        _chunkingFactory = chunkingFactory;
        _tokenCounter = tokenCounter;
        _embedder = embedder;
        _chunkRepo = chunkRepo;
        _docRepo = docRepo;
        _mediator = mediator;
        _licenseContext = licenseContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Indexes a document: chunks, embeds, and stores vectors.
    /// </summary>
    /// <param name="filePath">Path to the document.</param>
    /// <param name="content">Document content.</param>
    /// <param name="chunkingMode">Chunking strategy (default: auto-detect).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Indexing result with stats.</returns>
    public async Task<IndexingResult> IndexDocumentAsync(
        string filePath,
        string content,
        ChunkingMode? chunkingMode = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. License check
        EnsureLicensed();

        Document? document = null;

        try
        {
            _logger.LogInfo("Indexing document: {FilePath}", filePath);

            // 2. Create/update document record
            document = await GetOrCreateDocumentAsync(filePath);
            document.Status = DocumentStatus.Indexing;
            document.IndexedAt = DateTimeOffset.UtcNow;
            document = await _docRepo.UpsertAsync(document);

            // 3. Clear old chunks
            await _chunkRepo.DeleteByDocumentIdAsync(document.Id, ct);
            _logger.LogDebug("Deleted existing chunks for document {Id}", document.Id);

            // 4. Chunk content
            var strategy = chunkingMode.HasValue
                ? _chunkingFactory.GetStrategy(chunkingMode.Value)
                : _chunkingFactory.GetStrategy(content, Path.GetExtension(filePath));

            var chunks = strategy.Split(content, ChunkingOptions.Default);
            _logger.LogDebug("Created {ChunkCount} chunks using {Mode}", chunks.Count, strategy.Mode);

            if (chunks.Count == 0)
            {
                return CompleteIndexing(document, 0, stopwatch, false);
            }

            // 5. Validate tokens
            var (validatedTexts, truncationCount) = ValidateTokens(chunks);

            if (truncationCount > 0)
            {
                _logger.LogWarning(
                    "{TruncationCount} of {Total} chunks were truncated",
                    truncationCount, chunks.Count);
            }

            // 6. Generate embeddings (batched)
            var embeddings = await EmbedInBatchesAsync(validatedTexts, ct);

            // 7. Store chunks
            var chunkEntities = CreateChunkEntities(document.Id, chunks, validatedTexts, embeddings);
            await _chunkRepo.BulkInsertAsync(chunkEntities, ct);

            // 8. Update document
            return await CompleteIndexingAsync(document, chunks.Count, stopwatch, truncationCount > 0, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Indexing cancelled: {FilePath}", filePath);
            if (document != null)
            {
                await MarkDocumentFailedAsync(document, "Indexing cancelled");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document: {FilePath}", filePath);

            if (document != null)
            {
                await MarkDocumentFailedAsync(document, ex.Message);
            }

            await _mediator.Publish(new DocumentIndexingFailedEvent
            {
                FilePath = filePath,
                ErrorMessage = ex.Message
            }, ct);

            return new IndexingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private void EnsureLicensed()
    {
        if (_licenseContext.Tier < LicenseTier.WriterPro)
        {
            throw new FeatureNotLicensedException(
                "Document Embedding",
                LicenseTier.WriterPro);
        }
    }

    private async Task<Document> GetOrCreateDocumentAsync(string filePath)
    {
        var existing = await _docRepo.GetByPathAsync(filePath);
        return existing ?? new Document
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            Title = Path.GetFileName(filePath)
        };
    }

    private (List<string> Texts, int TruncationCount) ValidateTokens(IReadOnlyList<TextChunk> chunks)
    {
        var validatedTexts = new List<string>();
        var truncationCount = 0;

        foreach (var chunk in chunks)
        {
            var (text, wasTruncated) = _tokenCounter.TruncateToTokenLimit(
                chunk.Content,
                _options.MaxTokens);

            validatedTexts.Add(text);
            if (wasTruncated) truncationCount++;
        }

        return (validatedTexts, truncationCount);
    }

    private async Task<IReadOnlyList<float[]>> EmbedInBatchesAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct)
    {
        var results = new List<float[]>();
        var batchSize = _options.MaxBatchSize;
        var totalBatches = (int)Math.Ceiling((double)texts.Count / batchSize);

        for (var i = 0; i < texts.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = texts.Skip(i).Take(batchSize).ToList();
            var batchNumber = (i / batchSize) + 1;

            _logger.LogDebug(
                "Embedding batch {Current}/{Total} ({Count} texts)",
                batchNumber, totalBatches, batch.Count);

            var batchEmbeddings = await _embedder.EmbedBatchAsync(batch, ct);
            results.AddRange(batchEmbeddings);
        }

        return results;
    }

    private List<Chunk> CreateChunkEntities(
        Guid documentId,
        IReadOnlyList<TextChunk> chunks,
        IReadOnlyList<string> validatedTexts,
        IReadOnlyList<float[]> embeddings)
    {
        var entities = new List<Chunk>();

        for (var i = 0; i < chunks.Count; i++)
        {
            entities.Add(new Chunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Content = validatedTexts[i],
                ChunkIndex = i,
                Embedding = embeddings[i],
                Metadata = JsonSerializer.Serialize(chunks[i].Metadata)
            });
        }

        return entities;
    }

    private async Task<IndexingResult> CompleteIndexingAsync(
        Document document,
        int chunkCount,
        Stopwatch stopwatch,
        bool truncationOccurred,
        CancellationToken ct)
    {
        document.Status = DocumentStatus.Indexed;
        document.ChunkCount = chunkCount;
        document.ErrorMessage = null;
        await _docRepo.UpsertAsync(document);

        stopwatch.Stop();

        await _mediator.Publish(new DocumentIndexedEvent
        {
            DocumentId = document.Id,
            FilePath = document.FilePath,
            ChunkCount = chunkCount,
            Duration = stopwatch.Elapsed
        }, ct);

        _logger.LogInfo(
            "Indexed {FilePath}: {ChunkCount} chunks in {Duration}ms",
            document.FilePath, chunkCount, stopwatch.ElapsedMilliseconds);

        return new IndexingResult
        {
            Success = true,
            DocumentId = document.Id,
            ChunkCount = chunkCount,
            Duration = stopwatch.Elapsed,
            TruncationOccurred = truncationOccurred
        };
    }

    private IndexingResult CompleteIndexing(
        Document document,
        int chunkCount,
        Stopwatch stopwatch,
        bool truncationOccurred)
    {
        stopwatch.Stop();
        return new IndexingResult
        {
            Success = true,
            DocumentId = document.Id,
            ChunkCount = chunkCount,
            Duration = stopwatch.Elapsed,
            TruncationOccurred = truncationOccurred
        };
    }

    private async Task MarkDocumentFailedAsync(Document document, string errorMessage)
    {
        document.Status = DocumentStatus.Failed;
        document.ErrorMessage = errorMessage;
        await _docRepo.UpsertAsync(document);
    }
}
```

### 3.2 Supporting Types

```csharp
namespace Lexichord.Modules.RAG.Indexing;

/// <summary>
/// Result of a document indexing operation.
/// </summary>
public record IndexingResult
{
    /// <summary>Whether the indexing succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Document ID if successful.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Number of chunks created.</summary>
    public int ChunkCount { get; init; }

    /// <summary>Total indexing duration.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Whether any chunks were truncated.</summary>
    public bool TruncationOccurred { get; init; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Published when a document is successfully indexed.
/// </summary>
public record DocumentIndexedEvent : INotification
{
    /// <summary>ID of the indexed document.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Path to the indexed file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Number of chunks created.</summary>
    public int ChunkCount { get; init; }

    /// <summary>Total indexing duration.</summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Published when document indexing fails.
/// </summary>
public record DocumentIndexingFailedEvent : INotification
{
    /// <summary>Path to the file that failed.</summary>
    public required string FilePath { get; init; }

    /// <summary>Error message describing the failure.</summary>
    public required string ErrorMessage { get; init; }
}
```

### 3.3 DI Registration

```csharp
// In RAGModule.cs
services.AddScoped<DocumentIndexingPipeline>();
```

---

## 4. Integration with Ingestion Service

```csharp
// In IngestionService (from v0.4.2)
public class IngestionService : IIngestionService
{
    private readonly DocumentIndexingPipeline _pipeline;

    public async Task<IngestionResult> IngestFileAsync(string filePath, CancellationToken ct)
    {
        // Read file content
        var content = await File.ReadAllTextAsync(filePath, ct);

        // Index using pipeline
        var result = await _pipeline.IndexDocumentAsync(filePath, content, ct: ct);

        return new IngestionResult
        {
            Success = result.Success,
            FilesProcessed = 1,
            DocumentId = result.DocumentId,
            ErrorMessage = result.ErrorMessage,
            Duration = result.Duration
        };
    }
}
```

---

## 5. Event Handlers

### 5.1 Indexing Started Handler

```csharp
public class DocumentIndexingStartedHandler : INotificationHandler<DocumentIndexingStartedEvent>
{
    private readonly ILogger<DocumentIndexingStartedHandler> _logger;

    public Task Handle(DocumentIndexingStartedEvent notification, CancellationToken ct)
    {
        _logger.LogInfo(
            "Started indexing: {FilePath}",
            notification.FilePath);
        return Task.CompletedTask;
    }
}
```

### 5.2 Indexing Completed Handler

```csharp
public class DocumentIndexedHandler : INotificationHandler<DocumentIndexedEvent>
{
    private readonly ITelemetryService _telemetry;

    public async Task Handle(DocumentIndexedEvent notification, CancellationToken ct)
    {
        await _telemetry.TrackEventAsync(
            "DocumentIndexed",
            new Dictionary<string, object>
            {
                ["DocumentId"] = notification.DocumentId,
                ["ChunkCount"] = notification.ChunkCount,
                ["DurationMs"] = notification.Duration.TotalMilliseconds
            });
    }
}
```

---

## 6. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4d")]
public class DocumentIndexingPipelineTests
{
    private Mock<ChunkingStrategyFactory> _chunkingFactory = null!;
    private Mock<ITokenCounter> _tokenCounter = null!;
    private Mock<IEmbeddingService> _embedder = null!;
    private Mock<IChunkRepository> _chunkRepo = null!;
    private Mock<IDocumentRepository> _docRepo = null!;
    private Mock<IMediator> _mediator = null!;
    private Mock<ILicenseContext> _licenseContext = null!;
    private DocumentIndexingPipeline _sut = null!;

    [SetUp]
    public void Setup()
    {
        _chunkingFactory = new Mock<ChunkingStrategyFactory>();
        _tokenCounter = new Mock<ITokenCounter>();
        _embedder = new Mock<IEmbeddingService>();
        _chunkRepo = new Mock<IChunkRepository>();
        _docRepo = new Mock<IDocumentRepository>();
        _mediator = new Mock<IMediator>();
        _licenseContext = new Mock<ILicenseContext>();

        // Default: WriterPro licensed
        _licenseContext.Setup(l => l.Tier).Returns(LicenseTier.WriterPro);

        // Default: No truncation
        _tokenCounter
            .Setup(t => t.TruncateToTokenLimit(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((text, _) => (text, false));

        // Default: Return embeddings
        _embedder
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<IReadOnlyList<string>, CancellationToken, IEmbeddingService, IReadOnlyList<float[]>>(
                (texts, _) => texts.Select(_ => new float[1536]).ToList());

        _sut = CreatePipeline();
    }

    [Fact]
    public async Task IndexDocumentAsync_WithValidContent_ReturnsSuccess()
    {
        // Arrange
        SetupChunking(3);
        SetupDocumentRepo();

        // Act
        var result = await _sut.IndexDocumentAsync("/path/doc.md", "Test content");

        // Assert
        result.Success.Should().BeTrue();
        result.ChunkCount.Should().Be(3);
        result.DocumentId.Should().NotBeNull();
    }

    [Fact]
    public async Task IndexDocumentAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContext.Setup(l => l.Tier).Returns(LicenseTier.Core);

        // Act & Assert
        await _sut.Invoking(s => s.IndexDocumentAsync("/path/doc.md", "Content"))
            .Should().ThrowAsync<FeatureNotLicensedException>();
    }

    [Fact]
    public async Task IndexDocumentAsync_DeletesExistingChunks()
    {
        // Arrange
        var docId = Guid.NewGuid();
        SetupChunking(2);
        SetupDocumentRepo(docId);

        // Act
        await _sut.IndexDocumentAsync("/path/doc.md", "Content");

        // Assert
        _chunkRepo.Verify(r => r.DeleteByDocumentIdAsync(docId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexDocumentAsync_PublishesDocumentIndexedEvent()
    {
        // Arrange
        SetupChunking(5);
        SetupDocumentRepo();

        // Act
        await _sut.IndexDocumentAsync("/path/doc.md", "Content");

        // Assert
        _mediator.Verify(m => m.Publish(
            It.Is<DocumentIndexedEvent>(e => e.ChunkCount == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexDocumentAsync_OnFailure_PublishesFailedEvent()
    {
        // Arrange
        SetupChunking(2);
        SetupDocumentRepo();
        _embedder
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmbeddingException("API error"));

        // Act
        var result = await _sut.IndexDocumentAsync("/path/doc.md", "Content");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API error");
        _mediator.Verify(m => m.Publish(
            It.Is<DocumentIndexingFailedEvent>(e => e.ErrorMessage.Contains("API error")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexDocumentAsync_WithTruncation_SetsFlag()
    {
        // Arrange
        SetupChunking(3);
        SetupDocumentRepo();
        _tokenCounter
            .Setup(t => t.TruncateToTokenLimit(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(("truncated", true));

        // Act
        var result = await _sut.IndexDocumentAsync("/path/doc.md", "Long content");

        // Assert
        result.TruncationOccurred.Should().BeTrue();
    }

    [Fact]
    public async Task IndexDocumentAsync_BatchesEmbeddings()
    {
        // Arrange
        SetupChunking(150); // More than max batch of 100
        SetupDocumentRepo();

        // Act
        await _sut.IndexDocumentAsync("/path/doc.md", "Content");

        // Assert
        _embedder.Verify(
            e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // 100 + 50
    }

    [Fact]
    public async Task IndexDocumentAsync_EmptyContent_ReturnsSuccessWithZeroChunks()
    {
        // Arrange
        SetupChunking(0);
        SetupDocumentRepo();

        // Act
        var result = await _sut.IndexDocumentAsync("/path/doc.md", "");

        // Assert
        result.Success.Should().BeTrue();
        result.ChunkCount.Should().Be(0);
    }

    private void SetupChunking(int chunkCount)
    {
        var chunks = Enumerable.Range(0, chunkCount)
            .Select(i => new TextChunk($"Chunk {i}", i * 100, (i + 1) * 100, new ChunkMetadata(i)))
            .ToList();

        var strategy = new Mock<IChunkingStrategy>();
        strategy.Setup(s => s.Split(It.IsAny<string>(), It.IsAny<ChunkingOptions>()))
            .Returns(chunks);
        strategy.Setup(s => s.Mode).Returns(ChunkingMode.Paragraph);

        _chunkingFactory
            .Setup(f => f.GetStrategy(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(strategy.Object);
    }

    private void SetupDocumentRepo(Guid? existingId = null)
    {
        var doc = existingId.HasValue
            ? new Document { Id = existingId.Value, FilePath = "/path/doc.md" }
            : null;

        _docRepo.Setup(r => r.GetByPathAsync(It.IsAny<string>())).ReturnsAsync(doc);
        _docRepo.Setup(r => r.UpsertAsync(It.IsAny<Document>()))
            .ReturnsAsync<Document, IDocumentRepository, Document>(d =>
            {
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                return d;
            });
    }

    private DocumentIndexingPipeline CreatePipeline()
    {
        return new DocumentIndexingPipeline(
            _chunkingFactory.Object,
            _tokenCounter.Object,
            _embedder.Object,
            _chunkRepo.Object,
            _docRepo.Object,
            _mediator.Object,
            _licenseContext.Object,
            Options.Create(EmbeddingOptions.Default),
            NullLogger<DocumentIndexingPipeline>.Instance);
    }
}
```

---

## 7. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Info | "Indexing document: {FilePath}" | Start |
| Debug | "Deleted existing chunks for document {Id}" | After cleanup |
| Debug | "Created {ChunkCount} chunks using {Mode}" | After chunking |
| Warning | "{TruncationCount} of {Total} chunks were truncated" | Token validation |
| Debug | "Embedding batch {Current}/{Total} ({Count} texts)" | During embedding |
| Info | "Indexed {FilePath}: {ChunkCount} chunks in {Duration}ms" | Success |
| Warning | "Indexing cancelled: {FilePath}" | On cancellation |
| Error | "Failed to index document: {FilePath}" | On error |

---

## 8. File Locations

| File | Path |
| :--- | :--- |
| Pipeline | `src/Lexichord.Modules.RAG/Indexing/DocumentIndexingPipeline.cs` |
| Result | `src/Lexichord.Modules.RAG/Indexing/IndexingResult.cs` |
| Events | `src/Lexichord.Modules.RAG/Events/IndexingEvents.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Indexing/DocumentIndexingPipelineTests.cs` |

---

## 9. Dependencies

| Dependency | Source | Purpose |
| :--------- | :----- | :------ |
| `ChunkingStrategyFactory` | v0.4.3 | Strategy selection |
| `ITokenCounter` | v0.4.4c | Token validation |
| `IEmbeddingService` | v0.4.4a/b | Vector generation |
| `IDocumentRepository` | v0.4.1c | Document storage |
| `IChunkRepository` | v0.4.1c | Chunk storage |
| `IMediator` | v0.0.7a | Event publishing |
| `ILicenseContext` | v0.0.4c | License enforcement |

---

## 10. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Chunks document using selected strategy | [ ] |
| 2 | Validates all chunks against token limit | [ ] |
| 3 | Embeds chunks in batches of 100 | [ ] |
| 4 | Stores chunks with embeddings in database | [ ] |
| 5 | Updates document status to Indexed | [ ] |
| 6 | Publishes DocumentIndexedEvent on success | [ ] |
| 7 | Publishes DocumentIndexingFailedEvent on error | [ ] |
| 8 | Enforces WriterPro license | [ ] |
| 9 | Handles cancellation gracefully | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 11. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
