# LCS-CL-054a: Query Analyzer

**Version:** v0.5.4a
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the Query Analyzer for extracting keywords, recognizing entities, detecting intent, and calculating specificity scores for user search queries in the Relevance Tuner feature.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File               | Change                                                             |
| ------------------ | ------------------------------------------------------------------ |
| `QueryAnalysis.cs` | `QueryAnalysis` record with keywords, entities, intent, specificity |
| `QueryAnalysis.cs` | `QueryIntent` enum (Factual, Procedural, Conceptual, Navigational) |
| `QueryAnalysis.cs` | `QueryEntity` record with value, type, and position                |
| `QueryAnalysis.cs` | `EntityType` enum (CodeIdentifier, FilePath, Version, ErrorCode)   |
| `IQueryAnalyzer.cs`| Interface for query analysis with single `Analyze` method          |

### Services (`Lexichord.Modules.RAG`)

| File                | Change                                                           |
| ------------------- | ---------------------------------------------------------------- |
| `QueryAnalyzer.cs`  | Core service with stop-word filtering and keyword extraction     |
| `QueryAnalyzer.cs`  | Regex-based entity recognition (PascalCase, paths, versions)     |
| `QueryAnalyzer.cs`  | Intent detection heuristics based on query pattern matching      |
| `QueryAnalyzer.cs`  | Specificity scoring (0.0-1.0) from keyword/entity composition    |

### Stop Word Handling

The analyzer filters 36 common English stop words that add no search value:
`a`, `an`, `the`, `is`, `are`, `was`, `were`, `be`, `been`, `being`, `have`, `has`, `had`, `do`, `does`, `did`, `will`, `would`, `could`, `should`, `may`, `might`, `must`, `shall`, `can`, `to`, `of`, `in`, `for`, `on`, `with`, `at`, `by`, `from`, `as`, `or`

### Entity Recognition Patterns

| Entity Type      | Pattern                                       | Examples                     |
| ---------------- | --------------------------------------------- | ---------------------------- |
| CodeIdentifier   | PascalCase or camelCase with 2+ uppercase     | `MyClass`, `getElementById`  |
| FilePath         | Starts with `/`, `./`, `../`, or `C:\`        | `/usr/bin`, `./src/app.ts`   |
| Version          | Semantic versioning pattern                   | `v1.2.3`, `2.0.0-beta`       |
| ErrorCode        | Uppercase prefix + numbers                    | `ERR_001`, `HTTP_404`        |

### Intent Detection Heuristics

| Intent        | Trigger Patterns                                         |
| ------------- | -------------------------------------------------------- |
| Procedural    | "how to", "steps to", "tutorial", "guide", "create"      |
| Factual       | "what is", "define", "meaning", "example", entity present|
| Navigational  | "where is", "find", "location", "path", file extension   |
| Conceptual    | "why", "concept", "theory", "principle", "explain"       |

### Specificity Calculation

Score (0.0-1.0) is calculated from:
- Base: 0.3 (minimum for any query)
- Keywords: +0.1 per keyword (max +0.4)
- Entities: +0.15 per entity (max +0.3)
- Quoted phrases: +0.1 if present

## Tests

| File                     | Tests                                               |
| ------------------------ | --------------------------------------------------- |
| `QueryAnalyzerTests.cs`  | 4 tests - Null/empty input handling                 |
| `QueryAnalyzerTests.cs`  | 6 tests - Keyword extraction and stop-word filtering|
| `QueryAnalyzerTests.cs`  | 8 tests - Entity recognition for all types          |
| `QueryAnalyzerTests.cs`  | 8 tests - Intent detection heuristics               |
| `QueryAnalyzerTests.cs`  | 4 tests - Specificity scoring                       |

**Total: 30 unit tests**

## License Gating

- Feature Code: None (Core feature)
- Minimum Tier: Core (available to all users)

## Dependencies

- Microsoft.Extensions.Logging (existing)
- System.Text.RegularExpressions (framework)
