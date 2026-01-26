# Lexichord Workspace Roadmap (v0.1.1 - v0.1.7)

At this stage, the "Skeleton" (v0.0.x) exists. Now we build the muscles and skin. This version series focuses on creating a functional, persistent, and usable IDE-like environment. By the end of v0.1.x, a user should be able to write Markdown, manage files, and configure the UI, even without any AI features.

**Total Sub-Parts:** 36 distinct implementation steps.

## v0.1.1: The Layout Engine (Docking System)
**Goal:** Replace the static grid from v0.0.2 with a dynamic, resizable docking interface (similar to Visual Studio or VS Code).
*   **v0.1.1a:** **Dock Library Integration.** Integrate `Dock.Avalonia` or `Avalonia.Dock` into `Lexichord.Host`. Define the `IDockFactory` to create the default layout: A centralized "Document Region" flanked by collapsible "Tool Regions" (Left/Right/Bottom).
*   **v0.1.1b:** **Region Injection Service.** Implement `IRegionManager`. This allows future Modules (like RAG or Agents) to say, *"Inject my View into the Right Sidebar"* without knowing about the Docking library directly.
*   **v0.1.1c:** **Layout Serialization.** Implement `LayoutService`. On application exit, serialize the exact JSON state of the dock (width of panels, open tabs). On startup, deserialize and restore it.
*   **v0.1.1d:** **Tab Infrastructure.** Create the `DocumentViewModel` base class. Implement logic for Tab Drag-and-Drop, tearing tabs out into floating windows, and pinning tabs.

## v0.1.2: The Explorer (Project Management)
**Goal:** Ability to visualize and manipulate the file system.
*   **v0.1.2a:** **Workspace Service.** Create `Lexichord.Modules.Workspace`. Implement `IWorkspaceService` which holds the state of the "Current Open Folder."
*   **v0.1.2b:** **File System Watcher.** Implement a robust `FileSystemWatcher` wrapper that listens for external changes (files added/deleted by Git or other apps) and updates the internal model in real-time.
*   **v0.1.2c:** **The Tree View UI.** Build the `ProjectExplorerView` user control. Implement recursive rendering of folders and files. Add file icons (using a library like `Material.Icons.Avalonia`) based on extension.
*   **v0.1.2d:** **Context Menu Actions.** Implement "Right-Click" logic: *New File, New Folder, Rename (F2), Delete, Reveal in Explorer*. Publish `FileCreatedEvent` and `FileDeletedEvent` via the Event Bus.

## v0.1.3: The Manuscript (The Editor Module)
**Goal:** A high-performance text editor. This is the core "product" for the Free tier.
*   **v0.1.3a:** **AvalonEdit Integration.** specific `Lexichord.Modules.Editor` module. Embed the `TextEditor` control. Configure standard behaviors: Line numbers, Word wrap, Scroll syncing.
*   **v0.1.3b:** **Syntax Highlighting Service.** Implement a loader that reads `.xshd` definition files. Bundle definitions for Markdown, JSON, YAML, and XML. Ensure the coloring respects the active App Theme.
*   **v0.1.3c:** **Search & Replace Overlay.** Build a custom UI overlay (Ctrl+F) that sits *on top* of the text editor (not a popup window). Implement `Next`, `Previous`, and `Replace All` logic using AvalonEdit's search APIs.
*   **v0.1.3d:** **Editor Configuration.** detailed settings: Font Family (Cascadia Code default), Font Size (Ctrl+Scroll zoom), and Tab/Space settings. Persist these via `ISettingsService`.

## v0.1.4: The Scribe (IO & Persistence)
**Goal:** Robust handling of file states.
*   **v0.1.4a:** **Dirty State Tracking.** Implement the "Star" logic (`filename.md*`). Bind the Editor's `TextChanged` event to the `DocumentViewModel.IsDirty` property.
*   **v0.1.4b:** **Atomic Saves.** Implement `FileService.SaveAsync()`. Use a "Write to Temp -> Delete Original -> Rename Temp" strategy to prevent data corruption during crashes.
*   **v0.1.4c:** **Safe Close Workflow.** Implement the `IShutdownService`. Intercept the "X" button click. If dirty files exist, show a "Save Changes?" dialog list allowing bulk save or discard.
*   **v0.1.4d:** **Recent Files History.** maintain a `MRU` (Most Recently Used) list in the database. Populate the "File > Open Recent" menu on startup.

## v0.1.5: The Conductor's Baton (Command Palette)
**Goal:** Keyboard-centric navigation (The "Ctrl+P" or "Cmd+K" experience).
*   **v0.1.5a:** **Command Registry.** Define `ICommandRegistry` in Abstractions. Allow Modules to register "Actions" with metadata: `Title`, `Category`, `Icon`, `Shortcut`.
    *   *Example:* Editor Module registers "Toggle Word Wrap".
*   **v0.1.5b:** **The Palette UI.** Create a modal overlay centered in the window. Implement a fuzzy search algorithm (using `FuzzySharp` or similar) to filter commands as the user types.
*   **v0.1.5c:** **File Jumper.** Extend the palette to index the files in the `WorkspaceService`. Typing a filename should allow instant navigation/opening of that document.
*   **v0.1.5d:** **Keybinding Service.** Create a centralized `KeyBindingManager`. Allow users to remap keys (e.g., Change "Save" from Ctrl+S to Ctrl+Alt+S) via a `keybindings.json` config file.

## v0.1.6: The Tuning Room (Settings & Preferences)
**Goal:** A centralized configuration screen.
*   **v0.1.6a:** **Settings Dialog Framework.** Create a modal window with a left-hand category list. Implement `ISettingsPage` interface so Modules can inject their own settings tabs.
    *   *Example:* The Editor Module injects a "Typography" tab.
*   **v0.1.6b:** **Live Theme Preview.** Add a "Appearance" tab. changing the Theme (Light/Dark/System) should apply instantly without restart.
*   **v0.1.6c:** **License Management UI.** Create the "Account" tab. Add a text box for "License Key". Implement the logic to validate the key against the `LicenseService` and display the active Tier (Core/Pro/Teams).
*   **v0.1.6d:** **Update Channel Selector.** Add logic to switch between `Stable` and `Insider` update channels.

## v0.1.7: The Distribution (Packaging)
**Goal:** Get the app off the developer machine and onto a user's machine.
*   **v0.1.7a:** **Velopack Integration.** Install `Velopack`. Configure the build process to generate `Setup.exe` (Windows) and `.dmg` (Mac).
*   **v0.1.7b:** **Signing Infrastructure.** Update the CI pipeline to accept code signing certificates (PFX) from secrets. *Unsigned apps will trigger SmartScreen warnings.*
*   **v0.1.7c:** **Release Notes Viewer.** Implement logic that checks if the app was just updated (`FirstRun` flag). If yes, open a new Tab displaying the `CHANGELOG.md`.
*   **v0.1.7d:** **Telemetry Hooks.** (Optional/Opt-in). Hook up `Sentry` or similar for crash reporting in production builds. Ensure an "Opt-out" toggle exists in the Settings.

---

### Implementation Guide: Sample Workflow for v0.1.5a (Command Registry)

**LCS-01 (Design Composition)**
*   **Interface:**
    ```csharp
    public record CommandDefinition(string Id, string Title, string Category, KeyGesture DefaultShortcut, Action<object> Execute);

    public interface ICommandRegistry {
        void Register(CommandDefinition command);
        IEnumerable<CommandDefinition> Search(string query);
    }
    ```
*   **Logic:** The Registry must be a Singleton in the Host. Modules register commands during `InitializeAsync()`.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Register a command "TestCommand". Search for "tst". Verify "TestCommand" is returned.
*   **Test:** Verify that registering a duplicate ID throws an exception or logs a warning.

**LCS-03 (Performance Log)**
1.  Define interface in `Abstractions`.
2.  Implement `CommandRegistry` in `Host`.
3.  Inject `ICommandRegistry` into the `EditorModule`.
4.  In `EditorModule.InitializeAsync`, register `CommandDefinition("editor.save", "Save File", "File", ...)`.
5.  Wire up the `MainWindow` input bindings to forward key presses to the Registry.