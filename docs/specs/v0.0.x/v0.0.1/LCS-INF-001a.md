# LCS-01: Feature Design Composition

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `INF-001a` | Infrastructure - Solution Scaffolding |
| **Feature Name** | Solution Scaffolding & Hierarchy | The physical foundation of the codebase. |
| **Target Version** | `v0.0.1a` | The Genesis commit. |
| **Module Scope** | `Root` | Global Solution scope. |
| **Swimlane** | `Infrastructure` | The Podium (Platform). |
| **License Tier** | `Core` | Foundation (Required for all tiers). |
| **Feature Gate Key** | N/A | No runtime gating required for file structure. |
| **Author** | System Architect | |
| **Status** | **Approved** | Ready for execution. |
| **Last Updated** | 2026-01-26 | |

---

## 2. Executive Summary

### 2.1 The Requirement
The Lexichord project requires a strictly enforced file system hierarchy to support the **Modular Monolith** architecture. Without a predefined structure, developers may inadvertently introduce circular dependencies, mix test code with production code, or create a "Big Ball of Mud" where feature boundaries are ambiguous. We need a blank slate that physically separates the **Host** (Podium) from the **Modules** (Ensemble) and the **Interfaces** (Abstractions).

### 2.2 The Proposed Solution
We will initialize the git repository and the `.NET 9` solution (`.sln`) with a three-tier source directory structure:
1.  **`src/Core`**: Contains the Host application.
2.  **`src/Abstractions`**: Contains shared contracts.
3.  **`src/Modules`**: A dedicated container for future plugins.

We will also implement a root-level `Directory.Build.props` to enforce version consistency across all future projects automatically.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **External Tools:**
    *   .NET 9.0 SDK (`net9.0`)
    *   Git (Latest)
    *   Visual Studio 2022 / VS Code / Rider

### 3.2 Licensing Behavior
*   **N/A:** This specification concerns the compile-time environment. Licensing logic does not apply to directory creation.

---

## 4. Data Contract (The File System Layout)
*Instead of a C# Interface, the "Contract" for this task is the strict file system definition that all future sub-parts MUST adhere to.*

```text
/Lexichord (Root)
├── .github/
│   └── workflows/          # CI/CD pipelines
├── docs/                   # LCS Documentation (LCS-01, etc.)
├── src/
│   ├── Lexichord.Abstractions/
│   │   └── Lexichord.Abstractions.csproj
│   ├── Lexichord.Host/
│   │   └── Lexichord.Host.csproj
│   └── Lexichord.Modules/
│       └── .gitkeep        # Placeholder for future plugins
├── tests/
│   ├── Lexichord.Tests.Unit/
│   └── Lexichord.Tests.Integration/
├── .editorconfig           # Code style rules
├── .gitignore              # Standard Visual Studio ignore file
├── Directory.Build.props   # Centralized build configuration
├── LICENSE                 # MIT / Proprietary License text
├── README.md               # Project Entry Point
└── Lexichord.sln           # The Solution File
```

---

## 5. Implementation Logic

### 5.1 Directory.Build.props Configuration
To ensure every project in the monolith uses the same .NET version and C# features, we **SHALL** define the following properties at the root level.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <PropertyGroup>
    <Company>Lexichord</Company>
    <Authors>Lexichord Team</Authors>
    <Copyright>Copyright © 2026</Copyright>
  </PropertyGroup>
</Project>
```

### 5.2 CLI Execution Plan (Script)
The implementation **MUST** be performed via CLI to ensure reproducibility.

1.  **Initialize Git:**
    `git init`
    `dotnet new gitignore`
2.  **Create Solution:**
    `dotnet new sln -n Lexichord`
3.  **Create Projects:**
    `dotnet new classlib -o src/Lexichord.Abstractions -n Lexichord.Abstractions`
    `dotnet new avalonia.app -o src/Lexichord.Host -n Lexichord.Host` (If Avalonia templates installed)
    *Fallback if Avalonia templates missing:* `dotnet new console -o src/Lexichord.Host -n Lexichord.Host`
4.  **Add to Solution (With Virtual Folders):**
    `dotnet sln add src/Lexichord.Abstractions/Lexichord.Abstractions.csproj --solution-folder src`
    `dotnet sln add src/Lexichord.Host/Lexichord.Host.csproj --solution-folder src`

---

## 6. Data Persistence
*   **N/A:** No database is established in this step.

---

## 7. UI/UX Specifications
*   **N/A:** No runtime UI.
*   **Developer Experience (DX):**
    *   Opening `Lexichord.sln` in Visual Studio **MUST** display the projects nested cleanly under the `src` folder, not in the root list.
    *   Building the solution **MUST** trigger the analyzer rules defined in `Directory.Build.props`.

---

## 8. Observability & Logging
*   **N/A:** Runtime logging is not yet implemented.
*   **Build Logging:** The build output window should confirm that `Directory.Build.props` was imported (visible by inspecting project properties in IDE).

---

## 9. Security & Safety
*   **Git Ignore:** The `.gitignore` file **MUST** be present before the first commit to prevent `bin/`, `obj/`, and `.vs/` folders (which may contain local user secrets or cache) from entering source control.

---

## 10. Acceptance Criteria (QA)

These criteria feed directly into **LCS-02 (Rehearsal Strategy)**.

1.  **[Structure]** The root directory contains exactly: `src`, `tests`, `docs`, `.gitignore`, `Lexichord.sln`, `Directory.Build.props`.
2.  **[Compilation]** Running `dotnet build` from the root succeeds with 0 Errors and 0 Warnings.
3.  **[Configuration]** Inspecting `Lexichord.Host.csproj` shows **no** `<TargetFramework>` tag (or it inherits correctly), proving `Directory.Build.props` is working.
4.  **[Source Control]** `git status` shows no binary files (`.dll`, `.pdb`, `.exe`) pending commit.
5.  **[Module Placeholder]** The folder `src/Lexichord.Modules` exists and contains a `.gitkeep` file.