# LCS-SBD-v0.18.7-SEC: Scope Breakdown — Workspace Isolation & Sandboxing

## 1. Document Control

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-SBD-v0.18.7-SEC |
| **Release Version** | v0.18.7 |
| **Module Name** | Workspace Isolation & Sandboxing |
| **Parent Release** | v0.18.x — Security & Compliance |
| **Document Type** | Scope Breakdown Document (SBD) |
| **Author** | Security Architecture Lead |
| **Date Created** | 2026-02-03 |
| **Last Updated** | 2026-02-03 |
| **Status** | DRAFT |
| **Classification** | Internal — Technical Specification |
| **Estimated Total Hours** | 68 hours |
| **Target Completion** | Sprint 18.7 (6 weeks) |

---

## 2. Executive Summary

### 2.1 The Problem

AI systems with file system access, code execution capabilities, and network connectivity represent a significant attack surface. Without proper isolation:

- **Directory Traversal:** AI could access sensitive files outside the intended workspace (e.g., `~/.ssh/`, `/etc/passwd`, `.env` files)
- **Privilege Escalation:** AI-executed code could gain elevated permissions
- **Resource Exhaustion:** Runaway processes could consume all system resources
- **Cross-Tenant Leakage:** In multi-user environments, data could leak between users
- **Persistence Attacks:** Malicious code could establish persistence mechanisms

### 2.2 The Solution

Implement defense-in-depth workspace isolation using:

1. **Filesystem Isolation:** Restrict AI operations to defined workspace boundaries
2. **Privilege Containment:** Enforce least-privilege execution model
3. **Resource Quotas:** Hard limits on CPU, memory, disk, and network
4. **Process Sandboxing:** Isolate spawned processes in secure containers
5. **Cross-Tenant Security:** Complete isolation between users/projects

### 2.3 Security Model

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Workspace Isolation Layers                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  User Request                                                                │
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    Path Validation Layer                                 ││
│  │  • Canonicalization (resolve symlinks, .., etc.)                        ││
│  │  • Boundary checking (within workspace?)                                 ││
│  │  • Sensitive path detection (~/.ssh, .env, etc.)                        ││
│  │  • Permission verification (read/write/execute)                          ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    Privilege Enforcement Layer                           ││
│  │  • Capability checking (required vs. granted)                           ││
│  │  • Escalation detection (attempting elevated ops?)                       ││
│  │  • Least privilege enforcement (drop unneeded caps)                      ││
│  │  • Privilege logging (audit all elevated operations)                     ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    Resource Quota Layer                                  ││
│  │  • CPU limits (cgroups)                                                  ││
│  │  • Memory limits (cgroups)                                               ││
│  │  • Disk quotas (filesystem quotas)                                       ││
│  │  • Network bandwidth limits                                              ││
│  │  • Process count limits                                                  ││
│  │  • Open file limits                                                      ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│       │                                                                      │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    Process Sandbox Layer                                 ││
│  │  • Namespace isolation (mount, PID, network, user)                       ││
│  │  • Seccomp filtering (syscall restrictions)                              ││
│  │  • AppArmor/SELinux profiles                                             ││
│  │  • Capability dropping                                                   ││
│  │  • Read-only root filesystem                                             ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│       │                                                                      │
│       ▼                                                                      │
│  Isolated Execution Environment                                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Detailed Sub-Parts Breakdown

### 3.1 v0.18.7a: Working Directory Isolation

**Estimated Hours**: 12 hours
**Sprint Assignment**: Week 1-2
**Status**: Pending Development

#### 3.1.1 Objective

Implement strict filesystem isolation ensuring AI operations cannot access files outside the designated workspace, even through symlinks, relative paths, or other escape techniques.

#### 3.1.2 Scope

- Implement `IWorkspaceIsolationManager` for boundary enforcement
- Create path canonicalization to resolve all symlinks and relative references
- Build workspace boundary validation
- Implement sensitive path detection and blocking
- Create workspace inheritance for sub-projects
- Build escape attempt detection and logging
- Implement temporary workspace creation for untrusted operations

#### 3.1.3 Key Interfaces

```csharp
namespace Lexichord.Security.Isolation;

/// <summary>
/// Manages workspace isolation boundaries for AI operations.
/// </summary>
public interface IWorkspaceIsolationManager
{
    /// <summary>
    /// Create an isolated workspace with defined boundaries.
    /// </summary>
    Task<IsolatedWorkspace> CreateWorkspaceAsync(
        WorkspaceCreationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a path is within workspace boundaries.
    /// </summary>
    Task<PathValidationResult> ValidatePathAsync(
        IsolatedWorkspace workspace,
        string requestedPath,
        PathOperation operation,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve a path to its canonical form within the workspace.
    /// </summary>
    Task<CanonicalPathResult> CanonicalizePathAsync(
        IsolatedWorkspace workspace,
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a path would escape the workspace.
    /// </summary>
    Task<EscapeCheckResult> CheckEscapeAttemptAsync(
        IsolatedWorkspace workspace,
        string requestedPath,
        CancellationToken ct = default);

    /// <summary>
    /// Get workspace for a session.
    /// </summary>
    Task<IsolatedWorkspace?> GetWorkspaceAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Destroy workspace and clean up resources.
    /// </summary>
    Task DestroyWorkspaceAsync(
        Guid workspaceId,
        CancellationToken ct = default);
}

public record WorkspaceCreationRequest
{
    public required string RootPath { get; init; }
    public UserId OwnerId { get; init; }
    public Guid SessionId { get; init; }
    public IReadOnlyList<string>? AdditionalAllowedPaths { get; init; }
    public IReadOnlyList<string>? ExplicitlyDeniedPaths { get; init; }
    public IReadOnlyList<string>? ReadOnlyPaths { get; init; }
    public WorkspacePermissions Permissions { get; init; } = new();
    public ResourceQuotas Quotas { get; init; } = new();
    public TimeSpan? MaxLifetime { get; init; }
    public bool AllowSymlinks { get; init; } = false;
    public bool AllowHardLinks { get; init; } = false;
    public IsolationLevel IsolationLevel { get; init; } = IsolationLevel.Standard;
}

public record IsolatedWorkspace
{
    public Guid WorkspaceId { get; init; }
    public string RootPath { get; init; } = "";
    public string CanonicalRootPath { get; init; } = "";  // Fully resolved
    public UserId OwnerId { get; init; }
    public Guid SessionId { get; init; }
    public IReadOnlyList<string> AllowedPaths { get; init; } = [];
    public IReadOnlyList<string> DeniedPaths { get; init; } = [];
    public IReadOnlyList<string> ReadOnlyPaths { get; init; } = [];
    public WorkspacePermissions Permissions { get; init; } = new();
    public ResourceQuotas Quotas { get; init; } = new();
    public IsolationLevel IsolationLevel { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public WorkspaceStatus Status { get; init; }
}

public enum IsolationLevel
{
    /// <summary>
    /// Basic path validation only.
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard isolation with symlink resolution.
    /// </summary>
    Standard,

    /// <summary>
    /// Strict isolation with namespace separation.
    /// </summary>
    Strict,

    /// <summary>
    /// Maximum isolation with full containerization.
    /// </summary>
    Maximum
}

public enum WorkspaceStatus
{
    Active,
    Suspended,
    Expired,
    Destroyed
}

public record PathValidationResult
{
    public bool IsValid { get; init; }
    public string RequestedPath { get; init; } = "";
    public string CanonicalPath { get; init; } = "";
    public PathValidationStatus Status { get; init; }
    public string? RejectionReason { get; init; }
    public IReadOnlyList<PathSecurityIssue> Issues { get; init; } = [];
}

public enum PathValidationStatus
{
    Valid,
    OutsideWorkspace,
    DeniedPath,
    SensitivePath,
    SymlinkEscape,
    TraversalAttempt,
    PermissionDenied,
    PathNotFound,
    InvalidPath
}

public record PathSecurityIssue
{
    public PathIssueType Type { get; init; }
    public string Description { get; init; } = "";
    public string AffectedPath { get; init; } = "";
    public PathIssueSeverity Severity { get; init; }
}

public enum PathIssueType
{
    SymlinkPointsOutside,
    HardLinkToOutside,
    TraversalSequence,
    SensitiveFile,
    SystemPath,
    HiddenFile,
    ExecutableInWritable,
    WorldWritable
}

public record EscapeCheckResult
{
    public bool EscapeAttempted { get; init; }
    public EscapeType? DetectedType { get; init; }
    public string TargetPath { get; init; } = "";
    public string EscapeDescription { get; init; } = "";
    public EscapeAction ActionTaken { get; init; }
}

public enum EscapeType
{
    RelativeTraversal,      // ../../etc/passwd
    SymlinkEscape,          // Symlink pointing outside
    HardLinkEscape,         // Hard link to outside file
    RaceCondition,          // TOCTOU attack
    EncodingBypass,         // URL encoding, null bytes
    CaseSensitivityBypass,  // Case manipulation on case-insensitive FS
    UnicodeNormalization    // Unicode tricks
}

public enum EscapeAction
{
    Blocked,
    LoggedAndBlocked,
    AlertedAndBlocked,
    QuarantinedSession
}
```

#### 3.1.4 Sensitive Path Patterns

```csharp
/// <summary>
/// Default sensitive paths that should never be accessible.
/// </summary>
public static class SensitivePathPatterns
{
    public static readonly IReadOnlyList<string> AlwaysDenied = new[]
    {
        // SSH keys and config
        "~/.ssh/*",
        "*/.ssh/*",
        "*.pem",
        "*.key",
        "*_rsa",
        "*_ed25519",
        "*_ecdsa",
        "*_dsa",

        // Environment and secrets
        ".env",
        ".env.*",
        "*.env",
        ".secrets",
        "secrets.*",
        "*credentials*",
        "*password*",

        // Git credentials
        ".git-credentials",
        ".gitconfig",
        ".netrc",

        // Cloud credentials
        "~/.aws/*",
        "~/.azure/*",
        "~/.gcloud/*",
        "~/.config/gcloud/*",
        "*service-account*.json",

        // Database files
        "*.sqlite",
        "*.db",
        "*.mdb",

        // System files
        "/etc/passwd",
        "/etc/shadow",
        "/etc/sudoers",
        "/etc/hosts",
        "/proc/*",
        "/sys/*",

        // Shell history
        ".*_history",
        ".bash_history",
        ".zsh_history",

        // Keychain/keyring
        "~/.gnupg/*",
        "*keychain*",
        "*keyring*",

        // IDE secrets
        ".idea/**/workspace.xml",
        ".vscode/**/settings.json"
    };
}
```

#### 3.1.5 Acceptance Criteria

- [ ] Path validation blocks all escape attempts
- [ ] Symlink resolution handles circular links safely
- [ ] Race condition protection (TOCTOU) implemented
- [ ] Sensitive path patterns comprehensive
- [ ] Performance <5ms per path validation
- [ ] Full audit logging of all path operations

---

### 3.2 v0.18.7b: Privilege Containment & Least Privilege

**Estimated Hours**: 14 hours
**Sprint Assignment**: Week 2-3
**Status**: Pending Development

#### 3.2.1 Objective

Enforce the principle of least privilege, ensuring AI operations run with only the minimum permissions required and cannot escalate to higher privileges.

#### 3.2.2 Scope

- Implement `IPrivilegeManager` for capability management
- Create privilege requirement analysis for operations
- Build privilege escalation detection
- Implement capability dropping after operations
- Create privilege inheritance rules
- Build privileged operation approval workflow
- Implement emergency privilege revocation

#### 3.2.3 Key Interfaces

```csharp
namespace Lexichord.Security.Privileges;

/// <summary>
/// Manages privileges and enforces least-privilege principle.
/// </summary>
public interface IPrivilegeManager
{
    /// <summary>
    /// Get minimum required privileges for an operation.
    /// </summary>
    Task<RequiredPrivileges> AnalyzeRequiredPrivilegesAsync(
        OperationRequest operation,
        CancellationToken ct = default);

    /// <summary>
    /// Check if current context has required privileges.
    /// </summary>
    Task<PrivilegeCheckResult> CheckPrivilegesAsync(
        RequiredPrivileges required,
        ExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Check if operation would escalate privileges.
    /// </summary>
    Task<EscalationCheckResult> CheckEscalationAsync(
        OperationRequest operation,
        CurrentPrivileges current,
        CancellationToken ct = default);

    /// <summary>
    /// Request elevated privileges (requires approval).
    /// </summary>
    Task<ElevationResult> RequestElevationAsync(
        ElevationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Drop privileges after operation completes.
    /// </summary>
    Task DropPrivilegesAsync(
        Guid operationId,
        CancellationToken ct = default);

    /// <summary>
    /// Emergency revoke all privileges for a session.
    /// </summary>
    Task RevokeAllPrivilegesAsync(
        Guid sessionId,
        string reason,
        CancellationToken ct = default);
}

public record RequiredPrivileges
{
    public IReadOnlyList<Capability> Capabilities { get; init; } = [];
    public PrivilegeLevel MinimumLevel { get; init; }
    public IReadOnlyList<string> RequiredPermissions { get; init; } = [];
    public bool RequiresElevation { get; init; }
    public string? ElevationJustification { get; init; }
}

public enum Capability
{
    // File operations
    FileRead,
    FileWrite,
    FileDelete,
    FileExecute,
    FileChangeOwner,
    FileChangePermissions,

    // Directory operations
    DirectoryRead,
    DirectoryCreate,
    DirectoryDelete,
    DirectoryTraverse,

    // Process operations
    ProcessSpawn,
    ProcessKill,
    ProcessSignal,
    ProcessPtrace,

    // Network operations
    NetworkConnect,
    NetworkListen,
    NetworkRaw,
    NetworkBindPrivileged,

    // System operations
    SystemRead,
    SystemWrite,
    SystemMount,
    SystemTime,
    SystemReboot,

    // User operations
    UserSwitch,
    UserCreate,
    UserModify,

    // Special
    CapabilityGrant,
    CapabilityRevoke,
    AuditWrite,
    AuditControl
}

public enum PrivilegeLevel
{
    /// <summary>
    /// Minimal privileges - read-only, no execution.
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard user privileges.
    /// </summary>
    Standard,

    /// <summary>
    /// Elevated privileges - write access, process spawning.
    /// </summary>
    Elevated,

    /// <summary>
    /// Administrative privileges - system modifications.
    /// </summary>
    Administrative,

    /// <summary>
    /// Root/superuser privileges.
    /// </summary>
    Root
}

public record EscalationCheckResult
{
    public bool WouldEscalate { get; init; }
    public EscalationType? DetectedType { get; init; }
    public PrivilegeLevel CurrentLevel { get; init; }
    public PrivilegeLevel RequestedLevel { get; init; }
    public IReadOnlyList<Capability> EscalatedCapabilities { get; init; } = [];
    public IReadOnlyList<string> EscalationReasons { get; init; } = [];
    public EscalationAction RecommendedAction { get; init; }
    public bool RequiresApproval { get; init; }
}

public enum EscalationType
{
    VerticalEscalation,     // User → Admin
    HorizontalEscalation,   // User A → User B
    CapabilityEscalation,   // Gaining new capabilities
    TemporalEscalation,     // Extending privilege duration
    ScopeEscalation         // Expanding privilege scope
}

public enum EscalationAction
{
    Allow,
    RequireApproval,
    RequireMFA,
    Block,
    AlertAndBlock,
    QuarantineSession
}

public record ElevationRequest
{
    public Guid SessionId { get; init; }
    public UserId RequestedBy { get; init; }
    public IReadOnlyList<Capability> RequestedCapabilities { get; init; } = [];
    public PrivilegeLevel RequestedLevel { get; init; }
    public string Justification { get; init; } = "";
    public TimeSpan? Duration { get; init; }
    public string? OperationContext { get; init; }
}

public record ElevationResult
{
    public bool Granted { get; init; }
    public Guid? ElevationId { get; init; }
    public IReadOnlyList<Capability> GrantedCapabilities { get; init; } = [];
    public PrivilegeLevel GrantedLevel { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? DenialReason { get; init; }
    public UserId? ApprovedBy { get; init; }
}
```

#### 3.2.4 Acceptance Criteria

- [ ] All operations analyzed for required privileges
- [ ] Escalation detection catches all known patterns
- [ ] Privilege dropping verified after operations
- [ ] Approval workflow for elevated operations
- [ ] Emergency revocation works within 1 second
- [ ] Full audit trail for privilege operations

---

### 3.3 v0.18.7c: Resource Quota Management

**Estimated Hours**: 10 hours
**Sprint Assignment**: Week 3-4
**Status**: Pending Development

#### 3.3.1 Objective

Implement hard resource quotas to prevent resource exhaustion attacks, runaway processes, and denial-of-service conditions.

#### 3.3.2 Key Interfaces

```csharp
namespace Lexichord.Security.Resources;

/// <summary>
/// Manages resource quotas for isolated workspaces.
/// </summary>
public interface IResourceQuotaManager
{
    /// <summary>
    /// Set quotas for a workspace.
    /// </summary>
    Task SetQuotasAsync(
        Guid workspaceId,
        ResourceQuotas quotas,
        CancellationToken ct = default);

    /// <summary>
    /// Check if operation would exceed quotas.
    /// </summary>
    Task<QuotaCheckResult> CheckQuotaAsync(
        Guid workspaceId,
        ResourceRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get current resource usage.
    /// </summary>
    Task<ResourceUsage> GetUsageAsync(
        Guid workspaceId,
        CancellationToken ct = default);

    /// <summary>
    /// Reserve resources for an operation.
    /// </summary>
    Task<ResourceReservation> ReserveAsync(
        Guid workspaceId,
        ResourceRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Release reserved resources.
    /// </summary>
    Task ReleaseAsync(
        Guid reservationId,
        CancellationToken ct = default);

    /// <summary>
    /// Enforce quota limits (kill processes if needed).
    /// </summary>
    Task<EnforcementResult> EnforceQuotasAsync(
        Guid workspaceId,
        CancellationToken ct = default);
}

public record ResourceQuotas
{
    // Memory
    public long MaxMemoryBytes { get; init; } = 512 * 1024 * 1024; // 512MB
    public long MaxVirtualMemoryBytes { get; init; } = 1024 * 1024 * 1024; // 1GB

    // CPU
    public int MaxCpuPercent { get; init; } = 50;
    public int MaxCpuCores { get; init; } = 2;
    public TimeSpan MaxCpuTime { get; init; } = TimeSpan.FromMinutes(30);

    // Disk
    public long MaxDiskUsageBytes { get; init; } = 1024 * 1024 * 1024; // 1GB
    public int MaxFilesCreated { get; init; } = 1000;
    public long MaxFileSizeBytes { get; init; } = 100 * 1024 * 1024; // 100MB
    public int MaxOpenFiles { get; init; } = 100;

    // Process
    public int MaxProcesses { get; init; } = 10;
    public int MaxThreadsPerProcess { get; init; } = 50;

    // Network
    public int MaxNetworkConnections { get; init; } = 10;
    public long MaxNetworkBytesPerSecond { get; init; } = 10 * 1024 * 1024; // 10MB/s
    public int MaxDnsQueries { get; init; } = 100;

    // Time
    public TimeSpan MaxOperationDuration { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan MaxIdleTime { get; init; } = TimeSpan.FromMinutes(5);
}

public record ResourceUsage
{
    public long MemoryBytes { get; init; }
    public float CpuPercent { get; init; }
    public long DiskBytes { get; init; }
    public int ProcessCount { get; init; }
    public int OpenFileCount { get; init; }
    public int NetworkConnectionCount { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public DateTimeOffset LastActivity { get; init; }

    // Percentages of quota used
    public float MemoryPercentUsed { get; init; }
    public float DiskPercentUsed { get; init; }
    public float ProcessPercentUsed { get; init; }
}

public record QuotaCheckResult
{
    public bool WithinQuota { get; init; }
    public IReadOnlyList<QuotaViolation> Violations { get; init; } = [];
    public ResourceUsage CurrentUsage { get; init; } = new();
    public QuotaAction RecommendedAction { get; init; }
}

public record QuotaViolation
{
    public ResourceType Resource { get; init; }
    public long CurrentValue { get; init; }
    public long QuotaLimit { get; init; }
    public float OveragePercent { get; init; }
    public QuotaSeverity Severity { get; init; }
}

public enum ResourceType
{
    Memory,
    VirtualMemory,
    Cpu,
    CpuTime,
    DiskSpace,
    FileCount,
    FileSize,
    OpenFiles,
    Processes,
    Threads,
    NetworkConnections,
    NetworkBandwidth,
    DnsQueries,
    OperationTime
}

public enum QuotaAction
{
    Allow,
    Warn,
    Throttle,
    Block,
    TerminateProcesses,
    SuspendWorkspace
}
```

---

### 3.4 v0.18.7d: Process Sandboxing & Isolation

**Estimated Hours**: 14 hours
**Sprint Assignment**: Week 4-5
**Status**: Pending Development

#### 3.4.1 Objective

Implement process-level sandboxing using OS isolation mechanisms (namespaces, cgroups, seccomp) to contain AI-spawned processes.

#### 3.4.2 Key Interfaces

```csharp
namespace Lexichord.Security.Sandbox;

/// <summary>
/// Manages process sandboxing and isolation.
/// </summary>
public interface IProcessSandbox
{
    /// <summary>
    /// Create a sandboxed environment for process execution.
    /// </summary>
    Task<SandboxEnvironment> CreateSandboxAsync(
        SandboxConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a process within the sandbox.
    /// </summary>
    Task<SandboxedProcessResult> ExecuteAsync(
        SandboxEnvironment sandbox,
        ProcessExecutionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Monitor a running sandboxed process.
    /// </summary>
    IObservable<SandboxEvent> MonitorProcess(
        Guid processId);

    /// <summary>
    /// Terminate a sandboxed process.
    /// </summary>
    Task TerminateAsync(
        Guid processId,
        TerminationReason reason,
        CancellationToken ct = default);

    /// <summary>
    /// Destroy sandbox and clean up all resources.
    /// </summary>
    Task DestroySandboxAsync(
        Guid sandboxId,
        CancellationToken ct = default);
}

public record SandboxConfiguration
{
    public IsolationLevel Level { get; init; } = IsolationLevel.Standard;

    // Namespace isolation
    public bool IsolatePidNamespace { get; init; } = true;
    public bool IsolateMountNamespace { get; init; } = true;
    public bool IsolateNetworkNamespace { get; init; } = true;
    public bool IsolateUserNamespace { get; init; } = true;
    public bool IsolateUtsNamespace { get; init; } = true;
    public bool IsolateIpcNamespace { get; init; } = true;

    // Filesystem
    public string? RootFilesystem { get; init; }
    public bool ReadOnlyRoot { get; init; } = true;
    public IReadOnlyList<MountPoint> Mounts { get; init; } = [];
    public IReadOnlyList<string> HiddenPaths { get; init; } = [];

    // Network
    public NetworkMode NetworkMode { get; init; } = NetworkMode.None;
    public IReadOnlyList<string>? AllowedHosts { get; init; }
    public IReadOnlyList<int>? AllowedPorts { get; init; }

    // Capabilities
    public IReadOnlyList<Capability> AllowedCapabilities { get; init; } = [];
    public IReadOnlyList<Capability> DroppedCapabilities { get; init; } = [];

    // Seccomp
    public bool EnableSeccomp { get; init; } = true;
    public SeccompProfile? SeccompProfile { get; init; }

    // AppArmor/SELinux
    public string? AppArmorProfile { get; init; }
    public string? SeLinuxLabel { get; init; }

    // Resource limits
    public ResourceQuotas ResourceQuotas { get; init; } = new();
}

public enum NetworkMode
{
    None,           // No network access
    Host,           // Host networking (dangerous)
    Bridge,         // Bridged networking
    Isolated,       // Isolated network namespace
    AllowListed     // Only allow-listed destinations
}

public record MountPoint
{
    public string Source { get; init; } = "";
    public string Target { get; init; } = "";
    public bool ReadOnly { get; init; } = true;
    public bool NoExec { get; init; } = true;
    public bool NoSuid { get; init; } = true;
    public bool NoDev { get; init; } = true;
}

public record SeccompProfile
{
    public SeccompAction DefaultAction { get; init; } = SeccompAction.Kill;
    public IReadOnlyList<SyscallRule> Rules { get; init; } = [];
}

public record SyscallRule
{
    public string Syscall { get; init; } = "";
    public SeccompAction Action { get; init; }
    public IReadOnlyList<SyscallArg>? Args { get; init; }
}

public enum SeccompAction
{
    Allow,
    Log,
    Errno,
    Trap,
    Kill
}

public record SandboxedProcessResult
{
    public Guid ProcessId { get; init; }
    public int ExitCode { get; init; }
    public string Stdout { get; init; } = "";
    public string Stderr { get; init; } = "";
    public TimeSpan Duration { get; init; }
    public ResourceUsage ResourceUsage { get; init; } = new();
    public IReadOnlyList<SecurityEvent> SecurityEvents { get; init; } = [];
    public bool WasTerminated { get; init; }
    public TerminationReason? TerminationReason { get; init; }
}

public enum TerminationReason
{
    Completed,
    Timeout,
    QuotaExceeded,
    SecurityViolation,
    UserRequested,
    SystemShutdown,
    ParentTerminated
}
```

---

### 3.5 v0.18.7e: Cross-Tenant Security

**Estimated Hours**: 10 hours
**Sprint Assignment**: Week 5-6
**Status**: Pending Development

#### 3.5.1 Objective

Ensure complete isolation between users and projects in multi-tenant deployments, preventing any data leakage or cross-contamination.

#### 3.5.2 Key Interfaces

```csharp
namespace Lexichord.Security.MultiTenant;

/// <summary>
/// Manages cross-tenant security and isolation.
/// </summary>
public interface ITenantIsolationManager
{
    /// <summary>
    /// Create isolated tenant context.
    /// </summary>
    Task<TenantContext> CreateTenantContextAsync(
        TenantId tenantId,
        TenantIsolationConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Verify tenant isolation is maintained.
    /// </summary>
    Task<IsolationVerificationResult> VerifyIsolationAsync(
        TenantId tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Check for cross-tenant access attempts.
    /// </summary>
    Task<CrossTenantCheckResult> CheckCrossTenantAccessAsync(
        TenantId sourceTenant,
        ResourceId targetResource,
        CancellationToken ct = default);

    /// <summary>
    /// Sanitize data for cross-tenant operations (if authorized).
    /// </summary>
    Task<SanitizedData> SanitizeForCrossTenantAsync(
        object data,
        TenantId sourceTenant,
        TenantId targetTenant,
        CancellationToken ct = default);
}

public record TenantIsolationConfig
{
    public IsolationLevel DataIsolation { get; init; } = IsolationLevel.Strict;
    public IsolationLevel ProcessIsolation { get; init; } = IsolationLevel.Standard;
    public IsolationLevel NetworkIsolation { get; init; } = IsolationLevel.Standard;
    public bool AllowCrossTenantSharing { get; init; } = false;
    public IReadOnlyList<TenantId>? TrustedTenants { get; init; }
    public bool EncryptAtRest { get; init; } = true;
    public bool EncryptInTransit { get; init; } = true;
    public string? DedicatedEncryptionKey { get; init; }
}

public record IsolationVerificationResult
{
    public bool IsolationMaintained { get; init; }
    public IReadOnlyList<IsolationViolation> Violations { get; init; } = [];
    public DateTimeOffset VerifiedAt { get; init; }
    public string VerificationMethod { get; init; } = "";
}

public record IsolationViolation
{
    public ViolationType Type { get; init; }
    public string Description { get; init; } = "";
    public TenantId? AffectedTenant { get; init; }
    public string? AffectedResource { get; init; }
    public ViolationSeverity Severity { get; init; }
}

public enum ViolationType
{
    DataLeakage,
    ProcessSharing,
    MemorySharing,
    FileSystemSharing,
    NetworkCrossover,
    CredentialSharing,
    ConfigurationLeakage
}
```

---

### 3.6 v0.18.7f: Isolation Verification & Testing

**Estimated Hours**: 8 hours
**Sprint Assignment**: Week 6
**Status**: Pending Development

#### 3.6.1 Objective

Implement continuous verification of isolation boundaries and automated testing framework for security validation.

---

## 4. Database Schema

```sql
-- Isolated workspaces
CREATE TABLE isolated_workspaces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    root_path TEXT NOT NULL,
    canonical_root_path TEXT NOT NULL,
    owner_id UUID NOT NULL REFERENCES users(id),
    session_id UUID NOT NULL,
    allowed_paths TEXT[] DEFAULT '{}',
    denied_paths TEXT[] DEFAULT '{}',
    read_only_paths TEXT[] DEFAULT '{}',
    permissions JSONB NOT NULL DEFAULT '{}',
    quotas JSONB NOT NULL DEFAULT '{}',
    isolation_level VARCHAR(20) NOT NULL DEFAULT 'standard',
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    destroyed_at TIMESTAMPTZ
);

CREATE INDEX idx_workspaces_session ON isolated_workspaces(session_id);
CREATE INDEX idx_workspaces_owner ON isolated_workspaces(owner_id);
CREATE INDEX idx_workspaces_status ON isolated_workspaces(status);

-- Path access log
CREATE TABLE path_access_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL REFERENCES isolated_workspaces(id),
    requested_path TEXT NOT NULL,
    canonical_path TEXT,
    operation VARCHAR(20) NOT NULL,
    validation_status VARCHAR(30) NOT NULL,
    issues JSONB DEFAULT '[]',
    escape_attempted BOOLEAN DEFAULT FALSE,
    escape_type VARCHAR(30),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_path_access_workspace ON path_access_log(workspace_id);
CREATE INDEX idx_path_access_escape ON path_access_log(escape_attempted) WHERE escape_attempted = TRUE;
CREATE INDEX idx_path_access_created ON path_access_log(created_at);

-- Privilege operations
CREATE TABLE privilege_operations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID NOT NULL,
    operation_type VARCHAR(50) NOT NULL,
    required_privileges JSONB NOT NULL,
    current_privileges JSONB NOT NULL,
    escalation_detected BOOLEAN DEFAULT FALSE,
    escalation_type VARCHAR(30),
    action_taken VARCHAR(30) NOT NULL,
    approved_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_privilege_ops_session ON privilege_operations(session_id);
CREATE INDEX idx_privilege_ops_escalation ON privilege_operations(escalation_detected)
    WHERE escalation_detected = TRUE;

-- Resource usage snapshots
CREATE TABLE resource_usage_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL REFERENCES isolated_workspaces(id),
    memory_bytes BIGINT NOT NULL,
    cpu_percent FLOAT NOT NULL,
    disk_bytes BIGINT NOT NULL,
    process_count INT NOT NULL,
    open_file_count INT NOT NULL,
    network_connection_count INT NOT NULL,
    quota_violations JSONB DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_resource_snapshots_workspace ON resource_usage_snapshots(workspace_id);
CREATE INDEX idx_resource_snapshots_created ON resource_usage_snapshots(created_at);

-- Sandbox executions
CREATE TABLE sandbox_executions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sandbox_id UUID NOT NULL,
    workspace_id UUID REFERENCES isolated_workspaces(id),
    command TEXT NOT NULL,
    command_hash VARCHAR(64) NOT NULL,
    sandbox_config JSONB NOT NULL,
    exit_code INT,
    duration_ms BIGINT,
    resource_usage JSONB,
    security_events JSONB DEFAULT '[]',
    was_terminated BOOLEAN DEFAULT FALSE,
    termination_reason VARCHAR(30),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);

CREATE INDEX idx_sandbox_exec_workspace ON sandbox_executions(workspace_id);
CREATE INDEX idx_sandbox_exec_terminated ON sandbox_executions(was_terminated)
    WHERE was_terminated = TRUE;
```

---

## 5. Success Metrics

- **Escape Attempt Detection:** 100% of known escape techniques blocked
- **Path Validation Performance:** <5ms P95 per validation
- **Privilege Escalation Detection:** 100% of escalation attempts detected
- **Resource Quota Enforcement:** <1% variance from limits
- **Sandbox Isolation:** Zero container escapes in penetration testing
- **Cross-Tenant Isolation:** Zero data leakage in multi-tenant tests

---
