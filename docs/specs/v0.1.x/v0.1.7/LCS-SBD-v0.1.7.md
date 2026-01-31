# LCS-SBD-017: Scope Breakdown — Distribution (Packaging)

## Document Control

| Field            | Value                                                      |
| :--------------- | :--------------------------------------------------------- |
| **Document ID**  | LCS-SBD-017                                                |
| **Version**      | v0.1.7                                                     |
| **Status**       | Draft                                                      |
| **Last Updated** | 2026-01-26                                                 |
| **Depends On**   | v0.1.6 (Settings), v0.0.3 (Serilog), v0.1.3 (Editor Module) |

---

## 1. Executive Summary

### 1.1 The Vision

The **Distribution** module transforms Lexichord from a developer-only project into a **production-ready application** that can be installed on end-user machines. By implementing Velopack integration, code signing, first-run detection, and optional telemetry, users gain a professional installation experience while developers gain crash visibility—the final step before public release.

### 1.2 Business Value

- **User Trust:** Code-signed installers avoid SmartScreen warnings on Windows and Gatekeeper warnings on macOS.
- **Professional Experience:** Polished Setup.exe and .dmg installers match industry expectations.
- **Update Awareness:** Release notes automatically shown after updates keep users informed.
- **Crash Visibility:** Optional Sentry integration enables proactive bug identification.
- **User Control:** Opt-out telemetry toggle respects user privacy while enabling product improvement.

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| ISettingsService | v0.1.6 | Persist telemetry opt-out preference |
| SettingsViewModel | v0.1.6 | Add telemetry toggle to Settings view |
| Serilog Pipeline | v0.0.3b | Integrate Sentry as sink for crash reporting |
| Editor Module | v0.1.3 | Display CHANGELOG.md in read-only tab |
| IEditorService | v0.1.3a | Open changelog file programmatically |
| IConfigurationService | v0.0.3d | Retrieve application data paths |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.7a: Velopack Integration

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-017a |
| **Title** | Velopack Integration |
| **Module** | `Build Infrastructure` / `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Install and configure Velopack to generate Setup.exe for Windows and .dmg for macOS from CI builds.

**Key Deliverables:**
- Install Velopack NuGet package and CLI tools
- Configure VelopackApp bootstrap in Program.cs
- Create build scripts for Windows (Setup.exe) and macOS (.dmg)
- Implement delta update support for subsequent versions
- Configure auto-update check on application startup

**Key Interfaces:**
```csharp
public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default);
    Task ApplyUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default);
    bool IsUpdatePending { get; }
    string? PendingVersion { get; }
}
```

**Dependencies:**
- .NET 9.0 SDK
- Velopack CLI (vpk)
- GitHub Releases (or alternative release host)

---

### 2.2 v0.1.7b: Signing Infrastructure

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-017b |
| **Title** | Signing Infrastructure |
| **Module** | `Build Infrastructure` / `CI Pipeline` |
| **License Tier** | Core |

**Goal:** Update CI pipeline to code sign Windows executables with PFX certificates and macOS binaries with Developer ID certificates, avoiding SmartScreen/Gatekeeper warnings.

**Key Deliverables:**
- Store PFX certificate as base64-encoded GitHub secret
- Configure Windows code signing in CI workflow
- Configure macOS Developer ID signing and notarization
- Implement timestamp server for long-term validity
- Verify signature integrity post-build

**Key Interfaces:**
```yaml
# GitHub Actions secrets required
WINDOWS_CERTIFICATE_BASE64: # PFX as base64
WINDOWS_CERTIFICATE_PASSWORD: # PFX password
APPLE_DEVELOPER_ID: # Apple Developer ID
APPLE_TEAM_ID: # Apple Team ID
APPLE_APP_SPECIFIC_PASSWORD: # For notarization
```

**Signing Flow:**
1. CI builds release artifacts
2. Windows: SignTool.exe signs .exe with PFX from secrets
3. macOS: codesign signs .app bundle with Developer ID
4. macOS: xcrun notarytool notarizes and staples
5. Velopack packages signed artifacts into installers

**Dependencies:**
- v0.1.7a: Velopack Integration (generates artifacts to sign)
- GitHub Actions (CI runner)
- Windows SDK (SignTool.exe)
- Xcode Command Line Tools (codesign, notarytool)

---

### 2.3 v0.1.7c: Release Notes Viewer

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-017c |
| **Title** | Release Notes Viewer |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement FirstRun flag detection after update to automatically open CHANGELOG.md in a new tab, informing users of what changed.

**Key Deliverables:**
- Define IFirstRunService interface for version tracking
- Store last run version in user settings
- Detect version change on startup (update scenario)
- Open CHANGELOG.md in read-only editor tab on first run after update
- Create "What's New" toast notification as alternative UI

**Key Interfaces:**
```csharp
public interface IFirstRunService
{
    bool IsFirstRunAfterUpdate { get; }
    bool IsFirstRunEver { get; }
    string? PreviousVersion { get; }
    string CurrentVersion { get; }
    Task MarkRunCompletedAsync();
}
```

**Detection Logic:**
1. On startup, read last run version from settings
2. Compare to current assembly version
3. If different → IsFirstRunAfterUpdate = true
4. If null → IsFirstRunEver = true
5. Display release notes or welcome screen accordingly
6. Update stored version after display

**Dependencies:**
- v0.1.6: Settings (persist last run version)
- v0.1.3: Editor Module (display CHANGELOG.md)
- CHANGELOG.md embedded as resource or bundled with app

---

### 2.4 v0.1.7d: Telemetry Hooks

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-017d |
| **Title** | Telemetry Hooks |
| **Module** | `Lexichord.Host`, `Lexichord.Abstractions` |
| **License Tier** | Core |

**Goal:** Implement optional Sentry crash reporting with explicit user opt-out toggle in Settings, respecting user privacy while enabling crash visibility.

**Key Deliverables:**
- Define ITelemetryService interface for crash/event reporting
- Integrate Sentry SDK with Serilog sink
- Add "Send crash reports" toggle to Settings view
- Initialize Sentry conditionally based on user preference
- Scrub PII from crash reports before submission
- Implement breadcrumb logging for context

**Key Interfaces:**
```csharp
public interface ITelemetryService
{
    bool IsEnabled { get; }
    void Enable();
    void Disable();
    void CaptureException(Exception exception, IDictionary<string, string>? tags = null);
    void CaptureMessage(string message, TelemetryLevel level = TelemetryLevel.Info);
    void AddBreadcrumb(string message, string? category = null);
    IDisposable BeginScope(string operation);
}

public enum TelemetryLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
```

**Privacy Requirements:**
- Disabled by default (opt-in on first launch)
- Clear description of what is collected
- No PII (email, file paths, document content)
- Toggle takes effect immediately (no restart)
- Respect GDPR/privacy regulations

**Dependencies:**
- v0.1.6: Settings (persist telemetry preference)
- v0.0.3b: Serilog (integrate as sink)
- Sentry.io account and DSN

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.7a | Install Velopack NuGet package | 0.5 |
| 2 | v0.1.7a | Configure VelopackApp bootstrap in Program.cs | 1 |
| 3 | v0.1.7a | Create Windows build script (vpk pack) | 2 |
| 4 | v0.1.7a | Create macOS build script (.dmg generation) | 2 |
| 5 | v0.1.7a | Define IUpdateService interface | 1 |
| 6 | v0.1.7a | Implement UpdateService with auto-check | 3 |
| 7 | v0.1.7a | Create update available notification UI | 2 |
| 8 | v0.1.7a | Unit tests for update service | 2 |
| 9 | v0.1.7b | Store PFX certificate in GitHub secrets | 0.5 |
| 10 | v0.1.7b | Configure Windows signing in CI workflow | 2 |
| 11 | v0.1.7b | Configure macOS signing workflow | 3 |
| 12 | v0.1.7b | Implement notarization step | 2 |
| 13 | v0.1.7b | Add signature verification step | 1 |
| 14 | v0.1.7b | Document certificate renewal process | 1 |
| 15 | v0.1.7c | Define IFirstRunService interface | 0.5 |
| 16 | v0.1.7c | Implement FirstRunService | 2 |
| 17 | v0.1.7c | Add version tracking to settings | 1 |
| 18 | v0.1.7c | Bundle CHANGELOG.md with application | 0.5 |
| 19 | v0.1.7c | Open changelog on first run after update | 2 |
| 20 | v0.1.7c | Unit tests for first run detection | 1.5 |
| 21 | v0.1.7d | Define ITelemetryService interface | 0.5 |
| 22 | v0.1.7d | Install Sentry SDK | 0.5 |
| 23 | v0.1.7d | Implement TelemetryService | 3 |
| 24 | v0.1.7d | Configure Serilog Sentry sink | 1 |
| 25 | v0.1.7d | Add telemetry toggle to Settings view | 1.5 |
| 26 | v0.1.7d | Implement PII scrubbing | 2 |
| 27 | v0.1.7d | Unit tests for telemetry service | 2 |
| 28 | v0.1.7d | Document privacy policy | 1 |
| **Total** | | | **40 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| Certificate expires during release | High | Low | Document renewal; set calendar reminders |
| Notarization fails silently | High | Medium | Add explicit verification step in CI |
| Sentry costs exceed budget | Medium | Low | Monitor usage; implement rate limiting |
| Users disable telemetry entirely | Low | High | Expected; ensure core logging works offline |
| Delta updates corrupt installation | High | Low | Full installer fallback; verify after apply |
| CHANGELOG.md missing from build | Medium | Low | Fail CI if resource not embedded |
| SmartScreen warning despite signing | High | Medium | Build reputation over time; timestamp signatures |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Windows SmartScreen bypass | 100% | No warnings on signed release |
| macOS Gatekeeper bypass | 100% | No warnings on notarized release |
| First run detection accuracy | 100% | Correctly identifies update vs fresh install |
| Telemetry opt-in rate | > 30% | Users who enable crash reporting |
| Crash report submission | < 5s | Time to submit report to Sentry |
| Update check latency | < 2s | Time to check for available updates |
| Installer size (Windows) | < 80 MB | Compressed Setup.exe size |
| Installer size (macOS) | < 100 MB | .dmg file size |

---

## 6. What This Enables

After v0.1.7, Lexichord will support:
- Professional installation experience for end users
- Signed binaries trusted by OS security systems
- Automatic update notifications and application
- User awareness of new features via release notes
- Optional crash reporting for production debugging
- Foundation for v0.2.x public beta release
- Foundation for telemetry-driven product decisions

---

## 7. File Structure

```
src/
├── Lexichord.Host/
│   ├── Program.cs                    # VelopackApp bootstrap
│   ├── Services/
│   │   ├── UpdateService.cs          # IUpdateService implementation
│   │   ├── FirstRunService.cs        # IFirstRunService implementation
│   │   └── TelemetryService.cs       # ITelemetryService implementation
│   ├── Views/
│   │   └── UpdateAvailableDialog.axaml
│   └── Resources/
│       └── CHANGELOG.md              # Embedded resource
├── Lexichord.Abstractions/
│   └── Contracts/
│       ├── IUpdateService.cs
│       ├── IFirstRunService.cs
│       └── ITelemetryService.cs
build/
├── scripts/
│   ├── pack-windows.ps1              # Windows Velopack script
│   ├── pack-macos.sh                 # macOS Velopack script
│   └── sign-windows.ps1              # Windows signing script
.github/
└── workflows/
    └── release.yml                   # CI/CD with signing
```
