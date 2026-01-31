# LCS-SBD: Scope Breakdown - v0.0.1
**Target Version:** `v0.0.1`
**Codename:** The Architecture Skeleton
**Timeline:** Sprint 0 (Foundation)
**Owner:** Lead Architect
**Prerequisites:** .NET 9 SDK installed, Docker Desktop installed.

## 1. Executive Summary
**v0.0.1** is the implementation of the "Modular Monolith" physical structure. No business logic or UI will be visible to a user. The success of this release is measured entirely by the **correctness of the dependency graph** and the **reliability of the build pipeline**.

If this foundation is flawed, the Plugin architecture (v0.0.4) will fail.

---

## 2. Sub-Part Specifications

### v0.0.1a: Solution Scaffolding
**Goal:** physical creation of the file system hierarchy and Solution file.

*   **Task 1.1: Root Setup**
    *   Initialize a new Git repository.
    *   Create a standard `.gitignore` (Visual Studio / .NET template).
    *   Create a `README.md` with the Lexichord title.
*   **Task 1.2: Directory Structure**
    *   Create `/src`.
    *   Create `/tests`.
    *   Create `/docs` (Move these LCS documents here).
*   **Task 1.3: Project Creation (The Big Three)**
    *   **Core:** Create `Lexichord.Host` (Console App -> to be converted to Avalonia later, or generic Class Lib for now). *Note: Use Avalonia Template if available, otherwise Console.*
    *   **Abstractions:** Create `Lexichord.Abstractions` (Class Library).
    *   **Modules:** Create folder `src/Lexichord.Modules`. (Empty for now).
*   **Task 1.4: Solution File**
    *   Create `Lexichord.sln`.
    *   Add projects to solution, organizing them into Virtual Solution Folders (`src`, `tests`).

**Definition of Done:**
*   Folder structure matches the spec.
*   `dotnet build` runs successfully (even if empty).
*   Repo is committed to source control.

---

### v0.0.1b: Project Reference Lock
**Goal:** Enforce the "Onion Architecture" / Modular Monolith rules via `.csproj` configuration.

*   **Task 1.1: Configure Abstractions**
    *   Edit `Lexichord.Abstractions.csproj`.
    *   **Constraint:** This project MUST NOT reference any other internal project.
    *   **Constraint:** Dependencies should be minimal (Standard system libraries).
*   **Task 1.2: Configure Host**
    *   Edit `Lexichord.Host.csproj`.
    *   Add Project Reference to `Lexichord.Abstractions`.
    *   **Constraint:** Host MUST NOT reference any project inside `src/Lexichord.Modules` (Circular dependency prevention).
*   **Task 1.3: Centralized Versioning (Optional but Recommended)**
    *   Create `Directory.Build.props` in the root.
    *   Define `<TargetFramework>net9.0</TargetFramework>`.
    *   Define `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.

**Technical Example (.csproj):**
```xml
<!-- Inside Lexichord.Host.csproj -->
<ItemGroup>
    <ProjectReference Include="..\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />
    <!-- DO NOT ADD REFERENCES TO MODULES HERE -->
</ItemGroup>
```

**Definition of Done:**
*   The Dependency Graph shows `Host -> Abstractions`.
*   There are no circular references.

---

### v0.0.1c: Test Suite Genesis
**Goal:** Establish the safety net before writing complex logic.

*   **Task 1.1: Unit Test Project**
    *   Create `Lexichord.Tests.Unit` (xUnit) in `/tests`.
    *   Add Reference to `Lexichord.Abstractions` and `Lexichord.Host`.
    *   Install NuGets: `FluentAssertions`, `Moq`, `xunit.runner.visualstudio`.
*   **Task 1.2: Integration Test Project**
    *   Create `Lexichord.Tests.Integration` (xUnit) in `/tests`.
    *   Install NuGets: `Testcontainers`, `Testcontainers.PostgreSql`.
*   **Task 1.3: The "Sanity" Test**
    *   Create `InfrastructureTests.cs` in Unit tests.
    *   Write a test `Fact` that asserts `true` is `true`.
*   **Task 1.4: The "Docker" Test**
    *   Create a test in Integration that spins up a generic PostgreSQL container, connects, and tears it down. This proves Docker Desktop is reachable.

**Code Example (Docker Check):**
```csharp
[Fact]
public async Task Docker_Container_Starts()
{
    var container = new PostgreSqlBuilder().Build();
    await container.StartAsync();
    Assert.True(container.State == Testcontainers.Containers.ResourceReaperState.Running); // Pseudo-code check
    await container.StopAsync();
}
```

**Definition of Done:**
*   `dotnet test` discovers and runs both test projects.
*   The Integration test successfully spins up a container.

---

### v0.0.1d: CI/CD Pipeline
**Goal:** Automate the "Build & Test" loop on GitHub.

*   **Task 1.1: Workflow File**
    *   Create `.github/workflows/ci.yml`.
*   **Task 1.2: Job Definition**
    *   **Trigger:** `push` to `main`, `pull_request` to `main`.
    *   **OS:** `ubuntu-latest`.
    *   **Step 1:** Checkout Code.
    *   **Step 2:** Setup .NET 9 (`actions/setup-dotnet`).
    *   **Step 3:** Restore Dependencies (`dotnet restore`).
    *   **Step 4:** Build (`dotnet build --no-restore --configuration Release`).
    *   **Step 5:** Test (`dotnet test --no-build --configuration Release`).
*   **Task 1.3: Enforcement**
    *   Configure the workflow to fail if any step returns a non-zero exit code.

**Definition of Done:**
*   Pushing the code triggers the Action.
*   The Action completes with a Green Checkmark.
*   Deliberately breaking the code (syntax error) causes a Red Cross (Email notification).

---

## 3. Implementation Checklist (for Developer)

| Step | Description | Status |
| :--- | :--- | :--- |
| **0.1a** | Created `src`, `tests`, `docs` folders. | [ ] |
| **0.1a** | Created `Lexichord.sln` and added projects. | [ ] |
| **0.1b** | `Directory.Build.props` created with .NET 9. | [ ] |
| **0.1b** | Host references Abstractions. | [ ] |
| **0.1c** | `dotnet test` runs 1 Unit Test & 1 Integration Test. | [ ] |
| **0.1d** | `.github/workflows/ci.yml` exists. | [ ] |
| **0.1d** | GitHub Action run #1 passed. | [ ] |

## 4. Risks & Mitigations
*   **Risk:** Docker not installed on Dev machine.
    *   *Mitigation:* `Lexichord.Tests.Integration` should be marked with `[Trait("Category", "Integration")]` so devs can run `dotnet test --filter Category!=Integration` if they lack Docker.
*   **Risk:** Circular Dependencies creep in.
    *   *Mitigation:* The `.csproj` structure prevents compile-time circular refs, but logical circular refs (Event loops) must be watched in code review later.