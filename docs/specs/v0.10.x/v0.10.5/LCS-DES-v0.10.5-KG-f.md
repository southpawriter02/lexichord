# LCS-DES-v0.10.5-KG-f: Design Specification — Import/Export UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-105-f` | Knowledge Import/Export sub-part f |
| **Feature Name** | `Import/Export UI` | User interface for import/export operations |
| **Target Version** | `v0.10.5f` | Sixth sub-part of v0.10.5-KG |
| **Module Scope** | `Lexichord.Modules.CKVS.Interop` | CKVS Interoperability module |
| **Swimlane** | `Knowledge Management` | Knowledge vertical |
| **License Tier** | `Teams` | Teams and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.KnowledgeInterop` | Knowledge interoperability feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.5-KG](./LCS-SBD-v0.10.5-KG.md) | Knowledge Import/Export scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.5-KG S2.1](./LCS-SBD-v0.10.5-KG.md#21-sub-parts) | f = Import/Export UI |

---

## 2. Executive Summary

### 2.1 The Requirement

The Import/Export UI provides an intuitive web interface for users to:

- Upload files for import with preview and validation
- Configure schema mappings interactively
- Review import previews before committing
- Export knowledge in multiple formats
- Configure export options and filters
- Track import/export progress
- View operation results and errors

### 2.2 The Proposed Solution

Implement a comprehensive UI with:

1. **Import wizard:** Step-by-step import flow
2. **Schema mapping UI:** Interactive mapping configuration
3. **Export configurator:** Format and option selection
4. **Progress tracking:** Real-time operation updates
5. **REST API endpoints:** Support for programmatic access

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IKnowledgeImporter` | v0.10.5c | Import execution |
| `IKnowledgeExporter` | v0.10.5d | Export execution |
| `ISchemaMappingService` | v0.10.5b | Mapping detection/validation |
| `IImportValidator` | v0.10.5e | Pre-import validation |
| `IFeatureFlags` | Core | Feature gate checks |
| `ILicenseContext` | Core | License tier validation |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Microsoft.AspNetCore` | 8.0+ | Web framework |
| `MediatR` | 12.0+ | CQRS command handling |

### 3.2 Licensing Behavior

- **Core Tier:** No import/export UI
- **WriterPro Tier:** CSV export UI only
- **Teams Tier:** Full import/export UI
- **Enterprise Tier:** Teams tier + advanced features

---

## 4. Data Contract (The API)

### 4.1 REST API Endpoints

```csharp
namespace Lexichord.Modules.CKVS.Interop.Api;

/// <summary>
/// REST API endpoints for knowledge import/export operations.
/// </summary>
[ApiController]
[Route("api/v1/knowledge")]
[Authorize]
public class KnowledgeImportExportController : ControllerBase
{
    /// <summary>
    /// POST: Start an import operation.
    /// </summary>
    [HttpPost("import/start")]
    public async Task<IActionResult> StartImportAsync(
        IFormFile file,
        [FromQuery] ImportFormat format,
        [FromQuery] ImportMode mode = ImportMode.Merge,
        CancellationToken ct = default)
    {
        // Validate license tier
        if (!_licenseContext.IsAtLeast(LicenseTier.Teams))
            return Forbid("Import/export requires Teams tier");

        // Check feature gate
        if (!_featureFlags.IsEnabled("CKVS.KnowledgeInterop"))
            return Forbid("Knowledge interop feature is not enabled");

        using var stream = file.OpenReadStream();

        // Auto-detect format if not specified
        var detectedFormat = format != 0 ? format : DetectFormat(file.FileName);

        // Start import workflow
        var result = await _importer.PreviewAsync(
            stream, detectedFormat, new ImportOptions { Mode = mode }, ct);

        return Ok(new ImportStartResponse { Preview = result });
    }

    /// <summary>
    /// GET: Fetch schema mappings for editing.
    /// </summary>
    [HttpGet("import/mappings")]
    public async Task<IActionResult> GetMappingsAsync(CancellationToken ct)
    {
        var mappings = await _schemaMappingService.GetSavedMappingsAsync(ct);
        return Ok(mappings);
    }

    /// <summary>
    /// POST: Execute import with finalized mapping.
    /// </summary>
    [HttpPost("import/execute")]
    public async Task<IActionResult> ExecuteImportAsync(
        [FromBody] ExecuteImportRequest request,
        CancellationToken ct = default)
    {
        // Validate mapping
        var mappingValidation = await _schemaMappingService
            .ValidateMappingAsync(request.Mapping, ct);

        if (!mappingValidation.IsValid)
            return BadRequest(mappingValidation.Errors);

        // Re-open file stream (stored during preview)
        var fileStream = await _fileStorage.GetAsync(request.PreviewId);

        var result = await _importer.ImportAsync(
            fileStream,
            request.Format,
            request.Options,
            request.Mapping,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// POST: Get export configuration options.
    /// </summary>
    [HttpPost("export/prepare")]
    public async Task<IActionResult> PrepareExportAsync(
        [FromBody] PrepareExportRequest request,
        CancellationToken ct = default)
    {
        var metadata = await _exporter.GetMetadataAsync(request.Options, ct);
        return Ok(metadata);
    }

    /// <summary>
    /// POST: Execute export operation.
    /// </summary>
    [HttpPost("export/execute")]
    public async Task ExportAsync(
        [FromBody] ExecuteExportRequest request,
        CancellationToken ct = default)
    {
        var ms = new MemoryStream();

        await _exporter.ExportAsync(
            ms, request.Format, request.Options, null, ct);

        ms.Position = 0;
        await Response.Body.WriteAsync(ms.ToArray(), 0, (int)ms.Length, ct);

        Response.ContentType = GetContentType(request.Format);
        Response.Headers.ContentDisposition =
            $"attachment; filename=export.{GetExtension(request.Format)}";
    }

    /// <summary>
    /// GET: Get available import/export formats.
    /// </summary>
    [HttpGet("formats")]
    public IActionResult GetAvailableFormats()
    {
        return Ok(new
        {
            ImportFormats = _importer.SupportedFormats,
            ExportFormats = _exporter.SupportedFormats
        });
    }
}
```

### 4.2 Request/Response DTOs

```csharp
namespace Lexichord.Modules.CKVS.Interop.Contracts.UI;

/// <summary>
/// Request to start an import operation.
/// </summary>
public record ImportStartRequest
{
    public required IFormFile File { get; init; }
    public required ImportFormat Format { get; init; }
    public ImportMode Mode { get; init; } = ImportMode.Merge;
}

/// <summary>
/// Response from starting import.
/// </summary>
public record ImportStartResponse
{
    public required ImportPreview Preview { get; init; }
    public string? PreviewId { get; init; }
    public string? RecommendedMapping { get; init; }
}

/// <summary>
/// Request to execute import with mapping.
/// </summary>
public record ExecuteImportRequest
{
    public required string PreviewId { get; init; }
    public required ImportFormat Format { get; init; }
    public required ImportOptions Options { get; init; }
    public SchemaMapping? Mapping { get; init; }
}

/// <summary>
/// Request to prepare export (get metadata).
/// </summary>
public record PrepareExportRequest
{
    public required ExportFormat Format { get; init; }
    public required ExportOptions Options { get; init; }
}

/// <summary>
/// Request to execute export.
/// </summary>
public record ExecuteExportRequest
{
    public required ExportFormat Format { get; init; }
    public required ExportOptions Options { get; init; }
}

/// <summary>
/// Schema mapping with UI hints.
/// </summary>
public record SchemaMappingUI
{
    public required SchemaMapping Mapping { get; init; }

    /// <summary>
    /// Confidence scores for each type mapping.
    /// </summary>
    public IReadOnlyDictionary<string, float> ConfidenceScores { get; init; }
        = new Dictionary<string, float>();

    /// <summary>
    /// Unmapped types from source.
    /// </summary>
    public IReadOnlyList<string> UnmappedTypes { get; init; } = [];

    /// <summary>
    /// Warnings about mapping.
    /// </summary>
    public IReadOnlyList<MappingWarning> Warnings { get; init; } = [];
}

public record MappingWarning
{
    public required string WarningCode { get; init; }
    public required string Message { get; init; }
    public string? AffectedType { get; init; }
}
```

### 4.3 WebSocket Message Types

```csharp
/// <summary>
/// WebSocket message for real-time import progress.
/// </summary>
public record ImportProgressMessage
{
    public required string OperationId { get; init; }
    public required ImportProgressType Type { get; init; }
    public required int ProcessedCount { get; init; }
    public required int TotalCount { get; init; }
    public string? CurrentEntity { get; init; }
    public string? Status { get; init; }
}

public enum ImportProgressType
{
    Started,
    ProcessingEntity,
    Processing Relationship,
    Validating,
    Committing,
    Completed,
    Error
}

/// <summary>
/// WebSocket message for real-time export progress.
/// </summary>
public record ExportProgressMessage
{
    public required string OperationId { get; init; }
    public required ExportProgressType Type { get; init; }
    public required int ProcessedEntities { get; init; }
    public required int TotalEntities { get; init; }
    public long BytesWritten { get; init; }
    public string? Status { get; init; }
}

public enum ExportProgressType
{
    Started,
    ProcessingEntity,
    WritingData,
    Completed,
    Error
}
```

---

## 5. UI Workflow Specifications

### 5.1 Import Wizard Flow

```
┌─────────────────────────────────────┐
│ Step 1: Upload File                  │
├─────────────────────────────────────┤
│ [Choose File...] [Clear]             │
│ Format: [Auto-detect ▼]              │
│ File info: 2.4 MB, OWL/XML           │
│                                       │
│ [Cancel] [Next]                      │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ Step 2: Review Preview               │
├─────────────────────────────────────┤
│ Entities found: 147                  │
│ Relationships: 312                   │
│ New entities: 12                     │
│ Updates: 135                         │
│ Conflicts: 3                         │
│ Warnings: 2                          │
│                                       │
│ Import mode: [Merge ▼]               │
│ Conflict resolution: [Skip ▼]        │
│                                       │
│ [Cancel] [Back] [Next]               │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ Step 3: Configure Mappings           │
├─────────────────────────────────────┤
│ Type Mappings:                        │
│ ┌─────────────────────────────────┐ │
│ │ api:Endpoint → Endpoint (98%)   │ │
│ │ api:Service → Service (95%)     │ │
│ │ api:Response → [Entity ▼]       │ │
│ │ api:ErrorCode → + New Type      │ │
│ └─────────────────────────────────┘ │
│                                       │
│ Property Mappings:                    │
│ ┌─────────────────────────────────┐ │
│ │ api:httpMethod → method         │ │
│ │ api:urlPath → path              │ │
│ │ [+ Add mapping]                 │ │
│ └─────────────────────────────────┘ │
│                                       │
│ [Cancel] [Back] [Next]               │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ Step 4: Confirm & Execute            │
├─────────────────────────────────────┤
│ Summary:                              │
│ • Format: OWL/XML                    │
│ • Mode: Merge                        │
│ • Entities: 147 (12 new)             │
│ • Mappings: 8 configured             │
│                                       │
│ ☐ Create missing types               │
│ ☑ Validate on import                 │
│                                       │
│ [Cancel] [Back] [Import]             │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ Step 5: Progress & Results           │
├─────────────────────────────────────┤
│ [████████████░░░░░░░] 60%             │
│                                       │
│ Processing: api:getUserEndpoint      │
│ Status: Validating...                │
│                                       │
│ Imported: 88 entities                │
│ Skipped: 0                           │
│ Errors: 0                            │
│                                       │
│ [Close]                              │
└─────────────────────────────────────┘
```

### 5.2 Export Configurator Flow

```
┌─────────────────────────────────────┐
│ Export Knowledge Graph                │
├─────────────────────────────────────┤
│ Format: [JSON-LD ▼]                  │
│                                       │
│ Scope:                                │
│ ◉ Entire graph (847 entities)        │
│ ○ Selected types only                │
│ │ [☑ Endpoint] [☑ Service]          │
│ └ ○ Custom selection                 │
│                                       │
│ Options:                              │
│ ☑ Include metadata                   │
│ ☑ Include claims                     │
│ ☐ Include derived facts              │
│ ☐ Include validation status          │
│                                       │
│ Namespaces:                           │
│ Base URI: [https://example.com/...] │
│ Prefix: [api]                        │
│ ☑ Include standard prefixes          │
│                                       │
│ Estimated size: 2.4 MB               │
│                                       │
│ [Cancel] [Preview] [Export]          │
└─────────────────────────────────────┘
```

---

## 6. UI Component Specification

### 6.1 Import Wizard Component

```typescript
// TypeScript/React component structure
interface ImportWizardProps {
  onImportComplete: (result: ImportResult) => void;
  licenseContext: LicenseContext;
}

interface ImportWizardState {
  currentStep: 1 | 2 | 3 | 4 | 5;
  uploadedFile: File | null;
  detectedFormat: ImportFormat;
  preview: ImportPreview | null;
  previewId: string | null;
  mapping: SchemaMapping | null;
  importOptions: ImportOptions;
  progress: ImportProgress | null;
  result: ImportResult | null;
  error: string | null;
}

export function ImportWizard(props: ImportWizardProps) {
  return (
    <Dialog open={true}>
      {step === 1 && <FileUploadStep />}
      {step === 2 && <PreviewStep preview={preview} />}
      {step === 3 && <MappingStep mapping={mapping} />}
      {step === 4 && <ConfirmStep />}
      {step === 5 && <ProgressStep progress={progress} />}
    </Dialog>
  );
}
```

### 6.2 Schema Mapping Editor Component

```typescript
interface SchemaMappingEditorProps {
  sourceSchema: ImportedSchema;
  targetSchema: CkvsSchema;
  initialMapping: SchemaMapping | null;
  onMappingChange: (mapping: SchemaMapping) => void;
}

interface MappingEditorState {
  typeMappings: TypeMapping[];
  propertyMappings: PropertyMapping[];
  relationshipMappings: RelationshipMapping[];
  validationResult: MappingValidationResult | null;
}

export function SchemaMappingEditor(props: SchemaMappingEditorProps) {
  return (
    <div>
      <TypeMappingTable
        mappings={typeMappings}
        onEdit={updateTypeMapping}
        confidenceScores={confidenceScores}
      />
      <PropertyMappingTable
        mappings={propertyMappings}
        onEdit={updatePropertyMapping}
      />
      <ValidationResults result={validationResult} />
    </div>
  );
}
```

### 6.3 Export Configurator Component

```typescript
interface ExportConfiguratorProps {
  onExportStart: (config: ExecuteExportRequest) => void;
  licenseContext: LicenseContext;
}

interface ExportConfigState {
  selectedFormat: ExportFormat;
  selectedScope: ExportScope;
  selectedTypes: string[];
  options: ExportOptions;
  metadata: ExportMetadata | null;
  isExporting: boolean;
  progress: ExportProgress | null;
}

export function ExportConfigurator(props: ExportConfiguratorProps) {
  return (
    <Dialog open={true}>
      <FormatSelector value={format} onChange={setFormat} />
      <ScopeSelector value={scope} onChange={setScope} />
      <OptionsPanel options={options} onChange={setOptions} />
      <MetadataPreview metadata={metadata} />
      {isExporting && <ProgressBar progress={progress} />}
    </Dialog>
  );
}
```

---

## 7. Error Handling

### 7.1 Invalid File Upload

**Scenario:** User uploads unsupported file format.

**Handling:**
- Show error message with supported formats
- Allow retry with different file
- Maintain wizard state

**Code:**
```csharp
try
{
    var format = DetectFormat(file.FileName);
    if (!_importer.SupportedFormats.Contains(format))
    {
        return BadRequest(new { error = "Unsupported file format" });
    }
}
catch (Exception ex)
{
    _logger.LogError("File format detection failed: {Error}", ex.Message);
    return BadRequest(new { error = "Could not detect file format" });
}
```

### 7.2 Mapping Validation Failure

**Scenario:** User's schema mapping is invalid.

**Handling:**
- Display validation errors to user
- Highlight problematic mappings
- Suggest fixes
- Block import until resolved

**Code:**
```csharp
var validationResult = await _schemaMappingService
    .ValidateMappingAsync(request.Mapping);

if (!validationResult.IsValid)
{
    return BadRequest(new
    {
        errors = validationResult.Errors,
        warnings = validationResult.Warnings
    });
}
```

### 7.3 Import Execution Error

**Scenario:** Import fails during execution.

**Handling:**
- Report error with context
- Show partial results
- Allow rollback or retry
- Log for debugging

**Code:**
```csharp
try
{
    var result = await _importer.ImportAsync(...);
    return Ok(result);
}
catch (ImportExecutionException ex)
{
    _logger.LogError("Import failed: {Error}", ex.Message);
    return StatusCode(500, new
    {
        error = ex.Message,
        partialResult = ex.PartialResult
    });
}
```

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| File upload (100 MB) | <30s | Streaming upload |
| Preview generation | <5s | Async processing |
| Mapping UI render | <500ms | Virtual scrolling for large schemas |
| Export preparation | <1s | Cached metadata |
| Real-time progress | <500ms latency | WebSocket updates |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Unauthorized access | Critical | Authorization checks on all endpoints |
| Large file DoS | Medium | File size limits per tier |
| Sensitive data export | High | Confirm sensitive data export |
| Long-running operations | Medium | Operation timeout with cleanup |

---

## 10. License Gating

| Tier | Import/Export UI |
| :--- | :--- |
| Core | Not available |
| WriterPro | CSV export only |
| Teams | Full import/export UI |
| Enterprise | Teams + advanced features |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Licensed Teams user | Access import UI | Wizard displayed |
| 2 | File upload | Select file | Format auto-detected |
| 3 | Preview available | Review step | Summary shown with entity counts |
| 4 | No mapping | Mapping step | Auto-detected mappings offered |
| 5 | Invalid mapping | Validate | Errors displayed, import blocked |
| 6 | Complete mapping | Execute import | Import starts, progress shown |
| 7 | Export options | Configure | Metadata preview shows estimated size |
| 8 | Export ready | Click Export | File downloads with correct MIME type |
| 9 | Long operation | Monitor | WebSocket progress updates received |
| 10 | CoreTier user | Access UI | Forbidden/Upgrade prompt shown |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - Import/export wizard, schema mapping editor, export configurator, REST API |

---

**Parent Document:** [LCS-DES-v0.10.5-KG-INDEX](./LCS-DES-v0.10.5-KG-INDEX.md)
**Previous Part:** [LCS-DES-v0.10.5-KG-e](./LCS-DES-v0.10.5-KG-e.md)
