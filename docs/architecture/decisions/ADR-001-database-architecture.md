# ADR-001: Hybrid Database Architecture

**Status:** Accepted  
**Date:** 2026-01-27  
**Deciders:** Architecture Review Board

## Context

The Lexichord specification documents contained conflicting database references:

- `DESIGN_PROPOSAL.md` and `ROADMAP.md` specify PostgreSQL as the primary data layer
- Several specifications (`LCS-DES-008b`, `LCS-DES-034a`, `LCS-DES-048d`) referenced SQLite

This created ambiguity about which database technology to use for which purposes.

## Decision

We adopt a **hybrid database architecture**:

### PostgreSQL (Primary Data Layer)

Used for all **domain entities** requiring:

- Transactional integrity
- Cross-device synchronization
- Complex queries and relationships
- Long-term persistence

**Examples:**

- Terms and Lexicons (`v0.2.5` Librarian)
- Voice Profiles (`v0.3.4` Voice Engine)
- User accounts and preferences
- Project configurations

### SQLite (Local Cache Layer)

Used for **derived/ephemeral data** with characteristics:

- Non-critical (can be regenerated)
- High-frequency local access
- Device-specific (no sync needed)
- Temporary by design

**Examples:**

- Health metrics cache (`v0.0.8b` StatusBar)
- Embedding vectors cache (`v0.4.8d` RAG)
- Local session state

## Consequences

### Positive

- Clear separation of concerns
- Optimal performance for each use case
- Reduced PostgreSQL load for high-frequency writes
- Offline capability for cached data

### Negative

- Two database technologies to maintain
- Developers must understand the distinction
- Cache invalidation complexity

## Compliance

All specifications must clearly indicate which database layer they use and why. Use the following format in specification headers:

```markdown
> [!IMPORTANT]
> **Database Layer:** [PostgreSQL|SQLite] — [brief justification]
```
