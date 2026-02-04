# LCS-DS-v0.18.5c-SEC: Design Specification — Compliance Report Generator

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.5c-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.5-SEC                          |
| **Release Version**   | v0.18.5c                                     |
| **Component Name**    | Compliance Report Generator                  |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Compliance Report Generator** (v0.18.5c). This component automates the labor-intensive process of gathering evidence for security audits. It maps system events and policies to specific requirements of frameworks like SOC2, HIPAA, and GDPR, generating professional-grade reports.

---

## 3. Detailed Design

### 3.1. Objective

Automate the generation of evidence-backed compliance reports to reduce audit costs and preparation time.

### 3.2. Scope

-   Define `IComplianceReportGenerator`.
-   Implement mapping engine: `Control Requirement` <-> `Audit Query`.
-   Support PDF and Excel export.
-   Templates for: SOC2 Type II, HIPAA, PCI-DSS, GDPR.
-   Scheduling of periodic reports.

### 3.3. Detailed Architecture

```mermaid
graph TD
    User -->|Request Report| Generator[ReportGenerator];
    Generator --> Template[Load Template (e.g. SOC2)];
    Template --> Requirements[List Controls];
    Requirements --> QueryEngine[AuditQueryEngine];
    QueryEngine --> DB[(Audit Logs)];
    DB --> Evidence[Evidence Set];
    Evidence --> Renderer[DocumentRenderer];
    Renderer --> PDF[Report.pdf];
```

#### 3.3.1. Data Mapping

-   **Control**: "A1.1 Access Control"
-   **Mapping**: Query `audit_logs` where `Action in ('Login_Success', 'Login_Failure')` AND `Timestamp` in Period.
-   **Evidence**: "Total Login Attempts: 10,500. Failed: 50. MFA Usage: 99%."

### 3.4. Interfaces & Data Models

```csharp
public interface IComplianceReportGenerator
{
    Task<ComplianceReport> GenerateReportAsync(
        ComplianceFramework framework,
        DateRange period,
        ReportOptions options,
        CancellationToken ct = default);

    Task<Stream> ExportToPdfAsync(ComplianceReport report, CancellationToken ct = default);
}

public record ComplianceReport
{
    public ComplianceFramework Framework { get; init; }
    public DateRange Period { get; init; }
    public double ComplianceScore { get; init; }
    public IReadOnlyList<ControlResult> Results { get; init; }
}

public record ControlResult(
    string ControlId,
    bool Passed,
    string EvidenceSummary,
    IReadOnlyList<string> EvidenceLinks);
```

### 3.5. Security Considerations

-   **Report Sensitivity**: Generated reports expose valid/invalid controls. They must be stored securely and only accessible to auditors/admins.
-   **Evidence Leakage**: Evidence snippets in reports shouldn't contain raw sensitive data (masking rules apply).

### 3.6. Performance Considerations

-   **Heavy Queries**: Generating a 1-year report scans massive log volumes.
    -   *Strategy*: Utilize `Summary Tables` (Daily Stats) for long-term trends instead of raw log counting where possible. Run as a background job.

### 3.7. Testing Strategy

-   **Data Verification**: Manually count a specific event type and ensure report matches.
-   **Rendering**: Verify PDF layout is correct and readable.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `ReportGenerator`        | Core logic.                                                              |
| `EvidenceMapper`         | Translates controls to SQL/ES queries.                                   |
| `PdfRenderer`            | Uses libraries like iText/QuestPDF.                                      |

---

## 5. Acceptance Criteria

-   [ ] **Frameworks**: SOC2 and HIPAA templates are functional.
-   [ ] **Evidence**: Reports include actual data counts, not just placeholders.
-   [ ] **Format**: Exports valid PDF files.
-   [ ] **Performance**: 30-day report generates in <30 seconds.
