# Lexichord Security & Sandboxing Roadmap (v0.18.1 - v0.18.5)

In v0.17.x, we delivered **The Intelligent Editor** — a deeply AI-integrated writing environment. In v0.18.x, we introduce **Security & Sandboxing** — comprehensive protection mechanisms that ensure AI operations are safe, auditable, and user-controlled, establishing the security foundation required for v1.0.0.

**Architectural Note:** This version introduces `Lexichord.Security` module with permission frameworks, execution sandboxing, and audit capabilities. Security becomes a first-class concern with defense-in-depth strategies protecting users from unintended AI actions.

**Total Sub-Parts:** 42 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 312 hours (~7.8 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.18.1-SEC | Permission Framework | Permission requests, grants, denials, scopes, and user consent flows | 58 |
| v0.18.2-SEC | Command Sandboxing | Terminal command approval, blocking, safe execution environments | 64 |
| v0.18.3-SEC | File System Security | Read/write/delete protections, path restrictions, sensitive file detection | 62 |
| v0.18.4-SEC | Network & API Security | Outbound request controls, API key protection, data exfiltration prevention | 60 |
| v0.18.5-SEC | Audit & Compliance | Comprehensive logging, security policies, compliance reporting | 68 |

---

## Security Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Security & Sandboxing System                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Audit & Compliance Layer (v0.18.5)                  │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │   Security   │  │    Audit     │  │  Compliance  │  │  Security  │ │ │
│  │  │   Policies   │  │    Logger    │  │   Reports    │  │  Alerts    │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                   Network & API Security (v0.18.4)                     │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │   Outbound   │  │   API Key    │  │    Data      │                  │ │
│  │  │   Controls   │  │  Protection  │  │  Exfil Guard │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                   File System Security (v0.18.3)                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │    Path      │  │  Sensitive   │  │   Delete     │                  │ │
│  │  │ Restrictions │  │  File Guard  │  │  Protection  │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Command Sandboxing (v0.18.2)                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │   Command    │  │   Approval   │  │    Safe      │  │  Rollback  │ │ │
│  │  │   Analyzer   │  │    Queue     │  │   Executor   │  │   Engine   │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Permission Framework (v0.18.1)                      │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │  Permission  │  │   Consent    │  │    Scope     │  │   Grant    │ │ │
│  │  │   Registry   │  │    Dialog    │  │   Manager    │  │   Store    │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.18.1-SEC: Permission Framework

**Goal:** Establish a comprehensive permission system that enables users to grant, deny, and scope AI capabilities with full transparency and control.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.18.1a | Permission Registry & Types | 10 |
| v0.18.1b | Permission Request Pipeline | 10 |
| v0.18.1c | User Consent Dialog System | 10 |
| v0.18.1d | Permission Scope Manager | 10 |
| v0.18.1e | Grant Persistence & Storage | 8 |
| v0.18.1f | Permission Revocation & Expiry | 6 |
| v0.18.1g | Permission Inheritance & Delegation | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Core permission types for AI operations.
/// </summary>
public enum PermissionType
{
    // File System
    FileRead,
    FileWrite,
    FileDelete,
    FileExecute,
    DirectoryCreate,
    DirectoryDelete,

    // Terminal/Command
    CommandExecute,
    CommandElevated,
    ProcessSpawn,
    ProcessKill,

    // Network
    NetworkOutbound,
    NetworkInbound,
    ApiKeyAccess,

    // System
    SystemInfoRead,
    EnvironmentModify,
    RegistryAccess,

    // Data
    ClipboardRead,
    ClipboardWrite,
    SensitiveDataAccess,

    // AI Specific
    ModelDownload,
    ModelExecute,
    AgentSpawn,
    OrchestrationControl
}

/// <summary>
/// Manages permission requests, grants, and enforcement.
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// Request permission for an operation.
    /// </summary>
    Task<PermissionResult> RequestPermissionAsync(
        PermissionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Check if permission is currently granted.
    /// </summary>
    Task<bool> HasPermissionAsync(
        PermissionType type,
        PermissionScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Revoke a previously granted permission.
    /// </summary>
    Task RevokePermissionAsync(
        PermissionGrantId grantId,
        CancellationToken ct = default);

    /// <summary>
    /// Get all active permission grants.
    /// </summary>
    Task<IReadOnlyList<PermissionGrant>> GetActiveGrantsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of permission events.
    /// </summary>
    IObservable<PermissionEvent> PermissionEvents { get; }
}

/// <summary>
/// Represents a permission request from an AI operation.
/// </summary>
public record PermissionRequest
{
    public PermissionRequestId Id { get; init; }
    public PermissionType Type { get; init; }
    public PermissionScope Scope { get; init; }
    public string Justification { get; init; } = "";
    public AgentId RequestingAgent { get; init; }
    public OperationContext Context { get; init; }
    public TimeSpan? RequestedDuration { get; init; }
    public RiskLevel EstimatedRisk { get; init; }
    public IReadOnlyList<string> AffectedResources { get; init; } = [];
    public bool AllowRemember { get; init; } = true;
}

/// <summary>
/// Defines the scope of a permission grant.
/// </summary>
public record PermissionScope
{
    public ScopeType Type { get; init; }
    public string? PathPattern { get; init; }
    public string? CommandPattern { get; init; }
    public string? HostPattern { get; init; }
    public IReadOnlyList<string>? AllowedExtensions { get; init; }
    public IReadOnlyList<string>? ExcludedPaths { get; init; }
}

public enum ScopeType
{
    Global,           // Applies everywhere
    Workspace,        // Current workspace only
    Directory,        // Specific directory and subdirs
    File,             // Specific file only
    Pattern,          // Matches a pattern
    Session,          // Current session only
    OneTime           // Single use
}

/// <summary>
/// Result of a permission request.
/// </summary>
public record PermissionResult
{
    public bool Granted { get; init; }
    public PermissionGrantId? GrantId { get; init; }
    public DenialReason? DenialReason { get; init; }
    public TimeSpan? GrantDuration { get; init; }
    public PermissionScope? GrantedScope { get; init; }
    public IReadOnlyList<string>? Conditions { get; init; }
}

public enum DenialReason
{
    UserDenied,
    PolicyViolation,
    ScopeExceeded,
    RiskTooHigh,
    RateLimited,
    Expired,
    Revoked,
    SystemRestriction
}

public enum RiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Displays consent dialogs to users for permission requests.
/// </summary>
public interface IConsentDialogService
{
    /// <summary>
    /// Show a permission request dialog to the user.
    /// </summary>
    Task<ConsentResponse> ShowPermissionDialogAsync(
        PermissionRequest request,
        ConsentDialogOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Show a batch permission request for multiple operations.
    /// </summary>
    Task<BatchConsentResponse> ShowBatchDialogAsync(
        IReadOnlyList<PermissionRequest> requests,
        CancellationToken ct = default);

    /// <summary>
    /// Show an emergency action confirmation.
    /// </summary>
    Task<bool> ShowEmergencyConfirmationAsync(
        string action,
        string consequences,
        CancellationToken ct = default);
}

public record ConsentDialogOptions
{
    public bool ShowRiskAssessment { get; init; } = true;
    public bool AllowRemember { get; init; } = true;
    public bool AllowScopeModification { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public bool RequireExplicitAction { get; init; } = true;
}

public record ConsentResponse
{
    public ConsentDecision Decision { get; init; }
    public PermissionScope? ModifiedScope { get; init; }
    public TimeSpan? GrantDuration { get; init; }
    public bool RememberDecision { get; init; }
}

public enum ConsentDecision
{
    Allow,
    AllowOnce,
    AllowForSession,
    Deny,
    DenyAlways,
    AskLater,
    Timeout
}
```

### Permission Categories & Risk Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Permission Risk Matrix                               │
├──────────────────────┬──────────┬──────────────────────────────────────────┤
│ Permission           │ Risk     │ Default Behavior                         │
├──────────────────────┼──────────┼──────────────────────────────────────────┤
│ FileRead             │ Low      │ Allow with scope limits                  │
│ FileWrite            │ Medium   │ Prompt with preview                      │
│ FileDelete           │ High     │ Always prompt, require confirmation      │
│ FileExecute          │ Critical │ Always prompt, show command              │
├──────────────────────┼──────────┼──────────────────────────────────────────┤
│ CommandExecute       │ High     │ Analyze & categorize, prompt for risky   │
│ CommandElevated      │ Critical │ Always deny without explicit enable      │
│ ProcessSpawn         │ Medium   │ Allow known processes, prompt others     │
│ ProcessKill          │ High     │ Always prompt                            │
├──────────────────────┼──────────┼──────────────────────────────────────────┤
│ NetworkOutbound      │ Medium   │ Allow known hosts, prompt others         │
│ ApiKeyAccess         │ High     │ Always prompt, mask values               │
│ SensitiveDataAccess  │ Critical │ Always prompt, audit                     │
├──────────────────────┼──────────┼──────────────────────────────────────────┤
│ ModelDownload        │ Low      │ Allow from approved sources              │
│ AgentSpawn           │ Medium   │ Allow with resource limits               │
│ OrchestrationControl │ High     │ Prompt for state changes                 │
└──────────────────────┴──────────┴──────────────────────────────────────────┘
```

---

## v0.18.2-SEC: Command Sandboxing

**Goal:** Implement a robust command execution sandbox that analyzes, approves, and safely executes terminal commands with full user visibility and control.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.18.2a | Command Parser & Analyzer | 12 |
| v0.18.2b | Risk Classification Engine | 10 |
| v0.18.2c | Approval Queue & UI | 10 |
| v0.18.2d | Safe Execution Environment | 12 |
| v0.18.2e | Command Allowlist/Blocklist | 8 |
| v0.18.2f | Rollback & Undo System | 8 |
| v0.18.2g | Command History & Replay | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Analyzes commands for risk and required permissions.
/// </summary>
public interface ICommandAnalyzer
{
    /// <summary>
    /// Analyze a command before execution.
    /// </summary>
    Task<CommandAnalysis> AnalyzeAsync(
        string command,
        CommandContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Parse a command into structured components.
    /// </summary>
    Task<ParsedCommand> ParseAsync(
        string command,
        CancellationToken ct = default);

    /// <summary>
    /// Check if command matches known dangerous patterns.
    /// </summary>
    Task<IReadOnlyList<DangerPattern>> DetectDangerPatternsAsync(
        string command,
        CancellationToken ct = default);
}

public record CommandAnalysis
{
    public ParsedCommand Parsed { get; init; }
    public RiskLevel OverallRisk { get; init; }
    public IReadOnlyList<RiskFactor> RiskFactors { get; init; } = [];
    public IReadOnlyList<PermissionType> RequiredPermissions { get; init; } = [];
    public IReadOnlyList<string> AffectedPaths { get; init; } = [];
    public IReadOnlyList<string> AffectedProcesses { get; init; } = [];
    public bool IsReversible { get; init; }
    public string? RollbackCommand { get; init; }
    public CommandCategory Category { get; init; }
    public string HumanExplanation { get; init; } = "";
}

public record ParsedCommand
{
    public string Executable { get; init; } = "";
    public IReadOnlyList<string> Arguments { get; init; } = [];
    public IReadOnlyDictionary<string, string> Environment { get; init; }
    public string? WorkingDirectory { get; init; }
    public bool UseShell { get; init; }
    public IReadOnlyList<ParsedCommand>? PipedCommands { get; init; }
    public CommandOperator? Operator { get; init; }
}

public enum CommandOperator
{
    None,
    Pipe,           // |
    And,            // &&
    Or,             // ||
    Sequence,       // ;
    Background,     // &
    Redirect,       // > or >>
    RedirectInput   // <
}

public enum CommandCategory
{
    SafeRead,           // ls, cat, head, etc.
    SafeNavigation,     // cd, pwd
    FileModification,   // touch, mkdir, cp
    FileDeletion,       // rm, rmdir
    SystemInfo,         // uname, df, ps
    ProcessControl,     // kill, pkill
    NetworkOperation,   // curl, wget, ssh
    PackageManagement,  // apt, npm, pip
    Compilation,        // gcc, make, cargo
    Scripting,          // bash, python, node
    Elevated,           // sudo, doas
    Unknown
}

public record RiskFactor
{
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public RiskLevel Severity { get; init; }
    public string Mitigation { get; init; } = "";
}

/// <summary>
/// Known dangerous command patterns.
/// </summary>
public record DangerPattern
{
    public string PatternId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Pattern { get; init; } = "";
    public RiskLevel Severity { get; init; }
    public bool BlockByDefault { get; init; }
}

/// <summary>
/// Manages the command approval workflow.
/// </summary>
public interface ICommandApprovalQueue
{
    /// <summary>
    /// Submit a command for approval.
    /// </summary>
    Task<ApprovalTicket> SubmitForApprovalAsync(
        CommandAnalysis analysis,
        ApprovalOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Wait for approval decision.
    /// </summary>
    Task<ApprovalDecision> WaitForDecisionAsync(
        ApprovalTicket ticket,
        CancellationToken ct = default);

    /// <summary>
    /// Get all pending approvals.
    /// </summary>
    Task<IReadOnlyList<PendingApproval>> GetPendingAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Approve a pending command.
    /// </summary>
    Task ApproveAsync(
        ApprovalTicket ticket,
        ApprovalModifications? modifications = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deny a pending command.
    /// </summary>
    Task DenyAsync(
        ApprovalTicket ticket,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of approval events.
    /// </summary>
    IObservable<ApprovalEvent> ApprovalEvents { get; }
}

/// <summary>
/// Executes commands in a sandboxed environment.
/// </summary>
public interface ISandboxedExecutor
{
    /// <summary>
    /// Execute a command in the sandbox.
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(
        ApprovedCommand command,
        SandboxOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Execute with real-time output streaming.
    /// </summary>
    IObservable<ExecutionOutput> ExecuteStreaming(
        ApprovedCommand command,
        SandboxOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Abort a running command.
    /// </summary>
    Task AbortAsync(
        ExecutionId executionId,
        CancellationToken ct = default);
}

public record SandboxOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public long MaxMemoryBytes { get; init; } = 512 * 1024 * 1024;
    public long MaxDiskBytes { get; init; } = 1024 * 1024 * 1024;
    public IReadOnlyList<string> AllowedPaths { get; init; } = [];
    public IReadOnlyList<string> BlockedPaths { get; init; } = [];
    public IReadOnlyList<string> AllowedEnvironment { get; init; } = [];
    public bool AllowNetwork { get; init; }
    public bool AllowProcessSpawn { get; init; }
    public bool CaptureFilesystemChanges { get; init; } = true;
}

/// <summary>
/// Manages command rollback and undo operations.
/// </summary>
public interface ICommandRollbackManager
{
    /// <summary>
    /// Create a checkpoint before command execution.
    /// </summary>
    Task<RollbackCheckpoint> CreateCheckpointAsync(
        IReadOnlyList<string> affectedPaths,
        CancellationToken ct = default);

    /// <summary>
    /// Rollback to a previous checkpoint.
    /// </summary>
    Task<RollbackResult> RollbackAsync(
        RollbackCheckpoint checkpoint,
        CancellationToken ct = default);

    /// <summary>
    /// Generate undo command for an executed command.
    /// </summary>
    Task<string?> GenerateUndoCommandAsync(
        ExecutionRecord record,
        CancellationToken ct = default);

    /// <summary>
    /// Get rollback history.
    /// </summary>
    Task<IReadOnlyList<RollbackRecord>> GetHistoryAsync(
        CancellationToken ct = default);
}
```

### Dangerous Command Patterns

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Dangerous Command Pattern Database                        │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  CRITICAL - Always Block:                                                    │
│  ├── rm -rf /                    # System destruction                        │
│  ├── rm -rf /*                   # System destruction                        │
│  ├── dd if=/dev/zero of=/dev/sda # Disk wipe                                │
│  ├── mkfs.* /dev/sd*             # Format disk                              │
│  ├── :(){:|:&};:                 # Fork bomb                                │
│  ├── chmod -R 777 /              # Security destruction                      │
│  └── > /etc/passwd               # Credential destruction                   │
│                                                                              │
│  HIGH RISK - Always Prompt:                                                  │
│  ├── rm -rf *                    # Recursive delete in current dir          │
│  ├── rm -rf ~/*                  # Home directory wipe                      │
│  ├── sudo *                      # Elevated commands                        │
│  ├── chmod -R *                  # Recursive permission change              │
│  ├── chown -R *                  # Recursive ownership change               │
│  ├── kill -9 *                   # Force kill processes                     │
│  ├── pkill *                     # Pattern kill                             │
│  └── curl * | bash               # Remote code execution                    │
│                                                                              │
│  MEDIUM RISK - Prompt with Context:                                          │
│  ├── rm *                        # Delete files                             │
│  ├── mv * /dev/null              # Discard files                            │
│  ├── git push --force            # Destructive git operation                │
│  ├── git reset --hard            # Discard changes                          │
│  ├── npm install -g              # Global package install                   │
│  └── pip install --user          # User package install                     │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.18.3-SEC: File System Security

**Goal:** Implement comprehensive file system protections that prevent unauthorized access, detect sensitive files, and protect against accidental or malicious deletion.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.18.3a | Path Restriction Engine | 12 |
| v0.18.3b | Sensitive File Detection | 10 |
| v0.18.3c | Delete Protection & Trash | 10 |
| v0.18.3d | File Access Audit Trail | 10 |
| v0.18.3e | Workspace Boundaries | 10 |
| v0.18.3f | Backup Before Modify | 6 |
| v0.18.3g | File Type Restrictions | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Enforces file system access restrictions.
/// </summary>
public interface IPathRestrictionEngine
{
    /// <summary>
    /// Check if a path is accessible for an operation.
    /// </summary>
    Task<PathAccessResult> CheckAccessAsync(
        string path,
        FileOperation operation,
        OperationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Get effective restrictions for a path.
    /// </summary>
    Task<PathRestrictions> GetRestrictionsAsync(
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Add a path restriction rule.
    /// </summary>
    Task AddRestrictionAsync(
        PathRestrictionRule rule,
        CancellationToken ct = default);

    /// <summary>
    /// Get all restriction rules.
    /// </summary>
    Task<IReadOnlyList<PathRestrictionRule>> GetRulesAsync(
        CancellationToken ct = default);
}

public enum FileOperation
{
    Read,
    Write,
    Delete,
    Create,
    Rename,
    Move,
    Copy,
    Execute,
    ListDirectory,
    GetMetadata,
    SetPermissions
}

public record PathAccessResult
{
    public bool Allowed { get; init; }
    public AccessDenialReason? DenialReason { get; init; }
    public PathRestrictionRule? MatchedRule { get; init; }
    public PermissionType? RequiredPermission { get; init; }
    public string? Explanation { get; init; }
}

public enum AccessDenialReason
{
    OutsideWorkspace,
    SystemPath,
    SensitiveFile,
    ExplicitBlock,
    NoPermission,
    OperationNotAllowed,
    FileTypeForbidden,
    PatternMatch
}

public record PathRestrictionRule
{
    public RestrictionRuleId Id { get; init; }
    public string Name { get; init; } = "";
    public string PathPattern { get; init; } = "";
    public IReadOnlyList<FileOperation> BlockedOperations { get; init; } = [];
    public IReadOnlyList<FileOperation> AllowedOperations { get; init; } = [];
    public RuleSource Source { get; init; }
    public int Priority { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public enum RuleSource
{
    System,      // Built-in system rules
    Policy,      // Organization policy
    User,        // User-defined
    Workspace,   // Workspace-specific
    Agent        // Agent-specific
}

/// <summary>
/// Detects sensitive files and content.
/// </summary>
public interface ISensitiveFileDetector
{
    /// <summary>
    /// Check if a file is sensitive.
    /// </summary>
    Task<SensitivityAssessment> AssessAsync(
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Scan content for sensitive patterns.
    /// </summary>
    Task<IReadOnlyList<SensitiveMatch>> ScanContentAsync(
        string content,
        CancellationToken ct = default);

    /// <summary>
    /// Get known sensitive file patterns.
    /// </summary>
    Task<IReadOnlyList<SensitivePattern>> GetPatternsAsync(
        CancellationToken ct = default);
}

public record SensitivityAssessment
{
    public bool IsSensitive { get; init; }
    public SensitivityLevel Level { get; init; }
    public IReadOnlyList<SensitivityReason> Reasons { get; init; } = [];
    public IReadOnlyList<string> DetectedPatterns { get; init; } = [];
    public string Recommendation { get; init; } = "";
}

public enum SensitivityLevel
{
    None,
    Low,        // May contain personal info
    Medium,     // Contains credentials or keys
    High,       // Contains secrets or private keys
    Critical    // System security files
}

public record SensitivityReason
{
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public string MatchedPattern { get; init; } = "";
    public SensitivityLevel Severity { get; init; }
}

/// <summary>
/// Protects against file deletion with trash and recovery.
/// </summary>
public interface IDeleteProtectionService
{
    /// <summary>
    /// Move file to trash instead of permanent deletion.
    /// </summary>
    Task<TrashResult> MoveToTrashAsync(
        string path,
        DeleteContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Restore file from trash.
    /// </summary>
    Task<RestoreResult> RestoreFromTrashAsync(
        TrashItemId itemId,
        CancellationToken ct = default);

    /// <summary>
    /// List items in trash.
    /// </summary>
    Task<IReadOnlyList<TrashItem>> GetTrashContentsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Check if permanent deletion is allowed.
    /// </summary>
    Task<bool> CanPermanentlyDeleteAsync(
        string path,
        OperationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Empty trash (requires explicit permission).
    /// </summary>
    Task EmptyTrashAsync(
        TrashEmptyOptions options,
        CancellationToken ct = default);
}

public record TrashItem
{
    public TrashItemId Id { get; init; }
    public string OriginalPath { get; init; } = "";
    public string TrashPath { get; init; } = "";
    public DateTime DeletedAt { get; init; }
    public AgentId? DeletedBy { get; init; }
    public string? DeleteReason { get; init; }
    public long SizeBytes { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Creates backups before file modifications.
/// </summary>
public interface IBackupBeforeModifyService
{
    /// <summary>
    /// Create backup before modifying a file.
    /// </summary>
    Task<FileBackup> BackupAsync(
        string path,
        BackupOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Restore from backup.
    /// </summary>
    Task RestoreAsync(
        FileBackupId backupId,
        CancellationToken ct = default);

    /// <summary>
    /// List available backups for a file.
    /// </summary>
    Task<IReadOnlyList<FileBackup>> GetBackupsAsync(
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Configure auto-backup settings.
    /// </summary>
    Task ConfigureAutoBackupAsync(
        AutoBackupSettings settings,
        CancellationToken ct = default);
}
```

### Sensitive File Patterns

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Sensitive File Detection Patterns                        │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  CRITICAL - Always Protect:                                                  │
│  ├── .env, .env.*                   # Environment variables                 │
│  ├── *.pem, *.key, *.p12            # Private keys                          │
│  ├── id_rsa, id_ed25519             # SSH private keys                      │
│  ├── .aws/credentials               # AWS credentials                       │
│  ├── .gcloud/*.json                 # GCP service accounts                  │
│  ├── .kube/config                   # Kubernetes config                     │
│  ├── *.keystore, *.jks              # Java keystores                        │
│  └── *password*, *secret*           # Files with sensitive names            │
│                                                                              │
│  HIGH - Require Confirmation:                                                │
│  ├── .git/config                    # Git configuration                     │
│  ├── .npmrc, .pypirc                # Package manager credentials           │
│  ├── .docker/config.json            # Docker credentials                    │
│  ├── .netrc                         # FTP/HTTP credentials                  │
│  ├── .pgpass                        # PostgreSQL passwords                  │
│  └── wp-config.php                  # WordPress config                      │
│                                                                              │
│  CONTENT PATTERNS - Scan for:                                                │
│  ├── API[_-]?KEY.*=.*               # API keys                              │
│  ├── (password|passwd|pwd)\s*[:=]   # Passwords                             │
│  ├── (secret|token)\s*[:=]          # Secrets/tokens                        │
│  ├── -----BEGIN.*PRIVATE KEY-----   # Private keys in content               │
│  ├── AKIA[0-9A-Z]{16}               # AWS access key IDs                    │
│  └── ghp_[a-zA-Z0-9]{36}            # GitHub personal access tokens         │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.18.4-SEC: Network & API Security

**Goal:** Implement controls for outbound network requests, protect API keys and credentials, and prevent data exfiltration.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.18.4a | Outbound Request Controls | 12 |
| v0.18.4b | API Key Vault & Protection | 12 |
| v0.18.4c | Data Exfiltration Prevention | 10 |
| v0.18.4d | Host Allowlist/Blocklist | 8 |
| v0.18.4e | Request Inspection & Logging | 10 |
| v0.18.4f | Certificate & TLS Validation | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Controls outbound network requests from AI operations.
/// </summary>
public interface IOutboundRequestController
{
    /// <summary>
    /// Check if an outbound request is allowed.
    /// </summary>
    Task<RequestDecision> EvaluateRequestAsync(
        OutboundRequest request,
        OperationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Execute an approved outbound request.
    /// </summary>
    Task<HttpResponseMessage> ExecuteAsync(
        ApprovedRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get request statistics.
    /// </summary>
    Task<RequestStatistics> GetStatisticsAsync(
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of request events.
    /// </summary>
    IObservable<RequestEvent> RequestEvents { get; }
}

public record OutboundRequest
{
    public string Url { get; init; } = "";
    public HttpMethod Method { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
    public string? Body { get; init; }
    public AgentId RequestingAgent { get; init; }
    public string Purpose { get; init; } = "";
}

public record RequestDecision
{
    public bool Allowed { get; init; }
    public RequestDenialReason? DenialReason { get; init; }
    public HostRule? MatchedRule { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public bool RequiresApproval { get; init; }
    public IReadOnlyList<string>? RedactedHeaders { get; init; }
}

public enum RequestDenialReason
{
    BlockedHost,
    UnknownHost,
    SensitiveDataDetected,
    RateLimited,
    MethodNotAllowed,
    ContentTypeBlocked,
    CertificateInvalid,
    NoPermission
}

/// <summary>
/// Secure storage and management of API keys.
/// </summary>
public interface IApiKeyVault
{
    /// <summary>
    /// Store an API key securely.
    /// </summary>
    Task<ApiKeyId> StoreAsync(
        ApiKeyDefinition definition,
        string keyValue,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieve an API key for use (returns masked reference).
    /// </summary>
    Task<ApiKeyReference> GetReferenceAsync(
        ApiKeyId keyId,
        OperationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Use an API key in a request (injects securely).
    /// </summary>
    Task<HttpRequestMessage> InjectKeyAsync(
        HttpRequestMessage request,
        ApiKeyId keyId,
        CancellationToken ct = default);

    /// <summary>
    /// Rotate an API key.
    /// </summary>
    Task RotateAsync(
        ApiKeyId keyId,
        string newValue,
        CancellationToken ct = default);

    /// <summary>
    /// Revoke an API key.
    /// </summary>
    Task RevokeAsync(
        ApiKeyId keyId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// List all stored keys (metadata only).
    /// </summary>
    Task<IReadOnlyList<ApiKeyMetadata>> ListAsync(
        CancellationToken ct = default);
}

public record ApiKeyDefinition
{
    public string Name { get; init; } = "";
    public string Service { get; init; } = "";
    public ApiKeyLocation Location { get; init; }
    public string HeaderOrParamName { get; init; } = "";
    public IReadOnlyList<string>? AllowedHosts { get; init; }
    public IReadOnlyList<AgentId>? AllowedAgents { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public enum ApiKeyLocation
{
    Header,
    QueryParameter,
    BasicAuth,
    BearerToken,
    Custom
}

/// <summary>
/// Prevents data exfiltration through network requests.
/// </summary>
public interface IDataExfiltrationGuard
{
    /// <summary>
    /// Scan outbound data for sensitive information.
    /// </summary>
    Task<ExfiltrationScan> ScanOutboundDataAsync(
        string data,
        string destinationHost,
        CancellationToken ct = default);

    /// <summary>
    /// Check if data transfer is allowed.
    /// </summary>
    Task<bool> IsTransferAllowedAsync(
        DataTransferRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get data transfer policies.
    /// </summary>
    Task<DataTransferPolicy> GetPolicyAsync(
        CancellationToken ct = default);
}

public record ExfiltrationScan
{
    public bool ContainsSensitiveData { get; init; }
    public IReadOnlyList<DetectedSensitiveData> Detections { get; init; } = [];
    public ExfiltrationRisk RiskLevel { get; init; }
    public bool ShouldBlock { get; init; }
    public string? Recommendation { get; init; }
}

public record DetectedSensitiveData
{
    public SensitiveDataType Type { get; init; }
    public string Pattern { get; init; } = "";
    public int Position { get; init; }
    public int Length { get; init; }
    public string MaskedPreview { get; init; } = "";
}

public enum SensitiveDataType
{
    ApiKey,
    Password,
    PrivateKey,
    PersonalInfo,
    CreditCard,
    SocialSecurity,
    HealthInfo,
    SourceCode,
    InternalPath,
    Credential
}

/// <summary>
/// Manages host allowlists and blocklists.
/// </summary>
public interface IHostPolicyManager
{
    /// <summary>
    /// Check if a host is allowed.
    /// </summary>
    Task<HostCheckResult> CheckHostAsync(
        string host,
        CancellationToken ct = default);

    /// <summary>
    /// Add host to allowlist.
    /// </summary>
    Task AllowHostAsync(
        string hostPattern,
        HostAllowOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Add host to blocklist.
    /// </summary>
    Task BlockHostAsync(
        string hostPattern,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Get all host rules.
    /// </summary>
    Task<IReadOnlyList<HostRule>> GetRulesAsync(
        CancellationToken ct = default);
}

public record HostRule
{
    public HostRuleId Id { get; init; }
    public string HostPattern { get; init; } = "";
    public HostRuleType Type { get; init; }
    public string? Reason { get; init; }
    public RuleSource Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public enum HostRuleType
{
    Allow,
    Block,
    RequireApproval,
    AuditOnly
}
```

---

## v0.18.5-SEC: Audit & Compliance

**Goal:** Implement comprehensive audit logging, security policies, and compliance reporting to ensure accountability and meet enterprise security requirements.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.18.5a | Security Audit Logger | 12 |
| v0.18.5b | Policy Engine & Enforcement | 14 |
| v0.18.5c | Compliance Report Generator | 10 |
| v0.18.5d | Security Alerts & Notifications | 10 |
| v0.18.5e | Audit Trail Viewer | 8 |
| v0.18.5f | Policy Templates & Import | 6 |
| v0.18.5g | Security Dashboard | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Comprehensive security audit logging.
/// </summary>
public interface ISecurityAuditLogger
{
    /// <summary>
    /// Log a security event.
    /// </summary>
    Task LogEventAsync(
        SecurityEvent securityEvent,
        CancellationToken ct = default);

    /// <summary>
    /// Query audit logs.
    /// </summary>
    Task<AuditQueryResult> QueryAsync(
        AuditQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Export audit logs.
    /// </summary>
    Task<Stream> ExportAsync(
        AuditExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Real-time audit event stream.
    /// </summary>
    IObservable<SecurityEvent> AuditStream { get; }
}

public record SecurityEvent
{
    public SecurityEventId Id { get; init; }
    public DateTime Timestamp { get; init; }
    public SecurityEventType Type { get; init; }
    public SecuritySeverity Severity { get; init; }
    public AgentId? Agent { get; init; }
    public UserId? User { get; init; }
    public string Action { get; init; } = "";
    public string? Resource { get; init; }
    public SecurityOutcome Outcome { get; init; }
    public string? Details { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; }
    public string? CorrelationId { get; init; }
    public string? SessionId { get; init; }
}

public enum SecurityEventType
{
    // Permission Events
    PermissionRequested,
    PermissionGranted,
    PermissionDenied,
    PermissionRevoked,

    // Command Events
    CommandAnalyzed,
    CommandApproved,
    CommandBlocked,
    CommandExecuted,
    CommandFailed,

    // File Events
    SensitiveFileAccessed,
    FileDeleted,
    FileRestored,
    BackupCreated,

    // Network Events
    OutboundRequestMade,
    OutboundRequestBlocked,
    ApiKeyUsed,
    DataExfiltrationAttempt,

    // Policy Events
    PolicyViolation,
    PolicyUpdated,
    PolicyOverride,

    // Authentication Events
    SessionStarted,
    SessionEnded,
    AuthenticationFailed,

    // System Events
    SecurityConfigChanged,
    AlertTriggered,
    AuditExported
}

public enum SecuritySeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum SecurityOutcome
{
    Success,
    Failure,
    Blocked,
    Warning,
    Pending
}

/// <summary>
/// Security policy engine for enforcement.
/// </summary>
public interface ISecurityPolicyEngine
{
    /// <summary>
    /// Evaluate an action against policies.
    /// </summary>
    Task<PolicyEvaluation> EvaluateAsync(
        PolicyEvaluationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get active policies.
    /// </summary>
    Task<IReadOnlyList<SecurityPolicy>> GetPoliciesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Add or update a policy.
    /// </summary>
    Task<PolicyId> UpsertPolicyAsync(
        SecurityPolicy policy,
        CancellationToken ct = default);

    /// <summary>
    /// Import policies from template.
    /// </summary>
    Task<ImportResult> ImportPoliciesAsync(
        PolicyTemplate template,
        CancellationToken ct = default);

    /// <summary>
    /// Validate policy syntax.
    /// </summary>
    Task<ValidationResult> ValidatePolicyAsync(
        SecurityPolicy policy,
        CancellationToken ct = default);
}

public record SecurityPolicy
{
    public PolicyId Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public PolicyType Type { get; init; }
    public int Priority { get; init; }
    public IReadOnlyList<PolicyCondition> Conditions { get; init; } = [];
    public PolicyAction Action { get; init; }
    public bool IsEnabled { get; init; } = true;
    public IReadOnlyList<string>? AppliesTo { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveUntil { get; init; }
}

public enum PolicyType
{
    Permission,
    Command,
    FileAccess,
    Network,
    DataProtection,
    RateLimit,
    ResourceUsage,
    Audit
}

public record PolicyCondition
{
    public string Field { get; init; } = "";
    public ConditionOperator Operator { get; init; }
    public object Value { get; init; }
    public bool Negate { get; init; }
}

public enum ConditionOperator
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    EndsWith,
    Matches,
    In,
    NotIn,
    GreaterThan,
    LessThan,
    Between
}

public enum PolicyAction
{
    Allow,
    Deny,
    RequireApproval,
    Audit,
    Warn,
    RateLimit,
    Quarantine
}

/// <summary>
/// Generates compliance and security reports.
/// </summary>
public interface IComplianceReportGenerator
{
    /// <summary>
    /// Generate a compliance report.
    /// </summary>
    Task<ComplianceReport> GenerateReportAsync(
        ComplianceReportRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get available report types.
    /// </summary>
    Task<IReadOnlyList<ReportType>> GetReportTypesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Schedule recurring reports.
    /// </summary>
    Task<ReportScheduleId> ScheduleReportAsync(
        ReportSchedule schedule,
        CancellationToken ct = default);

    /// <summary>
    /// Export report in various formats.
    /// </summary>
    Task<Stream> ExportReportAsync(
        ComplianceReport report,
        ReportExportFormat format,
        CancellationToken ct = default);
}

public record ComplianceReport
{
    public ReportId Id { get; init; }
    public string Title { get; init; } = "";
    public ReportType Type { get; init; }
    public DateTime GeneratedAt { get; init; }
    public DateRange Period { get; init; }
    public ComplianceSummary Summary { get; init; }
    public IReadOnlyList<ComplianceFinding> Findings { get; init; } = [];
    public IReadOnlyList<SecurityMetric> Metrics { get; init; } = [];
    public IReadOnlyList<Recommendation> Recommendations { get; init; } = [];
}

public record ComplianceSummary
{
    public int TotalEvents { get; init; }
    public int PolicyViolations { get; init; }
    public int BlockedActions { get; init; }
    public int PermissionRequests { get; init; }
    public int HighSeverityEvents { get; init; }
    public double ComplianceScore { get; init; }
    public IReadOnlyDictionary<string, int> EventsByType { get; init; }
}

/// <summary>
/// Security alerts and notifications.
/// </summary>
public interface ISecurityAlertService
{
    /// <summary>
    /// Create a security alert.
    /// </summary>
    Task<SecurityAlert> CreateAlertAsync(
        AlertDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Get active alerts.
    /// </summary>
    Task<IReadOnlyList<SecurityAlert>> GetActiveAlertsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Acknowledge an alert.
    /// </summary>
    Task AcknowledgeAsync(
        AlertId alertId,
        string acknowledgedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Configure alert rules.
    /// </summary>
    Task<AlertRuleId> ConfigureRuleAsync(
        AlertRule rule,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribe to alert notifications.
    /// </summary>
    IObservable<SecurityAlert> Alerts { get; }
}

public record SecurityAlert
{
    public AlertId Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public AlertSeverity Severity { get; init; }
    public AlertStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
    public IReadOnlyList<SecurityEventId> RelatedEvents { get; init; } = [];
    public IReadOnlyDictionary<string, object> Context { get; init; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Investigating,
    Resolved,
    FalsePositive
}
```

---

## PostgreSQL Schema

```sql
-- Permission Management
CREATE TABLE security.permission_grants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_type VARCHAR(50) NOT NULL,
    scope_type VARCHAR(30) NOT NULL,
    scope_pattern TEXT,
    granted_to_agent UUID REFERENCES agents.agents(id),
    granted_by_user UUID REFERENCES users.users(id),
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    revoke_reason TEXT,
    conditions JSONB DEFAULT '{}',
    metadata JSONB DEFAULT '{}'
);

CREATE INDEX idx_permission_grants_agent ON security.permission_grants(granted_to_agent);
CREATE INDEX idx_permission_grants_type ON security.permission_grants(permission_type);
CREATE INDEX idx_permission_grants_active ON security.permission_grants(is_revoked, expires_at);

-- Command Execution Audit
CREATE TABLE security.command_executions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    command_text TEXT NOT NULL,
    command_hash VARCHAR(64) NOT NULL,
    parsed_command JSONB NOT NULL,
    risk_level VARCHAR(20) NOT NULL,
    risk_factors JSONB DEFAULT '[]',
    required_permissions TEXT[] DEFAULT '{}',
    approval_status VARCHAR(20) NOT NULL,
    approved_by UUID REFERENCES users.users(id),
    approved_at TIMESTAMPTZ,
    executed_at TIMESTAMPTZ,
    execution_result VARCHAR(20),
    exit_code INTEGER,
    stdout_preview TEXT,
    stderr_preview TEXT,
    affected_paths TEXT[] DEFAULT '{}',
    rollback_available BOOLEAN DEFAULT FALSE,
    rollback_command TEXT,
    agent_id UUID REFERENCES agents.agents(id),
    session_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_command_executions_agent ON security.command_executions(agent_id);
CREATE INDEX idx_command_executions_risk ON security.command_executions(risk_level);
CREATE INDEX idx_command_executions_time ON security.command_executions(created_at DESC);

-- File Access Audit
CREATE TABLE security.file_access_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path TEXT NOT NULL,
    operation VARCHAR(30) NOT NULL,
    agent_id UUID REFERENCES agents.agents(id),
    user_id UUID REFERENCES users.users(id),
    access_granted BOOLEAN NOT NULL,
    denial_reason VARCHAR(50),
    matched_rule UUID,
    sensitivity_level VARCHAR(20),
    backup_created BOOLEAN DEFAULT FALSE,
    backup_id UUID,
    accessed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    metadata JSONB DEFAULT '{}'
);

CREATE INDEX idx_file_access_path ON security.file_access_log(file_path);
CREATE INDEX idx_file_access_agent ON security.file_access_log(agent_id);
CREATE INDEX idx_file_access_time ON security.file_access_log(accessed_at DESC);

-- API Key Vault
CREATE TABLE security.api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    service VARCHAR(100) NOT NULL,
    key_encrypted BYTEA NOT NULL,
    key_hash VARCHAR(64) NOT NULL,
    location_type VARCHAR(30) NOT NULL,
    header_or_param_name VARCHAR(100),
    allowed_hosts TEXT[] DEFAULT '{}',
    allowed_agents UUID[] DEFAULT '{}',
    created_by UUID REFERENCES users.users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    last_used_at TIMESTAMPTZ,
    use_count INTEGER DEFAULT 0,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    revoke_reason TEXT
);

CREATE INDEX idx_api_keys_service ON security.api_keys(service);
CREATE INDEX idx_api_keys_active ON security.api_keys(is_revoked, expires_at);

-- Security Policies
CREATE TABLE security.policies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    policy_type VARCHAR(30) NOT NULL,
    priority INTEGER DEFAULT 0,
    conditions JSONB NOT NULL DEFAULT '[]',
    action VARCHAR(30) NOT NULL,
    is_enabled BOOLEAN DEFAULT TRUE,
    applies_to TEXT[] DEFAULT '{}',
    effective_from TIMESTAMPTZ,
    effective_until TIMESTAMPTZ,
    created_by UUID REFERENCES users.users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

CREATE INDEX idx_policies_type ON security.policies(policy_type);
CREATE INDEX idx_policies_enabled ON security.policies(is_enabled);
CREATE INDEX idx_policies_priority ON security.policies(priority DESC);

-- Security Audit Log
CREATE TABLE security.audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(50) NOT NULL,
    severity VARCHAR(20) NOT NULL,
    agent_id UUID,
    user_id UUID,
    action TEXT NOT NULL,
    resource TEXT,
    outcome VARCHAR(20) NOT NULL,
    details TEXT,
    metadata JSONB DEFAULT '{}',
    correlation_id VARCHAR(100),
    session_id UUID,
    source_ip INET,
    user_agent TEXT,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (timestamp);

CREATE INDEX idx_audit_log_type ON security.audit_log(event_type);
CREATE INDEX idx_audit_log_severity ON security.audit_log(severity);
CREATE INDEX idx_audit_log_agent ON security.audit_log(agent_id);
CREATE INDEX idx_audit_log_time ON security.audit_log(timestamp DESC);
CREATE INDEX idx_audit_log_correlation ON security.audit_log(correlation_id);

-- Security Alerts
CREATE TABLE security.alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(500) NOT NULL,
    description TEXT,
    severity VARCHAR(20) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'active',
    rule_id UUID REFERENCES security.alert_rules(id),
    related_events UUID[] DEFAULT '{}',
    context JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    acknowledged_at TIMESTAMPTZ,
    acknowledged_by UUID REFERENCES users.users(id),
    resolved_at TIMESTAMPTZ,
    resolved_by UUID,
    resolution_notes TEXT
);

CREATE INDEX idx_alerts_status ON security.alerts(status);
CREATE INDEX idx_alerts_severity ON security.alerts(severity);
CREATE INDEX idx_alerts_created ON security.alerts(created_at DESC);
```

---

## MediatR Events

```csharp
// Permission Events
public record PermissionRequestedEvent(
    PermissionRequest Request,
    OperationContext Context
) : INotification;

public record PermissionGrantedEvent(
    PermissionGrant Grant,
    ConsentResponse Response
) : INotification;

public record PermissionDeniedEvent(
    PermissionRequest Request,
    DenialReason Reason
) : INotification;

// Command Events
public record CommandSubmittedForApprovalEvent(
    CommandAnalysis Analysis,
    ApprovalTicket Ticket
) : INotification;

public record CommandApprovedEvent(
    ApprovalTicket Ticket,
    UserId ApprovedBy
) : INotification;

public record CommandBlockedEvent(
    CommandAnalysis Analysis,
    IReadOnlyList<DangerPattern> MatchedPatterns
) : INotification;

public record CommandExecutedEvent(
    ExecutionRecord Record,
    ExecutionResult Result
) : INotification;

// File Security Events
public record SensitiveFileAccessAttemptEvent(
    string Path,
    FileOperation Operation,
    SensitivityAssessment Assessment
) : INotification;

public record FileDeletedEvent(
    string Path,
    TrashItem? TrashItem,
    bool Permanent
) : INotification;

// Network Events
public record OutboundRequestBlockedEvent(
    OutboundRequest Request,
    RequestDenialReason Reason
) : INotification;

public record DataExfiltrationAttemptEvent(
    ExfiltrationScan Scan,
    string DestinationHost
) : INotification;

// Policy Events
public record PolicyViolationEvent(
    PolicyEvaluation Evaluation,
    SecurityPolicy Policy
) : INotification;

public record SecurityAlertCreatedEvent(
    SecurityAlert Alert
) : INotification;
```

---

## License Gating

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:----:|:---------:|:-----:|:----------:|
| Basic Permissions | ✓ | ✓ | ✓ | ✓ |
| Command Analysis | ✓ | ✓ | ✓ | ✓ |
| Delete Protection | ✓ | ✓ | ✓ | ✓ |
| File Backups | 7 days | 30 days | 90 days | Unlimited |
| API Key Vault | 5 keys | 25 keys | 100 keys | Unlimited |
| Custom Policies | - | 5 | 50 | Unlimited |
| Audit Log Retention | 7 days | 30 days | 1 year | Unlimited |
| Compliance Reports | - | - | ✓ | ✓ |
| Security Dashboard | - | - | ✓ | ✓ |
| Policy Templates | - | - | - | ✓ |
| SIEM Integration | - | - | - | ✓ |
| Custom Alert Rules | - | - | 10 | Unlimited |

---

## Dependencies

```
v0.18.1-SEC (Permission Framework)
    └── Core Platform (v0.1.x-v0.17.x)

v0.18.2-SEC (Command Sandboxing)
    └── v0.18.1-SEC (Permission Framework)

v0.18.3-SEC (File System Security)
    ├── v0.18.1-SEC (Permission Framework)
    └── v0.18.2-SEC (Command Sandboxing)

v0.18.4-SEC (Network & API Security)
    └── v0.18.1-SEC (Permission Framework)

v0.18.5-SEC (Audit & Compliance)
    ├── v0.18.1-SEC (Permission Framework)
    ├── v0.18.2-SEC (Command Sandboxing)
    ├── v0.18.3-SEC (File System Security)
    └── v0.18.4-SEC (Network & API Security)
```

---

## Performance Targets

| Metric | Target | Critical Threshold |
|:-------|:-------|:-------------------|
| Permission check latency | < 5ms | < 20ms |
| Command analysis time | < 100ms | < 500ms |
| Sensitive file detection | < 50ms | < 200ms |
| Outbound request evaluation | < 10ms | < 50ms |
| Audit log write | < 5ms | < 20ms |
| Policy evaluation | < 20ms | < 100ms |
| Alert generation | < 50ms | < 200ms |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|:-----|:------------|:-------|:-----------|
| False positive command blocks | Medium | High | Tunable thresholds, easy override |
| Permission fatigue | Medium | Medium | Smart grouping, remember decisions |
| Performance overhead | Low | Medium | Async evaluation, caching |
| Bypass attempts | Low | Critical | Defense in depth, audit all bypasses |
| Key exposure | Low | Critical | Encryption at rest, access logging |
| Audit log tampering | Low | High | Immutable logging, checksums |
