# LCS-SBD-v0.18.8-SEC: Scope Breakdown — Threat Detection & Incident Response

## 1. Document Control

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-SBD-v0.18.8-SEC |
| **Release Version** | v0.18.8 |
| **Module Name** | Threat Detection & Incident Response |
| **Parent Release** | v0.18.x — Security & Compliance |
| **Document Type** | Scope Breakdown Document (SBD) |
| **Author** | Security Architecture Lead |
| **Date Created** | 2026-02-03 |
| **Last Updated** | 2026-02-03 |
| **Status** | DRAFT |
| **Classification** | Internal — Technical Specification |
| **Estimated Total Hours** | 76 hours |
| **Target Completion** | Sprint 18.8 (6 weeks) |

---

## 2. Executive Summary

### 2.1 The Need for Proactive Security

Previous security versions (v0.18.1-v0.18.7) establish defensive controls — permissions, sandboxing, isolation. However, sophisticated attackers will probe for weaknesses and attempt to bypass these controls. This version implements:

1. **Proactive Threat Detection:** Identify attacks before they succeed
2. **Automated Response:** Contain threats within seconds
3. **Threat Intelligence:** Learn from the broader security community
4. **Incident Management:** Structured response to security events
5. **Forensic Capability:** Investigate and learn from incidents

### 2.2 Attack Lifecycle Coverage

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Attack Lifecycle Detection                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌────────┐│
│  │  Recon   │───▶│  Weapon  │───▶│ Delivery │───▶│  Exploit │───▶│ Install││
│  │          │    │          │    │          │    │          │    │        ││
│  │ Probing  │    │ Crafting │    │  Prompt  │    │ Injection│    │Persist ││
│  │ Scanning │    │ Payloads │    │  Input   │    │ Execution│    │  ence  ││
│  └────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘    └───┬────┘│
│       │               │               │               │              │      │
│       ▼               ▼               ▼               ▼              ▼      │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    DETECTION LAYER                                    │  │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐        │  │
│  │  │ Behavioral │ │  Pattern   │ │  Anomaly   │ │ Correlation│        │  │
│  │  │  Analysis  │ │  Matching  │ │ Detection  │ │   Engine   │        │  │
│  │  └────────────┘ └────────────┘ └────────────┘ └────────────┘        │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│                                    ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    RESPONSE LAYER                                     │  │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐        │  │
│  │  │   Alert    │ │  Contain   │ │ Eradicate  │ │  Recover   │        │  │
│  │  │            │ │            │ │            │ │            │        │  │
│  │  └────────────┘ └────────────┘ └────────────┘ └────────────┘        │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.3 MITRE ATT&CK Mapping

This version maps detections to MITRE ATT&CK framework for AI systems:

| Tactic | Techniques Detected |
|:-------|:-------------------|
| **Reconnaissance** | Prompt probing, capability testing, boundary testing |
| **Resource Development** | Jailbreak crafting, payload development |
| **Initial Access** | Prompt injection, context poisoning |
| **Execution** | Code injection, command execution |
| **Persistence** | Memory manipulation, context persistence |
| **Privilege Escalation** | Permission bypass, capability escalation |
| **Defense Evasion** | Encoding, obfuscation, multi-turn attacks |
| **Credential Access** | Secret extraction, API key theft |
| **Discovery** | System probing, capability enumeration |
| **Collection** | Data harvesting, conversation extraction |
| **Exfiltration** | Data encoding, covert channels |
| **Impact** | Resource exhaustion, data destruction |

---

## 3. Detailed Sub-Parts Breakdown

### 3.1 v0.18.8a: Attack Pattern Detection Engine

**Estimated Hours**: 14 hours
**Sprint Assignment**: Week 1-2
**Status**: Pending Development

#### 3.1.1 Objective

Implement comprehensive attack pattern detection combining rule-based, statistical, and ML-based approaches.

#### 3.1.2 Key Interfaces

```csharp
namespace Lexichord.Security.ThreatDetection;

/// <summary>
/// Core attack pattern detection engine.
/// </summary>
public interface IAttackPatternDetector
{
    /// <summary>
    /// Analyze activity for attack patterns.
    /// </summary>
    Task<AttackDetectionResult> DetectAsync(
        SecurityActivity activity,
        DetectionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Correlate multiple events to identify coordinated attacks.
    /// </summary>
    Task<CorrelationResult> CorrelateEventsAsync(
        IReadOnlyList<SecurityEvent> events,
        CorrelationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Run threat hunting query against historical data.
    /// </summary>
    Task<HuntingResult> HuntAsync(
        ThreatHuntingQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Get current threat level assessment.
    /// </summary>
    Task<ThreatAssessment> AssessThreatLevelAsync(
        AssessmentScope scope,
        CancellationToken ct = default);
}

public record AttackDetectionResult
{
    public bool AttackDetected { get; init; }
    public AttackClassification? Classification { get; init; }
    public float Confidence { get; init; }
    public AttackSeverity Severity { get; init; }
    public IReadOnlyList<AttackIndicator> Indicators { get; init; } = [];
    public IReadOnlyList<MitreAttackMapping> MitreMappings { get; init; } = [];
    public ResponseRecommendation Recommendation { get; init; } = new();
    public string AnalysisNarrative { get; init; } = "";
}

public record AttackClassification
{
    public AttackType PrimaryType { get; init; }
    public IReadOnlyList<AttackType> SecondaryTypes { get; init; } = [];
    public AttackComplexity Complexity { get; init; }
    public AttackOrigin Origin { get; init; }
    public bool IsTargeted { get; init; }
    public bool IsAutomated { get; init; }
}

public enum AttackType
{
    // AI-Specific
    PromptInjection,
    JailbreakAttempt,
    ContextPoisoning,
    ModelManipulation,
    OutputExploitation,

    // Traditional
    CredentialTheft,
    DataExfiltration,
    PrivilegeEscalation,
    LateralMovement,
    PersistenceMechanism,
    DenialOfService,
    AccountTakeover,
    SupplyChainAttack,
    InsiderThreat,
    Reconnaissance,

    // Hybrid
    SocialEngineering,
    PhishingViaAI,
    AIAssistedAttack
}

public enum AttackSeverity
{
    Informational,
    Low,
    Medium,
    High,
    Critical
}

public enum AttackComplexity
{
    Trivial,        // Script kiddie level
    Low,            // Known techniques
    Medium,         // Some sophistication
    High,           // Advanced techniques
    Advanced        // APT-level sophistication
}

public enum AttackOrigin
{
    External,       // Outside the system
    Internal,       // Insider threat
    Compromised,    // Compromised legitimate user
    Automated,      // Bot/script
    Unknown
}

public record AttackIndicator
{
    public string IndicatorId { get; init; } = "";
    public IndicatorType Type { get; init; }
    public string Value { get; init; } = "";
    public string Description { get; init; } = "";
    public float Confidence { get; init; }
    public string? Source { get; init; }
    public DateTimeOffset FirstSeen { get; init; }
    public DateTimeOffset LastSeen { get; init; }
    public int OccurrenceCount { get; init; }
}

public enum IndicatorType
{
    // Network
    IpAddress,
    Domain,
    Url,
    UserAgent,

    // Content
    MaliciousPrompt,
    MaliciousPattern,
    EncodedPayload,
    ObfuscatedContent,

    // Behavioral
    AnomalousActivity,
    VelocityAnomaly,
    PatternAnomaly,
    TemporalAnomaly,

    // Identity
    SuspiciousUser,
    CompromisedAccount,
    SuspiciousSession,

    // Technical
    FileHash,
    ProcessSignature,
    MemoryPattern
}

public record MitreAttackMapping
{
    public string TechniqueId { get; init; } = "";  // e.g., "T1059.001"
    public string TechniqueName { get; init; } = "";
    public string Tactic { get; init; } = "";
    public float Confidence { get; init; }
    public string? SubTechnique { get; init; }
}

public record ResponseRecommendation
{
    public ResponseUrgency Urgency { get; init; }
    public IReadOnlyList<RecommendedAction> ImmediateActions { get; init; } = [];
    public IReadOnlyList<RecommendedAction> FollowUpActions { get; init; } = [];
    public bool RequiresHumanReview { get; init; }
    public string Rationale { get; init; } = "";
}

public enum ResponseUrgency
{
    None,
    Low,           // Can wait for normal review
    Medium,        // Should be addressed today
    High,          // Needs immediate attention
    Critical       // Drop everything
}

public record RecommendedAction
{
    public ActionType Type { get; init; }
    public string Description { get; init; } = "";
    public int Priority { get; init; }
    public bool CanAutomate { get; init; }
    public TimeSpan? SuggestedTimeframe { get; init; }
}

public enum ActionType
{
    // Containment
    BlockUser,
    TerminateSession,
    IsolateWorkspace,
    BlockNetwork,
    QuarantineProcess,

    // Investigation
    CollectForensics,
    ExpandMonitoring,
    AnalyzeLogs,
    ReviewHistory,

    // Eradication
    RevokeCredentials,
    PurgeCache,
    ResetState,
    PatchVulnerability,

    // Recovery
    RestoreFromBackup,
    ValidateIntegrity,
    ResumeOperations,

    // Communication
    AlertAdmin,
    NotifyUser,
    EscalateToSecurity,
    ReportToAuthorities
}

/// <summary>
/// Event correlation for identifying attack campaigns.
/// </summary>
public interface IEventCorrelator
{
    /// <summary>
    /// Correlate events across time windows.
    /// </summary>
    Task<CorrelationResult> CorrelateAsync(
        IReadOnlyList<SecurityEvent> events,
        CorrelationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Identify attack chains across multiple events.
    /// </summary>
    Task<AttackChain> IdentifyAttackChainAsync(
        IReadOnlyList<SecurityEvent> events,
        CancellationToken ct = default);
}

public record CorrelationResult
{
    public bool CampaignDetected { get; init; }
    public CampaignClassification? Campaign { get; init; }
    public IReadOnlyList<CorrelatedEventGroup> EventGroups { get; init; } = [];
    public float CorrelationStrength { get; init; }
    public string CorrelationNarrative { get; init; } = "";
}

public record AttackChain
{
    public Guid ChainId { get; init; }
    public IReadOnlyList<AttackChainStep> Steps { get; init; } = [];
    public AttackType FinalObjective { get; init; }
    public bool ChainComplete { get; init; }
    public int StepsBlocked { get; init; }
    public int StepsSucceeded { get; init; }
}

public record AttackChainStep
{
    public int StepNumber { get; init; }
    public string Tactic { get; init; } = "";
    public string Technique { get; init; } = "";
    public SecurityEvent? AssociatedEvent { get; init; }
    public StepStatus Status { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public enum StepStatus
{
    Attempted,
    Succeeded,
    Blocked,
    Detected,
    Inferred
}
```

---

### 3.2 v0.18.8b: Automated Incident Response

**Estimated Hours**: 12 hours
**Sprint Assignment**: Week 2-3
**Status**: Pending Development

#### 3.2.1 Key Interfaces

```csharp
namespace Lexichord.Security.IncidentResponse;

/// <summary>
/// Automated incident response and containment.
/// </summary>
public interface IIncidentResponseManager
{
    /// <summary>
    /// Create incident from detected attack.
    /// </summary>
    Task<SecurityIncident> CreateIncidentAsync(
        AttackDetectionResult detection,
        IncidentCreationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Execute response playbook.
    /// </summary>
    Task<PlaybookExecutionResult> ExecutePlaybookAsync(
        SecurityIncident incident,
        ResponsePlaybook playbook,
        CancellationToken ct = default);

    /// <summary>
    /// Contain threat immediately.
    /// </summary>
    Task<ContainmentResult> ContainThreatAsync(
        SecurityIncident incident,
        ContainmentStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Escalate incident to human responders.
    /// </summary>
    Task<EscalationResult> EscalateAsync(
        SecurityIncident incident,
        EscalationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Close incident with resolution.
    /// </summary>
    Task<ClosureResult> CloseIncidentAsync(
        Guid incidentId,
        IncidentClosure closure,
        CancellationToken ct = default);
}

public record SecurityIncident
{
    public Guid IncidentId { get; init; }
    public string IncidentNumber { get; init; } = "";  // Human-readable ID
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public IncidentSeverity Severity { get; init; }
    public IncidentStatus Status { get; init; }
    public IncidentPhase Phase { get; init; }
    public AttackClassification? Classification { get; init; }

    // Timeline
    public DateTimeOffset DetectedAt { get; init; }
    public DateTimeOffset? ContainedAt { get; init; }
    public DateTimeOffset? EradicatedAt { get; init; }
    public DateTimeOffset? RecoveredAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }

    // Impact
    public IReadOnlyList<AffectedResource> AffectedResources { get; init; } = [];
    public IReadOnlyList<AffectedUser> AffectedUsers { get; init; } = [];
    public ImpactAssessment Impact { get; init; } = new();

    // Response
    public IReadOnlyList<ResponseAction> ActionsTaken { get; init; } = [];
    public IReadOnlyList<IncidentNote> Notes { get; init; } = [];
    public UserId? AssignedTo { get; init; }
    public UserId? EscalatedTo { get; init; }

    // Analysis
    public string? RootCause { get; init; }
    public IReadOnlyList<string> LessonsLearned { get; init; } = [];
    public IReadOnlyList<string> Recommendations { get; init; } = [];
}

public enum IncidentSeverity
{
    P4_Low,         // Minor issue, no immediate action needed
    P3_Medium,      // Moderate issue, address within business hours
    P2_High,        // Significant issue, address immediately
    P1_Critical     // Major incident, all hands on deck
}

public enum IncidentStatus
{
    New,
    Triaging,
    InProgress,
    Contained,
    Eradicated,
    Recovered,
    Closed,
    Reopened
}

public enum IncidentPhase
{
    Detection,
    Triage,
    Containment,
    Eradication,
    Recovery,
    PostIncident
}

public record ResponsePlaybook
{
    public Guid PlaybookId { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public AttackType TargetAttackType { get; init; }
    public IReadOnlyList<PlaybookStep> Steps { get; init; } = [];
    public IReadOnlyList<PlaybookCondition> Conditions { get; init; } = [];
    public bool RequiresApproval { get; init; }
    public TimeSpan? MaxAutomatedDuration { get; init; }
}

public record PlaybookStep
{
    public int StepNumber { get; init; }
    public string Name { get; init; } = "";
    public ActionType Action { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
    public bool IsAutomated { get; init; }
    public bool ContinueOnFailure { get; init; }
    public TimeSpan? Timeout { get; init; }
    public IReadOnlyList<int>? OnSuccessGoTo { get; init; }
    public IReadOnlyList<int>? OnFailureGoTo { get; init; }
}

public record ContainmentResult
{
    public bool Success { get; init; }
    public ContainmentStrategy Strategy { get; init; }
    public IReadOnlyList<ContainmentAction> ActionsExecuted { get; init; } = [];
    public DateTimeOffset ContainedAt { get; init; }
    public TimeSpan TimeToContain { get; init; }
    public IReadOnlyList<string> Failures { get; init; } = [];
}

public enum ContainmentStrategy
{
    /// <summary>
    /// Block the specific user account.
    /// </summary>
    IsolateUser,

    /// <summary>
    /// Terminate specific session only.
    /// </summary>
    IsolateSession,

    /// <summary>
    /// Stop all agent activity.
    /// </summary>
    IsolateAgent,

    /// <summary>
    /// Lock down the workspace.
    /// </summary>
    IsolateWorkspace,

    /// <summary>
    /// Block all network access.
    /// </summary>
    IsolateNetwork,

    /// <summary>
    /// Complete system quarantine.
    /// </summary>
    FullQuarantine,

    /// <summary>
    /// Selective containment based on attack type.
    /// </summary>
    Adaptive
}

public record ImpactAssessment
{
    public ImpactLevel DataImpact { get; init; }
    public ImpactLevel SystemImpact { get; init; }
    public ImpactLevel BusinessImpact { get; init; }
    public ImpactLevel ReputationImpact { get; init; }
    public int EstimatedAffectedUsers { get; init; }
    public int EstimatedAffectedRecords { get; init; }
    public bool DataBreachConfirmed { get; init; }
    public bool RegulatoryNotificationRequired { get; init; }
}

public enum ImpactLevel
{
    None,
    Minimal,
    Moderate,
    Significant,
    Severe,
    Catastrophic
}
```

---

### 3.3 v0.18.8c: Threat Intelligence Integration

**Estimated Hours**: 12 hours
**Sprint Assignment**: Week 3-4
**Status**: Pending Development

#### 3.3.1 Key Interfaces

```csharp
namespace Lexichord.Security.ThreatIntelligence;

/// <summary>
/// Integration with threat intelligence sources.
/// </summary>
public interface IThreatIntelligenceService
{
    /// <summary>
    /// Check indicator against threat intelligence.
    /// </summary>
    Task<ThreatIntelResult> CheckIndicatorAsync(
        ThreatIndicator indicator,
        CancellationToken ct = default);

    /// <summary>
    /// Enrich indicator with threat intelligence.
    /// </summary>
    Task<EnrichedIndicator> EnrichIndicatorAsync(
        ThreatIndicator indicator,
        CancellationToken ct = default);

    /// <summary>
    /// Get threat feed updates.
    /// </summary>
    Task<ThreatFeed> GetThreatFeedAsync(
        ThreatFeedOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Submit indicator to community.
    /// </summary>
    Task SubmitIndicatorAsync(
        ThreatIndicator indicator,
        SubmissionOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get threat landscape report.
    /// </summary>
    Task<ThreatLandscapeReport> GetThreatLandscapeAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

public record ThreatIntelResult
{
    public bool IsKnownThreat { get; init; }
    public ThreatClassification? Classification { get; init; }
    public float Confidence { get; init; }
    public IReadOnlyList<ThreatIntelSource> Sources { get; init; } = [];
    public IReadOnlyList<string> AssociatedCampaigns { get; init; } = [];
    public IReadOnlyList<string> AssociatedActors { get; init; } = [];
    public DateTimeOffset? FirstSeen { get; init; }
    public DateTimeOffset? LastSeen { get; init; }
    public string? Analysis { get; init; }
}

public record ThreatClassification
{
    public ThreatCategory Category { get; init; }
    public string Family { get; init; } = "";
    public string? Variant { get; init; }
    public ThreatSeverity Severity { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum ThreatCategory
{
    Malware,
    Phishing,
    Ransomware,
    BotNet,
    APT,
    Exploit,
    Vulnerability,
    PromptAttack,
    JailbreakTechnique,
    DataBreach
}

public record ThreatFeed
{
    public string FeedId { get; init; } = "";
    public string FeedName { get; init; } = "";
    public DateTimeOffset LastUpdated { get; init; }
    public IReadOnlyList<ThreatIndicator> NewIndicators { get; init; } = [];
    public IReadOnlyList<ThreatIndicator> UpdatedIndicators { get; init; } = [];
    public IReadOnlyList<string> RemovedIndicatorIds { get; init; } = [];
    public ThreatLandscapeSummary Summary { get; init; } = new();
}

public record ThreatLandscapeSummary
{
    public int ActiveThreats { get; init; }
    public int NewThreatsThisPeriod { get; init; }
    public IReadOnlyList<TrendingThreat> TrendingThreats { get; init; } = [];
    public IReadOnlyList<string> EmergingTechniques { get; init; } = [];
    public string RiskAssessment { get; init; } = "";
}

public record TrendingThreat
{
    public string ThreatName { get; init; } = "";
    public ThreatCategory Category { get; init; }
    public float TrendScore { get; init; }  // How much it's increasing
    public int IncidentCount { get; init; }
    public string Description { get; init; } = "";
}
```

---

### 3.4 v0.18.8d: Security Operations Dashboard

**Estimated Hours**: 14 hours
**Sprint Assignment**: Week 4-5
**Status**: Pending Development

#### 3.4.1 Objective

Provide real-time visibility into security posture, active threats, and incident status.

#### 3.4.2 Dashboard Components

```csharp
namespace Lexichord.Security.Dashboard;

/// <summary>
/// Security operations dashboard data provider.
/// </summary>
public interface ISecurityDashboard
{
    /// <summary>
    /// Get current security posture overview.
    /// </summary>
    Task<SecurityPosture> GetSecurityPostureAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get active incidents summary.
    /// </summary>
    Task<IncidentsSummary> GetActiveIncidentsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get threat metrics over time.
    /// </summary>
    Task<ThreatMetrics> GetThreatMetricsAsync(
        TimeSpan period,
        MetricsGranularity granularity,
        CancellationToken ct = default);

    /// <summary>
    /// Get real-time alert stream.
    /// </summary>
    IObservable<SecurityAlert> GetAlertStream();

    /// <summary>
    /// Get top threats by category.
    /// </summary>
    Task<IReadOnlyList<ThreatSummary>> GetTopThreatsAsync(
        int count,
        TimeSpan period,
        CancellationToken ct = default);
}

public record SecurityPosture
{
    public PostureLevel OverallPosture { get; init; }
    public float RiskScore { get; init; }  // 0-100
    public IReadOnlyList<PostureMetric> Metrics { get; init; } = [];
    public IReadOnlyList<SecurityRecommendation> Recommendations { get; init; } = [];
    public DateTimeOffset AssessedAt { get; init; }
}

public enum PostureLevel
{
    Critical,       // Immediate action required
    Poor,           // Significant improvements needed
    Fair,           // Some improvements needed
    Good,           // Minor improvements possible
    Excellent       // Well-protected
}

public record PostureMetric
{
    public string MetricName { get; init; } = "";
    public MetricCategory Category { get; init; }
    public float Score { get; init; }
    public float Target { get; init; }
    public TrendDirection Trend { get; init; }
    public string Description { get; init; } = "";
}

public enum MetricCategory
{
    ThreatDetection,
    IncidentResponse,
    VulnerabilityManagement,
    AccessControl,
    DataProtection,
    ComplianceStatus
}

public enum TrendDirection
{
    Improving,
    Stable,
    Degrading
}

public record ThreatMetrics
{
    public TimeSpan Period { get; init; }
    public IReadOnlyList<TimeSeriesDataPoint> DetectionCount { get; init; } = [];
    public IReadOnlyList<TimeSeriesDataPoint> BlockedAttacks { get; init; } = [];
    public IReadOnlyList<TimeSeriesDataPoint> IncidentCount { get; init; } = [];
    public IReadOnlyList<TimeSeriesDataPoint> MeanTimeToDetect { get; init; } = [];
    public IReadOnlyList<TimeSeriesDataPoint> MeanTimeToContain { get; init; } = [];
    public IReadOnlyDictionary<AttackType, int> AttackTypeDistribution { get; init; } =
        new Dictionary<AttackType, int>();
}

public record TimeSeriesDataPoint
{
    public DateTimeOffset Timestamp { get; init; }
    public float Value { get; init; }
}
```

---

### 3.5 v0.18.8e: Incident Workflow Management

**Estimated Hours**: 12 hours
**Sprint Assignment**: Week 5
**Status**: Pending Development

#### 3.5.1 Objective

Structured workflows for incident handling with role assignments, escalation paths, and SLA tracking.

---

### 3.6 v0.18.8f: Forensic Data Collection

**Estimated Hours**: 12 hours
**Sprint Assignment**: Week 6
**Status**: Pending Development

#### 3.6.1 Key Interfaces

```csharp
namespace Lexichord.Security.Forensics;

/// <summary>
/// Collects and preserves forensic evidence.
/// </summary>
public interface IForensicCollector
{
    /// <summary>
    /// Initiate forensic collection for an incident.
    /// </summary>
    Task<ForensicCollection> CollectAsync(
        SecurityIncident incident,
        CollectionScope scope,
        CancellationToken ct = default);

    /// <summary>
    /// Create incident timeline.
    /// </summary>
    Task<IncidentTimeline> CreateTimelineAsync(
        SecurityIncident incident,
        TimelineOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Preserve evidence with chain of custody.
    /// </summary>
    Task<PreservedEvidence> PreserveEvidenceAsync(
        ForensicCollection collection,
        PreservationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Export for external analysis.
    /// </summary>
    Task<ForensicExport> ExportAsync(
        ForensicCollection collection,
        ExportFormat format,
        CancellationToken ct = default);
}

public record ForensicCollection
{
    public Guid CollectionId { get; init; }
    public Guid IncidentId { get; init; }
    public DateTimeOffset CollectionStarted { get; init; }
    public DateTimeOffset CollectionCompleted { get; init; }
    public CollectionScope Scope { get; init; }

    // Evidence
    public IReadOnlyList<SecurityEvent> SecurityEvents { get; init; } = [];
    public IReadOnlyList<AuditLogEntry> AuditLogs { get; init; } = [];
    public IReadOnlyList<AIInteraction> AIInteractions { get; init; } = [];
    public IReadOnlyList<NetworkCapture> NetworkCaptures { get; init; } = [];
    public IReadOnlyList<FileSystemSnapshot> FileSnapshots { get; init; } = [];
    public IReadOnlyList<MemorySnapshot> MemorySnapshots { get; init; } = [];
    public IReadOnlyList<ProcessSnapshot> ProcessSnapshots { get; init; } = [];

    // Integrity
    public string CollectionHash { get; init; } = "";
    public IReadOnlyList<ChainOfCustodyEntry> ChainOfCustody { get; init; } = [];
}

public record CollectionScope
{
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public IReadOnlyList<UserId>? Users { get; init; }
    public IReadOnlyList<Guid>? Sessions { get; init; }
    public IReadOnlyList<string>? Workspaces { get; init; }
    public bool IncludeNetworkCaptures { get; init; }
    public bool IncludeMemorySnapshots { get; init; }
    public bool IncludeFileSnapshots { get; init; }
}

public record IncidentTimeline
{
    public Guid IncidentId { get; init; }
    public IReadOnlyList<TimelineEvent> Events { get; init; } = [];
    public DateTimeOffset EarliestEvent { get; init; }
    public DateTimeOffset LatestEvent { get; init; }
    public string NarrativeSummary { get; init; } = "";
}

public record TimelineEvent
{
    public DateTimeOffset Timestamp { get; init; }
    public TimelineEventType Type { get; init; }
    public string Description { get; init; } = "";
    public string? Actor { get; init; }
    public string? Target { get; init; }
    public TimelineEventSignificance Significance { get; init; }
    public object? RawEvent { get; init; }
}

public enum TimelineEventType
{
    InitialAccess,
    Execution,
    Persistence,
    PrivilegeEscalation,
    DefenseEvasion,
    CredentialAccess,
    Discovery,
    LateralMovement,
    Collection,
    Exfiltration,
    Impact,
    Detection,
    Response,
    Containment,
    Eradication
}

public enum TimelineEventSignificance
{
    Low,
    Medium,
    High,
    Critical,
    KeyMoment  // Turning point in the incident
}

public record ChainOfCustodyEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Action { get; init; } = "";
    public UserId PerformedBy { get; init; }
    public string Description { get; init; } = "";
    public string? IntegrityHash { get; init; }
}

public enum ExportFormat
{
    Json,
    Csv,
    Stix,       // Structured Threat Information Expression
    OpenIOC,    // Open Indicators of Compromise
    Cef,        // Common Event Format
    Leef        // Log Event Extended Format
}
```

---

## 4. Database Schema

```sql
-- Security incidents
CREATE TABLE security_incidents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_number VARCHAR(20) NOT NULL UNIQUE,
    title TEXT NOT NULL,
    description TEXT,
    severity VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'new',
    phase VARCHAR(20) NOT NULL DEFAULT 'detection',
    classification JSONB,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    contained_at TIMESTAMPTZ,
    eradicated_at TIMESTAMPTZ,
    recovered_at TIMESTAMPTZ,
    closed_at TIMESTAMPTZ,
    impact_assessment JSONB,
    root_cause TEXT,
    lessons_learned TEXT[],
    recommendations TEXT[],
    assigned_to UUID REFERENCES users(id),
    escalated_to UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_incidents_status ON security_incidents(status);
CREATE INDEX idx_incidents_severity ON security_incidents(severity);
CREATE INDEX idx_incidents_assigned ON security_incidents(assigned_to);
CREATE INDEX idx_incidents_created ON security_incidents(created_at);

-- Incident response actions
CREATE TABLE incident_response_actions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_id UUID NOT NULL REFERENCES security_incidents(id),
    action_type VARCHAR(50) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    is_automated BOOLEAN DEFAULT FALSE,
    playbook_id UUID,
    step_number INT,
    executed_by UUID REFERENCES users(id),
    executed_at TIMESTAMPTZ,
    result JSONB,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_response_actions_incident ON incident_response_actions(incident_id);
CREATE INDEX idx_response_actions_status ON incident_response_actions(status);

-- Threat indicators
CREATE TABLE threat_indicators (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    indicator_type VARCHAR(30) NOT NULL,
    value TEXT NOT NULL,
    value_hash VARCHAR(64) NOT NULL,
    classification JSONB,
    confidence FLOAT NOT NULL,
    source VARCHAR(100),
    first_seen TIMESTAMPTZ NOT NULL,
    last_seen TIMESTAMPTZ NOT NULL,
    occurrence_count INT DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    tags TEXT[] DEFAULT '{}',
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(indicator_type, value_hash)
);

CREATE INDEX idx_indicators_type ON threat_indicators(indicator_type);
CREATE INDEX idx_indicators_active ON threat_indicators(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_indicators_hash ON threat_indicators(value_hash);

-- Attack correlations
CREATE TABLE attack_correlations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    correlation_type VARCHAR(30) NOT NULL,
    events JSONB NOT NULL,
    event_count INT NOT NULL,
    correlation_strength FLOAT NOT NULL,
    campaign_classification JSONB,
    attack_chain JSONB,
    narrative TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_correlations_type ON attack_correlations(correlation_type);
CREATE INDEX idx_correlations_created ON attack_correlations(created_at);

-- Forensic collections
CREATE TABLE forensic_collections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_id UUID NOT NULL REFERENCES security_incidents(id),
    scope JSONB NOT NULL,
    collection_started TIMESTAMPTZ NOT NULL,
    collection_completed TIMESTAMPTZ,
    collection_hash VARCHAR(64),
    chain_of_custody JSONB DEFAULT '[]',
    storage_location TEXT,
    is_preserved BOOLEAN DEFAULT FALSE,
    preserved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_forensics_incident ON forensic_collections(incident_id);
CREATE INDEX idx_forensics_preserved ON forensic_collections(is_preserved);

-- Response playbooks
CREATE TABLE response_playbooks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    target_attack_type VARCHAR(50) NOT NULL,
    steps JSONB NOT NULL,
    conditions JSONB DEFAULT '[]',
    requires_approval BOOLEAN DEFAULT FALSE,
    max_automated_duration_ms BIGINT,
    is_active BOOLEAN DEFAULT TRUE,
    execution_count INT DEFAULT 0,
    success_count INT DEFAULT 0,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_playbooks_attack_type ON response_playbooks(target_attack_type);
CREATE INDEX idx_playbooks_active ON response_playbooks(is_active) WHERE is_active = TRUE;
```

---

## 5. Success Metrics

- **Mean Time to Detect (MTTD):** <5 minutes for known attack types
- **Mean Time to Contain (MTTC):** <2 minutes with automated response
- **Detection Rate:** >95% for attacks in threat intelligence
- **False Positive Rate:** <5% requiring human review
- **Incident Resolution Time:** P1 <4 hours, P2 <24 hours
- **Playbook Success Rate:** >90% automated containment success
- **Forensic Completeness:** 100% evidence preservation for P1/P2

---

## 6. Dependencies

- v0.18.5-SEC (Audit & Compliance) — for security logging
- v0.18.6-SEC (AI Security) — for AI-specific detection
- v0.18.7-SEC (Workspace Isolation) — for containment capabilities

---
