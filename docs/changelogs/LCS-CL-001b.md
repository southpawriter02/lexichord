# LCS-CL-001b: Changelog — Dependency Graph Enforcement

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-001b                                            |
| **Version**      | v0.0.1b                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-001b](../specs/v0.0.x/v0.0.1/LCS-DES-001b.md) |

---

## Summary

Configured the project files (`.csproj`) to enforce the Modular Monolith "Onion Architecture" dependency rules at compile-time.

---

## Changes

### Files Modified

| File                                                       | Change                                               |
| :--------------------------------------------------------- | :--------------------------------------------------- |
| `src/Lexichord.Abstractions/Lexichord.Abstractions.csproj` | Added constraint documentation comment               |
| `src/Lexichord.Host/Lexichord.Host.csproj`                 | Added constraint documentation comments to ItemGroup |

### Lexichord.Abstractions.csproj (Final)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!--
    CONSTRAINT: This project MUST NOT reference any other internal project.
    It defines shared interfaces, enums, and attributes only.
    All external dependencies SHOULD be minimal (standard system libraries).
  -->
</Project>
```

### Lexichord.Host.csproj (Modified ItemGroup)

```xml
<ItemGroup>
  <!-- ALLOWED: Reference to Abstractions (Tier 0) -->
  <ProjectReference Include="..\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />

  <!-- DO NOT ADD: References to any project inside src/Lexichord.Modules/ -->
  <!-- Modules are loaded via Reflection at runtime, never compile-time. -->
</ItemGroup>
```

---

## Dependency Hierarchy Established

```text
┌──────────────────────────────────────────────────────────────┐
│                    Lexichord.Host (Tier 1)                   │
│                         The Podium                           │
│                            │                                 │
│                            ▼                                 │
│             Lexichord.Abstractions (Tier 0)                  │
│                    Interfaces & Contracts                    │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │               Lexichord.Modules.* (Tier 2)              │ │
│  │   Future plugins load via Reflection only.              │ │
│  │   Host MUST NOT reference Modules at compile-time.      │ │
│  └─────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

### Dependency Rules

| Project                  | MAY Reference               | MUST NOT Reference                      |
| :----------------------- | :-------------------------- | :-------------------------------------- |
| `Lexichord.Abstractions` | Nothing (Tier 0—The Bottom) | Any internal project                    |
| `Lexichord.Host`         | `Lexichord.Abstractions`    | Any project in `src/Lexichord.Modules/` |
| `Lexichord.Modules.*`    | `Host` and `Abstractions`   | Other Modules (use Event Bus)           |

---

## Verification Commands Executed

```bash
# 1. Verify build succeeds
dotnet build --configuration Release
# ✅ PASS: Build succeeded

# 2. Verify Abstractions has no project references
grep -c "ProjectReference" src/Lexichord.Abstractions/Lexichord.Abstractions.csproj
# ✅ PASS: 0 ProjectReference elements found

# 3. Verify Host references only Abstractions
grep "ProjectReference" src/Lexichord.Host/Lexichord.Host.csproj
# ✅ PASS: Single reference to Lexichord.Abstractions

# 4. Verify no Module references exist anywhere
grep -r "Lexichord.Modules.*\.csproj" src/**/*.csproj
# ✅ PASS: No Module references found
```

---

## Acceptance Criteria Verification

| Criterion                                                                         | Status  |
| :-------------------------------------------------------------------------------- | :------ |
| Dependency graph shows `Host → Abstractions` with no other internal references    | ✅ Pass |
| `dotnet build` succeeds with 0 Errors and 0 Warnings                              | ✅ Pass |
| `Lexichord.Abstractions.csproj` contains zero `<ProjectReference>` elements       | ✅ Pass |
| `Lexichord.Host.csproj` contains exactly one `<ProjectReference>` to Abstractions | ✅ Pass |
| No `.csproj` file references any project inside `src/Lexichord.Modules/`          | ✅ Pass |

---

## Notes

- Constraint comments serve as documentation for developers and code reviewers.
- The architecture prevents compile-time circular dependencies.
- Future modules (v0.0.4+) will be loaded dynamically via reflection.
