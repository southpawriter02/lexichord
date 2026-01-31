# LCS-DES-111-SEC-h: Design Specification — Role Management

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `SEC-111-h` | Access Control sub-part h |
| **Feature Name** | `Role Management` | Create, modify, and assign roles |
| **Target Version** | `v0.11.1h` | Eighth sub-part of v0.11.1-SEC |
| **Module Scope** | `Lexichord.Modules.Security` | Security module |
| **Swimlane** | `Security` | Security vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.Security.RoleManagement` | Role management feature flag |
| **Author** | Security Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-111-SEC](./LCS-SBD-111-SEC.md) | Access Control & Authorization scope |
| **Scope Breakdown** | [LCS-SBD-111-SEC S2.1](./LCS-SBD-111-SEC.md#21-sub-parts) | h = Role Management |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.11.1-SEC requires a role management system to:

1. Define and manage built-in and custom roles
2. Assign roles to users and teams
3. Modify role permissions and policies
4. Track role membership and assignments
5. Prevent modification of built-in roles
6. Support role hierarchy and inheritance

### 2.2 The Proposed Solution

Implement role management with:

1. **IRoleService interface:** Public API for role operations
2. **Role creation/modification:** Fluent builders for custom roles
3. **Role assignment:** Grant/revoke roles to principals
4. **Built-in role protection:** Prevent modification of system roles
5. **Audit logging:** Track all role changes

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| Permission model | v0.11.1e | Role, Permission, PolicyRule definitions |
| IProfileService | v0.9.1 | User/team management |
| IAuditLogService | v0.11.2-SEC | Log role changes |
| IAclEvaluator | v0.11.1g | Updated when role permissions change |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Microsoft.Extensions.Caching.Memory` | 8.0+ | Role cache |

### 3.2 Licensing Behavior

- **Core Tier:** No role management (single user)
- **WriterPro:** Built-in roles only (read-only)
- **Teams:** Full role management (custom roles)
- **Enterprise:** Full + policy editing

---

## 4. Data Contract (The API)

### 4.1 IRoleService Interface

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Service for managing roles and role assignments.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Gets all available roles (built-in and custom).
    /// </summary>
    /// <param name="includeBuiltIn">Whether to include built-in roles</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all roles</returns>
    Task<IReadOnlyList<Role>> GetAllRolesAsync(
        bool includeBuiltIn = true,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific role by ID.
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Role or null if not found</returns>
    Task<Role?> GetRoleAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets a role by name.
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Role or null if not found</returns>
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom role.
    /// </summary>
    /// <param name="role">Role to create</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created role with generated ID</returns>
    /// <exception cref="InvalidOperationException">If name already exists or invalid</exception>
    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing custom role.
    /// Built-in roles cannot be modified.
    /// </summary>
    /// <param name="role">Updated role</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated role</returns>
    /// <exception cref="InvalidOperationException">If built-in role or not found</exception>
    Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom role.
    /// Built-in roles cannot be deleted.
    /// </summary>
    /// <param name="roleId">Role to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidOperationException">If built-in role or not found</exception>
    Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Assigns a role to a principal (user or team).
    /// </summary>
    /// <param name="principalId">User or team ID</param>
    /// <param name="roleId">Role to assign</param>
    /// <param name="expiresAt">Optional expiration date for role assignment</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Assignment record</returns>
    Task<RoleAssignment> AssignRoleAsync(
        Guid principalId,
        Guid roleId,
        DateTimeOffset? expiresAt = null,
        CancellationToken ct = default);

    /// <summary>
    /// Revokes a role assignment.
    /// </summary>
    /// <param name="principalId">User or team ID</param>
    /// <param name="roleId">Role to revoke</param>
    /// <param name="ct">Cancellation token</param>
    Task RevokeRoleAsync(Guid principalId, Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets all roles assigned to a principal.
    /// </summary>
    /// <param name="principalId">User or team ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assigned roles</returns>
    Task<IReadOnlyList<Role>> GetPrincipalRolesAsync(
        Guid principalId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all principals assigned to a role.
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assignment records</returns>
    Task<IReadOnlyList<RoleAssignment>> GetRoleAssignmentsAsync(
        Guid roleId,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a policy rule to a role.
    /// </summary>
    /// <param name="roleId">Role to modify</param>
    /// <param name="policy">Policy rule to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated role</returns>
    Task<Role> AddPolicyAsync(Guid roleId, PolicyRule policy, CancellationToken ct = default);

    /// <summary>
    /// Removes a policy rule from a role.
    /// </summary>
    /// <param name="roleId">Role to modify</param>
    /// <param name="policyId">Policy rule ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    Task RemovePolicyAsync(Guid roleId, Guid policyId, CancellationToken ct = default);

    /// <summary>
    /// Invalidates role cache (call when roles change externally).
    /// </summary>
    /// <param name="roleId">Role ID or null to clear all</param>
    Task InvalidateRoleCacheAsync(Guid? roleId = null);
}
```

### 4.2 RoleAssignment Record

```csharp
namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Records the assignment of a role to a principal.
/// </summary>
public record RoleAssignment
{
    /// <summary>
    /// Unique identifier for this assignment.
    /// </summary>
    public Guid AssignmentId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The principal (user or team) being assigned.
    /// </summary>
    public required Guid PrincipalId { get; init; }

    /// <summary>
    /// Type of principal.
    /// </summary>
    public required PrincipalType PrincipalType { get; init; }

    /// <summary>
    /// The role being assigned.
    /// </summary>
    public required Guid RoleId { get; init; }

    /// <summary>
    /// The role details (convenience, may be null in some contexts).
    /// </summary>
    public Role? RoleDetails { get; init; }

    /// <summary>
    /// Optional expiration date for this assignment.
    /// After this date, the assignment is automatically inactive.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Whether this assignment is currently active.
    /// Inactive assignments are ignored.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// When this assignment was created.
    /// </summary>
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who made this assignment.
    /// </summary>
    public Guid? AssignedBy { get; init; }

    /// <summary>
    /// Optional reason for the assignment.
    /// Used for audit purposes.
    /// </summary>
    public string? Reason { get; init; }
}
```

### 4.3 RoleBuilder Helper

```csharp
namespace Lexichord.Modules.Security.Builders;

/// <summary>
/// Fluent builder for creating and modifying roles.
/// </summary>
public class RoleBuilder
{
    private Guid? _roleId;
    private string? _name;
    private string? _description;
    private Permission _permissions = Permission.None;
    private RoleType _type = RoleType.Custom;
    private List<PolicyRule> _policies = [];
    private List<Guid> _assignedTo = [];

    public RoleBuilder WithId(Guid roleId)
    {
        _roleId = roleId;
        return this;
    }

    public RoleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoleBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public RoleBuilder WithPermissions(Permission permissions)
    {
        _permissions = permissions;
        return this;
    }

    public RoleBuilder GrantPermission(Permission permission)
    {
        _permissions |= permission;
        return this;
    }

    public RoleBuilder RevokePermission(Permission permission)
    {
        _permissions &= ~permission;
        return this;
    }

    public RoleBuilder WithType(RoleType type)
    {
        _type = type;
        return this;
    }

    public RoleBuilder AddPolicy(PolicyRule policy)
    {
        _policies.Add(policy);
        return this;
    }

    public RoleBuilder AssignTo(Guid principalId)
    {
        _assignedTo.Add(principalId);
        return this;
    }

    public Role Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Role name is required");

        if (_name.Length > 100)
            throw new InvalidOperationException("Role name cannot exceed 100 characters");

        return new Role
        {
            RoleId = _roleId ?? Guid.NewGuid(),
            Name = _name,
            Description = _description,
            Permissions = _permissions,
            Type = _type,
            IsBuiltIn = false,
            Policies = _policies.Count > 0 ? _policies.AsReadOnly() : null,
            AssignedTo = _assignedTo.Count > 0 ? _assignedTo.AsReadOnly() : null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

---

## 5. Role Management Operations

### 5.1 Create Role Example

```csharp
var apiReviewerRole = new RoleBuilder()
    .WithName("API Reviewer")
    .WithDescription("Reviews and validates API documentation")
    .GrantPermission(Permission.EntityRead)
    .GrantPermission(Permission.ClaimValidate)
    .GrantPermission(Permission.ValidationRun)
    .AddPolicy(new PolicyRule
    {
        RuleId = Guid.NewGuid(),
        Name = "APIs Only",
        Description = "Only review API entities",
        Condition = "resource.entityType = 'Endpoint'",
        Effect = PolicyEffect.Allow,
        GrantPermissions = Permission.ClaimValidate
    })
    .Build();

var created = await roleService.CreateRoleAsync(apiReviewerRole);
```

### 5.2 Assign Role Example

```csharp
// Assign role to user
await roleService.AssignRoleAsync(
    principalId: userId,
    roleId: apiReviewerRole.RoleId,
    expiresAt: DateTimeOffset.UtcNow.AddMonths(3));

// Assign to team
await roleService.AssignRoleAsync(
    principalId: teamId,
    roleId: BuiltInRoles.Editor.RoleId);
```

### 5.3 Modify Role Permissions Example

```csharp
var role = await roleService.GetRoleAsync(customRoleId);

// Build updated role
var updated = new RoleBuilder()
    .WithId(role.RoleId)
    .WithName(role.Name)
    .WithDescription(role.Description)
    .WithPermissions(role.Permissions)
    .GrantPermission(Permission.InferenceRun)
    .Build();

await roleService.UpdateRoleAsync(updated);
```

---

## 6. Implementation

### 6.1 RoleService Implementation Outline

```csharp
namespace Lexichord.Modules.Security.Services;

/// <summary>
/// Service for managing roles and role assignments.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;
    private readonly IProfileService _profiles;
    private readonly IAuditLogService _audit;
    private readonly IAuthorizationService _authService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository repository,
        IProfileService profiles,
        IAuditLogService audit,
        IAuthorizationService authService,
        IMemoryCache cache,
        ILogger<RoleService> logger)
    {
        _repository = repository;
        _profiles = profiles;
        _audit = audit;
        _authService = authService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(
        bool includeBuiltIn = true,
        CancellationToken ct = default)
    {
        var cacheKey = $"roles:all:{includeBuiltIn}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Role>? cached))
            return cached!;

        var roles = new List<Role>();

        if (includeBuiltIn)
        {
            roles.Add(BuiltInRoles.Viewer);
            roles.Add(BuiltInRoles.Contributor);
            roles.Add(BuiltInRoles.Editor);
            roles.Add(BuiltInRoles.Admin);
        }

        var customRoles = await _repository.GetAllRolesAsync(ct);
        roles.AddRange(customRoles);

        var result = roles.AsReadOnly();
        _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

        return result;
    }

    public async Task<Role?> GetRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        // Check built-in roles first
        var builtIn = GetBuiltInRole(roleId);
        if (builtIn != null)
            return builtIn;

        var cacheKey = $"role:{roleId}";
        if (_cache.TryGetValue(cacheKey, out Role? cached))
            return cached;

        var role = await _repository.GetRoleAsync(roleId, ct);
        if (role != null)
            _cache.Set(cacheKey, role, TimeSpan.FromHours(1));

        return role;
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
    {
        // Check built-in roles first
        var builtIn = GetBuiltInRoleByName(roleName);
        if (builtIn != null)
            return builtIn;

        return await _repository.GetRoleByNameAsync(roleName, ct);
    }

    public async Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(role.Name))
            throw new InvalidOperationException("Role name is required");

        if (role.IsBuiltIn)
            throw new InvalidOperationException("Cannot create built-in roles");

        // Check name doesn't conflict
        var existing = await GetRoleByNameAsync(role.Name, ct);
        if (existing != null)
            throw new InvalidOperationException($"Role '{role.Name}' already exists");

        // Save
        var created = await _repository.CreateRoleAsync(role, ct);

        // Audit
        await _audit.LogAsync(
            action: "RoleCreated",
            resourceId: created.RoleId,
            resourceType: "Role",
            details: $"Created role: {created.Name}");

        // Invalidate cache
        await InvalidateRoleCacheAsync();

        _logger.LogInformation("Created custom role {RoleName} ({RoleId})", created.Name, created.RoleId);

        return created;
    }

    public async Task<Role> UpdateRoleAsync(Role role, CancellationToken ct = default)
    {
        var existing = await GetRoleAsync(role.RoleId, ct);
        if (existing == null)
            throw new InvalidOperationException($"Role {role.RoleId} not found");

        if (existing.IsBuiltIn)
            throw new InvalidOperationException("Cannot modify built-in roles");

        // Check name conflict
        if (role.Name != existing.Name)
        {
            var withSameName = await GetRoleByNameAsync(role.Name, ct);
            if (withSameName != null)
                throw new InvalidOperationException($"Role '{role.Name}' already exists");
        }

        var updated = await _repository.UpdateRoleAsync(role, ct);

        // Audit
        await _audit.LogAsync(
            action: "RoleUpdated",
            resourceId: updated.RoleId,
            resourceType: "Role",
            details: $"Updated role: {updated.Name}");

        // Invalidate cache and user permissions
        await InvalidateRoleCacheAsync(role.RoleId);

        // Invalidate permissions for all users with this role
        var assignments = await _repository.GetAssignmentsForRoleAsync(role.RoleId, ct);
        foreach (var assignment in assignments)
            await _authService.InvalidateUserPermissionsCacheAsync(assignment.PrincipalId);

        _logger.LogInformation("Updated custom role {RoleName} ({RoleId})", updated.Name, updated.RoleId);

        return updated;
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await GetRoleAsync(roleId, ct);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} not found");

        if (role.IsBuiltIn)
            throw new InvalidOperationException("Cannot delete built-in roles");

        // Get all assignments
        var assignments = await _repository.GetAssignmentsForRoleAsync(roleId, ct);

        // Delete
        await _repository.DeleteRoleAsync(roleId, ct);

        // Audit
        await _audit.LogAsync(
            action: "RoleDeleted",
            resourceId: roleId,
            resourceType: "Role",
            details: $"Deleted role: {role.Name}");

        // Invalidate cache and user permissions
        await InvalidateRoleCacheAsync();

        foreach (var assignment in assignments)
            await _authService.InvalidateUserPermissionsCacheAsync(assignment.PrincipalId);

        _logger.LogInformation("Deleted custom role {RoleName} ({RoleId})", role.Name, roleId);
    }

    public async Task<RoleAssignment> AssignRoleAsync(
        Guid principalId,
        Guid roleId,
        DateTimeOffset? expiresAt = null,
        CancellationToken ct = default)
    {
        var role = await GetRoleAsync(roleId, ct);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} not found");

        var assignment = new RoleAssignment
        {
            AssignmentId = Guid.NewGuid(),
            PrincipalId = principalId,
            RoleId = roleId,
            ExpiresAt = expiresAt,
            AssignedAt = DateTimeOffset.UtcNow
        };

        var created = await _repository.CreateAssignmentAsync(assignment, ct);

        // Audit
        await _audit.LogAsync(
            action: "RoleAssigned",
            resourceId: principalId,
            resourceType: "RoleAssignment",
            details: $"Assigned role {role.Name} to principal {principalId}");

        // Invalidate user permissions
        await _authService.InvalidateUserPermissionsCacheAsync(principalId);

        _logger.LogInformation("Assigned role {RoleId} to principal {PrincipalId}", roleId, principalId);

        return created;
    }

    public async Task RevokeRoleAsync(Guid principalId, Guid roleId, CancellationToken ct = default)
    {
        await _repository.DeleteAssignmentAsync(principalId, roleId, ct);

        // Audit
        await _audit.LogAsync(
            action: "RoleRevoked",
            resourceId: principalId,
            resourceType: "RoleAssignment",
            details: $"Revoked role {roleId} from principal {principalId}");

        // Invalidate user permissions
        await _authService.InvalidateUserPermissionsCacheAsync(principalId);

        _logger.LogInformation("Revoked role {RoleId} from principal {PrincipalId}", roleId, principalId);
    }

    public async Task<IReadOnlyList<Role>> GetPrincipalRolesAsync(
        Guid principalId,
        CancellationToken ct = default)
    {
        var cacheKey = $"principal-roles:{principalId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Role>? cached))
            return cached!;

        var roles = await _repository.GetPrincipalRolesAsync(principalId, ct);
        _cache.Set(cacheKey, roles, TimeSpan.FromMinutes(5));

        return roles;
    }

    public async Task<IReadOnlyList<RoleAssignment>> GetRoleAssignmentsAsync(
        Guid roleId,
        CancellationToken ct = default)
    {
        return await _repository.GetAssignmentsForRoleAsync(roleId, ct);
    }

    public async Task<Role> AddPolicyAsync(Guid roleId, PolicyRule policy, CancellationToken ct = default)
    {
        var role = await GetRoleAsync(roleId, ct);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} not found");

        if (role.IsBuiltIn)
            throw new InvalidOperationException("Cannot add policies to built-in roles");

        var updated = await _repository.AddPolicyAsync(roleId, policy, ct);

        // Invalidate cache
        await InvalidateRoleCacheAsync(roleId);

        return updated;
    }

    public async Task RemovePolicyAsync(Guid roleId, Guid policyId, CancellationToken ct = default)
    {
        var role = await GetRoleAsync(roleId, ct);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} not found");

        if (role.IsBuiltIn)
            throw new InvalidOperationException("Cannot remove policies from built-in roles");

        await _repository.RemovePolicyAsync(roleId, policyId, ct);

        // Invalidate cache
        await InvalidateRoleCacheAsync(roleId);
    }

    public async Task InvalidateRoleCacheAsync(Guid? roleId = null)
    {
        if (roleId.HasValue)
        {
            _cache.Remove($"role:{roleId}");
        }
        else
        {
            _cache.Remove("roles:all:true");
            _cache.Remove("roles:all:false");
        }
    }

    private Role? GetBuiltInRole(Guid roleId)
    {
        return roleId switch
        {
            var id when id == BuiltInRoles.Viewer.RoleId => BuiltInRoles.Viewer,
            var id when id == BuiltInRoles.Contributor.RoleId => BuiltInRoles.Contributor,
            var id when id == BuiltInRoles.Editor.RoleId => BuiltInRoles.Editor,
            var id when id == BuiltInRoles.Admin.RoleId => BuiltInRoles.Admin,
            _ => null
        };
    }

    private Role? GetBuiltInRoleByName(string name)
    {
        return name switch
        {
            nameof(BuiltInRoles.Viewer) => BuiltInRoles.Viewer,
            nameof(BuiltInRoles.Contributor) => BuiltInRoles.Contributor,
            nameof(BuiltInRoles.Editor) => BuiltInRoles.Editor,
            nameof(BuiltInRoles.Admin) => BuiltInRoles.Admin,
            _ => null
        };
    }
}
```

---

## 7. Error Handling

### 7.1 Built-In Role Modification

**Scenario:** Attempt to modify or delete built-in role.

**Handling:**
- Throw InvalidOperationException with clear message
- Log attempt for audit
- Built-in roles always remain unchanged

### 7.2 Duplicate Role Name

**Scenario:** Create role with name that already exists.

**Handling:**
- Throw InvalidOperationException before save
- Check existing names
- Return helpful error message

### 7.3 Expired Assignment

**Scenario:** Role assignment has passed expiration date.

**Handling:**
- Assignment remains in database but marked inactive
- IsActive flag checked during permission evaluation
- Optional cleanup job removes old assignments

### 7.4 Circular Policy Dependencies

**Scenario:** Policy rules reference each other.

**Handling:**
- Policy condition evaluation handles cycles gracefully
- Malformed policies log error and are skipped
- Service continues to operate

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class RoleServiceTests
{
    [TestMethod]
    public async Task GetAllRoles_IncludesBuiltInRoles()
    {
        var roles = await _roleService.GetAllRolesAsync(includeBuiltIn: true);

        Assert.IsTrue(roles.Any(r => r.Name == "Viewer"));
        Assert.IsTrue(roles.Any(r => r.Name == "Contributor"));
        Assert.IsTrue(roles.Any(r => r.Name == "Editor"));
        Assert.IsTrue(roles.Any(r => r.Name == "Admin"));
    }

    [TestMethod]
    public async Task CreateRole_WithValidInput_CreatesRole()
    {
        var role = new RoleBuilder()
            .WithName("DataAnalyst")
            .WithPermissions(Permission.ReadOnly)
            .Build();

        var created = await _roleService.CreateRoleAsync(role);

        Assert.IsNotNull(created.RoleId);
        Assert.AreEqual("DataAnalyst", created.Name);
    }

    [TestMethod]
    public async Task CreateRole_DuplicateName_ThrowsException()
    {
        var role = new RoleBuilder()
            .WithName("Editor")
            .WithPermissions(Permission.ReadOnly)
            .Build();

        // Should fail because "Editor" already exists (built-in)
        Assert.ThrowsException<InvalidOperationException>(
            async () => await _roleService.CreateRoleAsync(role));
    }

    [TestMethod]
    public async Task UpdateRole_BuiltInRole_ThrowsException()
    {
        var admin = BuiltInRoles.Admin;

        Assert.ThrowsException<InvalidOperationException>(
            async () => await _roleService.UpdateRoleAsync(admin));
    }

    [TestMethod]
    public async Task AssignRole_ToUser_CreatesAssignment()
    {
        var role = await _roleService.CreateRoleAsync(
            new RoleBuilder().WithName("TestRole").Build());

        var assignment = await _roleService.AssignRoleAsync(
            principalId: _userId,
            roleId: role.RoleId);

        Assert.AreEqual(_userId, assignment.PrincipalId);
        Assert.AreEqual(role.RoleId, assignment.RoleId);
    }

    [TestMethod]
    public async Task RevokeRole_RemovesAssignment()
    {
        var role = await _roleService.CreateRoleAsync(
            new RoleBuilder().WithName("TestRole").Build());

        await _roleService.AssignRoleAsync(_userId, role.RoleId);
        await _roleService.RevokeRoleAsync(_userId, role.RoleId);

        var roles = await _roleService.GetPrincipalRolesAsync(_userId);
        Assert.IsFalse(roles.Any(r => r.RoleId == role.RoleId));
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class RoleServiceIntegrationTests
{
    [TestMethod]
    public async Task CompleteRoleLifecycle()
    {
        // Create role
        var role = await _roleService.CreateRoleAsync(
            new RoleBuilder()
                .WithName("SecurityAuditor")
                .GrantPermission(Permission.EntityRead)
                .GrantPermission(Permission.ValidationRun)
                .Build());

        // Assign to user
        await _roleService.AssignRoleAsync(_auditUserId, role.RoleId);

        // Verify assignment
        var userRoles = await _roleService.GetPrincipalRolesAsync(_auditUserId);
        Assert.IsTrue(userRoles.Any(r => r.RoleId == role.RoleId));

        // Update role
        var updated = new RoleBuilder()
            .WithId(role.RoleId)
            .WithName("SecurityAuditor")
            .GrantPermission(Permission.EntityRead)
            .GrantPermission(Permission.ValidationConfigure)  // Added
            .Build();

        await _roleService.UpdateRoleAsync(updated);

        // Verify update
        var updatedRole = await _roleService.GetRoleAsync(role.RoleId);
        Assert.IsTrue(updatedRole.Permissions.HasPermission(Permission.ValidationConfigure));

        // Revoke role
        await _roleService.RevokeRoleAsync(_auditUserId, role.RoleId);

        // Verify revocation
        var finalRoles = await _roleService.GetPrincipalRolesAsync(_auditUserId);
        Assert.IsFalse(finalRoles.Any(r => r.RoleId == role.RoleId));

        // Delete role
        await _roleService.DeleteRoleAsync(role.RoleId);

        var deleted = await _roleService.GetRoleAsync(role.RoleId);
        Assert.IsNull(deleted);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| Get role | <1ms | Cache built-in + database roles |
| List roles | <5ms | Cache full list, TTL 1 hour |
| Create role | <10ms | Direct database write |
| Assign role | <5ms | Cache invalidation on write |
| List assignments | <10ms | Database query |

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Built-in role modification | Critical | Prevent modification, explicit IsBuiltIn flag |
| Role escalation | High | Audit all assignments, permission validation |
| Circular policies | Medium | Error handling in policy evaluation |
| Cache inconsistency | Low | Strategic invalidation on writes |

---

## 11. License Gating

| Tier | Support |
| :--- | :--- |
| Core | No role management |
| WriterPro | Built-in roles only (read-only) |
| Teams | Full role management |
| Enterprise | Full + policy management |

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | GetAllRoles called | includeBuiltIn = true | Viewer, Contributor, Editor, Admin returned |
| 2 | CreateRole called | Valid role data | New custom role created with ID |
| 3 | Built-in role | UpdateRoleAsync called | InvalidOperationException thrown |
| 4 | Valid role | DeleteRoleAsync called | Role deleted from system |
| 5 | Valid role and user | AssignRoleAsync called | RoleAssignment created |
| 6 | Active assignment | GetPrincipalRolesAsync called | Role included in list |
| 7 | Expired assignment | GetPrincipalRolesAsync called | Role not included (expired) |
| 8 | Role assignment exists | RevokeRoleAsync called | Assignment removed |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - Role CRUD, assignments, built-in role protection, caching |
