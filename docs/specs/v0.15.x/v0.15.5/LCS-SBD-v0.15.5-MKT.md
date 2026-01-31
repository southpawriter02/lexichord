# LCS-SBD-v0.15.5-MKT: Scope Overview — Enterprise Extensions

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-v0.15.5-MKT                                          |
| **Version**      | v0.15.5                                                      |
| **Codename**     | Enterprise Extensions (MKT Phase 2)                          |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Lead Architect                                               |
| **Depends On**   | v0.15.3-MKT (Marketplace), v0.15.1-MKT (Plugin Architecture), v0.11.1-SEC (Authorization), v0.11.2-SEC (Audit), v0.11.3-SEC (Encryption) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.15.5-MKT** delivers **Enterprise Extensions** — a comprehensive suite of enterprise-grade features enabling organizations to deploy LexiChord in regulated, high-governance environments. While v0.15.3 provides the Marketplace foundation, v0.15.5 adds the controls, compliance tools, and visibility features required for enterprise adoption.

This enables:
- **Private Registries:** Organizations host their own plugin repositories with complete control
- **Approval Workflows:** Multi-step governance processes for plugin deployment and usage
- **Compliance Scanning:** Automated security vulnerability and regulatory compliance detection
- **Governance Policies:** Define and enforce organizational plugin usage rules
- **Usage Analytics:** Comprehensive audit trails and usage metrics for compliance and optimization
- **Admin Console:** Centralized management interface for enterprise operations

### 1.2 Business Value

- **Enterprise Readiness:** Enable Fortune 500 deployments with compliance requirements.
- **Risk Mitigation:** Compliance scanning and policy enforcement reduce security exposure.
- **Operational Visibility:** Usage analytics provide ROI measurement and optimization opportunities.
- **Regulatory Compliance:** Audit logging and governance tools satisfy HIPAA, SOC2, ISO27001 requirements.
- **Governance Control:** Organizations enforce standards without technical dependencies on LexiChord core team.
- **Multi-Tenancy Support:** Private registries enable managed SaaS deployments for enterprise customers.

### 1.3 Success Criteria

1. Private Registry supports >1000 plugins with sub-second search performance.
2. Approval Workflows support 3+ approval chain levels with conditional routing.
3. Compliance Scanner detects >95% of OWASP Top 10 vulnerabilities and major CVEs.
4. Governance Policies support role-based, time-based, and context-based constraints.
5. Usage Analytics capture all plugin operations with <5% overhead.
6. Admin Console UI achieves <2s page load time for all views.
7. Enterprise tier adoption reaches 15+ customers within 6 months.

---

## 2. Relationship to Existing v0.15.x

The existing v0.15.x roadmap covers **Marketplace** (v0.15.3), **Plugin Architecture** (v0.15.1), and **Governance** (v0.15.2). Enterprise Extensions integrates by:

- **Marketplace Foundation:** Builds on plugin discovery and distribution.
- **Plugin Architecture:** Enforces policies at plugin boundary/execution.
- **Governance Policies:** Extend v0.15.2 governance into enterprise compliance domains.
- **Security Subsystems:** Integrate v0.11.x (Authorization, Audit, Encryption) for compliance.

---

## 3. Key Deliverables

### 3.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.15.5e | Private Registry | Organization-hosted plugin repository with RBAC | 12 |
| v0.15.5f | Approval Workflows | Multi-step plugin deployment governance | 10 |
| v0.15.5g | Compliance Scanner | Security vulnerability and regulatory scanning | 10 |
| v0.15.5h | Governance Policies | Enforcement engine for organizational policies | 10 |
| v0.15.5i | Usage Analytics | Comprehensive audit logging and metrics | 6 |
| v0.15.5j | Admin Console UI | Enterprise dashboard and management interface | 4 |
| **Total** | | | **52 hours** |

### 3.2 Key Interfaces

```csharp
/// <summary>
/// Manages private plugin registries for organizations.
/// </summary>
public interface IPrivateRegistry
{
    /// <summary>
    /// Creates a new private registry for an organization.
    /// </summary>
    /// <param name="registryInfo">Registry metadata and configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created registry with generated ID.</returns>
    Task<RegistryInfo> CreateRegistryAsync(
        RegistryInfo registryInfo,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes a plugin version to the registry.
    /// </summary>
    /// <param name="registryId">Target registry ID.</param>
    /// <param name="item">Plugin item to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Published registry item with metadata.</returns>
    Task<RegistryItem> PublishItemAsync(
        Guid registryId,
        RegistryItem item,
        CancellationToken ct = default);

    /// <summary>
    /// Searches registry items with filters and pagination.
    /// </summary>
    /// <param name="registryId">Registry to search.</param>
    /// <param name="query">Search query.</param>
    /// <param name="filters">Optional search filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated search results.</returns>
    Task<SearchResult<RegistryItem>> SearchAsync(
        Guid registryId,
        string query,
        SearchFilters? filters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Manages permissions for registry access and operations.
    /// </summary>
    /// <param name="registryId">Registry ID.</param>
    /// <param name="permissions">Permission configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated registry permissions.</returns>
    Task<RegistryPermissions> SetPermissionsAsync(
        Guid registryId,
        RegistryPermissions permissions,
        CancellationToken ct = default);
}

/// <summary>
/// Registry information and configuration.
/// </summary>
public record RegistryInfo
{
    /// <summary>Unique registry identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Organization that owns the registry.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>Human-readable registry name.</summary>
    public required string Name { get; init; }

    /// <summary>Registry description.</summary>
    public string? Description { get; init; }

    /// <summary>Registry icon/logo URL.</summary>
    public Uri? IconUrl { get; init; }

    /// <summary>Registry access scope: Public, Internal, Private.</summary>
    public RegistryAccessScope AccessScope { get; init; }

    /// <summary>Total storage used in bytes.</summary>
    public long StorageUsedBytes { get; init; }

    /// <summary>Maximum storage allowed in bytes.</summary>
    public long StorageMaxBytes { get; init; }

    /// <summary>Timestamp of registry creation.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Timestamp of last modification.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// A plugin item in the registry.
/// </summary>
public record RegistryItem
{
    /// <summary>Unique item identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Registry containing this item.</summary>
    public Guid RegistryId { get; init; }

    /// <summary>Plugin name (e.g., "acme.logger").</summary>
    public required string Name { get; init; }

    /// <summary>Semantic version (e.g., "1.2.3").</summary>
    public required string Version { get; init; }

    /// <summary>Plugin display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Plugin description.</summary>
    public string? Description { get; init; }

    /// <summary>Plugin manifest with capabilities.</summary>
    public required PluginManifest Manifest { get; init; }

    /// <summary>Author/organization name.</summary>
    public string? Author { get; init; }

    /// <summary>License identifier (SPDX).</summary>
    public string? License { get; init; }

    /// <summary>Download count.</summary>
    public long DownloadCount { get; init; }

    /// <summary>Compliance scan result (latest).</summary>
    public ComplianceScanResult? LastScanResult { get; init; }

    /// <summary>Timestamp of publication.</summary>
    public DateTime PublishedAt { get; init; }
}

/// <summary>
/// Registry access permissions configuration.
/// </summary>
public record RegistryPermissions
{
    /// <summary>Registry ID.</summary>
    public Guid RegistryId { get; init; }

    /// <summary>User roles with registry permissions.</summary>
    public IReadOnlyDictionary<string, RegistryRolePermissions> RolePermissions { get; init; }
        = new Dictionary<string, RegistryRolePermissions>();

    /// <summary>User IDs with direct registry access.</summary>
    public IReadOnlyList<Guid> AllowedUserIds { get; init; }
        = Array.Empty<Guid>();

    /// <summary>Whether anonymous/unauthenticated access is allowed.</summary>
    public bool AllowAnonymousRead { get; init; }
}

public enum RegistryAccessScope
{
    Public,   // Visible to all users
    Internal, // Visible within organization
    Private   // Visible only to explicit users
}

/// <summary>
/// Manages approval workflows for plugin deployments.
/// </summary>
public interface IApprovalWorkflow
{
    /// <summary>
    /// Initiates approval workflow for plugin deployment.
    /// </summary>
    /// <param name="request">Approval request with context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Approval request with assigned workflow ID.</returns>
    Task<ApprovalRequest> InitiateAsync(
        ApprovalRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Submits decision for an approval step.
    /// </summary>
    /// <param name="requestId">Approval request ID.</param>
    /// <param name="stepId">Workflow step ID.</param>
    /// <param name="decision">Approval decision.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated approval request with new status.</returns>
    Task<ApprovalRequest> DecideAsync(
        Guid requestId,
        Guid stepId,
        ApprovalDecision decision,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves approval status and history.
    /// </summary>
    /// <param name="requestId">Approval request ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current approval status with step history.</returns>
    Task<ApprovalStatus> GetStatusAsync(
        Guid requestId,
        CancellationToken ct = default);
}

/// <summary>
/// An approval request for plugin deployment or usage.
/// </summary>
public record ApprovalRequest
{
    /// <summary>Unique request identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Organization context.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>Plugin being deployed/requested.</summary>
    public string PluginName { get; init; } = string.Empty;

    /// <summary>Plugin version.</summary>
    public string PluginVersion { get; init; } = string.Empty;

    /// <summary>Request type: Deploy, Update, Usage, Removal.</summary>
    public ApprovalRequestType RequestType { get; init; }

    /// <summary>User initiating the request.</summary>
    public Guid RequestedBy { get; init; }

    /// <summary>Deployment target environment.</summary>
    public string? TargetEnvironment { get; init; }

    /// <summary>User-provided justification.</summary>
    public string? Justification { get; init; }

    /// <summary>Workflow steps to execute.</summary>
    public IReadOnlyList<ApprovalStep> Steps { get; init; }
        = Array.Empty<ApprovalStep>();

    /// <summary>Overall request status.</summary>
    public ApprovalStatus.Status CurrentStatus { get; init; }

    /// <summary>When request was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Expected completion deadline.</summary>
    public DateTime? DeadlineAt { get; init; }
}

/// <summary>
/// A single approval step in the workflow.
/// </summary>
public record ApprovalStep
{
    /// <summary>Step identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Step sequence number (1-based).</summary>
    public int StepNumber { get; init; }

    /// <summary>Step name/description.</summary>
    public required string Name { get; init; }

    /// <summary>Role required to approve this step.</summary>
    public required string RequiredRole { get; init; }

    /// <summary>Number of approvals required.</summary>
    public int RequiredApprovals { get; init; } = 1;

    /// <summary>Whether step can be skipped if conditions met.</summary>
    public bool CanSkip { get; init; }

    /// <summary>Conditional logic (e.g., "riskScore > 5").</summary>
    public string? Condition { get; init; }

    /// <summary>Current approvers.</summary>
    public IReadOnlyList<Guid> AssignedApprovers { get; init; }
        = Array.Empty<Guid>();

    /// <summary>Step execution status.</summary>
    public ApprovalStatus.Status Status { get; init; }
}

/// <summary>
/// Decision submitted for an approval step.
/// </summary>
public record ApprovalDecision
{
    /// <summary>Approver user ID.</summary>
    public Guid ApprovedBy { get; init; }

    /// <summary>Decision: Approved, Rejected, RequestedChanges.</summary>
    public DecisionType Decision { get; init; }

    /// <summary>Comments from approver.</summary>
    public string? Comments { get; init; }

    /// <summary>When decision was made.</summary>
    public DateTime DecidedAt { get; init; }

    /// <summary>Supporting evidence or reference.</summary>
    public string? Reference { get; init; }
}

public enum ApprovalRequestType
{
    Deploy,
    Update,
    Usage,
    Removal
}

public enum DecisionType
{
    Approved,
    Rejected,
    RequestedChanges
}

/// <summary>
/// Tracks approval request status and history.
/// </summary>
public record ApprovalStatus
{
    public enum Status
    {
        Pending,
        InReview,
        Approved,
        Rejected,
        Expired,
        Cancelled
    }

    /// <summary>Request identifier.</summary>
    public Guid RequestId { get; init; }

    /// <summary>Current status.</summary>
    public Status CurrentStatus { get; init; }

    /// <summary>Currently pending approval steps.</summary>
    public IReadOnlyList<ApprovalStep> PendingSteps { get; init; }
        = Array.Empty<ApprovalStep>();

    /// <summary>Completed steps with decisions.</summary>
    public IReadOnlyList<(ApprovalStep Step, ApprovalDecision Decision)> CompletedSteps { get; init; }
        = Array.Empty<(ApprovalStep, ApprovalDecision)>();

    /// <summary>Progress percentage (0-100).</summary>
    public int ProgressPercent { get; init; }

    /// <summary>Estimated time to completion.</summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// Scans plugins for compliance violations and security issues.
/// </summary>
public interface IComplianceScanner
{
    /// <summary>
    /// Scans a plugin for compliance violations.
    /// </summary>
    /// <param name="pluginId">Plugin identifier to scan.</param>
    /// <param name="policies">Compliance policies to check against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Scan result with violations and recommendations.</returns>
    Task<ComplianceScanResult> ScanAsync(
        Guid pluginId,
        IReadOnlyList<GovernancePolicy> policies,
        CancellationToken ct = default);

    /// <summary>
    /// Scans plugin manifest for known vulnerabilities.
    /// </summary>
    /// <param name="manifest">Plugin manifest to scan.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Security vulnerabilities found.</returns>
    Task<IReadOnlyList<SecurityVulnerability>> ScanVulnerabilitiesAsync(
        PluginManifest manifest,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves scan history for a plugin.
    /// </summary>
    /// <param name="pluginId">Plugin to retrieve history for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Historical scan results.</returns>
    Task<IReadOnlyList<ComplianceScanResult>> GetScanHistoryAsync(
        Guid pluginId,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a compliance scan operation.
/// </summary>
public record ComplianceScanResult
{
    /// <summary>Unique scan identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Plugin that was scanned.</summary>
    public Guid PluginId { get; init; }

    /// <summary>Overall compliance level.</summary>
    public ComplianceLevel Level { get; init; }

    /// <summary>Compliance score (0-100).</summary>
    public int Score { get; init; }

    /// <summary>Violations found during scan.</summary>
    public IReadOnlyList<ComplianceViolation> Violations { get; init; }
        = Array.Empty<ComplianceViolation>();

    /// <summary>Security vulnerabilities detected.</summary>
    public IReadOnlyList<SecurityVulnerability> Vulnerabilities { get; init; }
        = Array.Empty<SecurityVulnerability>();

    /// <summary>When the scan completed.</summary>
    public DateTime ScanCompletedAt { get; init; }

    /// <summary>Recommendations for remediation.</summary>
    public IReadOnlyList<string> Recommendations { get; init; }
        = Array.Empty<string>();
}

public enum ComplianceLevel
{
    Excellent,  // >= 90%
    Good,       // >= 75%
    Fair,       // >= 50%
    Poor,       // >= 25%
    Failed      // < 25%
}

/// <summary>
/// A single compliance violation.
/// </summary>
public record ComplianceViolation
{
    /// <summary>Violation identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Policy violated.</summary>
    public string PolicyName { get; init; } = string.Empty;

    /// <summary>Violation severity: Critical, High, Medium, Low.</summary>
    public ViolationSeverity Severity { get; init; }

    /// <summary>Description of the violation.</summary>
    public required string Description { get; init; }

    /// <summary>Location/component affected.</summary>
    public string? AffectedComponent { get; init; }

    /// <summary>Remediation guidance.</summary>
    public string? Remediation { get; init; }
}

public enum ViolationSeverity
{
    Critical,
    High,
    Medium,
    Low,
    Info
}

/// <summary>
/// A security vulnerability found in plugin dependencies.
/// </summary>
public record SecurityVulnerability
{
    /// <summary>CVE identifier (if applicable).</summary>
    public string? CveId { get; init; }

    /// <summary>Vulnerable component/library name.</summary>
    public required string Component { get; init; }

    /// <summary>Current version in plugin.</summary>
    public string? CurrentVersion { get; init; }

    /// <summary>Version with fix (if available).</summary>
    public string? FixedVersion { get; init; }

    /// <summary>Vulnerability severity.</summary>
    public ViolationSeverity Severity { get; init; }

    /// <summary>Description of the vulnerability.</summary>
    public required string Description { get; init; }

    /// <summary>CVSS score (0-10).</summary>
    public float? CvssScore { get; init; }

    /// <summary>Published date of vulnerability.</summary>
    public DateTime? PublishedDate { get; init; }
}

/// <summary>
/// Defines and enforces organizational governance policies.
/// </summary>
public interface IGovernanceService
{
    /// <summary>
    /// Creates a new governance policy for the organization.
    /// </summary>
    /// <param name="policy">Policy definition.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created policy with assigned ID.</returns>
    Task<GovernancePolicy> CreatePolicyAsync(
        GovernancePolicy policy,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluates a plugin against governance policies.
    /// </summary>
    /// <param name="pluginId">Plugin to evaluate.</param>
    /// <param name="context">Evaluation context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Policy evaluation results.</returns>
    Task<PolicyEvaluation> EvaluateAsync(
        Guid pluginId,
        EvaluationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all policies for the organization.
    /// </summary>
    /// <param name="organizationId">Organization ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of governance policies.</returns>
    Task<IReadOnlyList<GovernancePolicy>> GetPoliciesAsync(
        Guid organizationId,
        CancellationToken ct = default);
}

/// <summary>
/// An organizational governance policy.
/// </summary>
public record GovernancePolicy
{
    /// <summary>Policy identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Organization that owns this policy.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>Human-readable policy name.</summary>
    public required string Name { get; init; }

    /// <summary>Policy description and rationale.</summary>
    public string? Description { get; init; }

    /// <summary>Policy type: Usage, Security, Compliance, Data.</summary>
    public PolicyType Type { get; init; }

    /// <summary>Policy enforcement level: Enforce, Warn, Audit.</summary>
    public EnforcementLevel EnforcementLevel { get; init; }

    /// <summary>CEL expression for policy evaluation.</summary>
    public required string Rule { get; init; }

    /// <summary>Target: Plugin, Registry, Organization, User.</summary>
    public PolicyTarget Target { get; init; }

    /// <summary>When policy becomes effective.</summary>
    public DateTime EffectiveAt { get; init; }

    /// <summary>When policy expires (optional).</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Policy creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Last update timestamp.</summary>
    public DateTime UpdatedAt { get; init; }
}

public enum PolicyType
{
    Usage,      // How plugins can be used
    Security,   // Security requirements
    Compliance, // Regulatory compliance
    Data,       // Data handling restrictions
    Performance // Performance/resource constraints
}

public enum EnforcementLevel
{
    Enforce,    // Block violation
    Warn,       // Alert but allow
    Audit       // Log but take no action
}

public enum PolicyTarget
{
    Plugin,
    Registry,
    Organization,
    User,
    Team
}

/// <summary>
/// Result of policy evaluation.
/// </summary>
public record PolicyEvaluation
{
    /// <summary>Plugin that was evaluated.</summary>
    public Guid PluginId { get; init; }

    /// <summary>Whether all policies passed.</summary>
    public bool AllPoliciesPassed { get; init; }

    /// <summary>Violations found during evaluation.</summary>
    public IReadOnlyList<PolicyViolation> Violations { get; init; }
        = Array.Empty<PolicyViolation>();

    /// <summary>Policies that passed.</summary>
    public IReadOnlyList<GovernancePolicy> PassedPolicies { get; init; }
        = Array.Empty<GovernancePolicy>();

    /// <summary>When evaluation was performed.</summary>
    public DateTime EvaluatedAt { get; init; }
}

/// <summary>
/// A policy evaluation violation.
/// </summary>
public record PolicyViolation
{
    /// <summary>Policy that was violated.</summary>
    public Guid PolicyId { get; init; }

    /// <summary>Policy name.</summary>
    public string PolicyName { get; init; } = string.Empty;

    /// <summary>Description of the violation.</summary>
    public required string Description { get; init; }

    /// <summary>Severity: Critical, High, Medium, Low.</summary>
    public ViolationSeverity Severity { get; init; }

    /// <summary>What remediation is needed.</summary>
    public string? RemediationSteps { get; init; }
}

/// <summary>
/// Tracks plugin usage analytics and metrics.
/// </summary>
public interface IUsageAnalytics
{
    /// <summary>
    /// Records a plugin usage event.
    /// </summary>
    /// <param name="usageEvent">Event to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Completion task.</returns>
    Task RecordEventAsync(
        UsageEvent usageEvent,
        CancellationToken ct = default);

    /// <summary>
    /// Generates usage report for a time period.
    /// </summary>
    /// <param name="organizationId">Organization to report on.</param>
    /// <param name="startDate">Report start date.</param>
    /// <param name="endDate">Report end date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Compiled usage report.</returns>
    Task<UsageReport> GenerateReportAsync(
        Guid organizationId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Gets usage metrics for a specific plugin.
    /// </summary>
    /// <param name="pluginId">Plugin to get metrics for.</param>
    /// <param name="days">Number of days to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Plugin usage metrics.</returns>
    Task<ItemUsage> GetItemMetricsAsync(
        Guid pluginId,
        int days = 30,
        CancellationToken ct = default);
}

/// <summary>
/// A recorded usage event.
/// </summary>
public record UsageEvent
{
    /// <summary>Unique event identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Organization context.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>Plugin that was used.</summary>
    public Guid PluginId { get; init; }

    /// <summary>User who triggered the event.</summary>
    public Guid UserId { get; init; }

    /// <summary>Event type: Execute, Install, Update, Enable, Disable, etc.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>Execution duration in milliseconds (if applicable).</summary>
    public long? DurationMs { get; init; }

    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Error message (if operation failed).</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>When the event occurred.</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Custom metadata for event.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Comprehensive usage report.
/// </summary>
public record UsageReport
{
    /// <summary>Report period start date.</summary>
    public DateTime StartDate { get; init; }

    /// <summary>Report period end date.</summary>
    public DateTime EndDate { get; init; }

    /// <summary>Organization included in report.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>Total unique plugins used.</summary>
    public int UniquePluginsCount { get; init; }

    /// <summary>Total plugin executions.</summary>
    public long TotalExecutions { get; init; }

    /// <summary>Total unique users.</summary>
    public int UniqueUsersCount { get; init; }

    /// <summary>Success rate percentage.</summary>
    public float SuccessRatePercent { get; init; }

    /// <summary>Usage by plugin.</summary>
    public IReadOnlyList<ItemUsage> ItemUsage { get; init; }
        = Array.Empty<ItemUsage>();

    /// <summary>Usage by user.</summary>
    public IReadOnlyList<UserUsage> UserUsage { get; init; }
        = Array.Empty<UserUsage>();

    /// <summary>Trends and patterns.</summary>
    public ReportMetrics Metrics { get; init; } = new();
}

/// <summary>
/// Usage metrics for a specific plugin.
/// </summary>
public record ItemUsage
{
    /// <summary>Plugin identifier.</summary>
    public Guid PluginId { get; init; }

    /// <summary>Plugin name.</summary>
    public string PluginName { get; init; } = string.Empty;

    /// <summary>Total executions.</summary>
    public long ExecutionCount { get; init; }

    /// <summary>Number of unique users.</summary>
    public int UniqueUserCount { get; init; }

    /// <summary>Success count.</summary>
    public long SuccessCount { get; init; }

    /// <summary>Failure count.</summary>
    public long FailureCount { get; init; }

    /// <summary>Average execution duration in milliseconds.</summary>
    public long AverageDurationMs { get; init; }

    /// <summary>Total CPU time used.</summary>
    public long TotalCpuMs { get; init; }

    /// <summary>Total memory used in MB.</summary>
    public long TotalMemoryMb { get; init; }
}

/// <summary>
/// Usage metrics for a specific user.
/// </summary>
public record UserUsage
{
    /// <summary>User identifier.</summary>
    public Guid UserId { get; init; }

    /// <summary>User name/email.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Total executions by this user.</summary>
    public long ExecutionCount { get; init; }

    /// <summary>Unique plugins used.</summary>
    public int UniquePluginsCount { get; init; }

    /// <summary>Success rate for this user.</summary>
    public float SuccessRatePercent { get; init; }

    /// <summary>Most recently used plugin.</summary>
    public Guid? LastPluginUsedId { get; init; }

    /// <summary>When user last used a plugin.</summary>
    public DateTime? LastActivityAt { get; init; }
}

/// <summary>
/// Aggregate metrics for a report.
/// </summary>
public record ReportMetrics
{
    /// <summary>Peak execution hour (0-23).</summary>
    public int? PeakHour { get; init; }

    /// <summary>Most used plugin.</summary>
    public Guid? TopPluginId { get; init; }

    /// <summary>Most active user.</summary>
    public Guid? TopUserId { get; init; }

    /// <summary>Trend compared to previous period.</summary>
    public float TrendPercent { get; init; }
}
```

---

## 4. Architecture

### 4.1 Enterprise Extensions System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Admin Console UI                            │
│  (Dashboard, Registry Mgmt, Policies, Analytics, Compliance)    │
└─────────────────────────────────────────────────────────────────┘
         │              │              │              │
         ▼              ▼              ▼              ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Enterprise API Layer                          │
├──────────────────────────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌──────────────────┐ ┌────────────────────┐ │
│ │  Registry API   │ │ Approval API     │ │  Compliance API   │ │
│ │  - Publish      │ │ - Initiate WF    │ │  - Scan           │ │
│ │  - Search       │ │ - Decide         │ │  - Get Results    │ │
│ │  - Permissions  │ │ - Get Status     │ │  - History        │ │
│ └─────────────────┘ │                  │ │                   │ │
│                      └──────────────────┘ └────────────────────┘ │
│ ┌─────────────────┐ ┌──────────────────┐ ┌────────────────────┐ │
│ │ Policy API      │ │ Analytics API    │ │  Audit API        │ │
│ │ - Create Policy │ │ - Record Event   │ │  - Query Logs     │ │
│ │ - Evaluate      │ │ - Generate Report│ │  - Filter Events  │ │
│ │ - List Policies │ │ - Get Metrics    │ │  - Export         │ │
│ └─────────────────┘ │                  │ │                   │ │
│                      └──────────────────┘ └────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
         │              │              │              │
         ▼              ▼              ▼              ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Core Services Layer                           │
├──────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Private Registry Service (v0.15.5e)                        │ │
│ │ - Registry lifecycle management                            │ │
│ │ - RBAC for registry access                                │ │
│ │ - Plugin publish/version control                          │ │
│ │ - Full-text indexing & search                             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Approval Workflow Engine (v0.15.5f)                       │ │
│ │ - Multi-step workflow definition                          │ │
│ │ - CEL-based conditional routing                           │ │
│ │ - Notification/escalation system                          │ │
│ │ - Timeout & SLA tracking                                  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Compliance Scanner (v0.15.5g)                             │ │
│ │ - Vulnerability database integration (NIST/NVD)          │ │
│ │ - Policy violation detection                              │ │
│ │ - Risk scoring & severity assessment                      │ │
│ │ - Recommendation engine                                   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Governance Policy Engine (v0.15.5h)                       │ │
│ │ - CEL expression evaluation                               │ │
│ │ - Policy conflict detection                               │ │
│ │ - Time-based & context-aware enforcement                  │ │
│ │ - Audit trail for policy decisions                        │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Usage Analytics Service (v0.15.5i)                        │ │
│ │ - Event ingestion & deduplication                         │ │
│ │ - Metrics aggregation (1m, 5m, 1h, 1d)                   │ │
│ │ - Trend analysis & anomaly detection                      │ │
│ │ - Report generation & export                              │ │
│ └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
         │              │              │              │
         ▼              ▼              ▼              ▼
┌──────────────────────────────────────────────────────────────────┐
│            Security & Data Layer (Shared)                        │
├──────────────────────────────────────────────────────────────────┤
│ ┌────────────────────┐ ┌──────────────────┐ ┌────────────────┐ │
│ │ Authorization (0.11.1) │ Audit Logging  │ Encryption     │ │
│ │ - Fine-grained RBAC    │ (v0.11.2)      │ (v0.11.3)     │ │
│ │ - OAuth/SSO support    │ - All actions  │ - TLS/AES     │ │
│ │ - Token validation     │ - Audit trail  │ - Key mgmt    │ │
│ └────────────────────┘ │                │ │              │ │
│                        └──────────────────┘ └────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
         │              │              │              │
         ▼              ▼              ▼              ▼
┌──────────────────────────────────────────────────────────────────┐
│               PostgreSQL / Storage Layer                         │
├──────────────────────────────────────────────────────────────────┤
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐  │
│ │ Registries   │ │ Approvals    │ │ Compliance / Policies   │  │
│ │ - registry   │ │ - requests   │ │ - scan_results         │  │
│ │ - items      │ │ - steps      │ │ - violations           │  │
│ │ - manifests  │ │ - decisions  │ │ - vulnerabilities      │  │
│ │ - perms      │ │ - history    │ │ - policies             │  │
│ └──────────────┘ └──────────────┘ └──────────────────────────┘  │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ Analytics (TimescaleDB)                                     │ │
│ │ - usage_events (millions of rows)                          │ │
│ │ - aggregated metrics (hourly, daily)                       │ │
│ │ - retention: 90d detailed, 1y aggregated                  │ │
│ └──────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 Data Flow Diagram

```
User/Admin
    │
    ▼
┌─────────────────────────────────────────┐
│  Admin Console UI                       │
│  (Next.js, TypeScript)                 │
└─────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────┐
│  REST API Gateway                       │
│  - Auth/RBAC enforcement               │
│  - Rate limiting                       │
│  - Request validation                  │
└─────────────────────────────────────────┘
    │
    ├────────────────────────────────────────────────────────┐
    │                                                        │
    ▼                                                        ▼
┌──────────────────────┐                        ┌──────────────────────┐
│ Plugin Registry Path │                        │ Governance Path      │
│                      │                        │                      │
│ 1. Publish Plugin    │                        │ 1. Define Policy     │
│    ↓                 │                        │    ↓                 │
│ 2. Run Compliance    │                        │ 2. Attach to Rules   │
│    Scanner           │                        │    ↓                 │
│    ↓                 │                        │ 3. Evaluate on Use   │
│ 3. Update Registry   │                        │    ↓                 │
│    ↓                 │                        │ 4. Record Decision   │
│ 4. Index for Search  │                        │                      │
└──────────────────────┘                        └──────────────────────┘
    │                                                        │
    ├────────────────────────────────────────────────────────┤
    │                                                        │
    ▼                                                        ▼
┌──────────────────────┐                        ┌──────────────────────┐
│ Approval Path        │                        │ Usage Analytics      │
│                      │                        │                      │
│ 1. Create Request    │                        │ 1. Plugin Execute    │
│    ↓                 │                        │    ↓                 │
│ 2. Route to Step 1   │                        │ 2. Record Event      │
│    ↓                 │                        │    ↓                 │
│ 3. Notify Approvers  │                        │ 3. Aggregate Stats   │
│    ↓                 │                        │    ↓                 │
│ 4. Process Decision  │                        │ 4. Generate Report   │
│    ↓                 │                        │    ↓                 │
│ 5. Route to Step N   │                        │ 5. Export Data       │
│    ↓                 │                        │                      │
│ 6. Final Status      │                        │                      │
└──────────────────────┘                        └──────────────────────┘
    │                                                        │
    └────────────────────────────────────────────────────────┘
                    │
                    ▼
        ┌──────────────────────────────┐
        │ PostgreSQL Database          │
        │ - Transactional data         │
        │ - Configuration              │
        │ - Audit logs                 │
        └──────────────────────────────┘
                    │
                    ▼
        ┌──────────────────────────────┐
        │ TimescaleDB (Analytics)      │
        │ - High-volume metrics        │
        │ - Time-series data           │
        │ - Retention policies         │
        └──────────────────────────────┘
```

---

## 5. PostgreSQL Schema

### 5.1 Private Registry Tables

```sql
-- Organizations (shared with other subsystems)
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    tier VARCHAR(50) NOT NULL, -- 'core', 'pro', 'teams', 'enterprise'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(name)
);

-- Private Registries
CREATE TABLE registries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    icon_url VARCHAR(2048),
    access_scope VARCHAR(50) NOT NULL DEFAULT 'private', -- 'public', 'internal', 'private'
    storage_used_bytes BIGINT DEFAULT 0,
    storage_max_bytes BIGINT DEFAULT 10737418240, -- 10GB default
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, name),
    INDEX idx_registry_org (organization_id)
);

-- Registry Items (Plugins)
CREATE TABLE registry_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    registry_id UUID NOT NULL REFERENCES registries(id),
    name VARCHAR(255) NOT NULL,
    version VARCHAR(50) NOT NULL,
    display_name VARCHAR(255) NOT NULL,
    description TEXT,
    manifest JSONB NOT NULL,
    author VARCHAR(255),
    license VARCHAR(50),
    download_count BIGINT DEFAULT 0,
    published_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(registry_id, name, version),
    INDEX idx_item_registry (registry_id),
    INDEX idx_item_name (name),
    INDEX idx_item_published (published_at)
);

-- Registry Permissions
CREATE TABLE registry_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    registry_id UUID NOT NULL REFERENCES registries(id),
    role_name VARCHAR(100) NOT NULL,
    can_read BOOLEAN DEFAULT true,
    can_publish BOOLEAN DEFAULT false,
    can_delete BOOLEAN DEFAULT false,
    can_manage_access BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(registry_id, role_name),
    INDEX idx_perms_registry (registry_id)
);

-- Registry Access (User permissions)
CREATE TABLE registry_access (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    registry_id UUID NOT NULL REFERENCES registries(id),
    user_id UUID NOT NULL,
    granted_role VARCHAR(100) NOT NULL,
    granted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    granted_by UUID NOT NULL,
    INDEX idx_access_registry (registry_id),
    INDEX idx_access_user (user_id)
);
```

### 5.2 Approval Workflow Tables

```sql
-- Approval Requests
CREATE TABLE approval_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    plugin_name VARCHAR(255) NOT NULL,
    plugin_version VARCHAR(50) NOT NULL,
    request_type VARCHAR(50) NOT NULL, -- 'deploy', 'update', 'usage', 'removal'
    requested_by UUID NOT NULL,
    target_environment VARCHAR(100),
    justification TEXT,
    current_status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'in_review', 'approved', 'rejected', 'expired', 'cancelled'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deadline_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    INDEX idx_request_org (organization_id),
    INDEX idx_request_status (current_status),
    INDEX idx_request_created (created_at)
);

-- Approval Steps
CREATE TABLE approval_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID NOT NULL REFERENCES approval_requests(id),
    step_number INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    required_role VARCHAR(100) NOT NULL,
    required_approvals INT DEFAULT 1,
    can_skip BOOLEAN DEFAULT false,
    condition TEXT,
    current_status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'approved', 'rejected', 'skipped'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_step_request (request_id),
    INDEX idx_step_status (current_status)
);

-- Step Approvers
CREATE TABLE step_approvers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    step_id UUID NOT NULL REFERENCES approval_steps(id),
    approver_id UUID NOT NULL,
    assigned_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_approver_step (step_id),
    INDEX idx_approver_user (approver_id)
);

-- Approval Decisions
CREATE TABLE approval_decisions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    step_id UUID NOT NULL REFERENCES approval_steps(id),
    approved_by UUID NOT NULL,
    decision VARCHAR(50) NOT NULL, -- 'approved', 'rejected', 'requested_changes'
    comments TEXT,
    reference VARCHAR(1024),
    decided_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_decision_step (step_id),
    INDEX idx_decision_approver (approved_by)
);
```

### 5.3 Compliance & Policy Tables

```sql
-- Governance Policies
CREATE TABLE governance_policies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL, -- 'usage', 'security', 'compliance', 'data', 'performance'
    enforcement_level VARCHAR(50) NOT NULL DEFAULT 'enforce', -- 'enforce', 'warn', 'audit'
    rule TEXT NOT NULL, -- CEL expression
    target VARCHAR(50) NOT NULL DEFAULT 'plugin', -- 'plugin', 'registry', 'organization', 'user', 'team'
    effective_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_policy_org (organization_id),
    INDEX idx_policy_type (type),
    INDEX idx_policy_effective (effective_at, expires_at)
);

-- Compliance Scan Results
CREATE TABLE compliance_scan_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plugin_id UUID NOT NULL,
    registry_item_id UUID REFERENCES registry_items(id),
    level VARCHAR(50) NOT NULL, -- 'excellent', 'good', 'fair', 'poor', 'failed'
    score INT NOT NULL,
    scan_completed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_scan_plugin (plugin_id),
    INDEX idx_scan_item (registry_item_id),
    INDEX idx_scan_completed (scan_completed_at)
);

-- Compliance Violations
CREATE TABLE compliance_violations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scan_result_id UUID NOT NULL REFERENCES compliance_scan_results(id),
    violation_id VARCHAR(255) NOT NULL,
    policy_name VARCHAR(255) NOT NULL,
    severity VARCHAR(50) NOT NULL, -- 'critical', 'high', 'medium', 'low', 'info'
    description TEXT NOT NULL,
    affected_component VARCHAR(255),
    remediation TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_violation_scan (scan_result_id),
    INDEX idx_violation_severity (severity)
);

-- Security Vulnerabilities
CREATE TABLE security_vulnerabilities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scan_result_id UUID NOT NULL REFERENCES compliance_scan_results(id),
    cve_id VARCHAR(50),
    component VARCHAR(255) NOT NULL,
    current_version VARCHAR(50),
    fixed_version VARCHAR(50),
    severity VARCHAR(50) NOT NULL, -- 'critical', 'high', 'medium', 'low', 'info'
    description TEXT NOT NULL,
    cvss_score FLOAT,
    published_date TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_vuln_scan (scan_result_id),
    INDEX idx_vuln_cve (cve_id),
    INDEX idx_vuln_severity (severity)
);

-- Policy Evaluations
CREATE TABLE policy_evaluations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plugin_id UUID NOT NULL,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    all_policies_passed BOOLEAN NOT NULL,
    evaluated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_eval_plugin (plugin_id),
    INDEX idx_eval_org (organization_id),
    INDEX idx_eval_at (evaluated_at)
);

-- Policy Evaluation Details
CREATE TABLE policy_evaluation_violations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    evaluation_id UUID NOT NULL REFERENCES policy_evaluations(id),
    policy_id UUID NOT NULL REFERENCES governance_policies(id),
    description TEXT NOT NULL,
    severity VARCHAR(50) NOT NULL,
    remediation_steps TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_eval_detail_eval (evaluation_id),
    INDEX idx_eval_detail_policy (policy_id)
);
```

### 5.4 Usage Analytics Tables

```sql
-- Usage Events (TimescaleDB hypertable for high volume)
CREATE TABLE usage_events (
    time TIMESTAMP WITH TIME ZONE NOT NULL,
    event_id UUID NOT NULL DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL,
    plugin_id UUID NOT NULL,
    user_id UUID NOT NULL,
    event_type VARCHAR(50) NOT NULL, -- 'execute', 'install', 'update', 'enable', 'disable'
    duration_ms BIGINT,
    success BOOLEAN NOT NULL,
    error_message TEXT,
    metadata JSONB,
    PRIMARY KEY (time, event_id)
) PARTITION BY RANGE (time);

-- Create TimescaleDB hypertable if using TimescaleDB
-- SELECT create_hypertable('usage_events', 'time', if_not_exists => TRUE);

-- Hourly Metrics (aggregated)
CREATE TABLE usage_metrics_hourly (
    time TIMESTAMP WITH TIME ZONE NOT NULL,
    organization_id UUID NOT NULL,
    plugin_id UUID,
    user_id UUID,
    execution_count BIGINT DEFAULT 0,
    success_count BIGINT DEFAULT 0,
    failure_count BIGINT DEFAULT 0,
    avg_duration_ms BIGINT,
    total_cpu_ms BIGINT,
    total_memory_mb BIGINT,
    PRIMARY KEY (time, organization_id, plugin_id, user_id),
    INDEX idx_metrics_org_time (organization_id, time),
    INDEX idx_metrics_plugin_time (plugin_id, time)
);

-- Daily Metrics (aggregated)
CREATE TABLE usage_metrics_daily (
    date DATE NOT NULL,
    organization_id UUID NOT NULL,
    plugin_id UUID,
    user_id UUID,
    execution_count BIGINT DEFAULT 0,
    success_count BIGINT DEFAULT 0,
    failure_count BIGINT DEFAULT 0,
    avg_duration_ms BIGINT,
    total_cpu_ms BIGINT,
    total_memory_mb BIGINT,
    PRIMARY KEY (date, organization_id, plugin_id, user_id),
    INDEX idx_daily_org_date (organization_id, date),
    INDEX idx_daily_plugin_date (plugin_id, date)
);

-- Plugin Statistics
CREATE TABLE plugin_statistics (
    plugin_id UUID PRIMARY KEY NOT NULL,
    organization_id UUID NOT NULL,
    total_executions BIGINT DEFAULT 0,
    total_users INT DEFAULT 0,
    success_count BIGINT DEFAULT 0,
    failure_count BIGINT DEFAULT 0,
    last_executed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_stats_org (organization_id),
    INDEX idx_stats_updated (updated_at)
);

-- User Statistics
CREATE TABLE user_statistics (
    user_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    total_executions BIGINT DEFAULT 0,
    unique_plugins INT DEFAULT 0,
    success_count BIGINT DEFAULT 0,
    failure_count BIGINT DEFAULT 0,
    last_activity_at TIMESTAMP WITH TIME ZONE,
    PRIMARY KEY (user_id, organization_id),
    INDEX idx_user_stats_org (organization_id),
    INDEX idx_user_stats_activity (last_activity_at)
);
```

---

## 6. UI Mockups

### 6.1 Admin Console - Dashboard

```
╔════════════════════════════════════════════════════════════════════════════╗
║ LexiChord Enterprise — Admin Dashboard                                     ║
╠════════════════════════════════════════════════════════════════════════════╣
║                                                                             ║
║ Quick Stats (Top)                                                          ║
║ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐ ┌────────────┐  ║
║ │ Total Plugins  │ │ Total Executions│ │ Unique Users   │ │ Compliance │  ║
║ │      284       │ │    1,247,392    │ │      142       │ │  92.4%    │  ║
║ └────────────────┘ └────────────────┘ └────────────────┘ └────────────┘  ║
║                                                                             ║
║ ─────────────────────────────────────────────────────────────────────────  ║
║                                                                             ║
║ Left Sidebar                │ Main Content Area                             ║
║ ┌──────────────────────────┤ ┌──────────────────────────────────────────┐ ║
║ │ Dashboard   [selected]   │ │ Execution Trend (Last 30 Days)          │ ║
║ │                          │ │                                          │ ║
║ │ Registries             │ │     ╱╲     ╱╲    ╱╲                     │ ║
║ │ ├─ Prod Registry       │ │    ╱  ╲   ╱  ╲  ╱  ╲    ╱╲  ╱╲         │ ║
║ │ ├─ Dev Registry        │ │   ╱    ╲ ╱    ╲╱    ╲  ╱  ╲╱  ╲        │ ║
║ │ └─ Test Registry       │ │  ╱      ╳       ╳      ╱    ╳    ╳       │ ║
║ │                          │ │ ────────────────────────────────────    │ ║
║ │ Approvals              │ │ Jan  Feb  Mar  Apr  May  Jun  Jul  Aug │ ║
║ │ Policies               │ │                                          │ ║
║ │ Compliance             │ │ Trends: ↑ 12% week-over-week             │ ║
║ │ Analytics              │ └──────────────────────────────────────────┘ ║
║ │ Audit Logs             │                                             ║
║ │ Settings               │ ┌──────────────────────────────────────────┐ ║
║ │ Users & Access         │ │ Recent Compliance Scans                  │ ║
║ │                          │ ├──────────────────────────────────────────┤ ║
║ │                          │ │ ✓ acme.logger v2.1.0       90 (Good)   │ ║
║ │                          │ │ ✓ acme.cache v1.5.2        85 (Good)   │ ║
║ │                          │ │ ✗ badplugin.evil v0.1.0    15 (Failed) │ ║
║ │                          │ │ ⚠ acme.api v3.0.0          65 (Fair)   │ ║
║ │                          │ └──────────────────────────────────────────┘ ║
║ │                          │                                             ║
║ │                          │ ┌──────────────────────────────────────────┐ ║
║ │                          │ │ Pending Approvals (5)                   │ ║
║ │                          │ ├──────────────────────────────────────────┤ ║
║ │                          │ │ ⧖ Deploy acme.auth v2.0.0 (Security)   │ ║
║ │                          │ │   Awaiting: Security Lead (2/3 approved)│ ║
║ │                          │ │                                          │ ║
║ │                          │ │ ⧖ Update acme.db v1.3.0                 │ ║
║ │                          │ │   Awaiting: DBA (0/1 approved)          │ ║
║ │                          │ └──────────────────────────────────────────┘ ║
║ │                          │                                             ║
║ └──────────────────────────┴─────────────────────────────────────────────┘ ║
║                                                                             ║
╚════════════════════════════════════════════════════════════════════════════╝
```

### 6.2 Admin Console - Compliance Scanner

```
╔════════════════════════════════════════════════════════════════════════════╗
║ Compliance Scanner                                                         ║
╠════════════════════════════════════════════════════════════════════════════╣
║                                                                             ║
║ Scan History & Results                                                     ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │ Filter: [All Scans  ▼]  Level: [All ▼]  Sort: [Latest ▼]             │ ║
║ │                                                                        │ ║
║ │ Plugin Name      Version   Level      Score  Violations  Last Scanned│ ║
║ │ ─────────────────────────────────────────────────────────────────── │ ║
║ │ ✓ acme.logger    2.1.0     Good        90        0       2 days ago │ ║
║ │   OWASP Top 10: 0  CVEs: 0  Policy Violations: 0                     │ ║
║ │ ─────────────────────────────────────────────────────────────────── │ ║
║ │ ✓ acme.cache     1.5.2     Good        85        2       3 days ago │ ║
║ │   OWASP Top 10: 0  CVEs: 0  Policy Violations: 2 (Low)               │ ║
║ │ ─────────────────────────────────────────────────────────────────── │ ║
║ │ ⚠ acme.api       3.0.0     Fair        65        8       1 hour ago │ ║
║ │   OWASP Top 10: 1  CVEs: 1  Policy Violations: 7 (Medium)            │ ║
║ │   [View Details]                                                     │ ║
║ │ ─────────────────────────────────────────────────────────────────── │ ║
║ │ ✗ badplugin.evil 0.1.0     Failed      15       45       Now        │ ║
║ │   OWASP Top 10: 5  CVEs: 3  Policy Violations: 40 (Critical)         │ ║
║ │   [View Details] [Quarantine Plugin]                                 │ ║
║ │ ─────────────────────────────────────────────────────────────────── │ ║
║ │                                                  [Load More...]       │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
║ Scan Details (acme.api v3.0.0)                                             ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │ Vulnerabilities (1)          Policy Violations (7)                    │ ║
║ │ ┌──────────────────────┐    ┌──────────────────────────────────────┐ ║
║ │ │ ⚠ CVE-2024-12345    │    │ ⚠ No encryption configured         │ ║
║ │ │   axios ^0.27.0     │    │   Severity: High                   │ ║
║ │ │   CVSS: 7.5         │    │   Affects: API calls                │ ║
║ │ │   Status: Exists in  │    │   Remediation: Enable TLS 1.3      │ ║
║ │ │           dependencies│    │                                    │ ║
║ │ │   Fix: Upgrade to    │    │ ⚠ Insufficient audit logging      │ ║
║ │ │        ^1.6.0        │    │   Severity: Medium                 │ ║
║ │ └──────────────────────┘    │   Affects: API logging             │ ║
║ │                              │   Remediation: Add CloudWatch      │ ║
║ │                              │                                    │ ║
║ │                              │ • 5 more violations...             │ ║
║ │                              └──────────────────────────────────────┘ ║
║ │                                                                        │ ║
║ │ Recommendations:                                                       │ ║
║ │ 1. Upgrade axios to v1.6.0 or later to fix CVE-2024-12345            │ ║
║ │ 2. Enable TLS 1.3 encryption for all API calls                        │ ║
║ │ 3. Implement CloudWatch audit logging per policy CMP-LOG-001          │ ║
║ │ 4. Add authentication validation before plugin execution              │ ║
║ │                                                                        │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
╚════════════════════════════════════════════════════════════════════════════╝
```

### 6.3 Admin Console - Approval Workflow

```
╔════════════════════════════════════════════════════════════════════════════╗
║ Approval Workflows                                                         ║
╠════════════════════════════════════════════════════════════════════════════╣
║                                                                             ║
║ Pending Approvals (5 requests)                                             ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │ Request ID         Plugin          Type    Status   Progress Deadline  │ ║
║ │ ────────────────────────────────────────────────────────────────────── │ ║
║ │ APR-2024-0001 ⧖   acme.auth v2.0  Deploy  In       67%      2024-02-15│ ║
║ │                   Requested by: alice@acme.com                         │ ║
║ │                   Justification: Security fix for critical CVE         │ ║
║ │                   [View Details] [View Workflow]                       │ ║
║ │ ────────────────────────────────────────────────────────────────────── │ ║
║ │ APR-2024-0002 ⧖   acme.db v1.3.0  Update  Pending  0%       2024-02-18│ ║
║ │                   Requested by: bob@acme.com                           │ ║
║ │                   [View Details]                                       │ ║
║ │ ────────────────────────────────────────────────────────────────────── │ ║
║ │                              [Load More...]                            │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
║ Approval Workflow Detail: APR-2024-0001 (acme.auth v2.0)                   ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │                                                                        │ ║
║ │ Step 1: Security Review               ✓ APPROVED                      │ ║
║ │ ├─ Required Role: Security Lead (1 required)                          │ ║
║ │ ├─ ✓ Approved by: charlie@acme.com (2024-02-10 14:32)                │ ║
║ │ │    Comment: "Vulnerability fix confirmed, signatures validated"    │ ║
║ │ └─ Status: Complete                                                  │ ║
║ │                                                                        │ ║
║ │ Step 2: Architecture Review           ⧖ IN REVIEW (2/3 approved)     │ ║
║ │ ├─ Required Role: Tech Lead (2 required)                              │ ║
║ │ ├─ ✓ Approved by: dave@acme.com (2024-02-10 15:15)                  │ ║
║ │ │    Comment: "Design looks good, integration validated"            │ ║
║ │ ├─ ⧖ Awaiting: eve@acme.com (Security Lead) — Notified 2 hrs ago   │ ║
║ │ ├─ ⧖ Awaiting: frank@acme.com (Tech Lead) — Not yet notified       │ ║
║ │ └─ Status: Awaiting Decision                                        │ ║
║ │                                                                        │ ║
║ │ Step 3: Deployment Approval           ⧗ PENDING                      │ ║
║ │ ├─ Required Role: DevOps Lead (1 required)                            │ ║
║ │ └─ Condition: "Only required if riskScore > 5" (riskScore=8)         │ ║
║ │                                                                        │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
║ If you are eve@acme.com (Tech Lead):                                       ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │                                                                        │ ║
║ │ Your Action:                                                           │ ║
║ │ ┌──────────────────────────────────────────────────────────────────┐ ║
║ │ │ [ ✓ Approve ]  [ ✗ Reject ]  [ ⚠ Request Changes ]             │ ║
║ │ │                                                                  │ ║
║ │ │ Comments: [_________________________________]                  │ ║
║ │ │                                                                  │ ║
║ │ │ Reference: [_________________________________]                  │ ║
║ │ │            (e.g., JIRA ticket, Slack thread)                    │ ║
║ │ │                                                                  │ ║
║ │ │ [ Submit Decision ]                                             │ ║
║ │ └──────────────────────────────────────────────────────────────────┘ ║
║ │                                                                        │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
╚════════════════════════════════════════════════════════════════════════════╝
```

### 6.4 Admin Console - Governance Policies

```
╔════════════════════════════════════════════════════════════════════════════╗
║ Governance Policies                                                        ║
╠════════════════════════════════════════════════════════════════════════════╣
║                                                                             ║
║ Active Policies (12)                                                       ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │ [+ Create New Policy]                                                  │ ║
║ │                                                                        │ ║
║ │ Policy Name          Type       Target   Enforcement  Effective  Edit │ ║
║ │ ─────────────────────────────────────────────────────────────────── │ ║
║ │ ENCRYPTION-REQ       Security   Plugin   Enforce      2024-01-15 [...]│ ║
║ │   Rule: plugin.manifest.security.encryption == true                 │ ║
║ │                                                                        │ ║
║ │ NO-EVAL-CODE         Security   Plugin   Enforce      2024-01-15 [...]│ ║
║ │   Rule: !contains(plugin.manifest.code, "eval")                     │ ║
║ │                                                                        │ ║
║ │ AUDIT-LOG-REQ        Compliance Plugin   Enforce      2024-01-15 [...]│ ║
║ │   Rule: plugin.manifest.capabilities.audit == true                 │ ║
║ │                                                                        │ ║
║ │ MAX-EXECUTION-TIME   Performance Plugin  Warn         2024-01-15 [...]│ ║
║ │   Rule: plugin.manifest.maxExecutionTimeMs <= 60000                 │ ║
║ │                                                                        │ ║
║ │ PROD-ONLY            Usage      Plugin   Enforce      2024-02-01 [...]│ ║
║ │   Rule: context.environment == "production"                         │ ║
║ │   Target: [acme.auth, acme.db]                                      │ ║
║ │                                                                        │ ║
║ │ • 7 more policies                                                    │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
║ Create New Policy                                                          ║
║ ┌────────────────────────────────────────────────────────────────────────┐ ║
║ │                                                                        │ ║
║ │ Policy Name:  [____________________________________]                 │ ║
║ │                                                                        │ ║
║ │ Description:  [____________________________________]                 │ ║
║ │               [____________________________________]                 │ ║
║ │                                                                        │ ║
║ │ Policy Type:  [Security ▼]                                           │ ║
║ │               (Security, Compliance, Usage, Data, Performance)       │ ║
║ │                                                                        │ ║
║ │ Enforcement:  [Enforce ▼]                                            │ ║
║ │               (Enforce, Warn, Audit)                                 │ ║
║ │                                                                        │ ║
║ │ CEL Rule:     [                                                  ]   │ ║
║ │ ┌────────────────────────────────────────────────────────────────┐ ║
║ │ │ plugin.manifest.security.encryption == true &&                │ ║
║ │ │ plugin.manifest.author.verified == true                       │ ║
║ │ │                                                                │ ║
║ │ │ Variables Available:                                          │ ║
║ │ │ - plugin.* (manifest data)                                   │ ║
║ │ │ - context.* (evaluation context)                             │ ║
║ │ │ - user.* (requesting user)                                   │ ║
║ │ └────────────────────────────────────────────────────────────────┘ │ ║
║ │                                                                        │ ║
║ │ Effective Date: [2024-02-01 ▼]   Expires: [None ▼]                  │ ║
║ │                                                                        │ ║
║ │ [ Save Policy ]  [ Cancel ]                                          │ ║
║ │                                                                        │ ║
║ └────────────────────────────────────────────────────────────────────────┘ ║
║                                                                             ║
╚════════════════════════════════════════════════════════════════════════════╝
```

---

## 7. Dependencies

| Component | Source | Usage | Version |
|:----------|:-------|:------|:--------|
| `IPluginRepository` | v0.15.1-MKT | Plugin lookup and metadata | >= 0.15.1 |
| `IMarketplaceRegistry` | v0.15.3-MKT | Public marketplace integration | >= 0.15.3 |
| `IAuthorizationService` | v0.11.1-SEC | RBAC enforcement | >= 0.11.1 |
| `IAuditLogger` | v0.11.2-SEC | Compliance audit trail | >= 0.11.2 |
| `IEncryptionService` | v0.11.3-SEC | Data encryption at rest/transit | >= 0.11.3 |
| PostgreSQL | Database | Transactional data storage | >= 14.0 |
| TimescaleDB | Extension | Time-series metrics | >= 2.10.0 |
| CEL (Common Expression Language) | External | Policy evaluation engine | 0.13.2+ |
| NIST NVD | API | Vulnerability database | Current |
| OAuth 2.0 / OIDC | Protocol | Enterprise SSO | RFC 6749/6750 |

---

## 8. License Gating

| Tier | Feature Set | Details |
|:-----|:-----------|:--------|
| **Core** | Not available | Enterprise features require Teams or Enterprise tier |
| **WriterPro** | Not available | No private registries or governance controls |
| **Teams** | Basic Policies | Read-only access to public registries, basic compliance scanning, limited usage analytics, basic policy enforcement (audit only) |
| **Enterprise** | Full Suite | Private registries with unlimited storage, multi-step approval workflows, comprehensive compliance scanning (CVE + policy), full governance policies with enforcement, complete usage analytics with export, audit logging, SSO/SAML/OAuth integration, custom vulnerability rules |

License validation occurs:
- At service initialization (prevent tier downgrade exploits)
- On each API call (billing reconciliation)
- On policy evaluation (enforce tier-specific features)

---

## 9. Performance Targets

| Metric | Target | Measurement | Notes |
|:-------|:-------|:------------|:------|
| **Registry Search** | <500ms | 1000-item registry, complex query | Includes index hits |
| **Approval Initiation** | <1s | Create request + route to steps | Synchronous operation |
| **Policy Evaluation** | <100ms | CEL compilation + execution | Per-plugin evaluation |
| **Compliance Scan** | <5s | Plugin with 10 dependencies | Includes NVD API calls (cached) |
| **Usage Event Ingestion** | <10ms | Single event batch | Async write to TimescaleDB |
| **Report Generation** | <30s | 30-day report for 1000 plugins | 30-day retention tested |
| **Admin Console Load** | <2s | Dashboard initial page load | Including data fetches |
| **Concurrent Users** | 1000+ | Dashboard simultaneous sessions | WebSocket support for notifications |

---

## 10. Testing Strategy

### 10.1 Unit Tests

- **Registry Service:** CRUD operations, permission checks, search indexing
- **Approval Engine:** Workflow routing, decision logic, step transitions
- **Compliance Scanner:** Violation detection, scoring algorithm, CVE matching
- **Policy Evaluator:** CEL expression parsing, variable binding, result evaluation
- **Analytics Service:** Event deduplication, aggregation, metric calculation
- **Test Coverage Target:** >85% branch coverage

### 10.2 Integration Tests

- **Registry + Plugin Manager:** Publish workflow with manifest validation
- **Approval + Notification:** Request creation triggers notification service
- **Compliance + Registry:** Scan triggered on new plugin publication
- **Policy + Authorization:** Policy decision blocks/allows operations
- **Analytics + Plugin Execution:** Events recorded during plugin runs

### 10.3 Load Tests

- **Registry Search:** 1000 concurrent queries on 10,000-item registry
- **Event Ingestion:** 10,000 events/sec sustained for 1 hour
- **Report Generation:** 50 concurrent report requests for large datasets

### 10.4 Security Tests

- **RBAC Validation:** Unauthorized users cannot access restricted registries
- **Audit Logging:** All operations logged with user context
- **Encryption:** Data encrypted at rest and in transit
- **Input Validation:** CEL expressions and plugin manifests validated
- **CVE Database:** Vulnerability detection against current NVD

### 10.5 UI Tests

- **Admin Console:** E2E tests for dashboard, compliance view, approval workflow
- **Form Validation:** Policy creation form validates CEL syntax
- **Pagination:** Registry search and analytics tables handle large datasets
- **Responsive Design:** Console usable on desktop and tablet

---

## 11. MediatR Events

### 11.1 Registry Events

```csharp
/// <summary>
/// Fired when a plugin is published to a registry.
/// </summary>
public record PluginPublishedEvent : INotification
{
    public Guid RegistryId { get; init; }
    public Guid PluginId { get; init; }
    public string PluginName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public Guid PublishedBy { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when a registry's permissions are modified.
/// </summary>
public record RegistryPermissionsChangedEvent : INotification
{
    public Guid RegistryId { get; init; }
    public IReadOnlyList<Guid> AffectedUsers { get; init; } = Array.Empty<Guid>();
    public Guid ChangedBy { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### 11.2 Approval Events

```csharp
/// <summary>
/// Fired when an approval request is initiated.
/// </summary>
public record ApprovalRequestInitiatedEvent : INotification
{
    public Guid RequestId { get; init; }
    public Guid OrganizationId { get; init; }
    public string PluginName { get; init; } = string.Empty;
    public string RequestType { get; init; } = string.Empty;
    public IReadOnlyList<Guid> InitialApprovers { get; init; } = Array.Empty<Guid>();
    public Guid CreatedBy { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when an approval decision is submitted.
/// </summary>
public record ApprovalDecisionMadeEvent : INotification
{
    public Guid RequestId { get; init; }
    public Guid StepId { get; init; }
    public string Decision { get; init; } = string.Empty; // approved, rejected, requested_changes
    public Guid DecidedBy { get; init; }
    public string? Comments { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when an approval request is approved/rejected.
/// </summary>
public record ApprovalCompletedEvent : INotification
{
    public Guid RequestId { get; init; }
    public string FinalStatus { get; init; } = string.Empty; // approved, rejected
    public string PluginName { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### 11.3 Compliance Events

```csharp
/// <summary>
/// Fired when a compliance scan completes.
/// </summary>
public record ComplianceScanCompletedEvent : INotification
{
    public Guid ScanResultId { get; init; }
    public Guid PluginId { get; init; }
    public string PluginName { get; init; } = string.Empty;
    public string ComplianceLevel { get; init; } = string.Empty;
    public int ComplianceScore { get; init; }
    public int ViolationCount { get; init; }
    public int VulnerabilityCount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when a critical vulnerability is detected.
/// </summary>
public record CriticalVulnerabilityDetectedEvent : INotification
{
    public Guid ScanResultId { get; init; }
    public Guid PluginId { get; init; }
    public string CveId { get; init; } = string.Empty;
    public string Component { get; init; } = string.Empty;
    public float CvssScore { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### 11.4 Policy Events

```csharp
/// <summary>
/// Fired when a governance policy is created.
/// </summary>
public record GovernancePolicyCreatedEvent : INotification
{
    public Guid PolicyId { get; init; }
    public Guid OrganizationId { get; init; }
    public string PolicyName { get; init; } = string.Empty;
    public string PolicyType { get; init; } = string.Empty;
    public Guid CreatedBy { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when a policy evaluation results in a violation.
/// </summary>
public record PolicyViolationDetectedEvent : INotification
{
    public Guid EvaluationId { get; init; }
    public Guid PolicyId { get; init; }
    public Guid PluginId { get; init; }
    public string PolicyName { get; init; } = string.Empty;
    public string ViolationDescription { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty; // critical, high, medium, low
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

### 11.5 Analytics Events

```csharp
/// <summary>
/// Fired when usage analytics are generated.
/// </summary>
public record UsageReportGeneratedEvent : INotification
{
    public Guid ReportId { get; init; }
    public Guid OrganizationId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public long TotalExecutions { get; init; }
    public int UniquePlugins { get; init; }
    public float SuccessRate { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when usage anomalies are detected.
/// </summary>
public record UsageAnomalyDetectedEvent : INotification
{
    public Guid OrganizationId { get; init; }
    public Guid? PluginId { get; init; }
    public Guid? UserId { get; init; }
    public string AnomalyType { get; init; } = string.Empty; // spike, drop, error_rate
    public float Baseline { get; init; }
    public float Current { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

---

## 12. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|:-----|:-----------|:-------|:-----------|
| **Performance degradation with large registries** | Medium | High | Pre-caching, index optimization, pagination, async search indexing |
| **CVE database staleness** | Low | High | Automated daily NVD sync, cached results, update notifications |
| **False negatives in compliance scanning** | Medium | High | Maintain custom rule library, regular rule audits, human review queue |
| **Approval workflow deadlock** | Low | Medium | Step timeouts, escalation paths, admin override capability |
| **Policy expression complexity** | Medium | Medium | CEL template library, inline documentation, policy testing UI |
| **Event ingestion pipeline backlog** | Low | Medium | Async queue with backpressure, TimescaleDB auto-partitioning, circuit breakers |
| **Unauthorized registry access** | Low | Critical | Token validation, RBAC checks, audit logging, IP whitelisting |
| **Enterprise customer data leakage** | Very Low | Critical | Encryption at rest/transit, HIPAA/SOC2 compliance, regular security audits |
| **Admin console DDoS** | Low | High | Rate limiting, WAF, OAuth tokens (time-limited), monitoring/alerting |
| **CEL expression injection attacks** | Low | Critical | Input validation, sandboxed evaluation, no system access from CEL |

---

## 13. What This Enables

- **v0.16.x Marketplace Plugins:** Enterprise marketplace leverages private registries for controlled distribution.
- **v0.17.x Operational Dashboards:** Enterprise customers use analytics to optimize plugin deployments.
- **v0.18.x Compliance Automation:** Governance policies integrate with CI/CD for automated compliance checks.
- **v0.19.x Advanced Governance:** Multi-org governance hierarchies, inherited policies, cross-org collaboration.

---

## 14. Document History

| Version | Date | Author | Notes |
|:--------|:-----|:-------|:------|
| 0.1 | 2026-01-31 | Lead Architect | Initial draft - v0.15.5-MKT Enterprise Extensions |

---

**End of Document**
