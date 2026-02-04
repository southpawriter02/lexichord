# LDS-01: Feature Design Specification — Memory Data Model

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-01` | Matches the Roadmap ID. |
| **Feature Name** | Memory Data Model | The internal display name. |
| **Target Version** | `v0.8.9a` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Model` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
AI agents currently operate statelessly between sessions, forcing users to re-explain context, preferences, and project details. The system needs foundational data structures to represent persistent agent memories with cognitive-inspired categorization.

### 2.2 The Proposed Solution
Define core data models for the memory fabric: `MemoryType` enum for cognitive categorization (semantic, episodic, procedural, working), `Memory` record with content and metadata, `TemporalMetadata` for time-based tracking, `ProvenanceInfo` for source attribution, and `MemoryLink` for inter-memory relationships.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Host` (Core services)
*   **NuGet Packages:**
    *   None (pure data models)

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Models are always available; operations are gated.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Categorizes memories by cognitive function, inspired by cognitive science.
/// </summary>
public enum MemoryType
{
    /// <summary>
    /// Facts and concepts: "User prefers PostgreSQL", "The project uses .NET 8"
    /// </summary>
    Semantic,

    /// <summary>
    /// Specific events: "On Jan 15, we debugged the auth module"
    /// </summary>
    Episodic,

    /// <summary>
    /// How to do things: "To deploy, run 'make prod'"
    /// </summary>
    Procedural,

    /// <summary>
    /// Current session context (not persisted long-term).
    /// </summary>
    Working
}

/// <summary>
/// Represents a single unit of agent memory.
/// </summary>
/// <param name="Id">Unique memory identifier.</param>
/// <param name="Type">The cognitive category of this memory.</param>
/// <param name="Content">The textual content of the memory.</param>
/// <param name="Embedding">Vector embedding for semantic search.</param>
/// <param name="Temporal">Time-based metadata for decay and access tracking.</param>
/// <param name="Provenance">Source attribution for learning context.</param>
/// <param name="CurrentSalience">Computed importance score (0.0 to 1.0).</param>
/// <param name="LinkedMemoryIds">IDs of related memories.</param>
/// <param name="Status">Current lifecycle status of the memory.</param>
/// <param name="ProjectId">Optional project scope for memory isolation.</param>
/// <param name="UserId">Owner of this memory.</param>
public record Memory(
    string Id,
    MemoryType Type,
    string Content,
    float[] Embedding,
    TemporalMetadata Temporal,
    ProvenanceInfo Provenance,
    float CurrentSalience,
    IReadOnlyList<string> LinkedMemoryIds,
    MemoryStatus Status,
    string? ProjectId,
    string UserId);

/// <summary>
/// Lifecycle status of a memory.
/// </summary>
public enum MemoryStatus
{
    /// <summary>
    /// Memory is active and available for retrieval.
    /// </summary>
    Active,

    /// <summary>
    /// Memory has been archived due to low salience or age.
    /// </summary>
    Archived,

    /// <summary>
    /// Memory has been replaced by a newer version.
    /// </summary>
    Superseded
}

/// <summary>
/// Time-based metadata for memory decay and access tracking.
/// </summary>
/// <param name="CreatedAt">When the memory was first created.</param>
/// <param name="LastAccessed">When the memory was last retrieved or used.</param>
/// <param name="AccessCount">Total number of times this memory was accessed.</param>
/// <param name="LastReinforced">When the memory was last explicitly reinforced.</param>
/// <param name="ConfidenceTrajectory">Historical confidence scores over time.</param>
public record TemporalMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessed,
    int AccessCount,
    DateTimeOffset? LastReinforced,
    IReadOnlyList<float> ConfidenceTrajectory)
{
    /// <summary>
    /// Creates initial temporal metadata for a new memory.
    /// </summary>
    public static TemporalMetadata CreateNew() => new(
        CreatedAt: DateTimeOffset.UtcNow,
        LastAccessed: DateTimeOffset.UtcNow,
        AccessCount: 0,
        LastReinforced: null,
        ConfidenceTrajectory: new List<float> { 0.5f });

    /// <summary>
    /// Returns a copy with updated access tracking.
    /// </summary>
    public TemporalMetadata WithAccess() => this with
    {
        LastAccessed = DateTimeOffset.UtcNow,
        AccessCount = AccessCount + 1
    };

    /// <summary>
    /// Returns a copy with reinforcement recorded.
    /// </summary>
    public TemporalMetadata WithReinforcement(float newConfidence) => this with
    {
        LastReinforced = DateTimeOffset.UtcNow,
        ConfidenceTrajectory = ConfidenceTrajectory.Append(newConfidence).ToList()
    };
}

/// <summary>
/// Source attribution for how and where a memory was learned.
/// </summary>
/// <param name="SourceConversationId">The conversation where this was learned.</param>
/// <param name="LearningContext">Description of how the memory was acquired.</param>
/// <param name="SourceDocumentId">Optional document that contributed to this memory.</param>
public record ProvenanceInfo(
    string? SourceConversationId,
    string? LearningContext,
    string? SourceDocumentId)
{
    /// <summary>
    /// Creates provenance for a memory learned from conversation.
    /// </summary>
    public static ProvenanceInfo FromConversation(string conversationId, string context) =>
        new(conversationId, context, null);

    /// <summary>
    /// Creates provenance for a memory learned from a document.
    /// </summary>
    public static ProvenanceInfo FromDocument(string documentId, string context) =>
        new(null, context, documentId);

    /// <summary>
    /// Creates provenance for an explicitly created memory.
    /// </summary>
    public static ProvenanceInfo Explicit(string context) =>
        new(null, context, null);
}

/// <summary>
/// Represents a relationship between two memories.
/// </summary>
/// <param name="FromMemoryId">Source memory in the relationship.</param>
/// <param name="ToMemoryId">Target memory in the relationship.</param>
/// <param name="LinkType">The nature of the relationship.</param>
/// <param name="Strength">Relationship strength (0.0 to 1.0).</param>
/// <param name="CreatedAt">When the link was established.</param>
public record MemoryLink(
    string FromMemoryId,
    string ToMemoryId,
    MemoryLinkType LinkType,
    float Strength,
    DateTimeOffset CreatedAt);

/// <summary>
/// Types of relationships between memories.
/// </summary>
public enum MemoryLinkType
{
    /// <summary>
    /// Memories are topically related.
    /// </summary>
    Related,

    /// <summary>
    /// Target memory was derived from source memory.
    /// </summary>
    DerivedFrom,

    /// <summary>
    /// Memories contain contradictory information.
    /// </summary>
    Contradicts,

    /// <summary>
    /// Target memory replaces source memory.
    /// </summary>
    Supersedes,

    /// <summary>
    /// Memories mutually reinforce each other.
    /// </summary>
    Reinforces
}

/// <summary>
/// Context for creating a new memory.
/// </summary>
/// <param name="UserId">The user who owns this memory.</param>
/// <param name="ConversationId">Optional conversation context.</param>
/// <param name="ProjectId">Optional project scope.</param>
/// <param name="SourceDocumentId">Optional source document.</param>
/// <param name="LearningContext">Description of how this was learned.</param>
public record MemoryContext(
    string UserId,
    string? ConversationId,
    string? ProjectId,
    string? SourceDocumentId,
    string? LearningContext);

/// <summary>
/// Result of a memory recall operation.
/// </summary>
/// <param name="Memories">Retrieved memories ordered by relevance.</param>
/// <param name="TotalMatches">Total number of matching memories.</param>
/// <param name="QueryTime">Time taken for the query.</param>
public record MemoryRecallResult(
    IReadOnlyList<ScoredMemory> Memories,
    int TotalMatches,
    TimeSpan QueryTime);

/// <summary>
/// A memory with its computed relevance score.
/// </summary>
/// <param name="Memory">The retrieved memory.</param>
/// <param name="Score">Combined relevance score (0.0 to 1.0).</param>
/// <param name="MatchReason">Why this memory matched the query.</param>
public record ScoredMemory(
    Memory Memory,
    float Score,
    string MatchReason);
```

---

## 5. Implementation Logic

**Memory Type Classification Guidelines:**
```csharp
public static class MemoryTypeIndicators
{
    /// <summary>
    /// Patterns indicating semantic (factual) memory content.
    /// </summary>
    public static readonly string[] SemanticIndicators = new[]
    {
        "is", "are", "uses", "prefers", "requires", "the",
        "always", "never", "has", "contains", "means"
    };

    /// <summary>
    /// Patterns indicating episodic (event-based) memory content.
    /// </summary>
    public static readonly string[] EpisodicIndicators = new[]
    {
        "yesterday", "last week", "on", "when we", "that time",
        "remember when", "earlier", "ago", "back when", "that day"
    };

    /// <summary>
    /// Patterns indicating procedural (how-to) memory content.
    /// </summary>
    public static readonly string[] ProceduralIndicators = new[]
    {
        "to do", "how to", "steps to", "run", "execute",
        "first", "then", "finally", "in order to", "to accomplish"
    };
}
```

**Memory Builder Pattern:**
```csharp
public class MemoryBuilder
{
    private string? _id;
    private MemoryType _type = MemoryType.Semantic;
    private string _content = string.Empty;
    private float[]? _embedding;
    private TemporalMetadata? _temporal;
    private ProvenanceInfo? _provenance;
    private float _salience = 0.5f;
    private List<string> _linkedIds = new();
    private string? _projectId;
    private string _userId = string.Empty;

    public MemoryBuilder WithId(string id) { _id = id; return this; }
    public MemoryBuilder OfType(MemoryType type) { _type = type; return this; }
    public MemoryBuilder WithContent(string content) { _content = content; return this; }
    public MemoryBuilder WithEmbedding(float[] embedding) { _embedding = embedding; return this; }
    public MemoryBuilder FromContext(MemoryContext context)
    {
        _userId = context.UserId;
        _projectId = context.ProjectId;
        _provenance = new ProvenanceInfo(
            context.ConversationId,
            context.LearningContext,
            context.SourceDocumentId);
        return this;
    }
    public MemoryBuilder WithSalience(float salience) { _salience = salience; return this; }
    public MemoryBuilder LinkedTo(string memoryId) { _linkedIds.Add(memoryId); return this; }

    public Memory Build()
    {
        if (string.IsNullOrEmpty(_userId))
            throw new InvalidOperationException("UserId is required");
        if (string.IsNullOrEmpty(_content))
            throw new InvalidOperationException("Content is required");
        if (_embedding == null)
            throw new InvalidOperationException("Embedding is required");

        return new Memory(
            Id: _id ?? Guid.NewGuid().ToString(),
            Type: _type,
            Content: _content,
            Embedding: _embedding,
            Temporal: _temporal ?? TemporalMetadata.CreateNew(),
            Provenance: _provenance ?? ProvenanceInfo.Explicit("Unknown"),
            CurrentSalience: _salience,
            LinkedMemoryIds: _linkedIds,
            Status: MemoryStatus.Active,
            ProjectId: _projectId,
            UserId: _userId);
    }
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Memory.Model.Created` (Counter by Type)
*   **Log (Debug):** `[MEM:MODEL] Created {MemoryType} memory with {ContentLength} chars`

---

## 7. Acceptance Criteria (QA)

1.  **[Model]** `Memory` record SHALL contain all required fields.
2.  **[Temporal]** `TemporalMetadata.CreateNew()` SHALL initialize with current timestamp.
3.  **[Provenance]** `ProvenanceInfo` factory methods SHALL create valid provenance.
4.  **[Builder]** `MemoryBuilder` SHALL validate required fields before building.
5.  **[Types]** All four `MemoryType` values SHALL be distinguishable.

---

## 8. Test Scenarios

```gherkin
Scenario: Create memory with builder
    Given a MemoryBuilder with content "User prefers dark mode"
    And embedding vector of 1536 dimensions
    And userId "user-123"
    When Build is called
    Then Memory.Content SHALL be "User prefers dark mode"
    And Memory.Status SHALL be Active
    And Memory.Temporal.CreatedAt SHALL be approximately now

Scenario: Memory temporal tracking
    Given a memory with AccessCount 0
    When WithAccess is called on Temporal
    Then AccessCount SHALL be 1
    And LastAccessed SHALL be updated

Scenario: Memory builder validation
    Given a MemoryBuilder with no content
    When Build is called
    Then InvalidOperationException SHALL be thrown

Scenario: Provenance factory methods
    Given a conversation ID "conv-123"
    When ProvenanceInfo.FromConversation is called
    Then SourceConversationId SHALL be "conv-123"
    And SourceDocumentId SHALL be null
```

