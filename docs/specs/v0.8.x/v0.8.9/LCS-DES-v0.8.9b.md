# LDS-01: Feature Design Specification — Memory Storage Schema

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-02` | Matches the Roadmap ID. |
| **Feature Name** | Memory Storage Schema | The internal display name. |
| **Target Version** | `v0.8.9b` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Storage` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The memory data models (v0.8.9a) need persistent storage with efficient retrieval by vector similarity, temporal range, type, and salience. Storage must support the full memory lifecycle including archival and supersession.

### 2.2 The Proposed Solution
Implement PostgreSQL schema with pgvector for embeddings, `IMemoryStore` interface for CRUD operations, and FluentMigrator migrations. Indexes optimize for vector similarity, temporal queries, and salience-based ranking.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a models)
    *   `Lexichord.Host` (Database connection)
*   **NuGet Packages:**
    *   `Npgsql`
    *   `pgvector`
    *   `FluentMigrator`
    *   `Dapper`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Schema exists but operations are gated by license.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Persistent storage for agent memories.
/// </summary>
public interface IMemoryStore
{
    /// <summary>
    /// Store a new memory.
    /// </summary>
    /// <param name="memory">The memory to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored memory with generated ID if not provided.</returns>
    Task<Memory> StoreAsync(Memory memory, CancellationToken ct = default);

    /// <summary>
    /// Retrieve a memory by ID.
    /// </summary>
    /// <param name="memoryId">The memory identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The memory if found, null otherwise.</returns>
    Task<Memory?> GetByIdAsync(string memoryId, CancellationToken ct = default);

    /// <summary>
    /// Retrieve memories by type for a user.
    /// </summary>
    /// <param name="type">The memory type to filter by.</param>
    /// <param name="userId">The user who owns the memories.</param>
    /// <param name="limit">Maximum number to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memories of the specified type ordered by salience.</returns>
    Task<IReadOnlyList<Memory>> GetByTypeAsync(
        MemoryType type,
        string userId,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Update the salience score for a memory.
    /// </summary>
    /// <param name="memoryId">The memory to update.</param>
    /// <param name="newSalience">The new salience score.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateSalienceAsync(string memoryId, float newSalience, CancellationToken ct = default);

    /// <summary>
    /// Record an access to a memory (updates LastAccessed and AccessCount).
    /// </summary>
    /// <param name="memoryId">The memory that was accessed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordAccessAsync(string memoryId, CancellationToken ct = default);

    /// <summary>
    /// Archive a memory (sets status to Archived).
    /// </summary>
    /// <param name="memoryId">The memory to archive.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ArchiveAsync(string memoryId, CancellationToken ct = default);

    /// <summary>
    /// Mark a memory as superseded by another.
    /// </summary>
    /// <param name="memoryId">The memory being superseded.</param>
    /// <param name="replacementId">The memory that replaces it.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SupersedeAsync(string memoryId, string replacementId, CancellationToken ct = default);

    /// <summary>
    /// Store a link between two memories.
    /// </summary>
    /// <param name="link">The memory link to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StoreLinkAsync(MemoryLink link, CancellationToken ct = default);

    /// <summary>
    /// Get all links from a memory.
    /// </summary>
    /// <param name="memoryId">The source memory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All links originating from this memory.</returns>
    Task<IReadOnlyList<MemoryLink>> GetLinksFromAsync(string memoryId, CancellationToken ct = default);

    /// <summary>
    /// Record a confidence value in the trajectory.
    /// </summary>
    /// <param name="memoryId">The memory to update.</param>
    /// <param name="confidence">The confidence value to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordConfidenceAsync(string memoryId, float confidence, CancellationToken ct = default);

    /// <summary>
    /// Bulk update salience for all memories (for decay).
    /// </summary>
    /// <param name="decayFactor">Multiplicative decay factor.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of memories updated.</returns>
    Task<int> DecayAllSalienceAsync(float decayFactor, CancellationToken ct = default);
}
```

---

## 5. Data Persistence (Database)

### 5.1 Migration

*   **Migration ID:** `20260203_1200_CreateMemoryTables`
*   **Module Schema:** `agent_memory`

```csharp
[Migration(20260203_1200)]
public class CreateMemoryTables : Migration
{
    public override void Up()
    {
        // Ensure pgvector extension
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

        // Main memories table
        Create.Table("memories")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("memory_type").AsString(20).NotNullable()
            .WithColumn("content").AsString(int.MaxValue).NotNullable()
            .WithColumn("embedding").AsCustom("VECTOR(1536)").NotNullable()

            // Temporal metadata
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("last_accessed_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("access_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("last_reinforced_at").AsDateTimeOffset().Nullable()

            // Salience
            .WithColumn("current_salience").AsFloat().NotNullable().WithDefaultValue(0.5f)
            .WithColumn("importance_score").AsFloat().NotNullable().WithDefaultValue(0.5f)

            // Provenance
            .WithColumn("source_conversation_id").AsString(50).Nullable()
            .WithColumn("learning_context").AsString(500).Nullable()
            .WithColumn("source_document_id").AsGuid().Nullable()

            // Status
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("active")
            .WithColumn("superseded_by").AsGuid().Nullable()

            // Scoping
            .WithColumn("project_id").AsGuid().Nullable()
            .WithColumn("user_id").AsString(50).NotNullable();

        // Confidence trajectory history
        Create.Table("memory_confidence_history")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("memory_id").AsGuid().NotNullable()
            .WithColumn("confidence").AsFloat().NotNullable()
            .WithColumn("recorded_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("fk_confidence_memory")
            .FromTable("memory_confidence_history").ForeignColumn("memory_id")
            .ToTable("memories").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);

        // Memory links
        Create.Table("memory_links")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("from_memory_id").AsGuid().NotNullable()
            .WithColumn("to_memory_id").AsGuid().NotNullable()
            .WithColumn("link_type").AsString(20).NotNullable()
            .WithColumn("strength").AsFloat().NotNullable().WithDefaultValue(0.5f)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("fk_link_from")
            .FromTable("memory_links").ForeignColumn("from_memory_id")
            .ToTable("memories").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("fk_link_to")
            .FromTable("memory_links").ForeignColumn("to_memory_id")
            .ToTable("memories").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);

        Create.UniqueConstraint("uq_memory_link")
            .OnTable("memory_links")
            .Columns("from_memory_id", "to_memory_id", "link_type");

        // Indexes
        Execute.Sql(@"
            CREATE INDEX memories_embedding_idx ON memories
                USING ivfflat (embedding vector_cosine_ops)
                WITH (lists = 100);
        ");

        Create.Index("ix_memories_temporal")
            .OnTable("memories")
            .OnColumn("created_at").Ascending()
            .OnColumn("last_accessed_at").Ascending();

        Execute.Sql(@"
            CREATE INDEX ix_memories_salience ON memories
                (current_salience DESC)
                WHERE status = 'active';
        ");

        Create.Index("ix_memories_type_user")
            .OnTable("memories")
            .OnColumn("memory_type").Ascending()
            .OnColumn("user_id").Ascending();

        Execute.Sql(@"
            CREATE INDEX ix_memories_user_project ON memories
                (user_id, project_id)
                WHERE status = 'active';
        ");

        Create.Index("ix_confidence_memory")
            .OnTable("memory_confidence_history")
            .OnColumn("memory_id").Ascending()
            .OnColumn("recorded_at").Ascending();

        Create.Index("ix_links_from")
            .OnTable("memory_links")
            .OnColumn("from_memory_id").Ascending();

        Create.Index("ix_links_to")
            .OnTable("memory_links")
            .OnColumn("to_memory_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("memory_links");
        Delete.Table("memory_confidence_history");
        Delete.Table("memories");
    }
}
```

### 5.2 SQL Schema (Reference)

```sql
CREATE TABLE memories (
    id UUID PRIMARY KEY,
    memory_type TEXT NOT NULL, -- 'semantic', 'episodic', 'procedural'
    content TEXT NOT NULL,
    embedding VECTOR(1536) NOT NULL,

    -- Temporal metadata
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_accessed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    access_count INTEGER NOT NULL DEFAULT 0,
    last_reinforced_at TIMESTAMPTZ,

    -- Salience
    current_salience REAL NOT NULL DEFAULT 0.5,
    importance_score REAL NOT NULL DEFAULT 0.5,

    -- Provenance
    source_conversation_id TEXT,
    learning_context TEXT,
    source_document_id UUID,

    -- Status
    status TEXT NOT NULL DEFAULT 'active', -- 'active', 'archived', 'superseded'
    superseded_by UUID REFERENCES memories(id),

    -- Scoping
    project_id UUID,
    user_id TEXT NOT NULL
);

CREATE TABLE memory_confidence_history (
    id UUID PRIMARY KEY,
    memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    confidence REAL NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE memory_links (
    id UUID PRIMARY KEY,
    from_memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    to_memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    link_type TEXT NOT NULL,
    strength REAL NOT NULL DEFAULT 0.5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(from_memory_id, to_memory_id, link_type)
);
```

---

## 6. Implementation Logic

**Memory Store Implementation:**
```csharp
public class PostgresMemoryStore : IMemoryStore
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresMemoryStore> _logger;

    public async Task<Memory> StoreAsync(Memory memory, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO memories (
                id, memory_type, content, embedding,
                created_at, last_accessed_at, access_count, last_reinforced_at,
                current_salience, importance_score,
                source_conversation_id, learning_context, source_document_id,
                status, project_id, user_id
            ) VALUES (
                @Id, @Type, @Content, @Embedding,
                @CreatedAt, @LastAccessed, @AccessCount, @LastReinforced,
                @Salience, @Importance,
                @ConversationId, @LearningContext, @DocumentId,
                @Status, @ProjectId, @UserId
            )
            RETURNING id;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.Parse(memory.Id),
            Type = memory.Type.ToString().ToLowerInvariant(),
            memory.Content,
            Embedding = new Vector(memory.Embedding),
            memory.Temporal.CreatedAt,
            LastAccessed = memory.Temporal.LastAccessed,
            memory.Temporal.AccessCount,
            LastReinforced = memory.Temporal.LastReinforced,
            Salience = memory.CurrentSalience,
            Importance = memory.CurrentSalience,
            ConversationId = memory.Provenance.SourceConversationId,
            memory.Provenance.LearningContext,
            DocumentId = memory.Provenance.SourceDocumentId != null
                ? Guid.Parse(memory.Provenance.SourceDocumentId)
                : (Guid?)null,
            Status = memory.Status.ToString().ToLowerInvariant(),
            ProjectId = memory.ProjectId != null ? Guid.Parse(memory.ProjectId) : (Guid?)null,
            memory.UserId
        });

        _logger.LogInformation(
            "[MEM:STORE] Stored {MemoryType} memory {MemoryId} for user {UserId}",
            memory.Type, id, memory.UserId);

        return memory with { Id = id.ToString() };
    }

    public async Task RecordAccessAsync(string memoryId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE memories
            SET last_accessed_at = NOW(),
                access_count = access_count + 1
            WHERE id = @Id;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Id = Guid.Parse(memoryId) });
    }

    public async Task<int> DecayAllSalienceAsync(float decayFactor, CancellationToken ct)
    {
        const string sql = @"
            UPDATE memories
            SET current_salience = current_salience * @Factor
            WHERE status = 'active'
              AND current_salience > 0.01;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        return await conn.ExecuteAsync(sql, new { Factor = decayFactor });
    }
}
```

---

## 7. Observability & Logging

*   **Metric:** `Agents.Memory.Store.Operations` (Counter by operation type)
*   **Metric:** `Agents.Memory.Store.Latency` (Histogram)
*   **Metric:** `Agents.Memory.Store.Size` (Gauge by user)
*   **Log (Info):** `[MEM:STORE] Stored {MemoryType} memory {MemoryId} for user {UserId}`
*   **Log (Info):** `[MEM:STORE] Archived memory {MemoryId}`
*   **Log (Info):** `[MEM:STORE] Decayed salience for {Count} memories`

---

## 8. Acceptance Criteria (QA)

1.  **[Storage]** `StoreAsync` SHALL persist memory with all fields.
2.  **[Retrieval]** `GetByIdAsync` SHALL return stored memory with correct embedding.
3.  **[Access]** `RecordAccessAsync` SHALL increment AccessCount and update LastAccessed.
4.  **[Archive]** `ArchiveAsync` SHALL set status to "archived".
5.  **[Supersede]** `SupersedeAsync` SHALL link old memory to replacement.
6.  **[Decay]** `DecayAllSalienceAsync` SHALL multiply all salience values.
7.  **[Index]** Vector similarity queries SHALL use ivfflat index.

---

## 9. Test Scenarios

```gherkin
Scenario: Store and retrieve memory
    Given a memory with content "User prefers PostgreSQL"
    When StoreAsync is called
    Then GetByIdAsync SHALL return the same memory
    And embedding SHALL be preserved exactly

Scenario: Record access updates temporal metadata
    Given a stored memory with AccessCount 0
    When RecordAccessAsync is called
    Then AccessCount SHALL be 1
    And LastAccessed SHALL be updated to now

Scenario: Archive memory
    Given an active memory
    When ArchiveAsync is called
    Then status SHALL be "archived"
    And memory SHALL NOT appear in active queries

Scenario: Salience decay
    Given 100 memories with salience 1.0
    When DecayAllSalienceAsync is called with factor 0.9
    Then all memories SHALL have salience 0.9
```

