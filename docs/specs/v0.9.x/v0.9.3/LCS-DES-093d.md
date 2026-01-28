# LCS-DES-093d: Design Specification — Review Dashboard

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COL-093d` | Sub-part of COL-093 |
| **Feature Name** | `Review Dashboard` | Centralized review tracking |
| **Target Version** | `v0.9.3d` | Fourth sub-part of v0.9.3 |
| **Module Scope** | `Lexichord.Modules.Collaboration` | Collaboration module |
| **Swimlane** | `Collaboration` | Team features vertical |
| **License Tier** | `Teams` | Teams tier required |
| **Feature Gate Key** | `FeatureFlags.Collaboration.ReviewWorkflows` | License check key |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-093-INDEX](./LCS-DES-093-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-093 Section 3.4](./LCS-SBD-093.md#34-v093d-review-dashboard) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Team leads and reviewers need visibility into review activity:

- No centralized view of pending reviews across all documents
- Overdue reviews are not easily identifiable
- Review metrics (completion time, bottlenecks) are unknown
- Cannot quickly act on reviews without opening each document
- Status of review requests created by the user is scattered

> **Goal:** Create a centralized dashboard that displays all pending reviews, their status, deadlines, and provides quick actions for reviewers and team leads.

### 2.2 The Proposed Solution

Implement a Review Dashboard that:

1. Shows all pending reviews assigned to the current user
2. Shows reviews requested by the current user
3. Displays team-wide review activity (for team leads)
4. Highlights overdue reviews with visual indicators
5. Provides quick actions (approve, request changes, remind)
6. Supports filtering by status, priority, assignee, and date
7. Displays metrics on review velocity and bottlenecks

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Modules

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IReviewRequestService` | v0.9.3a | Retrieve review requests |
| `ICommentService` | v0.9.3b | Get comment counts |
| `IApprovalGateService` | v0.9.3c | Get approval status |
| `IProfileService` | v0.9.1a | User avatars and names |
| `ITeamService` | v0.7.1a | Team membership for filtering |
| `IDocumentService` | v0.1.3a | Document titles and metadata |
| `ILicenseStateService` | v0.9.2c | Verify Teams tier access |
| `INavigationService` | v0.1.1a | Navigate to documents |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `CommunityToolkit.Mvvm` | 8.x | MVVM infrastructure |
| `LiveChartsCore.SkiaSharpView.Avalonia` | 2.x | Metrics charts (optional) |

### 3.2 Licensing Behavior

- **Load Behavior:** Hard Gate — Dashboard menu item hidden for non-Teams users
- **Fallback Experience:**
  - Navigation menu does not show "Review Dashboard" item
  - Direct URL access redirects to upgrade prompt
  - API calls return `NotAuthorized` with upgrade message

---

## 4. Data Contract (The API)

### 4.1 View Models

```csharp
namespace Lexichord.Modules.Collaboration.ViewModels;

/// <summary>
/// Main view model for the Review Dashboard.
/// Manages review lists, filtering, and quick actions.
/// </summary>
public partial class ReviewDashboardViewModel : ObservableObject
{
    private readonly IReviewRequestService _reviewService;
    private readonly ICommentService _commentService;
    private readonly IApprovalGateService _approvalService;
    private readonly IProfileService _profileService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<ReviewSummary> _myReviews = [];
    [ObservableProperty] private ObservableCollection<ReviewSummary> _requestedByMe = [];
    [ObservableProperty] private ObservableCollection<ReviewSummary> _allTeamReviews = [];
    [ObservableProperty] private ReviewDashboardMetrics _metrics = new();
    [ObservableProperty] private DashboardTab _selectedTab = DashboardTab.MyReviews;
    [ObservableProperty] private ReviewDashboardFilter _currentFilter = new();
    [ObservableProperty] private ReviewSortOption _sortOption = ReviewSortOption.DueDateAsc;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _pageNumber = 1;
    [ObservableProperty] private bool _hasMoreItems;

    public ReviewDashboardViewModel(
        IReviewRequestService reviewService,
        ICommentService commentService,
        IApprovalGateService approvalService,
        IProfileService profileService,
        INavigationService navigationService)
    {
        _reviewService = reviewService;
        _commentService = commentService;
        _approvalService = approvalService;
        _profileService = profileService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task InitializeAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        await RefreshAllAsync(ct);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        IsRefreshing = true;
        await RefreshAllAsync(ct);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task ApproveReviewAsync(ReviewSummary review)
    {
        await _reviewService.UpdateReviewerStatusAsync(
            review.ReviewId,
            GetCurrentUserId(),
            ReviewerStatus.Approved);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RequestChangesAsync(ReviewSummary review, string? comment)
    {
        await _reviewService.UpdateReviewerStatusAsync(
            review.ReviewId,
            GetCurrentUserId(),
            ReviewerStatus.RequestedChanges,
            comment);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task SendReminderAsync(ReviewSummary review)
    {
        await _reviewService.SendReminderAsync(review.ReviewId);
        // Show success toast
    }

    [RelayCommand]
    private void OpenDocument(ReviewSummary review)
    {
        _navigationService.NavigateToDocument(review.DocumentId);
    }

    [RelayCommand]
    private void ApplyFilter(ReviewDashboardFilter filter)
    {
        CurrentFilter = filter;
        PageNumber = 1;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void ClearFilter()
    {
        CurrentFilter = new ReviewDashboardFilter();
        PageNumber = 1;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMoreItems || IsLoading) return;
        PageNumber++;
        await LoadPageAsync();
    }

    private async Task RefreshAllAsync(CancellationToken ct = default)
    {
        try
        {
            ErrorMessage = null;

            // Load metrics first (fast)
            Metrics = await LoadMetricsAsync(ct);

            // Load reviews based on selected tab
            await LoadPageAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
    }

    private async Task LoadPageAsync(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();

        switch (SelectedTab)
        {
            case DashboardTab.MyReviews:
                var myReviews = await _reviewService.GetPendingReviewsForUserAsync(userId, ct);
                MyReviews = new ObservableCollection<ReviewSummary>(
                    await EnrichReviewsAsync(ApplyFilterAndSort(myReviews), ct));
                break;

            case DashboardTab.RequestedByMe:
                var requested = await _reviewService.GetRequestedByUserAsync(userId, ct);
                RequestedByMe = new ObservableCollection<ReviewSummary>(
                    await EnrichReviewsAsync(ApplyFilterAndSort(requested), ct));
                break;

            case DashboardTab.AllTeam:
                var teamReviews = await _reviewService.GetTeamReviewsAsync(ct);
                AllTeamReviews = new ObservableCollection<ReviewSummary>(
                    await EnrichReviewsAsync(ApplyFilterAndSort(teamReviews), ct));
                break;
        }
    }

    partial void OnSelectedTabChanged(DashboardTab value)
    {
        PageNumber = 1;
        _ = RefreshAsync();
    }

    partial void OnSortOptionChanged(ReviewSortOption value)
    {
        PageNumber = 1;
        _ = RefreshAsync();
    }
}

/// <summary>
/// Dashboard tabs for different review views.
/// </summary>
public enum DashboardTab
{
    MyReviews,
    RequestedByMe,
    AllTeam
}
```

### 4.2 Summary Records

```csharp
namespace Lexichord.Modules.Collaboration.Models;

/// <summary>
/// Summary of a review request for dashboard display.
/// Contains enriched data from multiple sources.
/// </summary>
public record ReviewSummary
{
    public required Guid ReviewId { get; init; }
    public required Guid DocumentId { get; init; }
    public required string DocumentTitle { get; init; }
    public required string RequestedByName { get; init; }
    public string? RequestedByAvatarPath { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? DueDate { get; init; }
    public required ReviewPriority Priority { get; init; }
    public required ReviewStatus Status { get; init; }
    public required IReadOnlyList<ReviewerSummary> Reviewers { get; init; }
    public required int CommentCount { get; init; }
    public required int UnresolvedCommentCount { get; init; }

    /// <summary>
    /// Returns true if review is past due date and still active.
    /// </summary>
    public bool IsOverdue => DueDate.HasValue &&
                             DateTime.UtcNow > DueDate.Value &&
                             Status == ReviewStatus.Active;

    /// <summary>
    /// Days until due (negative if overdue).
    /// </summary>
    public int? DaysUntilDue => DueDate.HasValue
        ? (int)(DueDate.Value - DateTime.UtcNow).TotalDays
        : null;

    /// <summary>
    /// Friendly due date text.
    /// </summary>
    public string DueDateText => DueDate.HasValue
        ? DaysUntilDue switch
        {
            < -1 => $"{Math.Abs(DaysUntilDue.Value)} days overdue",
            -1 => "1 day overdue",
            0 => "Due today",
            1 => "Due tomorrow",
            _ => $"Due in {DaysUntilDue} days"
        }
        : "No due date";

    /// <summary>
    /// Number of reviewers who have approved.
    /// </summary>
    public int ApprovedCount => Reviewers.Count(r => r.Status == ReviewerStatus.Approved);

    /// <summary>
    /// Total number of non-observer reviewers.
    /// </summary>
    public int TotalReviewers => Reviewers.Count(r => r.Role != ReviewerRole.Observer);

    /// <summary>
    /// Progress text (e.g., "2/3 reviewed").
    /// </summary>
    public string ProgressText => $"{ApprovedCount}/{TotalReviewers} reviewed";
}

/// <summary>
/// Summary of a reviewer for dashboard display.
/// </summary>
public record ReviewerSummary
{
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarPath { get; init; }
    public required ReviewerRole Role { get; init; }
    public required ReviewerStatus Status { get; init; }
}

/// <summary>
/// Aggregated metrics for the dashboard header.
/// </summary>
public record ReviewDashboardMetrics
{
    public int TotalPendingReviews { get; init; }
    public int OverdueReviews { get; init; }
    public int CompletedThisWeek { get; init; }
    public int CompletedThisMonth { get; init; }
    public double AverageReviewTimeHours { get; init; }
    public int MyPendingReviews { get; init; }
    public int AwaitingMyApproval { get; init; }
    public int ReviewsIRequested { get; init; }

    /// <summary>
    /// Formatted average review time.
    /// </summary>
    public string AverageReviewTimeText => AverageReviewTimeHours switch
    {
        < 1 => $"{(int)(AverageReviewTimeHours * 60)} min",
        < 24 => $"{AverageReviewTimeHours:F1} hrs",
        _ => $"{AverageReviewTimeHours / 24:F1} days"
    };
}

/// <summary>
/// Filter options for the dashboard.
/// </summary>
public record ReviewDashboardFilter
{
    public ReviewStatus? Status { get; init; }
    public ReviewPriority? Priority { get; init; }
    public Guid? AssigneeId { get; init; }
    public Guid? RequesterId { get; init; }
    public DateRange? DueDateRange { get; init; }
    public bool ShowOverdueOnly { get; init; }
    public string? SearchText { get; init; }

    /// <summary>
    /// Returns true if any filter is active.
    /// </summary>
    public bool IsActive =>
        Status.HasValue ||
        Priority.HasValue ||
        AssigneeId.HasValue ||
        RequesterId.HasValue ||
        DueDateRange is not null ||
        ShowOverdueOnly ||
        !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>
    /// Count of active filter criteria.
    /// </summary>
    public int ActiveFilterCount
    {
        get
        {
            var count = 0;
            if (Status.HasValue) count++;
            if (Priority.HasValue) count++;
            if (AssigneeId.HasValue) count++;
            if (RequesterId.HasValue) count++;
            if (DueDateRange is not null) count++;
            if (ShowOverdueOnly) count++;
            if (!string.IsNullOrWhiteSpace(SearchText)) count++;
            return count;
        }
    }
}

/// <summary>
/// Date range for filtering.
/// </summary>
public record DateRange(DateTime? Start, DateTime? End);

/// <summary>
/// Sort options for reviews.
/// </summary>
public enum ReviewSortOption
{
    DueDateAsc,
    DueDateDesc,
    CreatedAtAsc,
    CreatedAtDesc,
    PriorityDesc,
    PriorityAsc,
    DocumentTitleAsc,
    DocumentTitleDesc
}
```

---

## 5. Implementation Logic

### 5.1 Review Enrichment

```csharp
/// <summary>
/// Enriches review requests with additional data for dashboard display.
/// </summary>
private async Task<IReadOnlyList<ReviewSummary>> EnrichReviewsAsync(
    IEnumerable<ReviewRequest> reviews, CancellationToken ct)
{
    var summaries = new List<ReviewSummary>();

    foreach (var review in reviews)
    {
        // Get document title
        var document = await _documentService.GetDocumentAsync(review.DocumentId, ct);

        // Get requester profile
        var requester = await _profileService.GetProfileAsync(review.RequestedBy, ct);

        // Get comment counts
        var comments = await _commentService.GetCommentsForReviewAsync(review.ReviewId, ct);

        // Build reviewer summaries
        var reviewerSummaries = new List<ReviewerSummary>();
        foreach (var assignment in review.Reviewers)
        {
            var profile = await _profileService.GetProfileAsync(assignment.UserId, ct);
            reviewerSummaries.Add(new ReviewerSummary
            {
                UserId = assignment.UserId,
                DisplayName = profile?.Name ?? assignment.DisplayName,
                AvatarPath = profile?.AvatarPath ?? assignment.AvatarPath,
                Role = assignment.Role,
                Status = assignment.Status
            });
        }

        summaries.Add(new ReviewSummary
        {
            ReviewId = review.ReviewId,
            DocumentId = review.DocumentId,
            DocumentTitle = document?.Title ?? "Untitled",
            RequestedByName = requester?.Name ?? "Unknown",
            RequestedByAvatarPath = requester?.AvatarPath,
            CreatedAt = review.CreatedAt,
            DueDate = review.DueDate,
            Priority = review.Priority,
            Status = review.Status,
            Reviewers = reviewerSummaries,
            CommentCount = comments.Count,
            UnresolvedCommentCount = comments.Count(c => !c.IsResolved)
        });
    }

    return summaries;
}
```

### 5.2 Filtering and Sorting

```csharp
/// <summary>
/// Applies current filter and sort options to reviews.
/// </summary>
private IEnumerable<ReviewRequest> ApplyFilterAndSort(IEnumerable<ReviewRequest> reviews)
{
    var filtered = reviews.AsEnumerable();

    // Apply filters
    if (CurrentFilter.Status.HasValue)
    {
        filtered = filtered.Where(r => r.Status == CurrentFilter.Status.Value);
    }

    if (CurrentFilter.Priority.HasValue)
    {
        filtered = filtered.Where(r => r.Priority == CurrentFilter.Priority.Value);
    }

    if (CurrentFilter.AssigneeId.HasValue)
    {
        filtered = filtered.Where(r =>
            r.Reviewers.Any(rv => rv.UserId == CurrentFilter.AssigneeId.Value));
    }

    if (CurrentFilter.RequesterId.HasValue)
    {
        filtered = filtered.Where(r => r.RequestedBy == CurrentFilter.RequesterId.Value);
    }

    if (CurrentFilter.ShowOverdueOnly)
    {
        filtered = filtered.Where(r => r.IsOverdue);
    }

    if (CurrentFilter.DueDateRange is not null)
    {
        if (CurrentFilter.DueDateRange.Start.HasValue)
        {
            filtered = filtered.Where(r =>
                r.DueDate >= CurrentFilter.DueDateRange.Start.Value);
        }
        if (CurrentFilter.DueDateRange.End.HasValue)
        {
            filtered = filtered.Where(r =>
                r.DueDate <= CurrentFilter.DueDateRange.End.Value);
        }
    }

    if (!string.IsNullOrWhiteSpace(CurrentFilter.SearchText))
    {
        var search = CurrentFilter.SearchText.ToLowerInvariant();
        // Note: Document title search would need enrichment first
        // For now, filter by requester name included in review
    }

    // Apply sorting
    filtered = SortOption switch
    {
        ReviewSortOption.DueDateAsc => filtered
            .OrderBy(r => r.DueDate ?? DateTime.MaxValue),
        ReviewSortOption.DueDateDesc => filtered
            .OrderByDescending(r => r.DueDate ?? DateTime.MinValue),
        ReviewSortOption.CreatedAtAsc => filtered
            .OrderBy(r => r.CreatedAt),
        ReviewSortOption.CreatedAtDesc => filtered
            .OrderByDescending(r => r.CreatedAt),
        ReviewSortOption.PriorityDesc => filtered
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.DueDate ?? DateTime.MaxValue),
        ReviewSortOption.PriorityAsc => filtered
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.DueDate ?? DateTime.MaxValue),
        _ => filtered
    };

    return filtered;
}
```

### 5.3 Metrics Calculation

```csharp
/// <summary>
/// Calculates dashboard metrics.
/// </summary>
private async Task<ReviewDashboardMetrics> LoadMetricsAsync(CancellationToken ct)
{
    var userId = GetCurrentUserId();
    var now = DateTime.UtcNow;
    var weekStart = now.AddDays(-(int)now.DayOfWeek);
    var monthStart = new DateTime(now.Year, now.Month, 1);

    // Get all reviews for metrics
    var allReviews = await _reviewService.GetAllReviewsAsync(ct);
    var myPending = await _reviewService.GetPendingReviewsForUserAsync(userId, ct);
    var myRequested = await _reviewService.GetRequestedByUserAsync(userId, ct);

    // Calculate completion times for average
    var completedReviews = allReviews.Where(r => r.CompletedAt.HasValue);
    var avgTime = completedReviews.Any()
        ? completedReviews.Average(r => (r.CompletedAt!.Value - r.CreatedAt).TotalHours)
        : 0;

    return new ReviewDashboardMetrics
    {
        TotalPendingReviews = allReviews.Count(r => r.Status == ReviewStatus.Active),
        OverdueReviews = allReviews.Count(r => r.IsOverdue),
        CompletedThisWeek = allReviews.Count(r =>
            r.CompletedAt >= weekStart && r.Status == ReviewStatus.Completed),
        CompletedThisMonth = allReviews.Count(r =>
            r.CompletedAt >= monthStart && r.Status == ReviewStatus.Completed),
        AverageReviewTimeHours = avgTime,
        MyPendingReviews = myPending.Count,
        AwaitingMyApproval = myPending.Count(r =>
            r.Reviewers.Any(rv => rv.UserId == userId && rv.Role == ReviewerRole.Approver)),
        ReviewsIRequested = myRequested.Count(r => r.Status == ReviewStatus.Active)
    };
}
```

---

## 6. UI/UX Specifications

### 6.1 Dashboard Layout

```text
+------------------------------------------------------------------+
|  Review Dashboard                                   [Refresh]     |
+------------------------------------------------------------------+
|                                                                   |
|  +------------+  +------------+  +------------+  +-------------+  |
|  |     8      |  |     2      |  |    12      |  |   4.2 hrs   |  |
|  |  Pending   |  |  Overdue   |  | This Week  |  |  Avg Time   |  |
|  +------------+  +------------+  +------------+  +-------------+  |
|                                                                   |
+------------------------------------------------------------------+
| [My Reviews (5)]  [Requested by Me (3)]  [All Team (15)]          |
+------------------------------------------------------------------+
| [Search reviews...]           [Filter (2)] [Sort: Due Date v]     |
+------------------------------------------------------------------+
|                                                                   |
| [Filter: Overdue Only, Priority: High]  [Clear Filters]           |
|                                                                   |
+------------------------------------------------------------------+
|                                                                   |
|  +--------------------------------------------------------------+ |
|  | [!] OVERDUE | Marketing Brief Q1                             | |
|  |     Priority: HIGH                                           | |
|  |     Requested by Jane Smith · Due: Jan 20 (3 days overdue)   | |
|  |     [Avatar][Avatar][Avatar] 3 reviewers · 2/3 reviewed      | |
|  |     5 comments (2 unresolved)                                | |
|  |                                                              | |
|  |     [Open Document]  [Send Reminder]  [Approve]  [Changes]   | |
|  +--------------------------------------------------------------+ |
|                                                                   |
|  +--------------------------------------------------------------+ |
|  | [HIGH] Technical Documentation Update                        | |
|  |     Priority: HIGH                                           | |
|  |     Requested by John Doe · Due: Jan 28 (3 days)             | |
|  |     [Avatar][Avatar] 2 reviewers · 0/2 reviewed              | |
|  |     0 comments                                               | |
|  |                                                              | |
|  |     [Open Document]  [Send Reminder]  [Approve]  [Changes]   | |
|  +--------------------------------------------------------------+ |
|                                                                   |
|  +--------------------------------------------------------------+ |
|  | [NORMAL] API Reference Guide                                 | |
|  |     Priority: Normal                                         | |
|  |     Requested by Sarah Chen · Due: Feb 5 (11 days)           | |
|  |     [Avatar] 1 reviewer · Awaiting review                    | |
|  |     3 comments (0 unresolved)                                | |
|  |                                                              | |
|  |     [Open Document]  [Send Reminder]  [Approve]  [Changes]   | |
|  +--------------------------------------------------------------+ |
|                                                                   |
|  [Load More...]                                                   |
+------------------------------------------------------------------+
```

### 6.2 Metrics Panel

```text
+------------------------------------------------------------------+
|  Metrics Overview                                                 |
+------------------------------------------------------------------+
|                                                                   |
|  +------------+  +------------+  +------------+  +-------------+  |
|  |            |  |            |  |            |  |             |  |
|  |     8      |  |     2      |  |    12      |  |   4.2 hrs   |  |
|  |            |  |            |  |            |  |             |  |
|  |  Pending   |  |  Overdue   |  | Completed  |  |  Avg Time   |  |
|  |  Reviews   |  |  Reviews   |  | This Week  |  |  to Review  |  |
|  +------------+  +------------+  +------------+  +-------------+  |
|                                                                   |
+------------------------------------------------------------------+
```

### 6.3 Review Summary Card

```text
+--------------------------------------------------------------+
| [!] OVERDUE | Marketing Brief Q1 2026                         |
+--------------------------------------------------------------+
|     Priority: [HIGH]                                          |
|     Requested by: [Avatar] Jane Smith                         |
|     Due: January 20, 2026 (3 days overdue)                    |
|     Created: January 15, 2026                                 |
+--------------------------------------------------------------+
|     Reviewers:                                                |
|     [Avatar] John Doe        [APPROVED]                       |
|     [Avatar] Sarah Chen      [IN PROGRESS]                    |
|     [Avatar] Mike Johnson    [PENDING]                        |
|                                                               |
|     Progress: 1/3 reviewed                                    |
+--------------------------------------------------------------+
|     Comments: 5 total (2 unresolved)                          |
+--------------------------------------------------------------+
|                                                               |
|  [Open Document]  [Send Reminder]  [Approve]  [Request Changes]|
+--------------------------------------------------------------+
```

### 6.4 Filter Panel

```text
+------------------------------------------------------------------+
|  Filter Reviews                                            [X]   |
+------------------------------------------------------------------+
|                                                                   |
|  Search:       [Search by document title...              ]       |
|                                                                   |
|  Status:                                                          |
|  [x] Active   [ ] Completed   [ ] Cancelled                       |
|                                                                   |
|  Priority:                                                        |
|  [ ] Urgent  [x] High  [ ] Normal  [ ] Low                        |
|                                                                   |
|  Assignee:     [Anyone                              v]            |
|                                                                   |
|  Requester:    [Anyone                              v]            |
|                                                                   |
|  Due Date:                                                        |
|  [ ] Any Time                                                     |
|  [x] Overdue Only                                                 |
|  [ ] Due This Week                                                |
|  [ ] Due This Month                                               |
|  [ ] Custom Range: [____/____/____] to [____/____/____]           |
|                                                                   |
|                              [Clear All]   [Apply Filters]        |
+------------------------------------------------------------------+
```

### 6.5 Empty State

```text
+------------------------------------------------------------------+
|  Review Dashboard                                                 |
+------------------------------------------------------------------+
|                                                                   |
|                        [No Reviews Icon]                          |
|                                                                   |
|                    No pending reviews found                       |
|                                                                   |
|      You're all caught up! There are no reviews waiting for       |
|      your attention right now.                                    |
|                                                                   |
|                     [Request a Review]                            |
|                                                                   |
+------------------------------------------------------------------+
```

### 6.6 Component Styling

| Component | Theme Resource | Usage |
| :--- | :--- | :--- |
| Metric Card | `Brush.Surface.Secondary` | Card background |
| Metric Value | `Brush.Text.Primary` | Large number |
| Metric Label | `Brush.Text.Tertiary` | Caption text |
| Overdue Badge | `Brush.Alert.Danger` | Red background |
| High Priority | `Brush.Priority.High` | Orange badge |
| Tab Active | `Brush.Accent.Primary` | Selected tab |
| Tab Inactive | `Brush.Text.Secondary` | Unselected tab |
| Card Hover | `Brush.Surface.Hover` | Hover state |
| Filter Badge | `Brush.Accent.Secondary` | Active filter count |

---

## 7. Navigation Integration

### 7.1 Menu Registration

```csharp
/// <summary>
/// Registers the Review Dashboard in the navigation menu.
/// </summary>
public void RegisterNavigation(INavigationService nav, ILicenseContext license)
{
    if (!license.HasFeature(FeatureFlags.Collaboration.ReviewWorkflows))
    {
        return; // Don't show menu item for non-Teams users
    }

    nav.RegisterMenuItem(new NavigationMenuItem
    {
        Id = "ReviewDashboard",
        Title = "Review Dashboard",
        Icon = Icons.Reviews,
        Order = 50,
        Group = NavigationGroup.Collaboration,
        ViewType = typeof(ReviewDashboardView),
        BadgeProvider = () => GetPendingReviewCount()
    });
}

/// <summary>
/// Gets pending review count for navigation badge.
/// </summary>
private async Task<int?> GetPendingReviewCount()
{
    var userId = GetCurrentUserId();
    var pending = await _reviewService.GetPendingReviewsForUserAsync(userId);
    return pending.Count > 0 ? pending.Count : null;
}
```

### 7.2 Deep Linking

```csharp
/// <summary>
/// Handles deep links to the review dashboard.
/// </summary>
public void HandleDeepLink(string path, IDictionary<string, string> query)
{
    // Examples:
    // /dashboard/reviews
    // /dashboard/reviews?tab=requestedByMe
    // /dashboard/reviews?filter=overdue

    if (query.TryGetValue("tab", out var tab))
    {
        SelectedTab = tab switch
        {
            "myReviews" => DashboardTab.MyReviews,
            "requestedByMe" => DashboardTab.RequestedByMe,
            "allTeam" => DashboardTab.AllTeam,
            _ => DashboardTab.MyReviews
        };
    }

    if (query.TryGetValue("filter", out var filter))
    {
        CurrentFilter = filter switch
        {
            "overdue" => new ReviewDashboardFilter { ShowOverdueOnly = true },
            "urgent" => new ReviewDashboardFilter { Priority = ReviewPriority.Urgent },
            "high" => new ReviewDashboardFilter { Priority = ReviewPriority.High },
            _ => new ReviewDashboardFilter()
        };
    }

    _ = RefreshAsync();
}
```

---

## 8. Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `R` | Refresh dashboard |
| `1` | Switch to My Reviews tab |
| `2` | Switch to Requested by Me tab |
| `3` | Switch to All Team tab |
| `F` | Open filter panel |
| `Esc` | Close filter panel |
| `Enter` | Open selected review |
| `A` | Quick approve selected review |
| `C` | Request changes on selected review |

---

## 9. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Info | `"Dashboard loaded for user {UserId}: {PendingCount} pending reviews"` |
| Info | `"Quick action: {Action} on review {ReviewId}"` |
| Debug | `"Filter applied: {Filter}"` |
| Debug | `"Sort changed to {SortOption}"` |
| Warning | `"Dashboard refresh failed: {Error}"` |
| Error | `"Quick action failed for review {ReviewId}: {Error}"` |

---

## 10. Performance Considerations

### 10.1 Data Loading Strategy

```text
INITIAL LOAD:
1. Load metrics (aggregated query, fast)
2. Load first page of current tab (paginated, 20 items)
3. Background: Pre-fetch other tabs

ENRICHMENT:
- Batch load profiles for all reviewers
- Cache document titles
- Lazy load comment counts

PAGINATION:
- Virtual scrolling for large lists
- Load 20 items per page
- Infinite scroll with "Load More" button
```

### 10.2 Caching Strategy

```csharp
/// <summary>
/// Caches dashboard data with configurable expiry.
/// </summary>
public class DashboardCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan MetricsCacheDuration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ReviewsCacheDuration = TimeSpan.FromSeconds(30);

    public async Task<ReviewDashboardMetrics> GetMetricsAsync(
        Guid userId, Func<Task<ReviewDashboardMetrics>> factory)
    {
        var key = $"dashboard:metrics:{userId}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = MetricsCacheDuration;
            return await factory();
        });
    }

    public void InvalidateForUser(Guid userId)
    {
        _cache.Remove($"dashboard:metrics:{userId}");
        _cache.Remove($"dashboard:reviews:{userId}:*");
    }
}
```

---

## 11. Acceptance Criteria

### 11.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | User has pending reviews | Loading My Reviews | Shows pending reviews |
| 2 | Review is overdue | Viewing dashboard | Shows overdue indicator |
| 3 | Filter set to High priority | Applying filter | Only shows high priority |
| 4 | User clicks Approve | Quick action | Review status updated |
| 5 | User sends reminder | Quick action | Notification sent to reviewers |
| 6 | User clicks Open Document | Navigation | Opens document in editor |
| 7 | Multiple tabs exist | Switching tabs | Loads correct review list |
| 8 | 100 reviews exist | Scrolling | Virtual scroll performs well |

### 11.2 Performance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | 100 reviews in system | Initial load | Completes in < 1 second |
| 10 | Metrics calculation | Loading dashboard | Completes in < 200ms |
| 11 | Applying filter | Filter change | Updates in < 300ms |
| 12 | Switching tabs | Tab click | Loads in < 500ms |

---

## 12. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `ReviewSummary.cs` record | [ ] |
| 2 | `ReviewerSummary.cs` record | [ ] |
| 3 | `ReviewDashboardMetrics.cs` record | [ ] |
| 4 | `ReviewDashboardFilter.cs` record | [ ] |
| 5 | `ReviewDashboardViewModel.cs` | [ ] |
| 6 | `ReviewDashboardView.axaml` | [ ] |
| 7 | `MetricsPanel.axaml` component | [ ] |
| 8 | `ReviewSummaryCard.axaml` component | [ ] |
| 9 | `FilterPanel.axaml` component | [ ] |
| 10 | `EmptyState.axaml` component | [ ] |
| 11 | Navigation menu registration | [ ] |
| 12 | Keyboard shortcuts | [ ] |
| 13 | Deep linking support | [ ] |
| 14 | Dashboard cache service | [ ] |
| 15 | Unit tests | [ ] |
| 16 | Integration tests | [ ] |

---

## 13. Verification Commands

```bash
# Run dashboard tests
dotnet test --filter "Version=v0.9.3d" --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ReviewDashboardViewModelTests"

# Run UI component tests
dotnet test --filter "FullyQualifiedName~ReviewSummaryCardTests"

# Performance test
dotnet test --filter "Category=Performance&Version=v0.9.3d"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
