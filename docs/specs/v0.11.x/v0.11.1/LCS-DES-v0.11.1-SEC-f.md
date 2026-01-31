# LCS-DES-111-SEC-b: Design Specification — Access Control UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `SEC-111-j` | Access Control sub-part f |
| **Feature Name** | `Access Control UI` | Administrative interface for permissions |
| **Target Version** | `v0.11.1f` | Sixth sub-part of v0.11.1-SEC |
| **Module Scope** | `Lexichord.Web.Security` | Web module for security admin |
| **Swimlane** | `Security` | Security vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.Security.AdminUI` | Security admin UI flag |
| **Author** | Security Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-111-SEC](./LCS-SBD-v0.11.1-SEC.md) | Access Control & Authorization scope |
| **Scope Breakdown** | [LCS-SBD-111-SEC S2.1](./LCS-SBD-v0.11.1-SEC.md#21-sub-parts) | f = Access Control UI |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.11.1-SEC requires an administrative user interface to:

1. View and manage entity-level access control lists
2. Create, modify, and delete roles
3. Assign and revoke roles to/from users and teams
4. View effective permissions for any user
5. Create and configure policy rules
6. Visualize permission inheritance chains
7. Audit access control changes

### 2.2 The Proposed Solution

Implement an admin UI with:

1. **Access Control Panel:** Per-entity ACL editor
2. **Role Management:** Role CRUD and assignments
3. **Permission Viewer:** Show effective permissions
4. **Policy Editor:** Create/edit ABAC policies
5. **Inheritance Visualization:** Show permission chains
6. **Audit Log:** Track all access control changes

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| IAuthorizationService | v0.11.1b | Check UI permissions |
| IRoleService | v0.11.1d | Role management |
| IAclEvaluator | v0.11.1c | ACL evaluation |
| IInheritanceEvaluator | v0.11.1e | Inheritance visualization |
| IAuditLogService | v0.11.2-SEC | Access control audit logs |
| IProfileService | v0.9.1 | User/team information |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Microsoft.AspNetCore.Mvc` | 8.0+ | Web API controllers |
| `Swashbuckle.AspNetCore` | 6.4+ | API documentation |

### 3.2 Licensing Behavior

- **Core Tier:** No admin UI
- **WriterPro:** View-only role information
- **Teams:** Full admin UI with role and ACL management
- **Enterprise:** Full + policy editing

---

## 4. UI Components & Layout

### 4.1 Entity Access Control Panel

```
┌────────────────────────────────────────────────────────────────┐
│ Access Control: UserService                         [Close]    │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Owner: alice@company.com                    [Change Owner]     │
│                                                                │
│ Default Access: [Inherit from Workspace ▼]                    │
│ ☑ Inherit permissions from parent entities                    │
│                                                                │
│ ═══════════════════════════════════════════════════════════   │
│ Access Control Entries                                         │
│ ═══════════════════════════════════════════════════════════   │
│                                                                │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ Principal          │ Type  │ Allow      │ Deny   │ Expires │ │
│ ├────────────────────────────────────────────────────────────┤ │
│ │ API Team           │ Team  │ Full       │ —      │ —       │ │
│ │ bob@company.com    │ User  │ Read/Write │ Delete │ —       │ │
│ │ External Auditors  │ Role  │ Read       │ —      │ 30 days │ │
│ │ ci-service-account │ Svc   │ Read       │ —      │ —       │ │
│ └────────────────────────────────────────────────────────────┘ │
│                                                                │
│ [+ Add Entry]                                                  │
│                                                                │
│ ═══════════════════════════════════════════════════════════   │
│ Effective Permissions (for current user)                       │
│ ═══════════════════════════════════════════════════════════   │
│                                                                │
│ ☑ Read   ☑ Write   ☐ Delete   ☐ Admin                        │
│                                                                │
│ Applied through:                                               │
│ ├── Role: Editor (EntityRead, EntityWrite)                    │
│ └── ACL: API Team (Full)                                      │
│                                                                │
│ [Cancel] [Save Changes]                                        │
└────────────────────────────────────────────────────────────────┘
```

### 4.2 Role Management Panel

```
┌────────────────────────────────────────────────────────────────┐
│ Role Management                                     [+ New]    │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Built-in Roles:                                                │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ 👤 Viewer                                         [View]   │ │
│ │    Can view entities, relationships, and claims           │ │
│ │    Permissions: EntityRead, RelationshipRead, ClaimRead   │ │
│ ├────────────────────────────────────────────────────────────┤ │
│ │ ✏️ Contributor                                    [View]   │ │
│ │    Can view and edit entities and claims                  │ │
│ │    Permissions: ReadOnly + Write permissions              │ │
│ ├────────────────────────────────────────────────────────────┤ │
│ │ 📝 Editor                                         [View]   │ │
│ │    Full edit access including axioms and inference        │ │
│ │    Permissions: Contributor + Axiom + Inference           │ │
│ ├────────────────────────────────────────────────────────────┤ │
│ │ 🔑 Admin                                          [View]   │ │
│ │    Full administrative access                             │ │
│ │    Permissions: All                                       │ │
│ └────────────────────────────────────────────────────────────┘ │
│                                                                │
│ Custom Roles:                                                  │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ 📋 API Reviewer                          [Edit] [Delete]   │ │
│ │    Can review and validate API documentation              │ │
│ │    Permissions: ReadOnly + ValidationRun                  │ │
│ │    Members: 5 users                                       │ │
│ ├────────────────────────────────────────────────────────────┤ │
│ │ 🔒 Security Auditor                      [Edit] [Delete]   │ │
│ │    Read-only access with audit log viewing                │ │
│ │    Permissions: ReadOnly + AuditRead                      │ │
│ │    Policy: EntityTypa = "Endpoint" AND hasAutd = true     │ │
│ │    Members: 2 users                                       │ │
│ └────────────────────────────────────────────────────────────┘ │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 4.3 Effective Permissions Viewer

```
┌────────────────────────────────────────────────────────────────┐
│ Effective Permissions: bob@company.com                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Roles:                                                         │
│ • Contributor (assigned by admin, never expires)             │
│ • API Reviewer (assigned by admin, expires 2026-03-31)       │
│                                                                │
│ ════════════════════════════════════════════════════════════  │
│ Base Role Permissions (from roles):                            │
│ ════════════════════════════════════════════════════════════  │
│                                                                │
│ ☑ EntityRead       ☑ EntityWrite      ☐ EntityDelete         │
│ ☑ RelationshipRead ☑ RelationshipWrite ☐ RelationshipDelete  │
│ ☑ ClaimRead        ☑ ClaimWrite        ☑ ClaimValidate       │
│ ☑ ValidationRun    ☐ ValidationConfigure                      │
│                                                                │
│ ════════════════════════════════════════════════════════════  │
│ Entity-Specific ACLs (current entity):                         │
│ ════════════════════════════════════════════════════════════  │
│                                                                │
│ UserService entity:                                            │
│ • Direct: ☑ EntityRead ☑ EntityWrite ☐ EntityDelete         │
│ • From Role "API Team": ☑ EntityFull                          │
│ • Result: ☑ Read ☑ Write ☐ Delete (limited by entity ACL)   │
│                                                                │
│ ════════════════════════════════════════════════════════════  │
│ Policy Rules Applied:                                          │
│ ════════════════════════════════════════════════════════════  │
│                                                                │
│ ✓ "APIs Only" (Allow): Grants EntityRead to Endpoint types   │
│ ✗ "Restricted PII" (Deny): Denies all access to PII entities │
│                                                                │
│ ════════════════════════════════════════════════════════════  │
│ Final Effective Permissions:                                   │
│ ════════════════════════════════════════════════════════════  │
│                                                                │
│ ☑ EntityRead       ☑ EntityWrite      ☐ EntityDelete         │
│ ☑ RelationshipRead ☑ RelationshipWrite ☐ RelationshipDelete  │
│ ☑ ClaimRead        ☑ ClaimWrite        ☑ ClaimValidate       │
│ ☑ ValidationRun    ☐ ValidationConfigure                      │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 4.4 Permission Inheritance Visualization

```
┌────────────────────────────────────────────────────────────────┐
│ Permission Inheritance: AuthenticationService                 │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Inheritance Chain (Path from root to this entity):             │
│                                                                │
│           ┌─────────────┐                                      │
│           │ Workspace   │ Permissions: Full                   │
│           │ (Root)      │ Alice: Full                          │
│           └──────┬──────┘                                      │
│                  │                                             │
│           ┌──────▼──────┐                                      │
│           │ Security    │ Permissions: Read/Write (inherited)  │
│           │ Component   │ Bob: Read (ACL override)             │
│           └──────┬──────┘                                      │
│                  │                                             │
│    ┌─────────────┼─────────────┐                              │
│    │             │             │                              │
│ ┌──▼──┐      ┌──▼──┐      ┌──▼──┐                             │
│ │Auth │      │Crypt│      │Audit│                             │
│ │Svc  │      │ Service    │Svc  │                             │
│ └─────┘      └─────┘      └─────┘                             │
│                                                                │
│ Inheritance Pattern: Strict (child ≤ parent)                  │
│ Blocks Inheritance: ☐ No                                      │
│                                                                │
│ Alice's Effective Permissions:                                │
│ • Via Workspace: Full                                         │
│ • Result: Full (inherited)                                    │
│                                                                │
│ Bob's Effective Permissions:                                  │
│ • Via inherited chain: Read/Write                             │
│ • Via local ACL override: Read only (explicit deny on Write)  │
│ • Result: Read (intersection of inherited and local)          │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## 5. API Endpoints

### 5.1 Entity Access Control Endpoints

```csharp
namespace Lexichord.Web.Security.Controllers;

[ApiController]
[Route("api/v1/entities/{entityId}/access-control")]
[Authorize]
public class EntityAccessControlController : ControllerBase
{
    /// <summary>GET entity ACL</summary>
    [HttpGet]
    public async Task<ActionResult<EntityAcl>> GetAcl(Guid entityId)

    /// <summary>PATCH update entity ACL</summary>
    [HttpPatch]
    public async Task<ActionResult<EntityAcl>> UpdateAcl(
        Guid entityId,
        [FromBody] EntityAcl updates)

    /// <summary>POST add ACL entry</summary>
    [HttpPost("entries")]
    public async Task<ActionResult<AclEntry>> AddEntry(
        Guid entityId,
        [FromBody] AclEntry entry)

    /// <summary>DELETE ACL entry</summary>
    [HttpDelete("entries/{entryId}")]
    public async Task<ActionResult> RemoveEntry(Guid entityId, Guid entryId)

    /// <summary>PATCH update ACL entry</summary>
    [HttpPatch("entries/{entryId}")]
    public async Task<ActionResult<AclEntry>> UpdateEntry(
        Guid entityId,
        Guid entryId,
        [FromBody] AclEntry updates)

    /// <summary>GET entity owner and default access</summary>
    [HttpGet("settings")]
    public async Task<ActionResult<EntityAccessSettings>> GetSettings(Guid entityId)

    /// <summary>PATCH update owner and default access</summary>
    [HttpPatch("settings")]
    public async Task<ActionResult<EntityAccessSettings>> UpdateSettings(
        Guid entityId,
        [FromBody] EntityAccessSettings settings)
}
```

### 5.2 Role Management Endpoints

```csharp
namespace Lexichord.Web.Security.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RoleManagementController : ControllerBase
{
    /// <summary>GET all roles</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Role>>> GetRoles(
        [FromQuery] bool includeBuiltIn = true)

    /// <summary>GET specific role</summary>
    [HttpGet("{roleId}")]
    public async Task<ActionResult<Role>> GetRole(Guid roleId)

    /// <summary>POST create custom role</summary>
    [HttpPost]
    public async Task<ActionResult<Role>> CreateRole([FromBody] CreateRoleRequest request)

    /// <summary>PATCH update custom role</summary>
    [HttpPatch("{roleId}")]
    public async Task<ActionResult<Role>> UpdateRole(
        Guid roleId,
        [FromBody] UpdateRoleRequest request)

    /// <summary>DELETE custom role</summary>
    [HttpDelete("{roleId}")]
    public async Task<ActionResult> DeleteRole(Guid roleId)

    /// <summary>POST assign role to principal</summary>
    [HttpPost("{roleId}/assign")]
    public async Task<ActionResult<RoleAssignment>> AssignRole(
        Guid roleId,
        [FromBody] AssignRoleRequest request)

    /// <summary>DELETE revoke role from principal</summary>
    [HttpDelete("{roleId}/assign")]
    public async Task<ActionResult> RevokeRole(
        Guid roleId,
        [FromQuery] Guid principalId)

    /// <summary>GET role assignments</summary>
    [HttpGet("{roleId}/assignments")]
    public async Task<ActionResult<IReadOnlyList<RoleAssignment>>> GetAssignments(
        Guid roleId)

    /// <summary>GET principal roles</summary>
    [HttpGet("principals/{principalId}")]
    public async Task<ActionResult<IReadOnlyList<Role>>> GetPrincipalRoles(
        Guid principalId)
}
```

### 5.3 Permissions Viewer Endpoints

```csharp
namespace Lexichord.Web.Security.Controllers;

[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    /// <summary>GET effective permissions for user</summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserPermissions>> GetUserPermissions(Guid userId)

    /// <summary>GET effective permissions at entity</summary>
    [HttpGet("users/{userId}/entities/{entityId}")]
    public async Task<ActionResult<EntityPermissions>> GetEntityPermissions(
        Guid userId,
        Guid entityId)

    /// <summary>GET permission explanation</summary>
    [HttpGet("explain")]
    public async Task<ActionResult<PermissionExplanation>> ExplainPermission(
        [FromQuery] Guid userId,
        [FromQuery] Guid entityId,
        [FromQuery] Permission permission)
}
```

### 5.4 Inheritance Visualization Endpoints

```csharp
namespace Lexichord.Web.Security.Controllers;

[ApiController]
[Route("api/v1/inheritance")]
[Authorize]
public class InheritanceController : ControllerBase
{
    /// <summary>GET inheritance chain for entity</summary>
    [HttpGet("entities/{entityId}/chain")]
    public async Task<ActionResult<InheritanceChain>> GetInheritanceChain(
        Guid entityId)

    /// <summary>GET ancestors of entity</summary>
    [HttpGet("entities/{entityId}/ancestors")]
    public async Task<ActionResult<IReadOnlyList<AncestorInfo>>> GetAncestors(
        Guid entityId)

    /// <summary>GET descendants of entity</summary>
    [HttpGet("entities/{entityId}/descendants")]
    public async Task<ActionResult<IReadOnlyList<DescendantInfo>>> GetDescendants(
        Guid entityId)
}
```

### 5.5 Audit Log Endpoints

```csharp
namespace Lexichord.Web.Security.Controllers;

[ApiController]
[Route("api/v1/access-control/audit")]
[Authorize]
public class AccessControlAuditController : ControllerBase
{
    /// <summary>GET access control audit log</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<AuditLog>>> GetAuditLog(
        [FromQuery] AuditLogFilter filter,
        [FromQuery] int pageSiza = 50,
        [FromQuery] int pageNumber = 1)

    /// <summary>GET entity access control changes</summary>
    [HttpGet("entities/{entityId}")]
    public async Task<ActionResult<PaginatedList<AuditLog>>> GetEntityAuditLog(
        Guid entityId,
        [FromQuery] int pageSiza = 50)

    /// <summary>GET role changes</summary>
    [HttpGet("roles/{roleId}")]
    public async Task<ActionResult<PaginatedList<AuditLog>>> GetRoleAuditLog(
        Guid roleId,
        [FromQuery] int pageSiza = 50)
}
```

---

## 6. Implementation Notes

### 6.1 Authorization Checks

All endpoints require authorization checks:

```csharp
// Entity ACL modification requires EntityAdmin permission
[Authorize(Policy = "EntityAdmin")]

// Role management requires Admin role
[Authorize(Policy = "AdminRole")]

// Viewing permissions requires EntityRead at minimum
[Authorize(Policy = "EntityRead")]
```

### 6.2 Audit Logging

All modifications are logged:

```csharp
await _auditLog.LogAsync(
    action: "AclEntryAdded",
    resourceId: entityId,
    resourceType: "EntityAcl",
    details: $"Added ACL entry: {entry.PrincipalId}");
```

### 6.3 Request/Response Types

```csharp
public record CreateRoleRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Permission Permissions { get; init; }
    public RoleType Type { get; init; } = RoleType.Custom;
}

public record AssignRoleRequest
{
    public required Guid PrincipalId { get; init; }
    public PrincipalType PrincipalType { get; init; } = PrincipalType.User;
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Reason { get; init; }
}

public record PermissionExplanation
{
    public bool IsAuthorized { get; init; }
    public Permission UserPermissions { get; init; }
    public IReadOnlyList<string> Sources { get; init; } = [];
    public string? Reason { get; init; }
}
```

---

## 7. Error Handling

### 7.1 Unauthorized Access

**Scenario:** User lacks permission to manage ACL.

**Handling:**
- Return 403 Forbidden
- Log unauthorized attempt
- Include error message in response

### 7.2 Invalid Entity

**Scenario:** Entity ID doesn't exist.

**Handling:**
- Return 404 Not Found
- Log error
- Return empty data

### 7.3 Built-In Role Modification

**Scenario:** Attempt to modify built-in role.

**Handling:**
- Return 400 Bad Request
- Include error message
- Log attempt for audit

### 7.4 Circular Inheritance

**Scenario:** Parent assignment would create cycle.

**Handling:**
- Return 400 Bad Request
- Clear error message about circular relationship
- Prevent save

---

## 8. Testing

### 8.1 Controller Tests

```csharp
[TestClass]
public class EntityAccessControlControllerTests
{
    [TestMethod]
    public async Task GetAcl_ReturnsEntityAcl()
    {
        var result = await _controller.GetAcl(_entityId);
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task AddEntry_UnauthorizedUser_Returns403()
    {
        // Setup: user without EntityAdmin permission
        var result = await _controller.AddEntry(_entityId, _entry);
        Assert.IsInstanceOfType(result.Result, typeof(ForbiddenResult));
    }

    [TestMethod]
    public async Task UpdateAcl_ValidChanges_Succeeds()
    {
        var result = await _controller.UpdateAcl(_entityId, _aclUpdates);
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Browser |
| :--- | :--- | :--- |
| Load ACL panel | <500ms | Real-time fetch + cache |
| Save ACL changes | <1s | Validation + database |
| List roles | <500ms | Server-side pagination |
| Render inheritance tree | <1s | Lazy load descendants |

---

## 10. Accessibility & UX

| Aspect | Implementation |
| :--- | :--- |
| Keyboard navigation | Full tab support, enter to submit |
| Screen readers | ARIA labels, semantic HTML |
| Error messages | Clear, actionable messages |
| Color contrast | WCAG AAA compliant |
| Responsive design | Mobile and desktop support |

---

## 11. License Gating

| Tier | Support |
| :--- | :--- |
| Core | No UI |
| WriterPro | View-only |
| Teams | Full management |
| Enterprise | Full + policy editing |

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Entity with ACL | Access Control panel opened | ACL entries displayed |
| 2 | User with EntityAdmin permission | Add entry clicked | New entry added to ACL |
| 3 | Valid ACL changes | Save clicked | Changes persisted, audit logged |
| 4 | Custom role exists | Role Management opened | Role listed with edit/delete options |
| 5 | Role selected | Assign clicked | Principal selection modal appears |
| 6 | Principal and role selected | Assign confirmed | Assignment created |
| 7 | User selected | Effective Permissions viewed | All sources and final permissions shown |
| 8 | Entity with parent | Inheritance panel opened | Chain visualized with inheritance type |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - Access control panel, role management, permissions viewer, inheritance visualization, audit log |
