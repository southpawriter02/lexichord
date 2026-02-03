# LCS-CL-054b: Query Expansion

**Version:** v0.5.4b
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the Query Expander for enhancing search queries with synonyms from the terminology database and morphological variants via Porter stemming, improving recall without sacrificing precision.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                 | Change                                                         |
| -------------------- | -------------------------------------------------------------- |
| `QueryExpansion.cs`  | `ExpandedQuery` record with original query, synonyms, variants |
| `QueryExpansion.cs`  | `Synonym` record with term, weight, and source                 |
| `QueryExpansion.cs`  | `SynonymSource` enum (TermDatabase, BuiltIn, UserDefined)      |
| `QueryExpansion.cs`  | `ExpansionOptions` record with weight thresholds and limits    |
| `IQueryExpander.cs`  | Interface for async query expansion with options               |

### Services (`Lexichord.Modules.RAG`)

| File                | Change                                                          |
| ------------------- | --------------------------------------------------------------- |
| `QueryExpander.cs`  | Core service with terminology database integration              |
| `QueryExpander.cs`  | Built-in technical synonyms dictionary (50+ abbreviations)      |
| `QueryExpander.cs`  | Porter stemming for morphological variants                      |
| `QueryExpander.cs`  | ConcurrentDictionary caching for expansion results              |
| `QueryExpander.cs`  | License gating (WriterPro+ tier)                                |

### Built-in Technical Synonyms

The expander includes 50+ built-in technical abbreviation mappings:

| Abbreviation | Expansions                     |
| ------------ | ------------------------------ |
| `api`        | application programming interface |
| `ui`         | user interface                 |
| `db`         | database                       |
| `auth`       | authentication, authorization  |
| `config`     | configuration                  |
| `repo`       | repository                     |
| `fn`         | function                       |
| `async`      | asynchronous                   |
| `sync`       | synchronous                    |
| `msg`        | message                        |
| ...          | (50+ total mappings)           |

### Porter Stemming

Generates morphological variants for improved recall:
- `running` → `run`, `runs`, `runner`
- `configuration` → `config`, `configure`, `configured`
- `authentication` → `auth`, `authenticate`, `authenticated`

### Expansion Options

| Property           | Default | Description                          |
| ------------------ | ------- | ------------------------------------ |
| `MinWeight`        | 0.5f    | Minimum synonym weight threshold     |
| `MaxSynonyms`      | 5       | Maximum synonyms per term            |
| `IncludeVariants`  | true    | Include Porter stemming variants     |
| `Sources`          | All     | Which synonym sources to query       |

## Tests

| File                    | Tests                                              |
| ----------------------- | -------------------------------------------------- |
| `QueryExpanderTests.cs` | 3 tests - Null/empty input handling                |
| `QueryExpanderTests.cs` | 4 tests - License gating behavior                  |
| `QueryExpanderTests.cs` | 6 tests - Synonym expansion from built-in dictionary|
| `QueryExpanderTests.cs` | 4 tests - Expansion options (weight, limits)       |
| `QueryExpanderTests.cs` | 3 tests - Result caching                           |

**Total: 20 unit tests**

## License Gating

- Feature Code: `FeatureFlags.RAG.RelevanceTuner`
- Minimum Tier: Writer Pro
- Fallback: Returns original query without expansion for Core tier

## Dependencies

- Microsoft.Extensions.Logging (existing)
- Dapper (existing, for terminology database queries)
- ILicenseContext (existing, for license verification)
