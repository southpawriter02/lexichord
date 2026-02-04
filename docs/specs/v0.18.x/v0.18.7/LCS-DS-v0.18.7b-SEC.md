# LCS-DS-v0.18.7b-SEC: Design Specification — Privilege Containment & Least Privilege

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.7b-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.7-SEC                          |
| **Release Version**   | v0.18.7b                                     |
| **Component Name**    | Privilege Containment & Least Privilege      |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for **Privilege Containment & Least Privilege** (v0.18.7b). It ensures that the AI agent and any code it executes run with the absolute minimum set of operating system permissions required, eliminating the risk of system compromise via privilege escalation.

---

## 3. Detailed Design

### 3.1. Objective

Enforce "Least Privilege" by running agents as low-privileged users and stripping Linux capabilities.

### 3.2. Scope

-   Define `IPrivilegeManager`.
-   **User Switching**: Spawn worker processes as `nobody` or dedicated `lexi-worker` user.
-   **Capability Dropping**: Explicitly remove dangerous Linux capabilities (`CAP_SYS_ADMIN`, `CAP_NET_RAW`).
-   **Sudo Blocking**: Ensure the worker user is not in `sudoers`.

### 3.3. Detailed Architecture

```mermaid
graph TD
    AgentHost[Host Service (Root/Admin)] --> Manager[PrivilegeManager];
    Manager --> UserMap[User Mapping];
    Manager --> Caps[Capability Dropper];
    
    UserMap -- Selects --> LowPrivUser[lexi-worker-uuid];
    Caps -- Drops --> LimitedEnv;
    
    Manager -- Spawns --> WorkerProcess[Worker Process];
    WorkerProcess -- Runs In --> LimitedEnv;
```

#### 3.3.1. Implementation

-   **Linux**: Use `setuid` / `setgid` syscalls (wrapped in C# via `Mono.Posix` or P/Invoke). Use `prctl` to clear bounding set.
-   **Windows**: Use "Restricted Tokens" and Job Objects to limit API access.

### 3.4. Interfaces & Data Models

```csharp
public interface IPrivilegeManager
{
    Task<Process> SpawnLowPrivilegeProcessAsync(
        ProcessStartInfo startInfo,
        PrivilegeProfile profile,
        CancellationToken ct = default);
}

public enum PrivilegeProfile
{
    Strict, // No network, read-only FS
    Standard, // Net access, Workspace Write
    Elevated // (Reserved for Admin tools)
}
```

### 3.5. Security Considerations

-   **Kernel Exploits**: Low privilege doesn't stop kernel exploits (Dirty COW).
    -   *Mitigation*: Keep OS patched. Use Seccomp (covered in v0.18.7d) to reduce kernel attack surface.

### 3.6. Performance Considerations

-   **Startup Time**: Creating new OS users dynamically is slow.
    -   *Strategy*: Use a pool of pre-created worker users (`worker_01`...`worker_50`) or use containerization (namespaces) which is faster.

### 3.7. Testing Strategy

-   **Escalation Check**: Agent script runs `cat /etc/shadow`. Should fail. `sudo -v`. Should fail.
-   **Network Bind**: Agent tries to listen on port 80. Should fail (requires Root).

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `PrivilegeManager`       | Logic to drop permissions.                                               |
| `Interop`                | P/Invoke definitions for `libc`.                                         |

---

## 5. Acceptance Criteria

-   [ ] **Identity**: Logic runs as non-root URL.
-   [ ] **Capabilities**: `getcap` shows empty set on worker process.
-   [ ] **Write Access**: Cannot write to System Directories.
