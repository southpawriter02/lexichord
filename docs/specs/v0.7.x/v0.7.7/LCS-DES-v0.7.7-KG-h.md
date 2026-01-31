# LCS-DES-077-KG-h: Pre-built Workflows

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-h |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | Pre-built Workflows (CKVS Phase 4d) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Pre-built Workflows** module provides curated validation workflow templates for common use cases: on-save validation, pre-publish gates, and nightly health checks. These YAML-defined workflows enable users to quickly enable comprehensive validation without custom configuration.

### 1.2 Key Responsibilities

- Define three primary pre-built workflow templates
- Support YAML workflow configuration format
- Load workflows from embedded resources
- Provide workflow registry for discovery
- Support workflow customization via overrides
- Track workflow versions and compatibility
- Enable user-defined custom workflows

### 1.3 Module Location

```
src/
  Lexichord.Workflows/
    Validation/
      Templates/
        PrebuiltWorkflows.cs
        WorkflowRegistry.cs
        WorkflowLoader.cs
        WorkflowCustomizer.cs
  Resources/
    Workflows/
      on-save-validation.yaml
      pre-publish-gate.yaml
      nightly-health-check.yaml
```

---

## 2. Workflow Definitions

### 2.1 On-Save Validation Workflow

**File:** `on-save-validation.yaml`

```yaml
id: on-save-validation
name: "On-Save Validation"
description: "Validates document on every save event"
version: "1.0.0"
trigger: on_save
enabled: true
timeout_minutes: 2
skip_on_user_cancel: true

steps:
  - id: schema-validation
    name: "Schema Validation"
    type: validation
    step_type: schema
    order: 1
    enabled: true
    timeout_ms: 5000
    failure_action: halt
    failure_severity: error
    execute_async: true
    options:
      check_json_schema: true
      check_xml_schema: false
      validate_metadata: true

  - id: consistency-check
    name: "Content Consistency"
    type: validation
    step_type: consistency
    order: 2
    enabled: true
    timeout_ms: 10000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_duplicate_terms: true
      check_contradictions: true
      terminology_tier: "project"

  - id: reference-validation
    name: "Cross-Reference Validation"
    type: validation
    step_type: cross_reference
    order: 3
    enabled: true
    timeout_ms: 5000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_broken_links: true
      check_citations: true
      auto_repair_links: false

  - id: post-save-notification
    name: "Notify User"
    type: notification
    order: 4
    enabled: true
    options:
      notify_on_errors: true
      notify_on_warnings: false
      show_summary: true

license_requirement:
  core: false
  writer_pro: true
  teams: true
  enterprise: true

expected_duration_minutes: 2
performance_targets:
  schema_validation_ms: 5000
  consistency_check_ms: 10000
  reference_validation_ms: 5000
```

### 2.2 Pre-Publish Gate Workflow

**File:** `pre-publish-gate.yaml`

```yaml
id: pre-publish-gate
name: "Pre-Publish Gate"
description: "Comprehensive validation before publication"
version: "1.0.0"
trigger: pre_publish
enabled: true
timeout_minutes: 5
skip_on_user_cancel: false

steps:
  - id: schema-validation
    name: "Schema Validation"
    type: validation
    step_type: schema
    order: 1
    enabled: true
    timeout_ms: 10000
    failure_action: halt
    failure_severity: error
    execute_async: true
    options:
      check_json_schema: true
      check_xml_schema: true
      validate_metadata: true
      require_all_properties: true

  - id: grammar-check
    name: "Grammar & Spell Check"
    type: validation
    step_type: grammar
    order: 2
    enabled: true
    timeout_ms: 15000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_spelling: true
      check_grammar: true
      check_readability: true
      language: "en-US"

  - id: consistency-check
    name: "Content Consistency"
    type: validation
    step_type: consistency
    order: 3
    enabled: true
    timeout_ms: 15000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_duplicate_terms: true
      check_contradictions: true
      check_tone_consistency: true
      terminology_tier: "enterprise"

  - id: knowledge-graph-alignment
    name: "Knowledge Graph Alignment"
    type: validation
    step_type: knowledge_graph_alignment
    order: 4
    enabled: true
    timeout_ms: 20000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_entity_references: true
      check_relationship_consistency: true
      allow_new_entities: false

  - id: reference-validation
    name: "Cross-Reference Validation"
    type: validation
    step_type: cross_reference
    order: 5
    enabled: true
    timeout_ms: 10000
    failure_action: halt
    failure_severity: error
    execute_async: true
    options:
      check_broken_links: true
      check_citations: true
      check_image_references: true
      auto_repair_links: false

  - id: publish-gate
    name: "Publication Gate"
    type: gating
    order: 6
    enabled: true
    timeout_ms: 5000
    condition_expression: "validation_count(error) == 0 AND validation_count(warning) <= 3"
    failure_message: "Document cannot be published: validation errors or too many warnings"
    require_all: true
    options:
      allow_override: false

  - id: sync-to-graph
    name: "Sync to Knowledge Graph"
    type: sync
    order: 7
    enabled: true
    timeout_ms: 30000
    direction: document_to_graph
    conflict_strategy: prefer_document
    skip_if_validation_failed: true
    options:
      update_entity_metadata: true
      link_new_entities: false

  - id: post-publish-notification
    name: "Notify Stakeholders"
    type: notification
    order: 8
    enabled: true
    options:
      notify_on_success: true
      notify_on_errors: true
      send_to_approval_team: true
      include_summary: true

license_requirement:
  core: false
  writer_pro: false
  teams: true
  enterprise: true

expected_duration_minutes: 5
performance_targets:
  schema_validation_ms: 10000
  grammar_check_ms: 15000
  consistency_check_ms: 15000
  knowledge_graph_alignment_ms: 20000
  reference_validation_ms: 10000
```

### 2.3 Nightly Health Check Workflow

**File:** `nightly-health-check.yaml`

```yaml
id: nightly-health-check
name: "Nightly Health Check"
description: "Scheduled validation of all documents in workspace"
version: "1.0.0"
trigger: scheduled_nightly
enabled: true
timeout_minutes: 120
schedule: "0 2 * * *"  # 2 AM UTC daily
skip_on_user_cancel: false

steps:
  - id: workspace-scan
    name: "Scan Workspace"
    type: custom
    order: 1
    enabled: true
    timeout_ms: 60000
    options:
      include_archived: false
      include_drafts: true
      max_documents: 1000

  - id: schema-validation-batch
    name: "Batch Schema Validation"
    type: validation
    step_type: schema
    order: 2
    enabled: true
    timeout_ms: 120000
    failure_action: continue
    failure_severity: error
    execute_async: true
    options:
      check_json_schema: true
      check_xml_schema: true
      validate_metadata: true
      batch_size: 50

  - id: consistency-check-batch
    name: "Batch Consistency Check"
    type: validation
    step_type: consistency
    order: 3
    enabled: true
    timeout_ms: 120000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_duplicate_terms: true
      check_contradictions: true
      terminology_tier: "workspace"
      batch_size: 50

  - id: reference-validation-batch
    name: "Batch Reference Validation"
    type: validation
    step_type: cross_reference
    order: 4
    enabled: true
    timeout_ms: 120000
    failure_action: continue
    failure_severity: error
    execute_async: true
    options:
      check_broken_links: true
      check_citations: true
      check_orphaned_entities: true
      batch_size: 50

  - id: knowledge-graph-health
    name: "Knowledge Graph Health"
    type: validation
    step_type: knowledge_graph_alignment
    order: 5
    enabled: true
    timeout_ms: 120000
    failure_action: continue
    failure_severity: warning
    execute_async: true
    options:
      check_entity_orphans: true
      check_broken_relationships: true
      check_stale_entities: true
      batch_size: 50

  - id: generate-health-report
    name: "Generate Health Report"
    type: custom
    order: 6
    enabled: true
    timeout_ms: 30000
    options:
      include_error_summary: true
      include_warning_summary: true
      include_metrics: true
      save_to_workspace: true
      report_format: "pdf"

  - id: notify-admins
    name: "Notify Admins"
    type: notification
    order: 7
    enabled: true
    options:
      notify_role: "workspace_admin"
      include_report: true
      include_metrics: true
      alert_on_failures: true

license_requirement:
  core: false
  writer_pro: false
  teams: true
  enterprise: true

expected_duration_minutes: 120
performance_targets:
  schema_validation_ms: 120000
  consistency_check_ms: 120000
  reference_validation_ms: 120000
  knowledge_graph_health_ms: 120000
```

---

## 3. Workflow Registry

### 3.1 Registry Interface

```csharp
namespace Lexichord.Workflows.Validation.Templates;

/// <summary>
/// Registry for pre-built and custom workflows.
/// </summary>
public interface IWorkflowRegistry
{
    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    Task<WorkflowDefinition> GetWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all available workflows.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> ListWorkflowsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Lists pre-built workflows.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> ListPrebuiltAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Registers custom workflow.
    /// </summary>
    Task<string> RegisterWorkflowAsync(
        WorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Updates existing workflow.
    /// </summary>
    Task UpdateWorkflowAsync(
        string workflowId,
        WorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes custom workflow.
    /// </summary>
    Task DeleteWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);
}
```

### 3.2 Workflow Definition

```csharp
/// <summary>
/// Complete workflow definition.
/// </summary>
public record WorkflowDefinition
{
    /// <summary>Unique workflow ID.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Description.</summary>
    public string? Description { get; init; }

    /// <summary>Workflow version.</summary>
    public required string Version { get; init; }

    /// <summary>Trigger event type.</summary>
    public WorkflowTrigger Trigger { get; init; }

    /// <summary>Is this a pre-built workflow?</summary>
    public bool IsPrebuilt { get; init; }

    /// <summary>Whether enabled by default.</summary>
    public bool EnabledByDefault { get; init; } = true;

    /// <summary>Overall timeout in minutes.</summary>
    public int TimeoutMinutes { get; init; } = 10;

    /// <summary>Workflow steps.</summary>
    public required IReadOnlyList<WorkflowStepDefinition> Steps { get; init; }

    /// <summary>License requirement.</summary>
    public required WorkflowLicenseRequirement LicenseRequirement { get; init; }

    /// <summary>Expected duration in minutes.</summary>
    public int? ExpectedDurationMinutes { get; init; }

    /// <summary>Performance targets.</summary>
    public IReadOnlyDictionary<string, int>? PerformanceTargets { get; init; }

    /// <summary>Created timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Last modified timestamp.</summary>
    public DateTime? ModifiedAt { get; init; }

    /// <summary>Created by user ID.</summary>
    public Guid? CreatedBy { get; init; }
}

public enum WorkflowTrigger
{
    Manual,
    OnSave,
    PrePublish,
    ScheduledNightly,
    Custom
}

public record WorkflowStepDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int Order { get; init; }
    public bool Enabled { get; init; } = true;
    public int? TimeoutMs { get; init; }
    public IReadOnlyDictionary<string, object>? Options { get; init; }
}

public record WorkflowLicenseRequirement
{
    public bool Core { get; init; } = false;
    public bool WriterPro { get; init; } = false;
    public bool Teams { get; init; } = true;
    public bool Enterprise { get; init; } = true;
}
```

---

## 4. Implementation

### 4.1 Workflow Registry Implementation

```csharp
public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IWorkflowStorage _storage;
    private readonly IWorkflowLoader _loader;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<WorkflowRegistry> _logger;
    private readonly Dictionary<string, WorkflowDefinition> _prebuiltCache;

    public WorkflowRegistry(
        IWorkflowStorage storage,
        IWorkflowLoader loader,
        ILicenseService licenseService,
        ILogger<WorkflowRegistry> logger)
    {
        _storage = storage;
        _loader = loader;
        _licenseService = licenseService;
        _logger = logger;
        _prebuiltCache = new Dictionary<string, WorkflowDefinition>();
    }

    public async Task<WorkflowDefinition> GetWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        // Try cache first
        if (_prebuiltCache.TryGetValue(workflowId, out var cached))
        {
            return cached;
        }

        // Try storage (custom workflows)
        var fromStorage = await _storage.GetWorkflowAsync(workflowId, ct);
        if (fromStorage != null)
        {
            return fromStorage;
        }

        // Load from embedded resources (pre-built)
        var prebuilt = await _loader.LoadWorkflowAsync(workflowId, ct);
        if (prebuilt != null)
        {
            _prebuiltCache[workflowId] = prebuilt;
            return prebuilt;
        }

        throw new InvalidOperationException($"Workflow not found: {workflowId}");
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListWorkflowsAsync(
        CancellationToken ct = default)
    {
        var custom = await _storage.ListWorkflowsAsync(ct);
        var prebuilt = await ListPrebuiltAsync(ct);

        var all = new List<WorkflowDefinition>();
        all.AddRange(custom);
        all.AddRange(prebuilt);

        return all.OrderBy(w => w.Name).ToList();
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListPrebuiltAsync(
        CancellationToken ct = default)
    {
        var workflows = new List<WorkflowDefinition>();

        var prebuiltIds = new[]
        {
            "on-save-validation",
            "pre-publish-gate",
            "nightly-health-check"
        };

        foreach (var id in prebuiltIds)
        {
            try
            {
                var workflow = await _loader.LoadWorkflowAsync(id, ct);
                if (workflow != null)
                {
                    workflows.Add(workflow);
                    _prebuiltCache[id] = workflow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pre-built workflow: {WorkflowId}", id);
            }
        }

        return workflows;
    }

    public async Task<string> RegisterWorkflowAsync(
        WorkflowDefinition workflow,
        CancellationToken ct = default)
    {
        // Validate license requirement
        var license = await _licenseService.GetCurrentLicenseAsync(ct);
        if (!IsLicensedFor(workflow.LicenseRequirement, license.Tier))
        {
            throw new LicenseException(
                $"License tier {license.Tier} not sufficient for workflow");
        }

        var id = await _storage.SaveWorkflowAsync(workflow, ct);
        _logger.LogInformation("Workflow registered: {WorkflowId}", id);
        return id;
    }

    public async Task UpdateWorkflowAsync(
        string workflowId,
        WorkflowDefinition workflow,
        CancellationToken ct = default)
    {
        // Cannot update pre-built workflows
        if (_prebuiltCache.ContainsKey(workflowId))
        {
            throw new InvalidOperationException(
                "Pre-built workflows cannot be modified");
        }

        await _storage.UpdateWorkflowAsync(workflowId, workflow, ct);
        _logger.LogInformation("Workflow updated: {WorkflowId}", workflowId);
    }

    public async Task DeleteWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        // Cannot delete pre-built workflows
        if (_prebuiltCache.ContainsKey(workflowId))
        {
            throw new InvalidOperationException(
                "Pre-built workflows cannot be deleted");
        }

        await _storage.DeleteWorkflowAsync(workflowId, ct);
        _logger.LogInformation("Workflow deleted: {WorkflowId}", workflowId);
    }

    private bool IsLicensedFor(
        WorkflowLicenseRequirement requirement,
        LicenseTier tier)
    {
        return tier switch
        {
            LicenseTier.Core => requirement.Core,
            LicenseTier.WriterPro => requirement.WriterPro,
            LicenseTier.Teams => requirement.Teams,
            LicenseTier.Enterprise => requirement.Enterprise,
            _ => false
        };
    }
}
```

### 4.2 Workflow Loader

```csharp
public interface IWorkflowLoader
{
    Task<WorkflowDefinition> LoadWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);
}

public class EmbeddedResourceWorkflowLoader : IWorkflowLoader
{
    private readonly ILogger<EmbeddedResourceWorkflowLoader> _logger;

    public EmbeddedResourceWorkflowLoader(
        ILogger<EmbeddedResourceWorkflowLoader> logger)
    {
        _logger = logger;
    }

    public async Task<WorkflowDefinition> LoadWorkflowAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        try
        {
            var resourceName = $"Lexichord.Workflows.Resources.Workflows.{workflowId}.yaml";
            var assembly = typeof(EmbeddedResourceWorkflowLoader).Assembly;
            var resource = assembly.GetManifestResourceStream(resourceName);

            if (resource == null)
            {
                _logger.LogWarning("Workflow resource not found: {ResourceName}", resourceName);
                return null;
            }

            using var reader = new StreamReader(resource);
            var yaml = await reader.ReadToEndAsync();

            var definition = ParseYamlWorkflow(yaml, workflowId);
            _logger.LogInformation("Loaded workflow: {WorkflowId}", workflowId);
            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow: {WorkflowId}", workflowId);
            throw;
        }
    }

    private WorkflowDefinition ParseYamlWorkflow(string yaml, string workflowId)
    {
        // Parse YAML using YamlDotNet or similar
        var deserializer = new YamlDotNet.Serialization.Deserializer();
        var data = deserializer.Deserialize<Dictionary<object, object>>(yaml);

        // Build WorkflowDefinition from parsed YAML
        // This is a simplified example - full implementation would handle
        // all YAML properties and nested structures

        return new WorkflowDefinition
        {
            Id = workflowId,
            Name = data["name"] as string,
            Description = data["description"] as string,
            Version = data["version"] as string,
            Trigger = Enum.Parse<WorkflowTrigger>(
                data["trigger"]?.ToString() ?? "Manual"),
            IsPrebuilt = true,
            EnabledByDefault = (bool?)data["enabled"] ?? true,
            Steps = new List<WorkflowStepDefinition>(),
            LicenseRequirement = new WorkflowLicenseRequirement(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

---

## 5. Workflow Template Architecture

```
[Workflow Trigger]
        |
        v
[Workflow Registry]
        |
        +---> [Pre-built Cache]
        |
        +---> [Custom Storage]
        |
        v
[Load Workflow Definition]
        |
        +---> [Parse YAML/Config]
        |
        +---> [Build Step Instances]
        |
        +---> [Instantiate Engine]
        |
        v
[Execute Workflow Steps]
        |
        +---> [Step 1] (Validation)
        |
        +---> [Step 2] (Validation)
        |
        +---> [Step N] (Gate/Sync/Notify)
        |
        v
[Collect Results & Metrics]
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Workflow not found | Throw InvalidOperationException |
| License check fails | Throw LicenseException |
| YAML parsing fails | Log error, throw |
| License insufficient | Reject registration |
| Invalid step type | Fail workflow load |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `LoadOnSaveWorkflow` | On-save workflow loads |
| `LoadPrePublishWorkflow` | Pre-publish workflow loads |
| `LoadNightlyWorkflow` | Nightly workflow loads |
| `ListPrebuiltWorkflows` | All prebuilt listed |
| `RegisterCustomWorkflow` | Custom workflow registered |
| `UpdateCustomWorkflow` | Custom workflow updated |
| `DeleteCustomWorkflow` | Custom workflow deleted |
| `CannotUpdatePrebuilt` | Prebuilt protection works |
| `LicenseCheck_WriterPro_OnSaveOnly` | License enforced |
| `ParseYamlWorkflow_Valid` | YAML parsing works |

---

## 8. Performance Considerations

| Aspect | Target |
| :------ | :------ |
| Workflow load from cache | < 10ms |
| First load from resource | < 100ms |
| YAML parsing | < 50ms |
| Registry list | < 200ms |
| Workflow execution | Per step timing |

---

## 9. License Gating

| Tier | Workflows Available |
| :--- | :--- |
| Core | None |
| WriterPro | On-Save Validation only |
| Teams | All (On-Save, Pre-Publish, Nightly) |
| Enterprise | All + custom workflows |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
