# LCS-DES-v0.10.1-KG-INDEX: Design Specifications Index — Knowledge Graph Versioning

## 1. Document Control

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-v0.10.1-KG-INDEX |
| **Version** | v0.10.1 |
| **Codename** | Knowledge Graph Versioning (CKVS Phase 5a) |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |
| **Owner** | Lead Architect |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) |

---

## 2. Overview

This index organizes the design specifications for **v0.10.1-KG (Knowledge Graph Versioning)**, which delivers Git-like version control semantics to the Lexichord knowledge layer. The feature includes version history tracking, time-travel queries, snapshots, rollback functionality, branch/merge capabilities, and a visualization UI.

### 2.1 Total Scope

- **Total Estimated Effort:** 44 hours
- **Number of Sub-Parts:** 6 design specs + 1 index
- **Module:** `Lexichord.Modules.CKVS`
- **Feature Gate:** `FeatureFlags.CKVS.GraphVersioning`
- **License Tiers:**
  - Teams: Full versioning + rollback
  - Enterprise: Full + branching + unlimited history

---

## 3. Design Specification Components

### 3.1 Index (This Document)

**LCS-DES-v0.10.1-KG-INDEX.md** — Navigation and overview of all design specs.

### 3.2 Version Store (8h)

**LCS-DES-v0.10.1-KG-a.md**

Covers the persistent storage infrastructure for graph versions and deltas:
- Version metadata storage (version IDs, timestamps, branches, creators, messages)
- Delta storage for efficient change representation
- Snapshot storage for full graph state backups
- Data structures for version and delta persistence
- Database schema and query patterns

**Key Responsibilities:**
- Persisting graph versions with metadata
- Storing deltas for efficient storage
- Creating and managing snapshots
- Supporting efficient retrieval of versions by ID, timestamp, or tag

### 3.3 Change Tracking (6h)

**LCS-DES-v0.10.1-KG-b.md**

Covers capturing and recording graph mutations as versioned changes:
- Mutation interception and change capture
- Change record creation and persistence
- Integration with graph mutation operations
- Change statistics (entities created/modified/deleted, relationships, claims affected)
- Audit trail generation

**Key Responsibilities:**
- Capturing all graph mutations in real-time
- Creating versioned change records with before/after values
- Computing change statistics
- Maintaining audit trail with user/source information

### 3.4 Time-Travel Queries (8h)

**LCS-DES-v0.10.1-KG-c.md**

Covers querying the graph as it existed at historical timestamps:
- Time-travel query service for retrieving graph state at any point
- Delta application/reversal for temporal reconstruction
- Snapshot-based fast paths for common queries
- Version reference resolution (ID, timestamp, tag, branch)
- Performance optimization for historical queries

**Key Responsibilities:**
- Providing snapshot retrieval at specific timestamps or versions
- Reconstructing historical graph state using deltas
- Supporting multiple version reference types
- Optimizing performance for time-travel operations

### 3.5 Snapshot Manager (6h)

**LCS-DES-v0.10.1-KG-d.md**

Covers creation, storage, and restoration of full graph snapshots:
- Named snapshot creation for important milestones
- Snapshot restoration to bring graph back to known states
- Snapshot metadata (name, description, timestamp, creator)
- Snapshot tagging for easy reference
- Retention policies for snapshot cleanup

**Key Responsibilities:**
- Creating named snapshots with metadata
- Storing complete graph state
- Restoring graph from snapshots
- Managing snapshot lifecycle and retention

### 3.6 Branch/Merge (10h)

**LCS-DES-v0.10.1-KG-e.md**

Covers graph branching for experimental changes and merging branches back:
- Branch creation from current state
- Branch switching (context switching between versions)
- Merge operations with conflict detection
- Three-way merge resolution for conflicts
- Branch deletion and cleanup
- Branch comparison and status tracking

**Key Responsibilities:**
- Creating and managing graph branches
- Switching between branches
- Merging branches with conflict handling
- Providing branch status and comparison information
- Cleaning up stale branches

### 3.7 Version History UI (6h)

**LCS-DES-v0.10.1-KG-f.md**

Covers user interface for visualizing and managing version history:
- Version history timeline view
- Change diff visualization
- Snapshot and rollback UI controls
- Branch visualization and switching
- Search and filtering of history
- Restore/compare/download actions

**Key Responsibilities:**
- Presenting version history timeline
- Visualizing changes and diffs
- Providing restore/rollback/compare controls
- Displaying branch status and allowing branch operations
- Enabling snapshot management and download

---

## 4. Design Spec Metadata Summary

| Spec | Hours | Title | Module | Tier |
|:-----|:------|:------|:-------|:-----|
| a | 8 | Version Store | CKVS | Teams |
| b | 6 | Change Tracking | CKVS | Teams |
| c | 8 | Time-Travel Queries | CKVS | Teams |
| d | 6 | Snapshot Manager | CKVS | Teams |
| e | 10 | Branch/Merge | CKVS | Enterprise |
| f | 6 | Version History UI | CKVS | Teams |
| **TOTAL** | **44** | | | |

---

## 5. Design Specification Reading Order

For a comprehensive understanding, read in this order:

1. **LCS-DES-v0.10.1-KG-a.md** — Start with Version Store to understand the data model
2. **LCS-DES-v0.10.1-KG-b.md** — Change Tracking shows how mutations are captured
3. **LCS-DES-v0.10.1-KG-c.md** — Time-Travel Queries demonstrates historical access
4. **LCS-DES-v0.10.1-KG-d.md** — Snapshot Manager covers milestone management
5. **LCS-DES-v0.10.1-KG-e.md** — Branch/Merge enables experimental workflows
6. **LCS-DES-v0.10.1-KG-f.md** — Version History UI ties it all together visually

---

## 6. Cross-Spec Dependencies

```
Change Tracking (b)
       ↓
Version Store (a)  ← Snapshot Manager (d)
       ↓
Time-Travel Queries (c)
       ↓
   Branch/Merge (e)
       ↓
Version History UI (f)
```

**Dependency Flow:**
- All specs depend on Version Store (a) for persistence
- Change Tracking (b) creates records stored in Version Store
- Time-Travel Queries (c) reads from Version Store and Snapshot Manager
- Branch/Merge (e) requires Version Store and Change Tracking
- Version History UI (f) consumes all services for visualization

---

## 7. Key Interfaces (Reference)

All design specs implement or use these core interfaces:

### 7.1 IGraphVersionService

Primary service for version management (used by all specs).

```csharp
Task<GraphVersion> GetCurrentVersionAsync(CancellationToken ct = default);
Task<IGraphSnapshot> GetSnapshotAsync(GraphVersionRef versionRef, CancellationToken ct = default);
Task<GraphSnapshot> CreateSnapshotAsync(string name, string? description, CancellationToken ct = default);
Task<RollbackResult> RollbackAsync(GraphVersionRef targetVersion, RollbackOptions options, CancellationToken ct = default);
Task<IReadOnlyList<GraphChange>> GetHistoryAsync(HistoryQuery query, CancellationToken ct = default);
```

### 7.2 IGraphBranchService

Branch management service (used by Branch/Merge spec).

```csharp
Task<GraphBranch> CreateBranchAsync(string branchName, string? description, CancellationToken ct = default);
Task SwitchBranchAsync(string branchName, CancellationToken ct = default);
Task<MergeResult> MergeBranchAsync(string sourceBranch, MergeOptions options, CancellationToken ct = default);
Task<IReadOnlyList<GraphBranch>> GetBranchesAsync(CancellationToken ct = default);
Task DeleteBranchAsync(string branchName, CancellationToken ct = default);
```

---

## 8. Module & Licensing

### 8.1 Module Structure

All implementations reside in: `Lexichord.Modules.CKVS`

### 8.2 Feature Gate

```csharp
FeatureFlags.CKVS.GraphVersioning
```

This feature flag gates all v0.10.1-KG functionality across all sub-specs.

### 8.3 License Tiers

| Tier | v0.10.1-KG Support |
|:-----|:-------------------|
| Core | ❌ Not available |
| WriterPro | ❌ Not available |
| Teams | ✅ Full versioning (a, b, c, d, f) |
| Enterprise | ✅ Full + branching (a-f) |

---

## 9. Data Retention Policies

| Tier | Retention |
|:-----|:----------|
| Teams | 1 year |
| Enterprise | Unlimited (configurable) |

Deltas older than retention period are compacted into periodic snapshots.

---

## 10. Performance Targets

| Metric | Target | Spec |
|:-------|:-------|:-----|
| Mutation overhead | <100ms P95 | b, a |
| Time-travel query | <2s P95 | c |
| Rollback (100 changes) | <10s P95 | a, c |
| History query | <500ms P95 | b, a |
| Branch creation | <1s P95 | e |
| Snapshot creation | <5s P95 | d |

---

## 11. Data Structures Reference

### 11.1 Core Records

- **GraphVersion** — Metadata for a version (ID, parent, branch, timestamp, creator, message, stats)
- **GraphChange** — Individual change record (type, element, before/after, user, timestamp)
- **GraphChangeStats** — Statistics about changes (entities created/modified/deleted, etc.)
- **GraphVersionRef** — Reference to a version (ID, timestamp, tag, or branch)
- **GraphSnapshot** — Complete graph state at a point in time
- **GraphBranch** — Branch metadata (name, head version, base version, creation info)
- **MergeResult** — Merge operation result (status, conflicts, stats)
- **MergeConflict** — Individual merge conflict (element, property, values)

---

## 12. External Dependencies

| Component | Source | Used By |
|:----------|:-------|:--------|
| `IGraphRepository` | v0.4.5e | All specs (graph operations) |
| `ISyncService` | v0.7.6-KG | Change Tracking (b) |
| `IMediator` | v0.0.7a | All specs (event publishing) |
| Neo4j | v0.4.5-KG | Version Store (a) |
| PostgreSQL | v0.4.6-KG | Version Store (a) |

---

## 13. Acceptance Criteria (Overview)

### 13.1 Version Control Completeness

| # | Given | When | Then |
|:---|:------|:------|:-----|
| 1 | Graph mutation | Recorded | Version created, change tracked |
| 2 | Version request | At specific timestamp | Graph state at that time returned |
| 3 | Snapshot request | Create named | Snapshot metadata + data stored |
| 4 | Rollback request | To previous version | Graph reverted, new version created |
| 5 | Branch request | Create new | Branch created at current state |
| 6 | Merge request | Between branches | Conflicts detected and reported |
| 7 | History query | For time range | All changes in range returned |
| 8 | History UI | Loaded | Timeline of versions displayed |

---

## 14. Design Spec Status

| Spec | Status | Author | Date |
|:-----|:-------|:-------|:-----|
| INDEX | Draft | Lead Architect | 2026-01-31 |
| a (Version Store) | Draft | Lead Architect | 2026-01-31 |
| b (Change Tracking) | Draft | Lead Architect | 2026-01-31 |
| c (Time-Travel Queries) | Draft | Lead Architect | 2026-01-31 |
| d (Snapshot Manager) | Draft | Lead Architect | 2026-01-31 |
| e (Branch/Merge) | Draft | Lead Architect | 2026-01-31 |
| f (Version History UI) | Draft | Lead Architect | 2026-01-31 |

---

## 15. Changelog

| Version | Date | Changes |
|:--------|:-----|:--------|
| 1.0 | 2026-01-31 | Initial design specification index for v0.10.1-KG |

---
