# LCS-DES-v0.10.5-KG-c: Design Specification — Import Engine

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-105-c` | Knowledge Import/Export sub-part c |
| **Feature Name** | `Import Engine` | Import with merge/replace/append modes |
| **Target Version** | `v0.10.5c` | Third sub-part of v0.10.5-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Core Knowledge Validation System |
| **Swimlane** | `Knowledge Management` | Knowledge vertical |
| **License Tier** | `WriterPro` | WriterPro and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.KnowledgeImportExport` | Import/Export feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.5-KG](./LCS-SBD-v0.10.5-KG.md) | Knowledge Import/Export scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.5-KG S2.1](./LCS-SBD-v0.10.5-KG.md#21-sub-parts) | c = Import Engine |

---

## 2. Executive Summary

### 2.1 The Requirement

The Import Engine orchestrates the complete import process with:

1. **Preview capability:** Non-destructive analysis before committing changes
2. **Multiple modes:** Merge (update existing), Replace (full replacement), Append (add only)
3. **Conflict resolution:** Multiple strategies for handling conflicts
4. **Validation:** Comprehensive validation during import
5. **Transaction support:** Atomic operations with rollback capability
6. **Progress tracking:** Real-time feedback on import progress

### 2.2 The Proposed Solution

Implement a robust import service with:

1. **IKnowledgeImporter interface:** Main import orchestration
2. **Preview system:** Parse + validate without persisting
3. **Conflict resolver:** Handle duplicate/conflicting entities
4. **Transaction manager:** Atomic import with rollback
5. **Progress reporter:** Track and report import progress

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Entity/relationship persistence |
| `IFormatParser` (v0.10.5a) | v0.10.5a | Parse external formats |
| `ISchemaMappingService` (v0.10.5b) | v0.10.5b | Apply schema mappings |
| `IImportValidator` (v0.10.5e) | v0.10.5e | Validate entities before insert |
| `IValidationEngine` | v0.6.5-KG | CKVS rule validation |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None additional) | | Uses existing infrastructure |

### 3.2 Licensing Behavior

- **WriterPro Tier:** Merge mode only, CSV/JSON formats
- **Teams Tier:** All modes (Merge, Replace, Append), all formats
- **Enterprise Tier:** Teams tier + scheduled/incremental imports + retry logic

---

## 4. Data Contract (The API)

### 4.1 IKnowledgeImporter Interface

```csharp
namespace Lexichord.Modules.CKVS.ImportExport.Contracts;

/// <summary>
/// Main import orchestrator for knowledge data.
/// Supports multiple import modes, conflict resolution, and preview capability.
/// </summary>
public interface IKnowledgeImporter
{
    /// <summary>
    /// Previews an import without applying changes.
    /// Performs full validation and conflict detection.
    /// </summary>
    Task<ImportPreview> PreviewAsync(
        Stream content,
        ImportFormat format,
        ImportOptions options,
        SchemaMapping? mapping = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes an import operation.
    /// Returns detailed results of the import.
    /// </summary>
    Task<ImportResult> ImportAsync(
        Stream content,
        ImportFormat format,
        ImportOptions options,
        SchemaMapping? mapping = null,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets supported import formats.
    /// </summary>
    IReadOnlyList<ImportFormat> SupportedFormats { get; }

    /// <summary>
    /// Cancels a running import operation.
    /// </summary>
    Task CancelAsync(Guid importId);
}

/// <summary>
/// Options controlling import behavior.
/// </summary>
public record ImportOptions
{
    /// <summary>
    /// Import mode (Merge, Replace, Append).
    /// </summary>
    public ImportMode Mode { get; init; } = ImportMode.Merge;

    /// <summary>
    /// Validate entities before import.
    /// </summary>
    public bool ValidateOnImport { get; init; } = true;

    /// <summary>
    /// Create missing entity types automatically.
    /// </summary>
    public bool CreateMissingTypes { get; init; } = false;

    /// <summary>
    /// Default namespace for entities without explicit namespace.
    /// </summary>
    public string? DefaultNamespace { get; init; }

    /// <summary>
    /// Only import entities with these namespaces (null = all).
    /// </summary>
    public IReadOnlyList<string>? IncludeNamespaces { get; init; }

    /// <summary>
    /// Skip entities with these namespaces.
    /// </summary>
    public IReadOnlyList<string>? ExcludeNamespaces { get; init; }

    /// <summary>
    /// Strategy for handling conflicts.
    /// </summary>
    public ConflictResolution ConflictResolution { get; init; } = ConflictResolution.Skip;

    /// <summary>
    /// User initiating the import (for audit trail).
    /// </summary>
    public Guid? ImportedBy { get; init; }

    /// <summary>
    /// Comment/reason for the import.
    /// </summary>
    public string? ImportComment { get; init; }

    /// <summary>
    /// Maximum entities to import (safety limit).
    /// </summary>
    public int? MaxEntitiesPerImport { get; init; }
}

/// <summary>
/// Import modes.
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Merge: Update existing entities, add new ones.
    /// </summary>
    Merge = 1,

    /// <summary>
    /// Replace: Clear graph and import all entities.
    /// WARNING: Destructive operation.
    /// </summary>
    Replace = 2,

    /// <summary>
    /// Append: Add only new entities, skip existing.
    /// </summary>
    Append = 3
}

/// <summary>
/// Conflict resolution strategies.
/// </summary>
public enum ConflictResolution
{
    /// <summary>Keep existing data, skip imported.</summary>
    Skip = 1,

    /// <summary>Overwrite existing with imported data.</summary>
    Overwrite = 2,

    /// <summary>Rename imported entity with suffix.</summary>
    Rename = 3,

    /// <summary>Fail the entire import on conflict.</summary>
    Error = 4,

    /// <summary>Merge properties of conflicting entities.</summary>
    Merge = 5
}

/// <summary>
/// Preview of what would happen if import were executed.
/// </summary>
public record ImportPreview
{
    /// <summary>
    /// Total entities found in source.
    /// </summary>
    public required int EntitiesFound { get; init; }

    /// <summary>
    /// Total relationships found in source.
    /// </summary>
    public required int RelationshipsFound { get; init; }

    /// <summary>
    /// Entities that would be created.
    /// </summary>
    public required int NewEntities { get; init; }

    /// <summary>
    /// Entities that would be updated.
    /// </summary>
    public required int UpdatedEntities { get; init; }

    /// <summary>
    /// Entities that conflict with existing data.
    /// </summary>
    public required int ConflictingEntities { get; init; }

    /// <summary>
    /// Entities that would be skipped (filtered).
    /// </summary>
    public int SkippedEntities { get; init; }

    /// <summary>
    /// Relationships that would be created.
    /// </summary>
    public int RelationshipsProcessed { get; init; }

    /// <summary>
    /// Relationships that conflict or cannot be created.
    /// </summary>
    public int ConflictingRelationships { get; init; }

    /// <summary>
    /// Warnings detected during preview.
    /// </summary>
    public IReadOnlyList<ImportWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Validation errors that would prevent import.
    /// </summary>
    public IReadOnlyList<ImportError> Errors { get; init; } = [];

    /// <summary>
    /// Type mappings detected in source.
    /// </summary>
    public IReadOnlyList<TypeMapping> DetectedMappings { get; init; } = [];

    /// <summary>
    /// Estimate of import time in seconds.
    /// </summary>
    public int EstimatedDurationSeconds { get; init; }

    /// <summary>
    /// Estimated data change in bytes.
    /// </summary>
    public long EstimatedChangeBytes { get; init; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public record ImportResult
{
    /// <summary>
    /// Unique identifier for this import (for audit trail).
    /// </summary>
    public required Guid ImportId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Overall status of the import.
    /// </summary>
    public required ImportStatus Status { get; init; }

    /// <summary>
    /// Number of entities successfully imported.
    /// </summary>
    public required int EntitiesImported { get; init; }

    /// <summary>
    /// Number of relationships successfully imported.
    /// </summary>
    public required int RelationshipsImported { get; init; }

    /// <summary>
    /// Number of entities skipped due to filters or conflicts.
    /// </summary>
    public required int EntitiesSkipped { get; init; }

    /// <summary>
    /// Number of existing entities updated.
    /// </summary>
    public required int EntitiesUpdated { get; init; }

    /// <summary>
    /// Number of relationships skipped or failed.
    /// </summary>
    public int RelationshipsSkipped { get; init; }

    /// <summary>
    /// Total time taken for import.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Warnings encountered during import.
    /// </summary>
    public IReadOnlyList<ImportWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Errors encountered during import.
    /// </summary>
    public IReadOnlyList<ImportError> Errors { get; init; } = [];

    /// <summary>
    /// IDs of created entities (for verification).
    /// </summary>
    public IReadOnlyList<Guid> CreatedEntityIds { get; init; } = [];

    /// <summary>
    /// IDs of updated entities.
    /// </summary>
    public IReadOnlyList<Guid> UpdatedEntityIds { get; init; } = [];

    /// <summary>
    /// Timestamp when import was completed.
    /// </summary>
    public required DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who performed the import.
    /// </summary>
    public Guid? ImportedBy { get; init; }

    /// <summary>
    /// Reason/comment for the import.
    /// </summary>
    public string? ImportComment { get; init; }
}

/// <summary>
/// Status of an import operation.
/// </summary>
public enum ImportStatus
{
    /// <summary>All entities imported successfully.</summary>
    Success = 1,

    /// <summary>Some entities imported, some failed or skipped.</summary>
    PartialSuccess = 2,

    /// <summary>Import failed (validation errors or exceptions).</summary>
    Failed = 3,

    /// <summary>Import was cancelled by user.</summary>
    Cancelled = 4
}

/// <summary>
/// A warning during import.
/// </summary>
public record ImportWarning
{
    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Entity ID if warning is about specific entity.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Warning code for categorization.
    /// </summary>
    public string? WarningCode { get; init; }

    /// <summary>
    /// Severity level.
    /// </summary>
    public WarningLevel Level { get; init; } = WarningLevel.Warning;
}

/// <summary>
/// An error during import.
/// </summary>
public record ImportError
{
    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Entity ID if error is about specific entity.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Relationship if error is about specific relationship.
    /// </summary>
    public string? RelationshipId { get; init; }

    /// <summary>
    /// Error code for categorization.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Exception message if from exception.
    /// </summary>
    public string? ExceptionMessage { get; init; }
}

/// <summary>
/// Progress information during import.
/// </summary>
public record ImportProgress
{
    /// <summary>
    /// Import ID.
    /// </summary>
    public required Guid ImportId { get; init; }

    /// <summary>
    /// Current phase of import.
    /// </summary>
    public required ImportPhase Phase { get; init; }

    /// <summary>
    /// Current entity being processed (0-indexed).
    /// </summary>
    public required int CurrentEntityIndex { get; init; }

    /// <summary>
    /// Total entities to process.
    /// </summary>
    public required int TotalEntities { get; init; }

    /// <summary>
    /// Percentage complete (0-100).
    /// </summary>
    public int PercentageComplete => TotalEntities > 0
        ? (int)((CurrentEntityIndex + 1) * 100 / TotalEntities)
        : 0;

    /// <summary>
    /// Entities processed so far.
    /// </summary>
    public required int EntitiesProcessed { get; init; }

    /// <summary>
    /// Entities successfully imported.
    /// </summary>
    public required int EntitiesImported { get; init; }

    /// <summary>
    /// Relationships processed.
    /// </summary>
    public int RelationshipsProcessed { get; init; }

    /// <summary>
    /// Current elapsed time.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Estimated remaining time.
    /// </summary>
    public TimeSpan? EstimatedRemainingTime { get; init; }

    /// <summary>
    /// Current status message.
    /// </summary>
    public string? StatusMessage { get; init; }
}

/// <summary>
/// Phases of the import process.
/// </summary>
public enum ImportPhase
{
    /// <summary>Initializing import.</summary>
    Initializing = 1,

    /// <summary>Parsing source format.</summary>
    Parsing = 2,

    /// <summary>Detecting and applying schema mappings.</summary>
    Mapping = 3,

    /// <summary>Validating entities and relationships.</summary>
    Validating = 4,

    /// <summary>Resolving conflicts.</summary>
    ResolvingConflicts = 5,

    /// <summary>Creating entities.</summary>
    CreatingEntities = 6,

    /// <summary>Creating relationships.</summary>
    CreatingRelationships = 7,

    /// <summary>Completing import.</summary>
    Completing = 8,

    /// <summary>Rollback on error.</summary>
    RollingBack = 9
}

/// <summary>
/// Warning severity levels.
/// </summary>
public enum WarningLevel
{
    Info,
    Warning,
    Caution
}
```

---

## 5. Implementation Strategy

### 5.1 Import Pipeline

```
Stream Input
    ↓
[Phase 1] Parse Format
    • Format detection
    • Content parsing
    • Output: ParsedGraph
    ↓
[Phase 2] Apply Schema Mapping
    • Load or detect mapping
    • Transform entities/relationships
    • Resolve namespaces
    • Output: MappedGraph
    ↓
[Phase 3] Validate
    • Run import validator
    • Check CKVS constraints
    • Collect errors/warnings
    • Output: ValidationResult
    ↓
[Phase 4] Preview/Abort Check
    • If preview only → return ImportPreview
    • If has critical errors → return error
    • Otherwise → continue
    ↓
[Phase 5] Resolve Conflicts
    • Detect duplicate entities
    • Apply conflict resolution strategy
    • Output: ResolvedEntities
    ↓
[Phase 6] Transaction Start
    • Begin transaction
    • Set save point
    ↓
[Phase 7] Persist Entities & Relationships
    • Insert/update entities
    • Insert/update relationships
    • Track created/updated IDs
    ↓
[Phase 8] Commit or Rollback
    • If all OK → commit
    • If errors → rollback
    ↓
ImportResult Output
```

### 5.2 Conflict Resolution

| Strategy | Behavior |
| :--- | :--- |
| **Skip** | Keep existing, discard imported |
| **Overwrite** | Replace existing with imported |
| **Rename** | Add suffix to imported entity (e.g., "-import1") |
| **Error** | Fail entire import |
| **Merge** | Combine properties from both |

### 5.3 Transaction Handling

- Use database transaction for atomicity
- Create save point before entity creation
- Rollback on validation failure
- Log transaction boundaries for audit trail

---

## 6. Testing

### 6.1 Unit Tests

```csharp
[TestClass]
public class KnowledgeImporterTests
{
    [TestMethod]
    public async Task PreviewAsync_ValidContent_ReturnsPreview()
    {
        var content = CreateValidRdfContent();
        var importer = new KnowledgeImporter(/* dependencies */);

        var preview = await importer.PreviewAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions());

        Assert.IsTrue(preview.EntitiesFound > 0);
        Assert.IsTrue(preview.Errors.Count == 0);
    }

    [TestMethod]
    public async Task PreviewAsync_InvalidContent_ReturnsErrors()
    {
        var content = CreateInvalidRdfContent();
        var importer = new KnowledgeImporter(/* dependencies */);

        var preview = await importer.PreviewAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions());

        Assert.IsTrue(preview.Errors.Count > 0);
    }

    [TestMethod]
    public async Task ImportAsync_MergeMode_UpdatesExisting()
    {
        // Create existing entity
        var existing = new Entity { Id = Guid.NewGuid(), Name = "Old Name" };
        await _graphRepo.AddEntityAsync(existing);

        // Import with updated property
        var content = CreateRdfWithEntity(existing.Id, "New Name");
        var importer = new KnowledgeImporter(/* dependencies */);

        var result = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions { Mode = ImportMode.Merge });

        Assert.AreEqual(ImportStatus.Success, result.Status);
        Assert.AreEqual(1, result.EntitiesUpdated);
    }

    [TestMethod]
    public async Task ImportAsync_AppendMode_SkipsExisting()
    {
        var importer = new KnowledgeImporter(/* dependencies */);
        var content = CreateRdfWithNewEntity();

        // First import
        var result1 = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions { Mode = ImportMode.Append });

        // Second import (same content)
        var result2 = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions { Mode = ImportMode.Append });

        Assert.AreEqual(1, result2.EntitiesSkipped);
    }

    [TestMethod]
    public async Task ImportAsync_ConflictSkip_KeepsExisting()
    {
        var importer = new KnowledgeImporter(/* dependencies */);

        // Import with Skip resolution
        var result = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions
            {
                ConflictResolution = ConflictResolution.Skip,
                Mode = ImportMode.Merge
            });

        // Conflicting entities should be skipped
        Assert.IsTrue(result.EntitiesSkipped > 0);
    }

    [TestMethod]
    public async Task ImportAsync_ConflictRename_RenamesImported()
    {
        var importer = new KnowledgeImporter(/* dependencies */);

        var result = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions
            {
                ConflictResolution = ConflictResolution.Rename
            });

        // All entities should be imported (with renames)
        Assert.AreEqual(ImportStatus.Success, result.Status);
    }
}
```

### 6.2 Integration Tests

```csharp
[TestClass]
public class KnowledgeImporterIntegrationTests
{
    [TestMethod]
    public async Task ImportAsync_CompleteOntology_FullImport()
    {
        var ontologyFile = "test-data/sample-ontology.owl";
        var content = File.OpenRead(ontologyFile);

        var importer = new KnowledgeImporter(/* dependencies */);
        var result = await importer.ImportAsync(
            content,
            ImportFormat.OwlXml,
            new ImportOptions());

        Assert.AreEqual(ImportStatus.Success, result.Status);
        Assert.IsTrue(result.EntitiesImported > 100);
        Assert.IsTrue(result.RelationshipsImported > 200);
    }

    [TestMethod]
    public async Task ImportAsync_WithProgress_ReportsProgress()
    {
        var importer = new KnowledgeImporter(/* dependencies */);
        var progressReports = new List<ImportProgress>();

        var progress = new Progress<ImportProgress>(p => progressReports.Add(p));
        var result = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions(),
            mapping: null,
            progress: progress);

        Assert.IsTrue(progressReports.Count > 0);
        Assert.IsTrue(progressReports.Last().PercentageComplete == 100);
    }

    [TestMethod]
    public async Task ImportAsync_ReplaceMode_ClearsAndReimports()
    {
        // Add existing data
        await _graphRepo.AddEntityAsync(new Entity { Name = "Old" });

        var importer = new KnowledgeImporter(/* dependencies */);
        var result = await importer.ImportAsync(
            content,
            ImportFormat.Turtle,
            new ImportOptions { Mode = ImportMode.Replace });

        // Old data should be gone, new data imported
        Assert.AreEqual(ImportStatus.Success, result.Status);
    }
}
```

---

## 7. Error Handling

### 7.1 Validation Failure

**Scenario:** Import contains invalid entities.

**Handling:**
- Collect all validation errors
- Return ImportStatus.Failed
- Preserve original state (no changes)
- Provide detailed error list for user action

### 7.2 Conflict Unresolvable

**Scenario:** Conflict resolution strategy fails.

**Handling:**
- Rollback transaction
- Return status with errors
- Log conflict details for audit
- Suggest alternative resolution strategies

### 7.3 Partial Failure

**Scenario:** Some entities import successfully, others fail.

**Handling:**
- If critical errors, rollback entire import
- If non-critical, mark as PartialSuccess
- List successfully created entities
- Report failures with retry options

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Small import (<100 entities) | <5s | In-memory batch processing |
| Medium import (100-10K entities) | <30s | Streaming with batches of 100 |
| Large import (10K+ entities) | <5min | Streaming with batches of 1000 |
| Conflict detection | <1s per 1K | Indexed lookup |
| Validation | <2s per 1K | Parallel validation |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Data loss from Replace | Critical | User confirmation required before Replace |
| Integrity violation | High | Full validation before commit |
| Transaction deadlock | Medium | Timeout + retry logic |
| Audit trail loss | Medium | Log all imports with metadata |

---

## 10. License Gating

| Tier | Modes | Formats |
| :--- | :--- | :--- |
| WriterPro | Merge | CSV, JSON-LD |
| Teams | Merge, Replace, Append | All formats |
| Enterprise | All modes | All formats + scheduling |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Valid external data | PreviewAsync | Preview shows entities/relationships found |
| 2 | Invalid data | PreviewAsync | Preview shows validation errors |
| 3 | Valid data | ImportAsync Merge | Entities created and updated |
| 4 | Conflicting entity | ConflictSkip | Existing kept, conflict skipped |
| 5 | Conflicting entity | ConflictOverwrite | Existing replaced with imported |
| 6 | Conflicting entity | ConflictRename | Imported renamed with suffix |
| 7 | Large dataset | ImportAsync | Completes in <5 minutes |
| 8 | Replace mode | ImportAsync | Graph cleared before import |
| 9 | Import with progress | ImportAsync | Progress reported regularly |
| 10 | Critical validation error | ImportAsync | Import fails, no changes made |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IKnowledgeImporter, preview, Merge/Replace/Append modes, conflict resolution, progress tracking |

---

**Parent Document:** [LCS-DES-v0.10.5-KG-INDEX](./LCS-DES-v0.10.5-KG-INDEX.md)
**Previous Part:** [LCS-DES-v0.10.5-KG-b](./LCS-DES-v0.10.5-KG-b.md)
**Next Part:** [LCS-DES-v0.10.5-KG-d](./LCS-DES-v0.10.5-KG-d.md)
