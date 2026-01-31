# LCS-DES-v0.10.1-KG-f: Design Specification — Version History UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-101-f` | Knowledge Graph Versioning sub-part f |
| **Feature Name** | `Version History UI` | Visualize changes and manage versions |
| **Target Version** | `v0.10.1f` | Sixth sub-part of v0.10.1-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Knowledge Graph Versioning module |
| **Swimlane** | `Graph Versioning` | Versioning vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVersioning` | Graph versioning feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.1-KG](./LCS-SBD-v0.10.1-KG.md) | Knowledge Graph Versioning scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.1-KG S2.1](./LCS-SBD-v0.10.1-KG.md#21-sub-parts) | f = Version History UI |
| **Estimated Hours** | 6h | Development and testing effort |

---

## 2. Executive Summary

### 2.1 The Requirement

The Version History UI provides users with visual access to graph version history, change details, and restoration capabilities. It must:

1. Display chronological timeline of graph versions
2. Show change summaries (entities added/modified/deleted)
3. Visualize diffs between versions
4. Enable restore/rollback operations
5. Support search and filtering of history
6. Display branch status and switching
7. Provide snapshot management interface
8. Download historical snapshots

### 2.2 The Proposed Solution

A comprehensive history UI consisting of:

1. **Version Timeline:** Chronological list of versions with metadata
2. **Change Details:** Expandable change summaries and diffs
3. **Diff Viewer:** Side-by-side before/after comparison
4. **Restore Controls:** Quick restore/rollback buttons
5. **Branch Panel:** Branch listing and switching
6. **Search & Filter:** Date range, user, change type filtering
7. **Snapshot Manager:** Create, tag, and download snapshots

**Key Design Principles:**
- Clean, intuitive timeline visualization
- Progressive disclosure of details
- Non-destructive browsing (no changes until confirmed)
- Responsive design (mobile-friendly)
- Performance optimized pagination/virtualization

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphVersionService` | v0.10.1a | Fetch version history and snapshots |
| `IGraphBranchService` | v0.10.1e | List branches, switch, merge |
| `IChangeTracker` | v0.10.1b | Get change details and diffs |
| `IMediator` | v0.0.7a | Event publishing |

#### 3.1.2 Frontend Stack

| Package | Purpose |
| :--- | :--- |
| React | UI component framework |
| TypeScript | Type-safe UI logic |
| TailwindCSS | Styling |
| React Query | Server state management |
| Zustand | Client state management |
| Vitest | Unit testing |
| React Testing Library | Component testing |

### 3.2 Licensing Behavior

- **Teams Tier:** Full history UI (with 1-year retention limit message)
- **Enterprise Tier:** Full UI with unlimited retention features

### 3.3 UI Component Hierarchy

```
VersionHistoryPanel (root)
├── HistoryTimeline
│   ├── TimelineItem (per version)
│   │   ├── VersionBadge
│   │   ├── VersionSummary
│   │   │   ├── ChangeStats
│   │   │   └── Metadata (timestamp, user, message)
│   │   └── VersionActions
│   │       ├── ViewChanges button
│   │       ├── Compare button
│   │       ├── Restore button
│   │       └── Download button
│   └── TimelineFilter
│       ├── DateRangePicker
│       ├── UserFilter
│       ├── ChangeTypeFilter
│       └── SearchInput
│
├── ChangeDetailsPanel
│   ├── ChangeListView
│   │   └── ChangeItem (per change)
│   │       ├── ElementIcon
│   │       ├── ChangeType badge
│   │       └── ElementLabel
│   └── DiffViewer
│       ├── BeforeView
│       └── AfterView
│
├── BranchPanel
│   ├── BranchList
│   │   └── BranchItem
│   │       ├── BranchName
│   │       ├── Ahead/Behind counts
│   │       └── Actions (switch, delete, merge)
│   └── BranchActions
│       ├── CreateBranch
│       └── MergeBranch
│
└── SnapshotPanel
    ├── SnapshotList
    │   └── SnapshotItem
    │       ├── SnapshotName
    │       ├── Tag
    │       └── Actions (restore, delete, download)
    └── CreateSnapshot
```

---

## 4. Data Contract (The API)

### 4.1 API Endpoints (Backend)

```csharp
namespace Lexichord.Modules.CKVS.Api.Endpoints;

/// <summary>
/// Endpoints for version history UI.
/// </summary>
public static class VersionHistoryEndpoints
{
    /// <summary>
    /// GET /api/versions - Get paginated version history.
    /// </summary>
    [HttpGet("/api/versions")]
    public async Task<IResult> GetVersionHistory(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? branchName = null,
        [FromQuery] string? search = null,
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var query = new HistoryQuery
        {
            Skip = skip,
            Take = take,
            StartDate = startDate,
            EndDate = endDate,
            BranchName = branchName,
            SearchText = search
        };

        var history = await versionService.GetHistoryAsync(query, ct);
        return Results.Ok(new { items = history, total = history.Count });
    }

    /// <summary>
    /// GET /api/versions/{versionId} - Get specific version details.
    /// </summary>
    [HttpGet("/api/versions/{versionId:guid}")]
    public async Task<IResult> GetVersionDetails(
        Guid versionId,
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var version = await versionService.GetVersionAsync(versionId, ct);
        if (version == null)
            return Results.NotFound();

        var changes = await versionService.GetHistoryAsync(
            new HistoryQuery { VersionId = versionId }, ct);

        return Results.Ok(new
        {
            version = version,
            changes = changes
        });
    }

    /// <summary>
    /// GET /api/versions/{versionId}/diff - Get diff between versions.
    /// </summary>
    [HttpGet("/api/versions/{versionId:guid}/diff")]
    public async Task<IResult> GetVersionDiff(
        Guid versionId,
        [FromQuery] Guid? compareWithVersionId = null,
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var versionA = await versionService.GetVersionAsync(versionId, ct);
        if (versionA == null)
            return Results.NotFound("Version not found");

        var compareVersion = compareWithVersionId.HasValue
            ? versionId
            : (await versionService.GetVersionAsync(compareWithVersionId.Value, ct)
                ?? versionA.ParentVersionId.HasValue
                    ? await versionService.GetVersionAsync(versionA.ParentVersionId.Value, ct)
                    : null);

        if (compareVersion == null)
            return Results.NotFound("Comparison version not found");

        // Get diffs
        var diff = await versionService.ComputeDiffAsync(versionId, compareVersion.VersionId, ct);

        return Results.Ok(diff);
    }

    /// <summary>
    /// POST /api/versions/{versionId}/restore - Restore graph to version.
    /// </summary>
    [HttpPost("/api/versions/{versionId:guid}/restore")]
    public async Task<IResult> RestoreVersion(
        Guid versionId,
        [FromBody] RestoreOptions options,
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var result = await versionService.RollbackAsync(
            GraphVersionRef.FromVersion(versionId),
            new RollbackOptions
            {
                CreatedBy = options.RestoredBy,
                Message = options.Message
            },
            ct);

        if (!result.Success)
            return Results.BadRequest(result.Error);

        return Results.Ok(new { rollbackVersionId = result.RollbackVersionId });
    }

    /// <summary>
    /// GET /api/snapshots - List snapshots.
    /// </summary>
    [HttpGet("/api/snapshots")]
    public async Task<IResult> GetSnapshots(
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var snapshots = await versionService.GetSnapshotsAsync(ct);
        return Results.Ok(new { items = snapshots });
    }

    /// <summary>
    /// POST /api/snapshots - Create snapshot.
    /// </summary>
    [HttpPost("/api/snapshots")]
    public async Task<IResult> CreateSnapshot(
        [FromBody] CreateSnapshotRequest request,
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var snapshot = await versionService.CreateSnapshotAsync(
            request.Name,
            request.Description,
            ct);

        return Results.Created($"/api/snapshots/{snapshot.SnapshotId}", snapshot);
    }

    /// <summary>
    /// GET /api/snapshots/{snapshotId}/download - Download snapshot.
    /// </summary>
    [HttpGet("/api/snapshots/{snapshotId:guid}/download")]
    public async Task<IResult> DownloadSnapshot(
        Guid snapshotId,
        IGraphVersionService versionService,
        CancellationToken ct)
    {
        var snapshot = await versionService.GetSnapshotAsync(snapshotId, ct);
        if (snapshot == null)
            return Results.NotFound();

        var data = await versionService.ExportSnapshotAsync(snapshotId, ct);
        return Results.File(data, "application/octet-stream", $"snapshot-{snapshotId}.zip");
    }

    /// <summary>
    /// GET /api/branches - List all branches.
    /// </summary>
    [HttpGet("/api/branches")]
    public async Task<IResult> GetBranches(
        IGraphBranchService branchService,
        CancellationToken ct)
    {
        var branches = await branchService.GetBranchesAsync(ct);
        return Results.Ok(new { items = branches });
    }

    /// <summary>
    /// POST /api/branches/{branchName}/switch - Switch active branch.
    /// </summary>
    [HttpPost("/api/branches/{branchName}/switch")]
    public async Task<IResult> SwitchBranch(
        string branchName,
        IGraphBranchService branchService,
        CancellationToken ct)
    {
        try
        {
            await branchService.SwitchBranchAsync(branchName, ct);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// GET /api/branches/compare - Compare two branches.
    /// </summary>
    [HttpGet("/api/branches/compare")]
    public async Task<IResult> CompareBranches(
        [FromQuery] string fromBranch,
        [FromQuery] string toBranch,
        IGraphBranchService branchService,
        CancellationToken ct)
    {
        var comparison = await branchService.CompareBranchesAsync(fromBranch, toBranch, ct);
        return Results.Ok(comparison);
    }
}
```

### 4.2 Request/Response Models

```csharp
namespace Lexichord.Modules.CKVS.Api.Models;

public record RestoreOptions
{
    public string? Message { get; init; }
    public string? RestoredBy { get; init; }
}

public record CreateSnapshotRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Tag { get; init; }
}

public record HistoryQuery
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? BranchName { get; init; }
    public string? SearchText { get; init; }
    public Guid? VersionId { get; init; }
    public GraphChangeType? ChangeType { get; init; }
    public GraphElementType? ElementType { get; init; }
    public string? ChangedByUser { get; init; }
}

public record VersionDiff
{
    public Guid VersionIdA { get; init; }
    public Guid VersionIdB { get; init; }
    public IReadOnlyList<GraphChange> Changes { get; init; } = [];
    public GraphChangeStats Stats { get; init; } = new();
}
```

---

## 5. Frontend Components

### 5.1 VersionHistoryPanel Component

```typescript
// src/components/VersionHistoryPanel.tsx
import React, { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { HistoryTimeline } from './HistoryTimeline';
import { ChangeDetailsPanel } from './ChangeDetailsPanel';
import { BranchPanel } from './BranchPanel';
import { SnapshotPanel } from './SnapshotPanel';
import { TimelineFilter } from './TimelineFilter';

export interface VersionHistoryPanelProps {
  graphId: string;
}

export const VersionHistoryPanel: React.FC<VersionHistoryPanelProps> = ({ graphId }) => {
  const [startDate, setStartDate] = useState<Date | null>(null);
  const [endDate, setEndDate] = useState<Date | null>(null);
  const [branchName, setBranchName] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);
  const [compareWithVersionId, setCompareWithVersionId] = useState<string | null>(null);
  const [activePanel, setActivePanel] = useState<'timeline' | 'details' | 'branches' | 'snapshots'>('timeline');

  // Fetch version history
  const { data: historyData, isLoading } = useQuery({
    queryKey: ['versions', { startDate, endDate, branchName, searchText }],
    queryFn: async () => {
      const params = new URLSearchParams({
        ...(startDate && { startDate: startDate.toISOString() }),
        ...(endDate && { endDate: endDate.toISOString() }),
        ...(branchName && { branchName }),
        ...(searchText && { search: searchText })
      });
      const response = await fetch(`/api/versions?${params}`);
      return response.json();
    }
  });

  // Fetch selected version details
  const { data: versionDetails } = useQuery({
    queryKey: ['version', selectedVersionId],
    queryFn: async () => {
      if (!selectedVersionId) return null;
      const response = await fetch(`/api/versions/${selectedVersionId}`);
      return response.json();
    },
    enabled: !!selectedVersionId
  });

  return (
    <div className="flex h-full gap-4 p-4">
      {/* Left sidebar - Timeline and filters */}
      <div className="w-1/3 flex flex-col gap-4 border-r">
        <TimelineFilter
          onDateRangeChange={(start, end) => {
            setStartDate(start);
            setEndDate(end);
          }}
          onBranchChange={setBranchName}
          onSearchChange={setSearchText}
        />

        {isLoading && <div className="text-center text-gray-500">Loading history...</div>}

        <HistoryTimeline
          versions={historyData?.items || []}
          selectedVersionId={selectedVersionId}
          onVersionSelect={setSelectedVersionId}
          onCompareClick={setCompareWithVersionId}
        />
      </div>

      {/* Right panel - Details, branches, or snapshots */}
      <div className="flex-1 flex flex-col">
        {/* Panel selector */}
        <div className="flex gap-2 border-b pb-2 mb-4">
          <button
            onClick={() => setActivePanel('timeline')}
            className={`px-4 py-2 rounded ${activePanel === 'timeline' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            Timeline
          </button>
          <button
            onClick={() => setActivePanel('details')}
            className={`px-4 py-2 rounded ${activePanel === 'details' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            Details
          </button>
          <button
            onClick={() => setActivePanel('branches')}
            className={`px-4 py-2 rounded ${activePanel === 'branches' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            Branches
          </button>
          <button
            onClick={() => setActivePanel('snapshots')}
            className={`px-4 py-2 rounded ${activePanel === 'snapshots' ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
          >
            Snapshots
          </button>
        </div>

        {/* Panel content */}
        {activePanel === 'details' && (
          <ChangeDetailsPanel
            versionDetails={versionDetails}
            compareWithVersionId={compareWithVersionId}
            onCompareVersionChange={setCompareWithVersionId}
          />
        )}

        {activePanel === 'branches' && <BranchPanel graphId={graphId} />}

        {activePanel === 'snapshots' && <SnapshotPanel graphId={graphId} />}
      </div>
    </div>
  );
};
```

### 5.2 HistoryTimeline Component

```typescript
// src/components/HistoryTimeline.tsx
import React, { useMemo } from 'react';
import { formatDistance, parseISO } from 'date-fns';
import { GraphVersion } from '../types';

interface HistoryTimelineProps {
  versions: GraphVersion[];
  selectedVersionId?: string | null;
  onVersionSelect: (versionId: string) => void;
  onCompareClick: (versionId: string) => void;
}

export const HistoryTimeline: React.FC<HistoryTimelineProps> = ({
  versions,
  selectedVersionId,
  onVersionSelect,
  onCompareClick
}) => {
  const sortedVersions = useMemo(
    () => [...versions].sort((a, b) =>
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    ),
    [versions]
  );

  return (
    <div className="space-y-4 flex-1 overflow-y-auto">
      {sortedVersions.map((version) => (
        <TimelineItem
          key={version.versionId}
          version={version}
          isSelected={version.versionId === selectedVersionId}
          onSelect={() => onVersionSelect(version.versionId)}
          onCompare={() => onCompareClick(version.versionId)}
        />
      ))}
    </div>
  );
};

interface TimelineItemProps {
  version: GraphVersion;
  isSelected: boolean;
  onSelect: () => void;
  onCompare: () => void;
}

const TimelineItem: React.FC<TimelineItemProps> = ({ version, isSelected, onSelect, onCompare }) => {
  const relativeTime = formatDistance(parseISO(version.createdAt), new Date(), { addSuffix: true });

  return (
    <div
      onClick={onSelect}
      className={`p-3 rounded-lg border-l-4 cursor-pointer transition ${
        isSelected
          ? 'border-l-blue-500 bg-blue-50'
          : 'border-l-gray-300 bg-white hover:bg-gray-50'
      }`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-sm">V{version.versionId.substring(0, 8)}</span>
            <span className="text-xs text-gray-500">{relativeTime}</span>
          </div>

          {version.message && (
            <p className="text-sm text-gray-700 mt-1">{version.message}</p>
          )}

          <div className="flex gap-2 mt-2 text-xs text-gray-600">
            {version.stats.entitiesCreated > 0 && (
              <span>+{version.stats.entitiesCreated} entities</span>
            )}
            {version.stats.entitiesModified > 0 && (
              <span>~{version.stats.entitiesModified} modified</span>
            )}
            {version.stats.entitiesDeleted > 0 && (
              <span>-{version.stats.entitiesDeleted} deleted</span>
            )}
          </div>
        </div>

        <div className="flex gap-1 ml-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              onCompare();
            }}
            className="px-2 py-1 text-xs bg-gray-200 rounded hover:bg-gray-300"
          >
            Compare
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              // Trigger restore modal
            }}
            className="px-2 py-1 text-xs bg-green-200 rounded hover:bg-green-300"
          >
            Restore
          </button>
        </div>
      </div>
    </div>
  );
};
```

### 5.3 DiffViewer Component

```typescript
// src/components/DiffViewer.tsx
import React from 'react';
import { GraphChange } from '../types';

interface DiffViewerProps {
  changes: GraphChange[];
  showAddedOnly?: boolean;
  showDeletedOnly?: boolean;
}

export const DiffViewer: React.FC<DiffViewerProps> = ({
  changes,
  showAddedOnly = false,
  showDeletedOnly = false
}) => {
  const filtered = changes.filter(change => {
    if (showAddedOnly) return change.changeType === 'Create';
    if (showDeletedOnly) return change.changeType === 'Delete';
    return true;
  });

  return (
    <div className="space-y-4">
      {filtered.map((change) => (
        <div
          key={change.changeId}
          className={`p-3 rounded border-l-4 ${
            change.changeType === 'Create'
              ? 'border-l-green-500 bg-green-50'
              : change.changeType === 'Delete'
              ? 'border-l-red-500 bg-red-50'
              : 'border-l-blue-500 bg-blue-50'
          }`}
        >
          <div className="flex items-center gap-2 mb-2">
            <span className="px-2 py-1 text-xs rounded font-semibold" style={{
              backgroundColor: change.changeType === 'Create' ? '#86efac' :
                              change.changeType === 'Delete' ? '#fca5a5' : '#93c5fd'
            }}>
              {change.changeType}
            </span>
            <span className="text-xs text-gray-600">{change.elementType}</span>
            <span className="font-semibold text-sm">{change.elementLabel}</span>
          </div>

          {change.oldValue && change.newValue && (
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <h4 className="font-semibold text-gray-700 mb-1">Before</h4>
                <pre className="bg-white p-2 rounded text-xs overflow-auto">
                  {JSON.stringify(change.oldValue, null, 2)}
                </pre>
              </div>
              <div>
                <h4 className="font-semibold text-gray-700 mb-1">After</h4>
                <pre className="bg-white p-2 rounded text-xs overflow-auto">
                  {JSON.stringify(change.newValue, null, 2)}
                </pre>
              </div>
            </div>
          )}
        </div>
      ))}
    </div>
  );
};
```

---

## 6. UI States & Interactions

### 6.1 Timeline States

```
Loading:        Spinner + "Loading history..."
Empty:          "No versions found"
Normal:         List of version items
With selection: Highlighted item, details panel populated
Comparison:     Two versions highlighted, diff view shown
```

### 6.2 User Actions

| Action | Trigger | Effect |
| :--- | :--- | :--- |
| View version | Click version item | Load version details, populate diff panel |
| View diff | Click "Compare" on item | Load diff with previous version |
| Restore | Click "Restore" on item | Show confirmation modal, then restore |
| Create snapshot | Click "Create Snapshot" | Show modal for name/description |
| Download snapshot | Click snapshot download | Download snapshot as ZIP |
| Switch branch | Click branch item | Switch active branch, reload history |
| Merge branch | Click "Merge" button | Show merge dialog with conflict preview |
| Search | Type in search box | Debounce, query with search text |
| Filter date | Pick date range | Update query with date bounds |

---

## 7. Error Handling

### 7.1 Version Not Found

**Scenario:** User selects version that was deleted.

**Handling:**
- Clear selection
- Show error toast: "Version not found"
- Reload history

**Code:**
```typescript
const { data, error } = useQuery({
  queryFn: async () => {
    const response = await fetch(`/api/versions/${versionId}`);
    if (response.status === 404) {
      throw new Error('Version not found');
    }
    return response.json();
  },
  onError: (error) => {
    toast.error('Version not found');
    setSelectedVersionId(null);
  }
});
```

### 7.2 Restore Permission Denied

**Scenario:** User lacks restore permission.

**Handling:**
- Disable restore button
- Show tooltip: "Insufficient permissions"
- Log attempt in audit trail

**Code:**
```typescript
const RestoreButton = ({ versionId, userPermissions }: RestoreButtonProps) => {
  const canRestore = userPermissions.includes('VersionRollback');

  if (!canRestore) {
    return (
      <button disabled title="Insufficient permissions">
        Restore
      </button>
    );
  }

  return <button onClick={() => restoreVersion(versionId)}>Restore</button>;
};
```

### 7.3 Merge Conflicts Detected

**Scenario:** User initiates merge with unresolved conflicts.

**Handling:**
- Show conflict resolution UI
- List conflicts clearly
- Provide resolution options (take source/target/custom)

---

## 8. Testing

### 8.1 Component Tests

```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import { HistoryTimeline } from './HistoryTimeline';

describe('HistoryTimeline', () => {
  it('renders version items', () => {
    const versions = [
      {
        versionId: '123',
        createdAt: new Date().toISOString(),
        message: 'Test version',
        stats: { entitiesCreated: 1, entitiesModified: 0, entitiesDeleted: 0 }
      }
    ];

    render(
      <HistoryTimeline
        versions={versions}
        onVersionSelect={jest.fn()}
        onCompareClick={jest.fn()}
      />
    );

    expect(screen.getByText('Test version')).toBeInTheDocument();
    expect(screen.getByText(/\+1 entities/)).toBeInTheDocument();
  });

  it('calls onVersionSelect when item clicked', () => {
    const onSelect = jest.fn();
    const versions = [
      {
        versionId: '123',
        createdAt: new Date().toISOString(),
        message: 'Test version',
        stats: { entitiesCreated: 0, entitiesModified: 0, entitiesDeleted: 0 }
      }
    ];

    render(
      <HistoryTimeline
        versions={versions}
        onVersionSelect={onSelect}
        onCompareClick={jest.fn()}
      />
    );

    fireEvent.click(screen.getByText('Test version'));
    expect(onSelect).toHaveBeenCalledWith('123');
  });
});
```

### 8.2 Integration Tests

```typescript
describe('Version History Panel Integration', () => {
  it('loads history, selects version, and shows details', async () => {
    render(<VersionHistoryPanel graphId="test-graph" />);

    // Wait for history to load
    await screen.findByText(/V[a-f0-9]+/);

    // Select a version
    const versionItem = screen.getAllByRole('button').filter(b => b.textContent.includes('V'))[0];
    fireEvent.click(versionItem);

    // Verify details panel is populated
    await screen.findByText(/Details/);
  });

  it('filters history by date range', async () => {
    render(<VersionHistoryPanel graphId="test-graph" />);

    // Open date filter
    const filterButton = screen.getByText(/Filter/);
    fireEvent.click(filterButton);

    // Select date range
    const startDateInput = screen.getByLabelText(/Start Date/);
    fireEvent.change(startDateInput, { target: { value: '2026-01-01' } });

    // Verify filtered history
    await waitFor(() => {
      expect(screen.queryByText(/Loading history/)).not.toBeInTheDocument();
    });
  });
});
```

---

## 9. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Load timeline (50 items) | <500ms | Virtual scrolling, pagination |
| Show version details | <200ms | Cached queries |
| Render diff view | <1s | Syntax highlighting deferral |
| Switch between panels | <100ms | Memoized components |
| Search with debounce | <300ms | Debounce 300ms, server-side filtering |

---

## 10. Accessibility

### WCAG 2.1 AA Compliance

- Keyboard navigation throughout all panels
- Screen reader labels for icons and buttons
- Color not sole indicator (use patterns + colors for diffs)
- Sufficient contrast ratio (4.5:1)
- Focus indicators visible
- Date pickers keyboard accessible

**Example:**
```typescript
<button
  aria-label="Restore to this version"
  aria-pressed={isSelected}
  className="..."
>
  ↻ Restore
</button>
```

---

## 11. License Gating

| Tier | Feature Support |
| :--- | :--- |
| Core | ❌ Not available |
| WriterPro | ⚠️ View only (30 day limit indicator) |
| Teams | ✅ Full UI (1 year retention indicator) |
| Enterprise | ✅ Full UI (unlimited retention) |

**Code Example:**
```typescript
const VersionHistoryUI = ({ licenseContext }) => {
  const canRestore = licenseContext.tier >= 'Teams';
  const canUnlimitedRetention = licenseContext.tier === 'Enterprise';

  return (
    <>
      {!canRestore && <BetaBanner message="Restore requires Teams tier" />}
      {licenseContext.tier === 'WriterPro' && <RetentionLimitWarning days={30} />}
      {!canUnlimitedRetention && <RetentionLimitWarning days={365} />}
    </>
  );
};
```

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Version history exists | Panel loads | All versions displayed in timeline |
| 2 | Multiple versions | Filter by date | Only versions in range shown |
| 3 | Version selected | Details loaded | Change summary visible |
| 4 | Two versions selected | Compare clicked | Diff view shows before/after |
| 5 | Version selected | Restore clicked | Confirmation modal shown |
| 6 | Branches exist | Branch panel opened | All branches listed with head versions |
| 7 | Feature branch | Switch branch clicked | Active branch changes |
| 8 | Current branch | Merge button clicked | Merge preview shows conflicts/changes |
| 9 | No snapshots | Create snapshot | Snapshot created with metadata |
| 10 | Snapshot exists | Download clicked | ZIP file downloaded |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - Version timeline, diff viewer, snapshot manager, branch UI |

---
