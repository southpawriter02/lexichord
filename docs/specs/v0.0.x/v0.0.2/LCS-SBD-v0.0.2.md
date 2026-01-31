# LCS-SBD: Scope Breakdown - v0.0.2

**Target Version:** `v0.0.2`
**Codename:** The Host Shell (UI Framework)
**Timeline:** Sprint 1 (Host Foundation)
**Owner:** Lead Architect
**Prerequisites:** v0.0.1d complete (Solution scaffolding, CI/CD).

## 1. Executive Summary

**v0.0.2** establishes the visual foundation of Lexichord. This release transforms the project from a headless Console application into a functional cross-platform desktop application using **AvaloniaUI**. The success of this release is measured by:

1. A blank Avalonia window runs on Windows, Mac, and Linux.
2. The MainWindow displays the "Podium Layout" (navigation rail, content region, status bar).
3. Theme switching (Dark/Light) works at runtime.
4. Window state persists between sessions.

If this foundation is flawed, the module UI system (v0.0.4) will fail to register views.

---

## 2. Sub-Part Specifications

### v0.0.2a: Avalonia Bootstrap

**Goal:** Initialize `Lexichord.Host` with proper Avalonia application lifecycle.

- **Task 1.1: Project Configuration**
    - Update `Lexichord.Host.csproj` to use Avalonia SDK.
    - Add Avalonia NuGet packages: `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`.
    - Remove `<OutputType>Exe</OutputType>` and configure for Avalonia desktop.
- **Task 1.2: Entry Point**
    - Replace the current `Program.cs` with Avalonia bootstrap pattern.
    - Configure `ClassicDesktopStyleApplicationLifetime`.
    - Set `ShutdownMode.OnMainWindowClose`.
- **Task 1.3: Application Class**
    - Create `App.axaml.cs` code-behind to complete the Application class.
    - Implement `OnFrameworkInitializationCompleted` to create MainWindow.
- **Task 1.4: MainWindow Stub**
    - Create `MainWindow.axaml` and `MainWindow.axaml.cs`.
    - Set initial window title: "Lexichord — The Orchestrator".
    - Set minimum dimensions: 1024x768.

**Definition of Done:**

- `dotnet run` launches a visible Avalonia window.
- Window displays on Windows, Mac, and Linux runners.
- Pressing the close button terminates the application.

---

### v0.0.2b: The Podium Layout

**Goal:** Design the `MainWindow` shell using a structured Grid layout.

- **Task 1.1: Shell Grid**
    - Define a Grid with named regions:
        - Row 0: **Top Bar** (Title Bar / Menu) — Height: Auto
        - Row 1: **Content Area** (Columns) — Height: \*
        - Row 2: **Status Bar** — Height: Auto
    - Define Content Area columns:
        - Column 0: **Navigation Rail** (Left) — Width: 60
        - Column 1: **Content Region** (Center) — Width: \*
- **Task 1.2: Region Placeholders**
    - **TopBar:** Create `TopBar.axaml` UserControl with application title.
    - **NavigationRail:** Create `NavigationRail.axaml` UserControl with placeholder icons.
    - **ContentHost:** Create `ContentHostPanel.axaml` UserControl with "Welcome to Lexichord" text.
    - **StatusBar:** Create `StatusBar.axaml` UserControl with version number display.
- **Task 1.3: View Injection Points**
    - Use `ContentControl` in ContentHost for future module view injection.
    - Define `x:Name="ContentHost"` for programmatic access.

**Definition of Done:**

- MainWindow displays all four regions visually.
- Regions are styled according to the Lexichord theme.
- Navigation rail shows placeholder icon buttons.

---

### v0.0.2c: Theme Infrastructure

**Goal:** Implement runtime theme switching between Dark and Light modes.

- **Task 1.1: ThemeManager Service**
    - Create `IThemeManager` interface in `Lexichord.Abstractions`.
    - Create `ThemeManager` implementation in `Lexichord.Host`.
    - Methods: `SetTheme(ThemeMode)`, `GetCurrentTheme()`, `ToggleTheme()`.
- **Task 1.2: Resource Dictionary Swapping**
    - Implement logic to swap `Colors.Dark.axaml` and `Colors.Light.axaml` at runtime.
    - Use `Application.Current.Resources.MergedDictionaries` manipulation.
- **Task 1.3: System Theme Detection**
    - Detect OS theme preference on startup.
    - Subscribe to system theme change events (where supported).
- **Task 1.4: Theme Toggle Control**
    - Add a theme toggle button to the StatusBar or TopBar.
    - Bind button icon to current theme state.

**Definition of Done:**

- Clicking the theme toggle switches between Dark and Light modes instantly.
- All controls rerender with correct colors on theme change.
- Application respects OS theme preference on first launch.

---

### v0.0.2d: Window State Persistence

**Goal:** Remember window position, size, and maximize state between sessions.

- **Task 1.1: State Model**
    - Create `WindowState` record: `{ X, Y, Width, Height, IsMaximized, MonitorId }`.
    - Location: `Lexichord.Host/Models/WindowState.cs`.
- **Task 1.2: State Service**
    - Create `IWindowStateService` interface.
    - Create `WindowStateService` implementation with JSON file persistence.
    - File location: `{AppData}/Lexichord/appstate.json`.
- **Task 1.3: Save on Close**
    - Hook into `MainWindow.Closing` event.
    - Capture current `Position`, `Bounds`, and `WindowState`.
    - Serialize and write to file.
- **Task 1.4: Restore on Open**
    - Check for `appstate.json` on startup.
    - If exists, apply saved position and dimensions.
    - Handle multi-monitor edge cases (saved position off-screen).

**Definition of Done:**

- Resizing the window and closing remembers the new size.
- Reopening the app restores the exact position and dimensions.
- If saved position is off-screen, window resets to center of primary monitor.

---

## 3. Implementation Checklist (for Developer)

| Step     | Description                                                                     | Status |
| :------- | :------------------------------------------------------------------------------ | :----- |
| **0.2a** | `Lexichord.Host.csproj` updated with Avalonia packages.                         | [ ]    |
| **0.2a** | `Program.cs` uses `BuildAvaloniaApp().StartWithClassicDesktopLifetime()`.       | [ ]    |
| **0.2a** | `MainWindow.axaml` created and displays on launch.                              | [ ]    |
| **0.2b** | MainWindow Grid layout implemented with 4 regions.                              | [ ]    |
| **0.2b** | `TopBar`, `NavigationRail`, `ContentHostPanel`, `StatusBar` UserControls exist. | [ ]    |
| **0.2c** | `IThemeManager` interface defined in Abstractions.                              | [ ]    |
| **0.2c** | Theme toggle button switches Dark/Light modes.                                  | [ ]    |
| **0.2d** | `appstate.json` persists window position between sessions.                      | [ ]    |
| **0.2d** | Off-screen restore logic validated.                                             | [ ]    |

## 4. Risks & Mitigations

- **Risk:** Avalonia templates not installed on dev machine.
    - _Mitigation:_ Install via `dotnet new install Avalonia.Templates` before starting.
- **Risk:** Different Avalonia versions between Host and Modules.
    - _Mitigation:_ Pin Avalonia version in `Directory.Build.props`.
- **Risk:** Window state file corruption.
    - _Mitigation:_ Use `try/catch` on JSON deserialization; fallback to default window size.
- **Risk:** Multi-monitor position becomes invalid after monitor disconnected.
    - _Mitigation:_ Validate saved position against current screen geometry on restore.
