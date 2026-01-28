# LCS-DES-092a: Design Specification — CRDT Engine

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COL-092a` | Sub-part of COL-092 |
| **Feature Name** | `CRDT Engine (Conflict-free Replicated Data Types)` | Core synchronization algorithm |
| **Target Version** | `v0.9.2a` | First sub-part of v0.9.2 |
| **Module Scope** | `Lexichord.Modules.Collaboration` | Collaboration module |
| **Swimlane** | `Collaboration` | Part of collaboration vertical |
| **License Tier** | `Teams` | Teams and above |
| **Feature Gate Key** | `Collaboration.RealTimeSync` | License gate key |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-092-INDEX](./LCS-DES-092-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-092 S3.1](./LCS-SBD-092.md#31-v092a-crdt-engine) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Real-time collaborative editing requires a data structure that can handle concurrent modifications from multiple users without coordination. Traditional operational transformation (OT) approaches require a central server to order operations, creating latency and complexity. CRDTs provide a mathematically proven approach where operations can be applied in any order and still converge to the same state.

> **Goal:** Implement a CRDT-based document model using the YATA algorithm that correctly merges all concurrent edits without data loss, enabling local-first editing with eventual consistency.

### 2.2 The Proposed Solution

Implement the YATA (Yet Another Transformation Approach) algorithm for text CRDTs:

1. **Unique Item IDs** — Each character/block has a globally unique identifier (ClientId, Sequence)
2. **Vector Clocks** — Track causality to detect concurrent operations
3. **Tombstone Deletion** — Deleted items are marked, not removed, to handle concurrent edits
4. **Origin References** — Each item tracks what it was inserted after for ordering
5. **Deterministic Ordering** — Concurrent inserts at the same position are ordered by client ID

This approach provides:
- **Strong Eventual Consistency** — All clients converge to the same state
- **Local-First Operation** — Edits apply instantly without network round-trip
- **Offline Support** — Changes accumulate and merge when reconnected
- **No Central Coordination** — Operations commute and are idempotent

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IEditorService` | v0.1.3a | Apply CRDT state to editor |
| `IDocumentRepository` | v0.1.2a | Persist CRDT state |
| `IMediator` | v0.0.7a | Publish CRDT events |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `MessagePack` | 2.x | Binary serialization (NEW) |
| `System.Collections.Immutable` | 9.0.x | Immutable collections |

### 3.2 Licensing Behavior

```csharp
public class CrdtEngineFactory(ILicenseContext license) : ICrdtEngineFactory
{
    public ICrdtEngine Create(Guid documentId, Guid clientId)
    {
        if (!license.HasFeature("Collaboration.RealTimeSync"))
        {
            throw new LicenseRequiredException(
                "Real-Time Sync requires a Teams subscription.",
                LicenseTier.Teams);
        }

        return new YataDocument(documentId, clientId);
    }
}
```

---

## 4. Data Contract (The API)

### 4.1 Core Types

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Globally unique identifier for a CRDT item.
/// Composed of client ID and monotonically increasing sequence number.
/// </summary>
[MessagePackObject]
public readonly record struct CrdtId(
    [property: Key(0)] Guid ClientId,
    [property: Key(1)] long Sequence) : IComparable<CrdtId>
{
    public static CrdtId Create(Guid clientId, long sequence) => new(clientId, sequence);

    public int CompareTo(CrdtId other)
    {
        var clientCompare = ClientId.CompareTo(other.ClientId);
        return clientCompare != 0 ? clientCompare : Sequence.CompareTo(other.Sequence);
    }

    public override string ToString() => $"{ClientId:N8}:{Sequence}";
}

/// <summary>
/// Vector clock for tracking causality across distributed clients.
/// Each entry tracks the highest sequence number seen from each client.
/// </summary>
[MessagePackObject]
public sealed class VectorClock : IEquatable<VectorClock>
{
    [Key(0)]
    private readonly Dictionary<Guid, long> _clocks;

    public VectorClock() => _clocks = new Dictionary<Guid, long>();

    private VectorClock(Dictionary<Guid, long> clocks) => _clocks = clocks;

    [IgnoreMember]
    public IReadOnlyDictionary<Guid, long> Clocks => _clocks;

    public long this[Guid clientId]
    {
        get => _clocks.TryGetValue(clientId, out var seq) ? seq : 0;
        set => _clocks[clientId] = value;
    }

    /// <summary>
    /// Creates a new clock with the specified client incremented.
    /// </summary>
    public VectorClock Increment(Guid clientId)
    {
        var clone = Clone();
        clone[clientId] = clone[clientId] + 1;
        return clone;
    }

    /// <summary>
    /// Creates a new clock that is the pointwise maximum of this and other.
    /// </summary>
    public VectorClock Merge(VectorClock other)
    {
        var result = Clone();
        foreach (var (clientId, seq) in other._clocks)
        {
            result[clientId] = Math.Max(result[clientId], seq);
        }
        return result;
    }

    /// <summary>
    /// Returns true if this clock is concurrent with (neither before nor after) other.
    /// </summary>
    public bool IsConcurrentWith(VectorClock other) =>
        !HappensBefore(other) && !other.HappensBefore(this);

    /// <summary>
    /// Returns true if this clock happens-before other (causally precedes).
    /// </summary>
    public bool HappensBefore(VectorClock other)
    {
        var atLeastOneSmaller = false;

        foreach (var (clientId, seq) in _clocks)
        {
            if (seq > other[clientId])
                return false;
            if (seq < other[clientId])
                atLeastOneSmaller = true;
        }

        // Check if other has clients we don't know about
        foreach (var (clientId, seq) in other._clocks)
        {
            if (!_clocks.ContainsKey(clientId) && seq > 0)
                atLeastOneSmaller = true;
        }

        return atLeastOneSmaller;
    }

    /// <summary>
    /// Returns true if this clock happens-after other.
    /// </summary>
    public bool HappensAfter(VectorClock other) => other.HappensBefore(this);

    public VectorClock Clone() => new(new Dictionary<Guid, long>(_clocks));

    public bool Equals(VectorClock? other)
    {
        if (other is null) return false;
        if (_clocks.Count != other._clocks.Count) return false;
        return _clocks.All(kv => other[kv.Key] == kv.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as VectorClock);
    public override int GetHashCode() => _clocks.Aggregate(0, (h, kv) => h ^ kv.GetHashCode());
}
```

### 4.2 Operation Types

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Base class for all CRDT operations.
/// </summary>
[MessagePackObject]
[Union(0, typeof(InsertOperation))]
[Union(1, typeof(DeleteOperation))]
[Union(2, typeof(FormatOperation))]
public abstract record CrdtOperation(
    [property: Key(0)] CrdtId Id,
    [property: Key(1)] CrdtId? ParentId,
    [property: Key(2)] VectorClock Clock,
    [property: Key(3)] Guid OriginClientId,
    [property: Key(4)] DateTime Timestamp);

/// <summary>
/// Insert text content at a position.
/// </summary>
[MessagePackObject]
public sealed record InsertOperation(
    [property: Key(0)] CrdtId Id,
    [property: Key(1)] CrdtId? ParentId,
    [property: Key(2)] VectorClock Clock,
    [property: Key(3)] Guid OriginClientId,
    [property: Key(4)] DateTime Timestamp,
    [property: Key(5)] string Content,
    [property: Key(6)] CrdtId? OriginLeft,
    [property: Key(7)] CrdtId? OriginRight,
    [property: Key(8)] TextAttributes? Attributes = null
) : CrdtOperation(Id, ParentId, Clock, OriginClientId, Timestamp);

/// <summary>
/// Delete text content (tombstone).
/// </summary>
[MessagePackObject]
public sealed record DeleteOperation(
    [property: Key(0)] CrdtId Id,
    [property: Key(1)] CrdtId? ParentId,
    [property: Key(2)] VectorClock Clock,
    [property: Key(3)] Guid OriginClientId,
    [property: Key(4)] DateTime Timestamp,
    [property: Key(5)] IReadOnlyList<CrdtId> TargetIds
) : CrdtOperation(Id, ParentId, Clock, OriginClientId, Timestamp);

/// <summary>
/// Apply formatting to a range.
/// </summary>
[MessagePackObject]
public sealed record FormatOperation(
    [property: Key(0)] CrdtId Id,
    [property: Key(1)] CrdtId? ParentId,
    [property: Key(2)] VectorClock Clock,
    [property: Key(3)] Guid OriginClientId,
    [property: Key(4)] DateTime Timestamp,
    [property: Key(5)] IReadOnlyList<CrdtId> TargetIds,
    [property: Key(6)] TextAttributes Attributes
) : CrdtOperation(Id, ParentId, Clock, OriginClientId, Timestamp);

/// <summary>
/// Text formatting attributes.
/// Null values indicate "no change" for that attribute.
/// </summary>
[MessagePackObject]
public record TextAttributes(
    [property: Key(0)] bool? Bold = null,
    [property: Key(1)] bool? Italic = null,
    [property: Key(2)] bool? Underline = null,
    [property: Key(3)] bool? Strikethrough = null,
    [property: Key(4)] string? FontFamily = null,
    [property: Key(5)] double? FontSize = null,
    [property: Key(6)] string? Color = null,
    [property: Key(7)] string? BackgroundColor = null)
{
    public TextAttributes Merge(TextAttributes other) => new(
        Bold: other.Bold ?? Bold,
        Italic: other.Italic ?? Italic,
        Underline: other.Underline ?? Underline,
        Strikethrough: other.Strikethrough ?? Strikethrough,
        FontFamily: other.FontFamily ?? FontFamily,
        FontSize: other.FontSize ?? FontSize,
        Color: other.Color ?? Color,
        BackgroundColor: other.BackgroundColor ?? BackgroundColor
    );
}
```

### 4.3 CRDT Engine Interface

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Core interface for CRDT document operations.
/// </summary>
public interface ICrdtEngine
{
    /// <summary>
    /// The document ID this engine is managing.
    /// </summary>
    Guid DocumentId { get; }

    /// <summary>
    /// The local client ID.
    /// </summary>
    Guid ClientId { get; }

    /// <summary>
    /// Gets the current document state as plain text.
    /// </summary>
    string GetText();

    /// <summary>
    /// Gets the total character count (excluding tombstones).
    /// </summary>
    int GetLength();

    /// <summary>
    /// Gets the current document state with formatting.
    /// </summary>
    RichTextDocument GetDocument();

    /// <summary>
    /// Applies a local insert operation.
    /// Returns the generated operation for syncing.
    /// </summary>
    InsertOperation Insert(int position, string content, TextAttributes? attributes = null);

    /// <summary>
    /// Applies a local delete operation.
    /// Returns the generated operation for syncing.
    /// </summary>
    DeleteOperation Delete(int position, int length);

    /// <summary>
    /// Applies a local format operation.
    /// Returns the generated operation for syncing.
    /// </summary>
    FormatOperation Format(int start, int end, TextAttributes attributes);

    /// <summary>
    /// Applies a remote operation received from another client.
    /// </summary>
    void ApplyRemote(CrdtOperation operation);

    /// <summary>
    /// Applies multiple remote operations in order.
    /// </summary>
    void ApplyRemoteBatch(IEnumerable<CrdtOperation> operations);

    /// <summary>
    /// Gets all operations since a given vector clock state.
    /// </summary>
    IReadOnlyList<CrdtOperation> GetOperationsSince(VectorClock since);

    /// <summary>
    /// Gets the current vector clock state.
    /// </summary>
    VectorClock GetClock();

    /// <summary>
    /// Gets pending operations not yet acknowledged by server.
    /// </summary>
    IReadOnlyList<CrdtOperation> GetPendingOperations();

    /// <summary>
    /// Marks operations as acknowledged by server.
    /// </summary>
    void AcknowledgeOperations(IEnumerable<CrdtId> operationIds);

    /// <summary>
    /// Converts a document position to the CRDT item ID at that position.
    /// </summary>
    CrdtId? PositionToId(int position);

    /// <summary>
    /// Converts a CRDT item ID to its current document position.
    /// Returns null if the item is deleted or not found.
    /// </summary>
    int? IdToPosition(CrdtId id);

    /// <summary>
    /// Event raised when local operations are generated.
    /// </summary>
    event EventHandler<CrdtOperationEventArgs> LocalOperationGenerated;

    /// <summary>
    /// Event raised when remote operations are applied.
    /// </summary>
    event EventHandler<CrdtOperationEventArgs> RemoteOperationApplied;

    /// <summary>
    /// Event raised when the document state changes.
    /// </summary>
    event EventHandler<DocumentChangedEventArgs> DocumentChanged;
}

/// <summary>
/// Event args for CRDT operations.
/// </summary>
public sealed class CrdtOperationEventArgs : EventArgs
{
    public required CrdtOperation Operation { get; init; }
    public required bool IsLocal { get; init; }
}

/// <summary>
/// Event args for document changes.
/// </summary>
public sealed class DocumentChangedEventArgs : EventArgs
{
    public required int Position { get; init; }
    public required int DeletedLength { get; init; }
    public required string InsertedText { get; init; }
    public required bool IsLocal { get; init; }
}
```

### 4.4 Serialization Interface

```csharp
namespace Lexichord.Abstractions.Collaboration;

/// <summary>
/// Serializer for CRDT operations and state.
/// </summary>
public interface ICrdtSerializer
{
    /// <summary>
    /// Serializes a single operation to binary format.
    /// </summary>
    byte[] SerializeOperation(CrdtOperation operation);

    /// <summary>
    /// Deserializes a single operation from binary format.
    /// </summary>
    CrdtOperation DeserializeOperation(byte[] data);

    /// <summary>
    /// Serializes multiple operations to binary format.
    /// </summary>
    byte[] SerializeOperations(IEnumerable<CrdtOperation> operations);

    /// <summary>
    /// Deserializes multiple operations from binary format.
    /// </summary>
    IReadOnlyList<CrdtOperation> DeserializeOperations(byte[] data);

    /// <summary>
    /// Serializes the full CRDT state for persistence or initial sync.
    /// </summary>
    byte[] SerializeState(ICrdtEngine engine);

    /// <summary>
    /// Deserializes and applies CRDT state to an engine.
    /// </summary>
    void DeserializeState(ICrdtEngine engine, byte[] data);
}
```

---

## 5. Implementation Logic

### 5.1 YATA Document Structure

```text
YATA Document Model:
┌───────────────────────────────────────────────────────────────────────┐
│ YataDocument                                                          │
│ ┌───────────────────────────────────────────────────────────────────┐ │
│ │ Items (Doubly Linked List)                                        │ │
│ │                                                                   │ │
│ │  HEAD ──► [Item A] ◄──► [Item B] ◄──► [Item C] ◄──► ... ──► TAIL │ │
│ │            │   │         │   │         │   │                      │ │
│ │            │   └─────────┤   └─────────┤   └──► originLeft        │ │
│ │            └─────────────┴─────────────┴──────► originRight       │ │
│ │                                                                   │ │
│ └───────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│ Index (Dictionary<CrdtId, Node>)    Fast lookup by ID                │
│ ┌───────────────────────────────────────────────────────────────────┐ │
│ │ { A:1 -> NodeA, A:2 -> NodeB, B:1 -> NodeC, ... }                │ │
│ └───────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│ VectorClock { ClientA: 5, ClientB: 3, ClientC: 2 }                   │
│                                                                       │
│ PendingOps [ Op6, Op7, Op8 ]                                         │
└───────────────────────────────────────────────────────────────────────┘

CrdtItem Structure:
┌─────────────────────────────────────┐
│ CrdtItem                            │
├─────────────────────────────────────┤
│ Id: CrdtId           (unique ID)    │
│ Content: string      (the text)     │
│ Deleted: bool        (tombstone)    │
│ Attributes: TextAttr (formatting)   │
│ OriginLeft: CrdtId?  (insert ref)   │
│ OriginRight: CrdtId? (insert ref)   │
│ Left: CrdtItem?      (linked list)  │
│ Right: CrdtItem?     (linked list)  │
└─────────────────────────────────────┘
```

### 5.2 Insert Algorithm (YATA)

```text
INSERT(content, position):
│
├── 1. Generate unique ID
│   └── id = CrdtId(clientId, ++sequence)
│
├── 2. Find origin references
│   ├── originLeft = item at position-1 (or null if start)
│   └── originRight = item at position (or null if end)
│
├── 3. Create operation
│   └── op = InsertOperation(id, originLeft, originRight, content, clock)
│
├── 4. Integrate into document
│   └── INTEGRATE(op)
│
└── 5. Return operation for sync

INTEGRATE(insertOp):
│
├── 1. Find origin items
│   ├── left = FindItem(originLeft) or HEAD
│   └── right = FindItem(originRight) or TAIL
│
├── 2. Find correct position between left and right
│   │   (Handle concurrent inserts at same position)
│   │
│   └── WHILE left.right != right:
│       │
│       ├── candidate = left.right
│       │
│       ├── IF candidate.originLeft == originLeft:
│       │   │   (Same origin - need tiebreaker)
│       │   │
│       │   ├── IF candidate.id.clientId < insertOp.id.clientId:
│       │   │   └── left = candidate  (candidate comes first)
│       │   │
│       │   └── ELSE:
│       │       └── BREAK  (new item comes first)
│       │
│       └── ELSE IF candidate.originLeft is between left and right:
│           └── left = candidate  (candidate comes first)
│
├── 3. Insert after left
│   ├── newItem.left = left
│   ├── newItem.right = left.right
│   ├── left.right.left = newItem (if exists)
│   └── left.right = newItem
│
└── 4. Add to index
    └── index[id] = newItem
```

### 5.3 Delete Algorithm

```text
DELETE(position, length):
│
├── 1. Find items to delete
│   ├── items = []
│   ├── current = FindItemAtPosition(position)
│   └── FOR i = 0 TO length-1:
│       ├── items.add(current.id)
│       └── current = NextVisible(current)
│
├── 2. Generate operation
│   └── op = DeleteOperation(newId, clock, items)
│
├── 3. Apply tombstones
│   └── FOR each id in items:
│       └── FindItem(id).deleted = true
│
└── 4. Return operation for sync

Note: Tombstones are never removed from the document.
      They are needed to correctly integrate concurrent inserts.
```

### 5.4 Convergence Proof

```text
THEOREM: All clients converge to the same document state.

PROOF:
1. Unique IDs: Each item has a globally unique ID (ClientId, Sequence).
   No two clients can generate the same ID.

2. Deterministic Ordering: When two items have the same origin references
   (concurrent inserts at the same position), they are ordered by
   ClientId comparison, which is deterministic.

3. Origin Preservation: Items maintain their origin references forever.
   Remote operations use these references to find their correct position.

4. Commutative Integration: The INTEGRATE algorithm produces the same
   result regardless of the order operations are received:
   - Each operation finds its position based on origin references
   - Concurrent operations at the same position are ordered by ID
   - Order of integration doesn't affect final position

5. Idempotent Application: Applying the same operation twice has no
   additional effect (items are looked up by ID).

Therefore, all clients that receive the same set of operations will
produce the same document state, regardless of the order received.
```

---

## 6. Test Scenarios

### 6.1 Basic Operations

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2a")]
public class CrdtBasicOperationTests
{
    [Fact]
    public void Insert_AtEmptyDocument_AddsContent()
    {
        // Arrange
        var sut = new YataDocument(Guid.NewGuid(), Guid.NewGuid());

        // Act
        sut.Insert(0, "Hello");

        // Assert
        sut.GetText().Should().Be("Hello");
        sut.GetLength().Should().Be(5);
    }

    [Fact]
    public void Insert_AtEnd_AppendsContent()
    {
        // Arrange
        var sut = new YataDocument(Guid.NewGuid(), Guid.NewGuid());
        sut.Insert(0, "Hello");

        // Act
        sut.Insert(5, " World");

        // Assert
        sut.GetText().Should().Be("Hello World");
    }

    [Fact]
    public void Insert_AtMiddle_InsertsContent()
    {
        // Arrange
        var sut = new YataDocument(Guid.NewGuid(), Guid.NewGuid());
        sut.Insert(0, "Hllo");

        // Act
        sut.Insert(1, "e");

        // Assert
        sut.GetText().Should().Be("Hello");
    }

    [Fact]
    public void Delete_RemovesContent()
    {
        // Arrange
        var sut = new YataDocument(Guid.NewGuid(), Guid.NewGuid());
        sut.Insert(0, "Hello World");

        // Act
        sut.Delete(5, 6); // Delete " World"

        // Assert
        sut.GetText().Should().Be("Hello");
    }

    [Fact]
    public void Delete_AllContent_LeavesEmpty()
    {
        // Arrange
        var sut = new YataDocument(Guid.NewGuid(), Guid.NewGuid());
        sut.Insert(0, "Hello");

        // Act
        sut.Delete(0, 5);

        // Assert
        sut.GetText().Should().BeEmpty();
        sut.GetLength().Should().Be(0);
    }

    [Fact]
    public void Format_AppliesAttributes()
    {
        // Arrange
        var sut = new YataDocument(Guid.NewGuid(), Guid.NewGuid());
        sut.Insert(0, "Hello World");

        // Act
        sut.Format(0, 5, new TextAttributes(Bold: true));

        // Assert
        var doc = sut.GetDocument();
        doc.GetAttributesAt(0).Bold.Should().BeTrue();
        doc.GetAttributesAt(6).Bold.Should().BeNull();
    }
}
```

### 6.2 Concurrent Operations

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2a")]
public class CrdtConcurrencyTests
{
    [Fact]
    public void ConcurrentInserts_DifferentPositions_BothPreserved()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        var docA = new YataDocument(Guid.NewGuid(), clientA);
        var docB = new YataDocument(Guid.NewGuid(), clientB);

        // Initial state
        var initOp = docA.Insert(0, "AC");
        docB.ApplyRemote(initOp);

        // Act - Concurrent inserts at different positions
        var opA = docA.Insert(1, "B"); // A inserts at position 1
        var opB = docB.Insert(2, "D"); // B inserts at position 2

        // Apply remote operations
        docA.ApplyRemote(opB);
        docB.ApplyRemote(opA);

        // Assert - Both documents converge
        docA.GetText().Should().Be(docB.GetText());
        docA.GetText().Should().Contain("B").And.Contain("D");
    }

    [Fact]
    public void ConcurrentInserts_SamePosition_DeterministicOrder()
    {
        // Arrange - Ensure clientA < clientB for deterministic test
        var clientA = new Guid("00000000-0000-0000-0000-000000000001");
        var clientB = new Guid("00000000-0000-0000-0000-000000000002");
        var docA = new YataDocument(Guid.NewGuid(), clientA);
        var docB = new YataDocument(Guid.NewGuid(), clientB);

        // Initial state
        var initOp = docA.Insert(0, "X");
        docB.ApplyRemote(initOp);

        // Act - Both insert at same position (after "X")
        var opA = docA.Insert(1, "A");
        var opB = docB.Insert(1, "B");

        // Apply in different orders
        docA.ApplyRemote(opB);
        docB.ApplyRemote(opA);

        // Assert - Same order regardless of application order
        docA.GetText().Should().Be(docB.GetText());
        // Lower client ID wins, so "A" comes before "B"
        docA.GetText().Should().Be("XAB");
    }

    [Fact]
    public void ConcurrentDeleteAndInsert_BothApplied()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        var docA = new YataDocument(Guid.NewGuid(), clientA);
        var docB = new YataDocument(Guid.NewGuid(), clientB);

        // Initial state
        var initOp = docA.Insert(0, "Hello");
        docB.ApplyRemote(initOp);

        // Act - A deletes "llo", B inserts at position 3
        var deleteOp = docA.Delete(2, 3); // Delete "llo"
        var insertOp = docB.Insert(3, "p"); // Insert "p" after "Hel"

        docA.ApplyRemote(insertOp);
        docB.ApplyRemote(deleteOp);

        // Assert - Both converge
        docA.GetText().Should().Be(docB.GetText());
        // The "p" was inserted into deleted range, but item still exists
        // Result depends on whether insert targets deleted item
    }

    [Fact]
    public void ConcurrentDeletes_SameRange_OnlyOneTombstone()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        var docA = new YataDocument(Guid.NewGuid(), clientA);
        var docB = new YataDocument(Guid.NewGuid(), clientB);

        // Initial state
        var initOp = docA.Insert(0, "Hello");
        docB.ApplyRemote(initOp);

        // Act - Both delete "ell"
        var deleteA = docA.Delete(1, 3);
        var deleteB = docB.Delete(1, 3);

        docA.ApplyRemote(deleteB);
        docB.ApplyRemote(deleteA);

        // Assert - Same result
        docA.GetText().Should().Be(docB.GetText());
        docA.GetText().Should().Be("Ho");
    }
}
```

### 6.3 Vector Clock Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2a")]
public class VectorClockTests
{
    [Fact]
    public void HappensBefore_EarlierClock_ReturnsTrue()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clock1 = new VectorClock();
        clock1[clientA] = 1;

        var clock2 = new VectorClock();
        clock2[clientA] = 2;

        // Assert
        clock1.HappensBefore(clock2).Should().BeTrue();
        clock2.HappensBefore(clock1).Should().BeFalse();
    }

    [Fact]
    public void IsConcurrentWith_ParallelClocks_ReturnsTrue()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();

        var clockA = new VectorClock();
        clockA[clientA] = 1;

        var clockB = new VectorClock();
        clockB[clientB] = 1;

        // Assert
        clockA.IsConcurrentWith(clockB).Should().BeTrue();
        clockB.IsConcurrentWith(clockA).Should().BeTrue();
    }

    [Fact]
    public void Merge_TakesMaximum()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();

        var clock1 = new VectorClock();
        clock1[clientA] = 5;
        clock1[clientB] = 2;

        var clock2 = new VectorClock();
        clock2[clientA] = 3;
        clock2[clientB] = 7;

        // Act
        var merged = clock1.Merge(clock2);

        // Assert
        merged[clientA].Should().Be(5);
        merged[clientB].Should().Be(7);
    }

    [Fact]
    public void Increment_IncreasesOwnSequence()
    {
        // Arrange
        var clientA = Guid.NewGuid();
        var clock = new VectorClock();
        clock[clientA] = 5;

        // Act
        var incremented = clock.Increment(clientA);

        // Assert
        incremented[clientA].Should().Be(6);
        clock[clientA].Should().Be(5); // Original unchanged
    }
}
```

### 6.4 Serialization Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.2a")]
public class CrdtSerializationTests
{
    private readonly ICrdtSerializer _serializer = new MessagePackCrdtSerializer();

    [Fact]
    public void SerializeDeserialize_InsertOperation_RoundTrips()
    {
        // Arrange
        var op = new InsertOperation(
            Id: new CrdtId(Guid.NewGuid(), 1),
            ParentId: null,
            Clock: new VectorClock(),
            OriginClientId: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow,
            Content: "Hello",
            OriginLeft: null,
            OriginRight: null,
            Attributes: new TextAttributes(Bold: true)
        );

        // Act
        var bytes = _serializer.SerializeOperation(op);
        var deserialized = _serializer.DeserializeOperation(bytes);

        // Assert
        deserialized.Should().BeEquivalentTo(op);
    }

    [Fact]
    public void SerializeDeserialize_DocumentState_RoundTrips()
    {
        // Arrange
        var doc = new YataDocument(Guid.NewGuid(), Guid.NewGuid());
        doc.Insert(0, "Hello World");
        doc.Delete(5, 1);
        doc.Format(0, 5, new TextAttributes(Bold: true));

        // Act
        var bytes = _serializer.SerializeState(doc);
        var newDoc = new YataDocument(doc.DocumentId, Guid.NewGuid());
        _serializer.DeserializeState(newDoc, bytes);

        // Assert
        newDoc.GetText().Should().Be(doc.GetText());
    }
}
```

---

## 7. UI/UX Specifications

**Not applicable.** The CRDT engine is a backend component with no direct UI. It provides the data model that the editor service renders.

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"CRDT insert: position={Position}, length={Length}, id={Id}"` |
| Debug | `"CRDT delete: position={Position}, length={Length}, targets={TargetCount}"` |
| Debug | `"CRDT remote operation applied: type={Type}, id={Id}, client={ClientId}"` |
| Info | `"CRDT document initialized: docId={DocumentId}, clientId={ClientId}"` |
| Info | `"CRDT state serialized: size={Size} bytes, items={ItemCount}"` |
| Warning | `"CRDT operation for unknown item: id={Id}"` |
| Warning | `"CRDT clock skew detected: local={LocalSeq}, remote={RemoteSeq}"` |
| Error | `"CRDT integration failed: {Error}"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Malicious operation injection | Medium | Validate operation structure and signatures |
| Clock manipulation | Low | Server validates and normalizes timestamps |
| Memory exhaustion (operation spam) | Medium | Rate limiting, operation size limits |
| Data corruption from invalid operations | High | Strict validation, reject malformed operations |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Empty document | Insert "Hello" at 0 | Text is "Hello" |
| 2 | Document with "Hello" | Insert " World" at 5 | Text is "Hello World" |
| 3 | Document with "Hello World" | Delete 5 characters at position 5 | Text is "Hello" |
| 4 | Two clients with same initial state | Both insert at same position | Documents converge to same state |
| 5 | Two clients with same initial state | Both insert at different positions | Both insertions preserved |
| 6 | Client A deletes, Client B inserts in same range | Operations applied in any order | Documents converge |
| 7 | Operations serialized | Deserialized | Equivalent to original |
| 8 | Full document state serialized | Loaded into new engine | Same text and structure |

### 10.2 Performance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | Document with 10,000 characters | Insert at position 5,000 | Completes in < 1ms |
| 10 | Document with 10,000 characters | Apply 100 remote operations | Completes in < 50ms |
| 11 | Document with 100,000 operations | Serialize state | Size < 5MB |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `CrdtId.cs` unique identifier type | [ ] |
| 2 | `VectorClock.cs` causality tracking | [ ] |
| 3 | `CrdtOperation.cs` operation types | [ ] |
| 4 | `TextAttributes.cs` formatting attributes | [ ] |
| 5 | `CrdtItem.cs` internal item structure | [ ] |
| 6 | `YataDocument.cs` CRDT implementation | [ ] |
| 7 | `ICrdtEngine.cs` interface definition | [ ] |
| 8 | `ICrdtSerializer.cs` interface definition | [ ] |
| 9 | `MessagePackCrdtSerializer.cs` implementation | [ ] |
| 10 | Unit tests for basic operations | [ ] |
| 11 | Unit tests for concurrent operations | [ ] |
| 12 | Unit tests for vector clock | [ ] |
| 13 | Unit tests for serialization | [ ] |

---

## 12. Verification Commands

```bash
# Run all CRDT engine tests
dotnet test --filter "Version=v0.9.2a" --logger "console;verbosity=detailed"

# Run only concurrency tests
dotnet test --filter "FullyQualifiedName~CrdtConcurrencyTests"

# Run only vector clock tests
dotnet test --filter "FullyQualifiedName~VectorClockTests"

# Run with coverage
dotnet test --filter "Version=v0.9.2a" --collect:"XPlat Code Coverage"

# Benchmark serialization
dotnet run --project tests/Lexichord.Tests.Performance -c Release -- --filter "*Crdt*"
```

---

## 13. Code Examples

### 13.1 YataDocument Implementation

```csharp
namespace Lexichord.Modules.Collaboration.Crdt;

/// <summary>
/// YATA-based CRDT document implementation.
/// </summary>
public sealed class YataDocument : ICrdtEngine
{
    private readonly object _lock = new();
    private readonly LinkedList<CrdtItem> _items = new();
    private readonly Dictionary<CrdtId, LinkedListNode<CrdtItem>> _index = new();
    private readonly List<CrdtOperation> _pendingOperations = new();
    private VectorClock _clock = new();
    private long _sequence = 0;

    public Guid DocumentId { get; }
    public Guid ClientId { get; }

    public YataDocument(Guid documentId, Guid clientId)
    {
        DocumentId = documentId;
        ClientId = clientId;
    }

    public string GetText()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            foreach (var item in _items.Where(i => !i.Deleted))
            {
                sb.Append(item.Content);
            }
            return sb.ToString();
        }
    }

    public int GetLength()
    {
        lock (_lock)
        {
            return _items.Where(i => !i.Deleted).Sum(i => i.Content.Length);
        }
    }

    public InsertOperation Insert(int position, string content, TextAttributes? attributes = null)
    {
        lock (_lock)
        {
            var id = new CrdtId(ClientId, ++_sequence);
            _clock = _clock.Increment(ClientId);

            // Find origin references
            var (originLeft, originRight) = FindOrigins(position);

            var operation = new InsertOperation(
                Id: id,
                ParentId: null,
                Clock: _clock.Clone(),
                OriginClientId: ClientId,
                Timestamp: DateTime.UtcNow,
                Content: content,
                OriginLeft: originLeft,
                OriginRight: originRight,
                Attributes: attributes
            );

            IntegrateInsert(operation);
            _pendingOperations.Add(operation);

            OnLocalOperationGenerated(operation);
            OnDocumentChanged(position, 0, content, isLocal: true);

            return operation;
        }
    }

    public DeleteOperation Delete(int position, int length)
    {
        lock (_lock)
        {
            var id = new CrdtId(ClientId, ++_sequence);
            _clock = _clock.Increment(ClientId);

            // Find items to delete
            var targetIds = FindItemsInRange(position, length)
                .Select(item => item.Id)
                .ToList();

            var operation = new DeleteOperation(
                Id: id,
                ParentId: null,
                Clock: _clock.Clone(),
                OriginClientId: ClientId,
                Timestamp: DateTime.UtcNow,
                TargetIds: targetIds
            );

            ApplyDelete(operation);
            _pendingOperations.Add(operation);

            OnLocalOperationGenerated(operation);
            OnDocumentChanged(position, length, "", isLocal: true);

            return operation;
        }
    }

    public void ApplyRemote(CrdtOperation operation)
    {
        lock (_lock)
        {
            // Update clock
            _clock = _clock.Merge(operation.Clock);

            switch (operation)
            {
                case InsertOperation insert:
                    IntegrateInsert(insert);
                    break;
                case DeleteOperation delete:
                    ApplyDelete(delete);
                    break;
                case FormatOperation format:
                    ApplyFormat(format);
                    break;
            }

            OnRemoteOperationApplied(operation);
        }
    }

    private void IntegrateInsert(InsertOperation op)
    {
        // Check if already integrated
        if (_index.ContainsKey(op.Id))
            return;

        var item = new CrdtItem
        {
            Id = op.Id,
            Content = op.Content,
            Attributes = op.Attributes,
            Deleted = false,
            OriginLeft = op.OriginLeft,
            OriginRight = op.OriginRight
        };

        // Find insertion point using YATA rules
        var insertAfter = FindInsertPosition(op);

        // Insert into linked list
        LinkedListNode<CrdtItem> node;
        if (insertAfter == null)
        {
            node = _items.AddFirst(item);
        }
        else
        {
            node = _items.AddAfter(insertAfter, item);
        }

        // Add to index
        _index[op.Id] = node;
    }

    private LinkedListNode<CrdtItem>? FindInsertPosition(InsertOperation op)
    {
        // Find left origin
        LinkedListNode<CrdtItem>? left = null;
        if (op.OriginLeft.HasValue && _index.TryGetValue(op.OriginLeft.Value, out var leftNode))
        {
            left = leftNode;
        }

        // Find right origin
        LinkedListNode<CrdtItem>? right = null;
        if (op.OriginRight.HasValue && _index.TryGetValue(op.OriginRight.Value, out var rightNode))
        {
            right = rightNode;
        }

        // YATA integration: find correct position between left and right
        var current = left;
        while (current?.Next != null && current.Next != right)
        {
            var candidate = current.Next.Value;

            // Check if candidate has same left origin (concurrent insert)
            if (candidate.OriginLeft == op.OriginLeft)
            {
                // Tiebreaker: lower client ID comes first
                if (candidate.Id.ClientId.CompareTo(op.Id.ClientId) < 0)
                {
                    current = current.Next;
                    continue;
                }
                break;
            }

            // Check if candidate's origin is between our origins
            if (IsBetween(candidate.OriginLeft, op.OriginLeft, op.OriginRight))
            {
                current = current.Next;
                continue;
            }

            break;
        }

        return current;
    }

    private void ApplyDelete(DeleteOperation op)
    {
        foreach (var targetId in op.TargetIds)
        {
            if (_index.TryGetValue(targetId, out var node))
            {
                node.Value.Deleted = true;
            }
        }
    }

    public event EventHandler<CrdtOperationEventArgs>? LocalOperationGenerated;
    public event EventHandler<CrdtOperationEventArgs>? RemoteOperationApplied;
    public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

    private void OnLocalOperationGenerated(CrdtOperation op) =>
        LocalOperationGenerated?.Invoke(this, new CrdtOperationEventArgs { Operation = op, IsLocal = true });

    private void OnRemoteOperationApplied(CrdtOperation op) =>
        RemoteOperationApplied?.Invoke(this, new CrdtOperationEventArgs { Operation = op, IsLocal = false });

    private void OnDocumentChanged(int position, int deletedLength, string insertedText, bool isLocal) =>
        DocumentChanged?.Invoke(this, new DocumentChangedEventArgs
        {
            Position = position,
            DeletedLength = deletedLength,
            InsertedText = insertedText,
            IsLocal = isLocal
        });
}
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
