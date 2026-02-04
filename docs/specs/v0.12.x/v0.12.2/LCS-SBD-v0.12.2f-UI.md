# LDS-01: Lifecycle Dashboard UI

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Feature ID** | `LCM-UI-01` |
| **Feature Name** | Lifecycle Dashboard UI |
| **Target Version** | `v0.12.2f` |
| **Module Scope** | `Lexichord.UI.Agents` |
| **Swimlane** | Ensemble |
| **License Tier** | WriterPro |
| **Feature Gate Key** | `FeatureFlags.Agents.Lifecycle.Dashboard` |
| **Author** | Agent Architecture Lead |
| **Reviewer** | Lead Architect, UX Designer |
| **Status** | Draft |
| **Last Updated** | 2026-02-03 |
| **Parent Spec** | [LCS-SBD-v0.12.2-AGT](./LCS-SBD-v0.12.2-AGT.md) |
| **Depends On** | [v0.12.2a (Spawner)](./LCS-SBD-v0.12.2a-SPW.md), [v0.12.2b (Monitor)](./LCS-SBD-v0.12.2b-MON.md), [v0.12.2d (Shutdown)](./LCS-SBD-v0.12.2d-SHD.md) |
| **Estimated Hours** | 8 |

---

## 2. Executive Summary

### 2.1 The Requirement

Operators need visibility into running agents to manage system health, diagnose issues, and control agent lifecycle. Without a dashboard, users must rely on logs or API calls to understand agent status. The system needs a real-time visual interface showing agent states, health, metrics, and providing manual controls for lifecycle operations.

### 2.2 The Proposed Solution

Implement an AvaloniaUI-based Lifecycle Dashboard providing:
- Real-time list of active agent instances with state and health indicators
- Filtering and sorting by state, health, definition, tags
- Detail panel with metrics charts, health check results, event timeline
- Manual controls: Spawn, Suspend, Resume, Terminate
- Alert panel showing active health alerts
- Resource usage visualization against limits
- License limit indicator

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Modules.Agents` — `IAgentLifecycleManager` (v0.12.2a)
- `Lexichord.Modules.Agents` — `IAgentMonitor` (v0.12.2b)
- `Lexichord.Modules.Agents` — `IShutdownCoordinator` (v0.12.2d)
- `Lexichord.Modules.Agents` — `IAgentRegistry` (v0.12.1d)
- `Lexichord.UI.Core` — MVVM infrastructure, controls

**NuGet Packages:**
- `Avalonia` (UI framework)
- `Avalonia.ReactiveUI` (MVVM)
- `LiveChartsCore.SkiaSharpView.Avalonia` (charts)

### 3.2 Licensing Behavior

- **Load Behavior:** [x] **UI Gate** — Dashboard menu item hidden on Core tier.
- **Fallback Experience:**
  - Core: Dashboard hidden, basic spawn/terminate via context menu
  - WriterPro: Full dashboard access
  - Teams: Dashboard + workspace-wide views
  - Enterprise: Dashboard + cross-workspace views + export

---

## 4. Data Contract (ViewModels)

### 4.1 Lifecycle Dashboard ViewModel

```csharp
namespace Lexichord.UI.Agents.ViewModels;

/// <summary>
/// Main ViewModel for the Lifecycle Dashboard.
/// </summary>
public partial class LifecycleDashboardViewModel : ViewModelBase
{
    private readonly IAgentLifecycleManager _lifecycleManager;
    private readonly IAgentMonitor _monitor;
    private readonly IAgentRegistry _registry;
    private readonly ILogger<LifecycleDashboardViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<AgentInstanceViewModel> _instances = [];

    [ObservableProperty]
    private AgentInstanceViewModel? _selectedInstance;

    [ObservableProperty]
    private AgentDetailViewModel? _detailPanel;

    [ObservableProperty]
    private LifecycleStatistics? _statistics;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private AgentState? _filterState;

    [ObservableProperty]
    private HealthState? _filterHealth;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ObservableCollection<HealthAlertViewModel> _activeAlerts = [];

    /// <summary>
    /// Refresh rate for data updates in milliseconds.
    /// </summary>
    public int RefreshRateMs { get; set; } = 5000;

    /// <summary>
    /// Concurrent instances used / limit from license.
    /// </summary>
    public string ConcurrentDisplay =>
        Statistics is null
            ? "Loading..."
            : $"{Statistics.ActiveInstances}/{Statistics.MaxConcurrent}";

    /// <summary>
    /// Percentage of concurrent limit used.
    /// </summary>
    public double ConcurrentPercentage =>
        Statistics?.MaxConcurrent > 0
            ? (double)Statistics.ActiveInstances / Statistics.MaxConcurrent * 100
            : 0;

    public LifecycleDashboardViewModel(
        IAgentLifecycleManager lifecycleManager,
        IAgentMonitor monitor,
        IAgentRegistry registry,
        ILogger<LifecycleDashboardViewModel> logger)
    {
        _lifecycleManager = lifecycleManager;
        _monitor = monitor;
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dashboard and starts data refresh.
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogDebug("[UI] Initializing Lifecycle Dashboard");
        await RefreshAsync();
        StartAutoRefresh();
    }

    /// <summary>
    /// Refreshes all dashboard data.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogDebug("[UI] Refreshing dashboard data");

            var filter = BuildFilter();
            var instances = await _lifecycleManager.GetActiveInstancesAsync(filter);
            var statistics = await _lifecycleManager.GetStatisticsAsync();
            var alerts = await _monitor.GetAllAlertsAsync(new AlertFilter { ActiveOnly = true });

            Instances = new ObservableCollection<AgentInstanceViewModel>(
                instances.Select(i => new AgentInstanceViewModel(i, _registry)));

            Statistics = statistics;

            ActiveAlerts = new ObservableCollection<HealthAlertViewModel>(
                alerts.Select(a => new HealthAlertViewModel(a)));

            _logger.LogDebug(
                "[UI] Dashboard refreshed: {Count} instances, {Alerts} alerts",
                Instances.Count, ActiveAlerts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Dashboard refresh failed");
            ErrorMessage = $"Failed to refresh: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Spawns a new agent instance.
    /// </summary>
    [RelayCommand]
    public async Task SpawnAgentAsync(Guid definitionId)
    {
        try
        {
            _logger.LogInfo("[UI] User spawning agent: {DefinitionId}", definitionId);

            var request = new SpawnRequest { AgentDefinitionId = definitionId };
            var instance = await _lifecycleManager.SpawnAsync(request);

            await RefreshAsync();

            // Select the new instance
            SelectedInstance = Instances.FirstOrDefault(i => i.InstanceId == instance.InstanceId);
        }
        catch (LicenseException ex)
        {
            _logger.LogWarn("[UI] Spawn rejected: {Reason}", ex.Message);
            ErrorMessage = $"Cannot spawn: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Spawn failed");
            ErrorMessage = $"Spawn failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Terminates the selected instance.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanTerminate))]
    public async Task TerminateSelectedAsync()
    {
        if (SelectedInstance is null) return;

        try
        {
            _logger.LogInfo("[UI] User terminating agent: {InstanceId}", SelectedInstance.InstanceId);

            await _lifecycleManager.TerminateAsync(
                SelectedInstance.InstanceId,
                TerminationOptions.Default);

            await RefreshAsync();
            SelectedInstance = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Terminate failed");
            ErrorMessage = $"Terminate failed: {ex.Message}";
        }
    }

    private bool CanTerminate() =>
        SelectedInstance?.IsActive == true;

    /// <summary>
    /// Suspends the selected instance.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSuspend))]
    public async Task SuspendSelectedAsync()
    {
        if (SelectedInstance is null) return;

        try
        {
            _logger.LogInfo("[UI] User suspending agent: {InstanceId}", SelectedInstance.InstanceId);
            await _lifecycleManager.SuspendAsync(SelectedInstance.InstanceId);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Suspend failed");
            ErrorMessage = $"Suspend failed: {ex.Message}";
        }
    }

    private bool CanSuspend() =>
        SelectedInstance?.State is AgentState.Ready or AgentState.Waiting;

    /// <summary>
    /// Resumes the selected instance.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResume))]
    public async Task ResumeSelectedAsync()
    {
        if (SelectedInstance is null) return;

        try
        {
            _logger.LogInfo("[UI] User resuming agent: {InstanceId}", SelectedInstance.InstanceId);
            await _lifecycleManager.ResumeAsync(SelectedInstance.InstanceId);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UI] Resume failed");
            ErrorMessage = $"Resume failed: {ex.Message}";
        }
    }

    private bool CanResume() =>
        SelectedInstance?.State is AgentState.Suspended;

    /// <summary>
    /// Shows details for the selected instance.
    /// </summary>
    partial void OnSelectedInstanceChanged(AgentInstanceViewModel? value)
    {
        if (value is null)
        {
            DetailPanel = null;
            return;
        }

        DetailPanel = new AgentDetailViewModel(
            value.InstanceId,
            _lifecycleManager,
            _monitor,
            _registry);

        _ = DetailPanel.LoadAsync();
    }

    private GetInstancesFilter BuildFilter() =>
        new()
        {
            State = FilterState,
            HealthState = FilterHealth,
            NamePattern = string.IsNullOrWhiteSpace(FilterText) ? null : $"*{FilterText}*"
        };

    private void StartAutoRefresh()
    {
        Observable.Interval(TimeSpan.FromMilliseconds(RefreshRateMs))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await RefreshAsync());
    }
}
```

### 4.2 Agent Instance ViewModel

```csharp
namespace Lexichord.UI.Agents.ViewModels;

/// <summary>
/// ViewModel for a single agent instance row in the list.
/// </summary>
public partial class AgentInstanceViewModel : ViewModelBase
{
    private readonly AgentInstance _instance;
    private readonly IAgentRegistry _registry;

    public Guid InstanceId => _instance.InstanceId;
    public Guid DefinitionId => _instance.DefinitionId;
    public AgentState State => _instance.State;
    public HealthState Health => _instance.Health.State;
    public string? Name => _instance.Name;
    public int RestartCount => _instance.RestartCount;
    public DateTimeOffset CreatedAt => _instance.CreatedAt;
    public TimeSpan Uptime => _instance.Uptime;
    public bool IsActive => _instance.IsActive;

    /// <summary>
    /// Display name combining user name and definition name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Icon for the current state.
    /// </summary>
    public string StateIcon => State switch
    {
        AgentState.Initializing => "⏳",
        AgentState.Ready => "✓",
        AgentState.Processing => "●",
        AgentState.Waiting => "◐",
        AgentState.Suspended => "⏸",
        AgentState.Terminating => "⏹",
        AgentState.Terminated => "✗",
        AgentState.Failed => "⚠",
        _ => "?"
    };

    /// <summary>
    /// Color for the state indicator.
    /// </summary>
    public string StateColor => State switch
    {
        AgentState.Ready or AgentState.Waiting => "Green",
        AgentState.Processing => "Blue",
        AgentState.Initializing or AgentState.Terminating => "Orange",
        AgentState.Suspended => "Gray",
        AgentState.Failed => "Red",
        AgentState.Terminated => "DarkGray",
        _ => "Gray"
    };

    /// <summary>
    /// Icon for the health state.
    /// </summary>
    public string HealthIcon => Health switch
    {
        HealthState.Healthy => "✓",
        HealthState.Degraded => "⚠",
        HealthState.Unhealthy => "✗",
        HealthState.Unknown => "?",
        _ => "?"
    };

    /// <summary>
    /// Color for the health indicator.
    /// </summary>
    public string HealthColor => Health switch
    {
        HealthState.Healthy => "Green",
        HealthState.Degraded => "Orange",
        HealthState.Unhealthy => "Red",
        HealthState.Unknown => "Gray",
        _ => "Gray"
    };

    /// <summary>
    /// Memory usage display.
    /// </summary>
    public string MemoryDisplay =>
        _instance.Metrics?.MemoryMb.ToString() + " MB" ?? "N/A";

    public AgentInstanceViewModel(AgentInstance instance, IAgentRegistry registry)
    {
        _instance = instance;
        _registry = registry;

        // Build display name
        DisplayName = Name ?? $"Instance {InstanceId.ToString()[..8]}";
    }
}
```

### 4.3 Agent Detail ViewModel

```csharp
namespace Lexichord.UI.Agents.ViewModels;

/// <summary>
/// ViewModel for the agent detail panel.
/// </summary>
public partial class AgentDetailViewModel : ViewModelBase
{
    private readonly Guid _instanceId;
    private readonly IAgentLifecycleManager _lifecycleManager;
    private readonly IAgentMonitor _monitor;
    private readonly IAgentRegistry _registry;

    [ObservableProperty]
    private AgentInstance? _instance;

    [ObservableProperty]
    private AgentManifest? _manifest;

    [ObservableProperty]
    private AgentMetrics? _currentMetrics;

    [ObservableProperty]
    private ObservableCollection<HealthCheckResultViewModel> _healthChecks = [];

    [ObservableProperty]
    private ObservableCollection<AgentEventViewModel> _recentEvents = [];

    [ObservableProperty]
    private ObservableCollection<ISeries> _memoryChartSeries = [];

    [ObservableProperty]
    private ObservableCollection<ISeries> _cpuChartSeries = [];

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Resource limit usage percentage.
    /// </summary>
    public double MemoryUsagePercent =>
        Instance?.SpawnOptions?.ResourceLimits?.MaxMemoryMb > 0 && CurrentMetrics is not null
            ? (double)CurrentMetrics.MemoryMb / Instance.SpawnOptions.ResourceLimits.MaxMemoryMb * 100
            : 0;

    public AgentDetailViewModel(
        Guid instanceId,
        IAgentLifecycleManager lifecycleManager,
        IAgentMonitor monitor,
        IAgentRegistry registry)
    {
        _instanceId = instanceId;
        _lifecycleManager = lifecycleManager;
        _monitor = monitor;
        _registry = registry;
    }

    /// <summary>
    /// Loads detail data for the instance.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            Instance = await _lifecycleManager.GetInstanceAsync(_instanceId);
            if (Instance is null) return;

            Manifest = await _registry.GetManifestAsync(Instance.DefinitionId.ToString());
            CurrentMetrics = await _monitor.GetMetricsAsync(_instanceId);

            // Load health check results
            var healthStatus = await _monitor.CheckHealthAsync(_instanceId);
            if (healthStatus.ProbeResults is not null)
            {
                HealthChecks = new ObservableCollection<HealthCheckResultViewModel>(
                    healthStatus.ProbeResults.Select(r => new HealthCheckResultViewModel(r)));
            }

            // Load metrics history for charts
            var history = await _monitor.GetMetricsHistoryAsync(
                _instanceId,
                DateTimeOffset.UtcNow.AddMinutes(-30));

            BuildCharts(history);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildCharts(IReadOnlyList<AgentMetrics> history)
    {
        var memoryValues = history.Select(m => new ObservablePoint(
            m.CollectedAt.ToUnixTimeSeconds(),
            m.MemoryMb)).ToList();

        var cpuValues = history.Select(m => new ObservablePoint(
            m.CollectedAt.ToUnixTimeSeconds(),
            m.CpuPercent)).ToList();

        MemoryChartSeries =
        [
            new LineSeries<ObservablePoint>
            {
                Values = memoryValues,
                Name = "Memory (MB)",
                Fill = null
            }
        ];

        CpuChartSeries =
        [
            new LineSeries<ObservablePoint>
            {
                Values = cpuValues,
                Name = "CPU (%)",
                Fill = null
            }
        ];
    }
}
```

### 4.4 Health Alert ViewModel

```csharp
namespace Lexichord.UI.Agents.ViewModels;

/// <summary>
/// ViewModel for a health alert.
/// </summary>
public class HealthAlertViewModel : ViewModelBase
{
    private readonly HealthAlert _alert;

    public Guid AlertId => _alert.AlertId;
    public Guid InstanceId => _alert.AgentInstanceId;
    public HealthAlertLevel Level => _alert.Level;
    public HealthAlertType Type => _alert.Type;
    public string Message => _alert.Message;
    public DateTimeOffset RaisedAt => _alert.RaisedAt;
    public TimeSpan Duration => _alert.Duration;
    public bool IsActive => _alert.IsActive;

    /// <summary>
    /// Icon for the alert level.
    /// </summary>
    public string LevelIcon => Level switch
    {
        HealthAlertLevel.Info => "ℹ️",
        HealthAlertLevel.Warning => "⚠️",
        HealthAlertLevel.Error => "❌",
        HealthAlertLevel.Critical => "🔴",
        _ => "❓"
    };

    /// <summary>
    /// Background color for the alert.
    /// </summary>
    public string BackgroundColor => Level switch
    {
        HealthAlertLevel.Info => "#E3F2FD",
        HealthAlertLevel.Warning => "#FFF3E0",
        HealthAlertLevel.Error => "#FFEBEE",
        HealthAlertLevel.Critical => "#F8D7DA",
        _ => "#F5F5F5"
    };

    public HealthAlertViewModel(HealthAlert alert)
    {
        _alert = alert;
    }
}
```

---

## 5. UI/UX Specifications

### 5.1 Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Agent Lifecycle Dashboard                    [↻ Refresh] [+ Spawn]     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ Active: 7/20 (Teams)  ████████░░░░░░░░ 35%  │ Alerts: 2 ⚠️ 1 ❌         │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│ Filter: [________] State: [All ▼] Health: [All ▼]  [Clear Filters]     │
│                                                                         │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ Name             │ Definition   │ State      │ Health │ Memory │ ↑  │ │
│ ├─────────────────────────────────────────────────────────────────────┤ │
│ │ ● PR Review #42  │ CodeAnalyzer │ Processing │ ✓ OK   │ 384MB  │    │ │
│ │ ✓ Data Pipeline  │ DataETL      │ Ready      │ ✓ OK   │ 256MB  │    │ │
│ │ ◐ Code Review    │ CodeAnalyzer │ Waiting    │ ⚠ Deg  │ 512MB  │ ←  │ │
│ │ ⏸ Batch Proc     │ DataETL      │ Suspended  │ ? Unk  │ 128MB  │    │ │
│ │ ⚠ Validator      │ Validator    │ Failed     │ ✗ Bad  │ 0MB    │    │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│ [Suspend] [Resume] [Terminate] [View Details]                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Detail Panel Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│ Agent Details: Code Review (CodeAnalyzer)                    [✕ Close] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ Instance: agt-003-def-123     State: ◐ Waiting    Health: ⚠ Degraded   │
│ Created: 2026-02-03 10:15:22  Uptime: 4h 23m      Restarts: 2          │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│ RESOURCE USAGE                           │ METRICS                      │
│                                          │                              │
│ Memory: ████████░░ 384/512 MB (75%)      │ Operations: 1,247 completed  │
│ CPU:    ████░░░░░░ 45% (no limit)        │ Errors: 23 (1.8% rate)       │
│ Workers: 2/4 active                      │ Avg latency: 123ms           │
│ Queue: 3 pending                         │ P95 latency: 845ms           │
│                                          │ Tokens/min: 1,523            │
├─────────────────────────────────────────────────────────────────────────┤
│ MEMORY USAGE (30 min)                    │ CPU USAGE (30 min)           │
│ ┌───────────────────────────┐            │ ┌───────────────────────────┐│
│ │     ╱╲                    │            │ │  ╱╲    ╱╲                 ││
│ │    ╱  ╲   ╱╲              │            │ │ ╱  ╲  ╱  ╲   ╱╲          ││
│ │   ╱    ╲ ╱  ╲────         │            │ │╱    ╲╱    ╲ ╱  ╲────     ││
│ │  ╱      ╲                 │            │ │                          ││
│ │ ╱                         │            │ │                          ││
│ └───────────────────────────┘            │ └───────────────────────────┘│
├─────────────────────────────────────────────────────────────────────────┤
│ HEALTH CHECKS                                                           │
│ ✓ heartbeat   PASS   2ms    Agent responding normally                  │
│ ⚠ http        FAIL   3.2s   Timeout (3 consecutive failures)           │
│ ✓ tcp:8080    PASS   15ms   Connection established                     │
├─────────────────────────────────────────────────────────────────────────┤
│ RECENT EVENTS                                                           │
│ 14:38:41  State: Waiting → Processing                                  │
│ 14:38:15  Health: Healthy → Degraded                                   │
│ 14:37:52  Operation completed (95ms)                                   │
│ 14:37:44  Operation started                                            │
│ 14:35:00  Restart #2 completed                                         │
│                                                                         │
│ [Suspend] [Resume] [Terminate] [Export Logs]                           │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Visual Components

| Component | Location | Description |
|:----------|:---------|:------------|
| `LifecycleDashboardView` | Main panel | Full dashboard view |
| `AgentInstanceListView` | Dashboard center | Scrollable list of instances |
| `AgentInstanceRow` | List item | Single instance row with indicators |
| `AgentDetailPanel` | Side panel / modal | Instance details and metrics |
| `HealthAlertBanner` | Dashboard top | Active alerts summary |
| `ResourceGauge` | Detail panel | Visual gauge for resource usage |
| `MetricsChart` | Detail panel | Line charts for metrics history |
| `HealthCheckList` | Detail panel | Health probe results |
| `EventTimeline` | Detail panel | Recent events chronologically |
| `SpawnDialog` | Modal | Agent spawn configuration dialog |

### 5.4 State Management

```csharp
/// <summary>
/// State transitions and their UI representation.
/// </summary>
public static class StateDisplay
{
    public static (string Icon, string Color, string Tooltip) GetDisplay(AgentState state) =>
        state switch
        {
            AgentState.Initializing => ("⏳", "#FFA500", "Starting up..."),
            AgentState.Ready => ("✓", "#22C55E", "Ready for work"),
            AgentState.Processing => ("●", "#3B82F6", "Processing request"),
            AgentState.Waiting => ("◐", "#22C55E", "Idle, waiting"),
            AgentState.Suspended => ("⏸", "#6B7280", "Suspended by user"),
            AgentState.Terminating => ("⏹", "#F59E0B", "Shutting down..."),
            AgentState.Terminated => ("✗", "#9CA3AF", "Terminated"),
            AgentState.Failed => ("⚠", "#EF4444", "Failed - see details"),
            _ => ("?", "#6B7280", "Unknown state")
        };
}
```

### 5.5 Accessibility (A11y)

| Requirement | Implementation |
|:------------|:---------------|
| Keyboard navigation | Full tab order, arrow key navigation in list |
| Screen reader | `AutomationProperties.Name` on all interactive elements |
| Color contrast | WCAG 2.1 AA compliant colors |
| State icons | Text alternatives for all icons |
| Focus indicators | Visible focus rings on all controls |
| Error announcements | Live region for error messages |

---

## 6. Observability & Logging

### 6.1 Log Templates

| Level | Template |
|:------|:---------|
| **Debug** | `[UI] Dashboard loaded with {Count} instances` |
| **Debug** | `[UI] Filter applied: state={State}, health={Health}` |
| **Info** | `[UI] User action: {Action} on instance {InstanceId}` |
| **Warn** | `[UI] Dashboard refresh failed: {Error}` |
| **Error** | `[UI] Action failed: {Action} - {Error}` |

### 6.2 Telemetry

| Event | Properties |
|:------|:-----------|
| `dashboard.opened` | `userId`, `instanceCount`, `tier` |
| `dashboard.action` | `action`, `instanceId`, `result` |
| `dashboard.filter` | `filterType`, `filterValue` |
| `dashboard.detail.viewed` | `instanceId`, `definitionId` |

---

## 7. Security & Safety

- **Authorization:** All actions require appropriate permissions.
- **Confirmation:** Terminate action requires confirmation dialog.
- **Audit:** All user actions logged.
- **Data Scoping:** Users see only their own instances (unless Teams/Enterprise).

---

## 8. Acceptance Criteria (QA)

1. **[Functional]** Dashboard displays all active instances.
2. **[Functional]** Filters narrow results correctly.
3. **[Functional]** Spawn/Suspend/Resume/Terminate work from UI.
4. **[Functional]** Detail panel shows correct metrics and health.
5. **[Functional]** Charts update with historical data.
6. **[Performance]** Dashboard loads in <500ms.
7. **[Performance]** Auto-refresh doesn't cause UI lag.
8. **[Accessibility]** Full keyboard navigation works.
9. **[Licensing]** Dashboard hidden on Core tier.

---

## 9. Test Scenarios

### 9.1 Unit Tests

**Scenario: `FilterByState_ReturnsMatchingInstances`**
- **Setup:** 10 instances in various states.
- **Action:** Set FilterState = Processing.
- **Assertion:** Only Processing instances displayed.

**Scenario: `TerminateCommand_RequiresActiveInstance`**
- **Setup:** Selected instance is Terminated.
- **Action:** Check CanTerminate.
- **Assertion:** Returns false.

### 9.2 Integration Tests

**Scenario: `SpawnFromUI_CreatesInstance`**
- **Setup:** Dashboard open, definition available.
- **Action:** Click Spawn, select definition, confirm.
- **Assertion:** New instance appears in list.

**Scenario: `DetailPanel_LoadsMetrics`**
- **Setup:** Running instance with metrics.
- **Action:** Select instance.
- **Assertion:** Detail panel shows correct metrics and charts.

---

## 10. Changelog

| Date | Author | Changes |
|:-----|:-------|:--------|
| 2026-02-03 | Agent Architecture Lead | Initial specification draft |
