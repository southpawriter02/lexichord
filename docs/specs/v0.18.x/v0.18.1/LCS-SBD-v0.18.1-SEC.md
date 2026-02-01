# LCS-SBD-v0.18.1-SEC: Scope Breakdown — Permission Framework

## 1. Document Control

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-SBD-v0.18.1-SEC |
| **Release Version** | v0.18.1 |
| **Module Name** | Permission Framework (Scoping Edition) |
| **Parent Release** | v0.18.x — Security & Compliance |
| **Document Type** | Scope Breakdown Document (SBD) |
| **Author** | Claude Code |
| **Date Created** | 2026-02-01 |
| **Last Updated** | 2026-02-01 |
| **Status** | DRAFT |
| **Classification** | Internal — Technical Specification |
| **Estimated Total Hours** | 58 hours |
| **Target Completion** | Sprint 18.1 (6 weeks) |

---

## 2. Executive Summary

### 2.1 Framework Overview

The Permission Framework (v0.18.1-SEC) establishes a comprehensive, enterprise-grade permission system that enables users to grant, deny, and scope AI capabilities with unprecedented transparency and control. This framework is fundamental to Lexichord's security architecture, ensuring that every action an AI system can take is explicitly authorized by the user with granular control over scope, duration, and contextual constraints.

Unlike traditional role-based access control (RBAC) systems, Lexichord's Permission Framework implements dynamic permission scoping with real-time consent dialogs, permission inheritance hierarchies, and temporal constraints. Users maintain complete visibility into what permissions are active, can revoke them at any time, and can set contextual boundaries such as "this permission applies only to this project" or "this permission expires in 24 hours."

### 2.2 Strategic Importance

Permission management is a critical differentiator in the AI assistant market. Users increasingly demand transparency and control over AI capabilities to address concerns about data privacy, security, and unintended system behavior. The Permission Framework addresses these concerns through:

- **Explicit Consent Model**: Every significant capability requires user acknowledgment
- **Fine-Grained Scope Control**: Permissions can be scoped by resource type, project, document, or time window
- **Revocation Rights**: Users can immediately revoke permissions without system restart
- **Audit Trail**: Complete logging of all permission grants, denials, and revocations
- **Contextual Constraints**: Apply permissions conditionally based on user context, data sensitivity, or operational conditions

This framework is also essential for compliance with emerging AI governance regulations such as GDPR, CCPA, and upcoming AI Act requirements. Organizations using Lexichord can demonstrate proper governance and user control mechanisms.

### 2.3 Implementation Strategy

The Permission Framework is implemented through seven integrated sub-components, each addressing a specific aspect of the permission lifecycle:

1. **Permission Registry & Types** — Centralized definition and categorization of all permissions
2. **Permission Request Pipeline** — Async pipeline for requesting and validating permissions
3. **User Consent Dialog System** — Rich UI for presenting permissions and capturing user decisions
4. **Permission Scope Manager** — Fine-grained scoping and context binding
5. **Grant Persistence & Storage** — Durable storage of permission grants in PostgreSQL
6. **Permission Revocation & Expiry** — Temporal management and immediate revocation
7. **Permission Inheritance & Delegation** — Hierarchical permissions and delegation chains

The total estimated effort is 58 hours, distributed across 6-week sprint cycles. The framework is designed to be extensible, allowing future versions to add advanced features such as AI-guided permission recommendations and role-based permission templates.

---

## 3. Detailed Sub-Parts Breakdown

### 3.1 v0.18.1a: Permission Registry & Types

**Estimated Hours**: 10 hours
**Sprint Assignment**: Week 1
**Swim Lane**: Security/Platform
**Status**: Pending Development

#### 3.1.1 Objective

Establish the foundational permission registry that defines all permissions available within Lexichord, their attributes, categories, risk levels, and scope constraints. This component serves as the single source of truth for the entire permission system.

#### 3.1.2 Scope

- Define `IPermissionRegistry` interface for accessing permission metadata
- Implement core `PermissionRegistry` service with in-memory caching and background refresh
- Create comprehensive permission types enumeration covering all AI capabilities
- Define permission attributes: ID, name, description, category, risk level, default scope, documentation links
- Implement permission categorization system (File Operations, Network Access, Code Execution, Data Analysis, etc.)
- Create risk assessment models (Low, Medium, High, Critical)
- Implement scope constraints (Project, Document, Resource, Global)
- Establish permission inheritance hierarchies (e.g., File.Write implies File.Read)

#### 3.1.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IPermissionRegistry` | Async interface for querying permissions |
| `PermissionRegistry` | Default implementation with caching |
| `PermissionType` | Record defining permission metadata |
| `PermissionCategory` | Enum: FileOps, NetworkAccess, CodeExecution, DataAnalysis, SystemControl, UserData, etc. |
| `PermissionRiskLevel` | Enum: Low, Medium, High, Critical |
| `PermissionScopeType` | Enum: Global, Project, Document, Resource, Session |
| `PermissionMetadata` | Record with documentation, examples, warnings |
| `permissions.json` | Seeded permission definitions file |

#### 3.1.4 Interfaces & Records

```csharp
/// <summary>
/// Provides access to the permission registry and permission metadata.
/// This is the single source of truth for all permissions available in Lexichord.
/// </summary>
public interface IPermissionRegistry
{
    /// <summary>
    /// Gets a permission by its identifier.
    /// </summary>
    /// <param name="permissionId">The permission identifier (e.g., "file.read", "network.http")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission type, or null if not found</returns>
    Task<PermissionType?> GetPermissionAsync(
        string permissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions in a specific category.
    /// </summary>
    /// <param name="category">The permission category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of permissions in the category</returns>
    Task<IReadOnlyCollection<PermissionType>> GetPermissionsByCategoryAsync(
        PermissionCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions matching the specified risk level.
    /// </summary>
    Task<IReadOnlyCollection<PermissionType>> GetPermissionsByRiskLevelAsync(
        PermissionRiskLevel riskLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions that are implied by the specified permission.
    /// </summary>
    Task<IReadOnlyCollection<PermissionType>> GetImpliedPermissionsAsync(
        string permissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered permissions.
    /// </summary>
    Task<IReadOnlyCollection<PermissionType>> GetAllPermissionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches permissions by name or description.
    /// </summary>
    Task<IReadOnlyCollection<PermissionType>> SearchPermissionsAsync(
        string searchQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a permission ID exists and is registered.
    /// </summary>
    Task<bool> PermissionExistsAsync(
        string permissionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a registered permission type in the system.
/// </summary>
public record PermissionType(
    string Id,
    string Name,
    string Description,
    PermissionCategory Category,
    PermissionRiskLevel RiskLevel,
    PermissionScopeType DefaultScope,
    IReadOnlyCollection<string> ImpliedPermissions,
    PermissionMetadata Metadata,
    DateTimeOffset RegisteredAt,
    string? DeprecatedSince = null,
    string? DeprecationMessage = null);

/// <summary>
/// Detailed metadata about a permission.
/// </summary>
public record PermissionMetadata(
    string? LongDescription,
    IReadOnlyCollection<string> Examples,
    IReadOnlyCollection<string> SecurityWarnings,
    string? DocumentationUrl,
    bool RequiresElevatedReview,
    string? RequiredFeatureGate = null);

/// <summary>
/// Categories of permissions for organization and discovery.
/// </summary>
public enum PermissionCategory
{
    FileOperations = 0,
    NetworkAccess = 1,
    CodeExecution = 2,
    DataAnalysis = 3,
    SystemControl = 4,
    UserData = 5,
    ExternalServices = 6,
    AuditLogging = 7,
    AdminFunctions = 8
}

/// <summary>
/// Risk level assessment for permissions.
/// </summary>
public enum PermissionRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Scope types that can constrain permission applicability.
/// </summary>
public enum PermissionScopeType
{
    Global = 0,
    Project = 1,
    Document = 2,
    Resource = 3,
    Session = 4
}
```

#### 3.1.5 Acceptance Criteria

- [ ] `IPermissionRegistry` interface defined and documented with full XML docs
- [ ] Default registry implementation supports in-memory caching with 5-minute TTL
- [ ] At least 50 permissions defined across all categories
- [ ] Permission search returns results within 10ms
- [ ] All critical permissions marked with risk level High or Critical
- [ ] Permission hierarchy (implied permissions) correctly resolves transitive implications
- [ ] Registry loads and validates on startup with clear error messages for invalid entries
- [ ] Unit tests achieve 95%+ code coverage
- [ ] Performance benchmarks show <= 5ms for single permission lookup

#### 3.1.6 Dependencies

- `Lexichord.Abstractions` (v0.0.3b)
- `Microsoft.Extensions.Caching.Abstractions` (latest stable)
- `Microsoft.Extensions.Logging` (latest stable)

#### 3.1.7 Integration Points

- `PermissionRequestedEvent` from v0.18.1b
- Permission database schema from v0.18.1e

---

### 3.2 v0.18.1b: Permission Request Pipeline

**Estimated Hours**: 10 hours
**Sprint Assignment**: Week 1-2
**Swim Lane**: Security/Platform
**Status**: Pending Development

#### 3.2.1 Objective

Implement an asynchronous, extensible pipeline for requesting permissions, validating requests, and managing the request lifecycle from initiation through grant/denial.

#### 3.2.2 Scope

- Define `IPermissionRequestPipeline` interface with async request processing
- Implement core pipeline with middleware support for cross-cutting concerns
- Create permission request validation (syntax, existence, scope validity)
- Implement request context binding (user, session, resource, timestamp)
- Design request lifecycle state machine (Pending → Granted/Denied/Expired)
- Implement caching of recent permission decisions to reduce prompt fatigue
- Create escalation rules for critical permissions requiring special review
- Implement audit logging for all permission requests

#### 3.2.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IPermissionRequestPipeline` | Pipeline interface for processing requests |
| `PermissionRequestPipeline` | Default async implementation |
| `IPermissionRequestMiddleware` | Middleware extension point |
| `PermissionRequest` | Record representing a permission request |
| `PermissionRequestContext` | Context data for request processing |
| `IPermissionRequestValidator` | Request validation interface |
| `PermissionRequestHandler` | Core request processing logic |

#### 3.2.4 Interfaces & Records

```csharp
/// <summary>
/// Pipeline for processing permission requests with async middleware support.
/// </summary>
public interface IPermissionRequestPipeline
{
    /// <summary>
    /// Processes a permission request through the pipeline.
    /// </summary>
    /// <param name="request">The permission request to process</param>
    /// <param name="context">The request context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission response (granted, denied, or escalated)</returns>
    Task<PermissionRequestResponse> ProcessAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a middleware component in the pipeline.
    /// </summary>
    IPermissionRequestPipeline Use(IPermissionRequestMiddleware middleware);
}

/// <summary>
/// Middleware component for permission request pipeline.
/// </summary>
public interface IPermissionRequestMiddleware
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    Task InvokeAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        Func<Task> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a request for a permission.
/// </summary>
public record PermissionRequest(
    string RequestId,
    string PermissionId,
    string UserId,
    string SessionId,
    string? ResourceId = null,
    PermissionScopeType ScopeType = PermissionScopeType.Global,
    DateTimeOffset RequestedAt = default,
    string? Justification = null,
    Dictionary<string, object>? AdditionalContext = null)
{
    public PermissionRequest() : this(
        Guid.NewGuid().ToString(),
        string.Empty,
        string.Empty,
        string.Empty) { }
}

/// <summary>
/// Context information for processing a permission request.
/// </summary>
public record PermissionRequestContext(
    string UserId,
    string SessionId,
    string? ProjectId = null,
    string? DocumentId = null,
    string? ImpersonatedUserId = null,
    Dictionary<string, object>? ContextData = null,
    DateTimeOffset ProcessedAt = default);

/// <summary>
/// Response to a permission request.
/// </summary>
public record PermissionRequestResponse(
    string RequestId,
    PermissionRequestDecision Decision,
    string? GrantId = null,
    DateTimeOffset? ExpiresAt = null,
    string? DenialReason = null,
    string? EscalationReason = null)
{
    public bool IsGranted => Decision == PermissionRequestDecision.Granted;
    public bool IsDenied => Decision == PermissionRequestDecision.Denied;
    public bool IsEscalated => Decision == PermissionRequestDecision.Escalated;
}

/// <summary>
/// Decision outcomes for permission requests.
/// </summary>
public enum PermissionRequestDecision
{
    Pending = 0,
    Granted = 1,
    Denied = 2,
    Escalated = 3,
    Expired = 4,
    Revoked = 5
}

/// <summary>
/// Validates permission requests for syntax and validity.
/// </summary>
public interface IPermissionRequestValidator
{
    /// <summary>
    /// Validates a permission request.
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <param name="context">The request context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any errors</returns>
    Task<ValidationResult> ValidateAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of permission request validation.
/// </summary>
public record ValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> Errors = default!)
{
    public ValidationResult() : this(true, Array.Empty<string>()) { }
}
```

#### 3.2.5 Acceptance Criteria

- [ ] `IPermissionRequestPipeline` interface defined with full async support
- [ ] Pipeline processes requests within 100ms (excluding user decision time)
- [ ] Middleware architecture allows for extensibility without modifying core
- [ ] Request validation catches invalid permission IDs, missing context, scope violations
- [ ] Audit logging captures request ID, user, permission, context, and decision
- [ ] Recent decision caching reduces duplicate requests (2-hour TTL)
- [ ] Critical permissions are automatically escalated for review
- [ ] Unit tests cover all validation scenarios and pipeline middleware
- [ ] Integration tests verify end-to-end request processing

#### 3.2.6 Dependencies

- `v0.18.1a` (Permission Registry)
- `Lexichord.Abstractions` (v0.0.3b)
- `Microsoft.Extensions.Logging` (latest stable)

#### 3.2.7 Integration Points

- `IPermissionRegistry` from v0.18.1a
- `IConsentDialogService` from v0.18.1c
- `IPermissionScopeManager` from v0.18.1d

---

### 3.3 v0.18.1c: User Consent Dialog System

**Estimated Hours**: 10 hours
**Sprint Assignment**: Week 2-3
**Swim Lane**: Security/UI
**Status**: Pending Development

#### 3.3.1 Objective

Implement a comprehensive user consent dialog system that presents permission requests to users in clear, actionable ways, captures user decisions, and provides granular control over scopes and conditions.

#### 3.3.2 Scope

- Define `IConsentDialogService` interface for rendering and capturing decisions
- Implement multiple dialog presentation modes (Modal, Sidebar, Banner, Notification)
- Create permission explanation templates for different risk levels
- Implement scope customization UI (allow users to narrow scope, set expiry)
- Design decision history tracking (show previous decisions on same permission)
- Create permission diff visualization (what's new vs. previous grants)
- Implement accessibility features (ARIA labels, keyboard navigation, screen reader support)
- Design mobile-responsive layouts for consent dialogs

#### 3.3.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IConsentDialogService` | Interface for rendering consent dialogs |
| `ConsentDialogService` | Default implementation (async) |
| `ConsentDialogModel` | ViewModel for dialog presentation |
| `ConsentDecision` | Record capturing user decision |
| `DialogPresentationMode` | Enum: Modal, Sidebar, Banner |
| `PermissionExplanation` | Record with risk-appropriate explanation |
| Consent dialog React components | UI components for consent flows |

#### 3.3.4 Interfaces & Records

```csharp
/// <summary>
/// Service for rendering permission consent dialogs and capturing user decisions.
/// </summary>
public interface IConsentDialogService
{
    /// <summary>
    /// Presents a permission request dialog to the user.
    /// </summary>
    /// <param name="request">The permission request</param>
    /// <param name="permissionType">The permission metadata</param>
    /// <param name="mode">Dialog presentation mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's consent decision</returns>
    Task<ConsentDecision> ShowConsentDialogAsync(
        PermissionRequest request,
        PermissionType permissionType,
        DialogPresentationMode mode = DialogPresentationMode.Modal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a batch consent dialog for multiple permission requests.
    /// </summary>
    /// <param name="requests">The permission requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decisions for each request</returns>
    Task<IReadOnlyDictionary<string, ConsentDecision>> ShowBatchConsentDialogAsync(
        IReadOnlyCollection<PermissionRequest> requests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets previously made consent decisions for a permission.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="permissionId">Permission identifier</param>
    /// <param name="limit">Maximum decisions to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Previous decisions in reverse chronological order</returns>
    Task<IReadOnlyCollection<ConsentDecision>> GetDecisionHistoryAsync(
        string userId,
        string permissionId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a pending consent dialog.
    /// </summary>
    Task<bool> DismissDialogAsync(
        string requestId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a user's consent decision for a permission.
/// </summary>
public record ConsentDecision(
    string DecisionId,
    string RequestId,
    string UserId,
    string PermissionId,
    ConsentChoice Choice,
    DateTimeOffset DecidedAt,
    string? ScopedResourceId = null,
    PermissionScopeType? ScopedScopeType = null,
    DateTimeOffset? ExpiresAt = null,
    Dictionary<string, object>? Conditions = null,
    string? Notes = null);

/// <summary>
/// User's consent choice for a permission.
/// </summary>
public enum ConsentChoice
{
    Pending = 0,
    Granted = 1,
    Denied = 2,
    GrantedOnce = 3,
    DeniedOnce = 4
}

/// <summary>
/// Modes for presenting consent dialogs.
/// </summary>
public enum DialogPresentationMode
{
    Modal = 0,
    Sidebar = 1,
    Banner = 2,
    Notification = 3,
    InlineAlert = 4
}

/// <summary>
/// Explains a permission to users in appropriate language.
/// </summary>
public record PermissionExplanation(
    string PermissionId,
    string SimpleSummary,
    string DetailedDescription,
    string? RiskWarning = null,
    IReadOnlyCollection<string>? Examples = null,
    IReadOnlyCollection<string>? Alternatives = null);

/// <summary>
/// Model for rendering a consent dialog.
/// </summary>
public record ConsentDialogModel(
    PermissionRequest Request,
    PermissionType Permission,
    PermissionExplanation Explanation,
    IReadOnlyCollection<ConsentDecision>? DecisionHistory = null,
    DialogPresentationMode PresentationMode = DialogPresentationMode.Modal,
    bool AllowScopeCustomization = true,
    bool AllowExpiryCustomization = true);
```

#### 3.3.5 Acceptance Criteria

- [ ] Dialog renders within 100ms of request
- [ ] All permission types have appropriate explanations
- [ ] Risk level determines visual styling (colors, emphasis, warning text)
- [ ] Users can customize scope and set expiry times
- [ ] Decision history shows up to 5 previous decisions
- [ ] Batch dialogs support up to 10 simultaneous permission requests
- [ ] Accessibility audit passes WCAG 2.1 AA standard
- [ ] Mobile layouts tested on iOS and Android devices
- [ ] All UI components have comprehensive unit tests
- [ ] End-to-end tests verify dialog rendering and decision capture

#### 3.3.6 Dependencies

- `v0.18.1a` (Permission Registry)
- `v0.18.1b` (Permission Request Pipeline)
- `Lexichord.Abstractions` (v0.0.3b)
- React.js (for dialog components)

#### 3.3.7 Integration Points

- `IPermissionRegistry` from v0.18.1a
- `PermissionRequest` and `PermissionRequestResponse` from v0.18.1b
- `IPermissionGrantStore` from v0.18.1e

---

### 3.4 v0.18.1d: Permission Scope Manager

**Estimated Hours**: 10 hours
**Sprint Assignment**: Week 3-4
**Swim Lane**: Security/Platform
**Status**: Pending Development

#### 3.4.1 Objective

Implement a sophisticated scope manager that allows permissions to be scoped to specific projects, documents, resources, or time windows, with support for scope composition, hierarchical constraints, and context-aware evaluation.

#### 3.4.2 Scope

- Define `IPermissionScopeManager` interface for scope creation and evaluation
- Implement scope constraints (Resource, Project, Document, TimeWindow)
- Create scope hierarchy and inheritance rules
- Design scope composition (AND, OR, NOT logic)
- Implement context-aware scope evaluation
- Create scope templates for common patterns
- Support for scope "narrowing" (granting broader permission, using narrower scope)
- Implement scope validation against actual resources

#### 3.4.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IPermissionScopeManager` | Interface for scope operations |
| `PermissionScopeManager` | Default implementation |
| `PermissionScope` | Record defining scope constraints |
| `ScopeConstraint` | Union type for constraint kinds |
| `ResourceScope` | Scope to specific resource |
| `ProjectScope` | Scope to specific project |
| `TimeWindowScope` | Scope with time boundaries |
| `ScopeEvaluationContext` | Context for evaluating scopes |

#### 3.4.4 Interfaces & Records

```csharp
/// <summary>
/// Manages permission scopes, including creation, composition, and evaluation.
/// </summary>
public interface IPermissionScopeManager
{
    /// <summary>
    /// Creates a new permission scope with specified constraints.
    /// </summary>
    /// <param name="constraints">The scope constraints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created scope</returns>
    Task<PermissionScope> CreateScopeAsync(
        IReadOnlyCollection<ScopeConstraint> constraints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a grant with the given scope applies to a context.
    /// </summary>
    /// <param name="scope">The permission scope</param>
    /// <param name="context">The evaluation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the scope applies to the context</returns>
    Task<bool> EvaluateScopeAsync(
        PermissionScope scope,
        ScopeEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Narrows an existing scope with additional constraints.
    /// </summary>
    /// <param name="scope">The original scope</param>
    /// <param name="additionalConstraints">Constraints to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The narrowed scope</returns>
    Task<PermissionScope> NarrowScopeAsync(
        PermissionScope scope,
        IReadOnlyCollection<ScopeConstraint> additionalConstraints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Combines multiple scopes with OR logic (union of applicability).
    /// </summary>
    Task<PermissionScope> CombineScopesAsync(
        IReadOnlyCollection<PermissionScope> scopes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scope templates for common patterns.
    /// </summary>
    Task<IReadOnlyCollection<ScopesTemplate>> GetScopeTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a scope against system constraints.
    /// </summary>
    Task<ScopeValidationResult> ValidateScopeAsync(
        PermissionScope scope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a permission scope with constraints.
/// </summary>
public record PermissionScope(
    string ScopeId,
    IReadOnlyCollection<ScopeConstraint> Constraints,
    DateTimeOffset CreatedAt,
    ScopeCompositionMode CompositionMode = ScopeCompositionMode.And);

/// <summary>
/// Union type for different scope constraint kinds.
/// </summary>
public abstract record ScopeConstraint
{
    public abstract string ConstraintType { get; }
}

/// <summary>
/// Constraint scoping permission to a specific resource.
/// </summary>
public record ResourceScopeConstraint(
    string ResourceId,
    string ResourceType) : ScopeConstraint
{
    public override string ConstraintType => "Resource";
}

/// <summary>
/// Constraint scoping permission to a specific project.
/// </summary>
public record ProjectScopeConstraint(
    string ProjectId) : ScopeConstraint
{
    public override string ConstraintType => "Project";
}

/// <summary>
/// Constraint scoping permission to a specific document.
/// </summary>
public record DocumentScopeConstraint(
    string DocumentId) : ScopeConstraint
{
    public override string ConstraintType => "Document";
}

/// <summary>
/// Constraint scoping permission to a time window.
/// </summary>
public record TimeWindowScopeConstraint(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime) : ScopeConstraint
{
    public override string ConstraintType => "TimeWindow";
}

/// <summary>
/// Constraint scoping permission to a user's session.
/// </summary>
public record SessionScopeConstraint(
    string SessionId) : ScopeConstraint
{
    public override string ConstraintType => "Session";
}

/// <summary>
/// Context for evaluating scope applicability.
/// </summary>
public record ScopeEvaluationContext(
    string UserId,
    string SessionId,
    DateTimeOffset EvaluatedAt,
    string? CurrentResourceId = null,
    string? CurrentProjectId = null,
    string? CurrentDocumentId = null,
    Dictionary<string, object>? AdditionalContext = null);

/// <summary>
/// How multiple constraints are combined in scope evaluation.
/// </summary>
public enum ScopeCompositionMode
{
    And = 0,  // All constraints must be satisfied
    Or = 1    // Any constraint can be satisfied
}

/// <summary>
/// Result of scope validation.
/// </summary>
public record ScopeValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> Warnings = default!)
{
    public ScopeValidationResult() : this(true, Array.Empty<string>()) { }
}

/// <summary>
/// Template for common scope patterns.
/// </summary>
public record ScopesTemplate(
    string TemplateId,
    string Name,
    string Description,
    IReadOnlyCollection<ScopeConstraint> Constraints);
```

#### 3.4.5 Acceptance Criteria

- [ ] Scope evaluation completes within 5ms for typical constraints
- [ ] All scope constraint types are supported and validated
- [ ] Scope narrowing preserves original constraints and adds new ones
- [ ] Context-aware evaluation correctly applies time window constraints
- [ ] At least 10 scope templates provided for common use cases
- [ ] Scope composition (AND, OR) works correctly
- [ ] Validation detects invalid resource IDs, expired time windows, circular constraints
- [ ] Unit tests cover all constraint types and composition modes
- [ ] Integration tests verify scope evaluation with real context data

#### 3.4.6 Dependencies

- `v0.18.1a` (Permission Registry)
- `Lexichord.Abstractions` (v0.0.3b)

#### 3.4.7 Integration Points

- `ScopeEvaluationContext` used in permission checks
- `PermissionScope` stored in permission grants (v0.18.1e)

---

### 3.5 v0.18.1e: Grant Persistence & Storage

**Estimated Hours**: 8 hours
**Sprint Assignment**: Week 4-5
**Swim Lane**: Data/Platform
**Status**: Pending Development

#### 3.5.1 Objective

Implement persistent storage for permission grants in PostgreSQL, with support for efficient querying, grant lifecycle management, and historical tracking.

#### 3.5.2 Scope

- Define `IPermissionGrantStore` interface for grant CRUD operations
- Implement grant storage in PostgreSQL with optimized schema
- Create indexes for common queries (user, permission, status)
- Implement grant lifecycle (Active, Expired, Revoked, Superseded)
- Design historical tracking (who granted, when, why)
- Create audit trail with full event logging
- Implement batch operations for performance
- Support for soft deletes and data retention policies

#### 3.5.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IPermissionGrantStore` | Interface for grant persistence |
| `PermissionGrantStore` | PostgreSQL implementation |
| `PermissionGrant` | Record representing a grant |
| `GrantLifecycleStatus` | Enum: Active, Expired, Revoked, Superseded |
| `GrantAuditEntry` | Record for audit trail |
| Database schema | PostgreSQL tables and indexes |

#### 3.5.4 Interfaces & Records

```csharp
/// <summary>
/// Stores and retrieves permission grants from persistent storage.
/// </summary>
public interface IPermissionGrantStore
{
    /// <summary>
    /// Creates a new permission grant.
    /// </summary>
    /// <param name="grant">The grant to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created grant with ID assigned</returns>
    Task<PermissionGrant> CreateGrantAsync(
        PermissionGrant grant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a grant by ID.
    /// </summary>
    /// <param name="grantId">The grant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The grant, or null if not found</returns>
    Task<PermissionGrant?> GetGrantAsync(
        string grantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active grants for a user.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All active grants for the user</returns>
    Task<IReadOnlyCollection<PermissionGrant>> GetActiveGrantsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all grants for a user for a specific permission.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="permissionId">Permission identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All grants matching the criteria</returns>
    Task<IReadOnlyCollection<PermissionGrant>> GetGrantsAsync(
        string userId,
        string permissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates grant status (e.g., revoke, expire).
    /// </summary>
    /// <param name="grantId">Grant identifier</param>
    /// <param name="newStatus">New lifecycle status</param>
    /// <param name="reason">Reason for status change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated grant</returns>
    Task<PermissionGrant> UpdateGrantStatusAsync(
        string grantId,
        GrantLifecycleStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the audit trail for a grant.
    /// </summary>
    Task<IReadOnlyCollection<GrantAuditEntry>> GetAuditTrailAsync(
        string grantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a grant exists and is active.
    /// </summary>
    Task<bool> GrantExistsAsync(
        string userId,
        string permissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes grants matching criteria (soft delete with retention).
    /// </summary>
    Task DeleteGrantsAsync(
        string userId,
        string? permissionId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a permission grant to a user.
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
/// Lifecycle status of a permission grant.
/// </summary>
public enum GrantLifecycleStatus
{
    Active = 0,
    Expired = 1,
    Revoked = 2,
    Superseded = 3,
    Pending = 4
}

/// <summary>
/// Audit entry for grant lifecycle events.
/// </summary>
public record GrantAuditEntry(
    string EntryId,
    string GrantId,
    GrantLifecycleStatus StatusChange,
    DateTimeOffset Timestamp,
    string ActorId,
    string ActionType,
    string? Reason = null,
    Dictionary<string, object>? Details = null);
```

#### 3.5.5 Database Schema (PostgreSQL)

```sql
-- Permission Grants table
CREATE TABLE permission_grants (
    grant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(255) NOT NULL,
    permission_id VARCHAR(255) NOT NULL,
    scope_id UUID NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    granted_by VARCHAR(255) NOT NULL,
    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    revocation_reason TEXT,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMPTZ,
    CONSTRAINT fk_scope_id FOREIGN KEY (scope_id) REFERENCES permission_scopes(scope_id)
);

CREATE INDEX idx_permission_grants_user_id ON permission_grants(user_id);
CREATE INDEX idx_permission_grants_permission_id ON permission_grants(permission_id);
CREATE INDEX idx_permission_grants_status ON permission_grants(status);
CREATE INDEX idx_permission_grants_user_permission ON permission_grants(user_id, permission_id);
CREATE INDEX idx_permission_grants_expires_at ON permission_grants(expires_at);
CREATE INDEX idx_permission_grants_is_deleted ON permission_grants(is_deleted);

-- Permission Scopes table
CREATE TABLE permission_scopes (
    scope_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    constraints JSONB NOT NULL,
    composition_mode INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Grant Audit Trail table
CREATE TABLE grant_audit_entries (
    entry_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    grant_id UUID NOT NULL,
    status_change INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    actor_id VARCHAR(255) NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    reason TEXT,
    details JSONB,
    CONSTRAINT fk_grant_id FOREIGN KEY (grant_id) REFERENCES permission_grants(grant_id)
);

CREATE INDEX idx_grant_audit_grant_id ON grant_audit_entries(grant_id);
CREATE INDEX idx_grant_audit_timestamp ON grant_audit_entries(timestamp);
```

#### 3.5.6 Acceptance Criteria

- [ ] All CRUD operations complete within 50ms (excluding network latency)
- [ ] Indexes optimize common queries (user, permission, status)
- [ ] Soft deletes preserve audit trail and support data retention
- [ ] Batch operations support up to 1,000 grants
- [ ] Audit trail captures all status changes with actor and reason
- [ ] Grant expiry is detected and cleaned up automatically
- [ ] Schema supports scope storage without serialization issues
- [ ] Unit tests mock storage with in-memory implementation
- [ ] Integration tests use test PostgreSQL database

#### 3.5.7 Dependencies

- `v0.18.1a` (Permission Registry)
- `v0.18.1d` (Permission Scope Manager)
- `Npgsql` (PostgreSQL driver)
- `Dapper` or Entity Framework Core

#### 3.5.8 Integration Points

- Permission grants created after consent (v0.18.1c)
- Grants queried during permission checks
- Revocation updates (v0.18.1f)

---

### 3.6 v0.18.1f: Permission Revocation & Expiry

**Estimated Hours**: 6 hours
**Sprint Assignment**: Week 5
**Swim Lane**: Security/Platform
**Status**: Pending Development

#### 3.6.1 Objective

Implement immediate revocation and temporal expiry management for permissions, ensuring that users can revoke permissions at any time and that expired permissions are automatically removed from active grants.

#### 3.6.2 Scope

- Define `IPermissionRevocationService` interface
- Implement immediate revocation with audit logging
- Create background job for expiry detection and cleanup
- Design revocation reasons and categorization
- Implement cascading revocation (revoking parent revokes children)
- Create revocation rollback capability (undo recent revocations)
- Implement revocation notifications to users
- Support for selective grant revocation (revoke only specific scope)

#### 3.6.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IPermissionRevocationService` | Revocation interface |
| `PermissionRevocationService` | Default implementation |
| `RevocationReason` | Enum: UserRequested, SecurityIncident, SystemUpdate, etc. |
| `ExpiryCleanupJob` | Background job for expiry processing |
| `RevocationNotification` | Event for revocation notifications |

#### 3.6.4 Interfaces & Records

```csharp
/// <summary>
/// Service for revoking permissions and managing expiry.
/// </summary>
public interface IPermissionRevocationService
{
    /// <summary>
    /// Immediately revokes a permission grant.
    /// </summary>
    /// <param name="grantId">The grant to revoke</param>
    /// <param name="reason">The revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if revocation was successful</returns>
    Task<bool> RevokeGrantAsync(
        string grantId,
        RevocationReason reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all grants for a user for a specific permission.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="permissionId">Permission identifier</param>
    /// <param name="reason">The revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of grants revoked</returns>
    Task<int> RevokeUserPermissionAsync(
        string userId,
        string permissionId,
        RevocationReason reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all grants for a user.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="reason">The revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of grants revoked</returns>
    Task<int> RevokeAllUserGrantsAsync(
        string userId,
        RevocationReason reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes a recent revocation (restores grant).
    /// </summary>
    /// <param name="grantId">The grant to restore</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The restored grant, or null if not possible</returns>
    Task<PermissionGrant?> UndoRevocationAsync(
        string grantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes expired grants and updates their status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of grants expired</returns>
    Task<int> ProcessExpiredGrantsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revocation history for a grant.
    /// </summary>
    Task<IReadOnlyCollection<RevocationRecord>> GetRevocationHistoryAsync(
        string grantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reason for revoking a permission.
/// </summary>
public enum RevocationReason
{
    UserRequested = 0,
    SecurityIncident = 1,
    SystemUpdate = 2,
    ComplianceRequirement = 3,
    RoleChange = 4,
    ProjectCompletion = 5,
    AdminAction = 6
}

/// <summary>
/// Record of a revocation action.
/// </summary>
public record RevocationRecord(
    string GrantId,
    RevocationReason Reason,
    DateTimeOffset RevokedAt,
    string RevokedBy,
    bool CanUndo,
    DateTimeOffset? UndoExpiresAt = null);

/// <summary>
/// Event published when a permission is revoked.
/// </summary>
public record PermissionRevokedEvent(
    string GrantId,
    string UserId,
    string PermissionId,
    RevocationReason Reason,
    DateTimeOffset RevokedAt) : INotification;
```

#### 3.6.5 Acceptance Criteria

- [ ] Revocation completes within 100ms
- [ ] Revoked grants are immediately excluded from permission checks
- [ ] Expiry cleanup job runs hourly and processes 100+ expired grants per second
- [ ] Revocation reasons are properly categorized and logged
- [ ] Cascading revocation works for hierarchical permissions
- [ ] Undo capability available for 24 hours after revocation
- [ ] Revocation notifications sent to users
- [ ] Unit tests cover all revocation scenarios
- [ ] Integration tests verify expiry cleanup job

#### 3.6.6 Dependencies

- `v0.18.1a` (Permission Registry)
- `v0.18.1e` (Grant Persistence)
- `Hangfire` or similar background job framework

#### 3.6.7 Integration Points

- `PermissionGrant` update via `IPermissionGrantStore`
- `PermissionRevokedEvent` published to MediatR

---

### 3.7 v0.18.1g: Permission Inheritance & Delegation

**Estimated Hours**: 4 hours
**Sprint Assignment**: Week 5
**Swim Lane**: Security/Platform
**Status**: Pending Development

#### 3.7.1 Objective

Implement permission inheritance hierarchies and delegation patterns, enabling administrators to delegate permissions to other users and supporting cascading permission implications.

#### 3.7.2 Scope

- Define `IPermissionDelegationService` interface
- Implement temporary delegation (time-bounded)
- Create delegation tracking and history
- Support for delegation revocation
- Implement permission inheritance chains
- Design delegation constraints (what can be delegated)
- Create delegation approval workflows for sensitive permissions
- Support for sub-delegation depth limits

#### 3.7.3 Key Artifacts

| Artifact | Description |
| :--- | :--- |
| `IPermissionDelegationService` | Delegation interface |
| `PermissionDelegation` | Record representing a delegation |
| `DelegationMetadata` | Record for delegation details |

#### 3.7.4 Interfaces & Records

```csharp
/// <summary>
/// Service for delegating permissions to other users.
/// </summary>
public interface IPermissionDelegationService
{
    /// <summary>
    /// Delegates a permission from one user to another.
    /// </summary>
    /// <param name="fromUserId">User delegating the permission</param>
    /// <param name="toUserId">User receiving the delegation</param>
    /// <param name="permissionId">Permission to delegate</param>
    /// <param name="scope">Scope of delegation</param>
    /// <param name="expiresAt">When delegation expires</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The delegation record</returns>
    Task<PermissionDelegation> DelegatePermissionAsync(
        string fromUserId,
        string toUserId,
        string permissionId,
        PermissionScope scope,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a delegation.
    /// </summary>
    Task<bool> RevokeDelegationAsync(
        string delegationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all delegations granted by a user.
    /// </summary>
    Task<IReadOnlyCollection<PermissionDelegation>> GetDelegationsGrantedAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all delegations received by a user.
    /// </summary>
    Task<IReadOnlyCollection<PermissionDelegation>> GetDelegationsReceivedAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a delegation is valid and active.
    /// </summary>
    Task<bool> IsDelegationValidAsync(
        string delegationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a permission delegation from one user to another.
/// </summary>
public record PermissionDelegation(
    string DelegationId,
    string FromUserId,
    string ToUserId,
    string PermissionId,
    PermissionScope Scope,
    DateTimeOffset DelegatedAt,
    DateTimeOffset? ExpiresAt = null,
    DateTimeOffset? RevokedAt = null,
    int DelegationDepth = 0);
```

#### 3.7.5 Acceptance Criteria

- [ ] Delegation creates grants for recipient with delegation metadata
- [ ] Delegations respect scope constraints
- [ ] Time-bounded delegations expire automatically
- [ ] Sub-delegation depth is limited (max 3 levels)
- [ ] Revocation of delegation removes all sub-delegations
- [ ] Delegation history is maintained for audit purposes
- [ ] Unit tests cover delegation chains and expiry
- [ ] Integration tests verify inheritance resolution

#### 3.7.6 Dependencies

- `v0.18.1a` (Permission Registry)
- `v0.18.1e` (Grant Persistence)
- `v0.18.1f` (Revocation Service)

#### 3.7.7 Integration Points

- Creates `PermissionGrant` records via `IPermissionGrantStore`
- Uses `PermissionRevokedEvent` for cascade handling

---

## 4. Complete C# Interfaces & Type Definitions

### 4.1 Core Permission Manager Interface

```csharp
/// <summary>
/// Central permission management service orchestrating all permission operations.
/// This is the primary facade for permission-related operations.
/// </summary>
public interface IPermissionManager
{
    /// <summary>
    /// Requests a permission from the user.
    /// </summary>
    /// <param name="request">The permission request</param>
    /// <param name="context">The request context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission response</returns>
    Task<PermissionRequestResponse> RequestPermissionAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission in a given context.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="permissionId">Permission identifier</param>
    /// <param name="context">The evaluation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission</returns>
    Task<bool> HasPermissionAsync(
        string userId,
        string permissionId,
        ScopeEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active permissions for a user.
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All active grants</returns>
    Task<IReadOnlyCollection<PermissionGrant>> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission grant.
    /// </summary>
    /// <param name="grantId">Grant identifier</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> RevokePermissionAsync(
        string grantId,
        RevocationReason reason,
        CancellationToken cancellationToken = default);
}
```

### 4.2 MediatR Events

```csharp
/// <summary>
/// Published when a permission is requested.
/// </summary>
public record PermissionRequestedEvent(
    string RequestId,
    string UserId,
    string PermissionId,
    DateTimeOffset RequestedAt) : INotification;

/// <summary>
/// Published when a permission is granted.
/// </summary>
public record PermissionGrantedEvent(
    string GrantId,
    string UserId,
    string PermissionId,
    PermissionScope Scope,
    DateTimeOffset GrantedAt,
    string GrantedBy) : INotification;

/// <summary>
/// Published when a permission is denied.
/// </summary>
public record PermissionDeniedEvent(
    string RequestId,
    string UserId,
    string PermissionId,
    string DenialReason,
    DateTimeOffset DeniedAt) : INotification;

/// <summary>
/// Published when a permission is revoked.
/// </summary>
public record PermissionRevokedEvent(
    string GrantId,
    string UserId,
    string PermissionId,
    RevocationReason Reason,
    DateTimeOffset RevokedAt) : INotification;

/// <summary>
/// Published when a permission grant expires.
/// </summary>
public record PermissionExpiredEvent(
    string GrantId,
    string UserId,
    string PermissionId,
    DateTimeOffset ExpiredAt) : INotification;

/// <summary>
/// Published when permissions are delegated.
/// </summary>
public record PermissionDelegatedEvent(
    string DelegationId,
    string FromUserId,
    string ToUserId,
    string PermissionId,
    DateTimeOffset DelegatedAt) : INotification;
```

---

## 5. Architecture Diagrams

### 5.1 Permission Framework Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      LEXICHORD PERMISSION FRAMEWORK                         │
└─────────────────────────────────────────────────────────────────────────────┘

                          ┌──────────────────────┐
                          │  User Interaction    │
                          │  (App/Web Client)    │
                          └──────────┬───────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    ▼                ▼                ▼
            ┌───────────────┐  ┌─────────────┐  ┌──────────────┐
            │ Permission    │  │  Consent    │  │ Permission   │
            │ Request       │  │  Dialog     │  │ Management   │
            │ Pipeline      │  │  Service    │  │ Dashboard    │
            │ (v0.18.1b)    │  │ (v0.18.1c)  │  │              │
            └───────┬───────┘  └──────┬──────┘  └────────┬─────┘
                    │                 │                   │
                    └─────────────────┼───────────────────┘
                                      │
                          ┌───────────▼──────────┐
                          │  IPermissionManager  │
                          │   (Core Facade)      │
                          └───────────┬──────────┘
                                      │
                ┌─────────────────────┼─────────────────────┐
                ▼                     ▼                     ▼
        ┌──────────────────┐   ┌──────────────────┐   ┌─────────────────┐
        │ Permission       │   │ Permission Scope │   │ Revocation &    │
        │ Registry         │   │ Manager          │   │ Expiry Service  │
        │ (v0.18.1a)       │   │ (v0.18.1d)       │   │ (v0.18.1f)      │
        └──────────┬───────┘   └──────────┬───────┘   └────────┬────────┘
                   │                      │                    │
                   └──────────┬───────────┴────────────────────┘
                              │
                    ┌─────────▼──────────┐
                    │ Permission Grant   │
                    │ Store              │
                    │ (v0.18.1e)         │
                    │ PostgreSQL DB      │
                    └────────┬───────────┘
                             │
            ┌────────────────┼────────────────┐
            ▼                ▼                ▼
    ┌─────────────────┐ ┌──────────────┐ ┌──────────────┐
    │ Audit Trail     │ │ Grant History │ │ Revocation   │
    │ Tables          │ │ Tables       │ │ Records      │
    └─────────────────┘ └──────────────┘ └──────────────┘
```

### 5.2 Permission Request Flow

```
User Action
    │
    ▼
┌─────────────────────────────────┐
│ Permission Required              │
│ (Code calls IPermissionManager)  │
└────────────────┬────────────────┘
                 │
                 ▼
        ┌────────────────────┐
        │ IPermissionManager  │
        │ .RequestPermission  │
        └────────┬───────────┘
                 │
                 ▼
    ┌────────────────────────────┐
    │ IPermissionRequestPipeline  │
    │ .ProcessAsync               │
    └────────────┬────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
    ┌──────────┐   ┌──────────────────┐
    │ Validate │   │ Check Recent     │
    │ Request  │   │ Decisions Cache  │
    └────┬─────┘   └─────────┬────────┘
         │                   │
         │        ┌──────────┘
         │        ▼
         │    ┌────────────┐
         │    │ Return     │
         │    │ Cached     │
         │    │ Decision   │
         │    └────────────┘
         │
         ▼
    ┌──────────────────────┐
    │ Check if Already     │
    │ Granted              │
    └────────┬─────────────┘
             │
       ┌─────┴─────┐
       │ Yes       │ No
       ▼           ▼
   ┌────────┐   ┌──────────────────────┐
   │ Return │   │ Show Consent Dialog   │
   │ Granted   │ IConsentDialogService │
   └────────┘   └────────┬─────────────┘
                         │
                    ┌────┴────┐
                    ▼         ▼
                ┌─────┐  ┌──────┐
                │User │  │User  │
                │ OK  │  │Deny  │
                └──┬──┘  └───┬──┘
                   │        │
                   ▼        ▼
            ┌──────────────────────┐
            │ Create Permission    │
            │ Grant / Denial       │
            │ in Store             │
            └────────┬─────────────┘
                     │
                     ▼
            ┌──────────────────┐
            │ Publish Event    │
            │ PermissionGranted│
            │ PermissionDenied │
            └────────┬─────────┘
                     │
                     ▼
            ┌──────────────────┐
            │ Return Response  │
            │ to Caller        │
            └──────────────────┘
```

### 5.3 Permission Check Flow

```
Code: HasPermissionAsync(userId, permissionId, context)
                         │
                         ▼
                ┌─────────────────┐
                │ Get Active      │
                │ Grants for User │
                └────────┬────────┘
                         │
                         ▼
                ┌─────────────────┐
                │ Filter by       │
                │ Permission ID   │
                └────────┬────────┘
                         │
                    ┌────┴────┐
                    ▼         ▼
                ┌────────┐  ┌──────────┐
                │ Found? │  │ No Match │
                │ Yes    │  │ Return   │
                └───┬────┘  │ False    │
                    │       └──────────┘
                    ▼
            ┌─────────────────────┐
            │ Evaluate Each Grant │
            │ Scope Against       │
            │ Context             │
            └────────┬────────────┘
                     │
            ┌────────┴────────┐
            ▼                 ▼
        ┌────────┐       ┌──────────┐
        │ Scope  │       │ Scope    │
        │ Match? │       │ No Match │
        │ Yes    │       │          │
        └───┬────┘       └──────────┘
            │
            ▼
        ┌──────────────┐
        │ Check if     │
        │ Grant Active │
        │ (not expired)│
        └────┬────────┘
             │
        ┌────┴─────┐
        ▼          ▼
     ┌────┐   ┌─────────┐
     │Yes │   │No/Error │
     └─┬──┘   └─────────┘
       │
       ▼
    ┌──────────────┐
    │ Return True  │
    │ Permission   │
    │ Granted      │
    └──────────────┘
```

---

## 6. PostgreSQL Database Schema

```sql
-- ============================================================================
-- Permission Framework Database Schema
-- ============================================================================

-- Permission Types Table (Reference Data)
CREATE TABLE permission_types (
    permission_id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    category INTEGER NOT NULL,  -- PermissionCategory enum
    risk_level INTEGER NOT NULL,  -- PermissionRiskLevel enum
    default_scope_type INTEGER NOT NULL,  -- PermissionScopeType enum
    implied_permissions JSONB,  -- Array of permission IDs
    metadata JSONB NOT NULL,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deprecated_since TIMESTAMPTZ,
    deprecation_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_permission_types_category ON permission_types(category);
CREATE INDEX idx_permission_types_risk_level ON permission_types(risk_level);

-- Permission Scopes Table
CREATE TABLE permission_scopes (
    scope_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    constraints JSONB NOT NULL,
    composition_mode INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_permission_scopes_created_at ON permission_scopes(created_at);

-- Permission Grants Table
CREATE TABLE permission_grants (
    grant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(255) NOT NULL,
    permission_id VARCHAR(255) NOT NULL,
    scope_id UUID NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,  -- GrantLifecycleStatus enum
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    granted_by VARCHAR(255) NOT NULL,
    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    revocation_reason TEXT,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMPTZ,
    CONSTRAINT fk_permission_types FOREIGN KEY (permission_id)
        REFERENCES permission_types(permission_id),
    CONSTRAINT fk_permission_scopes FOREIGN KEY (scope_id)
        REFERENCES permission_scopes(scope_id)
);

CREATE INDEX idx_permission_grants_user_id ON permission_grants(user_id);
CREATE INDEX idx_permission_grants_permission_id ON permission_grants(permission_id);
CREATE INDEX idx_permission_grants_status ON permission_grants(status);
CREATE INDEX idx_permission_grants_user_permission ON permission_grants(user_id, permission_id);
CREATE INDEX idx_permission_grants_expires_at ON permission_grants(expires_at);
CREATE INDEX idx_permission_grants_is_deleted ON permission_grants(is_deleted);
CREATE INDEX idx_permission_grants_revoked_at ON permission_grants(revoked_at);

-- Consent Decisions Table
CREATE TABLE consent_decisions (
    decision_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id VARCHAR(255) NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    permission_id VARCHAR(255) NOT NULL,
    choice INTEGER NOT NULL,  -- ConsentChoice enum
    decided_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    scoped_resource_id VARCHAR(255),
    scoped_scope_type INTEGER,
    expires_at TIMESTAMPTZ,
    conditions JSONB,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_consent_decisions_user_id ON consent_decisions(user_id);
CREATE INDEX idx_consent_decisions_permission_id ON consent_decisions(permission_id);
CREATE INDEX idx_consent_decisions_decided_at ON consent_decisions(decided_at);

-- Grant Audit Trail Table
CREATE TABLE grant_audit_entries (
    entry_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    grant_id UUID NOT NULL,
    status_change INTEGER NOT NULL,  -- GrantLifecycleStatus enum
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    actor_id VARCHAR(255) NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    reason TEXT,
    details JSONB,
    CONSTRAINT fk_grant_id FOREIGN KEY (grant_id)
        REFERENCES permission_grants(grant_id) ON DELETE CASCADE
);

CREATE INDEX idx_grant_audit_entries_grant_id ON grant_audit_entries(grant_id);
CREATE INDEX idx_grant_audit_entries_timestamp ON grant_audit_entries(timestamp);
CREATE INDEX idx_grant_audit_entries_actor_id ON grant_audit_entries(actor_id);

-- Permission Delegations Table
CREATE TABLE permission_delegations (
    delegation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    from_user_id VARCHAR(255) NOT NULL,
    to_user_id VARCHAR(255) NOT NULL,
    permission_id VARCHAR(255) NOT NULL,
    scope_id UUID NOT NULL,
    delegated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    delegation_depth INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_from_user FOREIGN KEY (from_user_id) REFERENCES users(user_id),
    CONSTRAINT fk_to_user FOREIGN KEY (to_user_id) REFERENCES users(user_id),
    CONSTRAINT fk_permission_types FOREIGN KEY (permission_id)
        REFERENCES permission_types(permission_id),
    CONSTRAINT fk_scope FOREIGN KEY (scope_id)
        REFERENCES permission_scopes(scope_id)
);

CREATE INDEX idx_permission_delegations_from_user ON permission_delegations(from_user_id);
CREATE INDEX idx_permission_delegations_to_user ON permission_delegations(to_user_id);
CREATE INDEX idx_permission_delegations_expires_at ON permission_delegations(expires_at);
CREATE INDEX idx_permission_delegations_revoked_at ON permission_delegations(revoked_at);

-- Permission Request Cache Table (for decision deduplication)
CREATE TABLE permission_request_cache (
    cache_key VARCHAR(512) PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    permission_id VARCHAR(255) NOT NULL,
    decision INTEGER NOT NULL,
    grant_id UUID,
    cached_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_permission_request_cache_user_id ON permission_request_cache(user_id);
CREATE INDEX idx_permission_request_cache_expires_at ON permission_request_cache(expires_at);
```

---

## 7. UI Mockups (ASCII)

### 7.1 Permission Request Dialog

```
┌────────────────────────────────────────────────────────────────┐
│                                                              [X]│
│                   PERMISSION REQUESTED                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  The AI assistant is requesting permission to:               │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ 📁 Read Files                                            ││
│  │                                                          ││
│  │ The AI wants to read files from your project directory  ││
│  │ to understand your codebase and provide better          ││
│  │ suggestions.                                            ││
│  │                                                          ││
│  │ Risk Level: ⚠️  MEDIUM                                  ││
│  └──────────────────────────────────────────────────────────┘│
│                                                                │
│  📋 Details:                                                  │
│  • Permission ID: file.read                                  │
│  • Scope: Current Project (my-app)                           │
│  • Duration: Until revoked                                   │
│                                                                │
│  🔍 What this means:                                         │
│  The AI assistant can read the contents of text files,      │
│  source code, and configuration files in your project.      │
│                                                                │
│  ⚠️  Security Note:                                          │
│  This permission includes access to all readable files in   │
│  the scope. Review the scope settings if you want to        │
│  restrict access further.                                    │
│                                                                │
│  🔧 Scope Customization:                                    │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Scope: Current Project ▼                                 ││
│  │ ☑ Limit to specific folder: src/                         ││
│  │ ☐ Set expiration: [Today]                    [Change]   ││
│  └─────────────────────────────────────────────────────────┘│
│                                                                │
│  📜 Previous Decisions:                                      │
│  • GRANTED - Jan 15, 2026 10:30 AM - Same scope             │
│  • DENIED - Jan 10, 2026 2:15 PM - Global scope             │
│                                                                │
│                                                                │
│  ┌──────────────┐                      ┌──────────────────┐ │
│  │ [⏸️ Ask Later]│                      │ [Deny] [✓ Grant] │ │
│  └──────────────┘                      └──────────────────┘ │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 7.2 Batch Permission Request Dialog

```
┌────────────────────────────────────────────────────────────────┐
│                                                              [X]│
│            MULTIPLE PERMISSIONS REQUESTED (3)                │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  The AI assistant needs these permissions to proceed:        │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ ☑ 📁 Read Files              Risk: ⚠️ MEDIUM            ││
│  │   Scope: Current Project (my-app)                        ││
│  │                                                          ││
│  │ ☑ 🔍 Search Code              Risk: 🟢 LOW             ││
│  │   Scope: Current Project (my-app)                        ││
│  │                                                          ││
│  │ ☐ 💾 Execute Analysis         Risk: 🔴 HIGH            ││
│  │   Scope: Current Project (my-app)                        ││
│  │                                                          ││
│  │    Click to expand for details →                        ││
│  └──────────────────────────────────────────────────────────┘│
│                                                                │
│  Review required permissions above. Uncheck any permissions  │
│  you want to deny individually.                              │
│                                                                │
│                                                                │
│  ┌──────────────────┐                  ┌──────────────────┐  │
│  │ [⏸️ Ask Later]   │                  │ [Deny All] [Grant] │  │
│  └──────────────────┘                  └──────────────────┘  │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 7.3 Permission Management Dashboard

```
┌────────────────────────────────────────────────────────────────┐
│ PERMISSION MANAGEMENT DASHBOARD                               │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Active Permissions (7)        Filter: [All ▼]  Search: [    ] │
│                                                                │
│ ┌────────────────────────────────────────────────────────────┐│
│ │ 📁 file.read                                            [...]││
│ │ Scope: Current Project (my-app)                         ││
│ │ Granted: Jan 15, 2026 10:30 AM by system                ││
│ │ Expires: Never                                          ││
│ │                                                          ││
│ │ [Details] [Edit Scope] [Set Expiry] [Revoke]            ││
│ └────────────────────────────────────────────────────────────┘│
│                                                                │
│ ┌────────────────────────────────────────────────────────────┐│
│ │ 🔍 search.semantic                                     [...]││
│ │ Scope: Current Project (my-app)                         ││
│ │ Granted: Jan 15, 2026 10:30 AM by system                ││
│ │ Expires: Jan 22, 2026 10:30 AM (7 days remaining) ⏳    ││
│ │                                                          ││
│ │ [Details] [Edit Scope] [Extend] [Revoke]                ││
│ └────────────────────────────────────────────────────────────┘│
│                                                                │
│ ┌────────────────────────────────────────────────────────────┐│
│ │ 💾 execute.analysis                                    [...]││
│ │ Scope: Limited to src/ folder                           ││
│ │ Granted: Jan 10, 2026 3:45 PM by system                 ││
│ │ Expires: Never                                          ││
│ │                                                          ││
│ │ [Details] [Edit Scope] [Set Expiry] [Revoke]            ││
│ └────────────────────────────────────────────────────────────┘│
│                                                                │
│ Denied Permissions (2)         [Show Denied Permissions ▼]   │
│                                                                │
│ Expired Permissions (12)        [Show Expired ▼]             │
│                                                                │
│                        [View Complete History]               │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 7.4 Scope Customization Interface

```
┌────────────────────────────────────────────────────────────────┐
│ CUSTOMIZE PERMISSION SCOPE                                   │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Permission: file.read                                         │
│ Default Scope: Global                                         │
│                                                                │
│ Choose Scope Type:                                            │
│ ○ Global (all resources)                                     │
│ ○ Project (select below)                                     │
│ ● Specific Resource (select below)  ◄──────── Selected       │
│ ○ Session (expires when you close app)                       │
│ ○ Time Window (select duration below)                        │
│                                                                │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Resource Selection:                                      │ │
│ │                                                          │ │
│ │ 📁 Folder: [my-app/src]                  [Change] [X]  │ │
│ │                                                          │ │
│ │ Or specify multiple resources:                          │ │
│ │ □ my-app/src/                                          │ │
│ │ □ my-app/package.json                                  │ │
│ │ □ my-app/tsconfig.json                                 │ │
│ └──────────────────────────────────────────────────────────┘ │
│                                                                │
│ Time Duration:                                                │
│ ○ Unlimited                                                  │
│ ○ Limited [24 hours ▼]                                      │
│ ● Custom: [Jan 22, 2026 10:30 AM]  [Set Time]              │
│                                                                │
│ Additional Conditions:                                        │
│ ☐ Only during business hours (9 AM - 6 PM)                  │
│ ☐ Require confirmation for each use                         │
│ ☐ Log all access attempts                                   │
│                                                                │
│                      [Cancel] [Save Customization]           │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## 8. Dependency Chain

```
v0.18.1-SEC Permission Framework Dependencies:

v0.18.1a ────────┐
(Registry)       │
                 ├──► v0.18.1b (Request Pipeline)
                 │
                 └──► v0.18.1d (Scope Manager)
                      │
                      └──► v0.18.1e (Grant Store)
                           │
                           └──► v0.18.1f (Revocation)
                                │
                                └──► v0.18.1g (Delegation)

v0.18.1c (Consent Dialog) ──┐
                            ├──► v0.18.1b (uses in pipeline)
                            └──► v0.18.1e (stores decisions)

Cross-Component Dependencies:
  • All components depend on: Lexichord.Abstractions (v0.0.3b)
  • All components use: ILogger<T> from Microsoft.Extensions.Logging
  • Request pipeline uses: IMediator from MediatR
  • Persistence uses: Npgsql + Dapper or EF Core
  • UI components use: React.js + TypeScript
```

---

## 9. License Gating Table

| Feature | Core | WriterPro | Teams | Enterprise |
| :--- | :---: | :---: | :---: | :---: |
| **Permission Registry** | ✓ | ✓ | ✓ | ✓ |
| **Basic Request Pipeline** | ✓ | ✓ | ✓ | ✓ |
| **Consent Dialogs** | ✓ | ✓ | ✓ | ✓ |
| **Simple Scopes** (Global/Project) | ✓ | ✓ | ✓ | ✓ |
| **Resource Scopes** | ✗ | ✓ | ✓ | ✓ |
| **Time-Based Scopes** | ✗ | ✓ | ✓ | ✓ |
| **Grant Persistence** | ✓ | ✓ | ✓ | ✓ |
| **Audit Trail** | Limited | Full | Full | Full |
| **Revocation Service** | ✓ | ✓ | ✓ | ✓ |
| **Permission Delegation** | ✗ | ✗ | ✓ | ✓ |
| **Delegation Approval Workflows** | ✗ | ✗ | ✗ | ✓ |
| **Role-Based Templates** | ✗ | ✗ | ✓ | ✓ |
| **AI-Guided Recommendations** | ✗ | ✗ | ✗ | ✓ |
| **Compliance Reporting** | ✗ | ✗ | ✗ | ✓ |

---

## 10. Performance Targets

| Metric | Target | Rationale |
| :--- | :---: | :--- |
| **Permission Registry Lookup** | < 5 ms | Single permission lookup should be fast enough for real-time checks |
| **Permission Check (HasPermissionAsync)** | < 10 ms | Synchronous code paths may depend on permission checks |
| **Request Pipeline Processing** | < 100 ms | Excluding user decision time; includes validation, caching, context setup |
| **Consent Dialog Render** | < 100 ms | User-facing operation; must feel responsive |
| **Scope Evaluation** | < 5 ms | Typical complex scope evaluation with multiple constraints |
| **Grant Store Insert** | < 50 ms | Single grant creation (excluding network latency) |
| **Grant Store Query (by user)** | < 30 ms | Common operation for loading user's active grants |
| **Batch Permission Requests** (10) | < 200 ms | Processing multiple simultaneous requests |
| **Revocation Operation** | < 100 ms | Immediate revocation must be fast |
| **Expiry Cleanup (per grant)** | < 100 µs | Background job processing many grants; amortized time per grant |

---

## 11. Testing Strategy

### 11.1 Unit Testing

**Coverage Target**: 95%+ for all interfaces and implementations

**Test Categories**:
- Permission validation tests (valid IDs, scope constraints, risk levels)
- Scope evaluation tests (all constraint types, composition modes)
- Registry lookup tests (caching, fallback behavior)
- Grant lifecycle tests (creation, expiry, revocation, superseding)
- Decision caching tests (hit rates, expiration)
- Delegation tests (validation, cascading, depth limits)

**Example Test Class**:
```csharp
public class PermissionScopeManagerTests
{
    [Theory]
    [InlineData("resource-123", "MyResourceType")]
    [InlineData("doc-456", "Document")]
    public async Task EvaluateScopeAsync_WithMatchingResource_ReturnsTrue(
        string resourceId, string resourceType)
    {
        // Arrange
        var manager = new PermissionScopeManager();
        var scope = new PermissionScope(
            Guid.NewGuid().ToString(),
            new[] { new ResourceScopeConstraint(resourceId, resourceType) },
            DateTimeOffset.UtcNow);
        var context = new ScopeEvaluationContext(
            "user-123", "session-456", DateTimeOffset.UtcNow,
            currentResourceId: resourceId);

        // Act
        var result = await manager.EvaluateScopeAsync(scope, context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("resource-123", "resource-999")]
    public async Task EvaluateScopeAsync_WithNonMatchingResource_ReturnsFalse(
        string scopeResourceId, string contextResourceId)
    {
        // Arrange
        var manager = new PermissionScopeManager();
        var scope = new PermissionScope(
            Guid.NewGuid().ToString(),
            new[] { new ResourceScopeConstraint(scopeResourceId, "Document") },
            DateTimeOffset.UtcNow);
        var context = new ScopeEvaluationContext(
            "user-123", "session-456", DateTimeOffset.UtcNow,
            currentResourceId: contextResourceId);

        // Act
        var result = await manager.EvaluateScopeAsync(scope, context);

        // Assert
        Assert.False(result);
    }
}
```

### 11.2 Integration Testing

**Database Tests**: Use test PostgreSQL instance with schema migrations
```csharp
[Integration]
public class PermissionGrantStoreTests : IAsyncLifetime
{
    private PostgresContainer _postgres;
    private IPermissionGrantStore _store;

    public async Task InitializeAsync()
    {
        _postgres = new PostgresBuilder()
            .WithDatabase("permissions_test")
            .Build();
        await _postgres.StartAsync();
        _store = new PermissionGrantStore(_postgres.GetConnectionString());
    }

    [Fact]
    public async Task CreateGrantAsync_WithValidGrant_PersistsSuccessfully()
    {
        // Arrange
        var grant = new PermissionGrant(
            Guid.NewGuid().ToString(),
            "user-123",
            "file.read",
            new PermissionScope(Guid.NewGuid().ToString(), Array.Empty<ScopeConstraint>(), DateTimeOffset.UtcNow),
            GrantLifecycleStatus.Active,
            DateTimeOffset.UtcNow,
            "system");

        // Act
        var created = await _store.CreateGrantAsync(grant);

        // Assert
        var retrieved = await _store.GetGrantAsync(created.GrantId);
        Assert.NotNull(retrieved);
        Assert.Equal(grant.UserId, retrieved.UserId);
    }

    public async Task DisposeAsync()
    {
        await _postgres.StopAsync();
    }
}
```

### 11.3 API/Pipeline Testing

Test permission request flows end-to-end:
```csharp
[Fact]
public async Task RequestPermissionAsync_WithValidRequest_ShowsDialogAndReturnsDecision()
{
    // Arrange
    var mockDialogService = new Mock<IConsentDialogService>();
    mockDialogService
        .Setup(s => s.ShowConsentDialogAsync(
            It.IsAny<PermissionRequest>(),
            It.IsAny<PermissionType>(),
            It.IsAny<DialogPresentationMode>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ConsentDecision(
            Guid.NewGuid().ToString(),
            "req-123",
            "user-123",
            "file.read",
            ConsentChoice.Granted,
            DateTimeOffset.UtcNow));

    var manager = new PermissionManager(
        mockDialogService.Object,
        _registry,
        _pipeline,
        _store);

    // Act
    var response = await manager.RequestPermissionAsync(
        new PermissionRequest("req-123", "file.read", "user-123", "session-456"),
        new PermissionRequestContext("user-123", "session-456"));

    // Assert
    Assert.True(response.IsGranted);
    mockDialogService.Verify(s => s.ShowConsentDialogAsync(
        It.IsAny<PermissionRequest>(),
        It.IsAny<PermissionType>(),
        It.IsAny<DialogPresentationMode>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

### 11.4 Security Testing

**Permission Boundary Tests**:
```csharp
[Fact]
public async Task HasPermissionAsync_WithRevokedGrant_ReturnsFalse()
{
    // Arrange
    var grantId = await _store.CreateGrantAsync(
        new PermissionGrant(
            Guid.NewGuid().ToString(),
            "user-123",
            "file.read",
            _scope,
            GrantLifecycleStatus.Active,
            DateTimeOffset.UtcNow,
            "system"));

    // Act - Grant exists
    var hasPermission1 = await _manager.HasPermissionAsync(
        "user-123", "file.read",
        new ScopeEvaluationContext("user-123", "session-456", DateTimeOffset.UtcNow));
    Assert.True(hasPermission1);

    // Revoke the grant
    await _revocationService.RevokeGrantAsync(
        grantId.GrantId, RevocationReason.UserRequested);

    // Assert - Grant no longer valid
    var hasPermission2 = await _manager.HasPermissionAsync(
        "user-123", "file.read",
        new ScopeEvaluationContext("user-123", "session-456", DateTimeOffset.UtcNow));
    Assert.False(hasPermission2);
}
```

### 11.5 Performance Testing

**Load Tests with k6/NBomber**:
```typescript
// Permission check load test
export const options = {
  stages: [
    { duration: '2m', target: 100 },   // Ramp-up to 100 VUs
    { duration: '5m', target: 100 },   // Stay at 100 VUs
    { duration: '2m', target: 0 },     // Ramp-down
  ],
};

export default function () {
  const url = 'http://localhost:5000/api/permissions/check';
  const params = {
    headers: { 'Content-Type': 'application/json' },
  };
  const payload = JSON.stringify({
    userId: `user-${Math.floor(Math.random() * 10000)}`,
    permissionId: `file.read`,
    context: {
      sessionId: `session-${__VU}`,
      evaluatedAt: new Date().toISOString(),
    },
  });

  const response = http.post(url, payload, params);
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 10ms': (r) => r.timings.duration < 10,
  });

  sleep(1);
}
```

---

## 12. Risks & Mitigations

| Risk | Severity | Probability | Mitigation |
| :--- | :---: | :---: | :--- |
| **Permission fatigue** (users ignore dialogs) | High | High | Implement smart caching, batch requests, defaults for low-risk permissions |
| **Scope complexity** (users confused by scoping UI) | Medium | Medium | Provide templates, progressive disclosure, clear examples |
| **Performance degradation** (permission checks slow down system) | Medium | Medium | Aggressive caching (5-min TTL), async evaluation, indexed queries |
| **Database scalability** (grant table grows unbounded) | Medium | Low | Implement data retention policies, archiving of old revocations |
| **Cascading revocation bugs** (unexpected grants revoked) | High | Low | Comprehensive unit tests, integration tests, audit trail review |
| **Time zone issues** (expiry times incorrect across regions) | Low | Medium | Use UTC everywhere, test with multiple time zones |
| **Delegation depth explosion** (circular or excessive depth) | Medium | Low | Enforce max depth (3), validate against cycles, audit logging |
| **UI security issues** (XSS in permission explanations) | Critical | Low | HTML sanitization, DOMPurify, CSP headers |
| **Audit trail tampering** (logs modified) | Critical | Low | Immutable audit entries, cryptographic signing, regular backups |
| **Concurrent revocation** (race conditions in status updates) | Medium | Low | Database constraints, pessimistic locking, tests with concurrent operations |

---

## 13. MediatR Event Publishing Strategy

### 13.1 Event Handlers

```csharp
public class PermissionGrantedEventHandler : INotificationHandler<PermissionGrantedEvent>
{
    private readonly ILogger<PermissionGrantedEventHandler> _logger;
    private readonly IPermissionAuditService _auditService;

    public async Task Handle(PermissionGrantedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Permission {PermissionId} granted to user {UserId}",
            notification.PermissionId,
            notification.UserId);

        await _auditService.LogGrantAsync(
            notification.GrantId,
            notification.UserId,
            notification.PermissionId,
            "PermissionGranted",
            new Dictionary<string, object>
            {
                { "GrantedAt", notification.GrantedAt },
                { "GrantedBy", notification.GrantedBy },
                { "Scope", notification.Scope }
            },
            cancellationToken);
    }
}

public class PermissionRevokedEventHandler : INotificationHandler<PermissionRevokedEvent>
{
    private readonly ILogger<PermissionRevokedEventHandler> _logger;
    private readonly IPermissionAuditService _auditService;
    private readonly IUserNotificationService _notificationService;

    public async Task Handle(PermissionRevokedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Permission {PermissionId} revoked for user {UserId}, reason: {Reason}",
            notification.PermissionId,
            notification.UserId,
            notification.Reason);

        await Task.WhenAll(
            _auditService.LogRevocationAsync(
                notification.GrantId,
                notification.UserId,
                notification.Reason,
                cancellationToken),
            _notificationService.NotifyPermissionRevokedAsync(
                notification.UserId,
                notification.PermissionId,
                notification.Reason,
                cancellationToken));
    }
}
```

### 13.2 Event Publishing

```csharp
public class PermissionRequestPipeline : IPermissionRequestPipeline
{
    private readonly IPublisher _mediator;
    private readonly IPermissionGrantStore _store;

    public async Task<PermissionRequestResponse> ProcessAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        CancellationToken cancellationToken = default)
    {
        // ... validation and processing ...

        if (decision == PermissionRequestDecision.Granted)
        {
            var grant = new PermissionGrant(/* ... */);
            await _store.CreateGrantAsync(grant, cancellationToken);

            // Publish event
            await _mediator.Publish(
                new PermissionGrantedEvent(
                    grant.GrantId,
                    grant.UserId,
                    grant.PermissionId,
                    grant.Scope,
                    grant.GrantedAt,
                    grant.GrantedBy),
                cancellationToken);
        }
        else if (decision == PermissionRequestDecision.Denied)
        {
            await _mediator.Publish(
                new PermissionDeniedEvent(
                    request.RequestId,
                    request.UserId,
                    request.PermissionId,
                    "User denied the permission request",
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return response;
    }
}
```

---

## 14. Implementation Roadmap

### Phase 1: Foundation (Weeks 1-2)
- v0.18.1a (Permission Registry & Types)
- v0.18.1b (Permission Request Pipeline)

### Phase 2: User Experience (Weeks 2-3)
- v0.18.1c (User Consent Dialog System)
- v0.18.1d (Permission Scope Manager)

### Phase 3: Persistence & Operations (Weeks 4-5)
- v0.18.1e (Grant Persistence & Storage)
- v0.18.1f (Permission Revocation & Expiry)

### Phase 4: Advanced Features (Week 5-6)
- v0.18.1g (Permission Inheritance & Delegation)
- Integration testing across all components
- Performance optimization
- Security audit

---

## 15. Success Criteria

- [ ] All 7 sub-components implemented and tested
- [ ] Permission checks complete within 10ms in 99th percentile
- [ ] Consent dialogs render within 100ms
- [ ] 95%+ unit test coverage across all components
- [ ] Integration tests pass with real PostgreSQL
- [ ] Security audit completed with zero critical findings
- [ ] 50+ permissions defined across all categories
- [ ] UI/UX tested on desktop and mobile
- [ ] Documentation complete with examples
- [ ] Performance benchmarks verified against targets

---

## 16. Future Enhancements (v0.18.2+)

- AI-guided permission recommendations
- Role-based permission templates
- Compliance reporting dashboards
- Permission analytics and usage insights
- Automated permission cleanup
- Contextual permission policies (e.g., "only during business hours")
- Permission marketplace for third-party integrations
- Machine learning for permission optimization

---

## Document Approval

| Role | Name | Date | Status |
| :--- | :--- | :--- | :--- |
| Technical Lead | - | - | Pending |
| Product Manager | - | - | Pending |
| Security Officer | - | - | Pending |
| Architecture Review | - | - | Pending |

---

**End of Scope Breakdown Document**

Document ID: LCS-SBD-v0.18.1-SEC
Version: 1.0
Status: DRAFT
Last Updated: 2026-02-01
