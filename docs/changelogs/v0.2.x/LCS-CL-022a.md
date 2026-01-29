# LCS-CL-022a: Style Schema Migration

**Version**: v0.2.2a  
**Released**: 2026-01-29  
**Status**: ✅ Complete

---

## Overview

Creates the **style_terms** database table via FluentMigrator, establishing the schema foundation for the Terminology Database feature.

---

## Changes

### New Files

| File                                                     | Purpose                   |
| :------------------------------------------------------- | :------------------------ |
| `Infrastructure/Migrations/Migration_002_StyleSchema.cs` | Creates style_terms table |

---

## Technical Details

### Table Schema

```sql
CREATE TABLE style_terms (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    StyleSheetId UUID NOT NULL,
    Term VARCHAR(255) NOT NULL,
    Replacement VARCHAR(500),
    Category VARCHAR(100) NOT NULL DEFAULT 'General',
    Severity VARCHAR(20) NOT NULL DEFAULT 'Suggestion',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    Notes TEXT,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Indexes

| Index                              | Type          | Purpose                            |
| :--------------------------------- | :------------ | :--------------------------------- |
| `IX_style_terms_StyleSheetId_Term` | Unique B-tree | Prevents duplicate terms per sheet |
| `IX_style_terms_IsActive`          | Partial       | Filters active terms only          |
| `IX_style_terms_Category`          | B-tree        | Category lookup                    |
| `IX_style_terms_Term_trgm`         | GIN (pg_trgm) | Fuzzy text search                  |

### Trigger

`trg_style_terms_updated_at` - Auto-updates `UpdatedAt` on row modification.

---

## Test Coverage

| Test Class                  | Tests | Result  |
| :-------------------------- | ----: | :------ |
| `MigrationConventionsTests` |     4 | ✅ Pass |
| `MigrationDiscoveryTests`   |     8 | ✅ Pass |
| **Total**                   |    12 | ✅ Pass |

---

## Dependencies

| Dependency                       | Purpose                    |
| :------------------------------- | :------------------------- |
| `FluentMigrator` (6.2.0)         | Migration framework        |
| `pg_trgm` (PostgreSQL extension) | Trigram-based fuzzy search |

---

## Related Documents

| Document                                                  | Relationship    |
| :-------------------------------------------------------- | :-------------- |
| [LCS-DES-022a](../../specs/v0.2.x/v0.2.2/LCS-DES-022a.md) | Specification   |
| [LCS-SBD-022](../../specs/v0.2.x/v0.2.2/LCS-SBD-022.md)   | Scope Breakdown |
