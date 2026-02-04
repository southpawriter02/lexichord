# LCS-DS-v0.18.2d-SEC: Design Specification — Safe Execution Environment

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.2d-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.2-SEC                          |
| **Release Version**   | v0.18.2d                                     |
| **Component Name**    | Safe Execution Environment                   |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Safe Execution Environment** (v0.18.2d). This component is the enforcement arm of the Command Sandboxing system, responsible for the actual execution of approved commands. It ensures that commands run within strictly defined boundaries, isolated from critical system resources, and constrained by resource quotas (CPU, memory, disk). It captures all outputs for auditing and handles process lifecycle management (timeouts, cancellation, cleanup).

---

## 3. Detailed Design

### 3.1. Objective

Implement a robust, cross-platform execution engine that isolates command execution processes, enforcing resource limits and preventing unauthorized system access while capturing comprehensive execution telemetry.

### 3.2. Scope

-   Define `ISandboxedExecutor` and supporting interfaces.
-   Implement platform-specific isolation strategies:
    -   **Linux**: Process-level isolation using namespaces (via `unshare` or similar) and resource control via cgroups.
    -   **Windows**: Process-level isolation using Job Objects for resource limits and lifecycle management.
    -   **Containerized**: Optional Docker-based isolation for high-risk commands.
-   Enforce resource quotas: CPU usage, Memory limits, Disk I/O throughput, and Execution time.
-   Capture standard output (stdout) and standard error (stderr) in real-time.
-   Ensure reliable process termination and cleanup of temporary resources.
-   Integrate with the Rollback system (v0.18.2f) by providing pre/post execution hooks.

### 3.3. Detailed Architecture

The `SandboxedExecutor` acts as a facade, delegating the actual process creation and management to platform-specific `IIsolationProvider` implementations. This allows the system to run on diverse environments while maintaining a consistent API.

```mermaid
graph TD
    A[SandboxedExecutor] --> B{IsolationProviderFactory};
    B -- Linux --> C[LinuxIsolationProvider];
    B -- Windows --> D[WindowsIsolationProvider];
    B -- Docker --> E[DockerIsolationProvider];

    subgraph Linux Implementation
        C --> C1[System-calls (unshare, cgroups)];
        C --> C2[Process Tracking];
    end

    subgraph Windows Implementation
        D --> D1[Job Objects API];
        D --> D2[Process Tokens];
    end

    subgraph Common Services
        F[OutputCaptureService];
        G[ResourceMonitor];
        H[TimeoutInspector];
    end

    C --> F;
    D --> F;
    C --> G;
    D --> G;
```

#### 3.3.1. Isolation Strategy

1.  **Process Isolation**:
    -   **Linux**: Utilize `namespaces` to isolate the process view of the system (PID, Mount, Network). Use `cgroups v2` to enforce memory and CPU limits.
    -   **Windows**: Utilize **Job Objects** to group processes, enforcing hard limits on memory and CPU time, and ensuring that killing the job kills all child processes.

2.  **Filesystem & Network**:
    -   The executor will enforce working directory restrictions.
    -   Network access will be controlled via platform-specific firewall rules or namespace isolation (Linux network namespace) based on the permission grant.

3.  **Lifecycle Management**:
    -   Commands are wrapped in a `Job` or `CGroup`.
    -   Timeouts trigger a forceful termination of the entire generic group/job, preventing orphaned child processes.

### 3.4. Interfaces & Data Models

```csharp
/// <summary>
/// Executes approved commands within a secure, isolated environment.
/// </summary>
public interface ISandboxedExecutor
{
    /// <summary>
    /// Executes a command synchronously (waiting for completion) with the specified options.
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(
        ApprovedCommand command,
        SandboxOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command and returns an observable stream of its output.
    /// </summary>
    IObservable<ExecutionOutputEvent> ExecuteStreaming(
        ApprovedCommand command,
        SandboxOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Forcefully aborts a running execution.
    /// </summary>
    Task AbortAsync(ExecutionId executionId, CancellationToken ct = default);
}

public interface IIsolationProvider
{
    Task<IProcessHandle> SpawnProcessAsync(ProcessStartInfo startInfo, ResourceLimits limits, CancellationToken ct);
}

public record SandboxOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public long MaxMemoryBytes { get; init; } = 512 * 1024 * 1024; // 512MB
    public double MaxCpuPercent { get; init; } = 100.0;
    public bool AllowNetwork { get; init; } = false;
    public string WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string> Environment { get; init; }
}

public record ExecutionResult
{
    public ExecutionId Id { get; init; }
    public int ExitCode { get; init; }
    public string Stdout { get; init; }
    public string Stderr { get; init; }
    public ExecutionMetrics Metrics { get; init; }
    public bool TimedOut { get; init; }
    public bool WasAborted { get; init; }
}

public record ExecutionMetrics
{
    public TimeSpan TotalCpuTime { get; init; }
    public long PeakMemoryBytes { get; init; }
    public Timestamp StartTime { get; init; }
    public Timestamp EndTime { get; init; }
}
```

### 3.5. Security Considerations

-   **Escape Prevention**: The primary risk is a process breaking out of its sandbox. On Linux, `chroot` is insufficient; `pivot_root` and namespace isolation must be used where possible. On Windows, Job Objects provide strong containment for resource usage but less for filesystem visibility; additional ACLs may be needed.
-   **Resource Exhaustion**: Malicious commands might try to consume all memory or fork-bomb.
    -   *Mitigation*: Cgroups/Job Objects limits are hard limits. Fork bombs are prevented by process count limits in the job/cgroup.
-   **Side-Channel Attacks**: While difficult to fully prevent, isolation minimizes the surface area. High-sensitivity commands should use the Docker provider for stronger isolation.

### 3.6. Performance Considerations

-   **Startup Overhead**: Process instantiation should add negligible overhead (<50ms). Docker containers will have higher overhead (>500ms), so they should be reserved for high-risk or long-running tasks.
-   **I/O Streaming**: Output capture must use asynchronous reading to avoid blocking the executing process, especially for commands generating massive output.

### 3.7. Testing Strategy

-   **Isolation Tests**:
    -   Verify memory limits: run a command that allocates 1GB RAM against a 512MB limit; assert process is killed (OOM).
    -   Verify timeout: run `sleep 10` against a 1s timeout; assert termination.
    -   Verify child cleanup: run a script that spawns background processes; assert all are killed on parent termination.
-   **Platform Matrix**: Tests must run on both Windows and Linux CI agents to verify platform-specific implementations.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `ISandboxedExecutor`     | Core interface.                                                          |
| `LinuxIsolationProvider` | Implementation using cgroups/namespaces.                                 |
| `WindowsIsolationProvider`| Implementation using Job Objects.                                        |
| `OutputCaptureService`   | Handles async stream reading and buffer management.                      |
| Resource Limit Tests     | Unit/Integration tests verifying quotas are enforced.                    |

---

## 5. Acceptance Criteria

-   [ ] **Resource Enforcement**: Commands exceeding memory limits are terminated by the system.
-   [ ] **Timeouts**: Commands exceeding the duration limit are forcefully terminated.
-   [ ] **Cleanup**: No orphaned processes remain after execution finishes or is aborted.
-   [ ] **Output Capture**: Stdout and Stderr are correctly captured, even for commands that output large data (up to configured buffer limits).
-   [ ] **Exit Codes**: Correct exit codes are returned for success, failure, signal termination, and OOM.
-   [ ] **Platform Support**: Works correctly on both Windows (Job Objects) and Linux (Cgroups).
