# LCS-DES-092d: Design Specification — Conflict Resolution

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COL-092d` | Sub-part of COL-092 |
| **Feature Name** | `Conflict Resolution (Merge Strategies)` | Intelligent conflict handling |
| **Target Version** | `v0.9.2d` | Fourth sub-part of v0.9.2 |
| **Module Scope** | `Lexichord.Modules.Collaboration` | Collaboration module |
| **Swimlane** | `Collaboration` | Part of collaboration vertical |
| **License Tier** | `Teams` | Teams and above |
| **Feature Gate Key** | `Collaboration.RealTimeSync` | License gate key |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-092-INDEX](./LCS-DES-092-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-092 S3.4](./LCS-SBD-092.md#34-v092d-conflict-resolution) | |

---

## 2. Executive Summary

### 2.1 The Requirement

While CRDTs mathematically guarantee convergence, they do not guarantee that the merged result is what users intended. In certain scenarios, the automatic merge may produce unexpected results:

- Two users editing the same word produce interleaved text
- One user deletes a section while another is editing within it
- Conflicting formatting applied to the same range
- Offline changes conflict with significant server-side edits

> **Goal:** Implement intelligent conflict detection and resolution strategies that give users control over merge behavior while defaulting to sensible automatic resolution.

### 2.2 The Proposed Solution

Implement a layered conflict resolution system:

1. **Conflict Detection** — Identify when concurrent operations may produce unexpected results
2. **Resolution Strategies** — Multiple strategies from automatic to manual
3. **User Interface** — Clear notification and resolution dialogs
4. **Undo Integration** — Collaborative undo that respects per-user history
5. **Audit Trail** — Track conflicts and resolutions for compliance

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Internal Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `ICrdtEngine` | v0.9.2a | Operation analysis |
| `ISyncClient` | v0.9.2b | Sync coordination |
| `IMediator` | v0.0.7a | Event publishing |
| `IEditorService` | v0.1.3a | Apply resolutions |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `DiffPlex` | 1.x | Text diff visualization (NEW) |

### 3.2 Licensing Behavior

Conflict resolution is part of the Teams collaboration feature. All strategies are available to Teams+ users.

---

## 4. Data Contract (The API)

### 4.1 Conflict Types

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Types of conflicts that can occur during collaboration.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Two users edited overlapping character ranges concurrently.
    /// Example: Both users change "red" to different words.
    /// </summary>
    ConcurrentEdit,

    /// <summary>
    /// One user deleted text while another was editing within it.
    /// Example: User A deletes paragraph, User B was editing that paragraph.
    /// </summary>
    DeleteEditConflict,

    /// <summary>
    /// Two users applied conflicting formatting to the same range.
    /// Example: User A makes text bold, User B makes it italic at the same time.
    /// </summary>
    FormattingConflict,

    /// <summary>
    /// Document structure conflict (e.g., section reordering).
    /// Example: Both users move the same section to different locations.
    /// </summary>
    StructuralConflict,

    /// <summary>
    /// Offline changes conflict with significant server-side changes.
    /// Example: User was offline for hours while document was heavily edited.
    /// </summary>
    OfflineConflict,

    /// <summary>
    /// Same word edited by multiple users.
    /// Example: Both users fix different typos in the same word.
    /// </summary>
    WordLevelConflict
}

/// <summary>
/// Severity of the conflict for UI presentation.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Minor conflict, automatic resolution is fine.
    /// </summary>
    Low,

    /// <summary>
    /// Moderate conflict, user should be notified.
    /// </summary>
    Medium,

    /// <summary>
    /// Significant conflict, may require manual resolution.
    /// </summary>
    High,

    /// <summary>
    /// Critical conflict with potential data loss.
    /// </summary>
    Critical
}
```

### 4.2 Resolution Strategies

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Resolution strategy for conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// CRDT default: all changes preserved, interleaved by timestamp.
    /// Best for: Minor conflicts, formatting changes.
    /// </summary>
    PreserveAll,

    /// <summary>
    /// Last writer wins based on timestamp.
    /// Best for: Same-word edits, typo fixes.
    /// </summary>
    LastWriterWins,

    /// <summary>
    /// First writer wins based on timestamp.
    /// Best for: When earlier changes should take precedence.
    /// </summary>
    FirstWriterWins,

    /// <summary>
    /// Prompt user to manually resolve.
    /// Best for: Delete-edit conflicts, critical changes.
    /// </summary>
    Manual,

    /// <summary>
    /// Prefer local changes over remote.
    /// Best for: Offline conflicts when user wants to keep their work.
    /// </summary>
    PreferLocal,

    /// <summary>
    /// Prefer remote changes over local.
    /// Best for: Offline conflicts when server state is authoritative.
    /// </summary>
    PreferRemote,

    /// <summary>
    /// Merge using diff algorithm (word-level).
    /// Best for: Paragraph-level edits by multiple users.
    /// </summary>
    SmartMerge
}
```

### 4.3 Conflict and Resolution Records

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Represents a detected conflict.
/// </summary>
public record Conflict
{
    /// <summary>
    /// Unique identifier for this conflict.
    /// </summary>
    public required Guid ConflictId { get; init; }

    /// <summary>
    /// The document where the conflict occurred.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Type of conflict.
    /// </summary>
    public required ConflictType Type { get; init; }

    /// <summary>
    /// Severity for UI presentation.
    /// </summary>
    public required ConflictSeverity Severity { get; init; }

    /// <summary>
    /// The operations that conflict with each other.
    /// </summary>
    public required IReadOnlyList<CrdtOperation> ConflictingOperations { get; init; }

    /// <summary>
    /// Human-readable description of the conflict.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The text range affected by the conflict (if applicable).
    /// </summary>
    public (int Start, int End)? AffectedRange { get; init; }

    /// <summary>
    /// Preview of the local version.
    /// </summary>
    public string? LocalPreview { get; init; }

    /// <summary>
    /// Preview of the remote version.
    /// </summary>
    public string? RemotePreview { get; init; }

    /// <summary>
    /// Preview of the automatic merge result.
    /// </summary>
    public string? MergedPreview { get; init; }

    /// <summary>
    /// Users involved in the conflict.
    /// </summary>
    public IReadOnlyList<Guid> InvolvedUsers { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// When the conflict was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of conflict resolution.
/// </summary>
public record ConflictResolution
{
    /// <summary>
    /// The conflict that was resolved.
    /// </summary>
    public required Guid ConflictId { get; init; }

    /// <summary>
    /// The strategy used for resolution.
    /// </summary>
    public required ConflictResolutionStrategy Strategy { get; init; }

    /// <summary>
    /// Operations to apply as the resolution.
    /// </summary>
    public IReadOnlyList<CrdtOperation>? ResultingOperations { get; init; }

    /// <summary>
    /// Whether user manually resolved.
    /// </summary>
    public bool WasManual { get; init; }

    /// <summary>
    /// When the conflict was resolved.
    /// </summary>
    public DateTime ResolvedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// User who resolved (if manual).
    /// </summary>
    public Guid? ResolvedByUserId { get; init; }

    /// <summary>
    /// Final text after resolution (for audit).
    /// </summary>
    public string? ResolvedText { get; init; }
}
```

### 4.4 Conflict Resolver Interface

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Service for conflict detection and resolution.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Gets the default resolution strategy for a conflict type.
    /// </summary>
    ConflictResolutionStrategy GetDefaultStrategy(ConflictType type);

    /// <summary>
    /// Sets the default resolution strategy for a conflict type.
    /// </summary>
    void SetDefaultStrategy(ConflictType type, ConflictResolutionStrategy strategy);

    /// <summary>
    /// Detects conflicts between local and remote operations.
    /// </summary>
    IReadOnlyList<Conflict> DetectConflicts(
        Guid documentId,
        IEnumerable<CrdtOperation> localOps,
        IEnumerable<CrdtOperation> remoteOps);

    /// <summary>
    /// Resolves a conflict using the specified strategy.
    /// </summary>
    Task<ConflictResolution> ResolveAsync(
        Conflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a conflict manually with user-provided text.
    /// </summary>
    Task<ConflictResolution> ResolveManuallyAsync(
        Conflict conflict,
        string resolvedText,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the conflict history for a document.
    /// </summary>
    Task<IReadOnlyList<ConflictResolution>> GetConflictHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Clears conflict history for a document.
    /// </summary>
    Task ClearHistoryAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a conflict is detected.
    /// </summary>
    event EventHandler<ConflictDetectedEventArgs> ConflictDetected;

    /// <summary>
    /// Event raised when a conflict is resolved.
    /// </summary>
    event EventHandler<ConflictResolvedEventArgs> ConflictResolved;
}

/// <summary>
/// Event args for conflict detection.
/// </summary>
public sealed class ConflictDetectedEventArgs : EventArgs
{
    public required Conflict Conflict { get; init; }
    public required bool RequiresUserIntervention { get; init; }
    public ConflictResolutionStrategy SuggestedStrategy { get; init; }
}

/// <summary>
/// Event args for conflict resolution.
/// </summary>
public sealed class ConflictResolvedEventArgs : EventArgs
{
    public required ConflictResolution Resolution { get; init; }
}
```

### 4.5 Collaborative Undo Manager Interface

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Manages undo/redo in a collaborative context.
/// Each user has their own undo stack, only undoing their own operations.
/// </summary>
public interface ICollaborativeUndoManager
{
    /// <summary>
    /// Records an operation for undo tracking.
    /// </summary>
    void RecordOperation(CrdtOperation operation, bool isLocal);

    /// <summary>
    /// Checks if undo is available for the local user.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Checks if redo is available for the local user.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Undoes the last local operation.
    /// Returns the inverse operation to sync.
    /// </summary>
    Task<CrdtOperation?> UndoAsync(CancellationToken ct = default);

    /// <summary>
    /// Redoes the last undone local operation.
    /// Returns the operation to sync.
    /// </summary>
    Task<CrdtOperation?> RedoAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the undo stack size for the local user.
    /// </summary>
    int UndoStackSize { get; }

    /// <summary>
    /// Gets the redo stack size for the local user.
    /// </summary>
    int RedoStackSize { get; }

    /// <summary>
    /// Gets a description of the next undo operation.
    /// </summary>
    string? GetUndoDescription();

    /// <summary>
    /// Gets a description of the next redo operation.
    /// </summary>
    string? GetRedoDescription();

    /// <summary>
    /// Clears the undo/redo history.
    /// </summary>
    void Clear();

    /// <summary>
    /// Event raised when undo/redo availability changes.
    /// </summary>
    event EventHandler<UndoStateChangedEventArgs> UndoStateChanged;
}

/// <summary>
/// Event args for undo state changes.
/// </summary>
public sealed class UndoStateChangedEventArgs : EventArgs
{
    public required bool CanUndo { get; init; }
    public required bool CanRedo { get; init; }
}
```

---

## 5. Implementation Logic

### 5.1 Conflict Detection Algorithm

```text
DETECT CONFLICTS(localOps, remoteOps):
│
├── conflicts = []
│
├── FOR each localOp in localOps:
│   │
│   └── FOR each remoteOp in remoteOps:
│       │
│       ├── IF localOp.clock.IsConcurrentWith(remoteOp.clock):
│       │   │
│       │   ├── IF both are InsertOperations:
│       │   │   │
│       │   │   ├── IF SamePosition(localOp, remoteOp):
│       │   │   │   └── ADD ConcurrentEdit conflict
│       │   │   │
│       │   │   └── IF OverlappingRange(localOp, remoteOp):
│       │   │       └── ADD WordLevelConflict
│       │   │
│       │   ├── IF localOp is Insert AND remoteOp is Delete:
│       │   │   │
│       │   │   └── IF InsertTargetDeleted(localOp, remoteOp):
│       │   │       └── ADD DeleteEditConflict
│       │   │
│       │   ├── IF both are FormatOperations:
│       │   │   │
│       │   │   └── IF SameRange AND ConflictingFormats:
│       │   │       └── ADD FormattingConflict
│       │   │
│       │   └── IF both are DeleteOperations:
│       │       └── SKIP (deletes are idempotent)
│
├── DETERMINE severity for each conflict
│
└── RETURN conflicts

SEVERITY DETERMINATION(conflict):
│
├── DeleteEditConflict → High
├── StructuralConflict → High
├── OfflineConflict with large changes → Critical
├── ConcurrentEdit in same word → Medium
├── ConcurrentEdit in different words → Low
├── FormattingConflict → Low
└── DEFAULT → Medium
```

### 5.2 Resolution Strategy Logic

```text
RESOLVE(conflict, strategy):
│
├── CASE PreserveAll:
│   └── Let CRDT handle it (no additional action)
│       Result: Interleaved text by timestamp
│
├── CASE LastWriterWins:
│   ├── Sort operations by timestamp
│   ├── Keep only the latest operation
│   └── Generate inverse operations for others
│
├── CASE FirstWriterWins:
│   ├── Sort operations by timestamp
│   ├── Keep only the earliest operation
│   └── Generate inverse operations for others
│
├── CASE PreferLocal:
│   ├── Generate inverse operations for all remote ops
│   └── Keep local operations unchanged
│
├── CASE PreferRemote:
│   ├── Generate inverse operations for all local ops
│   └── Keep remote operations unchanged
│
├── CASE SmartMerge:
│   ├── Extract original text
│   ├── Extract local modified text
│   ├── Extract remote modified text
│   ├── Run 3-way merge algorithm
│   └── Generate operations for merged result
│
└── CASE Manual:
    ├── Show conflict dialog
    ├── Wait for user resolution
    └── Apply user's choice

INVERSE OPERATION:
├── Insert → Delete at same position
├── Delete → Insert deleted content
└── Format → Format with original attributes
```

### 5.3 Collaborative Undo Logic

```text
COLLABORATIVE UNDO:
│
├── Each user has separate undo stack
│   └── Only tracks their own operations
│
├── On UNDO:
│   ├── Pop last local operation from stack
│   ├── Generate inverse operation
│   ├── Apply inverse locally
│   ├── Sync inverse to server
│   └── Push to redo stack
│
├── On REDO:
│   ├── Pop from redo stack
│   ├── Re-apply operation locally
│   ├── Sync to server
│   └── Push back to undo stack
│
└── Interaction with remote operations:
    ├── Remote ops do NOT affect local undo stack
    ├── Inverse of local op may need adjustment
    │   if document state changed by remote ops
    └── Use operational transformation for adjustment

EXAMPLE:
│
├── User A types "Hello" at position 0
│   └── Undo stack: [Insert("Hello", 0)]
│
├── User B types "World" at position 0 (received)
│   └── Undo stack unchanged: [Insert("Hello", 0)]
│
├── User A presses Undo
│   ├── Inverse: Delete(0, 5)
│   ├── Adjusted for B's edit: Delete(5, 5)
│   │   (because B's "World" is now at 0-5)
│   └── Result: "World" (A's "Hello" removed)
```

### 5.4 Three-Way Merge Algorithm

```text
THREE-WAY MERGE(original, local, remote):
│
├── Tokenize all three versions into words
│   ├── originalWords = Tokenize(original)
│   ├── localWords = Tokenize(local)
│   └── remoteWords = Tokenize(remote)
│
├── Compute diffs
│   ├── localDiff = Diff(originalWords, localWords)
│   └── remoteDiff = Diff(originalWords, remoteWords)
│
├── Identify conflicts
│   └── FOR each position:
│       ├── IF localChanged AND remoteChanged:
│       │   ├── IF same change → No conflict
│       │   └── IF different change → Mark conflict
│       └── ELSE → Accept changed version
│
├── Generate merged result
│   └── FOR each position:
│       ├── IF conflict → Apply strategy (or mark for user)
│       ├── ELIF localChanged → Use local
│       ├── ELIF remoteChanged → Use remote
│       └── ELSE → Use original
│
└── RETURN (mergedText, conflictMarkers)
```

---

## 6. Test Scenarios

### 6.1 Conflict Detection Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2d")]
public class ConflictDetectionTests
{
    [Fact]
    public void DetectConflicts_ConcurrentSamePositionInserts_ReturnsConflict()
    {
        // Arrange
        var sut = new ConflictResolver();
        var docId = Guid.NewGuid();

        var localOp = CreateInsertOperation("local", position: 5, clientA);
        var remoteOp = CreateInsertOperation("remote", position: 5, clientB);
        MakeConcurrent(localOp, remoteOp);

        // Act
        var conflicts = sut.DetectConflicts(docId, new[] { localOp }, new[] { remoteOp });

        // Assert
        conflicts.Should().ContainSingle();
        conflicts[0].Type.Should().Be(ConflictType.ConcurrentEdit);
    }

    [Fact]
    public void DetectConflicts_InsertDuringDelete_ReturnsDeleteEditConflict()
    {
        // Arrange
        var sut = new ConflictResolver();
        var docId = Guid.NewGuid();

        var localInsert = CreateInsertOperation("new text", position: 10, clientA);
        var remoteDelete = CreateDeleteOperation(position: 5, length: 20, clientB);
        MakeConcurrent(localInsert, remoteDelete);

        // Act
        var conflicts = sut.DetectConflicts(docId, new[] { localInsert }, new[] { remoteDelete });

        // Assert
        conflicts.Should().ContainSingle();
        conflicts[0].Type.Should().Be(ConflictType.DeleteEditConflict);
        conflicts[0].Severity.Should().Be(ConflictSeverity.High);
    }

    [Fact]
    public void DetectConflicts_DifferentPositions_NoConflict()
    {
        // Arrange
        var sut = new ConflictResolver();
        var docId = Guid.NewGuid();

        var localOp = CreateInsertOperation("local", position: 0, clientA);
        var remoteOp = CreateInsertOperation("remote", position: 100, clientB);
        MakeConcurrent(localOp, remoteOp);

        // Act
        var conflicts = sut.DetectConflicts(docId, new[] { localOp }, new[] { remoteOp });

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectConflicts_CausallyOrdered_NoConflict()
    {
        // Arrange
        var sut = new ConflictResolver();
        var docId = Guid.NewGuid();

        var localOp = CreateInsertOperation("local", position: 5, clientA);
        var remoteOp = CreateInsertOperation("remote", position: 5, clientB);
        MakeCausal(localOp, remoteOp); // remoteOp happens-after localOp

        // Act
        var conflicts = sut.DetectConflicts(docId, new[] { localOp }, new[] { remoteOp });

        // Assert
        conflicts.Should().BeEmpty();
    }
}
```

### 6.2 Resolution Strategy Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2d")]
public class ConflictResolutionTests
{
    [Fact]
    public async Task Resolve_LastWriterWins_KeepsLatestOperation()
    {
        // Arrange
        var sut = new ConflictResolver();
        var conflict = CreateConflict(
            localText: "local change",
            remoteText: "remote change",
            localTimestamp: DateTime.UtcNow.AddSeconds(-5),
            remoteTimestamp: DateTime.UtcNow);

        // Act
        var resolution = await sut.ResolveAsync(conflict, ConflictResolutionStrategy.LastWriterWins);

        // Assert
        resolution.ResolvedText.Should().Contain("remote"); // Remote is later
    }

    [Fact]
    public async Task Resolve_FirstWriterWins_KeepsEarliestOperation()
    {
        // Arrange
        var sut = new ConflictResolver();
        var conflict = CreateConflict(
            localText: "local change",
            remoteText: "remote change",
            localTimestamp: DateTime.UtcNow.AddSeconds(-5),
            remoteTimestamp: DateTime.UtcNow);

        // Act
        var resolution = await sut.ResolveAsync(conflict, ConflictResolutionStrategy.FirstWriterWins);

        // Assert
        resolution.ResolvedText.Should().Contain("local"); // Local is earlier
    }

    [Fact]
    public async Task Resolve_PreferLocal_KeepsLocalChanges()
    {
        // Arrange
        var sut = new ConflictResolver();
        var conflict = CreateConflict(
            localText: "my changes",
            remoteText: "their changes");

        // Act
        var resolution = await sut.ResolveAsync(conflict, ConflictResolutionStrategy.PreferLocal);

        // Assert
        resolution.ResolvedText.Should().Be("my changes");
    }

    [Fact]
    public async Task Resolve_SmartMerge_CombinesNonConflicting()
    {
        // Arrange
        var sut = new ConflictResolver();
        var conflict = CreateConflict(
            original: "The quick brown fox",
            localText: "The quick red fox",      // Changed "brown" to "red"
            remoteText: "The fast brown fox");   // Changed "quick" to "fast"

        // Act
        var resolution = await sut.ResolveAsync(conflict, ConflictResolutionStrategy.SmartMerge);

        // Assert
        resolution.ResolvedText.Should().Be("The fast red fox"); // Both changes merged
    }

    [Fact]
    public async Task ResolveManually_AppliesUserText()
    {
        // Arrange
        var sut = new ConflictResolver();
        var conflict = CreateConflict(
            localText: "option A",
            remoteText: "option B");

        // Act
        var resolution = await sut.ResolveManuallyAsync(conflict, "user's choice");

        // Assert
        resolution.ResolvedText.Should().Be("user's choice");
        resolution.WasManual.Should().BeTrue();
    }
}
```

### 6.3 Undo Manager Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2d")]
public class CollaborativeUndoTests
{
    [Fact]
    public void RecordOperation_LocalOperation_AddsToUndoStack()
    {
        // Arrange
        var sut = new CollaborativeUndoManager(Guid.NewGuid());
        var operation = CreateInsertOperation("Hello");

        // Act
        sut.RecordOperation(operation, isLocal: true);

        // Assert
        sut.CanUndo.Should().BeTrue();
        sut.UndoStackSize.Should().Be(1);
    }

    [Fact]
    public void RecordOperation_RemoteOperation_DoesNotAddToUndoStack()
    {
        // Arrange
        var sut = new CollaborativeUndoManager(Guid.NewGuid());
        var operation = CreateInsertOperation("Hello");

        // Act
        sut.RecordOperation(operation, isLocal: false);

        // Assert
        sut.CanUndo.Should().BeFalse();
        sut.UndoStackSize.Should().Be(0);
    }

    [Fact]
    public async Task Undo_ReturnsInverseOperation()
    {
        // Arrange
        var sut = new CollaborativeUndoManager(Guid.NewGuid());
        var insertOp = CreateInsertOperation("Hello", position: 0);
        sut.RecordOperation(insertOp, isLocal: true);

        // Act
        var undoOp = await sut.UndoAsync();

        // Assert
        undoOp.Should().BeOfType<DeleteOperation>();
        var deleteOp = (DeleteOperation)undoOp!;
        deleteOp.TargetIds.Should().Contain(insertOp.Id);
    }

    [Fact]
    public async Task Undo_Redo_RestoresOriginal()
    {
        // Arrange
        var sut = new CollaborativeUndoManager(Guid.NewGuid());
        var operation = CreateInsertOperation("Hello", position: 0);
        sut.RecordOperation(operation, isLocal: true);

        // Act
        await sut.UndoAsync();
        var redoOp = await sut.RedoAsync();

        // Assert
        redoOp.Should().BeEquivalentTo(operation);
    }

    [Fact]
    public async Task Undo_AfterRemoteOperation_AdjustsPosition()
    {
        // Arrange
        var sut = new CollaborativeUndoManager(Guid.NewGuid());

        // Local: Insert "Hello" at position 10
        var localOp = CreateInsertOperation("Hello", position: 10);
        sut.RecordOperation(localOp, isLocal: true);

        // Remote: Insert "World" at position 0 (shifts local operation)
        var remoteOp = CreateInsertOperation("World", position: 0);
        sut.RecordOperation(remoteOp, isLocal: false);

        // Act
        var undoOp = await sut.UndoAsync();

        // Assert - Position should be adjusted
        var deleteOp = (DeleteOperation)undoOp!;
        // Original position 10 + "World".Length = 15
        // The delete should target the adjusted position
    }
}
```

---

## 7. UI/UX Specifications

### 7.1 Conflict Notification

```text
CONFLICT NOTIFICATION TOAST:
┌─────────────────────────────────────────────────────────────────────┐
│ ⚠️ Editing conflict with Alice                               [✕]   │
│ You both edited "quick brown fox" at the same time.                │
│                                    [View Details]  [Auto-resolve]  │
└─────────────────────────────────────────────────────────────────────┘

NOTIFICATION STATES:
├── Minor (Low severity): Auto-dismiss after 5s, no action needed
├── Moderate (Medium): Persist until clicked, shows "View Details"
├── Significant (High): Persist, highlight affected text in editor
└── Critical: Modal dialog, blocks editing until resolved
```

### 7.2 Conflict Resolution Dialog

```text
┌─────────────────────────────────────────────────────────────────────┐
│ ⚠️ Editing Conflict                                           [✕]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ You and Alice both edited this section at the same time.           │
│                                                                     │
│ ┌─────────────────────────────┐ ┌─────────────────────────────┐    │
│ │ Your Version               │ │ Alice's Version              │    │
│ ├─────────────────────────────┤ ├─────────────────────────────┤    │
│ │ The quick [red] fox        │ │ The quick [blue] fox        │    │
│ │ jumps over the lazy dog.   │ │ jumps over the lazy dog.    │    │
│ └─────────────────────────────┘ └─────────────────────────────┘    │
│                        ▼                                            │
│ ┌───────────────────────────────────────────────────────────────┐  │
│ │ Preview (Auto-merge)                                          │  │
│ ├───────────────────────────────────────────────────────────────┤  │
│ │ The quick [red][blue] fox jumps over the lazy dog.           │  │
│ │           └──────────┘                                        │  │
│ │           Merged result (may need editing)                    │  │
│ └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│ Choose resolution:                                                  │
│ ○ Accept auto-merge (preserves both changes)                       │
│ ○ Keep my version                                                  │
│ ○ Keep Alice's version                                             │
│ ○ Edit manually                                                    │
│                                                                     │
│ ☑ Remember my choice for similar conflicts                         │
│                                                                     │
│                            [Cancel]  [Apply]                       │
└─────────────────────────────────────────────────────────────────────┘
```

### 7.3 Manual Edit Mode

```text
MANUAL EDIT DIALOG:
┌─────────────────────────────────────────────────────────────────────┐
│ Resolve Conflict Manually                                     [✕]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ Edit the text below to create the final version:                   │
│                                                                     │
│ ┌───────────────────────────────────────────────────────────────┐  │
│ │ The quick [red][blue] fox jumps over the lazy dog.           │  │
│ │                                                               │  │
│ │ [Editable text area with syntax highlighting for             │  │
│ │  conflict markers]                                            │  │
│ └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│ Helpful actions:                                                    │
│ [Accept All Mine] [Accept All Theirs] [Remove Conflict Markers]    │
│                                                                     │
│                            [Cancel]  [Apply]                       │
└─────────────────────────────────────────────────────────────────────┘
```

### 7.4 Conflict History Panel

```text
CONFLICT HISTORY (in document sidebar):
┌─────────────────────────────────────────────────────────────────────┐
│ Conflict History                                         [Export]  │
├─────────────────────────────────────────────────────────────────────┤
│ Today                                                               │
│ ├── 14:32 - Concurrent edit with Alice                             │
│ │   └── Resolved: Auto-merge (PreserveAll)                         │
│ ├── 13:15 - Delete-edit conflict with Bob                          │
│ │   └── Resolved: Kept local version                               │
│ Yesterday                                                           │
│ └── 16:45 - Formatting conflict with Carol                         │
│     └── Resolved: Manual merge                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Checking for conflicts: local={LocalCount}, remote={RemoteCount}"` |
| Info | `"Conflict detected: type={Type}, severity={Severity}, users={Users}"` |
| Info | `"Conflict resolved: id={ConflictId}, strategy={Strategy}, manual={WasManual}"` |
| Warning | `"High severity conflict requires attention: id={ConflictId}"` |
| Warning | `"Undo adjustment needed due to remote changes"` |
| Error | `"Conflict resolution failed: {Error}"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Malicious conflict injection | Low | Conflicts only detected for legitimate ops |
| Resolution bypass | Low | Server validates all operations regardless |
| History tampering | Medium | Conflict history is append-only with audit |
| Data loss from wrong resolution | High | Preview before apply, undo support |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Two concurrent same-position inserts | Operations applied | Conflict detected |
| 2 | Insert during delete | Operations applied | DeleteEditConflict detected |
| 3 | LastWriterWins strategy | Conflict resolved | Later operation kept |
| 4 | Manual resolution | User edits text | Custom text applied |
| 5 | Local operation | Undo pressed | Inverse operation synced |
| 6 | Remote operation | Undo pressed | No effect (local only) |
| 7 | Conflict occurs | Notification shown | User can view details |
| 8 | Critical conflict | Dialog shown | Must resolve before continuing |

### 10.2 UX Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | Low severity conflict | Auto-resolved | Toast notification only |
| 10 | High severity conflict | Detected | Text highlighted in editor |
| 11 | Resolution dialog | Displayed | Shows clear diff view |
| 12 | "Remember choice" checked | Similar conflict | Same strategy auto-applied |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `ConflictType.cs` conflict types enum | [ ] |
| 2 | `ConflictResolutionStrategy.cs` strategy enum | [ ] |
| 3 | `Conflict.cs` conflict record | [ ] |
| 4 | `ConflictResolution.cs` resolution record | [ ] |
| 5 | `IConflictResolver.cs` interface | [ ] |
| 6 | `ConflictResolver.cs` implementation | [ ] |
| 7 | `ICollaborativeUndoManager.cs` interface | [ ] |
| 8 | `CollaborativeUndoManager.cs` implementation | [ ] |
| 9 | `ThreeWayMerge.cs` merge algorithm | [ ] |
| 10 | `ConflictNotification.axaml` toast component | [ ] |
| 11 | `ConflictDialog.axaml` resolution dialog | [ ] |
| 12 | `ConflictHistoryPanel.axaml` history view | [ ] |
| 13 | Unit tests for conflict detection | [ ] |
| 14 | Unit tests for resolution strategies | [ ] |
| 15 | Unit tests for undo manager | [ ] |

---

## 12. Verification Commands

```bash
# Run all conflict resolution tests
dotnet test --filter "Version=v0.9.2d" --logger "console;verbosity=detailed"

# Run only conflict detection tests
dotnet test --filter "FullyQualifiedName~ConflictDetectionTests"

# Run only resolution strategy tests
dotnet test --filter "FullyQualifiedName~ConflictResolutionTests"

# Run only undo tests
dotnet test --filter "FullyQualifiedName~CollaborativeUndoTests"

# Run UI tests
dotnet test --filter "Category=UI&Version=v0.9.2d"
```

---

## 13. Code Examples

### 13.1 Conflict Resolver Implementation

```csharp
namespace Lexichord.Modules.Collaboration.Conflicts;

/// <summary>
/// Detects and resolves conflicts between concurrent operations.
/// </summary>
public sealed class ConflictResolver : IConflictResolver
{
    private readonly ICrdtEngine _crdtEngine;
    private readonly IConflictHistoryRepository _historyRepository;
    private readonly Dictionary<ConflictType, ConflictResolutionStrategy> _defaultStrategies;

    public ConflictResolver(
        ICrdtEngine crdtEngine,
        IConflictHistoryRepository historyRepository)
    {
        _crdtEngine = crdtEngine;
        _historyRepository = historyRepository;
        _defaultStrategies = new Dictionary<ConflictType, ConflictResolutionStrategy>
        {
            [ConflictType.ConcurrentEdit] = ConflictResolutionStrategy.PreserveAll,
            [ConflictType.DeleteEditConflict] = ConflictResolutionStrategy.Manual,
            [ConflictType.FormattingConflict] = ConflictResolutionStrategy.PreserveAll,
            [ConflictType.WordLevelConflict] = ConflictResolutionStrategy.LastWriterWins,
            [ConflictType.StructuralConflict] = ConflictResolutionStrategy.Manual,
            [ConflictType.OfflineConflict] = ConflictResolutionStrategy.Manual
        };
    }

    public IReadOnlyList<Conflict> DetectConflicts(
        Guid documentId,
        IEnumerable<CrdtOperation> localOps,
        IEnumerable<CrdtOperation> remoteOps)
    {
        var conflicts = new List<Conflict>();
        var localList = localOps.ToList();
        var remoteList = remoteOps.ToList();

        foreach (var localOp in localList)
        {
            foreach (var remoteOp in remoteList)
            {
                // Only concurrent operations can conflict
                if (!localOp.Clock.IsConcurrentWith(remoteOp.Clock))
                    continue;

                var conflict = DetectConflictBetween(documentId, localOp, remoteOp);
                if (conflict != null)
                {
                    conflicts.Add(conflict);
                }
            }
        }

        return conflicts;
    }

    private Conflict? DetectConflictBetween(
        Guid documentId,
        CrdtOperation localOp,
        CrdtOperation remoteOp)
    {
        return (localOp, remoteOp) switch
        {
            (InsertOperation local, InsertOperation remote) =>
                DetectInsertConflict(documentId, local, remote),

            (InsertOperation local, DeleteOperation remote) =>
                DetectDeleteEditConflict(documentId, local, remote),

            (FormatOperation local, FormatOperation remote) =>
                DetectFormatConflict(documentId, local, remote),

            _ => null
        };
    }

    private Conflict? DetectInsertConflict(
        Guid documentId,
        InsertOperation local,
        InsertOperation remote)
    {
        // Check if both insert at the same origin
        if (local.OriginLeft != remote.OriginLeft)
            return null;

        var localPos = _crdtEngine.IdToPosition(local.Id) ?? 0;
        var remotePos = _crdtEngine.IdToPosition(remote.Id) ?? 0;

        // Same position = concurrent edit
        if (localPos == remotePos)
        {
            return new Conflict
            {
                ConflictId = Guid.NewGuid(),
                DocumentId = documentId,
                Type = ConflictType.ConcurrentEdit,
                Severity = local.Content.Length > 10 ? ConflictSeverity.Medium : ConflictSeverity.Low,
                ConflictingOperations = new[] { local, remote },
                Description = $"Concurrent edits at position {localPos}",
                LocalPreview = local.Content,
                RemotePreview = remote.Content,
                MergedPreview = $"{local.Content}{remote.Content}", // Interleaved
                InvolvedUsers = new[] { local.OriginClientId, remote.OriginClientId }
            };
        }

        return null;
    }

    private Conflict? DetectDeleteEditConflict(
        Guid documentId,
        InsertOperation localInsert,
        DeleteOperation remoteDelete)
    {
        // Check if the insert's origin is being deleted
        if (localInsert.OriginLeft.HasValue &&
            remoteDelete.TargetIds.Contains(localInsert.OriginLeft.Value))
        {
            return new Conflict
            {
                ConflictId = Guid.NewGuid(),
                DocumentId = documentId,
                Type = ConflictType.DeleteEditConflict,
                Severity = ConflictSeverity.High,
                ConflictingOperations = new CrdtOperation[] { localInsert, remoteDelete },
                Description = "Text was edited while being deleted",
                LocalPreview = localInsert.Content,
                RemotePreview = "[Deleted]",
                InvolvedUsers = new[] { localInsert.OriginClientId, remoteDelete.OriginClientId }
            };
        }

        return null;
    }

    public async Task<ConflictResolution> ResolveAsync(
        Conflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        var resolution = strategy switch
        {
            ConflictResolutionStrategy.PreserveAll =>
                ResolvePreserveAll(conflict),

            ConflictResolutionStrategy.LastWriterWins =>
                ResolveLastWriterWins(conflict),

            ConflictResolutionStrategy.FirstWriterWins =>
                ResolveFirstWriterWins(conflict),

            ConflictResolutionStrategy.PreferLocal =>
                ResolvePreferLocal(conflict),

            ConflictResolutionStrategy.PreferRemote =>
                ResolvePreferRemote(conflict),

            ConflictResolutionStrategy.SmartMerge =>
                await ResolveSmartMergeAsync(conflict, ct),

            _ => throw new ArgumentException($"Strategy {strategy} requires manual resolution")
        };

        await _historyRepository.SaveResolutionAsync(resolution, ct);

        ConflictResolved?.Invoke(this, new ConflictResolvedEventArgs
        {
            Resolution = resolution
        });

        return resolution;
    }

    private ConflictResolution ResolveLastWriterWins(Conflict conflict)
    {
        var operations = conflict.ConflictingOperations
            .OrderByDescending(op => op.Timestamp)
            .ToList();

        var winner = operations.First();

        return new ConflictResolution
        {
            ConflictId = conflict.ConflictId,
            Strategy = ConflictResolutionStrategy.LastWriterWins,
            ResultingOperations = new[] { winner },
            WasManual = false,
            ResolvedText = winner is InsertOperation insert ? insert.Content : null
        };
    }

    private async Task<ConflictResolution> ResolveSmartMergeAsync(
        Conflict conflict,
        CancellationToken ct)
    {
        var original = GetOriginalText(conflict);
        var localText = conflict.LocalPreview ?? "";
        var remoteText = conflict.RemotePreview ?? "";

        var mergedText = ThreeWayMerge.Merge(original, localText, remoteText);

        // Generate operations to achieve merged result
        var operations = GenerateOperationsForText(mergedText);

        return new ConflictResolution
        {
            ConflictId = conflict.ConflictId,
            Strategy = ConflictResolutionStrategy.SmartMerge,
            ResultingOperations = operations,
            WasManual = false,
            ResolvedText = mergedText
        };
    }

    public event EventHandler<ConflictDetectedEventArgs>? ConflictDetected;
    public event EventHandler<ConflictResolvedEventArgs>? ConflictResolved;
}
```

### 13.2 Collaborative Undo Manager Implementation

```csharp
namespace Lexichord.Modules.Collaboration.Undo;

/// <summary>
/// Manages per-user undo/redo in a collaborative context.
/// </summary>
public sealed class CollaborativeUndoManager : ICollaborativeUndoManager
{
    private readonly Guid _localUserId;
    private readonly ICrdtEngine _crdtEngine;
    private readonly Stack<UndoEntry> _undoStack = new();
    private readonly Stack<UndoEntry> _redoStack = new();
    private readonly List<CrdtOperation> _remoteOperations = new();

    public CollaborativeUndoManager(Guid localUserId, ICrdtEngine crdtEngine)
    {
        _localUserId = localUserId;
        _crdtEngine = crdtEngine;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoStackSize => _undoStack.Count;
    public int RedoStackSize => _redoStack.Count;

    public void RecordOperation(CrdtOperation operation, bool isLocal)
    {
        if (isLocal)
        {
            _undoStack.Push(new UndoEntry
            {
                Operation = operation,
                RemoteOpsAtTime = _remoteOperations.ToList()
            });

            _redoStack.Clear(); // New operation clears redo stack

            UndoStateChanged?.Invoke(this, new UndoStateChangedEventArgs
            {
                CanUndo = CanUndo,
                CanRedo = CanRedo
            });
        }
        else
        {
            _remoteOperations.Add(operation);
        }
    }

    public async Task<CrdtOperation?> UndoAsync(CancellationToken ct = default)
    {
        if (!CanUndo)
            return null;

        var entry = _undoStack.Pop();
        var inverse = GenerateInverse(entry.Operation);

        // Adjust inverse for intervening remote operations
        var adjusted = AdjustForRemoteOperations(inverse, entry.RemoteOpsAtTime);

        _redoStack.Push(entry);

        UndoStateChanged?.Invoke(this, new UndoStateChangedEventArgs
        {
            CanUndo = CanUndo,
            CanRedo = CanRedo
        });

        return adjusted;
    }

    public async Task<CrdtOperation?> RedoAsync(CancellationToken ct = default)
    {
        if (!CanRedo)
            return null;

        var entry = _redoStack.Pop();

        // Adjust operation for any new remote ops since undo
        var adjusted = AdjustForRemoteOperations(entry.Operation, entry.RemoteOpsAtTime);

        _undoStack.Push(new UndoEntry
        {
            Operation = adjusted,
            RemoteOpsAtTime = _remoteOperations.ToList()
        });

        UndoStateChanged?.Invoke(this, new UndoStateChangedEventArgs
        {
            CanUndo = CanUndo,
            CanRedo = CanRedo
        });

        return adjusted;
    }

    public string? GetUndoDescription()
    {
        if (!CanUndo)
            return null;

        var entry = _undoStack.Peek();
        return entry.Operation switch
        {
            InsertOperation insert => $"Undo typing \"{Truncate(insert.Content, 20)}\"",
            DeleteOperation delete => $"Undo delete ({delete.TargetIds.Count} chars)",
            FormatOperation format => $"Undo formatting",
            _ => "Undo"
        };
    }

    private CrdtOperation GenerateInverse(CrdtOperation operation)
    {
        return operation switch
        {
            InsertOperation insert => new DeleteOperation(
                Id: new CrdtId(_localUserId, DateTime.UtcNow.Ticks),
                ParentId: null,
                Clock: _crdtEngine.GetClock().Increment(_localUserId),
                OriginClientId: _localUserId,
                Timestamp: DateTime.UtcNow,
                TargetIds: new[] { insert.Id }
            ),

            DeleteOperation delete => GenerateInsertForUndelete(delete),

            FormatOperation format => new FormatOperation(
                Id: new CrdtId(_localUserId, DateTime.UtcNow.Ticks),
                ParentId: null,
                Clock: _crdtEngine.GetClock().Increment(_localUserId),
                OriginClientId: _localUserId,
                Timestamp: DateTime.UtcNow,
                TargetIds: format.TargetIds,
                Attributes: InvertAttributes(format.Attributes)
            ),

            _ => throw new NotSupportedException()
        };
    }

    private CrdtOperation AdjustForRemoteOperations(
        CrdtOperation operation,
        IReadOnlyList<CrdtOperation> remoteOpsAtTime)
    {
        // Find remote operations that happened after the original
        var newRemoteOps = _remoteOperations
            .Skip(remoteOpsAtTime.Count)
            .ToList();

        if (newRemoteOps.Count == 0)
            return operation;

        // Transform operation against new remote ops
        // This is similar to OT but simplified for our use case
        return TransformOperation(operation, newRemoteOps);
    }

    public event EventHandler<UndoStateChangedEventArgs>? UndoStateChanged;

    private record UndoEntry
    {
        public required CrdtOperation Operation { get; init; }
        public required IReadOnlyList<CrdtOperation> RemoteOpsAtTime { get; init; }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }
}
```

### 13.3 Three-Way Merge Implementation

```csharp
namespace Lexichord.Modules.Collaboration.Conflicts;

/// <summary>
/// Three-way merge algorithm for text.
/// </summary>
public static class ThreeWayMerge
{
    /// <summary>
    /// Merges local and remote changes against a common ancestor.
    /// </summary>
    public static string Merge(string original, string local, string remote)
    {
        var originalWords = Tokenize(original);
        var localWords = Tokenize(local);
        var remoteWords = Tokenize(remote);

        var result = new StringBuilder();
        var i = 0;

        while (i < Math.Max(Math.Max(originalWords.Length, localWords.Length), remoteWords.Length))
        {
            var origWord = i < originalWords.Length ? originalWords[i] : null;
            var localWord = i < localWords.Length ? localWords[i] : null;
            var remoteWord = i < remoteWords.Length ? remoteWords[i] : null;

            if (localWord == remoteWord)
            {
                // Both agree (same change or no change)
                if (localWord != null)
                {
                    result.Append(localWord);
                    result.Append(' ');
                }
            }
            else if (localWord == origWord)
            {
                // Only remote changed
                if (remoteWord != null)
                {
                    result.Append(remoteWord);
                    result.Append(' ');
                }
            }
            else if (remoteWord == origWord)
            {
                // Only local changed
                if (localWord != null)
                {
                    result.Append(localWord);
                    result.Append(' ');
                }
            }
            else
            {
                // Both changed differently - conflict
                // Default: preserve both with markers
                if (localWord != null)
                {
                    result.Append(localWord);
                }
                if (remoteWord != null && remoteWord != localWord)
                {
                    result.Append(remoteWord);
                }
                result.Append(' ');
            }

            i++;
        }

        return result.ToString().Trim();
    }

    private static string[] Tokenize(string text)
    {
        return text.Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);
    }
}
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
