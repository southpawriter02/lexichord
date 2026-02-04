# LDS-01: Feature Design Specification — Memory Fabric Service

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-06` | Matches the Roadmap ID. |
| **Feature Name** | Memory Fabric Service | The internal display name. |
| **Target Version** | `v0.8.9f` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Fabric` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
Consumers need a single unified API for all memory operations: creating memories, recalling context, reinforcing learning, correcting errors, and linking related memories. The service must orchestrate encoding, storage, retrieval, and salience management.

### 2.2 The Proposed Solution
Implement `IMemoryFabric` as the primary facade for memory operations. It orchestrates `IMemoryEncoder`, `IMemoryStore`, `IMemoryRetriever`, and `ISalienceCalculator`, publishing MediatR events for downstream consumers.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a-e)
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9 for working memory)
*   **NuGet Packages:**
    *   `MediatR`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Core operations require Writer Pro.
*   **Fallback Experience:**
    *   Core users see "Memory features require Writer Pro" message.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Primary interface for agent memory operations.
/// Orchestrates encoding, storage, retrieval, and salience management.
/// </summary>
public interface IMemoryFabric
{
    /// <summary>
    /// Store a new memory, automatically classifying type and generating embedding.
    /// </summary>
    /// <param name="content">The content to remember.</param>
    /// <param name="context">Context about where/how this was learned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created memory.</returns>
    Task<Memory> RememberAsync(
        string content,
        MemoryContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Store a memory with explicit type specification.
    /// </summary>
    /// <param name="content">The content to remember.</param>
    /// <param name="type">The memory type to use.</param>
    /// <param name="context">Context about where/how this was learned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created memory.</returns>
    Task<Memory> RememberAsAsync(
        string content,
        MemoryType type,
        MemoryContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Recall memories relevant to a query.
    /// </summary>
    /// <param name="query">The natural language query.</param>
    /// <param name="options">Retrieval options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Relevant memories.</returns>
    Task<MemoryRecallResult> RecallAsync(
        string query,
        RecallOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Recall memories from a specific time period.
    /// </summary>
    /// <param name="from">Start of time range.</param>
    /// <param name="to">End of time range.</param>
    /// <param name="topicFilter">Optional topic to filter by.</param>
    /// <param name="userId">Optional user filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memories from the time range.</returns>
    Task<MemoryRecallResult> RecallTemporalAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? topicFilter = null,
        string? userId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reinforce a memory (increase its salience).
    /// </summary>
    /// <param name="memoryId">The memory to reinforce.</param>
    /// <param name="reason">Why the memory is being reinforced.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReinforceMemoryAsync(
        string memoryId,
        ReinforcementReason reason,
        CancellationToken ct = default);

    /// <summary>
    /// Correct or update a memory's content.
    /// Creates a new memory that supersedes the old one.
    /// </summary>
    /// <param name="memoryId">The memory to correct.</param>
    /// <param name="correctedContent">The corrected content.</param>
    /// <param name="correctionReason">Why the correction was made.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new corrected memory.</returns>
    Task<Memory> UpdateMemoryAsync(
        string memoryId,
        string correctedContent,
        string correctionReason,
        CancellationToken ct = default);

    /// <summary>
    /// Link two related memories.
    /// </summary>
    /// <param name="fromMemoryId">Source memory.</param>
    /// <param name="toMemoryId">Target memory.</param>
    /// <param name="linkType">Type of relationship.</param>
    /// <param name="strength">Relationship strength (0.0 to 1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    Task LinkMemoriesAsync(
        string fromMemoryId,
        string toMemoryId,
        MemoryLinkType linkType,
        float strength = 0.5f,
        CancellationToken ct = default);

    /// <summary>
    /// Archive a memory (remove from active retrieval).
    /// </summary>
    /// <param name="memoryId">The memory to archive.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ArchiveMemoryAsync(
        string memoryId,
        CancellationToken ct = default);

    /// <summary>
    /// Get a memory by ID.
    /// </summary>
    /// <param name="memoryId">The memory ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The memory if found.</returns>
    Task<Memory?> GetMemoryAsync(
        string memoryId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if content contradicts existing memories.
    /// </summary>
    /// <param name="content">Content to check.</param>
    /// <param name="userId">User context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Potentially contradicting memories.</returns>
    Task<MemoryRecallResult> CheckContradictionsAsync(
        string content,
        string userId,
        CancellationToken ct = default);
}
```

---

## 5. Implementation Logic

**Memory Fabric Implementation:**
```csharp
public class MemoryFabric : IMemoryFabric
{
    private readonly IMemoryEncoder _encoder;
    private readonly IMemoryStore _store;
    private readonly IMemoryRetriever _retriever;
    private readonly ISalienceCalculator _salienceCalculator;
    private readonly IMediator _mediator;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<MemoryFabric> _logger;

    public async Task<Memory> RememberAsync(
        string content,
        MemoryContext context,
        CancellationToken ct)
    {
        // License check
        if (!_licenseService.HasFeature("Agents.Memory.Fabric"))
        {
            throw new LicenseException("Memory features require Writer Pro license.");
        }

        // Encode and store
        var memory = await _encoder.EncodeAsync(content, context, ct);
        memory = await _store.StoreAsync(memory, ct);

        _logger.LogInformation(
            "[MEM:FABRIC] Remembered {MemoryType} memory {MemoryId}: {ContentPreview}",
            memory.Type, memory.Id, content[..Math.Min(50, content.Length)]);

        // Publish event
        await _mediator.Publish(new MemoryCreatedEvent(memory), ct);

        return memory;
    }

    public async Task<Memory> RememberAsAsync(
        string content,
        MemoryType type,
        MemoryContext context,
        CancellationToken ct)
    {
        if (!_licenseService.HasFeature("Agents.Memory.Fabric"))
        {
            throw new LicenseException("Memory features require Writer Pro license.");
        }

        var memory = await _encoder.EncodeWithTypeAsync(content, type, context, ct);
        memory = await _store.StoreAsync(memory, ct);

        _logger.LogInformation(
            "[MEM:FABRIC] Remembered explicit {MemoryType} memory {MemoryId}",
            type, memory.Id);

        await _mediator.Publish(new MemoryCreatedEvent(memory), ct);

        return memory;
    }

    public async Task<MemoryRecallResult> RecallAsync(
        string query,
        RecallOptions options,
        CancellationToken ct)
    {
        if (!_licenseService.HasFeature("Agents.Memory.Fabric"))
        {
            throw new LicenseException("Memory features require Writer Pro license.");
        }

        return await _retriever.RecallAsync(query, options, ct);
    }

    public async Task<MemoryRecallResult> RecallTemporalAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? topicFilter,
        string? userId,
        CancellationToken ct)
    {
        if (!_licenseService.HasFeature("Agents.Memory.Fabric"))
        {
            throw new LicenseException("Memory features require Writer Pro license.");
        }

        return await _retriever.RecallTemporalAsync(from, to, topicFilter, userId, ct);
    }

    public async Task ReinforceMemoryAsync(
        string memoryId,
        ReinforcementReason reason,
        CancellationToken ct)
    {
        var memory = await _store.GetByIdAsync(memoryId, ct);
        if (memory == null)
        {
            _logger.LogWarning("[MEM:FABRIC] Cannot reinforce non-existent memory {MemoryId}", memoryId);
            return;
        }

        await _salienceCalculator.ReinforceAsync(memoryId, reason, ct);

        _logger.LogInformation(
            "[MEM:FABRIC] Reinforced memory {MemoryId} ({Reason})",
            memoryId, reason);

        await _mediator.Publish(new MemoryReinforcedEvent(memoryId, reason), ct);
    }

    public async Task<Memory> UpdateMemoryAsync(
        string memoryId,
        string correctedContent,
        string correctionReason,
        CancellationToken ct)
    {
        var original = await _store.GetByIdAsync(memoryId, ct);
        if (original == null)
        {
            throw new InvalidOperationException($"Memory {memoryId} not found");
        }

        // Create new memory with corrected content
        var context = new MemoryContext(
            original.UserId,
            null,
            original.ProjectId,
            null,
            $"Correction of {memoryId}: {correctionReason}");

        var corrected = await _encoder.EncodeWithTypeAsync(
            correctedContent,
            original.Type,
            context,
            ct);

        // Boost salience for corrected memory
        corrected = corrected with { CurrentSalience = Math.Min(1.0f, original.CurrentSalience + 0.25f) };

        corrected = await _store.StoreAsync(corrected, ct);

        // Mark original as superseded
        await _store.SupersedeAsync(memoryId, corrected.Id, ct);

        // Link the memories
        await _store.StoreLinkAsync(new MemoryLink(
            corrected.Id,
            memoryId,
            MemoryLinkType.Supersedes,
            1.0f,
            DateTimeOffset.UtcNow), ct);

        _logger.LogInformation(
            "[MEM:FABRIC] Updated memory {OldId} -> {NewId}: {Reason}",
            memoryId, corrected.Id, correctionReason);

        await _mediator.Publish(new MemoryUpdatedEvent(memoryId, corrected.Id, correctionReason), ct);

        return corrected;
    }

    public async Task LinkMemoriesAsync(
        string fromMemoryId,
        string toMemoryId,
        MemoryLinkType linkType,
        float strength,
        CancellationToken ct)
    {
        var link = new MemoryLink(
            fromMemoryId,
            toMemoryId,
            linkType,
            strength,
            DateTimeOffset.UtcNow);

        await _store.StoreLinkAsync(link, ct);

        _logger.LogInformation(
            "[MEM:FABRIC] Linked {FromId} -> {ToId} ({LinkType}, strength {Strength})",
            fromMemoryId, toMemoryId, linkType, strength);

        await _mediator.Publish(new MemoriesLinkedEvent(link), ct);
    }

    public async Task ArchiveMemoryAsync(string memoryId, CancellationToken ct)
    {
        await _store.ArchiveAsync(memoryId, ct);

        _logger.LogInformation("[MEM:FABRIC] Archived memory {MemoryId}", memoryId);

        await _mediator.Publish(new MemoryArchivedEvent(memoryId), ct);
    }

    public async Task<Memory?> GetMemoryAsync(string memoryId, CancellationToken ct)
    {
        return await _store.GetByIdAsync(memoryId, ct);
    }

    public async Task<MemoryRecallResult> CheckContradictionsAsync(
        string content,
        string userId,
        CancellationToken ct)
    {
        return await _retriever.FindContradictionsAsync(content, userId, ct);
    }
}
```

---

## 6. MediatR Events

```csharp
namespace Lexichord.Modules.Agents.Memory.Events;

/// <summary>
/// Published when a new memory is created.
/// </summary>
public record MemoryCreatedEvent(Memory Memory) : INotification;

/// <summary>
/// Published when a memory is reinforced.
/// </summary>
public record MemoryReinforcedEvent(
    string MemoryId,
    ReinforcementReason Reason) : INotification;

/// <summary>
/// Published when a memory is updated/corrected.
/// </summary>
public record MemoryUpdatedEvent(
    string OriginalMemoryId,
    string NewMemoryId,
    string Reason) : INotification;

/// <summary>
/// Published when a memory is archived.
/// </summary>
public record MemoryArchivedEvent(string MemoryId) : INotification;

/// <summary>
/// Published when a memory is superseded.
/// </summary>
public record MemorySupersededEvent(
    string SupersededMemoryId,
    string ReplacementMemoryId) : INotification;

/// <summary>
/// Published when memories are linked.
/// </summary>
public record MemoriesLinkedEvent(MemoryLink Link) : INotification;
```

---

## 7. Observability & Logging

*   **Metric:** `Agents.Memory.Fabric.Operations` (Counter by operation type)
*   **Metric:** `Agents.Memory.Fabric.Latency` (Histogram by operation)
*   **Metric:** `Agents.Memory.Fabric.MemoriesCreated` (Counter by Type)
*   **Log (Info):** `[MEM:FABRIC] Remembered {MemoryType} memory {MemoryId}: {ContentPreview}`
*   **Log (Info):** `[MEM:FABRIC] Reinforced memory {MemoryId} ({Reason})`
*   **Log (Info):** `[MEM:FABRIC] Updated memory {OldId} -> {NewId}: {Reason}`
*   **Log (Info):** `[MEM:FABRIC] Archived memory {MemoryId}`
*   **Log (Info):** `[MEM:FABRIC] Linked {FromId} -> {ToId} ({LinkType})`

---

## 8. Acceptance Criteria (QA)

1.  **[Remember]** `RememberAsync` SHALL encode and store memory.
2.  **[Remember]** `RememberAsync` SHALL publish `MemoryCreatedEvent`.
3.  **[Recall]** `RecallAsync` SHALL delegate to retriever.
4.  **[Reinforce]** `ReinforceMemoryAsync` SHALL boost salience.
5.  **[Update]** `UpdateMemoryAsync` SHALL create superseding memory.
6.  **[Update]** `UpdateMemoryAsync` SHALL link with Supersedes relationship.
7.  **[Link]** `LinkMemoriesAsync` SHALL create memory relationship.
8.  **[License]** All operations SHALL check license before execution.

---

## 9. Test Scenarios

```gherkin
Scenario: Remember and recall
    Given a user with Writer Pro license
    When RememberAsync is called with "PostgreSQL is the database"
    And RecallAsync is called with query "database"
    Then the memory SHALL be in recall results

Scenario: Memory update creates superseding version
    Given an existing memory about "API uses HTTP"
    When UpdateMemoryAsync is called with "API uses gRPC"
    Then a new memory SHALL be created
    And original memory SHALL have status "superseded"
    And new memory SHALL link to original with Supersedes

Scenario: Reinforcement increases salience
    Given a memory with salience 0.5
    When ReinforceMemoryAsync is called with ExplicitUserConfirmation
    Then salience SHALL be approximately 0.8

Scenario: License check blocks unlicensed users
    Given a user with Core license
    When RememberAsync is called
    Then LicenseException SHALL be thrown
```

