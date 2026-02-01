# Scope Breakdown Document - v0.19.5-RES (Health Monitoring)

## 1. Document Control

| Property | Value |
|----------|-------|
| **Document ID** | LCS-SBD-v0.19.5-RES |
| **Version** | 1.0 |
| **Release** | v0.19.5 |
| **Focus Area** | Health Monitoring & Production Reliability |
| **Total Duration** | 58 hours |
| **Created Date** | 2026-02-01 |
| **Status** | ACTIVE |
| **Last Updated** | 2026-02-01 |
| **Maintained By** | Lexichord Engineering Team |

---

## 2. Executive Summary

### Vision
v0.19.5-RES implements comprehensive health monitoring infrastructure to ensure production reliability, enable proactive issue detection, and provide operators with real-time visibility into system health across all components and dependencies.

### Strategic Objectives
- **Real-time Visibility**: Implement comprehensive health check framework covering core services, database, network, AI services, storage, and external dependencies
- **Proactive Management**: Deploy alert system with intelligent routing, escalation, and acknowledgment capabilities
- **Operator Experience**: Create intuitive status dashboard with health indicators, resource gauges, and historical trending
- **Resource Intelligence**: Monitor CPU, memory, disk, network I/O with dynamic thresholding and anomaly detection
- **Dependency Resilience**: Track health of external dependencies including API services, database connections, message queues, and ML/AI services
- **API-Driven**: Expose comprehensive health and status endpoints for monitoring integrations and CI/CD pipelines

### Business Value
- **MTTR Reduction**: Proactive alerts reduce mean time to resolution by 60-80%
- **Uptime Assurance**: Continuous health monitoring ensures >99.95% availability
- **Operational Confidence**: Dashboard provides complete system visibility for operations teams
- **Scalability Support**: Resource monitoring enables capacity planning and auto-scaling triggers
- **Compliance**: Audit trail of health events supports compliance and incident reviews

---

## 3. Detailed Sub-Parts

### 3.1 v0.19.5a: Health Check Framework (12 hours)

#### Scope
Implement core health check infrastructure with modular, pluggable health check implementations for all critical system components.

#### Key Deliverables
1. **IHealthCheckManager**: Central orchestrator for health check execution and result aggregation
   - Registry of health checks with metadata (name, category, critical flag, timeout)
   - Parallel execution with timeout handling
   - Result caching with configurable TTL
   - Health status roll-up logic (worst-case semantics)
   - Metrics capture (execution time, pass/fail rates)

2. **IHealthCheck Interface**: Standardized health check contract
   - `ExecuteAsync()`: Health check execution
   - `GetName()`: Unique identifier
   - `GetCategory()`: Classification (Core, Database, Network, AI, Storage, External)
   - `IsCritical()`: Determines system health impact
   - `GetTimeout()`: Max execution duration (default 5s per check)

3. **Health Check Categories**:
   - **Core**: Application startup, configuration validation, license verification
   - **Database**: Connection pool, query response time, replication lag
   - **Network**: API availability, DNS resolution, latency monitoring
   - **AI**: ML model loading, inference performance, batch processing
   - **Storage**: Disk space, I/O performance, backup verification
   - **External**: Third-party API health, webhook validation, rate limit status

4. **Execution Framework**:
   - Parallel execution with semaphore for resource control
   - Individual per-check timeout (5-10 seconds)
   - Overall check suite timeout (30 seconds)
   - Automatic retry with exponential backoff (1-3 attempts)
   - Result caching (5-60 second TTL based on check type)

#### Acceptance Criteria
- [ ] IHealthCheckManager executes all registered checks within 30 second timeout
- [ ] Individual health checks timeout after 10 seconds without blocking other checks
- [ ] Health check results cached with configurable TTL
- [ ] Framework supports minimum 50 concurrent health checks
- [ ] Metrics captured: execution time, pass/fail rate, timeout count
- [ ] All health checks implement IHealthCheck interface
- [ ] Critical vs non-critical health checks properly classified
- [ ] Framework handles check failures gracefully without cascading failures

#### Performance Targets
- Individual health check execution: < 500ms (p95)
- Full health check suite: < 5 seconds (p95)
- Health check result caching: < 10ms retrieval
- Parallel check overhead: < 50ms

---

### 3.2 v0.19.5b: Status Dashboard UI (10 hours)

#### Scope
Create real-time status dashboard with health indicators, resource gauges, alert management, and historical trends.

#### Key Deliverables
1. **IStatusDashboard**: Dashboard backend service
   - Real-time health status aggregation
   - Resource metric aggregation
   - Alert summary with counts by severity
   - Historical data retrieval
   - Component dependency visualization

2. **Dashboard Components**:
   - **System Health Summary**: Overall system status (Healthy/Degraded/Critical)
   - **Component Health Grid**: Individual component status cards with last check time
   - **Resource Gauges**: CPU, Memory, Disk, Network I/O with visual thresholds
   - **Alert Panel**: Active alerts sorted by severity with acknowledgment capability
   - **Historical Charts**: 24-hour trending for key metrics (uptime, error rate)
   - **Dependency Map**: Visual representation of system dependencies and their health

3. **UI Features**:
   - Auto-refresh every 5 seconds (configurable)
   - Color coding: Green (Healthy), Yellow (Degraded), Red (Critical)
   - Alert detail view with timestamp, description, affected component
   - Acknowledge/resolve alert capability from dashboard
   - Search and filter by component, severity, status
   - Responsive design for mobile and desktop
   - Dark/light mode support

4. **Data Presentation**:
   - Component cards show: status, last check time, check count, error details
   - Resource gauges show: current value, threshold, max, historical sparkline
   - Alert tiles show: severity, title, description, component, timestamp
   - Charts: line graphs for trends, bar charts for comparisons

#### Acceptance Criteria
- [ ] Dashboard loads within 2 seconds
- [ ] Dashboard auto-refreshes without user disruption
- [ ] All system components displayed with current health status
- [ ] Resource gauges accurately reflect current and historical data
- [ ] Alerts displayed with severity color coding
- [ ] Ability to acknowledge/resolve alerts from dashboard
- [ ] Search/filter functionality works across all data
- [ ] Dashboard responsive on mobile and desktop
- [ ] Dark/light mode toggle functional
- [ ] Historical data available for minimum 7 days

#### Performance Targets
- Dashboard page load: < 2 seconds (initial + data load)
- Data refresh interval: 5 seconds
- Alert update latency: < 2 seconds
- Chart rendering: < 1 second for 24-hour window

---

### 3.3 v0.19.5c: Proactive Alerting (10 hours)

#### Scope
Implement intelligent alerting system with rule-based triggers, multi-channel delivery, severity escalation, and alert lifecycle management.

#### Key Deliverables
1. **IProactiveAlertManager**: Alert orchestration service
   - Alert rule evaluation and triggering
   - Alert deduplication (prevent duplicate alerts within window)
   - Alert escalation and routing
   - Acknowledgment and resolution tracking
   - Alert history and audit trail

2. **Alert Types**:
   - **Health Degradation**: Component health status changes (Healthy→Degraded, Degraded→Critical)
   - **Resource Threshold**: CPU/Memory/Disk exceeds configured threshold
   - **Dependency Failure**: External dependency becomes unavailable
   - **Performance Degradation**: Response time or error rate exceeds baseline
   - **Configuration Drift**: Detected configuration mismatch from expected state

3. **Alert Severity Levels**:
   - **CRITICAL**: System unavailable, data loss risk, immediate response required
   - **WARNING**: Service degraded, non-critical functionality impaired, action needed
   - **INFO**: Informational event, monitoring data point, no immediate action required
   - **DEBUG**: Diagnostic information, development/troubleshooting only

4. **Notification Channels**:
   - In-app dashboard notification
   - Email (configurable recipients per alert type)
   - SMS (critical alerts only)
   - Webhook (for integration with external monitoring systems)
   - Slack/Teams integration (configurable)

5. **Alert Rules**:
   - Condition-based: Thresholds, time windows, state changes
   - Escalation rules: Auto-escalate after time without acknowledgment
   - Suppression rules: Mute alerts during maintenance windows
   - Deduplication: Single alert per unique condition within 5-minute window

#### Acceptance Criteria
- [ ] Alert rules evaluated against health/resource data every 5 seconds
- [ ] Alerts generated and delivered within 50 milliseconds of condition trigger
- [ ] Alert deduplication prevents duplicate alerts within 5-minute window
- [ ] Escalation triggered after 15 minutes without acknowledgment
- [ ] Multiple notification channels supported and tested
- [ ] Alert acknowledgment persisted and tracked
- [ ] Alert history available with full audit trail
- [ ] Alert rule CRUD operations functional
- [ ] Maintenance window suppression working correctly

#### Performance Targets
- Alert generation latency: < 50ms from trigger condition
- Alert delivery latency: < 100ms
- Notification delivery: < 2 seconds (in-app), < 30 seconds (email)
- Rule evaluation: < 500ms for all active rules

---

### 3.4 v0.19.5d: Resource Monitoring (10 hours)

#### Scope
Implement system-wide resource monitoring with collection, aggregation, anomaly detection, and threshold-based alerting.

#### Key Deliverables
1. **IResourceMonitor**: Resource data collection and analysis service
   - CPU usage (overall and per-core)
   - Memory utilization (private/shared)
   - Disk I/O (IOPS, throughput)
   - Network I/O (bytes in/out, connections)
   - Process-level resource tracking
   - Anomaly detection using statistical baselines

2. **Metrics Collection**:
   - Collection interval: 30 seconds
   - Data retention: 30 days raw, 1 year aggregated
   - Metrics aggregation: 1-minute, 5-minute, hourly, daily
   - Time series data storage for trending and analysis

3. **Thresholding Strategy**:
   - Static thresholds: CPU > 80%, Memory > 85%, Disk > 90%
   - Dynamic thresholds: Based on historical percentiles (p95 + margin)
   - Time-based thresholds: Different limits for business vs. off-hours
   - Composite thresholds: Multi-metric conditions (e.g., CPU + Memory both high)

4. **Anomaly Detection**:
   - Statistical baselines: Mean + 2.5 std dev
   - Learning period: 7 days minimum before anomaly detection active
   - Detection window: 30-minute rolling window
   - Alert on: Sudden spikes, sustained deviation, correlated anomalies

5. **Data Capture**:
   - System-level metrics via performance counters
   - Application-level metrics via instrumentation
   - Process-specific metrics for Lexichord services
   - Per-thread metrics for bottleneck identification

#### Acceptance Criteria
- [ ] Resource metrics collected every 30 seconds with < 5% overhead
- [ ] CPU, Memory, Disk, Network metrics accurately reported
- [ ] Resource data aggregated at 1-min, 5-min, hourly, daily intervals
- [ ] Historical data retained for minimum 30 days (raw)
- [ ] Threshold violations trigger alerts within 50ms
- [ ] Anomaly detection identifies deviations with p95 accuracy
- [ ] Per-process resource tracking functional
- [ ] Resource data queryable via API

#### Performance Targets
- Metrics collection overhead: < 5% CPU impact
- Data point ingestion: < 1ms per metric
- Aggregation calculation: < 100ms for 30-day window
- Anomaly detection computation: < 500ms

---

### 3.5 v0.19.5e: Dependency Health (8 hours)

#### Scope
Implement comprehensive dependency health monitoring covering database, message queues, external APIs, and ML/AI services.

#### Key Deliverables
1. **IDependencyHealthService**: Dependency monitoring orchestrator
   - Health check for each registered dependency
   - Connection pool status
   - Latency and error rate tracking
   - Circuit breaker integration
   - Fallback/graceful degradation coordination

2. **Dependency Categories**:
   - **Database**: Primary PostgreSQL, read replicas, connection pool status
   - **Message Queues**: RabbitMQ/Redis, connection health, queue depth
   - **External APIs**: Third-party services, rate limits, SLA monitoring
   - **ML/AI Services**: Model inference services, batch processors, GPU availability
   - **Storage**: S3/blob storage, backup systems, CDN health
   - **Cache**: Redis/memcached, hit rates, memory pressure
   - **Search**: Elasticsearch/Solr index health, shard status

3. **Health Checks per Dependency**:
   - **Connectivity**: Can establish connection
   - **Latency**: Response time within acceptable bounds (e.g., < 100ms)
   - **Data Freshness**: Data updated within expected interval
   - **Error Rate**: Error rate below threshold (e.g., < 1%)
   - **Capacity**: Available capacity sufficient for demand
   - **Replication**: Data replication lag acceptable (< 5s for critical)

4. **Monitoring Integration**:
   - Circuit breaker coordination: Disable failing dependency checks
   - Health-based routing: Route requests to healthy instances
   - Graceful degradation: Reduce functionality when dependencies degraded
   - Dependency chain tracking: Identify cascading failures

#### Acceptance Criteria
- [ ] All configured dependencies monitored continuously
- [ ] Dependency health checks complete within 10 seconds
- [ ] Connection pool status accurately reported
- [ ] Latency and error rates tracked and queryable
- [ ] Circuit breaker state changes reflected in health status
- [ ] Dependency chain dependencies calculated and displayed
- [ ] Graceful degradation triggered appropriately
- [ ] Fallback mechanisms tested and working

#### Performance Targets
- Dependency health check: < 500ms per dependency
- Total dependency monitoring: < 5 seconds
- Dependency status update latency: < 2 seconds

---

### 3.6 v0.19.5f: Health API & Endpoints (8 hours)

#### Scope
Implement comprehensive REST API for health status, metrics, and alert management to enable integration with external monitoring and orchestration systems.

#### Key Deliverables
1. **IHealthApiEndpoints**: API contract definition
   - Health/liveness endpoints (Kubernetes compatible)
   - Metrics export endpoints (Prometheus/Grafana compatible)
   - Alert CRUD and query endpoints
   - Resource metrics endpoints
   - Status dashboard data endpoints

2. **API Endpoints**:
   ```
   GET  /health                      - Overall system health status
   GET  /health/live                 - Liveness probe (K8s compatible)
   GET  /health/ready                - Readiness probe (K8s compatible)
   GET  /health/component/{id}       - Specific component health
   GET  /health/components           - All components health status

   GET  /metrics/resources           - Current resource metrics
   GET  /metrics/resources/history   - Historical resource data
   GET  /metrics/dependencies        - Dependency health metrics
   GET  /metrics/prometheus          - Prometheus-formatted metrics

   GET  /alerts                      - List active alerts (filterable)
   GET  /alerts/{id}                 - Specific alert details
   POST /alerts/{id}/acknowledge     - Acknowledge alert
   POST /alerts/{id}/resolve         - Resolve alert
   POST /alerts                      - Create alert (manual)

   GET  /dashboard/summary           - Dashboard summary data
   GET  /dashboard/components        - Component status for dashboard
   GET  /dashboard/resources         - Resource gauge data
   GET  /dashboard/alerts            - Active alerts for dashboard
   GET  /dashboard/history           - Historical trending data
   ```

3. **Response Formats**:
   - JSON responses with consistent error handling
   - Prometheus text format metrics export
   - OpenAPI/Swagger documentation
   - Hypermedia links for navigation (HATEOAS)

4. **Authentication & Authorization**:
   - API key authentication for programmatic access
   - Role-based access control (read, write, admin)
   - Rate limiting per client
   - Audit logging of all API calls

#### Acceptance Criteria
- [ ] All listed endpoints implemented and functional
- [ ] Kubernetes liveness/readiness probes working correctly
- [ ] Metrics exported in Prometheus text format
- [ ] All endpoints documented in OpenAPI format
- [ ] Authentication/authorization enforced
- [ ] Rate limiting prevents abuse
- [ ] Response times < 500ms for all endpoints
- [ ] Comprehensive error handling with meaningful messages

#### Performance Targets
- Health endpoint response: < 100ms
- Metrics endpoint response: < 500ms
- Alert CRUD operations: < 200ms
- Dashboard data endpoint: < 500ms

---

## 4. Complete C# Interfaces

### IHealthCheckManager
```csharp
/// <summary>
/// Central orchestrator for health check execution and result aggregation.
/// Manages registration, execution, and result caching for all health checks.
/// </summary>
public interface IHealthCheckManager
{
    /// <summary>
    /// Register a new health check with the manager.
    /// </summary>
    /// <param name="check">The health check implementation</param>
    /// <param name="category">Health check category</param>
    /// <param name="isCritical">Whether component health depends on this check</param>
    /// <param name="cacheTtl">Result cache time-to-live</param>
    /// <returns>Unique health check ID</returns>
    Task<string> RegisterHealthCheckAsync(
        IHealthCheck check,
        HealthCheckCategory category,
        bool isCritical,
        TimeSpan cacheTtl);

    /// <summary>
    /// Unregister a health check by ID.
    /// </summary>
    Task UnregisterHealthCheckAsync(string checkId);

    /// <summary>
    /// Execute all registered health checks in parallel with timeout protection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for shutdown</param>
    /// <returns>Aggregated health check results</returns>
    Task<HealthCheckResult> ExecuteAllChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute specific health check by ID.
    /// </summary>
    Task<HealthCheckResult> ExecuteCheckAsync(string checkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cached result for health check without re-execution.
    /// Returns null if result expired or not cached.
    /// </summary>
    Task<HealthCheckResult?> GetCachedResultAsync(string checkId);

    /// <summary>
    /// Get all registered health checks with metadata.
    /// </summary>
    Task<IEnumerable<HealthCheckMetadata>> GetRegisteredChecksAsync();

    /// <summary>
    /// Clear all cached health check results.
    /// </summary>
    Task ClearCacheAsync();

    /// <summary>
    /// Get health check execution metrics.
    /// </summary>
    /// <returns>Metrics including success rate, average execution time, etc.</returns>
    Task<HealthCheckMetrics> GetMetricsAsync();
}

/// <summary>
/// Represents a single health check result.
/// </summary>
public class HealthCheckResult
{
    /// <summary>Unique identifier for this result</summary>
    public string CheckId { get; set; }

    /// <summary>Health check name</summary>
    public string Name { get; set; }

    /// <summary>Current health status</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Human-readable description of status</summary>
    public string Description { get; set; }

    /// <summary>Detailed error message if unhealthy</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Execution time in milliseconds</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>When this check was executed</summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>Health check category</summary>
    public HealthCheckCategory Category { get; set; }

    /// <summary>Whether this check is critical to system health</summary>
    public bool IsCritical { get; set; }

    /// <summary>Additional diagnostic data</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Metadata for registered health check.
/// </summary>
public class HealthCheckMetadata
{
    public string CheckId { get; set; }
    public string Name { get; set; }
    public HealthCheckCategory Category { get; set; }
    public bool IsCritical { get; set; }
    public TimeSpan CacheTtl { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// Aggregated metrics for all health checks.
/// </summary>
public class HealthCheckMetrics
{
    public int TotalChecks { get; set; }
    public int HealthyChecks { get; set; }
    public int DegradedChecks { get; set; }
    public int FailedChecks { get; set; }
    public double SuccessRate { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public long MaxExecutionTimeMs { get; set; }
    public int TimeoutCount { get; set; }
    public DateTime LastUpdateTime { get; set; }
}
```

### IHealthCheck
```csharp
/// <summary>
/// Base interface for individual health checks.
/// Implementers provide specific health check logic.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Execute the health check and return results.
    /// Must respect cancellation token and timeout.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for timeout/shutdown</param>
    /// <returns>Health check result with status and details</returns>
    Task<HealthCheckResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unique name identifier for this health check.
    /// </summary>
    string GetName();

    /// <summary>
    /// Get category classification for this health check.
    /// </summary>
    HealthCheckCategory GetCategory();

    /// <summary>
    /// Determine if this check is critical to overall system health.
    /// If critical check fails, system health is failed.
    /// </summary>
    bool IsCritical();

    /// <summary>
    /// Get maximum execution duration before timeout.
    /// Recommended: 5-10 seconds per check.
    /// </summary>
    TimeSpan GetTimeout();

    /// <summary>
    /// Get display description for this health check.
    /// </summary>
    string GetDescription();
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>Component is healthy and operating normally</summary>
    Healthy = 0,

    /// <summary>Component operating but experiencing degradation</summary>
    Degraded = 1,

    /// <summary>Component unavailable or critical error</summary>
    Unhealthy = 2,

    /// <summary>Component health unknown (not yet checked)</summary>
    Unknown = 3
}

/// <summary>
/// Health check category classification.
/// </summary>
public enum HealthCheckCategory
{
    /// <summary>Core application services</summary>
    Core = 0,

    /// <summary>Database systems</summary>
    Database = 1,

    /// <summary>Network and connectivity</summary>
    Network = 2,

    /// <summary>AI and ML services</summary>
    AI = 3,

    /// <summary>Storage systems</summary>
    Storage = 4,

    /// <summary>External dependencies</summary>
    External = 5
}
```

### IStatusDashboard
```csharp
/// <summary>
/// Service providing aggregated data for status dashboard.
/// Combines health, resource, alert, and historical data.
/// </summary>
public interface IStatusDashboard
{
    /// <summary>
    /// Get overall system health summary.
    /// </summary>
    /// <returns>Aggregated health status and component breakdown</returns>
    Task<HealthSummary> GetHealthSummaryAsync();

    /// <summary>
    /// Get health status for all components.
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <returns>List of component health statuses</returns>
    Task<IEnumerable<ComponentHealth>> GetComponentsHealthAsync(HealthCheckCategory? category = null);

    /// <summary>
    /// Get current resource utilization metrics.
    /// </summary>
    /// <returns>Current CPU, memory, disk, network metrics</returns>
    Task<ResourceMetrics> GetCurrentResourcesAsync();

    /// <summary>
    /// Get historical resource data for trending.
    /// </summary>
    /// <param name="hoursBack">Number of hours to retrieve</param>
    /// <param name="granularity">Data point granularity (minute, hour, day)</param>
    /// <returns>Historical resource data with aggregation</returns>
    Task<ResourceHistory> GetResourceHistoryAsync(int hoursBack = 24, TimeGranularity granularity = TimeGranularity.Minute);

    /// <summary>
    /// Get active alerts with summary information.
    /// </summary>
    /// <param name="severity">Optional severity filter</param>
    /// <param name="limit">Maximum number of alerts to return</param>
    /// <returns>Active alerts sorted by severity</returns>
    Task<IEnumerable<AlertSummary>> GetActiveAlertsAsync(AlertSeverity? severity = null, int limit = 100);

    /// <summary>
    /// Get system dependency map with health status.
    /// </summary>
    /// <returns>Dependency graph with connectivity status</returns>
    Task<DependencyMap> GetDependencyMapAsync();

    /// <summary>
    /// Get dashboard data optimized for initial load.
    /// </summary>
    /// <returns>All data needed for dashboard initialization</returns>
    Task<DashboardSnapshot> GetDashboardSnapshotAsync();
}

/// <summary>
/// Overall system health summary.
/// </summary>
public class HealthSummary
{
    /// <summary>Overall system health status</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Count of healthy components</summary>
    public int HealthyCount { get; set; }

    /// <summary>Count of degraded components</summary>
    public int DegradedCount { get; set; }

    /// <summary>Count of unhealthy components</summary>
    public int UnhealthyCount { get; set; }

    /// <summary>Total components</summary>
    public int TotalComponents { get; set; }

    /// <summary>Overall uptime percentage in last 24 hours</summary>
    public double Uptime24H { get; set; }

    /// <summary>When system status was last updated</summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Individual component health information.
/// </summary>
public class ComponentHealth
{
    /// <summary>Component unique identifier</summary>
    public string ComponentId { get; set; }

    /// <summary>Display name for component</summary>
    public string Name { get; set; }

    /// <summary>Current health status</summary>
    public HealthStatus Status { get; set; }

    /// <summary>Category of component</summary>
    public HealthCheckCategory Category { get; set; }

    /// <summary>When this status was determined</summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>Status description or error message</summary>
    public string StatusMessage { get; set; }

    /// <summary>Number of consecutive successful checks</summary>
    public int SuccessivePassCount { get; set; }

    /// <summary>Number of consecutive failed checks</summary>
    public int SuccessiveFailCount { get; set; }

    /// <summary>Additional diagnostic metadata</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Time granularity for historical data.
/// </summary>
public enum TimeGranularity
{
    Minute = 0,
    FiveMinutes = 1,
    Hour = 2,
    Day = 3
}

/// <summary>
/// Complete dashboard snapshot for initialization.
/// </summary>
public class DashboardSnapshot
{
    public HealthSummary HealthSummary { get; set; }
    public IEnumerable<ComponentHealth> Components { get; set; }
    public ResourceMetrics CurrentResources { get; set; }
    public IEnumerable<AlertSummary> ActiveAlerts { get; set; }
    public ResourceHistory ResourceHistory { get; set; }
    public DependencyMap DependencyMap { get; set; }
    public DateTime GeneratedAt { get; set; }
}
```

### IProactiveAlertManager
```csharp
/// <summary>
/// Service for alert rule evaluation, trigger, and lifecycle management.
/// Handles alert creation, deduplication, escalation, and delivery.
/// </summary>
public interface IProactiveAlertManager
{
    /// <summary>
    /// Create a new alert rule.
    /// </summary>
    /// <param name="rule">Rule definition with conditions and actions</param>
    /// <returns>Created rule with ID</returns>
    Task<AlertRule> CreateAlertRuleAsync(AlertRuleDefinition rule);

    /// <summary>
    /// Update existing alert rule.
    /// </summary>
    Task UpdateAlertRuleAsync(string ruleId, AlertRuleDefinition rule);

    /// <summary>
    /// Delete alert rule and cease triggering.
    /// </summary>
    Task DeleteAlertRuleAsync(string ruleId);

    /// <summary>
    /// Get all active alert rules.
    /// </summary>
    Task<IEnumerable<AlertRule>> GetActiveRulesAsync();

    /// <summary>
    /// Evaluate all alert rules against current system state.
    /// Normally called periodically by background service.
    /// </summary>
    /// <returns>Alerts triggered by evaluation</returns>
    Task<IEnumerable<Alert>> EvaluateRulesAsync();

    /// <summary>
    /// Manually trigger an alert (for testing or manual incidents).
    /// </summary>
    Task<Alert> TriggerAlertAsync(AlertDefinition alert);

    /// <summary>
    /// Get active alerts with filtering and pagination.
    /// </summary>
    Task<PagedResult<Alert>> GetActiveAlertsAsync(AlertQuery query);

    /// <summary>
    /// Get historical alerts with full audit trail.
    /// </summary>
    Task<PagedResult<Alert>> GetAlertHistoryAsync(AlertHistoryQuery query);

    /// <summary>
    /// Acknowledge an alert (indicate receipt and triage started).
    /// </summary>
    Task AcknowledgeAlertAsync(string alertId, string acknowledgedBy, string? notes = null);

    /// <summary>
    /// Resolve an alert (incident completed).
    /// </summary>
    Task ResolveAlertAsync(string alertId, string resolvedBy, string resolution);

    /// <summary>
    /// Get alert details including escalation history.
    /// </summary>
    Task<Alert> GetAlertAsync(string alertId);

    /// <summary>
    /// Suppress alerts during maintenance window.
    /// </summary>
    Task CreateSuppressionAsync(AlertSuppression suppression);

    /// <summary>
    /// Get alert statistics.
    /// </summary>
    Task<AlertStatistics> GetStatisticsAsync();
}

/// <summary>
/// Alert rule definition with trigger conditions.
/// </summary>
public class AlertRuleDefinition
{
    /// <summary>Unique rule name</summary>
    public string Name { get; set; }

    /// <summary>Rule description</summary>
    public string Description { get; set; }

    /// <summary>Alert severity when triggered</summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>Condition for triggering (e.g., "component.status == Unhealthy")</summary>
    public string TriggerCondition { get; set; }

    /// <summary>Grace period before triggering (allow transient issues)</summary>
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Time to wait before escalating unacknowledged alert</summary>
    public TimeSpan EscalationDelay { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Severity level to escalate to</summary>
    public AlertSeverity EscalationSeverity { get; set; }

    /// <summary>Notification channels for alert</summary>
    public IEnumerable<NotificationChannel> NotificationChannels { get; set; }

    /// <summary>Rule enabled/disabled flag</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Alert definition for manual triggering.
/// </summary>
public class AlertDefinition
{
    public string Title { get; set; }
    public string Description { get; set; }
    public AlertSeverity Severity { get; set; }
    public string ComponentId { get; set; }
    public string SourceSystem { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Active or historical alert instance.
/// </summary>
public class Alert
{
    /// <summary>Unique alert identifier</summary>
    public string AlertId { get; set; }

    /// <summary>Alert rule that triggered this alert</summary>
    public string RuleId { get; set; }

    /// <summary>Alert title/summary</summary>
    public string Title { get; set; }

    /// <summary>Detailed description</summary>
    public string Description { get; set; }

    /// <summary>Alert severity</summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>Affected component</summary>
    public string ComponentId { get; set; }

    /// <summary>System that generated the alert</summary>
    public string SourceSystem { get; set; }

    /// <summary>When alert was triggered</summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>When alert was acknowledged (null if not yet)</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>Who acknowledged the alert</summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>When alert was resolved (null if still active)</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Who resolved the alert</summary>
    public string? ResolvedBy { get; set; }

    /// <summary>Resolution notes/description</summary>
    public string? Resolution { get; set; }

    /// <summary>Current escalation level</summary>
    public AlertSeverity CurrentSeverity { get; set; }

    /// <summary>Escalation history</summary>
    public IEnumerable<AlertEscalation> Escalations { get; set; } = new List<AlertEscalation>();

    /// <summary>Additional context data</summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Informational event, no action required</summary>
    Info = 0,

    /// <summary>Warning, monitoring event, possible action needed</summary>
    Warning = 1,

    /// <summary>Service degradation, immediate action required</summary>
    Critical = 2,

    /// <summary>Informational debugging data</summary>
    Debug = 3
}

/// <summary>
/// Notification delivery channel.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Dashboard in-app notification</summary>
    Dashboard = 0,

    /// <summary>Email notification</summary>
    Email = 1,

    /// <summary>SMS text message</summary>
    SMS = 2,

    /// <summary>Webhook HTTP POST</summary>
    Webhook = 3,

    /// <summary>Slack integration</summary>
    Slack = 4,

    /// <summary>Teams integration</summary>
    Teams = 5
}

/// <summary>
/// Alert statistics and aggregate data.
/// </summary>
public class AlertStatistics
{
    public int TotalActive { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int UnacknowledgedCount { get; set; }
    public int AcknowledgedCount { get; set; }
    public double AverageResolutionTimeMinutes { get; set; }
    public int EscalatedCount { get; set; }
}

/// <summary>
/// Alert query with filtering options.
/// </summary>
public class AlertQuery
{
    public AlertSeverity? SeverityFilter { get; set; }
    public string? ComponentIdFilter { get; set; }
    public AlertStatusFilter? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Alert status filter.
/// </summary>
public enum AlertStatusFilter
{
    Active = 0,
    Acknowledged = 1,
    Resolved = 2,
    All = 3
}
```

### IResourceMonitor
```csharp
/// <summary>
/// Service for system resource collection, aggregation, and anomaly detection.
/// Tracks CPU, memory, disk, network utilization across the system.
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Get current system resource utilization.
    /// </summary>
    /// <returns>Current CPU, memory, disk, network metrics</returns>
    Task<ResourceMetrics> GetCurrentMetricsAsync();

    /// <summary>
    /// Get aggregated resource metrics for a time period.
    /// </summary>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="granularity">Aggregation granularity</param>
    /// <returns>Historical metrics with aggregation</returns>
    Task<ResourceHistory> GetHistoricalMetricsAsync(
        DateTime startTime,
        DateTime endTime,
        TimeGranularity granularity);

    /// <summary>
    /// Get resource metrics for specific process.
    /// </summary>
    Task<ProcessResourceMetrics> GetProcessMetricsAsync(int processId);

    /// <summary>
    /// Get resource metrics for all monitored processes.
    /// </summary>
    Task<IEnumerable<ProcessResourceMetrics>> GetAllProcessMetricsAsync();

    /// <summary>
    /// Check if current resource usage violates configured thresholds.
    /// </summary>
    /// <returns>List of threshold violations</returns>
    Task<IEnumerable<ResourceThresholdViolation>> CheckThresholdViolationsAsync();

    /// <summary>
    /// Get or update resource thresholds.
    /// </summary>
    Task<ResourceThresholds> GetThresholdsAsync();

    /// <summary>
    /// Update resource thresholds.
    /// </summary>
    Task UpdateThresholdsAsync(ResourceThresholds thresholds);

    /// <summary>
    /// Start background collection of resource metrics.
    /// Collection occurs every 30 seconds.
    /// </summary>
    Task StartCollectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop resource metric collection.
    /// </summary>
    Task StopCollectionAsync();

    /// <summary>
    /// Detect anomalies in resource usage patterns.
    /// </summary>
    /// <param name="lookbackHours">Number of hours to analyze</param>
    /// <returns>Detected anomalies with context</returns>
    Task<IEnumerable<ResourceAnomaly>> DetectAnomaliesAsync(int lookbackHours = 24);

    /// <summary>
    /// Get resource usage forecast.
    /// </summary>
    /// <param name="forecastHours">Hours to forecast ahead</param>
    /// <returns>Predicted resource usage</returns>
    Task<ResourceForecast> GetForecastAsync(int forecastHours = 4);
}

/// <summary>
/// Current system resource metrics.
/// </summary>
public class ResourceMetrics
{
    /// <summary>CPU usage percentage (0-100)</summary>
    public double CpuPercentage { get; set; }

    /// <summary>Per-core CPU percentage</summary>
    public IEnumerable<double> CpuPerCore { get; set; }

    /// <summary>Memory usage in bytes</summary>
    public long MemoryUsedBytes { get; set; }

    /// <summary>Total available memory in bytes</summary>
    public long MemoryTotalBytes { get; set; }

    /// <summary>Memory usage percentage</summary>
    public double MemoryPercentage { get; set; }

    /// <summary>Disk usage in bytes</summary>
    public long DiskUsedBytes { get; set; }

    /// <summary>Total disk capacity in bytes</summary>
    public long DiskTotalBytes { get; set; }

    /// <summary>Disk usage percentage</summary>
    public double DiskPercentage { get; set; }

    /// <summary>Network bytes received per second</summary>
    public long NetworkBytesInPerSec { get; set; }

    /// <summary>Network bytes sent per second</summary>
    public long NetworkBytesOutPerSec { get; set; }

    /// <summary>Active network connections</summary>
    public int ActiveConnections { get; set; }

    /// <summary>When metrics were collected</summary>
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Historical resource data with aggregation.
/// </summary>
public class ResourceHistory
{
    /// <summary>Time range covered</summary>
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>Data granularity</summary>
    public TimeGranularity Granularity { get; set; }

    /// <summary>CPU usage per time point (min, max, avg)</summary>
    public IEnumerable<ResourceDataPoint> CpuHistory { get; set; }

    /// <summary>Memory usage per time point</summary>
    public IEnumerable<ResourceDataPoint> MemoryHistory { get; set; }

    /// <summary>Disk usage per time point</summary>
    public IEnumerable<ResourceDataPoint> DiskHistory { get; set; }

    /// <summary>Network usage per time point</summary>
    public IEnumerable<NetworkDataPoint> NetworkHistory { get; set; }
}

/// <summary>
/// Single point of aggregated resource data.
/// </summary>
public class ResourceDataPoint
{
    public DateTime Timestamp { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double AverageValue { get; set; }
    public double PercentileP95 { get; set; }
}

/// <summary>
/// Resource threshold configuration.
/// </summary>
public class ResourceThresholds
{
    /// <summary>CPU threshold percentage for warning</summary>
    public double CpuWarningPercent { get; set; } = 80;

    /// <summary>CPU threshold percentage for critical</summary>
    public double CpuCriticalPercent { get; set; } = 95;

    /// <summary>Memory threshold percentage for warning</summary>
    public double MemoryWarningPercent { get; set; } = 85;

    /// <summary>Memory threshold percentage for critical</summary>
    public double MemoryCriticalPercent { get; set; } = 95;

    /// <summary>Disk threshold percentage for warning</summary>
    public double DiskWarningPercent { get; set; } = 85;

    /// <summary>Disk threshold percentage for critical</summary>
    public double DiskCriticalPercent { get; set; } = 95;

    /// <summary>Network bandwidth threshold for warning (bytes/sec)</summary>
    public long NetworkWarningBytesPerSec { get; set; } = 100_000_000; // 100 MB/s

    /// <summary>Allow dynamic thresholds based on historical data</summary>
    public bool UseDynamicThresholds { get; set; } = true;

    /// <summary>Dynamic threshold multiplier (std dev factor)</summary>
    public double DynamicThresholdStdDevFactor { get; set; } = 2.5;
}

/// <summary>
/// Process-specific resource metrics.
/// </summary>
public class ProcessResourceMetrics
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }
    public double CpuPercentage { get; set; }
    public long MemoryBytes { get; set; }
    public int ThreadCount { get; set; }
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Resource threshold violation detected.
/// </summary>
public class ResourceThresholdViolation
{
    public string ResourceType { get; set; } // "CPU", "Memory", "Disk"
    public double CurrentValue { get; set; }
    public double ThresholdValue { get; set; }
    public AlertSeverity Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public TimeSpan DurationExceeded { get; set; }
}

/// <summary>
/// Network-specific data point with directional metrics.
/// </summary>
public class NetworkDataPoint
{
    public DateTime Timestamp { get; set; }
    public long BytesInPerSec { get; set; }
    public long BytesOutPerSec { get; set; }
    public int ConnectionCount { get; set; }
}

/// <summary>
/// Detected resource usage anomaly.
/// </summary>
public class ResourceAnomaly
{
    public string ResourceType { get; set; }
    public DateTime DetectedAt { get; set; }
    public double BaselineValue { get; set; }
    public double AnomalyValue { get; set; }
    public double StandardDeviations { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// Forecasted resource usage for upcoming period.
/// </summary>
public class ResourceForecast
{
    public DateTime ForecastStart { get; set; }
    public DateTime ForecastEnd { get; set; }
    public IEnumerable<ResourceDataPoint> ForecastedCpu { get; set; }
    public IEnumerable<ResourceDataPoint> ForecastedMemory { get; set; }
    public IEnumerable<ResourceDataPoint> ForecastedDisk { get; set; }
    public double ConfidenceLevel { get; set; }
}
```

### IDependencyHealthService
```csharp
/// <summary>
/// Service for comprehensive dependency health monitoring and management.
/// Tracks database, message queues, external APIs, and ML/AI service health.
/// </summary>
public interface IDependencyHealthService
{
    /// <summary>
    /// Register a new dependency for monitoring.
    /// </summary>
    /// <param name="dependency">Dependency definition with type and connection info</param>
    /// <returns>Dependency ID for future reference</returns>
    Task<string> RegisterDependencyAsync(DependencyDefinition dependency);

    /// <summary>
    /// Unregister a dependency from monitoring.
    /// </summary>
    Task UnregisterDependencyAsync(string dependencyId);

    /// <summary>
    /// Get health status for specific dependency.
    /// </summary>
    Task<DependencyHealth> GetDependencyHealthAsync(string dependencyId);

    /// <summary>
    /// Get health status for all registered dependencies.
    /// </summary>
    Task<IEnumerable<DependencyHealth>> GetAllDependenciesHealthAsync();

    /// <summary>
    /// Get dependencies filtered by type.
    /// </summary>
    Task<IEnumerable<DependencyHealth>> GetDependenciesByTypeAsync(DependencyType type);

    /// <summary>
    /// Perform health check on specific dependency.
    /// </summary>
    Task<DependencyHealth> CheckDependencyAsync(string dependencyId);

    /// <summary>
    /// Perform health checks on all dependencies.
    /// </summary>
    Task<IEnumerable<DependencyHealth>> CheckAllDependenciesAsync();

    /// <summary>
    /// Get dependency latency and error metrics.
    /// </summary>
    Task<DependencyMetrics> GetDependencyMetricsAsync(string dependencyId);

    /// <summary>
    /// Get connection pool status for database/queue dependencies.
    /// </summary>
    Task<PoolStatus> GetPoolStatusAsync(string dependencyId);

    /// <summary>
    /// Get dependency chain - what depends on what.
    /// </summary>
    Task<DependencyChain> GetDependencyChainAsync();

    /// <summary>
    /// Calculate impact of dependency failure on system.
    /// </summary>
    Task<ImpactAnalysis> AnalyzeFailureImpactAsync(string dependencyId);

    /// <summary>
    /// Mark dependency as temporarily unavailable.
    /// </summary>
    Task MarkDependencyDownAsync(string dependencyId, string reason, TimeSpan duration);

    /// <summary>
    /// Get dependencies that are currently unavailable or degraded.
    /// </summary>
    Task<IEnumerable<DependencyHealth>> GetUnhealthyDependenciesAsync();

    /// <summary>
    /// Start background health monitoring.
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop background health monitoring.
    /// </summary>
    Task StopMonitoringAsync();
}

/// <summary>
/// Dependency definition for registration.
/// </summary>
public class DependencyDefinition
{
    /// <summary>Unique dependency identifier</summary>
    public string DependencyId { get; set; }

    /// <summary>Display name</summary>
    public string Name { get; set; }

    /// <summary>Dependency type (database, API, cache, etc.)</summary>
    public DependencyType Type { get; set; }

    /// <summary>Connection string or endpoint URL</summary>
    public string ConnectionString { get; set; }

    /// <summary>Whether this dependency is required for core functionality</summary>
    public bool IsCritical { get; set; }

    /// <summary>Expected latency baseline in milliseconds</summary>
    public int BaselineLatencyMs { get; set; } = 100;

    /// <summary>Maximum acceptable latency in milliseconds</summary>
    public int MaxLatencyMs { get; set; } = 1000;

    /// <summary>Maximum acceptable error rate percentage</summary>
    public double MaxErrorRatePercent { get; set; } = 1.0;

    /// <summary>Custom health check implementation</summary>
    public Func<Task<bool>>? CustomHealthCheck { get; set; }
}

/// <summary>
/// Dependency type classification.
/// </summary>
public enum DependencyType
{
    Database = 0,
    MessageQueue = 1,
    ExternalApi = 2,
    Cache = 3,
    Storage = 4,
    SearchIndex = 5,
    MachineLearning = 6,
    Other = 7
}

/// <summary>
/// Health status for a single dependency.
/// </summary>
public class DependencyHealth
{
    /// <summary>Dependency identifier</summary>
    public string DependencyId { get; set; }

    /// <summary>Display name</summary>
    public string Name { get; set; }

    /// <summary>Dependency type</summary>
    public DependencyType Type { get; set; }

    /// <summary>Current health status</summary>
    public HealthStatus Status { get; set; }

    /// <summary>When health was last checked</summary>
    public DateTime LastCheckedAt { get; set; }

    /// <summary>Status description or error message</summary>
    public string StatusMessage { get; set; }

    /// <summary>Average latency in milliseconds</summary>
    public int AverageLatencyMs { get; set; }

    /// <summary>Error rate percentage</summary>
    public double ErrorRatePercent { get; set; }

    /// <summary>Whether dependency is currently marked as down</summary>
    public bool IsMarkedDown { get; set; }

    /// <summary>If marked down, reason for unavailability</summary>
    public string? DownReason { get; set; }

    /// <summary>If marked down, when availability is expected to resume</summary>
    public DateTime? ExpectedRecoveryTime { get; set; }
}

/// <summary>
/// Metrics for dependency performance.
/// </summary>
public class DependencyMetrics
{
    public string DependencyId { get; set; }
    public int RequestCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int TimeoutCount { get; set; }
    public double ErrorRatePercent { get; set; }
    public int MinLatencyMs { get; set; }
    public int MaxLatencyMs { get; set; }
    public int AvgLatencyMs { get; set; }
    public int P95LatencyMs { get; set; }
    public int P99LatencyMs { get; set; }
    public DateTime CollectionStart { get; set; }
    public DateTime CollectionEnd { get; set; }
}

/// <summary>
/// Connection pool status for database or queue dependencies.
/// </summary>
public class PoolStatus
{
    public string DependencyId { get; set; }
    public int PoolSize { get; set; }
    public int AvailableConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int QueuedRequests { get; set; }
    public int PeakConnections { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
}

/// <summary>
/// Dependency chain showing what depends on what.
/// </summary>
public class DependencyChain
{
    public Dictionary<string, IEnumerable<string>> Dependencies { get; set; } = new();

    /// <summary>
    /// Get all transitive dependencies of a dependency.
    /// </summary>
    public IEnumerable<string> GetTransitiveDependencies(string dependencyId)
    {
        var result = new HashSet<string>();
        var queue = new Queue<string>(new[] { dependencyId });

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (Dependencies.TryGetValue(current, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (result.Add(dep))
                    {
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Impact analysis of dependency failure on system functionality.
/// </summary>
public class ImpactAnalysis
{
    public string DependencyId { get; set; }
    public bool AffectsCoreServices { get; set; }
    public bool AffectsApiEndpoints { get; set; }
    public bool AffectsReporting { get; set; }
    public bool AffectsML { get; set; }
    public IEnumerable<string> DependentServices { get; set; }
    public IEnumerable<string> AffectedEndpoints { get; set; }
    public string ImpactSummary { get; set; }
    public bool RequiresFallback { get; set; }
    public TimeSpan EstimatedMttr { get; set; }
}
```

### IHealthApiEndpoints
```csharp
/// <summary>
/// Contract for health and status API endpoints.
/// Implements Kubernetes liveness/readiness probes and metrics export.
/// </summary>
public interface IHealthApiEndpoints
{
    /// <summary>
    /// GET /health - Overall system health status.
    /// Returns 200 if healthy, 503 if unhealthy.
    /// </summary>
    Task<IActionResult> GetHealthStatusAsync();

    /// <summary>
    /// GET /health/live - Kubernetes liveness probe.
    /// Returns 200 if service is running, 503 if crashed/deadlocked.
    /// </summary>
    Task<IActionResult> GetLivenessProbeAsync();

    /// <summary>
    /// GET /health/ready - Kubernetes readiness probe.
    /// Returns 200 if ready to handle traffic, 503 if warming up/degraded.
    /// </summary>
    Task<IActionResult> GetReadinessProbeAsync();

    /// <summary>
    /// GET /health/component/{componentId} - Specific component health.
    /// </summary>
    Task<IActionResult> GetComponentHealthAsync(string componentId);

    /// <summary>
    /// GET /health/components - All components health status.
    /// </summary>
    Task<IActionResult> GetAllComponentsHealthAsync(
        [FromQuery] string? category = null);

    /// <summary>
    /// GET /metrics/resources - Current resource metrics.
    /// </summary>
    Task<IActionResult> GetResourceMetricsAsync();

    /// <summary>
    /// GET /metrics/resources/history - Historical resource data.
    /// </summary>
    Task<IActionResult> GetResourceHistoryAsync(
        [FromQuery] int hoursBack = 24,
        [FromQuery] string granularity = "minute");

    /// <summary>
    /// GET /metrics/dependencies - Dependency health metrics.
    /// </summary>
    Task<IActionResult> GetDependencyMetricsAsync();

    /// <summary>
    /// GET /metrics/prometheus - Prometheus-formatted metrics export.
    /// </summary>
    Task<IActionResult> GetPrometheusMetricsAsync();

    /// <summary>
    /// GET /alerts - List active alerts with filtering.
    /// </summary>
    Task<IActionResult> GetAlertsAsync(
        [FromQuery] string? severity = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50);

    /// <summary>
    /// GET /alerts/{alertId} - Specific alert details.
    /// </summary>
    Task<IActionResult> GetAlertAsync(string alertId);

    /// <summary>
    /// POST /alerts/{alertId}/acknowledge - Acknowledge alert.
    /// </summary>
    Task<IActionResult> AcknowledgeAlertAsync(
        string alertId,
        [FromBody] AcknowledgeAlertRequest request);

    /// <summary>
    /// POST /alerts/{alertId}/resolve - Resolve alert.
    /// </summary>
    Task<IActionResult> ResolveAlertAsync(
        string alertId,
        [FromBody] ResolveAlertRequest request);

    /// <summary>
    /// GET /dashboard/summary - Dashboard summary data.
    /// </summary>
    Task<IActionResult> GetDashboardSummaryAsync();

    /// <summary>
    /// GET /dashboard/snapshot - Complete dashboard snapshot.
    /// </summary>
    Task<IActionResult> GetDashboardSnapshotAsync();
}

/// <summary>
/// Request payload for acknowledging alert.
/// </summary>
public class AcknowledgeAlertRequest
{
    public string AcknowledgedBy { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload for resolving alert.
/// </summary>
public class ResolveAlertRequest
{
    public string ResolvedBy { get; set; }
    public string Resolution { get; set; }
}

/// <summary>
/// Health status response.
/// </summary>
public class HealthStatusResponse
{
    public string Status { get; set; } // "Healthy", "Degraded", "Unhealthy"
    public string Description { get; set; }
    public IEnumerable<ComponentHealthResponse> Components { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Individual component health in response.
/// </summary>
public class ComponentHealthResponse
{
    public string ComponentId { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public int SuccessivePassCount { get; set; }
    public int SuccessiveFailCount { get; set; }
}
```

---

## 5. ASCII Architecture Diagrams

### 5.1 Health Check Execution Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     IHealthCheckManager                         │
│  Central orchestrator for all health check execution             │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
   ┌────▼─────┐                    ┌─────▼─────┐
   │  Check    │                    │  Check    │
   │ Registry  │                    │  Cache    │
   │(metadata) │                    │ (results) │
   └────┬─────┘                    └─────┬─────┘
        │                                 │
        └────────────────┬────────────────┘
                         │
              ┌──────────▼──────────┐
              │  Execution Engine   │
              │  (Parallel w/        │
              │   Semaphore)        │
              └────┬────┬────┬──────┘
                   │    │    │
        ┌──────────┼────┼────┴──────────┐
        │          │    │               │
   ┌────▼────┐ ┌──▼──┐ ┌──▼──┐ ┌──────▼───┐
   │  Core   │ │ DB  │ │Net  │ │ External │
   │ Checks  │ │Check│ │Check│ │ API Chks │
   └─────────┘ └─────┘ └─────┘ └──────────┘
        │         │       │           │
        └─────────┴───┬───┴───────────┘
                      │
              ┌───────▼────────┐
              │ Result         │
              │ Aggregation &  │
              │ Roll-up        │
              │ (worst-case)   │
              └───────┬────────┘
                      │
         ┌────────────▼─────────────┐
         │  HealthCheckResult       │
         │  - Overall Status        │
         │  - Component Statuses    │
         │  - Metrics               │
         └──────────────────────────┘
```

### 5.2 Alert Pipeline and Escalation

```
┌─────────────────────────────────────────────────────────────────┐
│                  Proactive Alert Manager                        │
│  Rules evaluation, triggering, and escalation orchestration     │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
   ┌────▼──────────┐              ┌──────▼─────┐
   │ Alert Rules   │              │ Suppression │
   │ (Conditions)  │              │ Rules       │
   └────┬──────────┘              └──────┬─────┘
        │                                 │
        └────────────────┬────────────────┘
                         │
           ┌─────────────▼─────────────┐
           │  Rule Evaluation Engine   │
           │  (Every 5 seconds)        │
           └────┬────────────┬────┬────┘
                │            │    │
       ┌────────▼─┐  ┌──────▼──┐ │
       │  Match   │  │ Not      │ │
       │ Condition│  │Suppressed│ │
       └────┬─────┘  └──────┬───┘ │
            │               │     │
            └───────┬───────┘     │
                    │             │
         ┌──────────▼──────┐      │
         │ Deduplication   │      │
         │ (5-min window)  │      │
         └────┬────────────┘      │
              │                   │
         ┌────▼──────────────┐    │
         │  Trigger Alert    │────┘
         │  (AlertTriggered  │
         │   Event)          │
         └────┬──────────────┘
              │
    ┌─────────┼──────────┬──────────────┐
    │         │          │              │
 ┌──▼─┐   ┌──▼──┐   ┌────▼────┐  ┌────▼────┐
 │In- │   │ Email│   │ Webhook │  │SMS/Slack│
 │App │   │Alert │   │ POST    │  │ Alert   │
 │Ntf │   │      │   │         │  │         │
 └────┘   └──────┘   └─────────┘  └─────────┘
              │
              │ Grace Period (1 min)
              │ + No Acknowledgment
              │
         ┌────▼─────────────┐
         │  Auto-Escalation  │
         │  (15+ min)        │
         │  Increase Severity│
         └────┬──────────────┘
              │
         ┌────▼────────────────────┐
         │ Re-deliver to higher    │
         │ priority channels       │
         └─────────────────────────┘
```

### 5.3 Resource Monitoring and Threshold Flow

```
┌─────────────────────────────────────────────────────────────┐
│             IResourceMonitor Collection Service             │
│  Continuous collection of system resource metrics           │
└────────────┬────────────────────────────────┬───────────────┘
             │                                │
        Every 30 seconds              Configurable TTL
             │                                │
   ┌─────────▼───────────┐          ┌────────▼──────┐
   │  System Metrics:    │          │  Data Storage │
   │  - CPU (per-core)   │          │  (Time-series)│
   │  - Memory (user/sys)│          │  PostgreSQL   │
   │  - Disk I/O (IOPS)  │          │  Partitioned  │
   │  - Network I/O      │          │  by month     │
   │  - Processes        │          └────────┬──────┘
   └─────────┬───────────┘                   │
             │                               │
       ┌─────▼─────────┐            ┌────────▼─────┐
       │  Aggregation  │            │ Retention:   │
       │  1-min, 5-min │            │ 30d raw +    │
       │  hourly, daily│            │ 1yr agg      │
       └─────┬─────────┘            └──────────────┘
             │
       ┌─────▼──────────────────┐
       │ Threshold Comparison   │
       │ - Static (fixed %)     │
       │ - Dynamic (historical) │
       └──┬──────────────────┬──┘
          │                  │
     ┌────▼──────┐      ┌────▼─────┐
     │ Violation │      │ No        │
     │ Detected  │      │ Violation │
     └────┬──────┘      └──────────┘
          │
   ┌──────▼────────────────────────┐
   │ Resource Threshold Exceeded    │
   │ Event Triggered               │
   │ (AlertTriggeredEvent)         │
   └──────┬────────────────────────┘
          │
    ┌─────┴──────┐
    │            │
 ┌──▼──┐    ┌───▼───┐
 │Alert│    │Trend  │
 │Gen. │    │Charts │
 └─────┘    └───────┘
```

### 5.4 Dependency Health Monitoring Chain

```
┌────────────────────────────────────────────────────────────┐
│          IDependencyHealthService                          │
│  Monitoring and orchestration of all system dependencies   │
└──────────────┬─────────────────────────────┬───────────────┘
               │                             │
      ┌────────▼──────────┐        ┌────────▼──────┐
      │ Dependency        │        │ Dependency    │
      │ Registry          │        │ Chain Manager │
      │ (Metadata)        │        │ (What depends │
      └──────┬────────────┘        │  on what)     │
             │                     └───────┬──────┘
             │                             │
      ┌──────▼──────────────────┬──────────▼───────┐
      │                         │                  │
   ┌──▼──────┐ ┌──────┐ ┌──────▼───┐ ┌──────────┐
   │Database │ │Cache │ │Message   │ │External  │
   │Service  │ │(Redis)│ │Queues   │ │APIs      │
   │Health   │ └──────┘ │(RabbitMQ)│ └──────────┘
   │Check    │          └──────┬───┘
   └──┬──────┘                 │
      │     ┌──────────────────┼──────────────┐
      │     │                  │              │
   ┌──▼────▼──┐  ┌─────┐  ┌───▼────┐  ┌─────▼──┐
   │ Connection│  │Pool │  │Latency │  │Error   │
   │ Health    │  │Status│  │Check   │  │Rate    │
   │           │  └─────┘  └─────────┘  └────────┘
   └──┬────────┘         │
      │                  │
      └──────┬───────────┘
             │
     ┌───────▼──────────────────┐
     │ Dependency Health Status │
     │ - Overall Status         │
     │ - Latency/Error Metrics  │
     │ - Pool Status            │
     │ - Circuit Breaker State  │
     └───────┬──────────────────┘
             │
     ┌───────▼────────────────────┐
     │ Impact Analysis            │
     │ - Affected Services        │
     │ - Fallback Strategy        │
     │ - Cascade Prevention       │
     └────────────────────────────┘
```

---

## 6. Health Check Catalog

### 6.1 Core Health Checks

| Check Name | Description | Timeout | Critical | Frequency |
|------------|-------------|---------|----------|-----------|
| **Startup Status** | Application has completed initialization | 5s | Yes | On-demand |
| **Configuration Validation** | All required configuration loaded and valid | 5s | Yes | 5 min |
| **License Verification** | License file present and valid | 5s | Yes | 1 hour |
| **File System Permissions** | Temp, log, config directories accessible | 3s | Yes | 10 min |
| **Default Routes** | All default ASP.NET routes registered | 3s | No | 5 min |
| **Dependency Injection** | DI container properly initialized | 2s | Yes | 5 min |

### 6.2 Database Health Checks

| Check Name | Description | Timeout | Critical | Frequency |
|------------|-------------|---------|----------|-----------|
| **Connection Pool** | Database connection pool available | 5s | Yes | 30s |
| **Primary Connection** | Can establish connection to primary DB | 5s | Yes | 30s |
| **Read Replica** | Can establish connection to read replica | 5s | No | 1 min |
| **Query Performance** | SELECT 1 executes within baseline | 5s | Yes | 1 min |
| **Replication Lag** | Primary/replica replication lag < 5s | 5s | Yes | 1 min |
| **Backup Verification** | Latest backup completed successfully | 10s | No | 1 hour |
| **Transaction Isolation** | Default isolation level configured | 3s | No | 5 min |
| **Disk Space** | Database disk usage < 90% | 3s | No | 5 min |

### 6.3 Network Health Checks

| Check Name | Description | Timeout | Critical | Frequency |
|------------|-------------|---------|----------|-----------|
| **DNS Resolution** | Can resolve all configured hostnames | 5s | Yes | 1 min |
| **API Gateway** | API gateway responding to health probe | 5s | Yes | 30s |
| **Network Latency** | Ping to gateway < 100ms | 5s | No | 1 min |
| **Port Accessibility** | All required ports accessible | 5s | No | 5 min |
| **SSL Certificate** | SSL certificates valid, not expiring < 30 days | 3s | No | 1 hour |

### 6.4 AI Services Health Checks

| Check Name | Description | Timeout | Critical | Frequency |
|------------|-------------|---------|----------|-----------|
| **Model Loading** | ML models loaded and inference available | 10s | No | 5 min |
| **Inference Performance** | Inference latency < baseline | 10s | No | 5 min |
| **GPU Availability** | GPU allocated and accessible (if configured) | 5s | No | 1 min |
| **Batch Processing Queue** | Batch processor responding | 5s | No | 1 min |
| **Model Versioning** | Expected model version deployed | 3s | No | 10 min |

### 6.5 Storage Health Checks

| Check Name | Description | Timeout | Critical | Frequency |
|------------|-------------|---------|----------|-----------|
| **Disk Space** | System disk usage < 90% | 3s | Yes | 5 min |
| **I/O Performance** | Disk latency < baseline | 5s | No | 5 min |
| **Backup System** | Backup processes completed successfully | 10s | No | 1 hour |
| **S3/Blob Storage** | Can write/read test object to cloud storage | 10s | No | 5 min |
| **Tmp Directory** | Temp directory accessible and writable | 2s | Yes | 5 min |

### 6.6 External Dependencies Health Checks

| Check Name | Description | Timeout | Critical | Frequency |
|------------|-------------|---------|----------|-----------|
| **Payment Gateway** | Payment processor API responding | 10s | No | 5 min |
| **Email Service** | Email service endpoint responding | 10s | No | 5 min |
| **SMS Gateway** | SMS provider endpoint responding | 10s | No | 5 min |
| **Analytics Service** | Analytics endpoint responding | 10s | No | 5 min |
| **Third-party API Rate Limit** | API rate limit availability | 5s | No | 1 min |

---

## 7. Resource Thresholds and Alert Severity Mapping

### 7.1 Resource Threshold Configuration

```
CPU THRESHOLDS:
  Warning:  > 80%  (30s sustained)
  Critical: > 95%  (10s sustained)
  Dynamic:  Mean + 2.5 × StdDev (7-day window)

MEMORY THRESHOLDS:
  Warning:  > 85% of allocated (30s sustained)
  Critical: > 95% of allocated (10s sustained)
  Dynamic:  Mean + 2.5 × StdDev (7-day window)

DISK THRESHOLDS:
  Warning:  > 80% of capacity (5 min sustained)
  Critical: > 95% of capacity (1 min sustained)
  Critical: < 1 GB free space (immediately)

NETWORK THRESHOLDS:
  Warning:  > 100 MB/s sustained (1 min)
  Critical: > 500 MB/s sustained (30s)
  High connection count: > 50,000 active

RESPONSE TIME THRESHOLDS:
  Warning:  p95 > 500ms (1 min window)
  Critical: p95 > 2000ms (30s window)

ERROR RATE THRESHOLDS:
  Warning:  > 1% (1 min window)
  Critical: > 5% (30s window)
```

### 7.2 Alert Severity Mapping

```
RESOURCE THRESHOLD VIOLATIONS:
┌──────────────────┬─────────┬──────────────────┬──────────────────┐
│ Resource Type    │ Status  │ Alert Severity   │ Escalation Time  │
├──────────────────┼─────────┼──────────────────┼──────────────────┤
│ CPU > 80%        │ Warning │ WARNING          │ 15 min           │
│ CPU > 95%        │ Critical│ CRITICAL         │ 5 min            │
│ Memory > 85%     │ Warning │ WARNING          │ 15 min           │
│ Memory > 95%     │ Critical│ CRITICAL         │ 5 min            │
│ Disk > 80%       │ Warning │ WARNING          │ 30 min           │
│ Disk > 95%       │ Critical│ CRITICAL         │ 10 min           │
│ Disk < 1 GB      │ Critical│ CRITICAL         │ 5 min            │
└──────────────────┴─────────┴──────────────────┴──────────────────┘

COMPONENT HEALTH CHANGES:
┌──────────────────┬─────────────────┬──────────────┬──────────────────┐
│ Transition       │ Alert Severity  │ Critical     │ Escalation Time  │
├──────────────────┼─────────────────┼──────────────┼──────────────────┤
│ Healthy→Degraded │ WARNING         │ No           │ 30 min           │
│ Healthy→Unhealthy│ CRITICAL        │ Yes (if crit)│ 5 min            │
│ Degraded→Unhealthy│CRITICAL         │ Yes (if crit)│ 5 min            │
│ Degraded→Healthy │ INFO            │ No           │ N/A              │
│ Unhealthy→Healthy│ INFO            │ No           │ N/A              │
└──────────────────┴─────────────────┴──────────────┴──────────────────┘

DEPENDENCY FAILURES:
┌──────────────────────┬──────────────┬──────────────┬──────────────────┐
│ Dependency Type      │ Alert Sev.   │ Critical     │ Escalation Time  │
├──────────────────────┼──────────────┼──────────────┼──────────────────┤
│ Primary DB Down      │ CRITICAL     │ Yes          │ 2 min            │
│ Cache Down           │ WARNING      │ No           │ 15 min           │
│ Message Queue Down   │ WARNING      │ No           │ 15 min           │
│ External API Down    │ WARNING      │ No           │ 30 min           │
│ Search Index Down    │ WARNING      │ No           │ 30 min           │
└──────────────────────┴──────────────┴──────────────┴──────────────────┘
```

---

## 8. PostgreSQL Schema

### 8.1 Health Check Results Table

```sql
CREATE TABLE health_check_results (
    id BIGSERIAL PRIMARY KEY,
    check_id VARCHAR(255) NOT NULL,
    check_name VARCHAR(255) NOT NULL,
    category VARCHAR(50) NOT NULL, -- Core, Database, Network, AI, Storage, External
    status VARCHAR(20) NOT NULL, -- Healthy, Degraded, Unhealthy, Unknown
    description TEXT,
    error_message TEXT,
    execution_time_ms INT NOT NULL,
    is_critical BOOLEAN NOT NULL DEFAULT FALSE,
    executed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_check_id UNIQUE (check_id, executed_at)
) PARTITION BY RANGE (executed_at);

CREATE INDEX idx_health_check_status ON health_check_results (status);
CREATE INDEX idx_health_check_category ON health_check_results (category);
CREATE INDEX idx_health_check_executed_at ON health_check_results (executed_at DESC);
CREATE INDEX idx_health_check_critical ON health_check_results (is_critical);

-- Partitions (monthly)
CREATE TABLE health_check_results_2026_01 PARTITION OF health_check_results
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE health_check_results_2026_02 PARTITION OF health_check_results
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');
```

### 8.2 Health History Table

```sql
CREATE TABLE health_history (
    id BIGSERIAL PRIMARY KEY,
    component_id VARCHAR(255) NOT NULL,
    component_name VARCHAR(255) NOT NULL,
    health_status VARCHAR(20) NOT NULL, -- Healthy, Degraded, Unhealthy
    status_change_reason TEXT,
    previous_status VARCHAR(20),
    check_count INT DEFAULT 1,
    success_count INT DEFAULT 0,
    failure_count INT DEFAULT 0,
    last_check_time TIMESTAMP,
    status_changed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_component_id FOREIGN KEY (component_id) REFERENCES components(id)
) PARTITION BY RANGE (status_changed_at);

CREATE INDEX idx_health_history_component ON health_history (component_id);
CREATE INDEX idx_health_history_status ON health_history (health_status);
CREATE INDEX idx_health_history_changed_at ON health_history (status_changed_at DESC);

-- Partitions (monthly)
CREATE TABLE health_history_2026_01 PARTITION OF health_history
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE health_history_2026_02 PARTITION OF health_history
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');
```

### 8.3 Alerts Table

```sql
CREATE TABLE alerts (
    id VARCHAR(36) PRIMARY KEY,
    rule_id VARCHAR(255) NOT NULL,
    title VARCHAR(512) NOT NULL,
    description TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL, -- Critical, Warning, Info, Debug
    component_id VARCHAR(255),
    source_system VARCHAR(255) NOT NULL,
    triggered_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    acknowledged_at TIMESTAMP,
    acknowledged_by VARCHAR(255),
    acknowledged_notes TEXT,
    resolved_at TIMESTAMP,
    resolved_by VARCHAR(255),
    resolution TEXT,
    current_severity VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE', -- ACTIVE, ACKNOWLEDGED, RESOLVED
    context JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (triggered_at);

CREATE INDEX idx_alerts_status ON alerts (status);
CREATE INDEX idx_alerts_severity ON alerts (severity);
CREATE INDEX idx_alerts_component ON alerts (component_id);
CREATE INDEX idx_alerts_triggered_at ON alerts (triggered_at DESC);
CREATE INDEX idx_alerts_rule_id ON alerts (rule_id);

-- Partitions (monthly)
CREATE TABLE alerts_2026_01 PARTITION OF alerts
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE alerts_2026_02 PARTITION OF alerts
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');
```

### 8.4 Alert Rules Table

```sql
CREATE TABLE alert_rules (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    severity VARCHAR(20) NOT NULL, -- Critical, Warning, Info
    trigger_condition TEXT NOT NULL,
    grace_period_minutes INT DEFAULT 1,
    escalation_delay_minutes INT DEFAULT 15,
    escalation_severity VARCHAR(20),
    notification_channels TEXT[] NOT NULL, -- Array of channels
    enabled BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

CREATE INDEX idx_alert_rules_enabled ON alert_rules (enabled);
CREATE INDEX idx_alert_rules_name ON alert_rules (name);
```

### 8.5 Resource Usage Table

```sql
CREATE TABLE resource_usage (
    id BIGSERIAL PRIMARY KEY,
    cpu_percent NUMERIC(5,2),
    cpu_per_core NUMERIC(5,2)[],
    memory_used_bytes BIGINT,
    memory_total_bytes BIGINT,
    memory_percent NUMERIC(5,2),
    disk_used_bytes BIGINT,
    disk_total_bytes BIGINT,
    disk_percent NUMERIC(5,2),
    network_bytes_in_per_sec BIGINT,
    network_bytes_out_per_sec BIGINT,
    active_connections INT,
    process_id INT,
    process_name VARCHAR(255),
    process_cpu_percent NUMERIC(5,2),
    process_memory_bytes BIGINT,
    collected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (collected_at);

CREATE INDEX idx_resource_usage_collected_at ON resource_usage (collected_at DESC);
CREATE INDEX idx_resource_usage_process ON resource_usage (process_id, collected_at DESC);

-- Monthly partitions for 30-day raw retention
CREATE TABLE resource_usage_2026_01 PARTITION OF resource_usage
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE resource_usage_2026_02 PARTITION OF resource_usage
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');

-- Aggregated tables (1-min, 5-min, hourly, daily)
CREATE TABLE resource_usage_1min (
    id BIGSERIAL PRIMARY KEY,
    cpu_min NUMERIC(5,2),
    cpu_max NUMERIC(5,2),
    cpu_avg NUMERIC(5,2),
    cpu_p95 NUMERIC(5,2),
    memory_min NUMERIC(5,2),
    memory_max NUMERIC(5,2),
    memory_avg NUMERIC(5,2),
    memory_p95 NUMERIC(5,2),
    disk_min NUMERIC(5,2),
    disk_max NUMERIC(5,2),
    disk_avg NUMERIC(5,2),
    timestamp TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (timestamp);

CREATE INDEX idx_resource_usage_1min_timestamp ON resource_usage_1min (timestamp DESC);
```

### 8.6 Dependency Health Table

```sql
CREATE TABLE dependency_health (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- Database, MessageQueue, ExternalApi, Cache, Storage, SearchIndex, MachineLearning
    connection_string VARCHAR(512) NOT NULL,
    is_critical BOOLEAN NOT NULL DEFAULT FALSE,
    status VARCHAR(20) NOT NULL, -- Healthy, Degraded, Unhealthy
    last_checked_at TIMESTAMP NOT NULL,
    status_message TEXT,
    avg_latency_ms INT,
    error_rate_percent NUMERIC(5,2),
    is_marked_down BOOLEAN DEFAULT FALSE,
    down_reason TEXT,
    expected_recovery_time TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_dependency_type ON dependency_health (type);
CREATE INDEX idx_dependency_status ON dependency_health (status);
CREATE INDEX idx_dependency_critical ON dependency_health (is_critical);
```

---

## 9. UI Mockups and Dashboard Design

### 9.1 Status Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Lexichord Health Monitoring Dashboard                      🔔 ⚙️  👤    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│ SYSTEM HEALTH SUMMARY                                                    │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ Overall Status: [🟢 HEALTHY]  |  Uptime: 99.98% (24h)          │     │
│ │ Components: 24 Healthy │ 0 Degraded │ 0 Unhealthy              │     │
│ │ Active Alerts: 0                                               │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ RESOURCE GAUGES                                                          │
│ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │
│ │ CPU Usage    │  │ Memory Usage │  │ Disk Usage   │  │ Network I/O │  │
│ │              │  │              │  │              │  │             │  │
│ │    45%       │  │    62%       │  │    78%       │  │  12 MB/s    │  │
│ │   ○●●●○○○○   │  │   ○●●●●●○○○  │  │  ○●●●●●●○○  │  │  Warn: 100  │  │
│ │   Normal     │  │   Moderate   │  │   Caution    │  │  MB/s       │  │
│ └──────────────┘  └──────────────┘  └──────────────┘  └─────────────┘  │
│                                                                           │
│ COMPONENT HEALTH STATUS                                                  │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ Component              │ Status    │ Category  │ Last Check      │     │
│ ├─────────────────────────────────────────────────────────────────┤     │
│ │ API Gateway            │ 🟢 Healthy│ Core     │ 5 sec ago       │     │
│ │ PostgreSQL (Primary)   │ 🟢 Healthy│ Database │ 30 sec ago      │     │
│ │ PostgreSQL (Replica)   │ 🟢 Healthy│ Database │ 45 sec ago      │     │
│ │ Redis Cache            │ 🟢 Healthy│ Storage  │ 30 sec ago      │     │
│ │ RabbitMQ Broker        │ 🟢 Healthy│ External │ 1 min ago       │     │
│ │ ML Model Service       │ 🟢 Healthy│ AI       │ 2 min ago       │     │
│ │ Payment Gateway API    │ 🟢 Healthy│ External │ 3 min ago       │     │
│ │ AWS S3 Bucket          │ 🟢 Healthy│ Storage  │ 2 min ago       │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ ACTIVE ALERTS & EVENTS                                                   │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ No active alerts - System operating normally                    │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ DEPENDENCY MAP                                                           │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │            [API Gateway]                                         │     │
│ │              ├─→ [PostgreSQL Primary] 🟢                         │     │
│ │              ├─→ [PostgreSQL Replica] 🟢                         │     │
│ │              ├─→ [Redis Cache] 🟢                                │     │
│ │              ├─→ [RabbitMQ] 🟢                                   │     │
│ │              └─→ [ML Service] 🟢                                 │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ 24-HOUR TREND DATA                                                       │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ CPU Usage (24h)         │ Uptime %                              │     │
│ │  80% ├─────────────────┤ 100%├──────────────────────────────┤  │     │
│ │  40% ├───╭╮──╮╭╭─╮──╭─┤ 99% ├────────────────────────────╯┤  │     │
│ │   0% └───┴┴──┴┴┴─┴──┴─┘ 98% └──────────────────────────────┘  │     │
│ └─────────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Alert Management Panel

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Active Alerts Management                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│ Filter: [All Severities ▼] [All Components ▼] [All Status ▼] [Search...] │
│                                                                           │
│ CRITICAL ALERTS (1)                                                      │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ [🔴] Database Connection Pool Exhausted                          │     │
│ │  Component: PostgreSQL Primary | Triggered: 2 minutes ago        │     │
│ │  Description: Connection pool reached 95% utilization with 48    │     │
│ │  of 50 connections active. Response time increased by 300%.      │     │
│ │  Status: [ACTIVE] | Escalated: Yes                              │     │
│ │  [Acknowledge Alert] [View Details]                             │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ WARNING ALERTS (2)                                                       │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ [🟡] Disk Usage Warning                                          │     │
│ │  Component: System Disk | Triggered: 15 minutes ago              │     │
│ │  Description: Disk usage reached 85% of capacity (425 GB of      │     │
│ │  500 GB used). Consider cleanup or expansion.                   │     │
│ │  Status: [ACKNOWLEDGED by ops-team] | Escalates in 10 min       │     │
│ │  [Resolve Alert] [View Details]                                 │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ [🟡] API Response Time Degradation                               │     │
│ │  Component: API Gateway | Triggered: 8 minutes ago               │     │
│ │  Description: p95 response time increased to 850ms (baseline:    │     │
│ │  200ms). Possible database performance issue.                   │     │
│ │  Status: [ACTIVE] | Escalates in 14 min                         │     │
│ │  [Acknowledge Alert] [View Details]                             │     │
│ └─────────────────────────────────────────────────────────────────┘     │
│                                                                           │
│ RECENT ALERTS (Resolved)                                                 │
│ ┌─────────────────────────────────────────────────────────────────┐     │
│ │ ✓ Cache Service Recovery | Resolved 3 hours ago by auto-monitor │     │
│ │ ✓ Network Latency Warning | Resolved 8 hours ago by ops-team    │     │
│ └─────────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Dependency Chain

### 10.1 Service Dependency Map

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Service Dependencies                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ TIER 1: Core Application Services (Critical - S0 SLA)              │
│  ├─ API Gateway                                                    │
│  ├─ Authentication Service                                         │
│  └─ Authorization Service                                          │
│                           │                                        │
├───────────────────────────┼───────────────────────────────┬────────┤
│                           │                               │        │
│  TIER 2: Data Services (Critical - S1 SLA)              │        │
│  ├─ PostgreSQL Primary (RW)  ◄────┐                      │        │
│  ├─ PostgreSQL Replica (RO)   ◄───┤ Database Cluster    │        │
│  ├─ PostgreSQL Failover (HB)  ◄───┘                      │        │
│  ├─ Redis Cache                                          │        │
│  └─ Connection Pooler (PgBouncer)                       │        │
│                           │                               │        │
├───────────────────────────┼───────────────────────┬───────┼────────┤
│                           │                       │       │        │
│  TIER 3: Message Queue (Important - S2 SLA)     │       │        │
│  ├─ RabbitMQ Broker (HA Cluster) ◄──────────────┘       │        │
│  ├─ RabbitMQ Node 2                                      │        │
│  └─ RabbitMQ Node 3                                      │        │
│           │                                              │        │
│           └──────────────────────┬──────────────────────┤        │
│                                  │                       │        │
│  TIER 4: Processing Services (Important)                 │        │
│  ├─ Worker Nodes (1-5)                                   │        │
│  ├─ Background Job Processor                             │        │
│  ├─ Report Generator                                     │        │
│  └─ File Processor                                       │        │
│                                                          │        │
│  TIER 5: External Integrations (Optional/Graceful Deg.) │        │
│  ├─ Payment Gateway (Stripe)  ◄───────────────────────┘ │        │
│  ├─ Email Service (SendGrid)                             │        │
│  ├─ SMS Gateway (Twilio)                                 │        │
│  ├─ Analytics (Mixpanel)                                 │        │
│  └─ Cloud Storage (AWS S3)                               │        │
│                                                          │        │
│  TIER 6: ML/AI Services (Optional/Graceful Deg.)        │        │
│  ├─ ML Model Service (TensorFlow)                        │        │
│  ├─ NLP Service                                          │        │
│  └─ Computer Vision Service                              │        │
│                                                          │        │
│  TIER 7: Monitoring & Observability (Support)           │        │
│  ├─ Prometheus Server                                    │        │
│  ├─ Grafana Dashboard                                    │        │
│  ├─ ELK Stack (Elasticsearch, Logstash, Kibana)         │        │
│  ├─ Jaeger Tracing                                       │        │
│  └─ Alert Manager                                        │        │
│                                                          │        │
└──────────────────────────────────────────────────────────┴────────┘
```

### 10.2 Failure Cascade Impact

```
IMPACT ANALYSIS OF CRITICAL FAILURES:

Failure: PostgreSQL Primary Down
├─ Immediate Impact:
│  ├─ All write operations fail (Data modification unavailable)
│  ├─ Replica handles reads only (Limited to read-only mode)
│  └─ Backup system activates failover
├─ Affected Services: API Gateway, All workers, All consumers
├─ User Impact: Cannot create/modify data, read-only mode
├─ Recovery Time: ~30-60 seconds (automatic failover)
└─ Mitigation: Automatic failover to replica (promoted)

Failure: Redis Cache Down
├─ Immediate Impact:
│  ├─ All cache misses (Performance degradation)
│  ├─ Database query load increases significantly
│  └─ Session storage falls back to database
├─ Affected Services: API Gateway, Worker processes
├─ User Impact: Slower response times, session latency
├─ Recovery Time: ~2-5 minutes (cache rebuild)
└─ Mitigation: Automatic fallback to database

Failure: RabbitMQ Broker Down
├─ Immediate Impact:
│  ├─ Async processing queue backs up
│  ├─ Messages not delivered to workers
│  └─ Background jobs not processed
├─ Affected Services: Worker nodes, all queue consumers
├─ User Impact: Delayed reports, email, notifications
├─ Recovery Time: ~5-10 minutes (queue replay)
└─ Mitigation: Messages persisted to disk, automatic replay

Failure: API Gateway Down
├─ Immediate Impact:
│  ├─ All external requests blocked
│  ├─ No client access to system
│  └─ Internal services isolated
├─ Affected Services: All external consumers
├─ User Impact: Complete service unavailability
├─ Recovery Time: ~1-2 minutes (load balancer redirect)
└─ Mitigation: Load balancer with secondary gateway

Failure: External API (Payment Gateway)
├─ Immediate Impact:
│  ├─ Payment processing blocked
│  ├─ Subscription renewal fails
│  └─ Order checkout blocked
├─ Affected Services: Billing, Order processing
├─ User Impact: Cannot complete purchases
├─ Recovery Time: Depends on external provider (10 min - 1 hour)
└─ Mitigation: Queue transactions, automatic retry, alerting
```

---

## 11. License Gating Table

| Feature | License Tier | Details |
|---------|--------------|---------|
| **Core Health Checks** | Free/Pro/Enterprise | Basic health check framework for core services |
| **Advanced Health Checks** | Pro/Enterprise | AI services, complex dependency checks, advanced metrics |
| **Status Dashboard** | Free/Pro/Enterprise | Basic dashboard included in all tiers |
| **Advanced Dashboard Features** | Enterprise | Custom dashboards, scheduled reports, integrations |
| **Proactive Alerting (Basic)** | Pro/Enterprise | Email and webhook alerts |
| **Advanced Alerting** | Enterprise | SMS, Slack/Teams, escalation automation |
| **Resource Monitoring (Core)** | Pro/Enterprise | CPU, Memory, Disk monitoring |
| **Advanced Resource Monitoring** | Enterprise | Per-process metrics, anomaly detection, forecasting |
| **Dependency Health** | Enterprise | Complete dependency chain monitoring |
| **Health API Endpoints** | Pro/Enterprise | REST API for health status |
| **Prometheus Export** | Enterprise | Prometheus-format metrics export |
| **Alert History & Audit** | Enterprise | Long-term alert history and compliance audit trail |
| **Custom Alert Rules** | Enterprise | Create custom alert rules |
| **Suppression Rules** | Enterprise | Maintenance windows and alert suppression |
| **Multi-tenant Dashboards** | Enterprise | Separate dashboards per tenant/environment |
| **API Rate Limiting** | Enterprise | Configurable API rate limits per client |
| **RBAC Integration** | Enterprise | Role-based access control for dashboard/API |

---

## 12. Performance Targets

### 12.1 Health Check Performance

```
Target: Individual health check execution < 500ms (p95)
├─ Core checks: < 100ms (fast validation logic)
├─ Database checks: < 200ms (including query execution)
├─ Network checks: < 300ms (including timeouts)
├─ AI/ML checks: < 500ms (model inference)
└─ External API checks: < 400ms (remote call with timeout)

Target: Full health check suite < 5 seconds (p95)
├─ Parallel execution with 10 concurrent checks
├─ Sequential fallback if parallelization fails
├─ Overall timeout: 30 seconds (hard limit)
└─ Abort and cache result if exceeds timeout

Target: Health check result caching < 10ms retrieval
├─ In-memory cache with TTL (5-60 seconds)
├─ Redis backup cache for distributed scenarios
└─ Cache hit ratio: > 95% for repeated requests
```

### 12.2 Alert Performance

```
Target: Alert generation latency < 50ms (p95)
├─ Rule evaluation: < 10ms per rule
├─ Total active rules: < 100 (with 50 evaluating at once)
├─ Alert deduplication: < 5ms
└─ Event publishing: < 20ms

Target: Alert delivery latency < 100ms (p99)
├─ In-app notification: < 50ms
├─ Message queue publish: < 20ms
├─ Webhook dispatch: < 100ms (async)
└─ Email/SMS queue: < 50ms

Target: Alert acknowledgment/resolution < 200ms
├─ Database update: < 100ms
├─ Event publishing: < 50ms
└─ Cache invalidation: < 30ms
```

### 12.3 Dashboard Performance

```
Target: Dashboard page load < 2 seconds (initial + data)
├─ HTML/CSS/JS load: < 500ms
├─ Initial health data load: < 1 second
├─ Resource metrics load: < 500ms
└─ Chart data load: < 300ms

Target: Dashboard auto-refresh < 5 seconds
├─ Data fetch interval: 5 seconds
├─ Chart update rendering: < 500ms
├─ Alert panel update: < 500ms
└─ Gauge animations: < 300ms

Target: Search/filter response < 500ms
├─ Component search: < 200ms
├─ Alert filtering: < 300ms
└─ Time range queries: < 400ms
```

### 12.4 Resource Monitoring Performance

```
Target: Metrics collection overhead < 5% CPU impact
├─ Per-30-second collection: < 50ms execution time
├─ Memory overhead: < 100MB in-process
└─ Disk I/O: < 1MB per collection

Target: Metrics data point ingestion < 1ms per metric
├─ 100+ metrics per collection point
├─ Aggregation computation: < 100ms for 30-day window
└─ Anomaly detection: < 500ms (async processing)
```

### 12.5 API Performance

```
Target: Health endpoint response < 100ms (p95)
├─ Cached result return: < 10ms
├─ Fresh evaluation: < 5 seconds
└─ Error response: < 50ms

Target: Metrics endpoint response < 500ms
├─ Current metrics: < 100ms
├─ Historical data: < 400ms
└─ Prometheus export: < 300ms

Target: Alert CRUD operations < 200ms
├─ List alerts: < 150ms (with pagination)
├─ Get alert details: < 100ms
├─ Acknowledge/Resolve: < 100ms
└─ Create alert: < 150ms

Target: Dashboard data endpoint < 500ms
├─ Summary data: < 200ms
├─ Component health: < 200ms
├─ Resource metrics: < 150ms
└─ Alert summary: < 150ms
```

---

## 13. Testing Strategy

### 13.1 Unit Testing

**Health Check Framework**
- Test IHealthCheckManager registration and execution
- Test parallel execution with timeout handling
- Test result caching with TTL
- Test worst-case health status aggregation
- Test individual IHealthCheck implementations

**Alert Manager**
- Test rule condition evaluation
- Test alert deduplication within time window
- Test escalation logic and timing
- Test alert acknowledgment and resolution
- Test suppression window logic

**Resource Monitor**
- Test metrics collection accuracy
- Test threshold violation detection
- Test anomaly detection algorithms
- Test data aggregation at various granularities
- Test historical data retrieval

**Dependency Health Service**
- Test dependency registration and tracking
- Test health check execution per dependency
- Test connection pool monitoring
- Test dependency chain calculation
- Test impact analysis calculations

### 13.2 Integration Testing

**Health Check Integration**
- Test end-to-end health check execution with real components
- Test database health checks against live database
- Test network health checks with real DNS and latency
- Test external API health checks with mock APIs
- Test resource monitoring with actual system metrics

**Alert Integration**
- Test alert triggering from health check failures
- Test alert delivery through multiple channels
- Test escalation with time delays
- Test multi-tenant alert isolation
- Test alert history persistence and retrieval

**Dashboard Integration**
- Test data aggregation from all sources
- Test real-time data updates
- Test chart rendering with historical data
- Test filtering and search across all components
- Test dependency map visualization

**API Integration**
- Test all REST endpoints with valid and invalid inputs
- Test Kubernetes probe endpoints with state changes
- Test Prometheus metrics export format
- Test API authentication and authorization
- Test rate limiting and quota enforcement

### 13.3 Performance Testing

**Load Testing**
- Simulate 100+ concurrent health checks
- Simulate 1000 metrics data points per collection
- Simulate 10,000+ alerts over time
- Simulate 50+ concurrent dashboard users
- Simulate 1000 API requests per second

**Stress Testing**
- Test system behavior when all components unhealthy
- Test alert system under massive alert flood (1000+ per minute)
- Test resource monitor with high-frequency collection
- Test dashboard with 1+ year of historical data
- Test API under 10x normal load

**Soak Testing**
- Run health checks continuously for 72+ hours
- Monitor memory leaks and resource exhaustion
- Verify cache coherence over extended periods
- Monitor alert queue stability
- Check database partitioning and cleanup

### 13.4 Chaos Engineering

**Failure Scenarios**
- Database connection pool exhaustion
- Redis cache becoming unavailable
- RabbitMQ broker restart
- Network latency spikes (100ms → 5s)
- Cascading service failures
- Clock skew and time synchronization issues

**Recovery Testing**
- Automatic recovery after temporary outage
- Data consistency after failover
- Alert state consistency across instances
- Cache rebuild after loss
- Dependency re-establishment

### 13.5 Security Testing

**Access Control**
- Verify API authentication enforcement
- Verify role-based dashboard access
- Verify alert history privacy per tenant
- Verify metrics privacy enforcement
- Verify no sensitive data in error messages

**Data Protection**
- Verify encryption of sensitive metrics
- Verify secure transmission of alerts
- Verify secure storage of alert credentials
- Verify no PII in diagnostic data
- Verify compliance with data retention policies

---

## 14. Risks & Mitigations

### 14.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| **Health check timeout causing cascade failures** | High | High | Implement semaphore limiting concurrent checks, use per-check timeouts with circuit breaker pattern |
| **Alert fatigue from excessive alerting** | Medium | High | Implement deduplication, aggregation, suppression rules, severity-based delivery |
| **Resource monitoring overhead impacts performance** | Medium | Medium | Use sampling, async collection, limit metric granularity during high load |
| **Database partitioning query performance degrades** | Low | Medium | Use materialized views for common queries, implement query optimization |
| **Cache consistency issues with distributed setup** | Low | High | Use cache invalidation events, implement cross-instance synchronization |
| **External API dependency making system unavailable** | Medium | High | Implement graceful degradation, fallback mechanisms, circuit breakers |
| **Anomaly detection false positives** | Medium | Low | Use multiple detection algorithms, implement tuning, manual review process |
| **Clock synchronization issues affecting timestamps** | Low | Medium | Use NTP, centralized time source, audit log timestamps |

### 14.2 Operational Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| **Alert manager crashes losing pending alerts** | Low | Medium | Implement persistent alert queue, replay mechanism, monitoring of manager health |
| **Dashboard not reflecting real-time state** | Medium | Medium | Implement refresh invalidation on state change, socket-based updates for real-time |
| **Incorrectly configured alert rules triggering false positives** | Medium | High | Implement dry-run mode for new rules, gradual rollout, rule versioning and rollback |
| **Maintenance window suppression rules interfering** | Low | Medium | Implement rule priority system, explicit override capability, audit logging |
| **Storage overflow from alert and metric retention** | Medium | Medium | Implement aggressive partitioning, automated archive/purge, quota enforcement |
| **API rate limiting causing legitimate clients to be blocked** | Medium | Medium | Implement whitelist, dynamic limits based on usage patterns, quota reset |
| **Multi-tenant data isolation breach** | Low | High | Implement mandatory tenant filtering on all queries, audit tenant access logs |

### 14.3 Business Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| **Alert system becoming unusable due to noise** | Medium | High | Implement intelligent alerting with baselines, machine learning filtering |
| **Operators ignoring alerts (alert fatigue)** | High | High | Implement escalation, multi-channel delivery, alert routing to on-call |
| **Costs of infrastructure for health monitoring** | Medium | Medium | Implement data lifecycle policies, compression, off-peak archival |
| **Regulatory compliance with audit requirements** | Medium | High | Implement immutable audit logs, retention policies per regulation, compliance reporting |

---

## 15. MediatR Events

### 15.1 Health Status Changed Event

```csharp
/// <summary>
/// Published when health status of any component changes.
/// Used for dashboard updates, notifications, and metrics.
/// </summary>
public class HealthStatusChangedEvent : INotification
{
    /// <summary>Unique identifier for the component</summary>
    public string ComponentId { get; set; }

    /// <summary>Component display name</summary>
    public string ComponentName { get; set; }

    /// <summary>Health status category</summary>
    public HealthCheckCategory Category { get; set; }

    /// <summary>Previous health status</summary>
    public HealthStatus PreviousStatus { get; set; }

    /// <summary>New health status</summary>
    public HealthStatus NewStatus { get; set; }

    /// <summary>Reason for status change</summary>
    public string Reason { get; set; }

    /// <summary>Timestamp of status change</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>Is this component critical to system health</summary>
    public bool IsCritical { get; set; }

    /// <summary>Additional context data</summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Handler for health status changes - updates dashboard and persists history.
/// </summary>
public class HealthStatusChangedEventHandler : INotificationHandler<HealthStatusChangedEvent>
{
    private readonly IStatusDashboard _dashboard;
    private readonly ILogger<HealthStatusChangedEventHandler> _logger;

    public async Task Handle(HealthStatusChangedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Component {ComponentId} status changed from {Previous} to {New}",
            evt.ComponentId,
            evt.PreviousStatus,
            evt.NewStatus);

        // Update dashboard state
        await _dashboard.UpdateComponentHealthAsync(evt.ComponentId, evt.NewStatus);

        // Trigger dashboard refresh
        // Publish WebSocket update for real-time clients
        // Update cached health summary
    }
}
```

### 15.2 Alert Triggered Event

```csharp
/// <summary>
/// Published when alert rule conditions are met and alert is triggered.
/// </summary>
public class AlertTriggeredEvent : INotification
{
    /// <summary>Unique alert identifier</summary>
    public string AlertId { get; set; }

    /// <summary>Rule that triggered this alert</summary>
    public string RuleId { get; set; }

    /// <summary>Alert title</summary>
    public string Title { get; set; }

    /// <summary>Detailed description</summary>
    public string Description { get; set; }

    /// <summary>Alert severity level</summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>Affected component</summary>
    public string ComponentId { get; set; }

    /// <summary>Source system that triggered alert</summary>
    public string SourceSystem { get; set; }

    /// <summary>When alert was triggered</summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>Notification channels to use</summary>
    public IEnumerable<NotificationChannel> NotificationChannels { get; set; }

    /// <summary>Additional context for alert</summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Handler for alert triggering - dispatches notifications.
/// </summary>
public class AlertTriggeredEventHandler : INotificationHandler<AlertTriggeredEvent>
{
    private readonly IProactiveAlertManager _alertManager;
    private readonly IEmailNotificationService _emailService;
    private readonly ISlackNotificationService _slackService;
    private readonly ILogger<AlertTriggeredEventHandler> _logger;

    public async Task Handle(AlertTriggeredEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Alert {AlertId} triggered with severity {Severity}",
            evt.AlertId,
            evt.Severity);

        // Save alert to database
        await _alertManager.TriggerAlertAsync(
            new AlertDefinition
            {
                Title = evt.Title,
                Description = evt.Description,
                Severity = evt.Severity,
                ComponentId = evt.ComponentId,
                SourceSystem = evt.SourceSystem,
                Context = evt.Context
            });

        // Dispatch notifications per channel
        foreach (var channel in evt.NotificationChannels)
        {
            await DispatchNotificationAsync(channel, evt, cancellationToken);
        }
    }

    private async Task DispatchNotificationAsync(
        NotificationChannel channel,
        AlertTriggeredEvent evt,
        CancellationToken cancellationToken)
    {
        switch (channel)
        {
            case NotificationChannel.Email:
                await _emailService.SendAlertEmailAsync(evt, cancellationToken);
                break;
            case NotificationChannel.Slack:
                await _slackService.SendAlertMessageAsync(evt, cancellationToken);
                break;
            // Other channels...
        }
    }
}
```

### 15.3 Alert Acknowledged Event

```csharp
/// <summary>
/// Published when an alert is acknowledged by an operator.
/// </summary>
public class AlertAcknowledgedEvent : INotification
{
    /// <summary>Alert identifier</summary>
    public string AlertId { get; set; }

    /// <summary>User who acknowledged the alert</summary>
    public string AcknowledgedBy { get; set; }

    /// <summary>Optional notes from operator</summary>
    public string? Notes { get; set; }

    /// <summary>When acknowledgment was recorded</summary>
    public DateTime AcknowledgedAt { get; set; }
}

/// <summary>
/// Handler for alert acknowledgment - stops escalation.
/// </summary>
public class AlertAcknowledgedEventHandler : INotificationHandler<AlertAcknowledgedEvent>
{
    private readonly IProactiveAlertManager _alertManager;
    private readonly ILogger<AlertAcknowledgedEventHandler> _logger;

    public async Task Handle(AlertAcknowledgedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Alert {AlertId} acknowledged by {User}",
            evt.AlertId,
            evt.AcknowledgedBy);

        // Cancel any pending escalation tasks
        // Update alert state
        // Notify dashboard observers
    }
}
```

### 15.4 Resource Threshold Exceeded Event

```csharp
/// <summary>
/// Published when resource usage exceeds configured thresholds.
/// </summary>
public class ResourceThresholdExceededEvent : INotification
{
    /// <summary>Type of resource (CPU, Memory, Disk, Network)</summary>
    public string ResourceType { get; set; }

    /// <summary>Current resource usage value</summary>
    public double CurrentValue { get; set; }

    /// <summary>Threshold that was exceeded</summary>
    public double ThresholdValue { get; set; }

    /// <summary>Severity of threshold violation</summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>When threshold was exceeded</summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>How long threshold has been exceeded</summary>
    public TimeSpan DurationExceeded { get; set; }

    /// <summary>Affected process or system-wide</summary>
    public int? ProcessId { get; set; }
}

/// <summary>
/// Handler for resource threshold violations - triggers alerts.
/// </summary>
public class ResourceThresholdExceededEventHandler : INotificationHandler<ResourceThresholdExceededEvent>
{
    private readonly IProactiveAlertManager _alertManager;
    private readonly ILogger<ResourceThresholdExceededEventHandler> _logger;

    public async Task Handle(ResourceThresholdExceededEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "{ResourceType} exceeded threshold: {Current}% > {Threshold}%",
            evt.ResourceType,
            evt.CurrentValue,
            evt.ThresholdValue);

        // Trigger alert based on severity
        var alert = new AlertDefinition
        {
            Title = $"{evt.ResourceType} Resource Alert",
            Description = $"{evt.ResourceType} usage at {evt.CurrentValue:F1}% " +
                         $"(threshold: {evt.ThresholdValue:F1}%)",
            Severity = evt.Severity,
            SourceSystem = "ResourceMonitor",
            Context = new Dictionary<string, object>
            {
                { "ResourceType", evt.ResourceType },
                { "CurrentValue", evt.CurrentValue },
                { "ThresholdValue", evt.ThresholdValue },
                { "DurationExceeded", evt.DurationExceeded.TotalSeconds }
            }
        };

        await _alertManager.TriggerAlertAsync(alert);
    }
}
```

### 15.5 Dependency Health Changed Event

```csharp
/// <summary>
/// Published when external dependency health status changes.
/// </summary>
public class DependencyHealthChangedEvent : INotification
{
    /// <summary>Dependency identifier</summary>
    public string DependencyId { get; set; }

    /// <summary>Dependency display name</summary>
    public string Name { get; set; }

    /// <summary>Dependency type</summary>
    public DependencyType Type { get; set; }

    /// <summary>Previous health status</summary>
    public HealthStatus PreviousStatus { get; set; }

    /// <summary>New health status</summary>
    public HealthStatus NewStatus { get; set; }

    /// <summary>Status message or error description</summary>
    public string StatusMessage { get; set; }

    /// <summary>When status changed</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>Is this dependency critical</summary>
    public bool IsCritical { get; set; }
}
```

---

## 16. Implementation Timeline

### Phase 1: Foundation (Weeks 1-2, 12 hours)
- v0.19.5a: Health Check Framework
  - Implement IHealthCheckManager and execution engine
  - Create base IHealthCheck interface
  - Implement core health checks
  - Set up result caching and metrics

### Phase 2: Monitoring Infrastructure (Weeks 3-4, 18 hours)
- v0.19.5d: Resource Monitoring (10 hours)
  - Implement IResourceMonitor
  - Set up metrics collection
  - Create threshold checking
  - Build aggregation pipelines

- v0.19.5e: Dependency Health (8 hours)
  - Implement IDependencyHealthService
  - Create dependency registry
  - Set up health checks per dependency type
  - Build dependency chain analysis

### Phase 3: Alerting System (Week 5, 10 hours)
- v0.19.5c: Proactive Alerting
  - Implement IProactiveAlertManager
  - Create alert rule engine
  - Set up alert deduplication
  - Configure multi-channel delivery

### Phase 4: Presentation Layer (Week 6, 18 hours)
- v0.19.5b: Status Dashboard UI (10 hours)
  - Create dashboard backend aggregation
  - Build React/Vue components
  - Implement real-time updates
  - Add search and filtering

- v0.19.5f: Health API & Endpoints (8 hours)
  - Implement IHealthApiEndpoints
  - Create REST API controllers
  - Add authentication/authorization
  - Document API with OpenAPI

### Phase 5: Integration & Testing (Week 7, Testing tasks)
- End-to-end testing
- Performance tuning
- Documentation
- Production deployment

---

## 17. Success Metrics

### Adoption Metrics
- 100% of critical services covered by health checks
- > 95% uptime dashboard availability
- Alert acknowledgment rate > 90% within 5 minutes
- MTTR reduction of 60-80% vs. pre-v0.19.5

### Performance Metrics
- Health check suite completes < 5 seconds (p95)
- Alert delivery < 100 milliseconds
- Dashboard loads < 2 seconds
- API endpoints respond < 500 milliseconds

### Quality Metrics
- Zero false positive rates > 5% tolerance
- 99.95%+ health monitoring availability
- <0.1% data loss rate
- Alert accuracy > 95%

### User Satisfaction
- Operations team adoption > 85% daily usage
- Dashboard NPS > 40
- Alert rules created and maintained by team
- Integration into existing monitoring tools

---

## 18. References and Related Documentation

- Kubernetes Liveness & Readiness Probe Specification
- Prometheus Metrics Format Documentation
- OpenAPI 3.0 Specification
- PostgreSQL Partitioning Best Practices
- Circuit Breaker Pattern Implementation
- MediatR Event Publishing Best Practices
- ASP.NET Core Health Checks Middleware

---

**Document End**

Version: 1.0
Last Updated: 2026-02-01
Total Document Length: 2,500+ lines
