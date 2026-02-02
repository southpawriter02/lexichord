# Changelog: v0.4.6f — Axiom Repository

**Version:** 0.4.6f  
**Date:** 2026-02-01  
**Module:** Lexichord.Modules.Knowledge  
**Feature:** Axiom Repository (CKVS Phase 1c)

## Summary

Implements persistent storage for domain axioms using PostgreSQL with Dapper, including in-memory caching and license-gated write operations.

## Changes

### Lexichord.Abstractions

| File                  | Change | Description                                                        |
| --------------------- | ------ | ------------------------------------------------------------------ |
| `IAxiomRepository.cs` | NEW    | Repository interface for axiom CRUD operations                     |
| `AxiomFilter.cs`      | NEW    | Filter record for querying axioms by type, category, tags, enabled |
| `AxiomStatistics.cs`  | NEW    | Aggregate statistics record (counts by category/type)              |

### Lexichord.Modules.Knowledge

| File                                 | Change   | Description                                            |
| ------------------------------------ | -------- | ------------------------------------------------------ |
| `Axioms/IAxiomCacheService.cs`       | NEW      | Cache abstraction for axiom storage                    |
| `Axioms/AxiomCacheService.cs`        | NEW      | IMemoryCache implementation with 5min/30min expiration |
| `Axioms/AxiomEntity.cs`              | NEW      | Internal database entity for PostgreSQL mapping        |
| `Axioms/AxiomRepository.cs`          | NEW      | Dapper implementation with caching and license checks  |
| `KnowledgeModule.cs`                 | MODIFIED | Added service registrations for axiom services         |
| `Lexichord.Modules.Knowledge.csproj` | MODIFIED | Added Microsoft.Extensions.Caching.Memory package      |

### Lexichord.Infrastructure

| File                                      | Change | Description                                        |
| ----------------------------------------- | ------ | -------------------------------------------------- |
| `Migrations/Migration_006_AxiomTables.cs` | NEW    | PostgreSQL migration for Axioms table with indexes |

## License Requirements

- **Read Operations**: WriterPro+ tier
- **Write Operations**: Teams+ tier (enforced via `FeatureNotLicensedException`)

## Database Schema

Table: `Axioms`

| Column        | Type          | Description                      |
| ------------- | ------------- | -------------------------------- |
| Id            | VARCHAR(100)  | Primary key                      |
| Name          | VARCHAR(200)  | Human-readable name              |
| Description   | VARCHAR(1000) | Optional description             |
| TargetType    | VARCHAR(100)  | Entity type (indexed)            |
| TargetKind    | VARCHAR(50)   | Entity/Relationship/Claim        |
| RulesJson     | JSONB         | Validation rules                 |
| Severity      | VARCHAR(50)   | Error/Warning/Info               |
| Category      | VARCHAR(100)  | Logical grouping (indexed)       |
| TagsJson      | JSONB         | Tags for filtering (GIN indexed) |
| IsEnabled     | BOOLEAN       | Active status                    |
| SourceFile    | VARCHAR(500)  | YAML source file (indexed)       |
| SchemaVersion | VARCHAR(20)   | Forward compatibility            |
| CreatedAt     | TIMESTAMPTZ   | Creation timestamp               |
| UpdatedAt     | TIMESTAMPTZ   | Last modification (auto-trigger) |
| Version       | INTEGER       | Optimistic concurrency           |

## Tests Added

- `AxiomRepositoryTests.cs` — 5 constructor validation tests
- `AxiomCacheServiceTests.cs` — 11 cache operation tests
- `AxiomContractTests.cs` — 7 record validation tests

## Dependencies

| Interface                           | Version | Purpose             |
| ----------------------------------- | ------- | ------------------- |
| IDbConnectionFactory                | v0.0.5b | PostgreSQL access   |
| ILicenseContext                     | v0.0.4c | License tier checks |
| Axiom, AxiomRule                    | v0.4.6e | Domain models       |
| Microsoft.Extensions.Caching.Memory | 9.0.0   | In-memory caching   |
