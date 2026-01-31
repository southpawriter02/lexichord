# LCS-DES-047b: Manual Indexing Controls

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-047b                             |
| **Version**      | v0.4.7b                                  |
| **Title**        | Manual Indexing Controls                 |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines `IIndexManagementService` and the UI commands for manually managing document indexing. Users can re-index individual documents, remove documents from the index, and trigger a full re-index of all documents.

### 1.2 Goals

- Define `IIndexManagementService` interface for manual operations
- Implement Re-index Document command
- Implement Remove from Index command with confirmation
- Implement Re-index All command with safety confirmation
- Publish MediatR events for operation tracking
- Handle errors gracefully with user feedback

### 1.3 Non-Goals

- Scheduled/automatic re-indexing (future)
- Selective batch operations (future)
- Index export/import (future)

---

## 2. Design

### 2.1 IIndexManagementService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for manual index management operations.
/// </summary>
public interface IIndexManagementService
{
    /// <summary>
    /// Re-indexes a specific document.
    /// </summary>
    /// <param name="documentId">The document ID to re-index.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<IndexOperationResult> ReindexDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Re-indexes a document by file path.
    /// </summary>
    /// <param name="filePath">The file path to re-index.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<IndexOperationResult> ReindexDocumentAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Removes a document and its chunks from the index.
    /// </summary>
    /// <param name="documentId">The document ID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<IndexOperationResult> RemoveDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Re-indexes all documents in the workspace.
    /// </summary>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<IndexOperationResult> ReindexAllAsync(
        IProgress<IndexingProgressInfo>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the count of documents that would be affected by ReindexAll.
    /// </summary>
    Task<int> GetReindexAllCountAsync(CancellationToken ct = default);
}
```

### 2.2 IndexOperationResult Record

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Result of an index management operation.
/// </summary>
public record IndexOperationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The type of operation performed.
    /// </summary>
    public IndexOperationType OperationType { get; init; }

    /// <summary>
    /// Number of documents affected.
    /// </summary>
    public int DocumentsAffected { get; init; }

    /// <summary>
    /// Number of chunks affected.
    /// </summary>
    public int ChunksAffected { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    public static IndexOperationResult Succeeded(
        IndexOperationType type,
        int documents = 1,
        int chunks = 0,
        TimeSpan? duration = null) => new()
    {
        Success = true,
        OperationType = type,
        DocumentsAffected = documents,
        ChunksAffected = chunks,
        Duration = duration ?? TimeSpan.Zero
    };

    public static IndexOperationResult Failed(
        IndexOperationType type,
        string error) => new()
    {
        Success = false,
        OperationType = type,
        ErrorMessage = error
    };
}

public enum IndexOperationType
{
    Reindex,
    Remove,
    ReindexAll
}
```

### 2.3 IndexingProgressInfo Record

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Progress information for indexing operations.
/// </summary>
public record IndexingProgressInfo
{
    /// <summary>
    /// Current document being processed.
    /// </summary>
    public string? CurrentDocument { get; init; }

    /// <summary>
    /// Number of documents processed so far.
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// Total number of documents to process.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int PercentComplete => TotalCount > 0
        ? (int)((double)ProcessedCount / TotalCount * 100)
        : 0;

    /// <summary>
    /// Whether the operation is complete.
    /// </summary>
    public bool IsComplete => ProcessedCount >= TotalCount;

    /// <summary>
    /// Whether the operation was cancelled.
    /// </summary>
    public bool WasCancelled { get; init; }
}
```

### 2.4 IndexManagementService Implementation

```csharp
namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of manual index management operations.
/// </summary>
public sealed class IndexManagementService : IIndexManagementService
{
    private readonly IIngestionService _ingestionService;
    private readonly IDocumentRepository _documentRepo;
    private readonly IChunkRepository _chunkRepo;
    private readonly IMediator _mediator;
    private readonly ILogger<IndexManagementService> _logger;

    public IndexManagementService(
        IIngestionService ingestionService,
        IDocumentRepository documentRepo,
        IChunkRepository chunkRepo,
        IMediator mediator,
        ILogger<IndexManagementService> logger)
    {
        _ingestionService = ingestionService;
        _documentRepo = documentRepo;
        _chunkRepo = chunkRepo;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IndexOperationResult> ReindexDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var document = await _documentRepo.GetByIdAsync(documentId);
            if (document == null)
            {
                return IndexOperationResult.Failed(IndexOperationType.Reindex, "Document not found");
            }

            return await ReindexDocumentAsync(document.FilePath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-index document {Id}", documentId);
            return IndexOperationResult.Failed(IndexOperationType.Reindex, ex.Message);
        }
    }

    public async Task<IndexOperationResult> ReindexDocumentAsync(string filePath, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Re-indexing document: {Path}", filePath);

            await _mediator.Publish(new IndexOperationRequestedEvent
            {
                OperationType = IndexOperationType.Reindex,
                FilePath = filePath
            }, ct);

            var result = await _ingestionService.IngestFileAsync(filePath, ct);

            stopwatch.Stop();

            if (result.Success)
            {
                _logger.LogInformation(
                    "Re-indexed document: {Path}, {ChunkCount} chunks in {Duration}ms",
                    filePath, result.ChunkCount, stopwatch.ElapsedMilliseconds);

                return IndexOperationResult.Succeeded(
                    IndexOperationType.Reindex,
                    documents: 1,
                    chunks: result.ChunkCount,
                    duration: stopwatch.Elapsed);
            }
            else
            {
                return IndexOperationResult.Failed(IndexOperationType.Reindex, result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Re-index cancelled: {Path}", filePath);
            return IndexOperationResult.Failed(IndexOperationType.Reindex, "Operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-index document: {Path}", filePath);
            return IndexOperationResult.Failed(IndexOperationType.Reindex, ex.Message);
        }
    }

    public async Task<IndexOperationResult> RemoveDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var document = await _documentRepo.GetByIdAsync(documentId);
            if (document == null)
            {
                return IndexOperationResult.Failed(IndexOperationType.Remove, "Document not found");
            }

            _logger.LogInformation("Removing document from index: {Path}", document.FilePath);

            // Delete chunks first (foreign key constraint)
            var chunksDeleted = await _chunkRepo.DeleteByDocumentAsync(documentId, ct);

            // Delete document
            await _documentRepo.DeleteAsync(documentId);

            stopwatch.Stop();

            await _mediator.Publish(new DocumentRemovedFromIndexEvent
            {
                DocumentId = documentId,
                FilePath = document.FilePath,
                ChunksRemoved = chunksDeleted
            }, ct);

            _logger.LogInformation(
                "Removed document from index: {Path}, {ChunkCount} chunks deleted",
                document.FilePath, chunksDeleted);

            return IndexOperationResult.Succeeded(
                IndexOperationType.Remove,
                documents: 1,
                chunks: chunksDeleted,
                duration: stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove document {Id}", documentId);
            return IndexOperationResult.Failed(IndexOperationType.Remove, ex.Message);
        }
    }

    public async Task<IndexOperationResult> ReindexAllAsync(
        IProgress<IndexingProgressInfo>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var documents = await _documentRepo.GetAllAsync();
        var totalCount = documents.Count;
        var processedCount = 0;
        var totalChunks = 0;
        var errors = new List<string>();

        _logger.LogInformation("Starting full re-index of {Count} documents", totalCount);

        await _mediator.Publish(new IndexOperationRequestedEvent
        {
            OperationType = IndexOperationType.ReindexAll,
            DocumentCount = totalCount
        }, ct);

        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();

            progress?.Report(new IndexingProgressInfo
            {
                CurrentDocument = document.FilePath,
                ProcessedCount = processedCount,
                TotalCount = totalCount
            });

            try
            {
                var result = await _ingestionService.IngestFileAsync(document.FilePath, ct);
                if (result.Success)
                {
                    totalChunks += result.ChunkCount;
                }
                else
                {
                    errors.Add($"{document.FilePath}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{document.FilePath}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to re-index: {Path}", document.FilePath);
            }

            processedCount++;
        }

        progress?.Report(new IndexingProgressInfo
        {
            ProcessedCount = processedCount,
            TotalCount = totalCount
        });

        stopwatch.Stop();

        _logger.LogInformation(
            "Full re-index complete: {Count} documents, {Chunks} chunks, {Errors} errors in {Duration}ms",
            processedCount, totalChunks, errors.Count, stopwatch.ElapsedMilliseconds);

        if (errors.Count > 0)
        {
            return new IndexOperationResult
            {
                Success = false,
                OperationType = IndexOperationType.ReindexAll,
                DocumentsAffected = processedCount - errors.Count,
                ChunksAffected = totalChunks,
                ErrorMessage = $"{errors.Count} document(s) failed to index",
                Duration = stopwatch.Elapsed
            };
        }

        return IndexOperationResult.Succeeded(
            IndexOperationType.ReindexAll,
            documents: processedCount,
            chunks: totalChunks,
            duration: stopwatch.Elapsed);
    }

    public async Task<int> GetReindexAllCountAsync(CancellationToken ct = default)
    {
        var documents = await _documentRepo.GetAllAsync();
        return documents.Count;
    }
}
```

### 2.5 MediatR Events

```csharp
namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Event published when an index operation is requested.
/// </summary>
public record IndexOperationRequestedEvent : INotification
{
    public IndexOperationType OperationType { get; init; }
    public string? FilePath { get; init; }
    public Guid? DocumentId { get; init; }
    public int? DocumentCount { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event published when an index operation completes.
/// </summary>
public record IndexOperationCompletedEvent : INotification
{
    public IndexOperationType OperationType { get; init; }
    public bool Success { get; init; }
    public int DocumentsAffected { get; init; }
    public int ChunksAffected { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
```

### 2.6 ViewModel Commands Integration

```csharp
// Add to IndexStatusViewModel
public partial class IndexStatusViewModel
{
    private readonly IIndexManagementService _managementService;
    private readonly IDialogService _dialogService;

    [RelayCommand]
    private async Task ReindexDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var result = await _managementService.ReindexDocumentAsync(documentId, ct);

        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync("Re-index Failed", result.ErrorMessage!);
        }

        await RefreshAsync(ct);
    }

    [RelayCommand]
    private async Task RetryDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        // Same as re-index for failed documents
        await ReindexDocumentAsync(documentId, ct);
    }

    [RelayCommand]
    private async Task RemoveDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = Documents.FirstOrDefault(d => d.Id == documentId);
        if (doc == null) return;

        var confirmed = await _dialogService.ConfirmAsync(
            "Remove from Index",
            $"Are you sure you want to remove \"{doc.FileName}\" from the index?\n\n" +
            "This will delete all indexed chunks for this document. The file itself will not be deleted.",
            confirmText: "Remove",
            cancelText: "Cancel");

        if (!confirmed) return;

        var result = await _managementService.RemoveDocumentAsync(documentId, ct);

        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync("Remove Failed", result.ErrorMessage!);
        }

        await RefreshAsync(ct);
    }

    [RelayCommand]
    private async Task ReindexAllAsync(CancellationToken ct = default)
    {
        var count = await _managementService.GetReindexAllCountAsync(ct);

        var confirmed = await _dialogService.ConfirmAsync(
            "Re-index All Documents",
            $"This will re-index all {count} documents in the workspace.\n\n" +
            "This operation may take several minutes and will use your embedding API quota.\n\n" +
            "Do you want to continue?",
            confirmText: "Re-index All",
            cancelText: "Cancel",
            isDestructive: true);

        if (!confirmed) return;

        // Show progress (handled by v0.4.7c)
        var progress = new Progress<IndexingProgressInfo>(info =>
        {
            // Update progress UI
            _mediator.Publish(new IndexingProgressUpdatedEvent { Progress = info });
        });

        var result = await _managementService.ReindexAllAsync(progress, ct);

        if (!result.Success)
        {
            await _dialogService.ShowWarningAsync(
                "Re-index Complete with Errors",
                $"Completed with {result.ErrorMessage}");
        }

        await RefreshAsync(ct);
    }
}
```

---

## 3. Confirmation Dialog Specifications

### 3.1 Remove from Index Dialog

```
┌─────────────────────────────────────────────────────┐
│ Remove from Index                              [X] │
├─────────────────────────────────────────────────────┤
│                                                     │
│   Are you sure you want to remove "chapter-01.md"  │
│   from the index?                                   │
│                                                     │
│   This will delete all indexed chunks for this      │
│   document. The file itself will not be deleted.    │
│                                                     │
├─────────────────────────────────────────────────────┤
│                        [Cancel]  [Remove]          │
└─────────────────────────────────────────────────────┘
```

### 3.2 Re-index All Dialog

```
┌─────────────────────────────────────────────────────┐
│ ⚠ Re-index All Documents                      [X] │
├─────────────────────────────────────────────────────┤
│                                                     │
│   This will re-index all 42 documents in the        │
│   workspace.                                        │
│                                                     │
│   This operation may take several minutes and       │
│   will use your embedding API quota.                │
│                                                     │
│   Do you want to continue?                          │
│                                                     │
├─────────────────────────────────────────────────────┤
│                      [Cancel]  [Re-index All]      │
└─────────────────────────────────────────────────────┘
```

---

## 4. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7b")]
public class IndexManagementServiceTests
{
    private readonly Mock<IIngestionService> _ingestionMock;
    private readonly Mock<IDocumentRepository> _docRepoMock;
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IndexManagementService _sut;

    public IndexManagementServiceTests()
    {
        _ingestionMock = new Mock<IIngestionService>();
        _docRepoMock = new Mock<IDocumentRepository>();
        _chunkRepoMock = new Mock<IChunkRepository>();
        _mediatorMock = new Mock<IMediator>();

        _sut = new IndexManagementService(
            _ingestionMock.Object,
            _docRepoMock.Object,
            _chunkRepoMock.Object,
            _mediatorMock.Object,
            NullLogger<IndexManagementService>.Instance);
    }

    [Fact]
    public async Task ReindexDocumentAsync_ById_CallsIngestionService()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var doc = new Document { Id = docId, FilePath = "/test/doc.md" };
        _docRepoMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync(doc);
        _ingestionMock.Setup(i => i.IngestFileAsync(doc.FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult { Success = true, ChunkCount = 10 });

        // Act
        var result = await _sut.ReindexDocumentAsync(docId);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentsAffected.Should().Be(1);
        result.ChunksAffected.Should().Be(10);
    }

    [Fact]
    public async Task ReindexDocumentAsync_NotFound_ReturnsFailure()
    {
        _docRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Document?)null);

        var result = await _sut.ReindexDocumentAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task RemoveDocumentAsync_DeletesChunksAndDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var doc = new Document { Id = docId, FilePath = "/test/doc.md" };
        _docRepoMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync(doc);
        _chunkRepoMock.Setup(r => r.DeleteByDocumentAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var result = await _sut.RemoveDocumentAsync(docId);

        // Assert
        result.Success.Should().BeTrue();
        result.ChunksAffected.Should().Be(15);
        _docRepoMock.Verify(r => r.DeleteAsync(docId), Times.Once);
        _mediatorMock.Verify(m => m.Publish(
            It.IsAny<DocumentRemovedFromIndexEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReindexAllAsync_ProcessesAllDocuments()
    {
        // Arrange
        var docs = new List<Document>
        {
            new() { Id = Guid.NewGuid(), FilePath = "/doc1.md" },
            new() { Id = Guid.NewGuid(), FilePath = "/doc2.md" }
        };
        _docRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(docs);
        _ingestionMock.Setup(i => i.IngestFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult { Success = true, ChunkCount = 5 });

        // Act
        var result = await _sut.ReindexAllAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentsAffected.Should().Be(2);
        result.ChunksAffected.Should().Be(10);
    }

    [Fact]
    public async Task ReindexAllAsync_ReportsProgress()
    {
        // Arrange
        var docs = new List<Document>
        {
            new() { Id = Guid.NewGuid(), FilePath = "/doc1.md" },
            new() { Id = Guid.NewGuid(), FilePath = "/doc2.md" }
        };
        _docRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(docs);
        _ingestionMock.Setup(i => i.IngestFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult { Success = true, ChunkCount = 5 });

        var progressReports = new List<IndexingProgressInfo>();
        var progress = new Progress<IndexingProgressInfo>(info => progressReports.Add(info));

        // Act
        await _sut.ReindexAllAsync(progress);

        // Assert
        progressReports.Should().HaveCountGreaterThan(0);
        progressReports.Last().IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task ReindexAllAsync_ContinuesOnPartialFailure()
    {
        // Arrange
        var docs = new List<Document>
        {
            new() { Id = Guid.NewGuid(), FilePath = "/doc1.md" },
            new() { Id = Guid.NewGuid(), FilePath = "/doc2.md" }
        };
        _docRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(docs);
        _ingestionMock.SetupSequence(i => i.IngestFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IngestionResult { Success = true, ChunkCount = 5 })
            .ReturnsAsync(new IngestionResult { Success = false, ErrorMessage = "API error" });

        // Act
        var result = await _sut.ReindexAllAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.DocumentsAffected.Should().Be(1); // One succeeded
        result.ErrorMessage.Should().Contain("1 document(s) failed");
    }
}
```

---

## 5. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Information | "Re-indexing document: {Path}" | Before re-index |
| Information | "Re-indexed document: {Path}, {ChunkCount} chunks" | After success |
| Information | "Removing document from index: {Path}" | Before remove |
| Information | "Removed document from index: {Path}, {ChunkCount} chunks deleted" | After remove |
| Information | "Starting full re-index of {Count} documents" | Before re-index all |
| Information | "Full re-index complete: {Count} docs, {Chunks} chunks, {Errors} errors" | After re-index all |
| Warning | "Failed to re-index: {Path}" | Partial failure |
| Error | "Failed to re-index document {Id}" | Exception |
| Error | "Failed to remove document {Id}" | Exception |
| Debug | "Re-index cancelled: {Path}" | Cancellation |

---

## 6. File Locations

| File | Path |
| :--- | :--- |
| IIndexManagementService | `src/Lexichord.Abstractions/Contracts/IIndexManagementService.cs` |
| IndexOperationResult | `src/Lexichord.Modules.RAG/Models/IndexOperationResult.cs` |
| IndexingProgressInfo | `src/Lexichord.Modules.RAG/Models/IndexingProgressInfo.cs` |
| IndexManagementService | `src/Lexichord.Modules.RAG/Services/IndexManagementService.cs` |
| Events | `src/Lexichord.Modules.RAG/Events/IndexOperationEvents.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Services/IndexManagementServiceTests.cs` |

---

## 7. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Re-index Document triggers ingestion | [ ] |
| 2 | Re-index returns success with chunk count | [ ] |
| 3 | Remove from Index deletes chunks first | [ ] |
| 4 | Remove from Index deletes document record | [ ] |
| 5 | Remove shows confirmation dialog | [ ] |
| 6 | Re-index All processes all documents | [ ] |
| 7 | Re-index All shows confirmation dialog | [ ] |
| 8 | Re-index All reports progress | [ ] |
| 9 | Operations can be cancelled | [ ] |
| 10 | Events published for telemetry | [ ] |
| 11 | Partial failures handled gracefully | [ ] |
| 12 | All unit tests pass | [ ] |

---

## 8. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
