# LCS-DS-v0.18.5g-SEC: Design Specification — Security Dashboard

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.5g-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.5-SEC                          |
| **Release Version**   | v0.18.5g                                     |
| **Component Name**    | Security Dashboard                           |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Security Dashboard** (v0.18.5g). This component provides a centralized, real-time view of the system's security posture, aggregating metrics from the Audit Logger, Policy Engine, and Alert Service.

---

## 3. Detailed Design

### 3.1. Objective

Provide Security Operations (SecOps) teams with immediate visibility into threats, activity trends, and compliance status.

### 3.2. Scope

-   Define `ISecurityDashboard`.
-   Implement Metric Aggregation Service.
-   Widgets:
    -   Event Volume (Time Series).
    -   Threat Indicator (Low/Med/High).
    -   Active Alerts.
    -   Authentication Failures Heatmap.
-   Support Real-time updates (SignalR/WebSocket).

### 3.3. Detailed Architecture

Using Pre-calculated Aggregations to ensure speed.

```mermaid
graph TD
    Audit --> Aggregator[MetricAggregator];
    Alerts --> Aggregator;
    Aggregator --> Cache[Redis Stats Cache];
    
    User --> DashboardAPI;
    DashboardAPI --> Cache;
    DashboardAPI --> Repo for Drilldown;
```

#### 3.3.1. Aggregation Strategy

-   **Windowing**: 1-minute buckets for the last 24h. 1-hour buckets for 7 days.
-   **Storage**: Redis HyperLogLog for unique user counts. Sorted Sets for Top Violators.

### 3.4. Interfaces & Data Models

```csharp
public interface ISecurityDashboard
{
    Task<SecurityMetrics> GetRealtimeMetricsAsync(CancellationToken ct = default);
    
    Task<IEnumerable<TimePoint>> GetTrendAsync(MetricType metric, TimeRange range, CancellationToken ct = default);
    
    IAsyncEnumerable<DashboardUpdate> StreamUpdatesAsync(CancellationToken ct = default);
}

public record SecurityMetrics
{
    public int ActiveAlerts { get; init; }
    public double ThreatScore { get; init; } // 0-100
    public int BlocksLastHour { get; init; }
    public int TotalEventsLast24h { get; init; }
    public ComplianceStatus Compliance { get; init; }
}
```

### 3.5. Security Considerations

-   **Data sensitivity**: Summary data is less sensitive than raw logs but can still reveal operational patterns. Restrict to authorized rolse.

### 3.6. Performance Considerations

-   **Expensive Queries**: Dashboard load must NEVER trigger a full table scan of the audit log. Always use cached counters.

### 3.7. Testing Strategy

-   **Latency**: Ensure dashboard API returns <100ms.
-   **Accuracy**: Verify counters match underlying data (eventual consistency acceptable).

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `DashboardService`       | API Logic.                                                               |
| `MetricAggregator`       | Background worker.                                                       |
| `DashboardUI`            | React Library.                                                           |

---

## 5. Acceptance Criteria

-   [ ] **Load Time**: <2 seconds.
-   [ ] **Live Updates**: Metrics refresh without page reload.
-   [ ] **Dril-down**: Clicking a widget navigates to Audit Viewer with correct filters.
