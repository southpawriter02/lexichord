# LCS-CL-001d: Changelog — Continuous Integration Pipeline

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-001d                                            |
| **Version**      | v0.0.1d                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-001d](../specs/v0.0.x/v0.0.1/LCS-DES-001d.md) |

---

## Summary

Established the GitHub Actions CI/CD pipeline for automated build and test execution on every push and pull request.

---

## Changes

### Files Created

| File                       | Purpose                                  |
| :------------------------- | :--------------------------------------- |
| `.github/workflows/ci.yml` | GitHub Actions CI workflow configuration |

---

## Workflow Configuration

### Triggers

| Event          | Target        | Action     |
| :------------- | :------------ | :--------- |
| `push`         | `main` branch | Trigger CI |
| `pull_request` | `main` branch | Trigger CI |

### Concurrency

- **Group**: Per-workflow, per-PR or per-ref
- **Cancel In-Progress**: `true` — Rapid pushes cancel in-progress runs

### Environment Variables

| Variable                      | Value   | Purpose                      |
| :---------------------------- | :------ | :--------------------------- |
| `DOTNET_VERSION`              | `9.0.x` | Consistent .NET SDK version  |
| `DOTNET_NOLOGO`               | `true`  | Suppress .NET welcome banner |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | `true`  | Disable Microsoft telemetry  |

---

## Pipeline Steps

```text
┌─────────────────────────────────────────────────────────────────┐
│                    GitHub Actions Workflow                       │
├─────────────────────────────────────────────────────────────────┤
│  Trigger: push to main, pull_request to main                    │
│  Runner: ubuntu-latest                                          │
├─────────────────────────────────────────────────────────────────┤
│  Step 1: Checkout Code (actions/checkout@v4)                     │
│  Step 2: Setup .NET 9 (actions/setup-dotnet@v4)                  │
│  Step 3: Cache NuGet Packages (actions/cache@v4)                 │
│  Step 4: Restore Dependencies (dotnet restore)                   │
│  Step 5: Build Solution (dotnet build --configuration Release)  │
│  Step 6: Run Unit Tests (--filter Category!=Integration)        │
│  Step 7: Upload Test Results (actions/upload-artifact@v4)       │
└─────────────────────────────────────────────────────────────────┘
```

### Step Details

| Step | Command                                                                         | Purpose                                  |
| :--- | :------------------------------------------------------------------------------ | :--------------------------------------- |
| 1    | `actions/checkout@v4`                                                           | Clone repository with full history       |
| 2    | `actions/setup-dotnet@v4`                                                       | Install .NET 9.0 SDK                     |
| 3    | `actions/cache@v4`                                                              | Cache NuGet packages for faster restores |
| 4    | `dotnet restore`                                                                | Restore NuGet dependencies               |
| 5    | `dotnet build --no-restore --configuration Release`                             | Compile solution                         |
| 6    | `dotnet test --no-build --configuration Release --filter Category!=Integration` | Run unit tests                           |
| 7    | `actions/upload-artifact@v4`                                                    | Store test results for 7 days            |

---

## Third-Party Actions Used

| Action                    | Version | Source          | Purpose               |
| :------------------------ | :------ | :-------------- | :-------------------- |
| `actions/checkout`        | v4      | GitHub Official | Clone repository      |
| `actions/setup-dotnet`    | v4      | GitHub Official | Install .NET SDK      |
| `actions/cache`           | v4      | GitHub Official | NuGet package caching |
| `actions/upload-artifact` | v4      | GitHub Official | Store test results    |

---

## Caching Strategy

```yaml
key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

**Cache Key Composition:**

- `runner.os`: Ensures OS-specific caches
- `hashFiles('**/*.csproj')`: Invalidates when project files change

**Expected Performance:**
| Scenario | Restore Time |
|:---------|:-------------|
| Cache Miss | ~30 seconds |
| Cache Hit | ~5 seconds |

---

## Test Execution

```bash
dotnet test --no-build --configuration Release --filter Category!=Integration --verbosity normal
```

**Rationale**: Integration tests are filtered out because they require Docker, which is not available on standard GitHub-hosted runners. Integration tests should be run locally before pushing.

---

## Acceptance Criteria Verification

| Criterion                                                  | Status                     |
| :--------------------------------------------------------- | :------------------------- |
| `.github/workflows/ci.yml` exists and is valid YAML        | ✅ Pass                    |
| Workflow triggers on push to `main`                        | ✅ Configured              |
| Workflow triggers on pull request to `main`                | ✅ Configured              |
| Build step executes `dotnet build --configuration Release` | ✅ Configured              |
| Test step executes with `--filter Category!=Integration`   | ✅ Configured              |
| Concurrency cancellation configured for rapid pushes       | ✅ Configured              |
| Green checkmark on successful push                         | ⏳ Requires push to verify |
| Red X on failed build                                      | ⏳ Requires push to verify |

---

## Verification Commands

```bash
# 1. Validate YAML syntax (requires pyyaml)
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))"

# 2. Push to main and observe Actions tab
git push origin main
# Navigate to: https://github.com/<owner>/lexichord/actions

# 3. View workflow runs (requires GitHub CLI)
gh run list --workflow=ci.yml
gh run view --web
```

---

## Notes

- Integration tests (`[Trait("Category", "Integration")]`) are skipped in CI due to Docker requirements.
- Test results are stored as artifacts for 7 days for debugging.
- No secrets are required for this workflow.
- Future enhancements may include code coverage uploads (`CODECOV_TOKEN`) and branch protection rules.
