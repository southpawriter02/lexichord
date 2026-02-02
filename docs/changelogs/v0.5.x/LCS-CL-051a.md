# LCS-CL-051a: BM25 Index Schema

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-051a                     |
| **Version**        | v0.5.1a                         |
| **Feature Name**   | BM25 Index Schema               |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |
| **Related Spec**   | [LCS-DES-v0.5.1a](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1a.md) |

---

## Summary

Extended the `Chunks` table with PostgreSQL full-text search capabilities to enable BM25-style keyword search in the hybrid search engine.

### Schema Changes

| Change Type | Object                           | Description                                    |
| :---------- | :------------------------------- | :--------------------------------------------- |
| ADD COLUMN  | `Chunks.ContentTsvector`         | Generated TSVECTOR column for full-text search |
| ADD INDEX   | `IX_Chunks_ContentTsvector_gin`  | GIN index for fast @@ operator queries         |

---

## Technical Details

### Generated Column

The `ContentTsvector` column uses PostgreSQL's `GENERATED ALWAYS AS ... STORED` syntax to automatically maintain a full-text search index of the `Content` column:

```sql
ALTER TABLE "Chunks"
ADD COLUMN "ContentTsvector" TSVECTOR
GENERATED ALWAYS AS (to_tsvector('english', "Content")) STORED;
```

**Benefits:**
- **Zero application code** — PostgreSQL handles all insert/update synchronization
- **STORED** — Column is persisted for query performance (not computed on-the-fly)
- **English stemming** — Uses 'english' text search configuration for stemming and stop words

### GIN Index

The GIN (Generalized Inverted Index) enables fast full-text search queries:

```sql
CREATE INDEX "IX_Chunks_ContentTsvector_gin" ON "Chunks"
USING GIN ("ContentTsvector");
```

**Query Example:**

```sql
SELECT c.*
FROM "Chunks" c
WHERE c."ContentTsvector" @@ plainto_tsquery('english', 'search terms')
ORDER BY ts_rank_cd(c."ContentTsvector", plainto_tsquery('english', 'search terms')) DESC;
```

---

## Files Changed

### New Files

| File | Description |
| :--- | :---------- |
| [Migration_004_FullTextSearch.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Infrastructure/Migrations/Migration_004_FullTextSearch.cs) | FluentMigrator migration adding tsvector column and GIN index |

### Modified Files

| File | Description |
| :--- | :---------- |
| [PostgresRagFixture.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Integration/RAG/Fixtures/PostgresRagFixture.cs) | Updated test schema with tsvector column |
| [MigrationIntegrationTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Integration/Infrastructure/MigrationIntegrationTests.cs) | Added 4 integration tests for full-text search migration |

---

## Tests Added

| Test Method | Verifies |
| :---------- | :------- |
| `MigrateUp_CreatesContentTsvectorColumn` | Column exists with correct type |
| `MigrateUp_CreatesContentTsvectorGinIndex` | GIN index exists |
| `MigrateUp_ContentTsvectorIsGeneratedColumn` | Auto-generation and searchability |
| `MigrateDown_DropsContentTsvectorColumn` | Rollback removes column and index |

---

## Dependencies

| Dependency | Description |
| :--------- | :---------- |
| v0.4.1b    | Chunks table must exist (Migration_003_VectorSchema) |

## Dependents

| Version | Feature |
| :------ | :------ |
| v0.5.1b | BM25 Search Service (uses content_tsvector for queries) |
