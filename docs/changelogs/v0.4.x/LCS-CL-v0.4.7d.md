# Changelog: v0.4.7d — Indexing Errors

**Date:** 2026-02-02  
**Author:** Antigravity  
**Status:** Complete

---

## Summary

Implements error categorization and reporting for document indexing failures. Failed documents are now categorized by error type, enabling targeted retry logic and user-friendly messaging.

---

## Added

### Models

- **`IndexingErrorCategory`** — Enum with 10 error categories: `Unknown`, `RateLimit`, `NetworkError`, `ServiceUnavailable`, `InvalidContent`, `FileTooLarge`, `FileNotFound`, `PermissionDenied`, `TokenLimitExceeded`, `ApiKeyInvalid`

- **`IndexingErrorInfo`** — Record encapsulating error details with computed properties:
    - `IsRetryable` — Transient errors (RateLimit, NetworkError, ServiceUnavailable)
    - `SuggestedAction` — User-facing guidance per category

### Services

- **`IndexingErrorCategorizer`** — Static class for:
    - `Categorize(Exception)` — Maps exceptions via HTTP status codes, types, and message patterns
    - `GetUserFriendlyMessage(Exception, Category)` — Readable error descriptions
    - `CreateErrorInfo(...)` — Factory for `IndexingErrorInfo`

### Handlers

- **`DocumentIndexingFailedHandler`** — MediatR handler that:
    - Categorizes exceptions
    - Logs at Warning level with category and retryability

---

## Changed

- **`DocumentIndexingFailedEvent`** — Added optional `Exception` property for categorization
- **`IDocumentRepository`** — Added `GetFailedDocumentsAsync()` convenience method
- **`DocumentRepository`** — Implemented `GetFailedDocumentsAsync()`

---

## File Manifest

| File                                        | Change   |
| ------------------------------------------- | -------- |
| `Models/IndexingErrorCategory.cs`           | NEW      |
| `Models/IndexingErrorInfo.cs`               | NEW      |
| `Services/IndexingErrorCategorizer.cs`      | NEW      |
| `Indexing/DocumentIndexingFailedHandler.cs` | NEW      |
| `Indexing/IndexingEvents.cs`                | MODIFIED |
| `IDocumentRepository.cs`                    | MODIFIED |
| `Data/DocumentRepository.cs`                | MODIFIED |

---

## Tests

- `IndexingErrorCategorizerTests.cs` — 17 tests (categorization, messages)
- `IndexingErrorInfoTests.cs` — 10 tests (IsRetryable, SuggestedAction)
- `DocumentIndexingFailedHandlerTests.cs` — 9 tests (handler behavior)

**Total:** 36 new tests, all passing
