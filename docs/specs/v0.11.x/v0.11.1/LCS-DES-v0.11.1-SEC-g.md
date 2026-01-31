# LCS-DES-111-SEC-g: Design Specification — Entity-Level ACLs

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `SEC-111-g` | Access Control sub-part g |
| **Feature Name** | `Entity-Level ACLs` | Per-entity access control lists |
| **Target Version** | `v0.11.1g` | Seventh sub-part of v0.11.1-SEC |
| **Module Scope** | `Lexichord.Modules.Security` | Security module |
| **Swimlane** | `Security` | Security vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.Security.EntityAcls` | Entity ACL feature flag |
| **Author** | Security Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-111-SEC](./LCS-SBD-111-SEC.md) | Access Control & Authorization scope |
| **Scope Breakdown** | [LCS-SBD-111-SEC S2.1](./LCS-SBD-111-SEC.md#21-sub-parts) | g = Entity-Level ACLs |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.11.1-SEC requires fine-grained, per-entity access control lists (ACLs) to:

1. Override or refine role-based permissions at the entity level
2. Support multiple principal types (users, roles, teams, service accounts)
3. Enable expiring permissions (time-limited access)
4. Support inheritance from parent entities through relationships
5. Allow explicit deny entries that block inherited permissions

### 2.2 The Proposed Solution

Implement entity-level ACLs with:

1. **EntityAcl record:** Per-entity ACL definition with default access level
2. **AclEntry record:** Individual access grant with principal and permissions
3. **Principal types:** User, Role, Team, ServiceAccount
4. **Expiration support:** Time-limited ACL entries
5. **Inheritance:** Child entities inherit from parents unless explicitly overridden

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| Permission model | v0.11.1e | Permission enum, principalType definitions |
| Graph relationships | v0.4.5e | Parent-child entity relationships |
| IProfileService | v0.9.1 | User/team/role information |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | ACL model uses only base C# |

### 3.2 Licensing Behavior

- **Core Tier:** No ACLs (implicit all-access for single user)
- **WriterPro:** Basic ACL support (user grants only)
- **Teams:** Full ACL support (users, roles, teams)
- **Enterprise:** Full + service accounts, policy integration

---

## 4. Data Contract (The API)

### 4.1 EntityAcl Record

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Access control list for a specific entity.
/// Defines who can access the entity and what they can do.
/// </summary>
public record EntityAcl
{
    /// <summary>
    /// The entity this ACL protects.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Optional owner of the entity (usually the creator).
    /// Owner always has full access regardless of ACL entries.
    /// </summary>
    public Guid? OwnerId { get; init; }

    /// <summary>
    /// Default access level for principals not explicitly listed.
    /// Options: None (deny by default), Read, Write, Full, Inherit (from parent).
    /// </summary>
    public AccessLevel DefaultAccess { get; init; } = AccessLevel.Inherit;

    /// <summary>
    /// Individual ACL entries granting/denying permissions to principals.
    /// Evaluated in order; first match wins (except explicit deny).
    /// </summary>
    public IReadOnlyList<AclEntry> Entries { get; init; } = [];

    /// <summary>
    /// Whether to inherit permissions from parent entity.
    /// If true, child permissions are intersection of parent and local ACL.
    /// If false, local ACL is absolute.
    /// </summary>
    public bool InheritFromParent { get; init; } = true;

    /// <summary>
    /// ID of the parent entity (if any).
    /// Used for inheritance chain.
    /// </summary>
    public Guid? ParentEntityId { get; init; }

    /// <summary>
    /// When this ACL was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this ACL was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <summary>
    /// User who created this ACL.
    /// </summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>
    /// User who last modified this ACL.
    /// </summary>
    public Guid? ModifiedBy { get; init; }
}
```

### 4.2 AclEntry Record

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// A single entry in an entity's access control list.
/// Grants or denies permissions to a specific principal.
/// </summary>
public record AclEntry
{
    /// <summary>
    /// Unique identifier for this ACL entry.
    /// </summary>
    public Guid EntryId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The principal (user, role, team, etc.) this entry applies to.
    /// </summary>
    public required Guid PrincipalId { get; init; }

    /// <summary>
    /// Type of principal (User, Role, Team, ServiceAccount).
    /// </summary>
    public required PrincipalType PrincipalType { get; init; }

    /// <summary>
    /// Permissions explicitly allowed for this principal.
    /// </summary>
    public required Permission AllowedPermissions { get; init; }

    /// <summary>
    /// Permissions explicitly denied for this principal.
    /// Deny always takes precedence over Allow.
    /// </summary>
    public required Permission DeniedPermissions { get; init; }

    /// <summary>
    /// Optional expiration time for this entry.
    /// After this time, the entry is automatically revoked.
    /// Null = no expiration.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Whether this entry is currently active.
    /// Inactive entries are ignored during evaluation.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Optional reason for creating this entry.
    /// Used for audit purposes.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// When this entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who created this entry.
    /// </summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>
    /// Whether this entry applies only to direct children.
    /// If true, inherited further down the hierarchy is blocked.
    /// </summary>
    public bool StopInheritance { get; init; } = false;
}
```

### 4.3 PrincipalType Enum

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Categorizes the type of principal in an ACL entry.
/// </summary>
public enum PrincipalType
{
    /// <summary>Individual user.</summary>
    User = 1,

    /// <summary>Predefined role (Viewer, Editor, Admin, etc.).</summary>
    Role = 2,

    /// <summary>Team of users.</summary>
    Team = 3,

    /// <summary>Service account (CI/CD, integrations, etc.).</summary>
    ServiceAccount = 4
}
```

### 4.4 AccessLevel Enum

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// High-level access level for quick ACL definition.
/// Maps to specific Permission combinations.
/// </summary>
public enum AccessLevel
{
    /// <summary>No access (explicit deny).</summary>
    None = 1,

    /// <summary>Read-only access.</summary>
    Read = 2,

    /// <summary>Read and write access.</summary>
    Write = 3,

    /// <summary>Full access including admin operations.</summary>
    Full = 4,

    /// <summary>Inherit from parent entity.</summary>
    Inherit = 5
}
```

### 4.5 AccessLevelExtensions

```csharp
namespace Lexichord.Modules.Security.Extensions;

/// <summary>
/// Helper methods for AccessLevel enum.
/// </summary>
public static class AccessLevelExtensions
{
    /// <summary>
    /// Maps AccessLevel to corresponding Permission set.
    /// </summary>
    public static Permission ToPermission(this AccessLevel level) =>
        level switch
        {
            AccessLevel.None => Permission.None,
            AccessLevel.Read => Permission.ReadOnly,
            AccessLevel.Write => Permission.Contributor,
            AccessLevel.Full => Permission.EntityFull,
            AccessLevel.Inherit => Permission.None, // Handled by evaluator
            _ => Permission.None
        };

    /// <summary>
    /// Maps Permission to closest AccessLevel.
    /// Used for display purposes.
    /// </summary>
    public static AccessLevel ToAccessLevel(this Permission permissions)
    {
        if (permissions == Permission.None)
            return AccessLevel.None;

        if (permissions.HasAllPermissions(Permission.EntityFull))
            return AccessLevel.Full;

        if (permissions.HasAllPermissions(Permission.Contributor))
            return AccessLevel.Write;

        if (permissions.HasPermission(Permission.EntityRead))
            return AccessLevel.Read;

        return AccessLevel.None;
    }
}
```

### 4.6 IAclEvaluator Interface

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Evaluates permissions using entity ACLs.
/// Handles inheritance, expiration, and aggregation.
/// </summary>
public interface IAclEvaluator
{
    /// <summary>
    /// Gets the ACL for an entity.
    /// Returns null if no ACL is defined (uses default access).
    /// </summary>
    /// <param name="entityId">Entity to get ACL for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Entity's ACL or null</returns>
    Task<EntityAcl?> GetAclAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Evaluates what permissions a principal has for an entity.
    /// Considers inheritance, expiration, and explicit denials.
    /// </summary>
    /// <param name="entityId">Entity being accessed</param>
    /// <param name="principalId">User/role/team/service account</param>
    /// <param name="principalType">Type of principal</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Effective permissions for the principal</returns>
    Task<Permission> EvaluatePermissionsAsync(
        Guid entityId,
        Guid principalId,
        PrincipalType principalType,
        CancellationToken ct = default);

    /// <summary>
    /// Creates or updates an ACL for an entity.
    /// </summary>
    /// <param name="acl">ACL to save</param>
    /// <param name="ct">Cancellation token</param>
    Task<EntityAcl> SaveAclAsync(EntityAcl acl, CancellationToken ct = default);

    /// <summary>
    /// Adds a new entry to an entity's ACL.
    /// </summary>
    /// <param name="entityId">Entity to modify</param>
    /// <param name="entry">Entry to add</param>
    /// <param name="ct">Cancellation token</param>
    Task<AclEntry> AddEntryAsync(Guid entityId, AclEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Removes an entry from an entity's ACL.
    /// </summary>
    /// <param name="entityId">Entity to modify</param>
    /// <param name="entryId">Entry to remove</param>
    /// <param name="ct">Cancellation token</param>
    Task RemoveEntryAsync(Guid entityId, Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing ACL entry.
    /// </summary>
    /// <param name="entityId">Entity to modify</param>
    /// <param name="entry">Updated entry (EntryId must match)</param>
    /// <param name="ct">Cancellation token</param>
    Task<AclEntry> UpdateEntryAsync(Guid entityId, AclEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Invalidates cached ACL for an entity.
    /// Called when ACL is modified.
    /// </summary>
    /// <param name="entityId">Entity whose cache should be cleared</param>
    Task InvalidateAclCacheAsync(Guid entityId);
}
```

---

## 5. ACL Evaluation Algorithm

### 5.1 Permission Evaluation Flow

```
Given: entityId, principalId, principalType

1. OWNER CHECK
   If principal is entity owner:
     Return Full permissions (EntityFull)

2. GET ACL
   Retrieve EntityAcl for entity
   If no ACL exists:
     Use default access (usually Inherit)

3. EXPIRATION CHECK
   For each AclEntry:
     If ExpiresAt < now:
       Mark entry as expired, skip

4. PRINCIPAL MATCHING
   Find all AclEntry where:
     PrincipalId = principalId AND
     PrincipalType = principalType AND
     IsActive = true AND
     NOT expired

   For Role entries:
     Also match if principal has that role

5. ENTRY EVALUATION (in order)
   For each matched entry:
     allowedPerms |= entry.AllowedPermissions
     deniedPerms |= entry.DeniedPermissions

6. EXPLICIT DENY CHECK
   If deniedPerms contains any permission:
     Remove from allowedPerms
     (Deny always wins)

7. INHERITANCE
   If InheritFromParent and ParentEntityId exists:
     parentPerms = Evaluate(ParentEntityId, principalId)
     effectivePerms = allowedPerms & parentPerms
   Else:
     effectivePerms = allowedPerms

8. DEFAULT ACCESS
   If effectivePerms = None:
     effectivePerms = DefaultAccess.ToPermission()

9. RETURN
   Return effectivePerms
```

---

## 6. Implementation

### 6.1 AclEvaluator Implementation Outline

```csharp
namespace Lexichord.Modules.Security.Services;

/// <summary>
/// Evaluates entity-level ACLs.
/// </summary>
public class AclEvaluator : IAclEvaluator
{
    private readonly IAclRepository _repository;
    private readonly IProfileService _profiles;
    private readonly IGraphRepository _graph;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AclEvaluator> _logger;

    public AclEvaluator(
        IAclRepository repository,
        IProfileService profiles,
        IGraphRepository graph,
        IMemoryCache cache,
        ILogger<AclEvaluator> logger)
    {
        _repository = repository;
        _profiles = profiles;
        _graph = graph;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EntityAcl?> GetAclAsync(Guid entityId, CancellationToken ct = default)
    {
        var cacheKey = $"acl:{entityId}";
        if (_cache.TryGetValue(cacheKey, out EntityAcl? cached))
            return cached;

        var acl = await _repository.GetAclAsync(entityId, ct);
        if (acl != null)
            _cache.Set(cacheKey, acl, TimeSpan.FromHours(1));

        return acl;
    }

    public async Task<Permission> EvaluatePermissionsAsync(
        Guid entityId,
        Guid principalId,
        PrincipalType principalType,
        CancellationToken ct = default)
    {
        // 1. Owner check
        var entity = await _graph.GetEntityAsync(entityId, ct);
        if (entity?.OwnerId == principalId)
            return Permission.EntityFull;

        // 2. Get ACL
        var acl = await GetAclAsync(entityId, ct);
        if (acl == null)
            return Permission.None;

        // 3-5. Find matching entries
        var allowedPerms = Permission.None;
        var deniedPerms = Permission.None;
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in acl.Entries)
        {
            // Skip inactive/expired
            if (!entry.IsActive || (entry.ExpiresAt.HasValue && entry.ExpiresAt < now))
                continue;

            // Match principal
            if (entry.PrincipalId != principalId)
            {
                // Check if principal has the role
                if (entry.PrincipalType != PrincipalType.Role)
                    continue;

                var hasRole = await _profiles.UserHasRoleAsync(
                    principalId,
                    entry.PrincipalId,
                    ct);

                if (!hasRole)
                    continue;
            }
            else if (entry.PrincipalType != principalType)
            {
                continue;
            }

            // Aggregate permissions
            allowedPerms |= entry.AllowedPermissions;
            deniedPerms |= entry.DeniedPermissions;
        }

        // 6. Apply explicit deny
        var effectivePerms = allowedPerms & ~deniedPerms;

        // 7. Inheritance
        if (acl.InheritFromParent && acl.ParentEntityId.HasValue && !acl.Entries.Any(e => e.StopInheritance))
        {
            var parentPerms = await EvaluatePermissionsAsync(
                acl.ParentEntityId.Value,
                principalId,
                principalType,
                ct);

            effectivePerms &= parentPerms;  // Intersection
        }

        // 8. Default access
        if (effectivePerms == Permission.None && acl.DefaultAccess != AccessLevel.Inherit)
            effectivePerms = acl.DefaultAccess.ToPermission();

        return effectivePerms;
    }

    public async Task<EntityAcl> SaveAclAsync(EntityAcl acl, CancellationToken ct = default)
    {
        var saved = await _repository.SaveAclAsync(acl, ct);
        await InvalidateAclCacheAsync(acl.EntityId);
        return saved;
    }

    public async Task<AclEntry> AddEntryAsync(Guid entityId, AclEntry entry, CancellationToken ct = default)
    {
        var acl = await GetAclAsync(entityId, ct);
        if (acl == null)
            throw new InvalidOperationException($"No ACL found for entity {entityId}");

        var newEntry = await _repository.AddEntryAsync(entityId, entry, ct);
        await InvalidateAclCacheAsync(entityId);

        return newEntry;
    }

    public async Task RemoveEntryAsync(Guid entityId, Guid entryId, CancellationToken ct = default)
    {
        await _repository.RemoveEntryAsync(entityId, entryId, ct);
        await InvalidateAclCacheAsync(entityId);
    }

    public async Task<AclEntry> UpdateEntryAsync(Guid entityId, AclEntry entry, CancellationToken ct = default)
    {
        var updated = await _repository.UpdateEntryAsync(entityId, entry, ct);
        await InvalidateAclCacheAsync(entityId);
        return updated;
    }

    public async Task InvalidateAclCacheAsync(Guid entityId)
    {
        _cache.Remove($"acl:{entityId}");
        _logger.LogDebug("Invalidated ACL cache for entity {EntityId}", entityId);
    }
}
```

---

## 7. Error Handling

### 7.1 Expired Entry

**Scenario:** ACL entry has passed its expiration date.

**Handling:**
- Entry is automatically skipped during evaluation
- Expired entries remain in database for audit
- Optional cleanup job removes old entries

### 7.2 Invalid Principal

**Scenario:** ACL references non-existent user/role/team.

**Handling:**
- Entry is kept as-is (may be restored)
- Evaluation skips entry with warning log
- Doesn't block evaluation

### 7.3 Circular Inheritance

**Scenario:** Parent entity references child as parent (circular).

**Handling:**
- Detected during SaveAclAsync
- Throws InvalidOperationException
- Prevents saving circular reference

### 7.4 Missing Parent

**Scenario:** InheritFromParent is true but ParentEntityId not set.

**Handling:**
- Treats as no inheritance
- Logs warning
- Uses DefaultAccess instead

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class AclEvaluatorTests
{
    [TestMethod]
    public async Task EvaluatePermissions_Owner_ReturnsFullAccess()
    {
        // Owner always has full access
        var result = await _evaluator.EvaluatePermissionsAsync(
            entityId: _entity.Id,
            principalId: _entity.OwnerId.Value,
            principalType: PrincipalType.User);

        Assert.AreEqual(Permission.EntityFull, result);
    }

    [TestMethod]
    public async Task EvaluatePermissions_ExplicitGrant_ReturnsPermissions()
    {
        // ACL entry grants read/write
        var result = await _evaluator.EvaluatePermissionsAsync(
            entityId: _entity.Id,
            principalId: _user.Id,
            principalType: PrincipalType.User);

        Assert.IsTrue(result.HasPermission(Permission.EntityRead));
        Assert.IsTrue(result.HasPermission(Permission.EntityWrite));
    }

    [TestMethod]
    public async Task EvaluatePermissions_ExplicitDeny_OverridesGrant()
    {
        // Entry grants Read but denies Write
        var result = await _evaluator.EvaluatePermissionsAsync(
            entityId: _entity.Id,
            principalId: _user.Id,
            principalType: PrincipalType.User);

        Assert.IsTrue(result.HasPermission(Permission.EntityRead));
        Assert.IsFalse(result.HasPermission(Permission.EntityWrite));
    }

    [TestMethod]
    public async Task EvaluatePermissions_ExpiredEntry_SkipsEntry()
    {
        // Entry expired 1 hour ago
        var result = await _evaluator.EvaluatePermissionsAsync(
            entityId: _entity.Id,
            principalId: _user.Id,
            principalType: PrincipalType.User);

        Assert.AreEqual(Permission.None, result);
    }

    [TestMethod]
    public async Task EvaluatePermissions_Inheritance_IntersectsWithParent()
    {
        // Parent grants Full, Child grants Write
        var result = await _evaluator.EvaluatePermissionsAsync(
            entityId: _childEntity.Id,
            principalId: _user.Id,
            principalType: PrincipalType.User);

        // Child is intersection of both = Write
        Assert.IsTrue(result.HasPermission(Permission.EntityRead));
        Assert.IsTrue(result.HasPermission(Permission.EntityWrite));
        Assert.IsFalse(result.HasPermission(Permission.EntityDelete));
    }

    [TestMethod]
    public async Task SaveAcl_CircularReference_ThrowsException()
    {
        var acl = new EntityAcl
        {
            EntityId = _entity.Id,
            ParentEntityId = _childEntity.Id
        };

        // _childEntity has _entity as parent
        Assert.ThrowsException<InvalidOperationException>(
            async () => await _evaluator.SaveAclAsync(acl));
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class AclEvaluatorIntegrationTests
{
    [TestMethod]
    public async Task CompleteFlow_AddModifyRemove_AclEntries()
    {
        // Test full lifecycle: add entry, modify, remove
        var entry = new AclEntry
        {
            PrincipalId = _user.Id,
            PrincipalType = PrincipalType.User,
            AllowedPermissions = Permission.EntityRead | Permission.EntityWrite
        };

        // Add
        var added = await _evaluator.AddEntryAsync(_entity.Id, entry);
        Assert.IsNotNull(added);

        // Verify
        var perms = await _evaluator.EvaluatePermissionsAsync(
            _entity.Id, _user.Id, PrincipalType.User);
        Assert.IsTrue(perms.HasPermission(Permission.EntityRead));

        // Remove
        await _evaluator.RemoveEntryAsync(_entity.Id, added.EntryId);

        // Verify removed
        var permsAfter = await _evaluator.EvaluatePermissionsAsync(
            _entity.Id, _user.Id, PrincipalType.User);
        Assert.AreEqual(Permission.None, permsAfter);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| ACL lookup | <1ms | In-memory cache with 1-hour TTL |
| Permission evaluation | <5ms | Single-pass entry matching |
| Inheritance chain | <10ms | Recursive evaluation with caching |
| Entry modification | <10ms | Cache invalidation on write |

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Owner access loss | High | Owner always has full access (cannot be revoked) |
| Circular inheritance | High | Detected and prevented during save |
| Expired entries linger | Medium | Cleanup job removes old entries |
| ACL corruption | Low | Audit logging of all changes |

---

## 11. License Gating

| Tier | Support |
| :--- | :--- |
| Core | No ACLs |
| WriterPro | Basic user grants only |
| Teams | Full ACL support |
| Enterprise | Full + service accounts |

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Entity with no ACL | EvaluatePermissionsAsync called | Uses DefaultAccess level |
| 2 | Owner principal | EvaluatePermissionsAsync called | Returns EntityFull |
| 3 | Matching entry with grant | EvaluatePermissionsAsync called | Permission included in result |
| 4 | Explicit deny entry | EvaluatePermissionsAsync called | Permission excluded from result |
| 5 | Entry with expiration in past | EvaluatePermissionsAsync called | Entry skipped |
| 6 | Entry with expiration in future | EvaluatePermissionsAsync called | Entry included |
| 7 | Parent and child ACL | Inheritance enabled | Child = intersection of parent and child |
| 8 | Circular parent reference | SaveAclAsync called | InvalidOperationException thrown |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - EntityAcl, AclEntry, evaluation algorithm, inheritance |
