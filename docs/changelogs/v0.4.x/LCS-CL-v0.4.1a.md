# Changelog: v0.4.1a - pgvector Docker Configuration

**Release Date:** 2026-01-31  
**Codename:** The Vector Foundation (Part A)  
**License Tier:** Core (no gating - infrastructure setup)

## Summary

Configured PostgreSQL with the pgvector extension for vector storage, enabling semantic search capabilities in the Lexichord RAG system.

## New Files

| File                                        | Purpose                                            |
| ------------------------------------------- | -------------------------------------------------- |
| `scripts/db-init/02-configure-postgres.sql` | PostgreSQL performance tuning for vector workloads |
| `scripts/test-pgvector.sh`                  | Shell verification script for pgvector extension   |
| `tests/.../PgVectorIntegrationTests.cs`     | Integration tests for pgvector operations          |
| `docs/development-setup.md`                 | Local development environment documentation        |

## Modified Files

| File                                           | Changes                                                          |
| ---------------------------------------------- | ---------------------------------------------------------------- |
| `docker-compose.yml`                           | Changed image to `pgvector/pgvector:pg16`, enhanced health check |
| `scripts/db-init/01-create-extensions.sql`     | Added vector extension creation and verification                 |
| `tests/.../Lexichord.Tests.Integration.csproj` | Added Pgvector NuGet package                                     |

## Key Features

- **pgvector Image**: Uses official `pgvector/pgvector:pg16` Docker image
- **Extension Verification**: Init script verifies extension loads correctly
- **Health Check**: Container health check validates both PostgreSQL and pgvector availability
- **Performance Tuning**: Configured `work_mem` and `maintenance_work_mem` for vector operations
- **Shell Verification**: `test-pgvector.sh` validates vector CRUD operations
- **Integration Tests**: Five C# tests covering extension availability and vector operations

## Verification

```bash
# Start database
./scripts/db-start.sh

# Verify pgvector
./scripts/test-pgvector.sh

# Run integration tests (requires running container)
dotnet test tests/Lexichord.Tests.Integration --filter "FullyQualifiedName~PgVector"
```

## Dependencies

- Docker / Docker Compose
- pgvector/pgvector:pg16 image
- Npgsql 9.0.3
- Pgvector 0.3.0 (test project only)

## Next Steps

- v0.4.1b: Schema migrations for vector tables
- v0.4.1c: Repository abstractions
- v0.4.1d: Dapper implementation
