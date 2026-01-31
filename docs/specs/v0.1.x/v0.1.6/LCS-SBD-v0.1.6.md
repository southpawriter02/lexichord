# LCS-SBD-016: Scope Breakdown — Tuning Room (Settings & Preferences)

## Document Control

| Field            | Value                                                      |
| :--------------- | :--------------------------------------------------------- |
| **Document ID**  | LCS-SBD-016                                                |
| **Version**      | v0.1.6                                                     |
| **Status**       | Draft                                                      |
| **Last Updated** | 2026-01-26                                                 |
| **Depends On**   | v0.0.4 (IModule), v0.0.4c (ILicenseContext), v0.1.3d (Editor Configuration) |

---

## 1. Executive Summary

### 1.1 The Vision

The **Tuning Room** provides a centralized, extensible settings experience for Lexichord. It introduces a modal Settings dialog with tabbed navigation, allowing both the Host and individual modules to contribute settings pages. This architecture enables a cohesive user experience while maintaining modular separation of concerns.

### 1.2 Business Value

- **Unified Experience:** Single location for all application settings improves discoverability.
- **Module Extensibility:** Modules can inject custom settings tabs via `ISettingsPage` interface.
- **Live Preview:** Theme changes apply instantly without restart, reducing friction.
- **License Management:** Users can view and manage their subscription tier directly in settings.
- **Update Control:** Power users can opt into Insider builds for early access to features.
- **Professional Polish:** Settings dialogs are expected in production-quality desktop applications.

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| IModule | v0.0.4 | Modules implement `ISettingsPage` for tab injection |
| ILicenseContext | v0.0.4c | License validation and tier display |
| ISettingsService | v0.1.6a | Persist settings to JSON storage (NEW in this version) |
| IEditorConfigurationService | v0.1.3d | Editor module contributes Typography tab |
| IThemeManager | v0.0.2c | Theme switching with live preview |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.6a: Settings Dialog Framework

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-016a |
| **Title** | Settings Dialog Framework |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement a modal Settings window with left-hand category list and right-hand content area. Define `ISettingsPage` interface for module injection (e.g., Editor injects Typography tab).

**Key Deliverables:**
- Define `ISettingsPage` interface in Abstractions
- Implement `SettingsWindow` (modal dialog)
- Implement `SettingsViewModel` with category navigation
- Create `ISettingsPageRegistry` for page registration
- Wire `Ctrl+,` keyboard shortcut to open Settings
- Add "Settings" menu item under Edit or Tools menu

**Key Interfaces:**
```csharp
public interface ISettingsPage
{
    string CategoryId { get; }
    string DisplayName { get; }
    string? Icon { get; }
    int SortOrder { get; }
    Control CreateView();
}

public interface ISettingsPageRegistry
{
    void RegisterPage(ISettingsPage page);
    IReadOnlyList<ISettingsPage> GetPages();
    ISettingsPage? GetPage(string categoryId);
}
```

**Dependencies:**
- v0.0.3d: ISettingsService (persistence)
- v0.0.4: IModule (module discovery)

---

### 2.2 v0.1.6b: Live Theme Preview

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-016b |
| **Title** | Live Theme Preview |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement Appearance tab in Settings with Live Theme Preview. Theme changes (Light/Dark/System) apply instantly without application restart.

**Key Deliverables:**
- Create `AppearanceSettingsPage` implementing `ISettingsPage`
- Implement theme selection RadioButtons (Light, Dark, System)
- Wire theme selection to `IThemeManager.SetThemeAsync()`
- Store theme preference in settings with key `"Appearance.Theme"`
- Handle "System" option to follow OS theme
- Apply theme changes immediately on selection (no "Apply" button needed)

**Key Interfaces:**
```csharp
public interface IThemeManager
{
    ThemeMode CurrentTheme { get; }
    Task SetThemeAsync(ThemeMode theme);
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}
```

**Dependencies:**
- v0.1.6a: Settings Dialog Framework
- v0.0.8c: IThemeManager (existing)

---

### 2.3 v0.1.6c: License Management UI

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-016c |
| **Title** | License Management UI |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement Account tab in Settings with License Key text box, validation logic, and display of active Tier (Core/Pro/Teams). Validate against `ILicenseService`.

**Key Deliverables:**
- Create `AccountSettingsPage` implementing `ISettingsPage`
- Display current license tier prominently
- Display license expiration date (if applicable)
- Display licensee name
- Implement License Key input TextBox
- Add "Activate" button to validate and apply license
- Show validation status (Valid/Invalid/Expired)
- Display tier-specific feature availability list
- Add "Deactivate" option for license removal

**Key Interfaces:**
```csharp
public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateLicenseKeyAsync(string licenseKey, CancellationToken ct = default);
    Task<bool> ActivateLicenseAsync(string licenseKey, CancellationToken ct = default);
    Task<bool> DeactivateLicenseAsync(CancellationToken ct = default);
    LicenseInfo GetCurrentLicense();
}

public record LicenseValidationResult(
    bool IsValid,
    LicenseTier Tier,
    string? LicenseeName,
    DateTime? ExpirationDate,
    string? ErrorMessage
);

public record LicenseInfo(
    LicenseTier Tier,
    string? LicenseeName,
    DateTime? ExpirationDate,
    string? LicenseKey
);
```

**Dependencies:**
- v0.1.6a: Settings Dialog Framework
- v0.0.4c: ILicenseContext (license state)

---

### 2.4 v0.1.6d: Update Channel Selector

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-016d |
| **Title** | Update Channel Selector |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Allow users to switch between Stable and Insider update channels. Insider channel receives early-access builds with new features.

**Key Deliverables:**
- Add Update Channel section to Appearance or dedicated Updates tab
- Implement RadioButtons for Stable vs Insider selection
- Store selection in settings with key `"Updates.Channel"`
- Display channel descriptions (Stable: tested releases, Insider: early access)
- Show warning when switching to Insider channel
- Integrate with future update service (IUpdateService)
- Display current application version

**Key Interfaces:**
```csharp
public interface IUpdateService
{
    UpdateChannel CurrentChannel { get; }
    Task SetChannelAsync(UpdateChannel channel);
    Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default);
    string CurrentVersion { get; }
}

public enum UpdateChannel
{
    Stable,
    Insider
}

public record UpdateInfo(
    string Version,
    string ReleaseNotes,
    string DownloadUrl,
    DateTime ReleaseDate
);
```

**Dependencies:**
- v0.1.6a: Settings Dialog Framework
- Future: IUpdateService implementation (v0.2.x)

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.6a | Define ISettingsPage interface | 0.5 |
| 2 | v0.1.6a | Define ISettingsPageRegistry interface | 0.5 |
| 3 | v0.1.6a | Implement SettingsPageRegistry | 1 |
| 4 | v0.1.6a | Create SettingsWindow.axaml (modal) | 3 |
| 5 | v0.1.6a | Implement SettingsViewModel | 2 |
| 6 | v0.1.6a | Wire Ctrl+, keyboard shortcut | 0.5 |
| 7 | v0.1.6a | Add Settings menu item | 0.5 |
| 8 | v0.1.6a | Unit tests for SettingsPageRegistry | 2 |
| 9 | v0.1.6b | Create AppearanceSettingsPage | 2 |
| 10 | v0.1.6b | Implement AppearanceSettingsViewModel | 2 |
| 11 | v0.1.6b | Wire theme preview to IThemeManager | 1 |
| 12 | v0.1.6b | Handle System theme option | 1 |
| 13 | v0.1.6b | Unit tests for theme preview | 2 |
| 14 | v0.1.6c | Create AccountSettingsPage | 2 |
| 15 | v0.1.6c | Implement AccountSettingsViewModel | 3 |
| 16 | v0.1.6c | Define ILicenseService interface | 1 |
| 17 | v0.1.6c | Implement license validation logic | 3 |
| 18 | v0.1.6c | Create tier feature availability display | 2 |
| 19 | v0.1.6c | Unit tests for license management | 3 |
| 20 | v0.1.6d | Create UpdatesSettingsPage | 2 |
| 21 | v0.1.6d | Implement UpdatesSettingsViewModel | 2 |
| 22 | v0.1.6d | Define IUpdateService interface (stub) | 1 |
| 23 | v0.1.6d | Implement channel switching UI | 1 |
| 24 | v0.1.6d | Display version information | 0.5 |
| 25 | v0.1.6d | Unit tests for update channel | 2 |
| **Total** | | | **40 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| Theme switch causes visual glitches | Medium | Medium | Test all views on theme change; use dynamic resources |
| License validation requires network | Medium | High | Implement offline grace period; cache validation |
| Module settings pages conflict | Low | Low | Use unique CategoryId; validate on registration |
| Insider builds are unstable | Medium | High | Clear warnings; easy rollback mechanism |
| Settings window blocks main UI | Low | Low | Use proper modal dialog pattern |
| ISettingsPage order conflicts | Low | Medium | Use SortOrder property; document conventions |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Settings window open time | < 100ms | Time from Ctrl+, to window visible |
| Theme switch response | < 50ms | Time from selection to theme applied |
| License validation response | < 2s | Time from click to validation result |
| Page navigation response | < 16ms | Time from category click to page visible |
| Memory overhead | < 5MB | Additional memory when settings open |

---

## 6. What This Enables

After v0.1.6, Lexichord will support:
- Centralized settings management with extensible architecture
- Module-contributed settings pages (Editor Typography already integrated)
- Live theme switching without restart
- User-managed license activation and tier visibility
- Update channel control for early access to features
- Foundation for v0.2.x additional settings pages
- Foundation for v0.2.x auto-update functionality
