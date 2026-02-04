# LCS-DS-v0.18.1e-SEC: Design Specification — Grant Persistence & Storage

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.1e-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.1-SEC                          |
| **Release Version**   | v0.18.1e                                     |
| **Component Name**    | Grant Persistence & Storage                  |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Grant Persistence & Storage** component (v0.18.1e). This component is responsible for the durable storage of permission grants in the PostgreSQL database. It provides a reliable and efficient data access layer for creating, retrieving, updating, and deleting permission grants, and includes a comprehensive audit trail for all grant lifecycle events.

---

## 3. Detailed Design

### 3.1. Objective

Implement a persistent storage layer for permission grants using PostgreSQL, with support for efficient querying, grant lifecycle management (Active, Expired, Revoked), and a complete, immutable audit history.

### 3.2. Scope

-   Define the `IPermissionGrantStore` interface for all grant CRUD (Create, Read, Update, Delete) operations.
-   Implement a `PermissionGrantStore` service using a data access technology like Dapper or Entity Framework Core to interact with the PostgreSQL database.
-   Design an optimized PostgreSQL schema with tables for grants, scopes, and audit entries.
-   Implement indexing strategies to ensure fast queries, especially for checking active grants for a user.
-   Manage the lifecycle status of grants (`Active`, `Expired`, `Revoked`, `Superseded`).
-   Implement soft deletes for grants to preserve historical data for auditing.
-   Provide methods for querying the audit trail associated with a grant.

### 3.3. Detailed Architecture

The `PermissionGrantStore` will be a service that encapsulates all database logic. It will be injected into other services (like the `PermissionRequestPipeline` and `PermissionManager`) that need to interact with grant data. It will not contain any business logic beyond what's necessary for data persistence.

```mermaid
graph TD
    A[Services (e.g., PermissionManager)] --> B(IPermissionGrantStore);
    
    subgraph PermissionGrantStore
        B --> C{Dapper / EF Core};
        C --> D[PostgreSQL Database];
    end
    
    subgraph Database Schema
        D -- Manages --> T1(permission_grants);
        D -- Manages --> T2(permission_scopes);
        D -- Manages --> T3(grant_audit_entries);
    end

    T1 -- FK --> T2;
    T3 -- FK --> T1;
```

#### 3.3.1. Database Schema

The schema is normalized to separate grants, scopes, and audit history. `PermissionScope` objects are serialized to JSON and stored in the `permission_scopes` table. This provides flexibility in scope structure without requiring schema changes.

**`permission_scopes` table:**
-   `scope_id` (UUID, PK): Unique identifier for a scope definition.
-   `constraints` (JSONB): A JSON array of the `ScopeConstraint` objects. This is flexible and avoids complex table joins.
-   `composition_mode` (INTEGER): `0` for AND, `1` for OR.
-   `created_at` (TIMESTAMPTZ): When the scope was first created.

**`permission_grants` table:**
-   `grant_id` (UUID, PK): Unique ID for the grant.
-   `user_id` (VARCHAR): The user to whom the permission is granted.
-   `permission_id` (VARCHAR): The ID of the permission (e.g., "file.read").
-   `scope_id` (UUID, FK to `permission_scopes`): The scope of this grant.
-   `status` (INTEGER): The lifecycle status (`Active`, `Expired`, `Revoked`).
-   `granted_at`, `granted_by`, `expires_at`, etc.: Auditing and lifecycle fields.
-   `is_deleted` (BOOLEAN): Flag for soft deletes.

**`grant_audit_entries` table:**
-   `entry_id` (UUID, PK): Unique ID for the audit entry.
-   `grant_id` (UUID, FK to `permission_grants`): The grant this entry pertains to.
-   `status_change` (INTEGER): The new status that was set.
-   `timestamp`, `actor_id`, `reason`, `details`: The who, what, when, and why of the change. This table is append-only to ensure an immutable audit trail.

### 3.4. Interfaces & Data Models

```csharp
/// <summary>
/// Provides an abstraction for storing and retrieving permission grants from a persistent data store.
/// </summary>
public interface IPermissionGrantStore
{
    /// <summary>
    /// Creates a new permission grant in the data store.
    /// </summary>
    /// <param name="grant">The grant to create. The GrantId will be ignored and a new one generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created grant, including its new unique identifier.</returns>
    Task<PermissionGrant> CreateGrantAsync(PermissionGrant grant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific grant by its unique identifier.
    /// </summary>
    Task<PermissionGrant?> GetGrantAsync(string grantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active (not expired or revoked) grants for a specific user and permission.
    /// </summary>
    /// <param name="userId">The user's identifier.</param>
    /// <param name="permissionId">The permission's identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active permission grants.</returns>
    Task<IReadOnlyCollection<PermissionGrant>> GetActiveGrantsAsync(string userId, string permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the lifecycle status of a grant (e.g., to Revoked or Expired).
    /// </summary>
    /// <param name="grantId">The identifier of the grant to update.</param>
    /// <param name="newStatus">The new lifecycle status.</param>
    /// <param name="actorId">The identifier of the user or system process making the change.</param>
    /// <param name="reason">An optional reason for the status change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated permission grant.</returns>
    Task<PermissionGrant> UpdateGrantStatusAsync(string grantId, GrantLifecycleStatus newStatus, string actorId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit history for a specific grant.
    /// </summary>
    Task<IReadOnlyCollection<GrantAuditEntry>> GetAuditTrailAsync(string grantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a persisted permission grant.
/// </summary>
public record PermissionGrant(
    string GrantId,
    string UserId,
    string PermissionId,
    PermissionScope Scope,
    GrantLifecycleStatus Status,
    DateTimeOffset GrantedAt,
    string GrantedBy,
    DateTimeOffset? ExpiresAt = null,
    DateTimeOffset? RevokedAt = null,
    string? RevocationReason = null,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// The lifecycle status of a permission grant.
/// </summary>
public enum GrantLifecycleStatus
{
    Active = 0,
    Expired = 1,
    Revoked = 2,
    Superseded = 3, // Replaced by a newer grant
    Pending = 4     // Awaiting finalization
}

/// <summary>
/// Represents a single entry in the immutable audit trail for a permission grant.
/// </summary>
public record GrantAuditEntry(
    string EntryId,
    string GrantId,
    GrantLifecycleStatus StatusChange,
    DateTimeOffset Timestamp,
    string ActorId, // User or System process that made the change
    string ActionType, // e.g., "Grant.Created", "Grant.Revoked"
    string? Reason = null,
    Dictionary<string, object>? Details = null);
```

### 3.5. Error Handling

-   **Database Unavailability**: The service will throw a custom `PermissionStoreUnavailableException` if it cannot connect to the database after a reasonable number of retries (e.g., using Polly for resilience). This allows upstream services to handle the failure gracefully.
-   **Constraint Violations**: If a database constraint is violated (e.g., trying to create a grant with a non-existent `scope_id`), the underlying `PostgresException` will be caught, logged, and re-thrown as a more specific `InvalidOperationException`.
-   **Concurrency**: The `UpdateGrantStatusAsync` method will use optimistic concurrency. It will include the `updated_at` timestamp in its `WHERE` clause. If the number of rows affected is 0, it means another process modified the record, and a `ConcurrencyException` will be thrown.

### 3.6. Security Considerations

-   **SQL Injection**: By using a modern data access library like Dapper or EF Core with parameterized queries, the risk of SQL injection is eliminated. No raw SQL strings will be constructed with user-provided data.
-   **Data Access**: The application's database user will be granted the minimum necessary privileges on the permission tables (e.g., SELECT, INSERT, UPDATE, but not DROP or TRUNCATE).
-   **Auditing**: The append-only nature of the `grant_audit_entries` table is critical for security. The application user should not have DELETE permissions on this table.
-   **Sensitive Data**: While the grants themselves are not typically secret, any sensitive data stored in the `metadata` JSONB column must be encrypted at the application layer before being passed to this service.

### 3.7. Performance Considerations

-   **Indexing**: The query performance of `GetActiveGrantsAsync(userId, permissionId)` is critical. A composite index on `(user_id, permission_id, status)` will be created to ensure these lookups are extremely fast. An index on `expires_at` will also be added for the background expiry job.
-   **Connection Pooling**: A properly configured connection pool is essential to handle concurrent requests without overwhelming the database.
-   **Query Optimization**: Queries will be designed to be "SARGable" (Searchable Argument-able), allowing the database to use indexes efficiently. For example, `WHERE status = 0` is better than `WHERE IsActive(status) = true`.

### 3.8. Testing Strategy

-   **Unit Tests**: The `PermissionGrantStore` will be tested against an in-memory database (like `SQLite` in-memory mode) or a mocked data access layer to verify the correctness of the CRUD operations without requiring a live database.
-   **Integration Tests**: A suite of tests will run against a real, temporary PostgreSQL database (e.g., spun up via Testcontainers). These tests will verify:
    -   Correct data persistence and retrieval.
    -   That database constraints and indexes are working as expected.
    -   Transactional behavior (e.g., creating a grant and its audit entry atomically).
    -   Soft delete functionality.
    -   Concurrency handling.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                          |
| :----------------------- | :------------------------------------------------------------------- |
| `IPermissionGrantStore`  | The core interface for the grant persistence layer.                  |
| `PermissionGrantStore`   | The default PostgreSQL implementation of the interface.              |
| `PermissionGrant`        | The C# record representing a grant.                                  |
| `GrantAuditEntry`        | The C# record for an audit trail entry.                              |
| Database Schema Scripts  | SQL scripts to create and migrate the database schema.               |
| Integration Tests        | A full suite of tests running against a real PostgreSQL instance.    |

---

## 5. Acceptance Criteria

-   [ ] All CRUD operations on grants complete within 50ms (P95, excluding network latency).
-   [ ] Database indexes are in place and proven to optimize queries for active grants by user and permission.
-   [ ] The soft delete mechanism is implemented, and deleted grants are excluded from active queries but remain for audit purposes.
-   [ ] Every status change to a grant results in a new entry in the `grant_audit_entries` table.
-   [ ] The database schema correctly stores `PermissionScope` objects as JSONB and supports all lifecycle statuses.
-   [ ] The data access layer correctly handles and logs database connection errors and concurrency conflicts.
-   [ ] Integration tests that run against a real PostgreSQL database achieve 90%+ coverage of the `PermissionGrantStore`'s public methods.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`v0.18.1d` (Permission Scope Manager)**: To validate scopes before they are persisted.
-   **`Npgsql`**: PostgreSQL driver for .NET.
-   **`Dapper` or `EF Core`**: For data access.

### 6.2. Integration Points
-   **`PermissionRequestPipeline` (from v0.18.1b)**: Will call `CreateGrantAsync` after a user grants consent and `GetActiveGrantsAsync` to check for pre-existing grants.
-   **`PermissionRevocationService` (from v0.18.1f)**: Will call `UpdateGrantStatusAsync` to mark grants as `Revoked` or `Expired`.
-   **`PermissionManager`**: The main service orchestrating permission checks will be the primary consumer of `GetActiveGrantsAsync`.
