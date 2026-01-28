# LCS-CL-005c: FluentMigrator Runner

**Version:** v0.0.5c  
**Category:** Infrastructure  
**Feature Name:** FluentMigrator Runner  
**Date:** 2026-01-28

---

## Summary

Implements FluentMigrator-based database schema versioning, enabling source-controlled, automated migrations with rollback support.

---

## New Features

### Migration Infrastructure

- **MigrationConventions** — Constants for PostgreSQL types (TIMESTAMPTZ, UUID)
- **LexichordMigration** — Base class with helper methods:
    - `IndexName()` / `ForeignKeyName()` — Naming conventions
    - `CreateUpdateTrigger()` / `DropUpdateTrigger()` — Auto-update triggers

### Initial Schema: Migration_001_InitSystem

- **Users table**: Id, Email, DisplayName, PasswordHash, IsActive, CreatedAt, UpdatedAt
- **SystemSettings table**: Key, Value, Description, UpdatedAt
- Indexes: IX_Users_Email, IX_Users_IsActive (partial)
- Update triggers for both tables
- Seed data: app:initialized, app:version, system:maintenance_mode

### Migration Runner

- **IMigrationRunner** interface with MigrateUp/Down, ListMigrations, ValidateMigrations
- **MigrationRunnerWrapper** — FluentMigrator wrapper with logging
- **VersionTableMetaData** — Custom SchemaVersions table configuration

### CLI Commands

- `--migrate` — Run all pending migrations
- `--migrate:up:N` — Run migrations up to version N
- `--migrate:down` — Rollback all migrations
- `--migrate:down:N` — Rollback to version N
- `--migrate:list` — List all migrations and status
- `--migrate:validate` — Validate without executing

---

## Files Added

| File                                                                  | Description                  |
| :-------------------------------------------------------------------- | :--------------------------- |
| `src/Lexichord.Infrastructure/Migrations/MigrationConventions.cs`     | Conventions and base class   |
| `src/Lexichord.Infrastructure/Migrations/Migration_001_InitSystem.cs` | Initial schema migration     |
| `src/Lexichord.Infrastructure/Migrations/MigrationServices.cs`        | DI, runner interface/wrapper |
| `src/Lexichord.Host/Commands/MigrationCommand.cs`                     | CLI command handler          |

## Files Modified

| File                                                           | Description                   |
| :------------------------------------------------------------- | :---------------------------- |
| `src/Lexichord.Infrastructure/Lexichord.Infrastructure.csproj` | Added FluentMigrator packages |

---

## NuGet Packages Added

| Package                        | Version |
| :----------------------------- | :------ |
| FluentMigrator                 | 6.2.0   |
| FluentMigrator.Runner          | 6.2.0   |
| FluentMigrator.Runner.Postgres | 6.2.0   |

---

## Unit Tests Added

| Test Class                  | Tests                                          |
| :-------------------------- | :--------------------------------------------- |
| `MigrationConventionsTests` | 7 tests for naming conventions and constants   |
| `MigrationDiscoveryTests`   | 5 tests for migration discovery and attributes |
| `VersionTableMetaDataTests` | 8 tests for version table configuration        |

## Integration Tests Added

| Test Class                  | Tests                                             |
| :-------------------------- | :------------------------------------------------ |
| `MigrationIntegrationTests` | 8 tests (skipped by default, requires PostgreSQL) |

---

## Usage

```bash
# List migrations
dotnet run --project src/Lexichord.Host -- --migrate:list

# Run all pending migrations
dotnet run --project src/Lexichord.Host -- --migrate

# Rollback last migration
dotnet run --project src/Lexichord.Host -- --migrate:down

# Validate without executing
dotnet run --project src/Lexichord.Host -- --migrate:validate
```

---

## Verification Commands

```bash
# Start PostgreSQL
./scripts/db-start.sh

# Run migrations
dotnet run --project src/Lexichord.Host -- --migrate

# Verify tables
docker exec -it lexichord-postgres psql -U lexichord -c "\dt"

# Check Users table
docker exec -it lexichord-postgres psql -U lexichord -c '\d "Users"'

# Check SystemSettings
docker exec -it lexichord-postgres psql -U lexichord -c 'SELECT * FROM "SystemSettings"'

# Check version tracking
docker exec -it lexichord-postgres psql -U lexichord -c 'SELECT * FROM "SchemaVersions"'
```

---

## Dependencies

- **From v0.0.5b:** DatabaseOptions, IDbConnectionFactory

## Enables

- **v0.0.5d:** GenericRepository<T> with Dapper integration
