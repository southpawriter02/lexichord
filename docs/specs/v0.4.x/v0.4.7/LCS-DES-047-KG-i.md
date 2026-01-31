# LCS-DES-047-KG-i: Axiom Viewer

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-047-KG-i |
| **Feature ID** | KG-047i |
| **Feature Name** | Axiom Viewer |
| **Target Version** | v0.4.7i |
| **Module Scope** | `Lexichord.Modules.Knowledge.UI` |
| **Swimlane** | UI |
| **License Tier** | WriterPro (read-only) |
| **Feature Gate Key** | `knowledge.browser.axioms` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need to view the axioms that govern their Knowledge Graph to understand validation rules and troubleshoot validation errors.

### 2.2 The Proposed Solution

Implement `AxiomViewer` as a tab in the Entity Browser:

- **List View**: All loaded axioms with filtering.
- **Detail View**: Axiom rules in human-readable format.
- **Validation Preview**: Test axiom against sample entity.
- **Source Indicator**: Show if axiom is built-in or custom.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.4.6-KG: `IAxiomStore` — Axiom queries
- v0.4.6e: `Axiom`, `AxiomRule` — Data model

### 3.2 Module Placement

```
Lexichord.Modules.Knowledge.UI/
├── Views/
│   └── AxiomViewerView.xaml
├── ViewModels/
│   ├── AxiomViewerViewModel.cs
│   └── AxiomItemViewModel.cs
```

---

## 4. Data Contract (The API)

### 4.1 ViewModel

```csharp
namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// ViewModel for Axiom Viewer.
/// </summary>
public partial class AxiomViewerViewModel : ViewModelBase
{
    private readonly IAxiomStore _axiomStore;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILogger<AxiomViewerViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAxioms))]
    private string? _searchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAxioms))]
    private string? _targetTypeFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAxioms))]
    private AxiomSeverity? _severityFilter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAxioms))]
    private bool _showBuiltInOnly;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAxioms))]
    private bool _showCustomOnly;

    [ObservableProperty]
    private AxiomItemViewModel? _selectedAxiom;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<AxiomItemViewModel> Axioms { get; } = new();

    public ICollectionView FilteredAxioms { get; }

    public IReadOnlyList<string> AvailableTargetTypes => _schemaRegistry.EntityTypes.Keys.ToList();

    public AxiomViewerViewModel(
        IAxiomStore axiomStore,
        ISchemaRegistry schemaRegistry,
        ILogger<AxiomViewerViewModel> logger)
    {
        _axiomStore = axiomStore;
        _schemaRegistry = schemaRegistry;
        _logger = logger;

        FilteredAxioms = CollectionViewSource.GetDefaultView(Axioms);
        FilteredAxioms.Filter = ApplyFilters;
        FilteredAxioms.SortDescriptions.Add(
            new SortDescription(nameof(AxiomItemViewModel.TargetType), ListSortDirection.Ascending));
        FilteredAxioms.SortDescriptions.Add(
            new SortDescription(nameof(AxiomItemViewModel.Name), ListSortDirection.Ascending));
    }

    [RelayCommand]
    private async Task LoadAxiomsAsync()
    {
        IsLoading = true;

        try
        {
            var axioms = _axiomStore.GetAllAxioms();

            Axioms.Clear();

            foreach (var axiom in axioms)
            {
                Axioms.Add(new AxiomItemViewModel
                {
                    Id = axiom.Id,
                    Name = axiom.Name,
                    Description = axiom.Description,
                    TargetType = axiom.TargetType,
                    TargetKind = axiom.TargetKind,
                    Severity = axiom.Severity,
                    RuleCount = axiom.Rules.Count,
                    IsBuiltIn = axiom.SourceFile?.StartsWith("builtin:") == true,
                    IsEnabled = axiom.IsEnabled,
                    Category = axiom.Category,
                    Tags = axiom.Tags,
                    Rules = axiom.Rules.Select(FormatRule).ToList(),
                    Axiom = axiom
                });
            }

            _logger.LogInformation("Loaded {Count} axioms", Axioms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load axioms");
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
        TargetTypeFilter = null;
        SeverityFilter = null;
        ShowBuiltInOnly = false;
        ShowCustomOnly = false;
    }

    private bool ApplyFilters(object obj)
    {
        if (obj is not AxiomItemViewModel item)
            return false;

        // Target type filter
        if (!string.IsNullOrEmpty(TargetTypeFilter) && item.TargetType != TargetTypeFilter)
            return false;

        // Severity filter
        if (SeverityFilter.HasValue && item.Severity != SeverityFilter.Value)
            return false;

        // Built-in / Custom filter
        if (ShowBuiltInOnly && !item.IsBuiltIn)
            return false;
        if (ShowCustomOnly && item.IsBuiltIn)
            return false;

        // Search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            if (!item.Name.ToLowerInvariant().Contains(search) &&
                !item.Id.ToLowerInvariant().Contains(search) &&
                !(item.Description?.ToLowerInvariant().Contains(search) ?? false))
                return false;
        }

        return true;
    }

    private static string FormatRule(AxiomRule rule)
    {
        var sb = new StringBuilder();

        if (rule.Property != null)
        {
            sb.Append($"Property '{rule.Property}' ");
        }
        else if (rule.Properties?.Any() == true)
        {
            sb.Append($"Properties [{string.Join(", ", rule.Properties)}] ");
        }

        sb.Append(rule.Constraint switch
        {
            AxiomConstraintType.Required => "must be present",
            AxiomConstraintType.OneOf => $"must be one of: {FormatValues(rule.Values)}",
            AxiomConstraintType.NotOneOf => $"must not be: {FormatValues(rule.Values)}",
            AxiomConstraintType.Range => FormatRange(rule.Min, rule.Max),
            AxiomConstraintType.Pattern => $"must match pattern: {rule.Pattern}",
            AxiomConstraintType.Cardinality => FormatCardinality(rule.MinCount, rule.MaxCount),
            AxiomConstraintType.NotBoth => "cannot both have values",
            AxiomConstraintType.RequiresTogether => "must all be present if any is",
            AxiomConstraintType.Equals => $"must equal: {FormatValues(rule.Values)}",
            AxiomConstraintType.NotEquals => $"must not equal: {FormatValues(rule.Values)}",
            AxiomConstraintType.Unique => "must be unique",
            AxiomConstraintType.ReferenceExists => $"must reference existing {rule.ReferenceType}",
            _ => rule.Constraint.ToString()
        });

        if (rule.When != null)
        {
            sb.Append($" (when {rule.When.Property} {FormatOperator(rule.When.Operator)} {rule.When.Value})");
        }

        return sb.ToString();
    }

    private static string FormatValues(IReadOnlyList<object>? values) =>
        values != null ? string.Join(", ", values) : "(none)";

    private static string FormatRange(object? min, object? max) =>
        (min, max) switch
        {
            (not null, not null) => $"must be between {min} and {max}",
            (not null, null) => $"must be >= {min}",
            (null, not null) => $"must be <= {max}",
            _ => "must be in range"
        };

    private static string FormatCardinality(int? min, int? max) =>
        (min, max) switch
        {
            (not null, not null) => $"must have {min}-{max} items",
            (not null, null) => $"must have at least {min} items",
            (null, not null) => $"must have at most {max} items",
            _ => "has cardinality constraint"
        };

    private static string FormatOperator(ConditionOperator op) =>
        op switch
        {
            ConditionOperator.Equals => "=",
            ConditionOperator.NotEquals => "≠",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.LessThan => "<",
            ConditionOperator.Contains => "contains",
            ConditionOperator.StartsWith => "starts with",
            ConditionOperator.EndsWith => "ends with",
            ConditionOperator.IsNull => "is null",
            ConditionOperator.IsNotNull => "is not null",
            _ => op.ToString()
        };
}

/// <summary>
/// View model for an axiom list item.
/// </summary>
public record AxiomItemViewModel
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string TargetType { get; init; } = "";
    public AxiomTargetKind TargetKind { get; init; }
    public AxiomSeverity Severity { get; init; }
    public int RuleCount { get; init; }
    public bool IsBuiltIn { get; init; }
    public bool IsEnabled { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Rules { get; init; } = Array.Empty<string>();
    public Axiom Axiom { get; init; } = null!;

    public string SeverityIcon => Severity switch
    {
        AxiomSeverity.Error => "⛔",
        AxiomSeverity.Warning => "⚠️",
        AxiomSeverity.Info => "ℹ️",
        _ => "❓"
    };

    public string SourceIcon => IsBuiltIn ? "📦" : "📝";
    public string SourceTooltip => IsBuiltIn ? "Built-in axiom" : "Custom axiom";
}
```

### 4.2 XAML View

```xml
<!-- AxiomViewerView.xaml -->
<UserControl x:Class="Lexichord.Modules.Knowledge.UI.Views.AxiomViewerView">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="2*"/>
        </Grid.ColumnDefinitions>

        <!-- Axiom List -->
        <Grid Grid.Column="0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Filters -->
            <StackPanel Grid.Row="0" Margin="8">
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                         PlaceholderText="Search axioms..."
                         Margin="0,0,0,8"/>

                <ComboBox ItemsSource="{Binding AvailableTargetTypes}"
                          SelectedItem="{Binding TargetTypeFilter}"
                          PlaceholderText="All Entity Types"
                          Margin="0,0,0,8"/>

                <StackPanel Orientation="Horizontal">
                    <CheckBox Content="Built-in only"
                              IsChecked="{Binding ShowBuiltInOnly}"
                              Margin="0,0,12,0"/>
                    <CheckBox Content="Custom only"
                              IsChecked="{Binding ShowCustomOnly}"/>
                </StackPanel>
            </StackPanel>

            <!-- List -->
            <ListView Grid.Row="1"
                      ItemsSource="{Binding FilteredAxioms}"
                      SelectedItem="{Binding SelectedAxiom}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <Grid Margin="4">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>

                            <TextBlock Grid.Column="0"
                                       Text="{Binding SeverityIcon}"
                                       VerticalAlignment="Center"
                                       Margin="0,0,8,0"/>

                            <StackPanel Grid.Column="1">
                                <TextBlock Text="{Binding Name}" FontWeight="Medium"/>
                                <TextBlock FontSize="11" Foreground="{DynamicResource TextSecondary}">
                                    <Run Text="{Binding TargetType}"/>
                                    <Run Text="·"/>
                                    <Run Text="{Binding RuleCount}"/>
                                    <Run Text="rules"/>
                                </TextBlock>
                            </StackPanel>

                            <TextBlock Grid.Column="2"
                                       Text="{Binding SourceIcon}"
                                       ToolTip="{Binding SourceTooltip}"
                                       VerticalAlignment="Center"/>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Grid>

        <!-- Axiom Detail -->
        <Border Grid.Column="1" BorderThickness="1,0,0,0" BorderBrush="{DynamicResource Border}">
            <ScrollViewer Visibility="{Binding SelectedAxiom, Converter={StaticResource NullToCollapsed}}">
                <StackPanel Margin="16">
                    <!-- Header -->
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
                        <TextBlock Text="{Binding SelectedAxiom.SeverityIcon}"
                                   FontSize="24"
                                   VerticalAlignment="Center"
                                   Margin="0,0,12,0"/>
                        <StackPanel>
                            <TextBlock Text="{Binding SelectedAxiom.Name}"
                                       FontSize="18"
                                       FontWeight="SemiBold"/>
                            <TextBlock Text="{Binding SelectedAxiom.Id}"
                                       FontFamily="Consolas"
                                       Foreground="{DynamicResource TextSecondary}"/>
                        </StackPanel>
                    </StackPanel>

                    <!-- Description -->
                    <TextBlock Text="{Binding SelectedAxiom.Description}"
                               TextWrapping="Wrap"
                               Margin="0,0,0,16"
                               Visibility="{Binding SelectedAxiom.Description, Converter={StaticResource NullToCollapsed}}"/>

                    <!-- Metadata -->
                    <Grid Margin="0,0,0,16">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition/>
                            <RowDefinition/>
                            <RowDefinition/>
                            <RowDefinition/>
                        </Grid.RowDefinitions>

                        <TextBlock Grid.Row="0" Grid.Column="0" Text="Target:" FontWeight="Medium" Margin="0,0,12,4"/>
                        <TextBlock Grid.Row="0" Grid.Column="1">
                            <Run Text="{Binding SelectedAxiom.TargetType}"/>
                            <Run Text="(" Foreground="{DynamicResource TextSecondary}"/>
                            <Run Text="{Binding SelectedAxiom.TargetKind}" Foreground="{DynamicResource TextSecondary}"/>
                            <Run Text=")" Foreground="{DynamicResource TextSecondary}"/>
                        </TextBlock>

                        <TextBlock Grid.Row="1" Grid.Column="0" Text="Severity:" FontWeight="Medium" Margin="0,0,12,4"/>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding SelectedAxiom.Severity}"/>

                        <TextBlock Grid.Row="2" Grid.Column="0" Text="Source:" FontWeight="Medium" Margin="0,0,12,4"/>
                        <TextBlock Grid.Row="2" Grid.Column="1"
                                   Text="{Binding SelectedAxiom.IsBuiltIn, Converter={StaticResource BoolToSource}}"/>

                        <TextBlock Grid.Row="3" Grid.Column="0" Text="Category:" FontWeight="Medium" Margin="0,0,12,4"
                                   Visibility="{Binding SelectedAxiom.Category, Converter={StaticResource NullToCollapsed}}"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding SelectedAxiom.Category}"
                                   Visibility="{Binding SelectedAxiom.Category, Converter={StaticResource NullToCollapsed}}"/>
                    </Grid>

                    <!-- Rules -->
                    <TextBlock Text="Rules" FontWeight="SemiBold" FontSize="14" Margin="0,0,0,8"/>
                    <ItemsControl ItemsSource="{Binding SelectedAxiom.Rules}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="{DynamicResource Surface2}"
                                        CornerRadius="4"
                                        Padding="8"
                                        Margin="0,0,0,4">
                                    <TextBlock Text="{Binding}" TextWrapping="Wrap"/>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>

                    <!-- Tags -->
                    <StackPanel Orientation="Horizontal"
                                Margin="0,16,0,0"
                                Visibility="{Binding SelectedAxiom.Tags.Count, Converter={StaticResource ZeroToCollapsed}}">
                        <TextBlock Text="Tags:" FontWeight="Medium" Margin="0,0,8,0"/>
                        <ItemsControl ItemsSource="{Binding SelectedAxiom.Tags}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <StackPanel Orientation="Horizontal"/>
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Background="{DynamicResource Surface3}"
                                            CornerRadius="4"
                                            Padding="6,2"
                                            Margin="0,0,4,0">
                                        <TextBlock Text="{Binding}" FontSize="11"/>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </StackPanel>
            </ScrollViewer>
        </Border>
    </Grid>
</UserControl>
```

---

## 5. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7i")]
public class AxiomViewerViewModelTests
{
    [Fact]
    public async Task LoadAxiomsAsync_LoadsAllAxioms()
    {
        // Arrange
        var store = CreateMockStore(axiomCount: 10);
        var vm = new AxiomViewerViewModel(store, CreateMockSchema(), NullLogger<AxiomViewerViewModel>.Instance);

        // Act
        await vm.LoadAxiomsCommand.ExecuteAsync(null);

        // Assert
        vm.Axioms.Should().HaveCount(10);
    }

    [Fact]
    public void TargetTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var vm = CreateViewModelWithAxioms(
            ("Endpoint", 5),
            ("Parameter", 3));

        // Act
        vm.TargetTypeFilter = "Endpoint";

        // Assert
        vm.FilteredAxioms.Cast<AxiomItemViewModel>()
            .Should().OnlyContain(a => a.TargetType == "Endpoint");
    }

    [Fact]
    public void ShowBuiltInOnly_FiltersCorrectly()
    {
        // Arrange
        var vm = CreateViewModelWithMixedSources();

        // Act
        vm.ShowBuiltInOnly = true;

        // Assert
        vm.FilteredAxioms.Cast<AxiomItemViewModel>()
            .Should().OnlyContain(a => a.IsBuiltIn);
    }

    [Fact]
    public void FormatRule_FormatsAllConstraintTypes()
    {
        // Arrange
        var rules = CreateAllConstraintTypeRules();

        // Act & Assert
        foreach (var rule in rules)
        {
            var formatted = AxiomViewerViewModel.FormatRule(rule);
            formatted.Should().NotBeNullOrEmpty();
        }
    }
}
```

---

## 6. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | All loaded axioms appear in list. |
| 2 | Target type filter works correctly. |
| 3 | Built-in vs custom filter works correctly. |
| 4 | Search filters by name, ID, and description. |
| 5 | Detail view shows all axiom metadata. |
| 6 | Rules are displayed in human-readable format. |
| 7 | Tags are displayed as chips. |

---

## 7. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `AxiomViewerViewModel` | [ ] |
| 2 | `AxiomItemViewModel` | [ ] |
| 3 | `AxiomViewerView.xaml` | [ ] |
| 4 | Rule formatting logic | [ ] |
| 5 | Unit tests | [ ] |

---

## 8. Changelog Entry

```markdown
### Added (v0.4.7i)

- `AxiomViewer` tab in Entity Browser
- Axiom list with filtering by type, source, and search
- Human-readable rule formatting
- Axiom metadata display (severity, category, tags)
```

---
