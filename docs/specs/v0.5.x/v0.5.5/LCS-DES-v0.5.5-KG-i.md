# LCS-DES-055-KG-i: Linking Review UI

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-055-KG-i |
| **Feature ID** | KG-055i |
| **Feature Name** | Linking Review UI |
| **Target Version** | v0.5.5i |
| **Module Scope** | `Lexichord.UI.Knowledge.LinkingReview` |
| **Swimlane** | User Interface |
| **License Tier** | WriterPro (view), Teams (review) |
| **Feature Gate Key** | `knowledge.linking.review.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Entity links with low or medium confidence need human review before being accepted. The **Linking Review UI** provides a streamlined interface for reviewers to accept, reject, or modify entity links, with the ability to process many pending links efficiently.

### 2.2 The Proposed Solution

Implement a review panel with:

- Queue of pending links sorted by document/confidence
- Side-by-side view of mention context and candidate entities
- One-click accept/reject with keyboard shortcuts
- Bulk review capabilities for similar mentions
- Feedback collection for improving the linker
- Statistics dashboard showing review progress

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.5g: `IEntityLinkingService` — Source of pending links
- v0.4.7f: `EntityDetailView` — Entity display component
- v0.4.7e: `EntityListView` — Entity selection component

**NuGet Packages:**
- `CommunityToolkit.Mvvm` — MVVM infrastructure
- `Microsoft.Toolkit.Uwp.UI` — UI utilities

### 3.2 Module Placement

```
Lexichord.UI/
├── Knowledge/
│   └── LinkingReview/
│       ├── LinkingReviewPanel.xaml
│       ├── LinkingReviewPanel.xaml.cs
│       ├── LinkingReviewViewModel.cs
│       ├── PendingLinkItem.cs
│       ├── LinkReviewDecision.cs
│       ├── ReviewQueueService.cs
│       └── Components/
│           ├── MentionContextView.xaml
│           ├── CandidateSelector.xaml
│           └── ReviewStatsCard.xaml
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Show tab in Index Manager
- **Fallback Experience:** WriterPro: view-only queue; Teams+: full review capabilities

---

## 4. Data Contract (The API)

### 4.1 Review Service Interface

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// Service for managing entity linking review queue.
/// </summary>
public interface ILinkingReviewService
{
    /// <summary>
    /// Gets pending links needing review.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Pending link items.</returns>
    Task<IReadOnlyList<PendingLinkItem>> GetPendingAsync(
        ReviewFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the count of pending reviews.
    /// </summary>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Submits a review decision.
    /// </summary>
    /// <param name="decision">The review decision.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubmitDecisionAsync(LinkReviewDecision decision, CancellationToken ct = default);

    /// <summary>
    /// Submits multiple review decisions in batch.
    /// </summary>
    Task SubmitDecisionsBatchAsync(
        IReadOnlyList<LinkReviewDecision> decisions,
        CancellationToken ct = default);

    /// <summary>
    /// Gets review statistics.
    /// </summary>
    Task<ReviewStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when the queue changes.
    /// </summary>
    event EventHandler<ReviewQueueChangedEventArgs>? QueueChanged;
}
```

### 4.2 PendingLinkItem

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// An entity link pending human review.
/// </summary>
public record PendingLinkItem
{
    /// <summary>Unique item ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The linked entity awaiting review.</summary>
    public required LinkedEntity LinkedEntity { get; init; }

    /// <summary>Document containing the mention.</summary>
    public Guid DocumentId { get; init; }

    /// <summary>Document title for display.</summary>
    public string? DocumentTitle { get; init; }

    /// <summary>Extended context around the mention.</summary>
    public string? ExtendedContext { get; init; }

    /// <summary>When the link was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Priority for review (higher = more urgent).</summary>
    public int Priority { get; init; }

    /// <summary>Whether this item is part of a group of similar mentions.</summary>
    public bool IsGrouped { get; init; }

    /// <summary>Group ID if grouped.</summary>
    public string? GroupId { get; init; }

    /// <summary>Count of similar mentions in group.</summary>
    public int GroupCount { get; init; }

    /// <summary>Suggested decision based on patterns.</summary>
    public ReviewSuggestion? Suggestion { get; init; }
}

/// <summary>
/// Suggested review decision based on patterns.
/// </summary>
public record ReviewSuggestion
{
    /// <summary>Suggested action.</summary>
    public ReviewAction SuggestedAction { get; init; }

    /// <summary>Suggested entity if accepting alternate.</summary>
    public Guid? SuggestedEntityId { get; init; }

    /// <summary>Reason for suggestion.</summary>
    public string? Reason { get; init; }

    /// <summary>Confidence in suggestion.</summary>
    public float Confidence { get; init; }
}
```

### 4.3 LinkReviewDecision

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// A decision made during link review.
/// </summary>
public record LinkReviewDecision
{
    /// <summary>ID of the pending link item.</summary>
    public required Guid PendingLinkId { get; init; }

    /// <summary>Action taken.</summary>
    public required ReviewAction Action { get; init; }

    /// <summary>Selected entity ID (for Accept/SelectAlternate).</summary>
    public Guid? SelectedEntityId { get; init; }

    /// <summary>New entity created (for CreateNew).</summary>
    public KnowledgeEntity? NewEntity { get; init; }

    /// <summary>Reason for decision (for Reject/Skip).</summary>
    public string? Reason { get; init; }

    /// <summary>Whether to apply this decision to all similar mentions.</summary>
    public bool ApplyToGroup { get; init; }

    /// <summary>Reviewer ID.</summary>
    public string? ReviewerId { get; init; }

    /// <summary>When the decision was made.</summary>
    public DateTimeOffset DecidedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Actions available during link review.
/// </summary>
public enum ReviewAction
{
    /// <summary>Accept the proposed link.</summary>
    Accept,

    /// <summary>Reject the link (unlink).</summary>
    Reject,

    /// <summary>Select a different candidate entity.</summary>
    SelectAlternate,

    /// <summary>Create a new entity for this mention.</summary>
    CreateNew,

    /// <summary>Skip (defer for later review).</summary>
    Skip,

    /// <summary>Mark as not an entity (false positive).</summary>
    NotAnEntity
}

/// <summary>
/// Filter criteria for review queue.
/// </summary>
public record ReviewFilter
{
    /// <summary>Filter by document.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Filter by entity type.</summary>
    public string? EntityType { get; init; }

    /// <summary>Minimum confidence (exclusive).</summary>
    public float? MinConfidence { get; init; }

    /// <summary>Maximum confidence (exclusive).</summary>
    public float? MaxConfidence { get; init; }

    /// <summary>Sort order.</summary>
    public ReviewSortOrder SortBy { get; init; } = ReviewSortOrder.Priority;

    /// <summary>Maximum items to return.</summary>
    public int Limit { get; init; } = 50;
}

public enum ReviewSortOrder
{
    Priority,
    ConfidenceAsc,
    ConfidenceDesc,
    CreatedAtAsc,
    CreatedAtDesc,
    DocumentOrder
}
```

### 4.4 Review Statistics

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// Review queue statistics.
/// </summary>
public record ReviewStats
{
    /// <summary>Total items pending review.</summary>
    public int PendingCount { get; init; }

    /// <summary>Items reviewed today.</summary>
    public int ReviewedToday { get; init; }

    /// <summary>Total items ever reviewed.</summary>
    public int TotalReviewed { get; init; }

    /// <summary>Acceptance rate.</summary>
    public float AcceptanceRate { get; init; }

    /// <summary>Average review time per item.</summary>
    public TimeSpan AverageReviewTime { get; init; }

    /// <summary>Breakdown by action.</summary>
    public IReadOnlyDictionary<ReviewAction, int>? ByAction { get; init; }

    /// <summary>Breakdown by entity type.</summary>
    public IReadOnlyDictionary<string, int>? ByEntityType { get; init; }

    /// <summary>Top reviewers.</summary>
    public IReadOnlyList<ReviewerStats>? TopReviewers { get; init; }
}

public record ReviewerStats
{
    public string ReviewerId { get; init; } = "";
    public string ReviewerName { get; init; } = "";
    public int ReviewCount { get; init; }
    public float AcceptanceRate { get; init; }
}
```

---

## 5. Implementation Logic

### 5.1 ViewModel

```csharp
namespace Lexichord.UI.Knowledge.LinkingReview;

/// <summary>
/// ViewModel for the linking review panel.
/// </summary>
public partial class LinkingReviewViewModel : ObservableObject
{
    private readonly ILinkingReviewService _reviewService;
    private readonly IGraphRepository _graphRepository;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private ObservableCollection<PendingLinkItem> _pendingItems = new();

    [ObservableProperty]
    private PendingLinkItem? _selectedItem;

    [ObservableProperty]
    private ReviewStats? _stats;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ReviewFilter _filter = new();

    [ObservableProperty]
    private bool _applyToGroup;

    public LinkingReviewViewModel(
        ILinkingReviewService reviewService,
        IGraphRepository graphRepository,
        IMessenger messenger)
    {
        _reviewService = reviewService;
        _graphRepository = graphRepository;
        _messenger = messenger;

        _reviewService.QueueChanged += OnQueueChanged;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _reviewService.GetPendingAsync(Filter);
            PendingItems = new ObservableCollection<PendingLinkItem>(items);

            if (PendingItems.Any() && SelectedItem == null)
            {
                SelectedItem = PendingItems.First();
            }

            Stats = await _reviewService.GetStatsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (SelectedItem == null) return;

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.Accept,
            SelectedEntityId = SelectedItem.LinkedEntity.ResolvedEntityId,
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        });
    }

    [RelayCommand]
    private async Task RejectAsync()
    {
        if (SelectedItem == null) return;

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.Reject,
            Reason = "Incorrect link",
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        });
    }

    [RelayCommand]
    private async Task SelectAlternateAsync(LinkCandidate candidate)
    {
        if (SelectedItem == null) return;

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.SelectAlternate,
            SelectedEntityId = candidate.EntityId,
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        });
    }

    [RelayCommand]
    private async Task CreateNewEntityAsync()
    {
        if (SelectedItem == null) return;

        // Show entity creation dialog
        var newEntity = await ShowCreateEntityDialogAsync(SelectedItem.LinkedEntity.Mention);

        if (newEntity != null)
        {
            await SubmitDecisionAsync(new LinkReviewDecision
            {
                PendingLinkId = SelectedItem.Id,
                Action = ReviewAction.CreateNew,
                NewEntity = newEntity
            });
        }
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        if (SelectedItem == null) return;

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.Skip
        });
    }

    [RelayCommand]
    private async Task MarkNotEntityAsync()
    {
        if (SelectedItem == null) return;

        await SubmitDecisionAsync(new LinkReviewDecision
        {
            PendingLinkId = SelectedItem.Id,
            Action = ReviewAction.NotAnEntity,
            Reason = "False positive - not an entity",
            ApplyToGroup = ApplyToGroup && SelectedItem.IsGrouped
        });
    }

    private async Task SubmitDecisionAsync(LinkReviewDecision decision)
    {
        await _reviewService.SubmitDecisionAsync(decision);

        // Remove from list and select next
        var index = PendingItems.IndexOf(SelectedItem!);
        PendingItems.Remove(SelectedItem!);

        if (PendingItems.Any())
        {
            SelectedItem = PendingItems[Math.Min(index, PendingItems.Count - 1)];
        }
        else
        {
            SelectedItem = null;
        }

        // Update stats
        Stats = await _reviewService.GetStatsAsync();
    }

    private void OnQueueChanged(object? sender, ReviewQueueChangedEventArgs e)
    {
        _ = LoadAsync();
    }

    private async Task<KnowledgeEntity?> ShowCreateEntityDialogAsync(EntityMention mention)
    {
        // Implementation would show a dialog for creating a new entity
        // Pre-populated with mention information
        throw new NotImplementedException();
    }
}
```

### 5.2 XAML View

```xml
<!-- LinkingReviewPanel.xaml -->
<UserControl x:Class="Lexichord.UI.Knowledge.LinkingReview.LinkingReviewPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:Lexichord.UI.Knowledge.LinkingReview">

    <UserControl.InputBindings>
        <KeyBinding Key="A" Modifiers="Ctrl" Command="{Binding AcceptCommand}" />
        <KeyBinding Key="R" Modifiers="Ctrl" Command="{Binding RejectCommand}" />
        <KeyBinding Key="S" Modifiers="Ctrl" Command="{Binding SkipCommand}" />
        <KeyBinding Key="N" Modifiers="Ctrl" Command="{Binding CreateNewEntityCommand}" />
    </UserControl.InputBindings>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="300" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="350" />
        </Grid.ColumnDefinitions>

        <!-- Left Panel: Queue -->
        <Border Grid.Column="0" BorderBrush="{DynamicResource BorderBrush}" BorderThickness="0,0,1,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>

                <!-- Stats Card -->
                <local:ReviewStatsCard Grid.Row="0" Stats="{Binding Stats}" Margin="8" />

                <!-- Filter Bar -->
                <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="8,0">
                    <ComboBox ItemsSource="{Binding EntityTypes}"
                              SelectedItem="{Binding Filter.EntityType}"
                              Width="120" Margin="0,0,8,0" />
                    <ComboBox ItemsSource="{Binding SortOptions}"
                              SelectedItem="{Binding Filter.SortBy}"
                              Width="120" />
                </StackPanel>

                <!-- Queue List -->
                <ListBox Grid.Row="2"
                         ItemsSource="{Binding PendingItems}"
                         SelectedItem="{Binding SelectedItem}"
                         Margin="8">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel>
                                <TextBlock Text="{Binding LinkedEntity.Mention.Value}"
                                           FontWeight="SemiBold" />
                                <TextBlock Text="{Binding LinkedEntity.Mention.EntityType}"
                                           FontSize="11" Opacity="0.7" />
                                <StackPanel Orientation="Horizontal">
                                    <TextBlock Text="Confidence: " FontSize="11" />
                                    <TextBlock Text="{Binding LinkedEntity.Confidence, StringFormat=P0}"
                                               FontSize="11" />
                                    <TextBlock Text=" • " FontSize="11" Visibility="{Binding IsGrouped, Converter={StaticResource BoolToVis}}" />
                                    <TextBlock Text="{Binding GroupCount, StringFormat='({0} similar)'}"
                                               FontSize="11"
                                               Visibility="{Binding IsGrouped, Converter={StaticResource BoolToVis}}" />
                                </StackPanel>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </Grid>
        </Border>

        <!-- Center Panel: Context and Decision -->
        <Grid Grid.Column="1" Margin="16">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- Mention Header -->
            <StackPanel Grid.Row="0" Margin="0,0,0,16">
                <TextBlock Text="Review Entity Link" Style="{StaticResource SubtitleTextBlock}" />
                <TextBlock Text="{Binding SelectedItem.DocumentTitle}"
                           FontSize="12" Opacity="0.7" />
            </StackPanel>

            <!-- Context View -->
            <local:MentionContextView Grid.Row="1"
                                      Mention="{Binding SelectedItem.LinkedEntity.Mention}"
                                      Context="{Binding SelectedItem.ExtendedContext}"
                                      ProposedEntity="{Binding SelectedItem.LinkedEntity.ResolvedEntity}" />

            <!-- Action Buttons -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,16,0,0">
                <Button Content="✓ Accept (Ctrl+A)"
                        Command="{Binding AcceptCommand}"
                        Style="{StaticResource PrimaryButton}"
                        Width="140" Margin="4" />
                <Button Content="✗ Reject (Ctrl+R)"
                        Command="{Binding RejectCommand}"
                        Style="{StaticResource DangerButton}"
                        Width="140" Margin="4" />
                <Button Content="Skip (Ctrl+S)"
                        Command="{Binding SkipCommand}"
                        Style="{StaticResource SecondaryButton}"
                        Width="100" Margin="4" />
            </StackPanel>

            <!-- Group Checkbox -->
            <CheckBox Grid.Row="2" Content="Apply to all similar mentions"
                      IsChecked="{Binding ApplyToGroup}"
                      HorizontalAlignment="Left"
                      Visibility="{Binding SelectedItem.IsGrouped, Converter={StaticResource BoolToVis}}"
                      Margin="0,16,0,0" />
        </Grid>

        <!-- Right Panel: Candidates -->
        <Border Grid.Column="2" BorderBrush="{DynamicResource BorderBrush}" BorderThickness="1,0,0,0">
            <Grid Margin="16">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" Text="Candidates" Style="{StaticResource SubtitleTextBlock}" />

                <!-- Candidate List -->
                <local:CandidateSelector Grid.Row="1"
                                         Candidates="{Binding SelectedItem.LinkedEntity.Candidates}"
                                         SelectedCandidate="{Binding SelectedItem.LinkedEntity.ResolvedEntity}"
                                         SelectCommand="{Binding SelectAlternateCommand}"
                                         Margin="0,8,0,0" />

                <!-- Additional Actions -->
                <StackPanel Grid.Row="2" Margin="0,16,0,0">
                    <Button Content="+ Create New Entity (Ctrl+N)"
                            Command="{Binding CreateNewEntityCommand}"
                            Style="{StaticResource SecondaryButton}"
                            HorizontalAlignment="Stretch" Margin="0,4" />
                    <Button Content="Not an Entity"
                            Command="{Binding MarkNotEntityCommand}"
                            Style="{StaticResource GhostButton}"
                            HorizontalAlignment="Stretch" Margin="0,4" />
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### 5.3 MentionContextView Component

```xml
<!-- MentionContextView.xaml -->
<UserControl x:Class="Lexichord.UI.Knowledge.LinkingReview.MentionContextView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Border Background="{DynamicResource CardBackground}"
            BorderBrush="{DynamicResource BorderBrush}"
            BorderThickness="1"
            CornerRadius="8"
            Padding="16">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- Mention Info -->
            <StackPanel Grid.Row="0" Margin="0,0,0,12">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="Mention: " FontWeight="SemiBold" />
                    <TextBlock Text="{Binding Mention.Value}"
                               Foreground="{DynamicResource AccentBrush}"
                               FontWeight="SemiBold" />
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="0,4,0,0">
                    <TextBlock Text="Type: " FontSize="12" Opacity="0.7" />
                    <TextBlock Text="{Binding Mention.EntityType}" FontSize="12" />
                    <TextBlock Text=" • Source: " FontSize="12" Opacity="0.7" />
                    <TextBlock Text="{Binding Mention.Source}" FontSize="12" />
                </StackPanel>
            </StackPanel>

            <!-- Context with Highlighted Mention -->
            <Border Grid.Row="1"
                    Background="{DynamicResource CodeBackground}"
                    CornerRadius="4"
                    Padding="12">
                <RichTextBox IsReadOnly="True"
                             Background="Transparent"
                             BorderThickness="0"
                             FontFamily="Consolas"
                             FontSize="13">
                    <!-- Content populated via code-behind with highlighting -->
                </RichTextBox>
            </Border>

            <!-- Proposed Link -->
            <Border Grid.Row="2"
                    Background="{DynamicResource HighlightBackground}"
                    CornerRadius="4"
                    Padding="12"
                    Margin="0,12,0,0"
                    Visibility="{Binding ProposedEntity, Converter={StaticResource NullToCollapsed}}">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>

                    <TextBlock Grid.Column="0" Text="→" FontSize="20" Margin="0,0,12,0"
                               VerticalAlignment="Center" />

                    <StackPanel Grid.Column="1">
                        <TextBlock Text="Proposed Link" FontSize="11" Opacity="0.7" />
                        <TextBlock Text="{Binding ProposedEntity.Name}"
                                   FontWeight="SemiBold" FontSize="14" />
                        <TextBlock Text="{Binding ProposedEntity.EntityType}"
                                   FontSize="12" Opacity="0.7" />
                    </StackPanel>
                </Grid>
            </Border>
        </Grid>
    </Border>
</UserControl>
```

---

## 6. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Linking Review UI Flow                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    Review Panel Layout                       ││
│  ├──────────────┬─────────────────────────┬────────────────────┤│
│  │              │                         │                    ││
│  │   QUEUE      │     CONTEXT VIEW        │   CANDIDATES       ││
│  │              │                         │                    ││
│  │ ┌──────────┐ │  ┌───────────────────┐  │ ┌────────────────┐ ││
│  │ │ Stats    │ │  │ Document Title    │  │ │ Candidate 1    │ ││
│  │ │ 47 pending│ │  │                   │  │ │ ★ GET /users   │ ││
│  │ │ 85% rate │ │  │ "...call the      │  │ │ Score: 0.75    │ ││
│  │ └──────────┘ │  │ [[users endpoint]]│  │ ├────────────────┤ ││
│  │              │  │ to retrieve..."   │  │ │ Candidate 2    │ ││
│  │ ┌──────────┐ │  │                   │  │ │ POST /users    │ ││
│  │ │ Filter   │ │  └───────────────────┘  │ │ Score: 0.72    │ ││
│  │ │ [Type ▼] │ │                         │ ├────────────────┤ ││
│  │ │ [Sort ▼] │ │  Proposed: GET /users   │ │ Candidate 3    │ ││
│  │ └──────────┘ │  Confidence: 65%        │ │ PUT /users     │ ││
│  │              │                         │ │ Score: 0.68    │ ││
│  │ ┌──────────┐ │  ┌───────────────────┐  │ └────────────────┘ ││
│  │ │▸ users   │ │  │ [✓ Accept] [✗ Rej]│  │                    ││
│  │ │  endpoint│ │  │     [Skip]        │  │ ┌────────────────┐ ││
│  │ │  65%     │ │  │                   │  │ │ + Create New   │ ││
│  │ ├──────────┤ │  │ ☐ Apply to group  │  │ │ Not an Entity  │ ││
│  │ │ limit    │ │  └───────────────────┘  │ └────────────────┘ ││
│  │ │ param    │ │                         │                    ││
│  │ │  58%     │ │                         │                    ││
│  │ └──────────┘ │                         │                    ││
│  │              │                         │                    ││
│  └──────────────┴─────────────────────────┴────────────────────┘│
│                                                                  │
│                     Keyboard Shortcuts                           │
│         Ctrl+A: Accept | Ctrl+R: Reject | Ctrl+S: Skip          │
│                     Ctrl+N: Create New                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5i")]
public class LinkingReviewViewModelTests
{
    private readonly Mock<ILinkingReviewService> _reviewServiceMock;
    private readonly LinkingReviewViewModel _viewModel;

    [Fact]
    public async Task LoadAsync_PopulatesPendingItems()
    {
        // Arrange
        var pending = new[]
        {
            CreatePendingItem("mention1", 0.65f),
            CreatePendingItem("mention2", 0.55f)
        };
        _reviewServiceMock.Setup(s => s.GetPendingAsync(It.IsAny<ReviewFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        // Act
        await _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PendingItems.Should().HaveCount(2);
        _viewModel.SelectedItem.Should().Be(pending[0]);
    }

    [Fact]
    public async Task AcceptCommand_SubmitsDecisionAndRemovesItem()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        _reviewServiceMock.Verify(s => s.SubmitDecisionAsync(
            It.Is<LinkReviewDecision>(d =>
                d.Action == ReviewAction.Accept &&
                d.PendingLinkId == pending.Id),
            It.IsAny<CancellationToken>()));
        _viewModel.PendingItems.Should().NotContain(pending);
    }

    [Fact]
    public async Task RejectCommand_SubmitsRejectDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        // Act
        await _viewModel.RejectCommand.ExecuteAsync(null);

        // Assert
        _reviewServiceMock.Verify(s => s.SubmitDecisionAsync(
            It.Is<LinkReviewDecision>(d => d.Action == ReviewAction.Reject),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AcceptCommand_WithApplyToGroup_AppliesGroupDecision()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f) with
        {
            IsGrouped = true,
            GroupId = "group1",
            GroupCount = 5
        };
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;
        _viewModel.ApplyToGroup = true;

        // Act
        await _viewModel.AcceptCommand.ExecuteAsync(null);

        // Assert
        _reviewServiceMock.Verify(s => s.SubmitDecisionAsync(
            It.Is<LinkReviewDecision>(d => d.ApplyToGroup == true),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SelectAlternateCommand_SubmitsWithSelectedEntity()
    {
        // Arrange
        var pending = CreatePendingItem("mention1", 0.65f);
        var alternateCandidate = new LinkCandidate
        {
            EntityId = Guid.NewGuid(),
            EntityName = "Alternate Entity",
            EntityType = "Endpoint"
        };
        _viewModel.PendingItems.Add(pending);
        _viewModel.SelectedItem = pending;

        // Act
        await _viewModel.SelectAlternateCommand.ExecuteAsync(alternateCandidate);

        // Assert
        _reviewServiceMock.Verify(s => s.SubmitDecisionAsync(
            It.Is<LinkReviewDecision>(d =>
                d.Action == ReviewAction.SelectAlternate &&
                d.SelectedEntityId == alternateCandidate.EntityId),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SkipCommand_MovesToNextItem()
    {
        // Arrange
        var pending1 = CreatePendingItem("mention1", 0.65f);
        var pending2 = CreatePendingItem("mention2", 0.55f);
        _viewModel.PendingItems.Add(pending1);
        _viewModel.PendingItems.Add(pending2);
        _viewModel.SelectedItem = pending1;

        // Act
        await _viewModel.SkipCommand.ExecuteAsync(null);

        // Assert
        _viewModel.SelectedItem.Should().Be(pending2);
    }

    [Fact]
    public void SelectedItem_WithNoItems_IsNull()
    {
        // Assert
        _viewModel.SelectedItem.Should().BeNull();
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Review queue displays pending links sorted by priority. |
| 2 | Context view shows mention highlighted in surrounding text. |
| 3 | Candidate list shows alternatives with scores. |
| 4 | Accept button confirms the proposed link. |
| 5 | Reject button removes the link. |
| 6 | Select Alternate allows choosing different candidate. |
| 7 | Create New opens entity creation dialog. |
| 8 | Apply to Group applies decision to similar mentions. |
| 9 | Keyboard shortcuts work (Ctrl+A, Ctrl+R, Ctrl+S). |
| 10 | Statistics card updates after each decision. |
| 11 | Filter by entity type works correctly. |
| 12 | Sort order affects queue display. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `ILinkingReviewService` interface | [ ] |
| 2 | `PendingLinkItem` record | [ ] |
| 3 | `LinkReviewDecision` record | [ ] |
| 4 | `ReviewStats` record | [ ] |
| 5 | `ReviewFilter` record | [ ] |
| 6 | `LinkingReviewViewModel` | [ ] |
| 7 | `LinkingReviewPanel.xaml` | [ ] |
| 8 | `MentionContextView` component | [ ] |
| 9 | `CandidateSelector` component | [ ] |
| 10 | `ReviewStatsCard` component | [ ] |
| 11 | Keyboard shortcuts | [ ] |
| 12 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.5i)

- `ILinkingReviewService` for managing review queue
- `LinkingReviewPanel` for human review of entity links
- `MentionContextView` with highlighted mention display
- `CandidateSelector` for choosing alternate entities
- Review actions: Accept, Reject, Select Alternate, Create New, Skip
- Group decision support for similar mentions
- Keyboard shortcuts for efficient review
- Review statistics dashboard
- Filter and sort capabilities for review queue
```

---
