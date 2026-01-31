# v0.2.2c: Terminology Seeding Service

**Status:** ✅ Complete  
**Date:** 2026-01-29

## Summary

Implemented out-of-the-box style guidance by seeding the Terminology Database with ~50 Microsoft Manual of Style terms on first application launch.

## New Files

| File                                   | Purpose                                                                                      |
| -------------------------------------- | -------------------------------------------------------------------------------------------- |
| `ITerminologySeeder.cs`                | Interface defining seeder contract with `SeedIfEmptyAsync`, `ReseedAsync`, `GetDefaultTerms` |
| `TerminologySeeder.cs`                 | Implementation with embedded seed terms (~50 terms across 6 categories)                      |
| `TerminologySeederTests.cs`            | Unit tests for seeding logic and data coverage                                               |
| `TerminologySeederIntegrationTests.cs` | Integration tests with PostgreSQL                                                            |

## Modified Files

| File             | Change                                                                      |
| ---------------- | --------------------------------------------------------------------------- |
| `StyleModule.cs` | Registered `ITerminologySeeder` and added seeding call in `InitializeAsync` |

## Technical Notes

### Seed Data Categories

| Category       | Count | Examples                                             |
| -------------- | ----- | ---------------------------------------------------- |
| Terminology    | 15    | leverage → use, utilize → use, synergy → cooperation |
| Capitalization | 8     | internet → Internet, e-mail → email                  |
| Punctuation    | 6     | e.g. → for example, i.e. → that is                   |
| Voice          | 8     | in order to → to, due to the fact that → because     |
| Clarity        | 8     | very, really, actually (flag as filler words)        |
| Grammar        | 5     | alot → a lot, could of → could have                  |

### Design Decisions

1. **Embedded data**: Seed terms are compiled into the assembly, preventing tampering
2. **Idempotent seeding**: `SeedIfEmptyAsync` only seeds if database is empty
3. **ReseedAsync warning**: `clearExisting: true` logs a warning about data loss
4. **Static initialization**: Terms created once on class load for performance

## Dependencies

- `ITerminologyRepository` (v0.2.2b) - Uses `CountAsync` and `InsertManyAsync`

## Unit Tests

- `TerminologySeederTests`: 14 tests covering constructor, seeding, idempotency, reseed, and data coverage
