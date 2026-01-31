# LCS-DES-v0.10.3-KG: Design Specification Index

## Document Control

| Field | Value |
| :--- | :--- |
| **Master Document ID** | LCS-DES-v0.10.3-KG-INDEX |
| **Version** | v0.10.3 |
| **Codename** | Entity Resolution (CKVS Phase 5c) |
| **Module** | Lexichord.Modules.CKVS |
| **Feature Gate** | FeatureFlags.CKVS.EntityResolution |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |
| **Owner** | Lead Architect |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) |

---

## Overview

This index aggregates all design specifications for **v0.10.3-KG: Entity Resolution**. Entity Resolution delivers workflows for disambiguating uncertain entity matches and deduplicating entities that represent the same real-world concept.

The version is organized into **6 design specifications** covering the complete feature scope across **38 hours** of implementation effort.

---

## Design Specifications

### Part A: Disambiguation Service (6 hours)

**File:** [LCS-DES-v0.10.3-KG-a.md](./LCS-DES-v0.10.3-KG-a.md)

**Feature:** Handle uncertain entity matches with disambiguation workflows.

**Key Interfaces:**
- `IDisambiguationService`
- `DisambiguationResult`
- `DisambiguationCandidate`

**Key Deliverables:**
- Candidate scoring based on confidence
- User choice recording for learning
- Entity creation from unmatched mentions
- Auto-resolution for high-confidence matches

**License Tier:** WriterPro (basic disambiguation)

**Estimated Effort:** 6 hours

---

### Part B: Duplicate Detector (8 hours)

**File:** [LCS-DES-v0.10.3-KG-b.md](./LCS-DES-v0.10.3-KG-b.md)

**Feature:** Find potential duplicate entities across the knowledge graph.

**Key Interfaces:**
- `IDuplicateDetector`
- `DuplicateScanResult`
- `DuplicateGroup`
- `DuplicateCandidate`

**Key Deliverables:**
- Pairwise similarity calculation
- Duplicate grouping with confidence scoring
- Name, type, and property matching
- Relationship pattern analysis

**License Tier:** Teams (deduplication)

**Estimated Effort:** 8 hours

---

### Part C: Entity Merger (8 hours)

**File:** [LCS-DES-v0.10.3-KG-c.md](./LCS-DES-v0.10.3-KG-c.md)

**Feature:** Merge duplicate entities while preserving all relationships.

**Key Interfaces:**
- `IEntityMerger`
- `MergePreview`
- `MergeResult`
- `MergeConflict`

**Key Deliverables:**
- Merge preview with conflict detection
- Relationship and claim transfer
- Document link updates
- Alias creation for merged entities
- Undo support via version service

**License Tier:** Teams (deduplication)

**Estimated Effort:** 8 hours

---

### Part D: Resolution Learning (6 hours)

**File:** [LCS-DES-v0.10.3-KG-d.md](./LCS-DES-v0.10.3-KG-d.md)

**Feature:** Learn from user disambiguation choices to improve future matching.

**Key Interfaces:**
- `IResolutionLearningService`
- `DisambiguationFeedback`
- `PatternMatch`

**Key Deliverables:**
- Feedback recording and analysis
- Pattern extraction from user choices
- Model improvement feedback
- Confidence adjustment in disambiguation

**License Tier:** Enterprise (learning)

**Estimated Effort:** 6 hours

---

### Part E: Bulk Resolution UI (6 hours)

**File:** [LCS-DES-v0.10.3-KG-e.md](./LCS-DES-v0.10.3-KG-e.md)

**Feature:** Review and resolve duplicate entities at scale with UI components.

**Key Deliverables:**
- Duplicate group listing and filtering
- Batch merge operations
- Merge preview display
- Progress tracking
- Conflict resolution UI
- Bulk action buttons (merge, skip, mark reviewed)

**License Tier:** Teams (bulk operations)

**Estimated Effort:** 6 hours

---

### Part F: Resolution Audit (4 hours)

**File:** [LCS-DES-v0.10.3-KG-f.md](./LCS-DES-v0.10.3-KG-f.md)

**Feature:** Track and audit all entity resolution decisions for compliance.

**Key Interfaces:**
- `IResolutionAuditService`
- `ResolutionAuditEntry`
- `DisambiguationAuditEntry`

**Key Deliverables:**
- Merge operation logging
- Disambiguation choice tracking
- User and timestamp recording
- Undo chain for merge operations
- Audit log queries and export

**License Tier:** Teams (basic audit), Enterprise (full audit)

**Estimated Effort:** 4 hours

---

## Implementation Roadmap

| Phase | Components | Duration | Deliverables |
| :--- | :--- | :--- | :--- |
| **Phase 1** | Part A (Disambiguation) | 6h | GetCandidatesAsync, RecordChoiceAsync, CreateFromMentionAsync |
| **Phase 2** | Part B (Duplicate Detection) | 8h | ScanForDuplicatesAsync, FindDuplicatesOfAsync, GetDuplicateGroupsAsync |
| **Phase 3** | Part C (Entity Merger) | 8h | PreviewMergeAsync, MergeAsync, UnmergeAsync |
| **Phase 4** | Part D (Learning) | 6h | RecordFeedbackAsync, ExtractPatternsAsync, UpdateModelAsync |
| **Phase 5** | Part E (UI) + Part F (Audit) | 10h | UI components, audit logging, resolution tracking |

---

## Cross-Cutting Concerns

### Dependencies

All parts depend on:
- `IEntityLinkingService` (v0.5.5-KG) - Entity linking integration
- `IEntityBrowser` (v0.4.7-KG) - Entity management
- `IGraphRepository` (v0.4.5e) - Graph operations
- `IGraphVersionService` (v0.10.1-KG) - Undo/redo support
- `IMediator` (v0.0.7a) - Event publishing

### Module Structure

```
Lexichord.Modules.CKVS
├── EntityResolution
│   ├── Disambiguation
│   │   ├── IDisambiguationService.cs
│   │   ├── DisambiguationService.cs
│   │   └── Contracts/
│   ├── Deduplication
│   │   ├── IDuplicateDetector.cs
│   │   ├── DuplicateDetector.cs
│   │   ├── IEntityMerger.cs
│   │   ├── EntityMerger.cs
│   │   └── Contracts/
│   ├── Learning
│   │   ├── IResolutionLearningService.cs
│   │   └── ResolutionLearningService.cs
│   ├── Audit
│   │   ├── IResolutionAuditService.cs
│   │   └── ResolutionAuditService.cs
│   └── UI/
│       └── Components/ (UI components list)
```

### Feature Gate

All Entity Resolution features are gated behind `FeatureFlags.CKVS.EntityResolution`:

```csharp
if (!_featureFlags.IsEnabled(FeatureFlags.CKVS.EntityResolution))
    throw new FeatureNotEnabledException("Entity Resolution");
```

### License Tiers

| Tier | Features |
| :--- | :--- |
| **Core** | Not available |
| **WriterPro** | Part A (Disambiguation) only |
| **Teams** | Parts A, B, C, E, F (without learning) |
| **Enterprise** | All parts A-F (with Part D learning) |

---

## Performance Targets

| Metric | Target |
| :--- | :--- |
| Disambiguation response | <500ms (P95) |
| Duplicate scan (1K entities) | <30s (P95) |
| Single merge operation | <2s (P95) |
| Bulk merge (10 groups) | <20s (P95) |

---

## Risk & Mitigation

| Risk | Tier | Mitigation |
| :--- | :--- | :--- |
| False positive merges | Critical | Preview before execute, full undo support |
| Learning bad patterns | High | Feedback loop validation, admin override |
| Performance at scale | High | Incremental scanning, smart caching |
| Broken document links | Medium | Alias system, link verification post-merge |

---

## Document Navigation

- **Scope Breakdown:** [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md)
- **Part A Spec:** [LCS-DES-v0.10.3-KG-a.md](./LCS-DES-v0.10.3-KG-a.md)
- **Part B Spec:** [LCS-DES-v0.10.3-KG-b.md](./LCS-DES-v0.10.3-KG-b.md)
- **Part C Spec:** [LCS-DES-v0.10.3-KG-c.md](./LCS-DES-v0.10.3-KG-c.md)
- **Part D Spec:** [LCS-DES-v0.10.3-KG-d.md](./LCS-DES-v0.10.3-KG-d.md)
- **Part E Spec:** [LCS-DES-v0.10.3-KG-e.md](./LCS-DES-v0.10.3-KG-e.md)
- **Part F Spec:** [LCS-DES-v0.10.3-KG-f.md](./LCS-DES-v0.10.3-KG-f.md)

---

## Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - Index for all 6 design specifications |
