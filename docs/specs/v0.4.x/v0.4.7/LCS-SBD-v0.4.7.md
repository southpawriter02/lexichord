# LCS-SBD-047: Scope Breakdown — The Index Manager

## Document Control

| Field            | Value                                     |
| :--------------- | :---------------------------------------- |
| **Document ID**  | LCS-SBD-047                               |
| **Version**      | v0.4.7                                    |
| **Codename**     | The Index Manager (Corpus Administration) |
| **Status**       | Draft                                     |
| **Last Updated** | 2026-01-27                                |
| **Owner**        | Lead Architect                            |

---

## 1. Executive Summary

### 1.1 Purpose

v0.4.7 delivers user-facing tools for managing the indexed document corpus. Users can view indexing status, manually trigger re-indexing, monitor progress, and handle indexing failures through a dedicated settings interface.

### 1.2 Success Metrics

| Metric                                                | Target |
| :---------------------------------------------------- | :----- |
| Index status visible within 100ms of opening settings | 100%   |
| Re-index action starts within 500ms of click          | 100%   |
| Progress updates display at least every 1 second      | 100%   |
| Failed documents show actionable error messages       | 100%   |

### 1.3 Estimated Effort

| Sub-Part  | Description              | Hours  |
| :-------- | :----------------------- | :----- |
| v0.4.7a   | Index Status View        | 8      |
| v0.4.7b   | Manual Indexing Controls | 6      |
| v0.4.7c   | Indexing Progress        | 6      |
| v0.4.7d   | Indexing Errors          | 5      |
| **Total** |                          | **25** |

---

## 2. Sub-Part Specifications

### 2.1 v0.4.7a: Index Status View

**Goal:** Display comprehensive status of all indexed documents.

**Tasks:**

1. Create `IndexStatusView.axaml` UserControl
2. Create `IndexStatusViewModel` with document collection
3. Implement `IIndexStatusService` for aggregating index statistics
4. Display document list with status indicators
5. Show summary statistics (total docs, chunks, storage)
6. Register view in Settings dialog

**Definition of Done:**

- [ ] View displays all indexed documents
- [ ] Status badges show Indexed/Pending/Failed/Stale
- [ ] Statistics refresh on view activation
- [ ] Unit tests cover ViewModel logic

---

### 2.2 v0.4.7b: Manual Indexing Controls

**Goal:** Allow users to manually manage document indexing.

**Tasks:**

1. Add "Re-index Document" button with command
2. Add "Remove from Index" button with confirmation
3. Add "Re-index All" button with safety confirmation
4. Implement `IIndexManagementService` interface
5. Wire commands to ingestion service

**Definition of Done:**

- [ ] Re-index button triggers document re-processing
- [ ] Remove button deletes document and chunks
- [ ] Re-index All processes entire workspace
- [ ] Confirmation dialogs prevent accidental operations
- [ ] Unit tests verify command execution

---

### 2.3 v0.4.7c: Indexing Progress

**Goal:** Show real-time progress during indexing operations.

**Tasks:**

1. Create `IndexingProgressView.axaml` toast overlay
2. Create `IndexingProgressViewModel` with progress state
3. Bind to `IngestionService.ProgressChanged` event
4. Display current file and progress bar
5. Implement cancellation support
6. Auto-dismiss on completion

**Definition of Done:**

- [ ] Progress overlay appears during indexing
- [ ] Current file name displays correctly
- [ ] Progress bar reflects batch completion
- [ ] Cancel button stops operation
- [ ] Toast auto-hides after completion

---

### 2.4 v0.4.7d: Indexing Errors

**Goal:** Handle and display indexing failures with recovery options.

**Tasks:**

1. Add `error_message` column to documents table via migration
2. Create `IndexingErrorInfo` record
3. Display failed documents with error details
4. Implement "Retry" action for failed documents
5. Log errors with full context

**Definition of Done:**

- [ ] Failed documents show in status view with error badge
- [ ] Error message displays on hover/expand
- [ ] Retry button attempts re-indexing
- [ ] Errors logged at Warning level with stack trace
- [ ] Unit tests verify error handling

---

## 3. Implementation Checklist

| #   | Task                                             | Sub-Part | Status |
| :-- | :----------------------------------------------- | :------- | :----- |
| 1   | Create IndexStatusView.axaml                     | v0.4.7a  | [ ]    |
| 2   | Create IndexStatusViewModel                      | v0.4.7a  | [ ]    |
| 3   | Implement IIndexStatusService                    | v0.4.7a  | [ ]    |
| 4   | Add status badges (Indexed/Pending/Failed/Stale) | v0.4.7a  | [ ]    |
| 5   | Display summary statistics                       | v0.4.7a  | [ ]    |
| 6   | Register in Settings dialog                      | v0.4.7a  | [ ]    |
| 7   | Add Re-index Document command                    | v0.4.7b  | [ ]    |
| 8   | Add Remove from Index command                    | v0.4.7b  | [ ]    |
| 9   | Add Re-index All command                         | v0.4.7b  | [ ]    |
| 10  | Implement confirmation dialogs                   | v0.4.7b  | [ ]    |
| 11  | Create IIndexManagementService                   | v0.4.7b  | [ ]    |
| 12  | Create IndexingProgressView.axaml                | v0.4.7c  | [ ]    |
| 13  | Create IndexingProgressViewModel                 | v0.4.7c  | [ ]    |
| 14  | Bind to ProgressChanged event                    | v0.4.7c  | [ ]    |
| 15  | Implement cancellation support                   | v0.4.7c  | [ ]    |
| 16  | Add auto-dismiss behavior                        | v0.4.7c  | [ ]    |
| 17  | Add error_message migration                      | v0.4.7d  | [ ]    |
| 18  | Create IndexingErrorInfo record                  | v0.4.7d  | [ ]    |
| 19  | Display failed documents                         | v0.4.7d  | [ ]    |
| 20  | Implement Retry action                           | v0.4.7d  | [ ]    |
| 21  | Add error logging                                | v0.4.7d  | [ ]    |
| 22  | Write unit tests                                 | All      | [ ]    |

---

## 4. Dependency Matrix

### 4.1 Required Interfaces (Upstream)

| Interface             | Source  | Usage                       |
| :-------------------- | :------ | :-------------------------- |
| `IIngestionService`   | v0.4.2a | Trigger indexing operations |
| `IDocumentRepository` | v0.4.1c | Query document status       |
| `IChunkRepository`    | v0.4.1c | Get chunk counts            |
| `ISettingsPage`       | v0.1.6a | Register status view        |
| `IDialogService`      | v0.2.5b | Confirmation dialogs        |
| `IFileHashService`    | v0.4.2b | Stale detection (hash)      |
| `IMediator`           | v0.0.7a | Publish/subscribe events    |

### 4.2 New Interfaces Introduced

| Interface                 | Purpose                    |
| :------------------------ | :------------------------- |
| `IIndexStatusService`     | Aggregate index statistics |
| `IIndexManagementService` | Manual indexing operations |

### 4.3 NuGet Packages

| Package                 | Version | Purpose                           |
| :---------------------- | :------ | :-------------------------------- |
| `CommunityToolkit.Mvvm` | 8.x     | MVVM infrastructure               |
| `FluentMigrator`        | 3.x     | Schema migration for error column |

---

## 5. Risks & Mitigations

| Risk                                   | Impact | Mitigation                         |
| :------------------------------------- | :----- | :--------------------------------- |
| Re-index All overwhelms API            | High   | Rate limiting, confirmation dialog |
| Progress events lost during heavy load | Medium | Throttle UI updates to 1Hz         |
| Users accidentally delete documents    | High   | Two-step confirmation dialog       |
| Stale status after background indexing | Medium | Refresh on ProgressChanged         |

---

## 6. User Stories

| ID       | Role   | Story                                    | Priority    |
| :------- | :----- | :--------------------------------------- | :---------- |
| US-047-1 | Writer | View which documents are indexed         | Must Have   |
| US-047-2 | Writer | See why a document failed to index       | Must Have   |
| US-047-3 | Writer | Manually re-index a changed document     | Should Have |
| US-047-4 | Writer | Remove a document from the index         | Should Have |
| US-047-5 | Writer | Monitor progress during batch indexing   | Should Have |
| US-047-6 | Writer | Cancel a long-running indexing operation | Could Have  |

---

## 7. Use Cases

### UC-047-1: View Index Status

**Preconditions:** User has WriterPro license, documents have been indexed.

**Flow:**

1. User opens Settings dialog
2. User navigates to "Index" tab
3. System displays list of indexed documents
4. System shows status badge for each document
5. System shows summary statistics

**Postconditions:** User understands current index state.

---

### UC-047-2: Re-index Failed Document

**Preconditions:** A document has Failed status.

**Flow:**

1. User views Index Status
2. User identifies document with Failed badge
3. User clicks "Retry" button
4. System shows progress toast
5. System re-processes document
6. System updates status to Indexed or Failed

**Postconditions:** Document is re-processed with updated status.

---

## 8. UI/UX Specifications

### 8.1 Index Status View

```
┌─────────────────────────────────────────────────────────────────┐
│ Index Status                                        [Re-index All]
├─────────────────────────────────────────────────────────────────┤
│ Summary: 42 documents • 1,247 chunks • ~12.4 MB                 │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📄 chapter-01.md              [Indexed]    2h ago    [⟳][✕] │ │
│ │ 📄 chapter-02.md              [Indexed]    2h ago    [⟳][✕] │ │
│ │ 📄 notes.md                   [Stale]      1d ago    [⟳][✕] │ │
│ │ 📄 research.md                [Failed]     3h ago    [↻][✕] │ │
│ │    └─ Error: Rate limit exceeded                            │ │
│ │ 📄 outline.md                 [Pending]    --        [  ][✕] │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Progress Toast

```
┌──────────────────────────────────────────┐
│ Indexing documents...           [Cancel] │
│ Processing: chapter-03.md                │
│ ████████████░░░░░░░░  12/20 (60%)       │
└──────────────────────────────────────────┘
```

### 8.3 Status Badge Colors

| Status  | Color            | Meaning                     |
| :------ | :--------------- | :-------------------------- |
| Indexed | Green (#22c55e)  | Successfully indexed        |
| Pending | Blue (#4a9eff)   | Queued for indexing         |
| Stale   | Yellow (#eab308) | File changed since indexing |
| Failed  | Red (#ef4444)    | Indexing failed             |

---

## 9. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7")]
public class IndexStatusViewModelTests
{
    [Fact]
    public async Task LoadDocumentsAsync_PopulatesCollection()
    {
        // Arrange
        var mockRepo = new Mock<IDocumentRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Document> { CreateDocument("test.md") });

        var sut = new IndexStatusViewModel(mockRepo.Object, ...);

        // Act
        await sut.LoadDocumentsAsync();

        // Assert
        sut.Documents.Should().HaveCount(1);
    }

    [Fact]
    public void Statistics_CalculatesCorrectly()
    {
        var sut = CreateViewModelWithDocuments(5, totalChunks: 100);

        sut.DocumentCount.Should().Be(5);
        sut.ChunkCount.Should().Be(100);
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7b")]
public class IndexManagementServiceTests
{
    [Fact]
    public async Task ReindexDocumentAsync_CallsIngestionService()
    {
        var mockIngestion = new Mock<IIngestionService>();
        var sut = new IndexManagementService(mockIngestion.Object, ...);

        await sut.ReindexDocumentAsync("/docs/test.md");

        mockIngestion.Verify(i => i.IngestFileAsync("/docs/test.md", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDocumentAsync_DeletesDocumentAndChunks()
    {
        var mockDocRepo = new Mock<IDocumentRepository>();
        var sut = new IndexManagementService(..., mockDocRepo.Object);

        await sut.RemoveDocumentAsync(Guid.NewGuid());

        mockDocRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
    }
}
```

---

## 10. Observability & Logging

| Level       | Source                    | Message                                              |
| :---------- | :------------------------ | :--------------------------------------------------- |
| Information | IndexStatusViewModel      | "Loaded {Count} indexed documents"                   |
| Information | IndexManagementService    | "Re-indexing document: {Path}"                       |
| Information | IndexManagementService    | "Removed document from index: {Id}"                  |
| Information | IndexManagementService    | "Starting full re-index of {Count} documents"        |
| Warning     | IndexingErrorHandler      | "Document indexing failed: {Path}, Error: {Message}" |
| Debug       | IndexingProgressViewModel | "Progress update: {Current}/{Total}"                 |

---

## 11. Acceptance Criteria (QA)

| #   | Criterion                                     | Status |
| :-- | :-------------------------------------------- | :----- |
| 1   | Index Status View shows all documents         | [ ]    |
| 2   | Status badges display correct state           | [ ]    |
| 3   | Summary statistics are accurate               | [ ]    |
| 4   | Re-index Document triggers ingestion          | [ ]    |
| 5   | Remove from Index deletes document and chunks | [ ]    |
| 6   | Re-index All processes all documents          | [ ]    |
| 7   | Confirmation dialogs prevent accidents        | [ ]    |
| 8   | Progress toast shows during operations        | [ ]    |
| 9   | Current file name updates during progress     | [ ]    |
| 10  | Cancel button stops operation                 | [ ]    |
| 11  | Failed documents show error message           | [ ]    |
| 12  | Retry action re-processes failed document     | [ ]    |
| 13  | All unit tests pass                           | [ ]    |

---

## 12. Verification Commands

```bash
# Run v0.4.7 unit tests
dotnet test --filter "Feature=v0.4.7"

# Verify migration
dotnet ef migrations list | grep -i "error_message"

# Build module
dotnet build src/Lexichord.Modules.RAG/Lexichord.Modules.RAG.csproj
```

---

## 13. Deferred Features

| Feature                   | Reason                        | Target Version |
| :------------------------ | :---------------------------- | :------------- |
| Index scheduling          | Requires background scheduler | v0.5.x         |
| Per-folder indexing rules | Complexity                    | v0.5.x         |
| Index size limits         | Requires quota system         | v0.6.x         |
| Export/import index       | Low priority                  | v0.7.x         |

---

## 14. Related Documents

| Document                                    | Relationship                  |
| :------------------------------------------ | :---------------------------- |
| [LCS-DES-047-INDEX](./LCS-DES-047-INDEX.md) | Design specification index    |
| [LCS-SBD-046](../v0.4.6/LCS-SBD-046.md)     | Predecessor (Reference Panel) |
| [LCS-SBD-048](../v0.4.8/LCS-SBD-048.md)     | Successor (Hardening)         |
| [roadmap-v0.4.x](../roadmap-v0.4.x.md)      | Version roadmap               |

---

## 15. Changelog Entry

```markdown
### v0.4.7 - The Index Manager

#### Added

- Index Status View in Settings dialog showing all indexed documents
- Manual indexing controls (Re-index, Remove, Re-index All)
- Indexing progress toast with current file and progress bar
- Error display for failed documents with retry action
- `IIndexStatusService` interface for index statistics
- `IIndexManagementService` interface for manual operations

#### Changed

- Documents table now includes `error_message` column

#### Fixed

- N/A (new feature)
```

---

## 16. Revision History

| Version | Date       | Author         | Changes       |
| :------ | :--------- | :------------- | :------------ |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft |

---
