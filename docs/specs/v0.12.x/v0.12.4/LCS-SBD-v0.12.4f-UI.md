# LDS-01: Memory Inspector UI

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Feature ID** | `MEM-UI-01` |
| **Feature Name** | Memory Inspector UI |
| **Target Version** | `v0.12.4f` |
| **Module Scope** | `Lexichord.Modules.Agents.UI` |
| **Swimlane** | Memory |
| **License Tier** | WriterPro |
| **Feature Gate Key** | `FeatureFlags.Agents.Memory.Inspector` |
| **Author** | Agent Architecture Lead |
| **Reviewer** | Lead Architect |
| **Status** | Draft |
| **Last Updated** | 2026-02-04 |
| **Parent Spec** | [LCS-SBD-v0.12.4-AGT](./LCS-SBD-v0.12.4-AGT.md) |
| **Depends On** | [v0.12.4a (Working Memory)](./LCS-SBD-v0.12.4a-WRK.md), [v0.12.4b (Long-Term Memory)](./LCS-SBD-v0.12.4b-LTM.md), [v0.12.4c (Context Window)](./LCS-SBD-v0.12.4c-CTX.md) |
| **Estimated Hours** | 4 |

---

## 2. Executive Summary

### 2.1 The Requirement

Developers and administrators need visibility into agent memory for debugging, monitoring, and management. Without inspection tools, memory issues are difficult to diagnose, and storage usage is opaque. The system needs a comprehensive UI for viewing, searching, and managing agent memory.

### 2.2 The Proposed Solution

Implement a memory inspector UI providing:
- Tabbed interface for Working Memory, Long-Term Memory, and Context Window
- Real-time memory state visualization
- Search and filter capabilities
- Entry viewing and editing (where appropriate)
- Storage statistics and monitoring
- Snapshot management interface

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Modules.Agents` — `IAgentMemory`, `IWorkingMemory` (v0.12.4a)
- `Lexichord.Modules.Agents` — `ILongTermMemory`, `MemoryEntry` (v0.12.4b)
- `Lexichord.Modules.Agents` — `IContextWindow`, `ContextItem` (v0.12.4c)
- `Lexichord.Host.UI` — AvaloniaUI infrastructure

**NuGet Packages:**
- `Avalonia` (UI framework)
- `Avalonia.ReactiveUI` (MVVM support)
- `ReactiveUI` (reactive programming)
- `LiveChartsCore.SkiaSharpView.Avalonia` (charts)

### 3.2 Licensing Behavior

- **Load Behavior:** [x] **UI Gate** — Inspector menu item hidden for Core tier. WriterPro gets basic inspector. Teams/Enterprise get advanced features.
- **Fallback Experience:** Core users see "Upgrade for Memory Inspector" in the Agents menu. WriterPro sees basic stats. Teams sees full inspector with editing.

---

## 4. Data Contract (The API)

### 4.1 Memory Inspector Service

```csharp
namespace Lexichord.Modules.Agents.UI;

/// <summary>
/// Service providing data for the Memory Inspector UI.
/// </summary>
/// <remarks>
/// <para>
/// The inspector service aggregates data from working memory, long-term
/// memory, and context window for display in the UI.
/// </para>
/// <para>
/// Thread Safety: All methods are thread-safe.
/// </para>
/// </remarks>
public interface IMemoryInspectorService
{
    /// <summary>
    /// Gets the current state of working memory for display.
    /// </summary>
    /// <param name="agentId">The agent to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Working memory view model data.</returns>
    Task<WorkingMemoryInspection> GetWorkingMemoryAsync(
        AgentId agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets long-term memory entries with pagination.
    /// </summary>
    /// <param name="agentId">The agent to inspect.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Long-term memory inspection data.</returns>
    Task<LongTermMemoryInspection> GetLongTermMemoryAsync(
        AgentId agentId,
        MemoryFilter? filter = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets context window state for display.
    /// </summary>
    /// <param name="agentId">The agent to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Context window inspection data.</returns>
    Task<ContextWindowInspection> GetContextWindowAsync(
        AgentId agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets aggregate memory statistics.
    /// </summary>
    /// <param name="agentId">The agent to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memory statistics for charts.</returns>
    Task<MemoryStatistics> GetStatisticsAsync(
        AgentId agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Searches across all memory types.
    /// </summary>
    /// <param name="agentId">The agent to search.</param>
    /// <param name="query">Search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Combined search results.</returns>
    Task<MemorySearchResults> SearchAsync(
        AgentId agentId,
        string query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets entry details for viewing.
    /// </summary>
    /// <param name="agentId">The agent.</param>
    /// <param name="entryId">The entry ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Detailed entry view.</returns>
    Task<MemoryEntryDetail?> GetEntryDetailAsync(
        AgentId agentId,
        Guid entryId,
        CancellationToken ct = default);

    /// <summary>
    /// Edits a working memory entry (Teams+ only).
    /// </summary>
    /// <param name="agentId">The agent.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if edited successfully.</returns>
    /// <exception cref="LicenseRestrictionException">
    /// Thrown for WriterPro tier (editing requires Teams+).
    /// </exception>
    Task<bool> EditWorkingMemoryAsync(
        AgentId agentId,
        string key,
        string newValue,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a working memory entry.
    /// </summary>
    Task<bool> DeleteWorkingMemoryEntryAsync(
        AgentId agentId,
        string key,
        CancellationToken ct = default);

    /// <summary>
    /// Forgets a long-term memory entry.
    /// </summary>
    Task<bool> ForgetLongTermEntryAsync(
        AgentId agentId,
        Guid entryId,
        bool permanent,
        CancellationToken ct = default);
}
```

### 4.2 View Model Types

```csharp
namespace Lexichord.Modules.Agents.UI;

/// <summary>
/// Working memory inspection data.
/// </summary>
public sealed record WorkingMemoryInspection
{
    public required AgentId AgentId { get; init; }
    public required IReadOnlyList<WorkingMemoryEntryView> Entries { get; init; }
    public required IReadOnlyList<WorkingMemoryScopeView> Scopes { get; init; }
    public required int TotalEntries { get; init; }
    public required long EstimatedSizeBytes { get; init; }
}

/// <summary>
/// View model for a working memory entry.
/// </summary>
public sealed record WorkingMemoryEntryView
{
    public required string Key { get; init; }
    public required string TypeName { get; init; }
    public required string ValuePreview { get; init; }
    public required string FullValue { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? ScopeName { get; init; }
    public Guid? ScopeId { get; init; }
}

/// <summary>
/// View model for a working memory scope.
/// </summary>
public sealed record WorkingMemoryScopeView
{
    public required Guid ScopeId { get; init; }
    public required string ScopeName { get; init; }
    public Guid? ParentScopeId { get; init; }
    public required int EntryCount { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
/// Long-term memory inspection data.
/// </summary>
public sealed record LongTermMemoryInspection
{
    public required AgentId AgentId { get; init; }
    public required IReadOnlyList<LongTermMemoryEntryView> Entries { get; init; }
    public required int TotalEntries { get; init; }
    public required int TotalPages { get; init; }
    public required int CurrentPage { get; init; }
    public required long StorageBytesUsed { get; init; }
    public required long StorageBytesLimit { get; init; }
    public required IReadOnlyDictionary<MemoryType, int> EntriesByType { get; init; }
}

/// <summary>
/// View model for a long-term memory entry.
/// </summary>
public sealed record LongTermMemoryEntryView
{
    public required Guid Id { get; init; }
    public required MemoryType Type { get; init; }
    public required string ContentPreview { get; init; }
    public required float Importance { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastAccessedAt { get; init; }
    public required int AccessCount { get; init; }
    public required bool IsForgotten { get; init; }
}

/// <summary>
/// Context window inspection data.
/// </summary>
public sealed record ContextWindowInspection
{
    public required AgentId AgentId { get; init; }
    public required IReadOnlyList<ContextItemView> Items { get; init; }
    public required int TotalItems { get; init; }
    public required int CurrentTokens { get; init; }
    public required int MaxTokens { get; init; }
    public required float UsagePercent { get; init; }
    public required IReadOnlyDictionary<ContextItemType, int> TokensByType { get; init; }
}

/// <summary>
/// View model for a context item.
/// </summary>
public sealed record ContextItemView
{
    public required Guid Id { get; init; }
    public required ContextItemType Type { get; init; }
    public required string ContentPreview { get; init; }
    public required int Priority { get; init; }
    public required int TokenCount { get; init; }
    public required bool IsPinned { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
    public string? SourceRef { get; init; }
}

/// <summary>
/// Memory statistics for dashboard charts.
/// </summary>
public sealed record MemoryStatistics
{
    public required AgentId AgentId { get; init; }

    // Working memory
    public required int WorkingMemoryEntries { get; init; }
    public required int WorkingMemoryScopes { get; init; }

    // Long-term memory
    public required long LongTermEntries { get; init; }
    public required long ForgottenEntries { get; init; }
    public required long StorageBytesUsed { get; init; }
    public required long StorageBytesLimit { get; init; }
    public required IReadOnlyDictionary<MemoryType, long> EntriesByType { get; init; }
    public required IReadOnlyList<TagUsage> TopTags { get; init; }

    // Context window
    public required int ContextItems { get; init; }
    public required int ContextTokens { get; init; }
    public required int ContextMaxTokens { get; init; }
    public required IReadOnlyDictionary<ContextItemType, int> ContextByType { get; init; }

    // Time series (last 24 hours)
    public required IReadOnlyList<MemoryUsagePoint> UsageHistory { get; init; }
}

/// <summary>
/// Point in memory usage time series.
/// </summary>
public sealed record MemoryUsagePoint
{
    public required DateTimeOffset Timestamp { get; init; }
    public required long StorageBytes { get; init; }
    public required int ContextTokens { get; init; }
    public required int WorkingEntries { get; init; }
}

/// <summary>
/// Combined search results across memory types.
/// </summary>
public sealed record MemorySearchResults
{
    public required string Query { get; init; }
    public required IReadOnlyList<SearchResultItem> Results { get; init; }
    public required int TotalResults { get; init; }
    public required TimeSpan SearchTime { get; init; }
}

/// <summary>
/// A single search result item.
/// </summary>
public sealed record SearchResultItem
{
    public required Guid Id { get; init; }
    public required string MemorySource { get; init; }  // "working", "longterm", "context"
    public required string ContentPreview { get; init; }
    public required float Relevance { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Detailed view of a memory entry.
/// </summary>
public sealed record MemoryEntryDetail
{
    public required Guid Id { get; init; }
    public required string Source { get; init; }
    public required string FullContent { get; init; }
    public required string TypeName { get; init; }
    public MemoryType? MemoryType { get; init; }
    public float? Importance { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastAccessedAt { get; init; }
    public int? AccessCount { get; init; }
    public bool HasEmbedding { get; init; }
}
```

---

## 5. UI/UX Specifications

### 5.1 Visual Layout

```
┌────────────────────────────────────────────────────────────────────────────────┐
│ Memory Inspector: Agent-12345                              [Refresh] [Export]  │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│ [Tab: Working Memory]  [Tab: Long-Term Memory]  [Tab: Context Window]  [Stats] │
│                                                                                │
│ ═══════════════════════════════════════════════════════════════════════════   │
│ Working Memory                                         [Search: ____________]  │
│ ═══════════════════════════════════════════════════════════════════════════   │
│                                                                                │
│ Scopes: [● Global (3)] [○ conversation-123 (5)] [○ subtask-456 (2)]          │
│                                                                                │
│ ┌─────────────────────────────────────────────────────────────────────────┐   │
│ │ Key              │ Type     │ Value Preview           │ Actions         │   │
│ ├─────────────────────────────────────────────────────────────────────────┤   │
│ │ current_user     │ Object   │ { id: "user-123", ...  │ [View] [Edit]   │   │
│ │ conversation_id  │ String   │ "conv-789"             │ [View] [Edit]   │   │
│ │ task_status      │ String   │ "in_progress"          │ [View] [Edit]   │   │
│ └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                │
│ Total: 10 entries | Est. Size: 4.2 KB                     [+ Add Entry]       │
└────────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────────┐
│ Memory Inspector: Agent-12345                              [Refresh] [Export]  │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│ [Tab: Working Memory]  [Tab: Long-Term Memory ▶]  [Tab: Context Window]  [Stats]│
│                                                                                │
│ ═══════════════════════════════════════════════════════════════════════════   │
│ Long-Term Memory (342 entries)                Storage: 156.3 MB / 1 GB [████░]│
│ ═══════════════════════════════════════════════════════════════════════════   │
│                                                                                │
│ [Filter: Type ▼] [Tags: ____] [Importance: >=___] [Semantic Search: _______]  │
│                                                                                │
│ ┌─────────────────────────────────────────────────────────────────────────┐   │
│ │ Type      │ Content Preview            │ Importance │ Age    │ Actions  │   │
│ ├─────────────────────────────────────────────────────────────────────────┤   │
│ │ Fact      │ "The user prefers dark..." │ ████░░ 0.82│ 2 days │ [V][F]   │   │
│ │ Insight   │ "Based on past conversa..."│ ██░░░░ 0.25│ 5 days │ [V][F]   │   │
│ │ Event     │ "User completed onboard..."│ █████░ 0.73│ 1 day  │ [V][F]   │   │
│ │ Preference│ "User prefers concise..."  │ ███░░░ 0.55│ 3 days │ [V][F]   │   │
│ └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                │
│ Page: [◀ 1 2 3 4 5 ▶]                      [+ Add Entry] [Cleanup Forgotten]  │
└────────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────────┐
│ Memory Inspector: Agent-12345                              [Refresh] [Export]  │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│ [Tab: Working Memory]  [Tab: Long-Term Memory]  [Tab: Context Window ▶] [Stats]│
│                                                                                │
│ ═══════════════════════════════════════════════════════════════════════════   │
│ Context Window                              Tokens: 2,847 / 4,096 [██████░░░] │
│ ═══════════════════════════════════════════════════════════════════════════   │
│                                                                                │
│ ┌─────────────────────────────────────────────────────────────────────────┐   │
│ │ Pri │ Type             │ Tokens │ Pinned │ Preview          │ Actions   │   │
│ ├─────────────────────────────────────────────────────────────────────────┤   │
│ │ 100 │ SystemPrompt     │ 512    │ ✓ Pin  │ "You are a help..."│ [View]  │   │
│ │ 95  │ UserMessage      │ 156    │ ✓ Pin  │ "Can you help m..."│ [View]  │   │
│ │ 90  │ AssistantMessage │ 234    │ ○      │ "Of course! I'l..."│ [View]  │   │
│ │ 85  │ RetrievedDoc     │ 389    │ ○      │ "[Memory] User ..."│ [View]  │   │
│ │ 60  │ ToolResult       │ 145    │ ○      │ "{ status: 'ok'..."│ [View]  │   │
│ └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                │
│ Compaction: [Strategy: RemoveLowPriority ▼] [Threshold: 85%] [Manual Compact] │
│ [Preview Build]                                                                │
└────────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────────┐
│ Memory Inspector: Agent-12345                              [Refresh] [Export]  │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│ [Tab: Working Memory]  [Tab: Long-Term Memory]  [Tab: Context Window]  [Stats▶]│
│                                                                                │
│ ═══════════════════════════════════════════════════════════════════════════   │
│ Memory Statistics                                                              │
│ ═══════════════════════════════════════════════════════════════════════════   │
│                                                                                │
│ ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐     │
│ │ Working Memory      │  │ Long-Term Memory    │  │ Context Window      │     │
│ │                     │  │                     │  │                     │     │
│ │ Entries: 10         │  │ Entries: 342        │  │ Items: 18           │     │
│ │ Scopes: 3           │  │ Forgotten: 28       │  │ Tokens: 2,847       │     │
│ │ Size: ~4.2 KB       │  │ Storage: 156 MB     │  │ Max: 4,096          │     │
│ └─────────────────────┘  └─────────────────────┘  └─────────────────────┘     │
│                                                                                │
│ ┌─────────────────────────────────┐  ┌─────────────────────────────────┐      │
│ │ Storage Usage (24h)             │  │ Entries by Type                 │      │
│ │ ▲                               │  │                                 │      │
│ │ │    ╱─────────                │  │ Fact     ████████░░░░ 45%       │      │
│ │ │   ╱                          │  │ Event    ███░░░░░░░░░ 18%       │      │
│ │ │──╱                           │  │ Insight  ██░░░░░░░░░░ 12%       │      │
│ │ └──────────────────────▶       │  │ Preference█░░░░░░░░░░░ 8%       │      │
│ │   12:00    18:00    00:00      │  │ Other    ███░░░░░░░░░ 17%       │      │
│ └─────────────────────────────────┘  └─────────────────────────────────┘      │
│                                                                                │
│ [Snapshots: 5 stored] [Create Snapshot] [Manage Snapshots] [Retention Policy] │
└────────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Component Hierarchy

```
MemoryInspectorView
├── MemoryInspectorToolbar
│   ├── AgentSelector
│   ├── RefreshButton
│   └── ExportButton
│
├── MemoryTabControl
│   ├── WorkingMemoryTab
│   │   ├── ScopeSelector
│   │   ├── SearchBox
│   │   ├── WorkingMemoryDataGrid
│   │   │   └── WorkingMemoryEntryRow
│   │   └── WorkingMemoryFooter
│   │
│   ├── LongTermMemoryTab
│   │   ├── FilterBar
│   │   │   ├── TypeFilter
│   │   │   ├── TagFilter
│   │   │   ├── ImportanceFilter
│   │   │   └── SemanticSearchBox
│   │   ├── LongTermMemoryDataGrid
│   │   │   └── LongTermMemoryEntryRow
│   │   ├── Pagination
│   │   └── StorageIndicator
│   │
│   ├── ContextWindowTab
│   │   ├── TokenUsageBar
│   │   ├── ContextItemsDataGrid
│   │   │   └── ContextItemRow
│   │   └── CompactionControls
│   │
│   └── StatisticsTab
│       ├── SummaryCards
│       ├── StorageChart
│       ├── TypeDistributionChart
│       └── SnapshotControls
│
└── EntryDetailPanel (Flyout)
    ├── EntryHeader
    ├── ContentViewer
    ├── MetadataSection
    └── ActionButtons
```

### 5.3 Key Interactions

1. **Scope Selection:** Clicking a scope filters working memory to that scope
2. **Entry View:** Clicking [View] opens the detail flyout panel
3. **Entry Edit:** Clicking [Edit] opens an inline editor (Teams+)
4. **Forget:** Clicking [F] prompts for confirmation then soft-deletes
5. **Semantic Search:** Typing in semantic search box triggers real-time search
6. **Compact:** Clicking [Manual Compact] runs compaction with selected strategy
7. **Create Snapshot:** Opens dialog to name and create snapshot

---

## 6. Implementation Logic

### 6.1 ViewModel Architecture

```csharp
namespace Lexichord.Modules.Agents.UI.ViewModels;

/// <summary>
/// Main view model for Memory Inspector.
/// </summary>
public partial class MemoryInspectorViewModel : ViewModelBase
{
    private readonly IMemoryInspectorService _inspectorService;
    private readonly ILicenseService _licenseService;

    [ObservableProperty]
    private AgentId? _selectedAgentId;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private WorkingMemoryInspection? _workingMemory;

    [ObservableProperty]
    private LongTermMemoryInspection? _longTermMemory;

    [ObservableProperty]
    private ContextWindowInspection? _contextWindow;

    [ObservableProperty]
    private MemoryStatistics? _statistics;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    // Feature flags based on license
    public bool CanEdit => _licenseService.HasFeature("FeatureFlags.Agents.Memory.Inspector.Edit");
    public bool CanSearch => _licenseService.HasFeature("FeatureFlags.Agents.Memory.SemanticRetrieval");

    public MemoryInspectorViewModel(
        IMemoryInspectorService inspectorService,
        ILicenseService licenseService)
    {
        _inspectorService = inspectorService;
        _licenseService = licenseService;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_selectedAgentId == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await Task.WhenAll(
                LoadWorkingMemoryAsync(),
                LoadLongTermMemoryAsync(),
                LoadContextWindowAsync(),
                LoadStatisticsAsync()
            );
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load memory: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadWorkingMemoryAsync()
    {
        WorkingMemory = await _inspectorService.GetWorkingMemoryAsync(
            _selectedAgentId!.Value);
    }

    [RelayCommand]
    private async Task EditEntryAsync(WorkingMemoryEntryView entry)
    {
        if (!CanEdit)
        {
            await ShowUpgradeDialogAsync("Entry editing requires Teams tier");
            return;
        }

        // Open edit dialog
        var result = await ShowEditDialogAsync(entry);
        if (result.Confirmed)
        {
            await _inspectorService.EditWorkingMemoryAsync(
                _selectedAgentId!.Value,
                entry.Key,
                result.NewValue);
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task ForgetEntryAsync(LongTermMemoryEntryView entry)
    {
        var confirmed = await ShowConfirmDialogAsync(
            "Forget Memory",
            $"Are you sure you want to forget this {entry.Type} entry?");

        if (confirmed)
        {
            await _inspectorService.ForgetLongTermEntryAsync(
                _selectedAgentId!.Value,
                entry.Id,
                permanent: false);
            await LoadLongTermMemoryAsync();
        }
    }

    [RelayCommand]
    private async Task CompactContextAsync()
    {
        // Get selected strategy from UI
        var strategy = SelectedCompactionStrategy;

        // This would call the context window's CompactAsync
        await RefreshAsync();
    }

    // ... additional commands
}
```

### 5.2 XAML View (Simplified)

```xml
<UserControl x:Class="Lexichord.Modules.Agents.UI.Views.MemoryInspectorView"
             xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:Lexichord.Modules.Agents.UI.ViewModels">

    <Design.DataContext>
        <vm:MemoryInspectorViewModel/>
    </Design.DataContext>

    <Grid RowDefinitions="Auto,*">
        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8" Margin="8">
            <TextBlock Text="Memory Inspector:" VerticalAlignment="Center"/>
            <ComboBox ItemsSource="{Binding Agents}"
                      SelectedItem="{Binding SelectedAgentId}"/>
            <Button Command="{Binding RefreshCommand}" Content="Refresh"/>
            <Button Content="Export"/>
        </StackPanel>

        <!-- Tab Control -->
        <TabControl Grid.Row="1" SelectedIndex="{Binding SelectedTabIndex}">
            <!-- Working Memory Tab -->
            <TabItem Header="Working Memory">
                <Grid RowDefinitions="Auto,*,Auto">
                    <!-- Scope selector and search -->
                    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="8">
                        <ItemsControl ItemsSource="{Binding WorkingMemory.Scopes}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <StackPanel Orientation="Horizontal"/>
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Button Content="{Binding ScopeName}"
                                            Command="{Binding $parent[UserControl].DataContext.SelectScopeCommand}"
                                            CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                        <TextBox Watermark="Search..." Width="200" Margin="8,0"/>
                    </StackPanel>

                    <!-- Data Grid -->
                    <DataGrid Grid.Row="1"
                              ItemsSource="{Binding WorkingMemory.Entries}"
                              AutoGenerateColumns="False">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Key" Binding="{Binding Key}"/>
                            <DataGridTextColumn Header="Type" Binding="{Binding TypeName}"/>
                            <DataGridTextColumn Header="Value" Binding="{Binding ValuePreview}"/>
                            <DataGridTemplateColumn Header="Actions">
                                <DataGridTemplateColumn.CellTemplate>
                                    <DataTemplate>
                                        <StackPanel Orientation="Horizontal">
                                            <Button Content="View"
                                                    Command="{Binding $parent[UserControl].DataContext.ViewEntryCommand}"
                                                    CommandParameter="{Binding}"/>
                                            <Button Content="Edit"
                                                    IsVisible="{Binding $parent[UserControl].DataContext.CanEdit}"
                                                    Command="{Binding $parent[UserControl].DataContext.EditEntryCommand}"
                                                    CommandParameter="{Binding}"/>
                                        </StackPanel>
                                    </DataTemplate>
                                </DataGridTemplateColumn.CellTemplate>
                            </DataGridTemplateColumn>
                        </DataGrid.Columns>
                    </DataGrid>

                    <!-- Footer -->
                    <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="8">
                        <TextBlock Text="{Binding WorkingMemory.TotalEntries, StringFormat='Total: {0} entries'}"/>
                        <TextBlock Text="{Binding WorkingMemory.EstimatedSizeBytes, StringFormat=' | Size: {0:N0} bytes'}" Margin="8,0"/>
                    </StackPanel>
                </Grid>
            </TabItem>

            <!-- Additional tabs... -->
        </TabControl>

        <!-- Loading Overlay -->
        <Border Grid.RowSpan="2"
                Background="#80000000"
                IsVisible="{Binding IsLoading}">
            <ProgressRing IsActive="{Binding IsLoading}"/>
        </Border>
    </Grid>
</UserControl>
```

---

## 7. Observability & Logging

### 7.1 Log Messages

| Level | Template |
|:------|:---------|
| Debug | `"Loaded working memory for agent {AgentId}: {Count} entries"` |
| Debug | `"Loaded long-term memory page {Page} for agent {AgentId}: {Count} entries"` |
| Info | `"Memory inspector opened for agent {AgentId}"` |
| Info | `"User edited working memory entry {Key} for agent {AgentId}"` |
| Warning | `"Memory inspector load failed: {ErrorMessage}"` |

### 7.2 Metrics

| Metric | Type | Description |
|:-------|:-----|:------------|
| `agents.inspector.opens` | Counter | Inspector open count |
| `agents.inspector.load_time_ms` | Histogram | Data load time |
| `agents.inspector.edits` | Counter | Entry edits |

---

## 8. Acceptance Criteria (QA)

### 8.1 Functional Criteria

1. **[View]** Inspector displays working memory, long-term memory, and context.
2. **[Search]** Search returns relevant entries across memory types.
3. **[Edit]** Teams+ users can edit working memory entries.
4. **[Forget]** Users can soft-delete long-term memory entries.
5. **[Stats]** Statistics tab shows accurate charts and metrics.

### 8.2 Non-Functional Criteria

1. **[Latency]** Initial load P95 < 500ms.
2. **[Usability]** All actions accessible via keyboard.
3. **[Accessibility]** Screen reader compatible.

---

## 9. Test Scenarios

### 9.1 Unit Tests

```gherkin
Scenario: Load working memory
  Given an agent with 10 working memory entries
  When inspector opens
  Then all 10 entries are displayed

Scenario: Edit entry requires Teams license
  Given user has WriterPro license
  When user clicks Edit on an entry
  Then upgrade dialog is shown
```

### 9.2 Integration Tests

```gherkin
Scenario: Full inspector workflow
  Given an agent with populated memory
  When user opens inspector
  And views each tab
  And searches for an entry
  Then all data loads correctly
```

---

## 10. Changelog

| Version | Date | Author | Changes |
|:--------|:-----|:-------|:--------|
| 1.0.0 | 2026-02-04 | Agent Architecture Lead | Initial specification |

---

**End of Specification**
