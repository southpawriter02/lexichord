# LCS-DES-056-KG-h: Claim Repository

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-056-KG-h |
| **Feature ID** | KG-056h |
| **Feature Name** | Claim Repository |
| **Target Version** | v0.5.6h |
| **Module Scope** | `Lexichord.Knowledge.Claims` |
| **Swimlane** | Memory |
| **License Tier** | Teams (full), Enterprise (full) |
| **Feature Gate Key** | `knowledge.claims.repository.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Extracted claims need persistent storage with efficient querying capabilities. The **Claim Repository** provides PostgreSQL-backed storage with full-text search, versioning, and bulk operations for managing claims across documents.

### 2.2 The Proposed Solution

Implement a repository layer that:

- Stores claims in PostgreSQL with proper indexing
- Supports full-text search on claim content and evidence
- Tracks claim versions for change detection
- Provides bulk upsert/delete operations
- Caches frequently accessed claims
- Publishes events for claim changes

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.6e: Claim data model
- v0.0.5b: `IDbConnectionFactory` — Database connections

**NuGet Packages:**
- `Dapper` — Data access
- `Npgsql` — PostgreSQL driver
- `MediatR` — Event publishing
- `Microsoft.Extensions.Caching.Memory` — Claim caching

### 3.2 Module Placement

```
Lexichord.Knowledge/
├── Claims/
│   └── Repository/
│       ├── IClaimRepository.cs
│       ├── ClaimRepository.cs
│       ├── ClaimSearchCriteria.cs
│       ├── ClaimQueryResult.cs
│       └── Events/
│           ├── ClaimCreatedEvent.cs
│           ├── ClaimUpdatedEvent.cs
│           └── ClaimDeletedEvent.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Load with claim extraction
- **Fallback Experience:** WriterPro: read-only; Teams+: full CRUD

---

## 4. Data Contract (The API)

### 4.1 Core Interface

```csharp
namespace Lexichord.Knowledge.Claims.Repository;

/// <summary>
/// Repository for claim persistence.
/// </summary>
public interface IClaimRepository
{
    /// <summary>
    /// Gets a claim by ID.
    /// </summary>
    Task<Claim?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple claims by IDs.
    /// </summary>
    Task<IReadOnlyList<Claim>> GetManyAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Searches claims by criteria.
    /// </summary>
    Task<ClaimQueryResult> SearchAsync(
        ClaimSearchCriteria criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all claims for a document.
    /// </summary>
    Task<IReadOnlyList<Claim>> GetByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets claims for an entity (as subject or object).
    /// </summary>
    Task<IReadOnlyList<Claim>> GetByEntityAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets claims by predicate type.
    /// </summary>
    Task<IReadOnlyList<Claim>> GetByPredicateAsync(
        string predicate,
        Guid? projectId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new claim.
    /// </summary>
    Task<Claim> CreateAsync(Claim claim, CancellationToken ct = default);

    /// <summary>
    /// Creates multiple claims.
    /// </summary>
    Task<IReadOnlyList<Claim>> CreateManyAsync(
        IEnumerable<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing claim.
    /// </summary>
    Task<Claim> UpdateAsync(Claim claim, CancellationToken ct = default);

    /// <summary>
    /// Upserts claims (create or update).
    /// </summary>
    Task<BulkUpsertResult> UpsertManyAsync(
        IEnumerable<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a claim.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes all claims for a document.
    /// </summary>
    Task<int> DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets claim count by criteria.
    /// </summary>
    Task<int> CountAsync(
        ClaimSearchCriteria? criteria = null,
        CancellationToken ct = default);

    /// <summary>
    /// Full-text search on claim content.
    /// </summary>
    Task<IReadOnlyList<Claim>> FullTextSearchAsync(
        string query,
        Guid? projectId = null,
        int limit = 50,
        CancellationToken ct = default);
}
```

### 4.2 Search Criteria

```csharp
namespace Lexichord.Knowledge.Claims.Repository;

/// <summary>
/// Criteria for searching claims.
/// </summary>
public record ClaimSearchCriteria
{
    /// <summary>Filter by project.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Filter by document.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Filter by subject entity ID.</summary>
    public Guid? SubjectEntityId { get; init; }

    /// <summary>Filter by object entity ID.</summary>
    public Guid? ObjectEntityId { get; init; }

    /// <summary>Filter by predicate.</summary>
    public string? Predicate { get; init; }

    /// <summary>Filter by predicates (any of).</summary>
    public IReadOnlyList<string>? Predicates { get; init; }

    /// <summary>Filter by validation status.</summary>
    public ClaimValidationStatus? ValidationStatus { get; init; }

    /// <summary>Filter by minimum confidence.</summary>
    public float? MinConfidence { get; init; }

    /// <summary>Filter by maximum confidence.</summary>
    public float? MaxConfidence { get; init; }

    /// <summary>Filter by reviewed status.</summary>
    public bool? IsReviewed { get; init; }

    /// <summary>Filter by active status.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Filter by extraction method.</summary>
    public ClaimExtractionMethod? ExtractionMethod { get; init; }

    /// <summary>Full-text search query.</summary>
    public string? TextQuery { get; init; }

    /// <summary>Extracted after date.</summary>
    public DateTimeOffset? ExtractedAfter { get; init; }

    /// <summary>Extracted before date.</summary>
    public DateTimeOffset? ExtractedBefore { get; init; }

    /// <summary>Sort field.</summary>
    public ClaimSortField SortBy { get; init; } = ClaimSortField.ExtractedAt;

    /// <summary>Sort direction.</summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    /// <summary>Page number (0-based).</summary>
    public int Page { get; init; }

    /// <summary>Page size.</summary>
    public int PageSize { get; init; } = 50;

    /// <summary>Skip count (alternative to page).</summary>
    public int? Skip { get; init; }

    /// <summary>Take count (alternative to page size).</summary>
    public int? Take { get; init; }
}

public enum ClaimSortField
{
    ExtractedAt,
    Confidence,
    Predicate,
    ValidationStatus
}

public enum SortDirection
{
    Ascending,
    Descending
}
```

### 4.3 Query Result

```csharp
namespace Lexichord.Knowledge.Claims.Repository;

/// <summary>
/// Paginated query result.
/// </summary>
public record ClaimQueryResult
{
    /// <summary>Claims in current page.</summary>
    public required IReadOnlyList<Claim> Claims { get; init; }

    /// <summary>Total count matching criteria.</summary>
    public int TotalCount { get; init; }

    /// <summary>Current page (0-based).</summary>
    public int Page { get; init; }

    /// <summary>Page size.</summary>
    public int PageSize { get; init; }

    /// <summary>Total pages.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Whether there are more pages.</summary>
    public bool HasMore => Page < TotalPages - 1;

    /// <summary>Creates an empty result.</summary>
    public static ClaimQueryResult Empty => new()
    {
        Claims = Array.Empty<Claim>()
    };
}

/// <summary>
/// Result of bulk upsert operation.
/// </summary>
public record BulkUpsertResult
{
    /// <summary>Number of claims created.</summary>
    public int CreatedCount { get; init; }

    /// <summary>Number of claims updated.</summary>
    public int UpdatedCount { get; init; }

    /// <summary>Number of claims unchanged.</summary>
    public int UnchangedCount { get; init; }

    /// <summary>Claims that failed to upsert.</summary>
    public IReadOnlyList<ClaimUpsertError>? Errors { get; init; }

    /// <summary>Total processed.</summary>
    public int TotalProcessed => CreatedCount + UpdatedCount + UnchangedCount;

    /// <summary>Whether all succeeded.</summary>
    public bool AllSucceeded => Errors == null || !Errors.Any();
}

public record ClaimUpsertError
{
    public Guid ClaimId { get; init; }
    public string Error { get; init; } = "";
}
```

### 4.4 Events

```csharp
namespace Lexichord.Knowledge.Claims.Repository.Events;

/// <summary>
/// Event raised when a claim is created.
/// </summary>
public record ClaimCreatedEvent : INotification
{
    public required Claim Claim { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when a claim is updated.
/// </summary>
public record ClaimUpdatedEvent : INotification
{
    public required Claim Claim { get; init; }
    public required Claim PreviousClaim { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when a claim is deleted.
/// </summary>
public record ClaimDeletedEvent : INotification
{
    public Guid ClaimId { get; init; }
    public Guid DocumentId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when document claims are replaced.
/// </summary>
public record DocumentClaimsReplacedEvent : INotification
{
    public Guid DocumentId { get; init; }
    public int OldClaimCount { get; init; }
    public int NewClaimCount { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
```

---

## 5. Implementation Logic

### 5.1 ClaimRepository

```csharp
namespace Lexichord.Knowledge.Claims.Repository;

/// <summary>
/// PostgreSQL-backed claim repository.
/// </summary>
public class ClaimRepository : IClaimRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMemoryCache _cache;
    private readonly IMediator _mediator;
    private readonly ILogger<ClaimRepository> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Claim?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = $"claim:{id}";
        if (_cache.TryGetValue(cacheKey, out Claim? cached))
        {
            return cached;
        }

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<ClaimRow>(
            @"SELECT * FROM claims WHERE id = @Id AND is_active = true",
            new { Id = id });

        if (row == null) return null;

        var claim = MapToClaim(row);
        _cache.Set(cacheKey, claim, CacheDuration);

        return claim;
    }

    public async Task<ClaimQueryResult> SearchAsync(
        ClaimSearchCriteria criteria,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var (whereClause, parameters) = BuildWhereClause(criteria);
        var orderBy = BuildOrderBy(criteria);

        var skip = criteria.Skip ?? criteria.Page * criteria.PageSize;
        var take = criteria.Take ?? criteria.PageSize;

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM claims WHERE {whereClause}";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);

        // Get page
        var querySql = $@"
            SELECT * FROM claims
            WHERE {whereClause}
            ORDER BY {orderBy}
            OFFSET @Skip LIMIT @Take";

        parameters.Add("Skip", skip);
        parameters.Add("Take", take);

        var rows = await conn.QueryAsync<ClaimRow>(querySql, parameters);
        var claims = rows.Select(MapToClaim).ToList();

        return new ClaimQueryResult
        {
            Claims = claims,
            TotalCount = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize
        };
    }

    public async Task<IReadOnlyList<Claim>> GetByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var rows = await conn.QueryAsync<ClaimRow>(
            @"SELECT * FROM claims
              WHERE document_id = @DocumentId AND is_active = true
              ORDER BY evidence_start_offset",
            new { DocumentId = documentId });

        return rows.Select(MapToClaim).ToList();
    }

    public async Task<IReadOnlyList<Claim>> GetByEntityAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var rows = await conn.QueryAsync<ClaimRow>(
            @"SELECT * FROM claims
              WHERE (subject_entity_id = @EntityId OR object_entity_id = @EntityId)
                AND is_active = true
              ORDER BY extracted_at DESC",
            new { EntityId = entityId });

        return rows.Select(MapToClaim).ToList();
    }

    public async Task<Claim> CreateAsync(Claim claim, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var row = MapToRow(claim);

        await conn.ExecuteAsync(
            @"INSERT INTO claims (
                id, project_id, document_id, subject_entity_id, subject_entity_type,
                subject_surface_form, predicate, object_type, object_entity_id,
                object_entity_type, object_surface_form, object_literal_value,
                object_literal_type, confidence, validation_status, is_reviewed,
                extraction_method, pattern_id, evidence_sentence, evidence_start_offset,
                evidence_end_offset, is_active, version, extracted_at
              ) VALUES (
                @Id, @ProjectId, @DocumentId, @SubjectEntityId, @SubjectEntityType,
                @SubjectSurfaceForm, @Predicate, @ObjectType, @ObjectEntityId,
                @ObjectEntityType, @ObjectSurfaceForm, @ObjectLiteralValue,
                @ObjectLiteralType, @Confidence, @ValidationStatus, @IsReviewed,
                @ExtractionMethod, @PatternId, @EvidenceSentence, @EvidenceStartOffset,
                @EvidenceEndOffset, @IsActive, @Version, @ExtractedAt
              )",
            row);

        await _mediator.Publish(new ClaimCreatedEvent { Claim = claim }, ct);

        return claim;
    }

    public async Task<IReadOnlyList<Claim>> CreateManyAsync(
        IEnumerable<Claim> claims,
        CancellationToken ct = default)
    {
        var claimList = claims.ToList();
        if (!claimList.Any()) return Array.Empty<Claim>();

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var transaction = conn.BeginTransaction();

        try
        {
            foreach (var claim in claimList)
            {
                var row = MapToRow(claim);
                await conn.ExecuteAsync(GetInsertSql(), row, transaction);
            }

            transaction.Commit();

            foreach (var claim in claimList)
            {
                await _mediator.Publish(new ClaimCreatedEvent { Claim = claim }, ct);
            }

            return claimList;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<BulkUpsertResult> UpsertManyAsync(
        IEnumerable<Claim> claims,
        CancellationToken ct = default)
    {
        var claimList = claims.ToList();
        if (!claimList.Any())
        {
            return new BulkUpsertResult();
        }

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        using var transaction = conn.BeginTransaction();

        int created = 0, updated = 0, unchanged = 0;
        var errors = new List<ClaimUpsertError>();

        try
        {
            foreach (var claim in claimList)
            {
                try
                {
                    var existing = await conn.QueryFirstOrDefaultAsync<ClaimRow>(
                        "SELECT * FROM claims WHERE id = @Id",
                        new { claim.Id },
                        transaction);

                    if (existing == null)
                    {
                        await conn.ExecuteAsync(GetInsertSql(), MapToRow(claim), transaction);
                        created++;
                    }
                    else if (HasChanged(existing, claim))
                    {
                        await conn.ExecuteAsync(GetUpdateSql(), MapToRow(claim), transaction);
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ClaimUpsertError
                    {
                        ClaimId = claim.Id,
                        Error = ex.Message
                    });
                }
            }

            transaction.Commit();

            return new BulkUpsertResult
            {
                CreatedCount = created,
                UpdatedCount = updated,
                UnchangedCount = unchanged,
                Errors = errors.Any() ? errors : null
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IReadOnlyList<Claim>> FullTextSearchAsync(
        string query,
        Guid? projectId = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT * FROM claims
            WHERE is_active = true
              AND (
                to_tsvector('english', evidence_sentence) @@ plainto_tsquery('english', @Query)
                OR subject_surface_form ILIKE @Pattern
                OR object_surface_form ILIKE @Pattern
              )
              " + (projectId.HasValue ? "AND project_id = @ProjectId" : "") + @"
            ORDER BY ts_rank(to_tsvector('english', evidence_sentence), plainto_tsquery('english', @Query)) DESC
            LIMIT @Limit";

        var rows = await conn.QueryAsync<ClaimRow>(sql, new
        {
            Query = query,
            Pattern = $"%{query}%",
            ProjectId = projectId,
            Limit = limit
        });

        return rows.Select(MapToClaim).ToList();
    }

    private (string whereClause, DynamicParameters parameters) BuildWhereClause(
        ClaimSearchCriteria criteria)
    {
        var conditions = new List<string> { "is_active = true" };
        var parameters = new DynamicParameters();

        if (criteria.ProjectId.HasValue)
        {
            conditions.Add("project_id = @ProjectId");
            parameters.Add("ProjectId", criteria.ProjectId.Value);
        }

        if (criteria.DocumentId.HasValue)
        {
            conditions.Add("document_id = @DocumentId");
            parameters.Add("DocumentId", criteria.DocumentId.Value);
        }

        if (criteria.SubjectEntityId.HasValue)
        {
            conditions.Add("subject_entity_id = @SubjectEntityId");
            parameters.Add("SubjectEntityId", criteria.SubjectEntityId.Value);
        }

        if (criteria.ObjectEntityId.HasValue)
        {
            conditions.Add("object_entity_id = @ObjectEntityId");
            parameters.Add("ObjectEntityId", criteria.ObjectEntityId.Value);
        }

        if (!string.IsNullOrEmpty(criteria.Predicate))
        {
            conditions.Add("predicate = @Predicate");
            parameters.Add("Predicate", criteria.Predicate);
        }

        if (criteria.Predicates?.Any() == true)
        {
            conditions.Add("predicate = ANY(@Predicates)");
            parameters.Add("Predicates", criteria.Predicates.ToArray());
        }

        if (criteria.ValidationStatus.HasValue)
        {
            conditions.Add("validation_status = @ValidationStatus");
            parameters.Add("ValidationStatus", criteria.ValidationStatus.Value.ToString());
        }

        if (criteria.MinConfidence.HasValue)
        {
            conditions.Add("confidence >= @MinConfidence");
            parameters.Add("MinConfidence", criteria.MinConfidence.Value);
        }

        if (!string.IsNullOrEmpty(criteria.TextQuery))
        {
            conditions.Add("to_tsvector('english', evidence_sentence) @@ plainto_tsquery('english', @TextQuery)");
            parameters.Add("TextQuery", criteria.TextQuery);
        }

        return (string.Join(" AND ", conditions), parameters);
    }

    private static string BuildOrderBy(ClaimSearchCriteria criteria)
    {
        var field = criteria.SortBy switch
        {
            ClaimSortField.ExtractedAt => "extracted_at",
            ClaimSortField.Confidence => "confidence",
            ClaimSortField.Predicate => "predicate",
            ClaimSortField.ValidationStatus => "validation_status",
            _ => "extracted_at"
        };

        var direction = criteria.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";

        return $"{field} {direction}";
    }

    // Mapping methods...
    private static Claim MapToClaim(ClaimRow row) { /* ... */ }
    private static ClaimRow MapToRow(Claim claim) { /* ... */ }
}
```

---

## 6. Database Schema

```sql
-- Claims table
CREATE TABLE claims (
    id UUID PRIMARY KEY,
    project_id UUID NOT NULL,
    document_id UUID NOT NULL,

    -- Subject
    subject_entity_id UUID,
    subject_entity_type VARCHAR(100) NOT NULL,
    subject_surface_form VARCHAR(500) NOT NULL,

    -- Predicate
    predicate VARCHAR(100) NOT NULL,

    -- Object
    object_type VARCHAR(20) NOT NULL, -- 'entity' or 'literal'
    object_entity_id UUID,
    object_entity_type VARCHAR(100),
    object_surface_form VARCHAR(500),
    object_literal_value JSONB,
    object_literal_type VARCHAR(50),

    -- Confidence & Validation
    confidence FLOAT NOT NULL DEFAULT 1.0,
    validation_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    validation_messages JSONB,

    -- Review
    is_reviewed BOOLEAN NOT NULL DEFAULT FALSE,
    review_notes TEXT,

    -- Extraction
    extraction_method VARCHAR(50) NOT NULL,
    pattern_id VARCHAR(100),

    -- Evidence
    evidence_sentence TEXT NOT NULL,
    evidence_start_offset INT NOT NULL,
    evidence_end_offset INT NOT NULL,
    evidence_section VARCHAR(500),

    -- Metadata
    metadata JSONB,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    version INT NOT NULL DEFAULT 1,
    extracted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,

    -- Indexes
    CONSTRAINT fk_project FOREIGN KEY (project_id) REFERENCES projects(id),
    CONSTRAINT fk_document FOREIGN KEY (document_id) REFERENCES documents(id)
);

-- Indexes
CREATE INDEX idx_claims_project ON claims(project_id);
CREATE INDEX idx_claims_document ON claims(document_id);
CREATE INDEX idx_claims_subject_entity ON claims(subject_entity_id) WHERE subject_entity_id IS NOT NULL;
CREATE INDEX idx_claims_object_entity ON claims(object_entity_id) WHERE object_entity_id IS NOT NULL;
CREATE INDEX idx_claims_predicate ON claims(predicate);
CREATE INDEX idx_claims_validation ON claims(validation_status);
CREATE INDEX idx_claims_confidence ON claims(confidence);
CREATE INDEX idx_claims_extracted ON claims(extracted_at DESC);

-- Full-text search index
CREATE INDEX idx_claims_fts ON claims
    USING GIN (to_tsvector('english', evidence_sentence));

-- Combined index for common queries
CREATE INDEX idx_claims_doc_active ON claims(document_id, is_active)
    WHERE is_active = true;
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Integration")]
[Trait("Feature", "v0.5.6h")]
public class ClaimRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly IClaimRepository _repository;

    [Fact]
    public async Task CreateAsync_ValidClaim_PersistsAndPublishesEvent()
    {
        // Arrange
        var claim = CreateTestClaim();

        // Act
        var created = await _repository.CreateAsync(claim);

        // Assert
        created.Id.Should().Be(claim.Id);
        var retrieved = await _repository.GetAsync(claim.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Subject.SurfaceForm.Should().Be(claim.Subject.SurfaceForm);
    }

    [Fact]
    public async Task SearchAsync_ByPredicate_ReturnsMatching()
    {
        // Arrange
        var claims = new[]
        {
            CreateTestClaim(predicate: ClaimPredicate.ACCEPTS),
            CreateTestClaim(predicate: ClaimPredicate.RETURNS),
            CreateTestClaim(predicate: ClaimPredicate.ACCEPTS)
        };
        await _repository.CreateManyAsync(claims);

        // Act
        var result = await _repository.SearchAsync(new ClaimSearchCriteria
        {
            Predicate = ClaimPredicate.ACCEPTS
        });

        // Assert
        result.Claims.Should().HaveCount(2);
        result.Claims.Should().OnlyContain(c => c.Predicate == ClaimPredicate.ACCEPTS);
    }

    [Fact]
    public async Task SearchAsync_WithPagination_ReturnsPaged()
    {
        // Arrange
        var claims = Enumerable.Range(1, 25).Select(_ => CreateTestClaim()).ToList();
        await _repository.CreateManyAsync(claims);

        // Act
        var page1 = await _repository.SearchAsync(new ClaimSearchCriteria
        {
            PageSize = 10,
            Page = 0
        });
        var page2 = await _repository.SearchAsync(new ClaimSearchCriteria
        {
            PageSize = 10,
            Page = 1
        });

        // Assert
        page1.Claims.Should().HaveCount(10);
        page1.TotalCount.Should().BeGreaterOrEqualTo(25);
        page2.Claims.Should().HaveCount(10);
        page2.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetByDocumentAsync_ReturnsAllForDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var claims = Enumerable.Range(1, 5)
            .Select(_ => CreateTestClaim(documentId: docId))
            .ToList();
        await _repository.CreateManyAsync(claims);

        // Act
        var result = await _repository.GetByDocumentAsync(docId);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task FullTextSearchAsync_FindsMatchingClaims()
    {
        // Arrange
        var claim = CreateTestClaim(evidence: "The authentication endpoint requires valid credentials.");
        await _repository.CreateAsync(claim);

        // Act
        var results = await _repository.FullTextSearchAsync("authentication credentials");

        // Assert
        results.Should().Contain(c => c.Id == claim.Id);
    }

    [Fact]
    public async Task UpsertManyAsync_CreatesAndUpdates()
    {
        // Arrange
        var existing = CreateTestClaim();
        await _repository.CreateAsync(existing);

        var updated = existing with { Confidence = 0.9f };
        var newClaim = CreateTestClaim();

        // Act
        var result = await _repository.UpsertManyAsync(new[] { updated, newClaim });

        // Assert
        result.CreatedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(1);
        result.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteByDocumentAsync_DeletesAllForDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var claims = Enumerable.Range(1, 3)
            .Select(_ => CreateTestClaim(documentId: docId))
            .ToList();
        await _repository.CreateManyAsync(claims);

        // Act
        var deleted = await _repository.DeleteByDocumentAsync(docId);

        // Assert
        deleted.Should().Be(3);
        var remaining = await _repository.GetByDocumentAsync(docId);
        remaining.Should().BeEmpty();
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Claims persist to PostgreSQL correctly. |
| 2 | Search by predicate returns matching claims. |
| 3 | Search by entity ID finds claims as subject or object. |
| 4 | Pagination works correctly with total count. |
| 5 | Full-text search finds claims by evidence text. |
| 6 | Bulk upsert creates new and updates existing. |
| 7 | Delete by document removes all claims for doc. |
| 8 | Events published on create/update/delete. |
| 9 | Cache returns recently accessed claims. |
| 10 | Query performance <100ms for typical searches. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IClaimRepository` interface | [ ] |
| 2 | `ClaimRepository` implementation | [ ] |
| 3 | `ClaimSearchCriteria` record | [ ] |
| 4 | `ClaimQueryResult` record | [ ] |
| 5 | `BulkUpsertResult` record | [ ] |
| 6 | `ClaimCreatedEvent` | [ ] |
| 7 | `ClaimUpdatedEvent` | [ ] |
| 8 | `ClaimDeletedEvent` | [ ] |
| 9 | Database migration script | [ ] |
| 10 | Full-text search index | [ ] |
| 11 | Claim caching | [ ] |
| 12 | Integration tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.6h)

- `IClaimRepository` interface for claim persistence
- PostgreSQL-backed `ClaimRepository` implementation
- Full-text search on claim evidence
- `ClaimSearchCriteria` for flexible querying
- Paginated `ClaimQueryResult`
- Bulk upsert with create/update tracking
- Event publishing for claim changes
- In-memory caching for frequently accessed claims
- Database schema with optimized indexes
```

---
