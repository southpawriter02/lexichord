# LCS-DES-047-KG-e: Entity List View

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-047-KG-e |
| **Feature ID** | KG-047e |
| **Feature Name** | Entity List View |
| **Target Version** | v0.4.7e |
| **Module Scope** | `Lexichord.Modules.Knowledge.UI` |
| **Swimlane** | UI |
| **License Tier** | WriterPro (read), Teams (full) |
| **Feature Gate Key** | `knowledge.browser.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need a way to browse and filter extracted entities in the Knowledge Graph. The Entity List View provides a filterable, sortable list of all entities with key metadata like type, confidence, and mention count.

### 2.2 The Proposed Solution

Implement `EntityListView` as a WPF UserControl:

- **Virtual Scrolling**: Handle large entity counts efficiently.
- **Filtering**: Filter by type, confidence threshold, and source document.
- **Sorting**: Sort by name, type, confidence, or mention count.
- **Selection**: Select entities to view details in adjacent panel.
- **Icons**: Type-specific icons from Schema Registry.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.4.5-KG: `IGraphRepository` — Entity queries
- v0.4.5f: `ISchemaRegistry` — Type metadata (icons, colors)
- v0.1.1b: `IRegionManager` — Panel registration

**NuGet Packages:**
- `CommunityToolkit.Mvvm` — MVVM infrastructure

### 3.2 Module Placement

```
Lexichord.Modules.Knowledge.UI/
├── Views/
│   ├── EntityListView.xaml
│   └── EntityListView.xaml.cs
├── ViewModels/
│   ├── EntityListViewModel.cs
│   └── EntityListItemViewModel.cs
├── Converters/
│   └── ConfidenceToColorConverter.cs
```

---

## 4. Data Contract (The API)

### 4.1 ViewModel

```csharp
namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// ViewModel for Entity List View.
/// </summary>
public partial class EntityListViewModel : ViewModelBase
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILogger<EntityListViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredEntities))]
    private string? _searchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredEntities))]
    private string? _typeFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredEntities))]
    private float _minConfidenceFilter = 0.0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredEntities))]
    private Guid? _documentFilter;

    [ObservableProperty]
    private EntityListItemViewModel? _selectedEntity;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _sortProperty = "Name";

    [ObservableProperty]
    private bool _sortDescending;

    public ObservableCollection<EntityListItemViewModel> Entities { get; } = new();

    public ICollectionView FilteredEntities { get; }

    public IReadOnlyList<string> AvailableTypes { get; private set; } = Array.Empty<string>();

    public EntityListViewModel(
        IGraphRepository graphRepository,
        ISchemaRegistry schemaRegistry,
        ILogger<EntityListViewModel> logger)
    {
        _graphRepository = graphRepository;
        _schemaRegistry = schemaRegistry;
        _logger = logger;

        FilteredEntities = CollectionViewSource.GetDefaultView(Entities);
        FilteredEntities.Filter = ApplyFilters;

        // Enable live sorting
        if (FilteredEntities is ListCollectionView lcv)
        {
            lcv.IsLiveSorting = true;
            lcv.LiveSortingProperties.Add(nameof(EntityListItemViewModel.Name));
        }
    }

    [RelayCommand]
    private async Task LoadEntitiesAsync()
    {
        IsLoading = true;

        try
        {
            var entities = await _graphRepository.GetAllEntitiesAsync();

            Entities.Clear();

            foreach (var entity in entities)
            {
                var schema = _schemaRegistry.EntityTypes.GetValueOrDefault(entity.Type);
                var item = new EntityListItemViewModel
                {
                    Id = entity.Id,
                    Type = entity.Type,
                    Name = entity.Name,
                    Confidence = GetConfidence(entity),
                    MentionCount = entity.SourceDocuments.Count,
                    RelationshipCount = await _graphRepository.GetRelationshipCountAsync(entity.Id),
                    Icon = schema?.Icon ?? "📦",
                    Color = schema?.Color ?? "#808080",
                    Entity = entity
                };
                Entities.Add(item);
            }

            TotalCount = Entities.Count;

            AvailableTypes = Entities
                .Select(e => e.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            OnPropertyChanged(nameof(AvailableTypes));

            _logger.LogInformation("Loaded {Count} entities", TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load entities");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = null;
        TypeFilter = null;
        MinConfidenceFilter = 0.0f;
        DocumentFilter = null;
    }

    [RelayCommand]
    private void RefreshList()
    {
        FilteredEntities.Refresh();
    }

    private bool ApplyFilters(object obj)
    {
        if (obj is not EntityListItemViewModel item)
            return false;

        // Type filter
        if (!string.IsNullOrEmpty(TypeFilter) && item.Type != TypeFilter)
            return false;

        // Confidence filter
        if (item.Confidence < MinConfidenceFilter)
            return false;

        // Document filter
        if (DocumentFilter.HasValue &&
            !item.Entity.SourceDocuments.Contains(DocumentFilter.Value))
            return false;

        // Search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            if (!item.Name.ToLowerInvariant().Contains(search) &&
                !item.Type.ToLowerInvariant().Contains(search))
                return false;
        }

        return true;
    }

    private static float GetConfidence(KnowledgeEntity entity)
    {
        return entity.Properties.TryGetValue("confidence", out var conf) &&
               conf is double d
            ? (float)d
            : 1.0f;
    }
}

/// <summary>
/// List item ViewModel for an entity.
/// </summary>
public partial class EntityListItemViewModel : ObservableObject
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public float Confidence { get; init; }
    public int MentionCount { get; init; }
    public int RelationshipCount { get; init; }
    public string Icon { get; init; } = "📦";
    public string Color { get; init; } = "#808080";
    public KnowledgeEntity Entity { get; init; } = null!;

    public string ConfidenceDisplay => $"{Confidence:P0}";
}
```

### 4.2 XAML View

```xml
<!-- EntityListView.xaml -->
<UserControl x:Class="Lexichord.Modules.Knowledge.UI.Views.EntityListView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:Lexichord.Modules.Knowledge.UI.ViewModels"
             xmlns:converters="clr-namespace:Lexichord.Modules.Knowledge.UI.Converters">

    <UserControl.Resources>
        <converters:ConfidenceToColorConverter x:Key="ConfidenceToColor"/>
    </UserControl.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Filters -->
        <Border Grid.Row="0" Padding="8" Background="{DynamicResource Surface1}">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <!-- Search -->
                    <TextBox Grid.Column="0"
                             Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                             PlaceholderText="Search entities..."
                             Margin="0,0,8,0"/>

                    <!-- Type Filter -->
                    <ComboBox Grid.Column="1"
                              ItemsSource="{Binding AvailableTypes}"
                              SelectedItem="{Binding TypeFilter}"
                              PlaceholderText="All Types"
                              Width="120"
                              Margin="0,0,8,0"/>

                    <!-- Clear Filters -->
                    <Button Grid.Column="2"
                            Command="{Binding ClearFiltersCommand}"
                            Content="Clear"
                            Style="{StaticResource SecondaryButton}"/>
                </Grid>

                <!-- Confidence Slider -->
                <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                    <TextBlock Text="Min Confidence:"
                               VerticalAlignment="Center"
                               Margin="0,0,8,0"/>
                    <Slider Minimum="0" Maximum="1"
                            Value="{Binding MinConfidenceFilter}"
                            Width="150"/>
                    <TextBlock Text="{Binding MinConfidenceFilter, StringFormat=P0}"
                               VerticalAlignment="Center"
                               Margin="8,0,0,0"/>
                </StackPanel>
            </StackPanel>
        </Border>

        <!-- Entity List -->
        <ListView Grid.Row="1"
                  ItemsSource="{Binding FilteredEntities}"
                  SelectedItem="{Binding SelectedEntity}"
                  VirtualizingPanel.IsVirtualizing="True"
                  VirtualizingPanel.VirtualizationMode="Recycling"
                  ScrollViewer.IsDeferredScrollingEnabled="True">

            <ListView.ItemTemplate>
                <DataTemplate DataType="{x:Type vm:EntityListItemViewModel}">
                    <Grid Margin="4">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>

                        <!-- Icon -->
                        <TextBlock Grid.Column="0"
                                   Text="{Binding Icon}"
                                   FontSize="16"
                                   VerticalAlignment="Center"
                                   Margin="0,0,8,0"/>

                        <!-- Name & Type -->
                        <StackPanel Grid.Column="1">
                            <TextBlock Text="{Binding Name}"
                                       FontWeight="Medium"/>
                            <TextBlock Text="{Binding Type}"
                                       FontSize="11"
                                       Foreground="{DynamicResource TextSecondary}"/>
                        </StackPanel>

                        <!-- Confidence -->
                        <Border Grid.Column="2"
                                Background="{Binding Confidence, Converter={StaticResource ConfidenceToColor}}"
                                CornerRadius="4"
                                Padding="6,2"
                                Margin="4,0">
                            <TextBlock Text="{Binding ConfidenceDisplay}"
                                       FontSize="11"/>
                        </Border>

                        <!-- Counts -->
                        <StackPanel Grid.Column="3"
                                    Orientation="Horizontal"
                                    Opacity="0.7">
                            <TextBlock Text="{Binding RelationshipCount}"
                                       ToolTip="Relationships"/>
                            <TextBlock Text="↔" Margin="2,0"/>
                            <TextBlock Text="{Binding MentionCount}"
                                       ToolTip="Mentions"/>
                            <TextBlock Text="📄" Margin="2,0,0,0"/>
                        </StackPanel>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <!-- Status Bar -->
        <Border Grid.Row="2" Padding="8,4" Background="{DynamicResource Surface2}">
            <StackPanel Orientation="Horizontal">
                <TextBlock>
                    <Run Text="Showing"/>
                    <Run Text="{Binding FilteredEntities.Count, Mode=OneWay}"/>
                    <Run Text="of"/>
                    <Run Text="{Binding TotalCount}"/>
                    <Run Text="entities"/>
                </TextBlock>

                <!-- Loading Indicator -->
                <ProgressBar IsIndeterminate="True"
                             Width="100"
                             Height="4"
                             Margin="16,0,0,0"
                             Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

## 5. Converters

```csharp
namespace Lexichord.Modules.Knowledge.UI.Converters;

/// <summary>
/// Converts confidence value (0-1) to a color.
/// </summary>
public class ConfidenceToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not float confidence)
            return Brushes.Gray;

        return confidence switch
        {
            >= 0.9f => new SolidColorBrush(Color.FromRgb(34, 197, 94)),  // Green
            >= 0.7f => new SolidColorBrush(Color.FromRgb(234, 179, 8)),  // Yellow
            >= 0.5f => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange
            _ => new SolidColorBrush(Color.FromRgb(239, 68, 68))         // Red
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

## 6. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7e")]
public class EntityListViewModelTests
{
    [Fact]
    public async Task LoadEntitiesAsync_PopulatesCollection()
    {
        // Arrange
        var repo = CreateMockRepository(entities: 50);
        var vm = new EntityListViewModel(repo, CreateMockSchema(), NullLogger<EntityListViewModel>.Instance);

        // Act
        await vm.LoadEntitiesCommand.ExecuteAsync(null);

        // Assert
        vm.Entities.Should().HaveCount(50);
        vm.TotalCount.Should().Be(50);
    }

    [Fact]
    public void TypeFilter_FiltersCorrectly()
    {
        // Arrange
        var vm = CreateViewModelWithEntities(
            ("Endpoint", 10),
            ("Parameter", 20),
            ("Response", 5));

        // Act
        vm.TypeFilter = "Endpoint";

        // Assert
        vm.FilteredEntities.Cast<EntityListItemViewModel>()
            .Should().OnlyContain(e => e.Type == "Endpoint");
    }

    [Fact]
    public void MinConfidenceFilter_FiltersCorrectly()
    {
        // Arrange
        var vm = CreateViewModelWithConfidences(0.3f, 0.5f, 0.7f, 0.9f);

        // Act
        vm.MinConfidenceFilter = 0.6f;

        // Assert
        vm.FilteredEntities.Cast<EntityListItemViewModel>()
            .Should().OnlyContain(e => e.Confidence >= 0.6f);
    }

    [Fact]
    public void ClearFilters_ResetsAllFilters()
    {
        // Arrange
        var vm = CreateViewModelWithEntities(("Endpoint", 10));
        vm.TypeFilter = "Endpoint";
        vm.MinConfidenceFilter = 0.5f;
        vm.SearchText = "test";

        // Act
        vm.ClearFiltersCommand.Execute(null);

        // Assert
        vm.TypeFilter.Should().BeNull();
        vm.MinConfidenceFilter.Should().Be(0);
        vm.SearchText.Should().BeNull();
    }
}
```

---

## 7. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Entity list loads and displays all entities. |
| 2 | Type filter shows only matching entities. |
| 3 | Confidence slider filters by minimum threshold. |
| 4 | Search filters by name or type. |
| 5 | Selecting entity updates detail panel. |
| 6 | Virtual scrolling handles 10,000+ entities. |
| 7 | Clear filters resets all filter values. |

---

## 8. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `EntityListView.xaml` | [ ] |
| 2 | `EntityListViewModel` | [ ] |
| 3 | `EntityListItemViewModel` | [ ] |
| 4 | `ConfidenceToColorConverter` | [ ] |
| 5 | Unit tests | [ ] |

---

## 9. Changelog Entry

```markdown
### Added (v0.4.7e)

- `EntityListView` for browsing Knowledge Graph entities
- Virtual scrolling for large entity counts
- Filtering by type, confidence, and search text
- Confidence color coding (green/yellow/orange/red)
```

---
