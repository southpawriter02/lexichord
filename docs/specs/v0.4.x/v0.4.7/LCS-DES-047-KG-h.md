# LCS-DES-047-KG-h: Relationship Viewer

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-047-KG-h |
| **Feature ID** | KG-047h |
| **Feature Name** | Relationship Viewer |
| **Target Version** | v0.4.7h |
| **Module Scope** | `Lexichord.Modules.Knowledge.UI` |
| **Swimlane** | UI |
| **License Tier** | WriterPro (tree), Teams (graph) |
| **Feature Gate Key** | `knowledge.browser.relationships` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need to visualize relationships between entities in the Knowledge Graph. This helps understand how concepts connect, identify missing relationships, and navigate the graph intuitively.

### 2.2 The Proposed Solution

Implement `RelationshipViewer` with two modes:

- **Tree View**: Hierarchical display of relationships from selected entity.
- **Graph View**: Interactive node-link diagram (Teams+ only).
- **Navigation**: Click nodes to navigate to entities.
- **Filtering**: Filter by relationship type.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.4.5-KG: `IGraphRepository` — Relationship queries
- v0.4.5f: `ISchemaRegistry` — Relationship type metadata
- v0.4.7e: `EntityListViewModel` — Selection binding

**NuGet Packages:**
- `Microsoft.Msagl` — Graph layout (optional, for graph view)
- `CommunityToolkit.Mvvm`

### 3.2 Module Placement

```
Lexichord.Modules.Knowledge.UI/
├── Views/
│   ├── RelationshipTreeView.xaml
│   └── RelationshipGraphView.xaml
├── ViewModels/
│   ├── RelationshipViewerViewModel.cs
│   ├── RelationshipTreeNode.cs
│   └── GraphNodeViewModel.cs
```

---

## 4. Data Contract (The API)

### 4.1 ViewModel

```csharp
namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// ViewModel for Relationship Viewer.
/// </summary>
public partial class RelationshipViewerViewModel : ViewModelBase
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<RelationshipViewerViewModel> _logger;

    [ObservableProperty]
    private KnowledgeEntity? _selectedEntity;

    [ObservableProperty]
    private RelationshipViewMode _viewMode = RelationshipViewMode.Tree;

    [ObservableProperty]
    private string? _relationshipTypeFilter;

    [ObservableProperty]
    private int _maxDepth = 2;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canUseGraphView;

    public ObservableCollection<RelationshipTreeNode> TreeNodes { get; } = new();
    public ObservableCollection<GraphNodeViewModel> GraphNodes { get; } = new();
    public ObservableCollection<GraphEdgeViewModel> GraphEdges { get; } = new();

    public IReadOnlyList<string> AvailableRelationshipTypes { get; private set; } = Array.Empty<string>();

    partial void OnSelectedEntityChanged(KnowledgeEntity? value)
    {
        if (value != null)
        {
            _ = LoadRelationshipsAsync(value);
        }
        else
        {
            ClearView();
        }
    }

    partial void OnViewModeChanged(RelationshipViewMode value)
    {
        if (SelectedEntity != null)
        {
            _ = LoadRelationshipsAsync(SelectedEntity);
        }
    }

    public RelationshipViewerViewModel(
        IGraphRepository graphRepository,
        ISchemaRegistry schemaRegistry,
        ILicenseContext licenseContext,
        ILogger<RelationshipViewerViewModel> logger)
    {
        _graphRepository = graphRepository;
        _schemaRegistry = schemaRegistry;
        _licenseContext = licenseContext;
        _logger = logger;

        CanUseGraphView = _licenseContext.Tier >= LicenseTier.Teams;

        AvailableRelationshipTypes = _schemaRegistry.RelationshipTypes.Keys.ToList();
    }

    private async Task LoadRelationshipsAsync(KnowledgeEntity entity)
    {
        IsLoading = true;

        try
        {
            if (ViewMode == RelationshipViewMode.Tree)
            {
                await LoadTreeViewAsync(entity);
            }
            else
            {
                await LoadGraphViewAsync(entity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load relationships");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTreeViewAsync(KnowledgeEntity rootEntity)
    {
        TreeNodes.Clear();

        var visited = new HashSet<Guid> { rootEntity.Id };
        var rootNode = await CreateTreeNodeAsync(rootEntity, visited, 0);
        TreeNodes.Add(rootNode);
    }

    private async Task<RelationshipTreeNode> CreateTreeNodeAsync(
        KnowledgeEntity entity,
        HashSet<Guid> visited,
        int depth)
    {
        var schema = _schemaRegistry.EntityTypes.GetValueOrDefault(entity.Type);
        var node = new RelationshipTreeNode
        {
            EntityId = entity.Id,
            EntityName = entity.Name,
            EntityType = entity.Type,
            Icon = schema?.Icon ?? "📦",
            Depth = depth,
            IsExpanded = depth < 1
        };

        if (depth < MaxDepth)
        {
            var relationships = await _graphRepository.GetRelationshipsForEntityAsync(entity.Id);

            if (!string.IsNullOrEmpty(RelationshipTypeFilter))
            {
                relationships = relationships.Where(r => r.Type == RelationshipTypeFilter).ToList();
            }

            foreach (var rel in relationships)
            {
                var otherEntityId = rel.FromEntityId == entity.Id
                    ? rel.ToEntityId
                    : rel.FromEntityId;

                if (visited.Contains(otherEntityId))
                {
                    // Add cycle indicator
                    node.Children.Add(new RelationshipTreeNode
                    {
                        EntityId = otherEntityId,
                        EntityName = "(cycle)",
                        EntityType = rel.Type,
                        Icon = "🔄",
                        Depth = depth + 1,
                        IsCycleReference = true
                    });
                    continue;
                }

                var otherEntity = await _graphRepository.GetEntityByIdAsync(otherEntityId);
                if (otherEntity == null) continue;

                visited.Add(otherEntityId);

                var relSchema = _schemaRegistry.RelationshipTypes.GetValueOrDefault(rel.Type);
                var isOutgoing = rel.FromEntityId == entity.Id;

                var childNode = await CreateTreeNodeAsync(otherEntity, visited, depth + 1);
                childNode.RelationshipType = rel.Type;
                childNode.RelationshipDirection = isOutgoing ? "→" : "←";
                childNode.RelationshipIcon = relSchema?.Icon ?? "↔";

                node.Children.Add(childNode);
            }
        }

        return node;
    }

    private async Task LoadGraphViewAsync(KnowledgeEntity rootEntity)
    {
        GraphNodes.Clear();
        GraphEdges.Clear();

        var visited = new HashSet<Guid>();
        var queue = new Queue<(KnowledgeEntity Entity, int Depth)>();
        queue.Enqueue((rootEntity, 0));

        while (queue.Count > 0)
        {
            var (entity, depth) = queue.Dequeue();

            if (visited.Contains(entity.Id))
                continue;

            visited.Add(entity.Id);

            var schema = _schemaRegistry.EntityTypes.GetValueOrDefault(entity.Type);
            GraphNodes.Add(new GraphNodeViewModel
            {
                Id = entity.Id,
                Label = entity.Name,
                Type = entity.Type,
                Icon = schema?.Icon ?? "📦",
                Color = schema?.Color ?? "#808080",
                IsRoot = entity.Id == rootEntity.Id
            });

            if (depth < MaxDepth)
            {
                var relationships = await _graphRepository.GetRelationshipsForEntityAsync(entity.Id);

                if (!string.IsNullOrEmpty(RelationshipTypeFilter))
                {
                    relationships = relationships.Where(r => r.Type == RelationshipTypeFilter).ToList();
                }

                foreach (var rel in relationships)
                {
                    var otherEntityId = rel.FromEntityId == entity.Id
                        ? rel.ToEntityId
                        : rel.FromEntityId;

                    // Add edge
                    var relSchema = _schemaRegistry.RelationshipTypes.GetValueOrDefault(rel.Type);
                    GraphEdges.Add(new GraphEdgeViewModel
                    {
                        Id = rel.Id,
                        SourceId = rel.FromEntityId,
                        TargetId = rel.ToEntityId,
                        Type = rel.Type,
                        Label = relSchema?.DisplayName ?? rel.Type,
                        Color = relSchema?.Color ?? "#808080"
                    });

                    // Queue other entity for processing
                    if (!visited.Contains(otherEntityId))
                    {
                        var otherEntity = await _graphRepository.GetEntityByIdAsync(otherEntityId);
                        if (otherEntity != null)
                        {
                            queue.Enqueue((otherEntity, depth + 1));
                        }
                    }
                }
            }
        }

        _logger.LogDebug("Loaded graph with {Nodes} nodes and {Edges} edges",
            GraphNodes.Count, GraphEdges.Count);
    }

    [RelayCommand]
    private void NavigateToEntity(Guid entityId)
    {
        Messenger.Send(new NavigateToEntityRequest(entityId));
    }

    [RelayCommand]
    private void ExpandNode(RelationshipTreeNode node)
    {
        if (!node.IsExpanded && !node.IsCycleReference && node.Children.Count == 0)
        {
            _ = ExpandNodeAsync(node);
        }
        node.IsExpanded = !node.IsExpanded;
    }

    private async Task ExpandNodeAsync(RelationshipTreeNode node)
    {
        var entity = await _graphRepository.GetEntityByIdAsync(node.EntityId);
        if (entity == null) return;

        var visited = new HashSet<Guid> { node.EntityId };
        var expanded = await CreateTreeNodeAsync(entity, visited, node.Depth);

        foreach (var child in expanded.Children)
        {
            node.Children.Add(child);
        }
    }

    private void ClearView()
    {
        TreeNodes.Clear();
        GraphNodes.Clear();
        GraphEdges.Clear();
    }
}

public enum RelationshipViewMode { Tree, Graph }

/// <summary>
/// Node in the relationship tree.
/// </summary>
public partial class RelationshipTreeNode : ObservableObject
{
    public Guid EntityId { get; init; }
    public string EntityName { get; init; } = "";
    public string EntityType { get; init; } = "";
    public string Icon { get; init; } = "📦";
    public int Depth { get; init; }

    public string? RelationshipType { get; set; }
    public string? RelationshipDirection { get; set; }
    public string? RelationshipIcon { get; set; }

    public bool IsCycleReference { get; init; }

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<RelationshipTreeNode> Children { get; } = new();
}

/// <summary>
/// Node in the graph view.
/// </summary>
public record GraphNodeViewModel
{
    public Guid Id { get; init; }
    public string Label { get; init; } = "";
    public string Type { get; init; } = "";
    public string Icon { get; init; } = "📦";
    public string Color { get; init; } = "#808080";
    public bool IsRoot { get; init; }
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Edge in the graph view.
/// </summary>
public record GraphEdgeViewModel
{
    public Guid Id { get; init; }
    public Guid SourceId { get; init; }
    public Guid TargetId { get; init; }
    public string Type { get; init; } = "";
    public string Label { get; init; } = "";
    public string Color { get; init; } = "#808080";
}
```

### 4.2 Tree View XAML

```xml
<!-- RelationshipTreeView.xaml -->
<UserControl x:Class="Lexichord.Modules.Knowledge.UI.Views.RelationshipTreeView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="8">
            <ComboBox ItemsSource="{Binding AvailableRelationshipTypes}"
                      SelectedItem="{Binding RelationshipTypeFilter}"
                      PlaceholderText="All Types"
                      Width="150"
                      Margin="0,0,8,0"/>

            <TextBlock Text="Depth:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <Slider Minimum="1" Maximum="5"
                    Value="{Binding MaxDepth}"
                    Width="100"/>
            <TextBlock Text="{Binding MaxDepth}" VerticalAlignment="Center" Margin="4,0"/>

            <ToggleButton Content="Graph"
                          IsChecked="{Binding ViewMode, Converter={StaticResource EnumToBool}, ConverterParameter=Graph}"
                          IsEnabled="{Binding CanUseGraphView}"
                          Margin="16,0,0,0"/>
        </StackPanel>

        <!-- Tree View -->
        <TreeView Grid.Row="1"
                  ItemsSource="{Binding TreeNodes}">
            <TreeView.ItemTemplate>
                <HierarchicalDataTemplate ItemsSource="{Binding Children}">
                    <StackPanel Orientation="Horizontal" Margin="2">
                        <!-- Relationship indicator -->
                        <TextBlock Text="{Binding RelationshipDirection}"
                                   Visibility="{Binding RelationshipDirection, Converter={StaticResource NullToCollapsed}}"
                                   Foreground="{DynamicResource TextSecondary}"
                                   Margin="0,0,4,0"/>
                        <TextBlock Text="{Binding RelationshipType}"
                                   Visibility="{Binding RelationshipType, Converter={StaticResource NullToCollapsed}}"
                                   FontStyle="Italic"
                                   Foreground="{DynamicResource TextSecondary}"
                                   Margin="0,0,4,0"/>

                        <!-- Entity -->
                        <TextBlock Text="{Binding Icon}" Margin="0,0,4,0"/>
                        <TextBlock Text="{Binding EntityName}"
                                   FontWeight="Medium"
                                   Opacity="{Binding IsCycleReference, Converter={StaticResource CycleToOpacity}}"/>
                        <TextBlock Text="{Binding EntityType, StringFormat=' ({0})'}"
                                   Foreground="{DynamicResource TextSecondary}"
                                   FontSize="11"/>
                    </StackPanel>
                </HierarchicalDataTemplate>
            </TreeView.ItemTemplate>

            <TreeView.ItemContainerStyle>
                <Style TargetType="TreeViewItem">
                    <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}"/>
                    <EventSetter Event="MouseDoubleClick" Handler="TreeItem_DoubleClick"/>
                </Style>
            </TreeView.ItemContainerStyle>
        </TreeView>
    </Grid>
</UserControl>
```

---

## 5. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7h")]
public class RelationshipViewerViewModelTests
{
    [Fact]
    public async Task LoadTreeView_BuildsHierarchy()
    {
        // Arrange
        var vm = CreateViewModelWithRelationships(
            root: "Entity A",
            children: new[] { "Entity B", "Entity C" });

        // Act
        vm.SelectedEntity = CreateTestEntity("Entity A");
        await Task.Delay(100);

        // Assert
        vm.TreeNodes.Should().HaveCount(1);
        vm.TreeNodes[0].Children.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadTreeView_DetectsCycles()
    {
        // Arrange
        var vm = CreateViewModelWithCyclicRelationship();

        // Act
        vm.SelectedEntity = CreateTestEntity("Entity A");
        await Task.Delay(100);

        // Assert
        var cycleNode = vm.TreeNodes[0].Children
            .SelectMany(c => c.Children)
            .FirstOrDefault(n => n.IsCycleReference);
        cycleNode.Should().NotBeNull();
    }

    [Fact]
    public async Task RelationshipTypeFilter_FiltersRelationships()
    {
        // Arrange
        var vm = CreateViewModelWithMixedRelationships();
        vm.RelationshipTypeFilter = "CONTAINS";

        // Act
        vm.SelectedEntity = CreateTestEntity("Root");
        await Task.Delay(100);

        // Assert
        vm.TreeNodes[0].Children
            .Should().OnlyContain(c => c.RelationshipType == "CONTAINS");
    }

    [Fact]
    public void GraphView_RequiresTeamsLicense()
    {
        // Arrange
        var vmWriterPro = CreateViewModelWithLicense(LicenseTier.WriterPro);
        var vmTeams = CreateViewModelWithLicense(LicenseTier.Teams);

        // Assert
        vmWriterPro.CanUseGraphView.Should().BeFalse();
        vmTeams.CanUseGraphView.Should().BeTrue();
    }
}
```

---

## 6. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Tree view displays entity relationships hierarchically. |
| 2 | Cycle references are detected and indicated. |
| 3 | Relationship type filter works correctly. |
| 4 | Depth slider limits traversal depth. |
| 5 | Double-clicking node navigates to entity. |
| 6 | Graph view available for Teams+ only. |
| 7 | Graph nodes are laid out without overlap. |

---

## 7. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `RelationshipViewerViewModel` | [ ] |
| 2 | `RelationshipTreeNode` | [ ] |
| 3 | `GraphNodeViewModel` | [ ] |
| 4 | `RelationshipTreeView.xaml` | [ ] |
| 5 | `RelationshipGraphView.xaml` | [ ] |
| 6 | Unit tests | [ ] |

---

## 8. Changelog Entry

```markdown
### Added (v0.4.7h)

- `RelationshipViewer` for visualizing entity relationships
- Tree view for hierarchical relationship display
- Graph view for node-link diagrams (Teams+)
- Cycle detection in relationship traversal
- Relationship type filtering
```

---
