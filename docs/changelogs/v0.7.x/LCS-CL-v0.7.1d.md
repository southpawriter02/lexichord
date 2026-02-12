# Changelog: v0.7.1d — Agent Selector UI

**Feature ID:** AGT-071d
**Version:** 0.7.1d
**Date:** 2026-02-11
**Status:** ✅ Complete

---

## Overview

Implements the Agent Selector UI component for the Co-pilot panel, completing the Agent Registry system (v0.7.1). Provides a user-friendly Avalonia-based dropdown for discovering, selecting, and switching between agents and personas. Integrates with the Agent Registry (v0.7.1b) for agent retrieval, Settings Service (v0.1.6a) for persistence, License Context (v0.0.4c) for tier enforcement, and MediatR (v0.0.7a) for real-time event updates.

---

## What's New

### Agent Selector ViewModel

**Location:** `Lexichord.Modules.Agents.ViewModels.AgentSelectorViewModel`

**Responsibilities:**
- Manages agent selection state and user interactions
- Subscribes to MediatR events (`AgentRegisteredEvent`, `PersonaSwitchedEvent`, `AgentConfigReloadedEvent`)
- Persists favorites, recents, and last selection via `ISettingsService`
- Provides search/filter functionality
- Routes upgrade prompts for locked agents

**Observable Properties:**
```csharp
[ObservableProperty] private string _searchQuery = string.Empty;
[ObservableProperty] private AgentItemViewModel? _selectedAgent;
[ObservableProperty] private PersonaItemViewModel? _selectedPersona;
[ObservableProperty] private bool _isDropdownOpen;
[ObservableProperty] private bool _isLoading = true;
[ObservableProperty] private string? _errorMessage;

public ObservableCollection<AgentItemViewModel> AllAgents { get; } = [];
public ObservableCollection<AgentItemViewModel> FavoriteAgents { get; } = [];
public ObservableCollection<AgentItemViewModel> RecentAgents { get; } = [];
public ObservableCollection<AgentItemViewModel> FilteredAgents { get; } // Computed
```

**Commands:**
- `InitializeCommand` — Loads agents, favorites, recents, restores last selection
- `SelectAgentCommand` — Selects agent, updates recents, persists choice, shows upgrade prompt if locked
- `SelectPersonaCommand` — Calls `IAgentRegistry.SwitchPersona()`
- `ToggleFavoriteCommand` — Adds/removes from favorites, persists
- `ToggleDropdownCommand` — Opens/closes dropdown
- `CloseDropdownCommand` — Closes dropdown, clears search

**Settings Keys:**
- `agent.last_used` — string (AgentId)
- `agent.last_persona` — string (PersonaId)
- `agent.favorites` — List<string> (AgentIds)
- `agent.recent` — List<string> (AgentIds, max 5)

### Agent Item ViewModel

**Location:** `Lexichord.Modules.Agents.ViewModels.AgentItemViewModel`

**Responsibilities:**
- Wraps `AgentConfiguration` for UI display
- Exposes computed properties for tier badges, lock status, accessibility

**Observable Properties:**
```csharp
[ObservableProperty] private string _agentId;
[ObservableProperty] private string _name;
[ObservableProperty] private string _description;
[ObservableProperty] private string _icon;
[ObservableProperty] private AgentCapabilities _capabilities;
[ObservableProperty] private LicenseTier _requiredTier;
[ObservableProperty] private bool _canAccess;
[ObservableProperty] private bool _isFavorite;
[ObservableProperty] private bool _isSelected;

public ObservableCollection<PersonaItemViewModel> Personas { get; }
```

**Computed Properties:**
- `DefaultPersona` — First persona or null
- `TierBadgeText` — "WRITER", "PRO", "TEAMS", "ENTERPRISE" (empty for Core)
- `ShowTierBadge` — True if `RequiredTier > Core`
- `IsLocked` — True if `!CanAccess && RequiredTier > Core`
- `CapabilitiesSummary` — Calls `Capabilities.ToDisplayString()`
- `AccessibilityLabel` — Full description for screen readers

### Persona Item ViewModel

**Location:** `Lexichord.Modules.Agents.ViewModels.PersonaItemViewModel`

**Responsibilities:**
- Wraps `AgentPersona` for UI display
- Provides temperature-based labels

**Observable Properties:**
```csharp
[ObservableProperty] private string _personaId;
[ObservableProperty] private string _displayName;
[ObservableProperty] private string _tagline;
[ObservableProperty] private string? _voiceDescription;
[ObservableProperty] private double _temperature;
[ObservableProperty] private bool _isSelected;
```

**Computed Properties:**
- `TemperatureLabel` — "Focused" (<0.3), "Balanced" (0.3-0.6), "Creative" (0.6-0.9), "Experimental" (≥0.9)
- `AccessibilityLabel` — Full description for screen readers

### Agent Selector View

**Location:** `Lexichord.Modules.Agents.Views.AgentSelectorView.axaml`

**Structure:**
- **Trigger Button** — Border with hover effects, displays current agent icon/name/persona, chevron (rotates when open)
- **Popup** — PlacementMode="Bottom", IsLightDismissEnabled="True"
  - **Search TextBox** — With magnifying glass icon, watermark "Search agents..."
  - **ScrollViewer** — MaxHeight="400", contains 3 sections
    - **Favorites Section** — IsVisible="{Binding !!FavoriteAgents.Count}", header "FAVORITES"
    - **Recent Section** — IsVisible="{Binding !!RecentAgents.Count}", header "RECENT"
    - **All Agents Section** — Header "ALL AGENTS", binds to `FilteredAgents`
  - **No Results Message** — IsVisible="{Binding !FilteredAgents.Count}"

**Styling:**
- Uses `DynamicResource` for theme support
- Applies hover/open state transitions (background, border, transform)
- Includes `AutomationProperties.Name` for accessibility
- Chevron rotates 180deg when dropdown is open

### Agent Card View

**Location:** `Lexichord.Modules.Agents.Views.AgentCardView.axaml`

**Structure:**
- **Border.agent-card** — With `.selected` and `.locked` classes
  - **Grid** — ColumnDefinitions="Auto,*,Auto,Auto,Auto"
    - **Agent Icon** — 40x40 rounded border
    - **Name + Description + Capabilities** — StackPanel with 3 TextBlocks
    - **Tier Badge** — Border with pill styling (CornerRadius="12"), IsVisible="{Binding ShowTierBadge}"
    - **Favorite Toggle** — Button with star icon (filled/outline via FavoriteIconConverter)
    - **Status Icon** — Panel with lock (IsLocked) or checkmark (IsSelected)
  - **Persona Selection** — StackPanel with RadioButtons, IsVisible="{Binding !!Personas.Count}"

**Styling:**
- `.selected` — Highlighted background (`AccentMutedBrush`), border (`AccentPrimaryBrush`)
- `.locked` — Opacity 0.6, cursor "Not-allowed", no hover effect
- Tier badge — Background `AccentPrimaryBrush`, white text, bold 10px font

### Favorite Icon Converter

**Location:** `Lexichord.Modules.Agents.Converters.FavoriteIconConverter`

**Purpose:** Converts boolean `IsFavorite` to SVG path data for star icons.

**Logic:**
- `true` → Filled star path (Material Design star icon)
- `false` → Outline star path (Material Design star outline)
- `null` / non-boolean → Outline star (fallback)

**Usage:**
```xml
<PathIcon Data="{Binding IsFavorite, Converter={StaticResource FavoriteIconConverter}}" />
```

### DI Registration

**Extension Method:** `AgentsServiceCollectionExtensions.AddAgentSelectorUI()`

**Registrations:**
```csharp
services.AddTransient<AgentSelectorViewModel>();
services.AddTransient<AgentItemViewModel>();
services.AddTransient<PersonaItemViewModel>();
```

**Lifetime:** Transient for per-instance isolation, allowing independent state management for each UI component instance.

---

## Files Changed

### New Files (11)

| File | Lines | Description |
|------|-------|-------------|
| `ViewModels/AgentSelectorViewModel.cs` | ~680 | Main selector ViewModel |
| `ViewModels/AgentItemViewModel.cs` | ~240 | Agent display wrapper |
| `ViewModels/PersonaItemViewModel.cs` | ~140 | Persona display wrapper |
| `Views/AgentSelectorView.axaml` | ~250 | Dropdown UI component |
| `Views/AgentSelectorView.axaml.cs` | ~35 | View code-behind |
| `Views/AgentCardView.axaml` | ~200 | Agent card UI component |
| `Views/AgentCardView.axaml.cs` | ~35 | Card code-behind |
| `Converters/FavoriteIconConverter.cs` | ~115 | Boolean → star icon converter |
| `Tests/ViewModels/AgentSelectorViewModelTests.cs` | ~550 | 17 unit tests |
| `Tests/ViewModels/AgentItemViewModelTests.cs` | ~280 | 12 unit tests |
| `Tests/ViewModels/PersonaItemViewModelTests.cs` | ~180 | 4 unit tests |

**Total New Code:** ~2,705 lines

### Modified Files (3)

| File | Change |
|------|--------|
| `Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddAgentSelectorUI()` extension method |
| `docs/changelogs/CHANGELOG.md` | Added v0.7.1d entry |
| `docs/specs/DEPENDENCY-MATRIX.md` | Added v0.7.1d interface entries |

---

## API Reference

### AgentSelectorViewModel

```csharp
public partial class AgentSelectorViewModel : ObservableObject,
    IRecipient<AgentRegisteredEvent>,
    IRecipient<PersonaSwitchedEvent>,
    IRecipient<AgentConfigReloadedEvent>
{
    // Observable Properties
    public string SearchQuery { get; set; }
    public AgentItemViewModel? SelectedAgent { get; set; }
    public PersonaItemViewModel? SelectedPersona { get; set; }
    public bool IsDropdownOpen { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    // Collections
    public ObservableCollection<AgentItemViewModel> AllAgents { get; }
    public ObservableCollection<AgentItemViewModel> FavoriteAgents { get; }
    public ObservableCollection<AgentItemViewModel> RecentAgents { get; }
    public ObservableCollection<AgentItemViewModel> FilteredAgents { get; }

    // Commands
    public IAsyncRelayCommand InitializeCommand { get; }
    public IAsyncRelayCommand<AgentItemViewModel> SelectAgentCommand { get; }
    public IAsyncRelayCommand<PersonaItemViewModel> SelectPersonaCommand { get; }
    public IAsyncRelayCommand<AgentItemViewModel> ToggleFavoriteCommand { get; }
    public IRelayCommand ToggleDropdownCommand { get; }
    public IRelayCommand CloseDropdownCommand { get; }

    // Event Handlers
    Task Receive(AgentRegisteredEvent message, CancellationToken cancellationToken);
    Task Receive(PersonaSwitchedEvent message, CancellationToken cancellationToken);
    Task Receive(AgentConfigReloadedEvent message, CancellationToken cancellationToken);
}
```

### AgentItemViewModel

```csharp
public partial class AgentItemViewModel : ObservableObject
{
    // Observable Properties
    public string AgentId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public AgentCapabilities Capabilities { get; set; }
    public LicenseTier RequiredTier { get; set; }
    public bool CanAccess { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsSelected { get; set; }
    public ObservableCollection<PersonaItemViewModel> Personas { get; }

    // Computed Properties
    public PersonaItemViewModel? DefaultPersona { get; }
    public string TierBadgeText { get; }
    public bool ShowTierBadge { get; }
    public bool IsLocked { get; }
    public string CapabilitiesSummary { get; }
    public string AccessibilityLabel { get; }
}
```

### PersonaItemViewModel

```csharp
public partial class PersonaItemViewModel : ObservableObject
{
    // Observable Properties
    public string PersonaId { get; set; }
    public string DisplayName { get; set; }
    public string Tagline { get; set; }
    public string? VoiceDescription { get; set; }
    public double Temperature { get; set; }
    public bool IsSelected { get; set; }

    // Computed Properties
    public string TemperatureLabel { get; }
    public string AccessibilityLabel { get; }
}
```

---

## Usage Examples

### Initialize Agent Selector

```csharp
// In CoPilotViewModel constructor
public CoPilotViewModel(
    AgentSelectorViewModel agentSelectorViewModel,
    // other dependencies...)
{
    AgentSelectorViewModel = agentSelectorViewModel;
}

// In CoPilotView.axaml
<views:AgentSelectorView DataContext="{Binding AgentSelectorViewModel}" />
```

### Programmatic Agent Selection

```csharp
// Select an agent by ID
var agent = agentSelectorViewModel.AllAgents.FirstOrDefault(a => a.AgentId == "editor");
await agentSelectorViewModel.SelectAgentCommand.ExecuteAsync(agent);

// Toggle favorite
await agentSelectorViewModel.ToggleFavoriteCommand.ExecuteAsync(agent);

// Switch persona
var persona = agent.Personas.FirstOrDefault(p => p.PersonaId == "strict");
await agentSelectorViewModel.SelectPersonaCommand.ExecuteAsync(persona);
```

### React to Agent Selection

```csharp
// Subscribe to SelectedAgent changes
agentSelectorViewModel.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(AgentSelectorViewModel.SelectedAgent))
    {
        var selectedAgent = agentSelectorViewModel.SelectedAgent;
        // React to agent change...
    }
};
```

---

## Validation Rules

### AgentSelectorViewModel

- **SearchQuery**: Case-insensitive filter on `Name`, `Description`, `AgentId`
- **SelectedAgent**: Must be from `AllAgents` collection
- **Favorites**: Max 50 agents (not enforced in v0.7.1d, for future enhancement)
- **Recents**: Max 5 agents, newest first

### AgentItemViewModel

- **AgentId**: Must match kebab-case pattern from `AgentConfiguration`
- **RequiredTier**: Must be valid `LicenseTier` enum value
- **CanAccess**: Set by `IAgentRegistry.CanAccess()`

### PersonaItemViewModel

- **PersonaId**: Must match kebab-case pattern from `AgentPersona`
- **Temperature**: Must be 0.0 to 2.0 (validated by `AgentPersona`)

---

## Dependencies

**No new NuGet packages required.**

Existing dependencies:
- `IAgentRegistry` (v0.7.1b) — Agent and persona retrieval
- `ISettingsService` (v0.1.6a) — Favorites and recents persistence
- `ILicenseContext` (v0.0.4c) — Tier display and access control
- `IMediator` (v0.0.7a) — Event subscription and upgrade prompts
- `CommunityToolkit.Mvvm` (8.x) — ObservableProperty, RelayCommand
- Avalonia (11.x) — UI framework

---

## Breaking Changes

**None.** All changes are additive:
- New ViewModels in Modules.Agents
- New Views in Modules.Agents
- New converter in Modules.Agents
- New extension method in AgentsServiceCollectionExtensions
- Existing interfaces unchanged

---

## Design Decisions

### ViewModel Lifetime

**Decision:** Registered ViewModels as transient instead of singleton.

**Rationale:**
- Each UI component instance needs independent state (favorites, selection, dropdown state)
- Avoids state leakage between multiple agent selector instances
- Simplifies testing with fresh instances per test

**Trade-off:** Slight memory overhead for multiple instances (negligible with UI component count).

### Settings Persistence

**Decision:** Used `List<string>` for favorites/recents instead of custom data structures.

**Rationale:**
- Simple serialization/deserialization via `ISettingsService`
- No need for custom JSON converters
- Easy to inspect and modify in settings files

**Trade-off:** No metadata (e.g., timestamp for recents). Can be enhanced in future versions.

### MediatR Event Subscriptions

**Decision:** Subscribed to 3 MediatR events via `IRecipient<T>` interfaces instead of manual `Subscribe()` calls.

**Rationale:**
- Automatic registration by MediatR
- Type-safe event handling
- Unsubscription managed by ViewModel disposal

**Trade-off:** Requires MediatR.RegisterAll() in DI setup (already in place from v0.0.7a).

### Inline Persona Selection

**Decision:** Embedded persona radio buttons inside `AgentCardView` instead of separate submenu.

**Rationale:**
- Simpler UI for agents with 2-3 personas (common case)
- Reduces click depth for persona switching
- Follows existing UI patterns (ProfileSelectorWidget uses ContextMenu, but personas are different)

**Trade-off:** Card height increases for agents with many personas (>5). Future enhancement could collapse/expand personas.

### Computed Properties vs Converters

**Decision:** Used computed properties (`TierBadgeText`, `TemperatureLabel`) instead of value converters.

**Rationale:**
- Simpler to test (no converter registration needed)
- Clearer logic in ViewModel code
- Type-safe with IntelliSense support

**Trade-off:** Slight code duplication if same logic needed elsewhere. Can extract to extensions if needed.

---

## Testing

### Unit Tests

**Total Tests:** 33 (all passing)

**Test Breakdown:**
- **AgentSelectorViewModelTests:** 17 tests
  - InitializeAsync (4 tests)
  - SelectAgentAsync (4 tests)
  - SelectPersonaAsync (2 tests)
  - ToggleFavoriteAsync (3 tests)
  - FilteredAgents (1 test)
  - MediatR event handlers (3 tests)
- **AgentItemViewModelTests:** 12 tests
  - DefaultPersona (2 tests)
  - TierBadgeText (5 tests)
  - ShowTierBadge (2 tests)
  - IsLocked (3 tests)
- **PersonaItemViewModelTests:** 4 tests
  - TemperatureLabel (1 test with 12 inline data cases)
  - AccessibilityLabel (3 tests)

**Testing Strategy:**
- Moq for all dependencies (`IAgentRegistry`, `ISettingsService`, `ILicenseContext`, `IMediator`, `ILogger`)
- FluentAssertions for readable assertions
- Xunit for test framework
- [Trait] attributes for categorization ("Unit", "v0.7.1d")
- Constructor-based mock setup with default behaviors

**Coverage:** 100% for ViewModels (Views excluded from unit test coverage as UI components)

---

## Performance Considerations

### Agent Loading

- **Time Complexity:** O(n) where n = number of agents (typically 4-20)
- **Memory:** ~1KB per agent ViewModel
- **Optimization:** Favorites and recents use hash-based lookups (O(1))

### Search Filtering

- **Time Complexity:** O(n) where n = number of agents
- **Debouncing:** None (not needed for small agent counts <100)
- **Optimization:** LINQ `Where()` with case-insensitive `Contains()` (fast for small collections)

### Settings Persistence

- **Write Frequency:** On every favorite toggle, agent selection
- **Write Size:** <1KB per settings key
- **Optimization:** Batching not needed (settings service handles async I/O)

---

## Accessibility

### Keyboard Navigation

- **Tab:** Navigate between trigger button, search, agents, personas
- **Arrow Keys:** Navigate within dropdown (ScrollViewer handles)
- **Enter/Space:** Select agent or persona
- **Escape:** Close dropdown (via IsLightDismissEnabled)

### Screen Readers

- **AutomationProperties.Name:** Set on all interactive elements
- **AccessibilityLabel:** Computed properties provide full context for agents and personas
- **Focus Management:** Dropdown opening focuses search box

### Visual Aids

- **Hover Effects:** Border and background color transitions
- **Selected State:** Highlighted background and checkmark icon
- **Locked State:** Reduced opacity and lock icon
- **Tier Badges:** Color-coded pills for license tiers

---

## Future Enhancements

### Potential Improvements (Not in v0.7.1d)

1. **Agent Search History** — Track recent search queries
2. **Agent Tags/Categories** — Group agents by capability or domain
3. **Persona Comparison** — Side-by-side persona comparison UI
4. **Agent Recommendations** — Suggest agents based on current document type
5. **Custom Agent Creation** — UI for YAML agent authoring
6. **Agent Preview** — Quick preview of agent responses before selection
7. **Keyboard Shortcuts** — Assign hotkeys to favorite agents (e.g., Ctrl+1, Ctrl+2)
8. **Drag-and-Drop Reordering** — Custom order for favorites

---

## Related Documents

- [LCS-DES-v0.7.1d.md](../../specs/v0.7.x/v0.7.1/LCS-DES-v0.7.1d.md) — Design Specification
- [LCS-DES-v0.7.1-INDEX.md](../../specs/v0.7.x/v0.7.1/LCS-DES-v0.7.1-INDEX.md) — Index
- [LCS-SBD-v0.7.1.md](../../specs/v0.7.x/v0.7.1/LCS-SBD-v0.7.1.md) — Scope Breakdown
- [LCS-CL-v0.7.1a.md](LCS-CL-v0.7.1a.md) — Agent Configuration Model
- [LCS-CL-v0.7.1b.md](LCS-CL-v0.7.1b.md) — Agent Registry Implementation
- [LCS-CL-v0.7.1c.md](LCS-CL-v0.7.1c.md) — Agent Configuration Files

---

**Author:** Lexichord Development Team
**Reviewed:** Automated CI
**Approved:** 2026-02-11
