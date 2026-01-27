# LCS-DES-046a: Reference Panel View

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-046a                             |
| **Version**      | v0.4.6a                                  |
| **Title**        | Reference Panel View                     |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines `ReferenceView` and `ReferenceViewModel`, the main search panel UI registered in the Right shell region. The panel provides the user-facing interface for semantic search, displaying a search input, history dropdown, and virtualized results list.

### 1.2 Goals

- Define `ReferenceView.axaml` UserControl layout
- Implement `ReferenceViewModel` with search command
- Integrate with `ISemanticSearchService` from v0.4.5
- Display license-gated UI states
- Support keyboard navigation and accessibility
- Register panel in `ShellRegion.Right` via `IRegionManager`

### 1.3 Non-Goals

- Advanced filtering UI (future)
- Batch operations on results (future)
- Result pinning or bookmarking (future)

---

## 2. Design

### 2.1 ReferenceViewModel

```csharp
namespace Lexichord.Modules.RAG.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for the Reference Panel search interface.
/// </summary>
public partial class ReferenceViewModel : ViewModelBase
{
    private readonly ISemanticSearchService _searchService;
    private readonly ISearchHistoryService _historyService;
    private readonly SearchLicenseGuard _licenseGuard;
    private readonly IMediator _mediator;
    private readonly ILogger<ReferenceViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _showNoResults;

    [ObservableProperty]
    private bool _isLicensed = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private TimeSpan _lastSearchDuration;

    [ObservableProperty]
    private int _resultCount;

    public ObservableCollection<SearchResultItemViewModel> Results { get; } = new();

    public IReadOnlyList<string> SearchHistory => _historyService.RecentQueries;

    public ReferenceViewModel(
        ISemanticSearchService searchService,
        ISearchHistoryService historyService,
        SearchLicenseGuard licenseGuard,
        IMediator mediator,
        ILogger<ReferenceViewModel> logger)
    {
        _searchService = searchService;
        _historyService = historyService;
        _licenseGuard = licenseGuard;
        _mediator = mediator;
        _logger = logger;

        // Check initial license state
        _isLicensed = _licenseGuard.IsFeatureAvailable();
    }

    public bool CanSearch => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching && IsLicensed;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync(CancellationToken ct)
    {
        if (!_licenseGuard.IsFeatureAvailable())
        {
            IsLicensed = false;
            ErrorMessage = "Semantic search requires WriterPro license";
            return;
        }

        var query = SearchQuery.Trim();
        if (string.IsNullOrEmpty(query))
            return;

        try
        {
            IsSearching = true;
            ErrorMessage = null;
            ShowNoResults = false;

            _logger.LogDebug("Executing search for query: {Query}", query);

            var options = new SearchOptions
            {
                TopK = 20,
                MinScore = 0.6f,
                ExpandAbbreviations = true,
                UseCache = true
            };

            var result = await _searchService.SearchAsync(query, options, ct);

            // Update history
            _historyService.AddQuery(query);
            OnPropertyChanged(nameof(SearchHistory));

            // Map results to ViewModels
            Results.Clear();
            foreach (var hit in result.Hits)
            {
                Results.Add(new SearchResultItemViewModel(hit, query));
            }

            HasResults = Results.Count > 0;
            ShowNoResults = Results.Count == 0;
            ResultCount = Results.Count;
            LastSearchDuration = result.Duration;

            _logger.LogInformation(
                "Search completed: {Count} results in {Duration}ms",
                result.Hits.Count, result.Duration.TotalMilliseconds);
        }
        catch (FeatureNotLicensedException ex)
        {
            IsLicensed = false;
            ErrorMessage = $"Feature requires {ex.RequiredTier} license";
            _logger.LogWarning(ex, "Search blocked by license");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Search cancelled");
        }
        catch (Exception ex)
        {
            ErrorMessage = "Search failed. Please try again.";
            _logger.LogError(ex, "Search failed for query: {Query}", query);
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        Results.Clear();
        HasResults = false;
        ShowNoResults = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _historyService.ClearHistory();
        OnPropertyChanged(nameof(SearchHistory));
    }

    [RelayCommand]
    private void SelectHistoryItem(string query)
    {
        SearchQuery = query;
        SearchCommand.Execute(null);
    }

    partial void OnSearchQueryChanged(string value)
    {
        // Clear error when user starts typing
        if (!string.IsNullOrEmpty(value))
        {
            ErrorMessage = null;
        }
    }
}
```

### 2.2 ReferenceView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Modules.RAG.ViewModels"
             xmlns:local="using:Lexichord.Modules.RAG.Views"
             x:Class="Lexichord.Modules.RAG.Views.ReferenceView"
             x:DataType="vm:ReferenceViewModel">

    <UserControl.Styles>
        <Style Selector="TextBox.search-input">
            <Setter Property="Watermark" Value="Search documents..."/>
            <Setter Property="InnerLeftContent">
                <Template>
                    <PathIcon Data="{StaticResource SearchIcon}"
                              Width="16" Height="16"
                              Margin="8,0,0,0"/>
                </Template>
            </Setter>
        </Style>
    </UserControl.Styles>

    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Search Input Section -->
        <Border Grid.Row="0"
                Padding="12"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,0,0,1">
            <Grid RowDefinitions="Auto,Auto">
                <!-- Search Box with History Dropdown -->
                <Grid ColumnDefinitions="*,Auto">
                    <AutoCompleteBox Grid.Column="0"
                                     Text="{Binding SearchQuery, Mode=TwoWay}"
                                     ItemsSource="{Binding SearchHistory}"
                                     FilterMode="Contains"
                                     Watermark="Search documents..."
                                     IsEnabled="{Binding IsLicensed}"
                                     KeyDown="OnSearchKeyDown"/>

                    <Button Grid.Column="1"
                            Command="{Binding SearchCommand}"
                            ToolTip.Tip="Search (Enter)"
                            Margin="8,0,0,0"
                            IsEnabled="{Binding CanSearch}">
                        <PathIcon Data="{StaticResource SearchIcon}"
                                  Width="16" Height="16"/>
                    </Button>
                </Grid>

                <!-- License Warning -->
                <Border Grid.Row="1"
                        IsVisible="{Binding !IsLicensed}"
                        Background="{DynamicResource WarningBackground}"
                        CornerRadius="4"
                        Padding="8"
                        Margin="0,8,0,0">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <PathIcon Data="{StaticResource WarningIcon}"
                                  Width="16" Height="16"
                                  Foreground="{DynamicResource WarningForeground}"/>
                        <TextBlock Text="Semantic search requires WriterPro license"
                                   Foreground="{DynamicResource WarningForeground}"/>
                    </StackPanel>
                </Border>
            </Grid>
        </Border>

        <!-- Results Section -->
        <Grid Grid.Row="1">
            <!-- Loading Indicator -->
            <Border IsVisible="{Binding IsSearching}"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center">
                <StackPanel Spacing="12">
                    <ProgressRing IsIndeterminate="True"
                                  Width="32" Height="32"/>
                    <TextBlock Text="Searching..."
                               HorizontalAlignment="Center"
                               Foreground="{DynamicResource SecondaryTextBrush}"/>
                </StackPanel>
            </Border>

            <!-- No Results Message -->
            <Border IsVisible="{Binding ShowNoResults}"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center"
                    Padding="24">
                <StackPanel Spacing="8">
                    <PathIcon Data="{StaticResource EmptySearchIcon}"
                              Width="48" Height="48"
                              Foreground="{DynamicResource SecondaryTextBrush}"/>
                    <TextBlock Text="No results found"
                               FontSize="16"
                               HorizontalAlignment="Center"/>
                    <TextBlock Text="Try different keywords or check spelling"
                               Foreground="{DynamicResource SecondaryTextBrush}"
                               HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>

            <!-- Error Message -->
            <Border IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                    Background="{DynamicResource ErrorBackground}"
                    Padding="12"
                    Margin="12">
                <TextBlock Text="{Binding ErrorMessage}"
                           Foreground="{DynamicResource ErrorForeground}"
                           TextWrapping="Wrap"/>
            </Border>

            <!-- Results List (Virtualized) -->
            <ScrollViewer IsVisible="{Binding HasResults}">
                <ItemsControl ItemsSource="{Binding Results}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <VirtualizingStackPanel/>
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType="vm:SearchResultItemViewModel">
                            <local:SearchResultItemView DataContext="{Binding}"/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Grid>

        <!-- Status Bar -->
        <Border Grid.Row="2"
                Padding="12,8"
                BorderBrush="{DynamicResource BorderBrush}"
                BorderThickness="0,1,0,0"
                IsVisible="{Binding HasResults}">
            <Grid ColumnDefinitions="*,Auto">
                <TextBlock Grid.Column="0"
                           Text="{Binding ResultCount, StringFormat='{}{0} results'}"
                           Foreground="{DynamicResource SecondaryTextBrush}"/>
                <TextBlock Grid.Column="1"
                           Text="{Binding LastSearchDuration.TotalMilliseconds, StringFormat='{}{0:F0}ms'}"
                           Foreground="{DynamicResource SecondaryTextBrush}"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### 2.3 ReferenceView.axaml.cs

```csharp
namespace Lexichord.Modules.RAG.Views;

using Avalonia.Controls;
using Avalonia.Input;

public partial class ReferenceView : UserControl
{
    public ReferenceView()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ReferenceViewModel vm)
        {
            if (vm.SearchCommand.CanExecute(null))
            {
                vm.SearchCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}
```

### 2.4 Module Registration

```csharp
// In RAGModule.cs
public class RAGModule : IModule
{
    private readonly IRegionManager _regionManager;
    private readonly IContainerProvider _container;

    public RAGModule(IRegionManager regionManager, IContainerProvider container)
    {
        _regionManager = regionManager;
        _container = container;
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ViewModels
        containerRegistry.RegisterForNavigation<ReferenceView, ReferenceViewModel>();

        // Services
        containerRegistry.RegisterSingleton<ISearchHistoryService, SearchHistoryService>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // Register Reference Panel in Right region
        _regionManager.RegisterViewWithRegion(
            RegionNames.Right,
            typeof(ReferenceView));
    }
}
```

---

## 3. Accessibility

### 3.1 Keyboard Navigation

| Key | Action |
| :-- | :----- |
| `Enter` | Execute search |
| `Escape` | Clear search input |
| `↓` | Move to first result / next result |
| `↑` | Move to previous result |
| `Enter` (on result) | Navigate to source |
| `Tab` | Move between controls |

### 3.2 Screen Reader Support

```xml
<!-- ARIA-like properties for Avalonia -->
<AutoCompleteBox AutomationProperties.Name="Search documents"
                 AutomationProperties.HelpText="Enter search terms to find relevant content"/>

<ItemsControl AutomationProperties.Name="Search results"
              AutomationProperties.ItemType="Search result"/>
```

---

## 4. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6a")]
public class ReferenceViewModelTests
{
    private readonly Mock<ISemanticSearchService> _searchServiceMock;
    private readonly Mock<ISearchHistoryService> _historyServiceMock;
    private readonly Mock<SearchLicenseGuard> _licenseGuardMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ReferenceViewModel _sut;

    public ReferenceViewModelTests()
    {
        _searchServiceMock = new Mock<ISemanticSearchService>();
        _historyServiceMock = new Mock<ISearchHistoryService>();
        _licenseGuardMock = new Mock<SearchLicenseGuard>();
        _mediatorMock = new Mock<IMediator>();

        _historyServiceMock.Setup(h => h.RecentQueries)
            .Returns(new List<string>());

        _licenseGuardMock.Setup(g => g.IsFeatureAvailable())
            .Returns(true);

        _sut = new ReferenceViewModel(
            _searchServiceMock.Object,
            _historyServiceMock.Object,
            _licenseGuardMock.Object,
            _mediatorMock.Object,
            NullLogger<ReferenceViewModel>.Instance);
    }

    [Fact]
    public void CanSearch_EmptyQuery_ReturnsFalse()
    {
        _sut.SearchQuery = "";
        _sut.CanSearch.Should().BeFalse();
    }

    [Fact]
    public void CanSearch_WhitespaceQuery_ReturnsFalse()
    {
        _sut.SearchQuery = "   ";
        _sut.CanSearch.Should().BeFalse();
    }

    [Fact]
    public void CanSearch_ValidQuery_ReturnsTrue()
    {
        _sut.SearchQuery = "test query";
        _sut.CanSearch.Should().BeTrue();
    }

    [Fact]
    public void CanSearch_WhileSearching_ReturnsFalse()
    {
        _sut.SearchQuery = "test";
        _sut.IsSearching = true;
        _sut.CanSearch.Should().BeFalse();
    }

    [Fact]
    public void CanSearch_NotLicensed_ReturnsFalse()
    {
        _sut.SearchQuery = "test";
        _sut.IsLicensed = false;
        _sut.CanSearch.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_PopulatesResults()
    {
        // Arrange
        var mockResult = new SearchResult
        {
            Hits = new List<SearchHit>
            {
                CreateMockHit(0.95f),
                CreateMockHit(0.85f)
            },
            Duration = TimeSpan.FromMilliseconds(50),
            Query = "test"
        };

        _searchServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        _sut.SearchQuery = "test";

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.Results.Should().HaveCount(2);
        _sut.HasResults.Should().BeTrue();
        _sut.ShowNoResults.Should().BeFalse();
        _sut.ResultCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_NoResults_ShowsNoResultsMessage()
    {
        // Arrange
        var mockResult = new SearchResult
        {
            Hits = new List<SearchHit>(),
            Duration = TimeSpan.FromMilliseconds(30),
            Query = "nonexistent"
        };

        _searchServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        _sut.SearchQuery = "nonexistent";

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.Results.Should().BeEmpty();
        _sut.HasResults.Should().BeFalse();
        _sut.ShowNoResults.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_LicenseError_SetsErrorMessage()
    {
        // Arrange
        _searchServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeatureNotLicensedException("SemanticSearch", LicenseTier.WriterPro, LicenseTier.Core));

        _sut.SearchQuery = "test";

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _sut.IsLicensed.Should().BeFalse();
        _sut.ErrorMessage.Should().Contain("WriterPro");
    }

    [Fact]
    public async Task SearchAsync_AddsToHistory()
    {
        // Arrange
        var mockResult = new SearchResult { Hits = new List<SearchHit>(), Duration = TimeSpan.Zero, Query = "test" };
        _searchServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        _sut.SearchQuery = "my search query";

        // Act
        await _sut.SearchCommand.ExecuteAsync(null);

        // Assert
        _historyServiceMock.Verify(h => h.AddQuery("my search query"), Times.Once);
    }

    [Fact]
    public void ClearSearch_ResetsState()
    {
        // Arrange
        _sut.SearchQuery = "test";
        _sut.Results.Add(new SearchResultItemViewModel(CreateMockHit(0.9f), "test"));
        _sut.HasResults = true;

        // Act
        _sut.ClearSearchCommand.Execute(null);

        // Assert
        _sut.SearchQuery.Should().BeEmpty();
        _sut.Results.Should().BeEmpty();
        _sut.HasResults.Should().BeFalse();
    }

    [Fact]
    public void ClearHistory_CallsService()
    {
        _sut.ClearHistoryCommand.Execute(null);
        _historyServiceMock.Verify(h => h.ClearHistory(), Times.Once);
    }

    private static SearchHit CreateMockHit(float score)
    {
        return new SearchHit
        {
            Score = score,
            Chunk = new TextChunk
            {
                Content = "Sample content for testing",
                Metadata = new ChunkMetadata { Index = 0, StartOffset = 0, EndOffset = 25 }
            },
            Document = new DocumentInfo { Id = Guid.NewGuid(), Name = "TestDoc.md", Path = "/test/doc.md" }
        };
    }
}
```

---

## 5. Performance

### 5.1 Virtualization

The results list uses `VirtualizingStackPanel` to ensure smooth scrolling with large result sets:

```xml
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <VirtualizingStackPanel/>
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

### 5.2 Debounced Search (Optional Enhancement)

For type-ahead search, implement debouncing:

```csharp
private readonly Subject<string> _searchSubject = new();

public ReferenceViewModel(...)
{
    _searchSubject
        .Throttle(TimeSpan.FromMilliseconds(300))
        .DistinctUntilChanged()
        .Subscribe(async query => await SearchAsync(query, CancellationToken.None));
}

partial void OnSearchQueryChanged(string value)
{
    _searchSubject.OnNext(value);
}
```

---

## 6. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Debug | "Executing search for query: {Query}" | Before search |
| Information | "Search completed: {Count} results in {Duration}ms" | After search |
| Warning | "Search blocked by license" | License check failed |
| Error | "Search failed for query: {Query}" | Exception thrown |

---

## 7. File Locations

| File | Path |
| :--- | :--- |
| View | `src/Lexichord.Modules.RAG/Views/ReferenceView.axaml` |
| View code-behind | `src/Lexichord.Modules.RAG/Views/ReferenceView.axaml.cs` |
| ViewModel | `src/Lexichord.Modules.RAG/ViewModels/ReferenceViewModel.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/ViewModels/ReferenceViewModelTests.cs` |

---

## 8. Dependencies

| Dependency | Version | Purpose |
| :--------- | :------ | :------ |
| `CommunityToolkit.Mvvm` | 8.x | MVVM infrastructure |
| `Avalonia.Controls` | 11.x | UI controls |

---

## 9. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Search input accepts text and executes on Enter | [ ] |
| 2 | Search button triggers search command | [ ] |
| 3 | Loading indicator shows during search | [ ] |
| 4 | Results display in virtualized list | [ ] |
| 5 | No results message shows when empty | [ ] |
| 6 | Error messages display appropriately | [ ] |
| 7 | License warning shows for unlicensed users | [ ] |
| 8 | History dropdown shows recent queries | [ ] |
| 9 | Status bar shows result count and duration | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 10. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
