# LCS-DES-091c: Design Specification — Workspace UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COL-091c` | Sub-part of COL-091 |
| **Feature Name** | `Workspace UI` | User interface for workspaces |
| **Target Version** | `v0.9.1c` | Third sub-part of v0.9.1 |
| **Module Scope** | `Lexichord.Modules.Collaboration` | Collaboration module |
| **Swimlane** | `Collaboration` | Team features vertical |
| **License Tier** | `Teams` | Requires Teams license |
| **Feature Gate Key** | `team_workspaces` | License feature key |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-091-INDEX](./LCS-DES-091-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-091 Section 3.3](./LCS-SBD-091.md#33-v091c-workspace-ui) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need intuitive interfaces to create, manage, and switch between workspaces. Without proper UI:

- Users cannot discover workspace features
- Switching between personal and team contexts is confusing
- Managing team members requires manual database operations
- License-gated features are not properly communicated

> **Goal:** Create a seamless user experience for workspace management with clear visual hierarchy, intuitive navigation, and appropriate license gating.

### 2.2 The Proposed Solution

Implement workspace UI components that:

1. **Workspace Selector** in title bar for quick context switching
2. **Workspace Manager** dialog for CRUD operations
3. **Member Management** panel for viewing and managing team
4. **Settings Panel** for workspace configuration
5. **License Gating** with upgrade prompts for non-Teams users

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Required Services

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IWorkspaceRepository` | v0.9.1a | Workspace data access |
| `IWorkspaceAuthorizationService` | v0.9.1b | Permission checking for UI state |
| `ILicenseContext` | v0.0.4c | Check Teams tier for feature access |
| `IMediator` | v0.0.7a | Publish workspace events |
| `IRegionManager` | v0.1.1b | Navigate panels on workspace switch |
| `ISettingsService` | v0.1.6a | Store active workspace preference |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `CommunityToolkit.Mvvm` | 8.x | MVVM framework |
| `Avalonia.Controls` | 11.x | UI controls |

### 3.2 Licensing Behavior

| License Tier | UI Behavior |
| :--- | :--- |
| Core | "Create Team Workspace" disabled, upgrade tooltip shown |
| WriterPro | Can join workspaces as Viewer, cannot create |
| Teams | Full workspace functionality enabled |
| Enterprise | Full functionality + admin features |

---

## 4. Data Contract (The API)

### 4.1 ViewModels

```csharp
namespace Lexichord.Modules.Collaboration.ViewModels;

/// <summary>
/// ViewModel for the workspace selector dropdown in the title bar.
/// </summary>
public partial class WorkspaceSelectorViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(AvatarInitial))]
    private Workspace? _activeWorkspace;

    [ObservableProperty]
    private ObservableCollection<WorkspaceListItem> _workspaces = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDropdownOpen;

    /// <summary>
    /// Whether the user has a Teams license and can create workspaces.
    /// </summary>
    [ObservableProperty]
    private bool _canCreateWorkspace;

    /// <summary>
    /// Display name shown in the title bar.
    /// </summary>
    public string DisplayName => ActiveWorkspace?.Name ?? "Personal Workspace";

    /// <summary>
    /// First letter of workspace name for avatar.
    /// </summary>
    public string AvatarInitial => DisplayName.Length > 0 ? DisplayName[..1].ToUpper() : "P";

    [RelayCommand]
    private async Task LoadWorkspacesAsync()
    {
        IsLoading = true;
        try
        {
            var workspaces = await _workspaceService.GetUserWorkspacesAsync();
            Workspaces.Clear();

            // Add personal workspace at top
            Workspaces.Add(new WorkspaceListItem
            {
                WorkspaceId = null,
                Name = "Personal Workspace",
                IsPersonal = true,
                IsActive = ActiveWorkspace is null
            });

            // Add team workspaces
            foreach (var ws in workspaces)
            {
                Workspaces.Add(new WorkspaceListItem
                {
                    WorkspaceId = ws.WorkspaceId,
                    Name = ws.Name,
                    Role = await GetUserRoleAsync(ws.WorkspaceId),
                    MemberCount = await _workspaceService.GetMemberCountAsync(ws.WorkspaceId),
                    IsActive = ws.WorkspaceId == ActiveWorkspace?.WorkspaceId
                });
            }

            CanCreateWorkspace = _licenseContext.HasFeature("team_workspaces");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SwitchWorkspaceAsync(Guid? workspaceId)
    {
        if (workspaceId == ActiveWorkspace?.WorkspaceId)
        {
            IsDropdownOpen = false;
            return;
        }

        IsLoading = true;
        try
        {
            if (workspaceId is null)
            {
                // Switch to personal workspace
                await _workspaceService.SetActiveWorkspaceAsync(null);
                ActiveWorkspace = null;
            }
            else
            {
                var workspace = await _workspaceService.GetWorkspaceAsync(workspaceId.Value);
                await _workspaceService.SetActiveWorkspaceAsync(workspaceId.Value);
                ActiveWorkspace = workspace;
            }

            // Update active flags
            foreach (var ws in Workspaces)
            {
                ws.IsActive = ws.WorkspaceId == workspaceId;
            }

            IsDropdownOpen = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenWorkspaceManager()
    {
        IsDropdownOpen = false;
        _mediator.Publish(new OpenWorkspaceManagerRequest());
    }

    [RelayCommand]
    private async Task CreateWorkspaceAsync()
    {
        if (!CanCreateWorkspace)
        {
            // Show upgrade dialog
            await _mediator.Publish(new ShowUpgradeDialogRequest("team_workspaces"));
            return;
        }

        IsDropdownOpen = false;
        _mediator.Publish(new OpenCreateWorkspaceDialogRequest());
    }
}

/// <summary>
/// Display item for workspace list.
/// </summary>
public partial class WorkspaceListItem : ObservableObject
{
    public Guid? WorkspaceId { get; init; }
    public required string Name { get; init; }
    public WorkspaceRole? Role { get; init; }
    public int MemberCount { get; init; }
    public bool IsPersonal { get; init; }

    [ObservableProperty]
    private bool _isActive;

    public string RoleDisplay => Role switch
    {
        WorkspaceRole.Owner => "Owner",
        WorkspaceRole.Editor => "Editor",
        WorkspaceRole.Viewer => "Viewer",
        _ => ""
    };
}
```

```csharp
/// <summary>
/// ViewModel for workspace manager dialog.
/// </summary>
public partial class WorkspaceManagerViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspaceAuthorizationService _authService;
    private readonly IMediator _mediator;

    [ObservableProperty]
    private ObservableCollection<WorkspaceManagerItem> _workspaces = [];

    [ObservableProperty]
    private WorkspaceManagerItem? _selectedWorkspace;

    [ObservableProperty]
    private bool _isLoading;

    // Create/Edit form fields
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveWorkspaceCommand))]
    private string _workspaceName = "";

    [ObservableProperty]
    private string _workspaceDescription = "";

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private Guid? _editingWorkspaceId;

    public bool CanSaveWorkspace => !string.IsNullOrWhiteSpace(WorkspaceName);

    [RelayCommand]
    private async Task LoadWorkspacesAsync()
    {
        IsLoading = true;
        try
        {
            var workspaces = await _workspaceService.GetUserWorkspacesAsync();
            Workspaces.Clear();

            foreach (var ws in workspaces)
            {
                var role = await _authService.GetRoleAsync(ws.WorkspaceId, _currentUserId);
                var memberCount = await _workspaceService.GetMemberCountAsync(ws.WorkspaceId);

                Workspaces.Add(new WorkspaceManagerItem
                {
                    WorkspaceId = ws.WorkspaceId,
                    Name = ws.Name,
                    Description = ws.Description,
                    Role = role ?? WorkspaceRole.Viewer,
                    MemberCount = memberCount,
                    CreatedAt = ws.CreatedAt,
                    CanEdit = role == WorkspaceRole.Owner,
                    CanDelete = role == WorkspaceRole.Owner
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void StartCreate()
    {
        IsEditing = true;
        EditingWorkspaceId = null;
        WorkspaceName = "";
        WorkspaceDescription = "";
    }

    [RelayCommand]
    private void StartEdit(WorkspaceManagerItem item)
    {
        if (!item.CanEdit) return;

        IsEditing = true;
        EditingWorkspaceId = item.WorkspaceId;
        WorkspaceName = item.Name;
        WorkspaceDescription = item.Description ?? "";
    }

    [RelayCommand(CanExecute = nameof(CanSaveWorkspace))]
    private async Task SaveWorkspaceAsync()
    {
        IsLoading = true;
        try
        {
            if (EditingWorkspaceId is null)
            {
                // Create new workspace
                var workspace = await _workspaceService.CreateWorkspaceAsync(new CreateWorkspaceRequest
                {
                    Name = WorkspaceName.Trim(),
                    Description = string.IsNullOrWhiteSpace(WorkspaceDescription)
                        ? null : WorkspaceDescription.Trim()
                });

                Workspaces.Add(new WorkspaceManagerItem
                {
                    WorkspaceId = workspace.WorkspaceId,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    Role = WorkspaceRole.Owner,
                    MemberCount = 1,
                    CreatedAt = workspace.CreatedAt,
                    CanEdit = true,
                    CanDelete = true
                });
            }
            else
            {
                // Update existing workspace
                await _workspaceService.UpdateWorkspaceAsync(new UpdateWorkspaceRequest
                {
                    WorkspaceId = EditingWorkspaceId.Value,
                    Name = WorkspaceName.Trim(),
                    Description = string.IsNullOrWhiteSpace(WorkspaceDescription)
                        ? null : WorkspaceDescription.Trim()
                });

                var item = Workspaces.FirstOrDefault(w => w.WorkspaceId == EditingWorkspaceId);
                if (item is not null)
                {
                    item.Name = WorkspaceName.Trim();
                    item.Description = WorkspaceDescription.Trim();
                }
            }

            CancelEdit();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingWorkspaceId = null;
        WorkspaceName = "";
        WorkspaceDescription = "";
    }

    [RelayCommand]
    private async Task DeleteWorkspaceAsync(WorkspaceManagerItem item)
    {
        if (!item.CanDelete) return;

        var confirmed = await _mediator.Send(new ConfirmDeleteRequest
        {
            Title = "Delete Workspace",
            Message = $"Are you sure you want to delete \"{item.Name}\"? " +
                     "This action cannot be undone and will remove all members."
        });

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            await _workspaceService.DeleteWorkspaceAsync(item.WorkspaceId);
            Workspaces.Remove(item);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenMembers(WorkspaceManagerItem item)
    {
        _mediator.Publish(new OpenWorkspaceMembersRequest(item.WorkspaceId));
    }

    [RelayCommand]
    private void OpenSettings(WorkspaceManagerItem item)
    {
        if (!item.CanEdit) return;
        _mediator.Publish(new OpenWorkspaceSettingsRequest(item.WorkspaceId));
    }
}

public partial class WorkspaceManagerItem : ObservableObject
{
    public Guid WorkspaceId { get; init; }

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string? _description;

    public WorkspaceRole Role { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }

    public string RoleBadge => Role switch
    {
        WorkspaceRole.Owner => "Owner",
        WorkspaceRole.Editor => "Editor",
        WorkspaceRole.Viewer => "Viewer",
        _ => ""
    };

    public string MemberCountDisplay => MemberCount == 1
        ? "1 member"
        : $"{MemberCount} members";
}
```

```csharp
/// <summary>
/// ViewModel for workspace members panel.
/// </summary>
public partial class WorkspaceMembersViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IWorkspaceAuthorizationService _authService;
    private readonly IInvitationService _invitationService;
    private readonly IMediator _mediator;

    private Guid _workspaceId;
    private Guid _currentUserId;

    [ObservableProperty]
    private Workspace? _workspace;

    [ObservableProperty]
    private ObservableCollection<MemberListItem> _members = [];

    [ObservableProperty]
    private ObservableCollection<PendingInvitationItem> _pendingInvitations = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canManageMembers;

    [ObservableProperty]
    private bool _canInviteMembers;

    public async Task InitializeAsync(Guid workspaceId, Guid currentUserId)
    {
        _workspaceId = workspaceId;
        _currentUserId = currentUserId;

        var permissions = await _authService.GetPermissionsAsync(workspaceId, currentUserId);
        CanManageMembers = permissions.HasFlag(WorkspacePermission.ChangeRoles);
        CanInviteMembers = permissions.HasFlag(WorkspacePermission.InviteMembers);

        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Workspace = await _workspaceService.GetWorkspaceAsync(_workspaceId);
            var members = await _workspaceService.GetMembersAsync(_workspaceId);

            Members.Clear();
            foreach (var member in members)
            {
                Members.Add(new MemberListItem
                {
                    UserId = member.UserId,
                    DisplayName = member.DisplayName ?? member.Email ?? "Unknown",
                    Email = member.Email,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    IsCurrentUser = member.UserId == _currentUserId,
                    CanChangeRole = CanManageMembers && member.UserId != _currentUserId,
                    CanRemove = CanManageMembers && member.UserId != _currentUserId
                });
            }

            // Load pending invitations
            if (CanManageMembers)
            {
                var invitations = await _invitationService.GetPendingInvitationsAsync(_workspaceId);
                PendingInvitations.Clear();
                foreach (var inv in invitations)
                {
                    PendingInvitations.Add(new PendingInvitationItem
                    {
                        InvitationId = inv.InvitationId,
                        Email = inv.Email,
                        Role = inv.Role,
                        InvitedAt = inv.CreatedAt,
                        ExpiresAt = inv.ExpiresAt
                    });
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChangeRoleAsync(MemberListItem member, WorkspaceRole newRole)
    {
        if (!member.CanChangeRole || member.Role == newRole) return;

        IsLoading = true;
        try
        {
            await _authService.ChangeRoleAsync(_workspaceId, member.UserId, newRole, _currentUserId);
            member.Role = newRole;
        }
        catch (InvalidOperationException ex)
        {
            await _mediator.Publish(new ShowErrorNotification(ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(MemberListItem member)
    {
        if (!member.CanRemove) return;

        var confirmed = await _mediator.Send(new ConfirmDeleteRequest
        {
            Title = "Remove Member",
            Message = $"Are you sure you want to remove {member.DisplayName} from this workspace?"
        });

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            await _workspaceService.RemoveMemberAsync(_workspaceId, member.UserId);
            Members.Remove(member);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenInviteDialog()
    {
        if (!CanInviteMembers) return;
        _mediator.Publish(new OpenInviteUserDialogRequest(_workspaceId));
    }

    [RelayCommand]
    private async Task GenerateJoinCodeAsync()
    {
        if (!CanInviteMembers) return;

        var code = await _invitationService.CreateJoinCodeAsync(
            _workspaceId,
            WorkspaceRole.Editor,
            _currentUserId,
            expiresIn: TimeSpan.FromDays(7));

        await _mediator.Publish(new ShowJoinCodeDialogRequest(code.Code));
    }

    [RelayCommand]
    private async Task ResendInvitationAsync(PendingInvitationItem invitation)
    {
        IsLoading = true;
        try
        {
            await _invitationService.ResendInvitationAsync(invitation.InvitationId);
            await _mediator.Publish(new ShowSuccessNotification("Invitation resent"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RevokeInvitationAsync(PendingInvitationItem invitation)
    {
        IsLoading = true;
        try
        {
            await _invitationService.RevokeInvitationAsync(invitation.InvitationId);
            PendingInvitations.Remove(invitation);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class MemberListItem : ObservableObject
{
    public Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? Email { get; init; }

    [ObservableProperty]
    private WorkspaceRole _role;

    public DateTime JoinedAt { get; init; }
    public bool IsCurrentUser { get; init; }
    public bool CanChangeRole { get; init; }
    public bool CanRemove { get; init; }

    public string JoinedDisplay => JoinedAt.ToString("MMM d, yyyy");
}

public record PendingInvitationItem
{
    public Guid InvitationId { get; init; }
    public required string Email { get; init; }
    public WorkspaceRole Role { get; init; }
    public DateTime InvitedAt { get; init; }
    public DateTime ExpiresAt { get; init; }

    public string StatusDisplay => "Pending invitation";
}
```

---

## 5. UI/UX Specifications

### 5.1 Workspace Selector (Title Bar)

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  [P] Personal Workspace ▼  │  Lexichord - My Document.md        [?] [_] [x]  │
└──────────────────────────────────────────────────────────────────────────────┘
      │
      │  (Click to expand dropdown)
      ▼
┌─────────────────────────────────────┐
│  PERSONAL                           │
│  ─────────────────────────────────  │
│  ◉ [P] Personal Workspace           │  ← Selected (radio button active)
│                                     │
│  TEAM WORKSPACES                    │
│  ─────────────────────────────────  │
│  ○ [A] Acme Writing Team    Owner   │  ← Role badge
│      5 members                      │
│  ○ [M] Marketing Dept       Editor  │
│      12 members                     │
│  ○ [P] Product Docs         Viewer  │
│      8 members                      │
│                                     │
│  ─────────────────────────────────  │
│  [+ Create Team Workspace]          │  ← Disabled if not Teams tier
│  [⚙ Manage Workspaces...]           │
└─────────────────────────────────────┘
```

**Component States:**

```text
WORKSPACE SELECTOR STATES:

┌─ Default State ─────────────────────┐
│  [A] Acme Team ▼                    │  Normal text, hover highlight
└─────────────────────────────────────┘

┌─ Loading State ──────────────────────┐
│  [⟳] Loading... ▼                    │  Spinner, disabled
└─────────────────────────────────────┘

┌─ License Gated (Core User) ─────────┐
│  [P] Personal Workspace ▼           │
│  ┌───────────────────────────────┐  │
│  │ [+ Create Team Workspace]     │  │  ← Grayed out
│  │    ⚡ Upgrade to Teams        │  │  ← Yellow badge
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

### 5.2 Workspace Manager Dialog

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  Manage Workspaces                                                    [x]    │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  [+ New Workspace]                                                           │
│                                                                              │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ [A]  Acme Writing Team                                                 │ │
│  │      Collaborative writing for marketing content                       │ │
│  │      ─────────────────────────────────────────────────────────────────│ │
│  │      [Owner]  5 members  •  Created Jan 15, 2026                      │ │
│  │                                                       [⚙] [👥] [🗑]   │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ [M]  Marketing Department                                              │ │
│  │      No description                                                    │ │
│  │      ─────────────────────────────────────────────────────────────────│ │
│  │      [Editor]  12 members  •  Created Jan 20, 2026                    │ │
│  │                                                       [⚙] [👥]        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│                                                             [Close]          │
└──────────────────────────────────────────────────────────────────────────────┘

Button Legend:
[⚙] = Settings (Owner only)
[👥] = View Members
[🗑] = Delete Workspace (Owner only)
```

### 5.3 Create/Edit Workspace Form

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  Create Team Workspace                                                [x]    │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Workspace Name *                                                            │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ Acme Writing Team                                                      │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  Description                                                                 │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ Collaborative space for our content team                              │ │
│  │                                                                        │ │
│  │                                                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ℹ️ You will be the Owner of this workspace and can invite team members.    │
│                                                                              │
│                                              [Cancel]  [Create Workspace]    │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 5.4 Members Panel

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  Team Members (5)                                [Invite] [Generate Code]    │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Name                    │ Role              │ Joined         │ Actions      │
│ ─────────────────────────────────────────────────────────────────────────── │
│  [👤] Alice Smith        │ Owner             │ Jan 15, 2026   │              │
│       alice@acme.com     │                   │                │              │
│ ─────────────────────────────────────────────────────────────────────────── │
│  [👤] Bob Johnson        │ [Editor ▼]        │ Jan 20, 2026   │ [Remove]     │
│       bob@acme.com       │                   │                │              │
│ ─────────────────────────────────────────────────────────────────────────── │
│  [👤] Carol Williams     │ [Viewer ▼]        │ Jan 22, 2026   │ [Remove]     │
│       carol@acme.com     │                   │                │              │
│                                                                              │
│  ─────────────────────────────────────────────────────────────────────────  │
│  PENDING INVITATIONS                                                         │
│  ─────────────────────────────────────────────────────────────────────────  │
│  [✉] dave@acme.com       │ Editor (pending)  │ Invited Jan 25 │ [Resend] [x] │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

Role Dropdown (Owner only, for other members):
┌─────────────────┐
│ ◉ Editor        │
│ ○ Viewer        │
│ ○ Owner         │  ← With warning about transferring control
└─────────────────┘
```

### 5.5 Component Styling

```text
COMPONENT STYLING SPECIFICATIONS:

┌─ Workspace Card ────────────────────────────────────────────┐
│  Background: Brush.Surface.Secondary                        │
│  Border: 1px solid Brush.Border.Default                     │
│  Border Radius: 8px                                         │
│  Padding: 16px                                              │
│  Hover: Background → Brush.Surface.Hover                    │
│  Selected: Border → Brush.Accent.Primary                    │
└─────────────────────────────────────────────────────────────┘

┌─ Role Badge ────────────────────────────────────────────────┐
│  Owner:  Background: #22c55e (green), Text: white           │
│  Editor: Background: #3b82f6 (blue), Text: white            │
│  Viewer: Background: #6b7280 (gray), Text: white            │
│  Pending: Background: #f59e0b (amber), Text: white          │
│                                                             │
│  Border Radius: 4px                                         │
│  Padding: 2px 8px                                           │
│  Font Size: 12px                                            │
└─────────────────────────────────────────────────────────────┘

┌─ Avatar Circle ─────────────────────────────────────────────┐
│  Size: 32px x 32px                                          │
│  Border Radius: 16px (full circle)                          │
│  Background: Hash-based color from workspace name           │
│  Text: White, centered, uppercase first letter              │
│  Font Size: 14px, Bold                                      │
└─────────────────────────────────────────────────────────────┘

┌─ Upgrade Badge ─────────────────────────────────────────────┐
│  Background: #fef3c7 (amber-100)                            │
│  Border: 1px solid #f59e0b (amber-500)                      │
│  Text: #92400e (amber-900)                                  │
│  Icon: Lightning bolt                                       │
│  Border Radius: 4px                                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. Test Scenarios

### 6.1 ViewModel Tests

```csharp
namespace Lexichord.Tests.Collaboration;

[Trait("Category", "Unit")]
[Trait("Version", "v0.9.1c")]
public class WorkspaceSelectorViewModelTests
{
    private readonly Mock<IWorkspaceService> _serviceMock;
    private readonly Mock<ILicenseContext> _licenseMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly WorkspaceSelectorViewModel _sut;

    public WorkspaceSelectorViewModelTests()
    {
        _serviceMock = new Mock<IWorkspaceService>();
        _licenseMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _sut = new WorkspaceSelectorViewModel(
            _serviceMock.Object,
            _licenseMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task LoadWorkspacesAsync_AddsPersonalWorkspaceFirst()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetUserWorkspacesAsync(default))
            .ReturnsAsync(new List<Workspace>
            {
                new() { WorkspaceId = Guid.NewGuid(), Name = "Team A" }
            });

        // Act
        await _sut.LoadWorkspacesCommand.ExecuteAsync(null);

        // Assert
        _sut.Workspaces.Should().HaveCount(2);
        _sut.Workspaces[0].IsPersonal.Should().BeTrue();
        _sut.Workspaces[0].Name.Should().Be("Personal Workspace");
    }

    [Fact]
    public async Task LoadWorkspacesAsync_SetsCanCreateBasedOnLicense()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserWorkspacesAsync(default)).ReturnsAsync([]);
        _licenseMock.Setup(l => l.HasFeature("team_workspaces")).Returns(true);

        // Act
        await _sut.LoadWorkspacesCommand.ExecuteAsync(null);

        // Assert
        _sut.CanCreateWorkspace.Should().BeTrue();
    }

    [Fact]
    public async Task LoadWorkspacesAsync_CoreUser_CannotCreate()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserWorkspacesAsync(default)).ReturnsAsync([]);
        _licenseMock.Setup(l => l.HasFeature("team_workspaces")).Returns(false);

        // Act
        await _sut.LoadWorkspacesCommand.ExecuteAsync(null);

        // Assert
        _sut.CanCreateWorkspace.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchWorkspaceAsync_UpdatesActiveWorkspace()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { WorkspaceId = workspaceId, Name = "Team A" };
        _serviceMock.Setup(s => s.GetWorkspaceAsync(workspaceId, default)).ReturnsAsync(workspace);

        // Act
        await _sut.SwitchWorkspaceCommand.ExecuteAsync(workspaceId);

        // Assert
        _sut.ActiveWorkspace.Should().Be(workspace);
        _sut.DisplayName.Should().Be("Team A");
    }

    [Fact]
    public async Task SwitchWorkspaceAsync_ToPersonal_SetsActiveToNull()
    {
        // Arrange
        _sut.ActiveWorkspace = new Workspace { WorkspaceId = Guid.NewGuid(), Name = "Team" };

        // Act
        await _sut.SwitchWorkspaceCommand.ExecuteAsync(null);

        // Assert
        _sut.ActiveWorkspace.Should().BeNull();
        _sut.DisplayName.Should().Be("Personal Workspace");
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WithoutLicense_ShowsUpgradeDialog()
    {
        // Arrange
        _sut.CanCreateWorkspace = false;

        // Act
        await _sut.CreateWorkspaceCommand.ExecuteAsync(null);

        // Assert
        _mediatorMock.Verify(m => m.Publish(
            It.IsAny<ShowUpgradeDialogRequest>(), default), Times.Once);
    }

    [Fact]
    public void DisplayName_NoActiveWorkspace_ReturnsPersonal()
    {
        _sut.ActiveWorkspace = null;
        _sut.DisplayName.Should().Be("Personal Workspace");
    }

    [Fact]
    public void AvatarInitial_NoActiveWorkspace_ReturnsP()
    {
        _sut.ActiveWorkspace = null;
        _sut.AvatarInitial.Should().Be("P");
    }

    [Fact]
    public void AvatarInitial_ActiveWorkspace_ReturnsFirstLetter()
    {
        _sut.ActiveWorkspace = new Workspace { Name = "Marketing Team" };
        _sut.AvatarInitial.Should().Be("M");
    }
}
```

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.9.1c")]
public class WorkspaceMembersViewModelTests
{
    private readonly Mock<IWorkspaceService> _serviceMock;
    private readonly Mock<IWorkspaceAuthorizationService> _authMock;
    private readonly Mock<IInvitationService> _invitationMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly WorkspaceMembersViewModel _sut;

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();

    public WorkspaceMembersViewModelTests()
    {
        _serviceMock = new Mock<IWorkspaceService>();
        _authMock = new Mock<IWorkspaceAuthorizationService>();
        _invitationMock = new Mock<IInvitationService>();
        _mediatorMock = new Mock<IMediator>();
        _sut = new WorkspaceMembersViewModel(
            _serviceMock.Object,
            _authMock.Object,
            _invitationMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_SetsPermissionsBasedOnRole()
    {
        // Arrange
        var permissions = WorkspacePermission.ChangeRoles | WorkspacePermission.InviteMembers;
        _authMock
            .Setup(a => a.GetPermissionsAsync(_workspaceId, _currentUserId, default))
            .ReturnsAsync(permissions);
        SetupEmptyMembers();

        // Act
        await _sut.InitializeAsync(_workspaceId, _currentUserId);

        // Assert
        _sut.CanManageMembers.Should().BeTrue();
        _sut.CanInviteMembers.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_Viewer_CannotManageOrInvite()
    {
        // Arrange
        _authMock
            .Setup(a => a.GetPermissionsAsync(_workspaceId, _currentUserId, default))
            .ReturnsAsync(WorkspacePermission.ViewerPermissions);
        SetupEmptyMembers();

        // Act
        await _sut.InitializeAsync(_workspaceId, _currentUserId);

        // Assert
        _sut.CanManageMembers.Should().BeFalse();
        _sut.CanInviteMembers.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_PopulatesMembersList()
    {
        // Arrange
        await InitializeWithOwnerPermissions();
        var members = new List<WorkspaceMember>
        {
            new() { UserId = Guid.NewGuid(), DisplayName = "Alice", Role = WorkspaceRole.Owner },
            new() { UserId = Guid.NewGuid(), DisplayName = "Bob", Role = WorkspaceRole.Editor }
        };
        _serviceMock
            .Setup(s => s.GetMembersAsync(_workspaceId, default))
            .ReturnsAsync(members);

        // Act
        await _sut.LoadCommand.ExecuteAsync(null);

        // Assert
        _sut.Members.Should().HaveCount(2);
        _sut.Members[0].DisplayName.Should().Be("Alice");
        _sut.Members[1].DisplayName.Should().Be("Bob");
    }

    [Fact]
    public async Task ChangeRoleAsync_UpdatesMemberRole()
    {
        // Arrange
        await InitializeWithOwnerPermissions();
        var member = new MemberListItem
        {
            UserId = Guid.NewGuid(),
            DisplayName = "Bob",
            Role = WorkspaceRole.Viewer,
            CanChangeRole = true
        };
        _sut.Members.Add(member);

        // Act
        await _sut.ChangeRoleCommand.ExecuteAsync((member, WorkspaceRole.Editor));

        // Assert
        _authMock.Verify(a => a.ChangeRoleAsync(
            _workspaceId, member.UserId, WorkspaceRole.Editor, _currentUserId, default),
            Times.Once);
        member.Role.Should().Be(WorkspaceRole.Editor);
    }

    [Fact]
    public async Task RemoveMemberAsync_WithConfirmation_RemovesMember()
    {
        // Arrange
        await InitializeWithOwnerPermissions();
        var member = new MemberListItem
        {
            UserId = Guid.NewGuid(),
            DisplayName = "Bob",
            CanRemove = true
        };
        _sut.Members.Add(member);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ConfirmDeleteRequest>(), default))
            .ReturnsAsync(true);

        // Act
        await _sut.RemoveMemberCommand.ExecuteAsync(member);

        // Assert
        _serviceMock.Verify(s => s.RemoveMemberAsync(_workspaceId, member.UserId, default),
            Times.Once);
        _sut.Members.Should().NotContain(member);
    }

    [Fact]
    public async Task RemoveMemberAsync_WithoutConfirmation_DoesNotRemove()
    {
        // Arrange
        await InitializeWithOwnerPermissions();
        var member = new MemberListItem
        {
            UserId = Guid.NewGuid(),
            DisplayName = "Bob",
            CanRemove = true
        };
        _sut.Members.Add(member);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ConfirmDeleteRequest>(), default))
            .ReturnsAsync(false);

        // Act
        await _sut.RemoveMemberCommand.ExecuteAsync(member);

        // Assert
        _serviceMock.Verify(s => s.RemoveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default),
            Times.Never);
        _sut.Members.Should().Contain(member);
    }

    private async Task InitializeWithOwnerPermissions()
    {
        _authMock
            .Setup(a => a.GetPermissionsAsync(_workspaceId, _currentUserId, default))
            .ReturnsAsync(WorkspacePermission.OwnerPermissions);
        SetupEmptyMembers();
        await _sut.InitializeAsync(_workspaceId, _currentUserId);
    }

    private void SetupEmptyMembers()
    {
        _serviceMock
            .Setup(s => s.GetWorkspaceAsync(_workspaceId, default))
            .ReturnsAsync(new Workspace { WorkspaceId = _workspaceId, Name = "Test" });
        _serviceMock
            .Setup(s => s.GetMembersAsync(_workspaceId, default))
            .ReturnsAsync(new List<WorkspaceMember>());
        _invitationMock
            .Setup(i => i.GetPendingInvitationsAsync(_workspaceId, default))
            .ReturnsAsync(new List<WorkspaceInvitation>());
    }
}
```

---

## 7. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Info | `"User switched to workspace {WorkspaceId}"` |
| Info | `"User switched to personal workspace"` |
| Info | `"Workspace created via UI: {WorkspaceId}"` |
| Debug | `"Loading workspaces for user"` |
| Debug | `"Loading members for workspace {WorkspaceId}"` |
| Warning | `"User attempted to create workspace without Teams license"` |

---

## 8. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| UI shows actions user cannot perform | Low | Disable buttons based on permissions |
| Stale permission state | Low | Reload permissions on focus/visibility |
| Click-through on disabled buttons | Low | Use proper disabled state styling |

---

## 9. Acceptance Criteria

### 9.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | User opens dropdown | Clicking workspace selector | Dropdown shows all workspaces |
| 2 | User has workspaces | Viewing dropdown | Personal workspace at top, teams below |
| 3 | User selects workspace | Clicking workspace item | Active workspace changes |
| 4 | Core user | Viewing Create button | Button disabled with upgrade tooltip |
| 5 | Teams user | Clicking Create button | Create dialog opens |
| 6 | Owner in manager | Viewing workspace card | Edit, members, delete buttons visible |
| 7 | Editor in manager | Viewing workspace card | Only members button visible |
| 8 | Owner in members | Viewing member row | Role dropdown and remove button enabled |
| 9 | Viewer in members | Viewing member row | Role dropdown and remove hidden |
| 10 | Creating workspace | Entering name and submitting | Workspace created and added to list |

### 9.2 Performance Criteria

| # | Operation | Target |
| :--- | :--- | :--- |
| 11 | Dropdown open to workspaces loaded | < 200ms |
| 12 | Workspace switch complete | < 300ms |
| 13 | Members panel load (100 members) | < 500ms |

---

## 10. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `WorkspaceSelectorView.axaml` | [ ] |
| 2 | `WorkspaceSelectorViewModel.cs` | [ ] |
| 3 | `WorkspaceManagerView.axaml` | [ ] |
| 4 | `WorkspaceManagerViewModel.cs` | [ ] |
| 5 | `WorkspaceMembersView.axaml` | [ ] |
| 6 | `WorkspaceMembersViewModel.cs` | [ ] |
| 7 | `WorkspaceSettingsView.axaml` | [ ] |
| 8 | `WorkspaceListItem.cs` display model | [ ] |
| 9 | `MemberListItem.cs` display model | [ ] |
| 10 | `WorkspaceSelectorViewModelTests.cs` | [ ] |
| 11 | `WorkspaceMembersViewModelTests.cs` | [ ] |

---

## 11. Verification Commands

```bash
# ═══════════════════════════════════════════════════════════════════════════
# v0.9.1c Verification
# ═══════════════════════════════════════════════════════════════════════════

# 1. Build module
dotnet build src/Lexichord.Modules.Collaboration

# 2. Run unit tests
dotnet test --filter "Version=v0.9.1c" --logger "console;verbosity=detailed"

# 3. Manual UI verification:
# a) Open application as Core user - verify Create button disabled
# b) Open application as Teams user - create workspace
# c) Switch between personal and team workspaces
# d) Open members panel as Owner - verify all controls enabled
# e) Open members panel as Viewer - verify controls disabled
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
