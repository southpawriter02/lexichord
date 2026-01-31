# LCS-DES-047-KG-f: Entity Detail View

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-047-KG-f |
| **Feature ID** | KG-047f |
| **Feature Name** | Entity Detail View |
| **Target Version** | v0.4.7f |
| **Module Scope** | `Lexichord.Modules.Knowledge.UI` |
| **Swimlane** | UI |
| **License Tier** | WriterPro (read), Teams (edit) |
| **Feature Gate Key** | `knowledge.browser.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

When users select an entity from the list, they need to see its full details including all properties, relationships to other entities, and source documents that mention it.

### 2.2 The Proposed Solution

Implement `EntityDetailView` displaying:

- **Header**: Entity name, type, icon, and confidence.
- **Properties Section**: All entity properties in key-value format.
- **Relationships Section**: Linked entities with relationship types.
- **Source Documents**: Documents where this entity is mentioned.
- **Actions**: Edit, merge, delete, navigate to source.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.4.5-KG: `IGraphRepository` — Entity and relationship queries
- v0.4.5f: `ISchemaRegistry` — Property metadata
- v0.4.7e: `EntityListViewModel` — Selection binding
- v0.1.3a: `IEditorService` — Navigate to document

### 3.2 Module Placement

```
Lexichord.Modules.Knowledge.UI/
├── Views/
│   └── EntityDetailView.xaml
├── ViewModels/
│   ├── EntityDetailViewModel.cs
│   ├── PropertyItemViewModel.cs
│   ├── RelationshipItemViewModel.cs
│   └── SourceDocumentItemViewModel.cs
```

---

## 4. Data Contract (The API)

### 4.1 ViewModel

```csharp
namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// ViewModel for Entity Detail View.
/// </summary>
public partial class EntityDetailViewModel : ViewModelBase
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly IDocumentRepository _documentRepository;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<EntityDetailViewModel> _logger;

    [ObservableProperty]
    private KnowledgeEntity? _entity;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _type = "";

    [ObservableProperty]
    private string _icon = "📦";

    [ObservableProperty]
    private float _confidence;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canEdit;

    public ObservableCollection<PropertyItemViewModel> Properties { get; } = new();
    public ObservableCollection<RelationshipItemViewModel> Relationships { get; } = new();
    public ObservableCollection<SourceDocumentItemViewModel> SourceDocuments { get; } = new();

    partial void OnEntityChanged(KnowledgeEntity? value)
    {
        if (value != null)
        {
            _ = LoadEntityDetailsAsync(value);
        }
        else
        {
            ClearDetails();
        }
    }

    public EntityDetailViewModel(
        IGraphRepository graphRepository,
        ISchemaRegistry schemaRegistry,
        IDocumentRepository documentRepository,
        IEditorService editorService,
        ILicenseContext licenseContext,
        ILogger<EntityDetailViewModel> logger)
    {
        _graphRepository = graphRepository;
        _schemaRegistry = schemaRegistry;
        _documentRepository = documentRepository;
        _editorService = editorService;
        _licenseContext = licenseContext;
        _logger = logger;

        CanEdit = _licenseContext.Tier >= LicenseTier.Teams;
    }

    private async Task LoadEntityDetailsAsync(KnowledgeEntity entity)
    {
        IsLoading = true;

        try
        {
            // Basic info
            Name = entity.Name;
            Type = entity.Type;
            Confidence = GetConfidence(entity);

            var schema = _schemaRegistry.EntityTypes.GetValueOrDefault(entity.Type);
            Icon = schema?.Icon ?? "📦";

            // Properties
            Properties.Clear();
            foreach (var prop in entity.Properties)
            {
                var propSchema = schema?.Properties.GetValueOrDefault(prop.Key);
                Properties.Add(new PropertyItemViewModel
                {
                    Name = prop.Key,
                    Value = FormatValue(prop.Value),
                    Type = propSchema?.Type ?? "string",
                    Description = propSchema?.Description,
                    IsRequired = propSchema?.Required ?? false
                });
            }

            // Relationships
            Relationships.Clear();
            var relationships = await _graphRepository.GetRelationshipsForEntityAsync(entity.Id);
            foreach (var rel in relationships)
            {
                var isOutgoing = rel.FromEntityId == entity.Id;
                var otherEntityId = isOutgoing ? rel.ToEntityId : rel.FromEntityId;
                var otherEntity = await _graphRepository.GetEntityByIdAsync(otherEntityId);

                if (otherEntity != null)
                {
                    var relSchema = _schemaRegistry.RelationshipTypes.GetValueOrDefault(rel.Type);
                    Relationships.Add(new RelationshipItemViewModel
                    {
                        Id = rel.Id,
                        Type = rel.Type,
                        Direction = isOutgoing ? "→" : "←",
                        OtherEntityId = otherEntity.Id,
                        OtherEntityName = otherEntity.Name,
                        OtherEntityType = otherEntity.Type,
                        Icon = relSchema?.Icon ?? "↔"
                    });
                }
            }

            // Source Documents
            SourceDocuments.Clear();
            foreach (var docId in entity.SourceDocuments)
            {
                var doc = await _documentRepository.GetByIdAsync(docId);
                if (doc != null)
                {
                    var mentionCount = await _graphRepository.GetMentionCountAsync(entity.Id, docId);
                    SourceDocuments.Add(new SourceDocumentItemViewModel
                    {
                        DocumentId = doc.Id,
                        Title = doc.Title,
                        Path = doc.Path,
                        MentionCount = mentionCount
                    });
                }
            }

            _logger.LogDebug("Loaded details for entity {EntityId}", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load entity details");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToSourceAsync(SourceDocumentItemViewModel source)
    {
        await _editorService.OpenDocumentAsync(source.DocumentId);
        // TODO: Navigate to first mention of entity
    }

    [RelayCommand]
    private void NavigateToRelatedEntity(RelationshipItemViewModel relationship)
    {
        // Publish navigation request event
        Messenger.Send(new NavigateToEntityRequest(relationship.OtherEntityId));
    }

    [RelayCommand]
    private async Task CopyPropertyValueAsync(PropertyItemViewModel property)
    {
        await Clipboard.SetTextAsync(property.Value);
    }

    private void ClearDetails()
    {
        Name = "";
        Type = "";
        Icon = "📦";
        Confidence = 0;
        Properties.Clear();
        Relationships.Clear();
        SourceDocuments.Clear();
    }

    private static float GetConfidence(KnowledgeEntity entity)
    {
        return entity.Properties.TryGetValue("confidence", out var conf) && conf is double d
            ? (float)d
            : 1.0f;
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            null => "(null)",
            string s => s,
            IEnumerable<object> list => string.Join(", ", list),
            _ => value.ToString() ?? ""
        };
    }
}

/// <summary>
/// ViewModel for a property item.
/// </summary>
public record PropertyItemViewModel
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
    public string Type { get; init; } = "string";
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
}

/// <summary>
/// ViewModel for a relationship item.
/// </summary>
public record RelationshipItemViewModel
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public string Direction { get; init; } = "→";
    public Guid OtherEntityId { get; init; }
    public string OtherEntityName { get; init; } = "";
    public string OtherEntityType { get; init; } = "";
    public string Icon { get; init; } = "↔";
}

/// <summary>
/// ViewModel for a source document item.
/// </summary>
public record SourceDocumentItemViewModel
{
    public Guid DocumentId { get; init; }
    public string Title { get; init; } = "";
    public string Path { get; init; } = "";
    public int MentionCount { get; init; }
}
```

### 4.2 XAML View

```xml
<!-- EntityDetailView.xaml -->
<UserControl x:Class="Lexichord.Modules.Knowledge.UI.Views.EntityDetailView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Padding="12" Background="{DynamicResource Surface1}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Column="0"
                           Text="{Binding Icon}"
                           FontSize="32"
                           VerticalAlignment="Center"
                           Margin="0,0,12,0"/>

                <StackPanel Grid.Column="1" VerticalAlignment="Center">
                    <TextBlock Text="{Binding Name}"
                               FontSize="18"
                               FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding Type}"
                               Foreground="{DynamicResource TextSecondary}"/>
                </StackPanel>

                <Border Grid.Column="2"
                        Background="{Binding Confidence, Converter={StaticResource ConfidenceToColor}}"
                        CornerRadius="4"
                        Padding="8,4">
                    <TextBlock Text="{Binding Confidence, StringFormat=P0}"/>
                </Border>
            </Grid>
        </Border>

        <!-- Content -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="12">
                <!-- Properties Section -->
                <Expander Header="Properties" IsExpanded="True">
                    <ItemsControl ItemsSource="{Binding Properties}" Margin="0,8">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Grid Margin="0,4">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="120"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>

                                    <TextBlock Grid.Column="0"
                                               Text="{Binding Name}"
                                               FontWeight="Medium"
                                               ToolTip="{Binding Description}"/>

                                    <TextBlock Grid.Column="1"
                                               Text="{Binding Value}"
                                               TextTrimming="CharacterEllipsis"
                                               Margin="8,0"/>

                                    <Button Grid.Column="2"
                                            Command="{Binding DataContext.CopyPropertyValueCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                            CommandParameter="{Binding}"
                                            Content="📋"
                                            ToolTip="Copy value"
                                            Style="{StaticResource IconButton}"/>
                                </Grid>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </Expander>

                <!-- Relationships Section -->
                <Expander Header="{Binding Relationships.Count, StringFormat='Relationships ({0})'}"
                          IsExpanded="True"
                          Margin="0,12,0,0">
                    <ItemsControl ItemsSource="{Binding Relationships}" Margin="0,8">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Button Command="{Binding DataContext.NavigateToRelatedEntityCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource LinkButton}"
                                        HorizontalAlignment="Left"
                                        Margin="0,2">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Text="{Binding Direction}" Margin="0,0,4,0"/>
                                        <TextBlock Text="{Binding Type}" FontWeight="Medium"/>
                                        <TextBlock Text=": " Margin="0,0,4,0"/>
                                        <TextBlock Text="{Binding OtherEntityName}"
                                                   Foreground="{DynamicResource Primary}"/>
                                        <TextBlock Text="{Binding OtherEntityType, StringFormat=' ({0})'}"
                                                   Foreground="{DynamicResource TextSecondary}"
                                                   FontSize="11"/>
                                    </StackPanel>
                                </Button>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </Expander>

                <!-- Source Documents Section -->
                <Expander Header="{Binding SourceDocuments.Count, StringFormat='Source Documents ({0})'}"
                          IsExpanded="True"
                          Margin="0,12,0,0">
                    <ItemsControl ItemsSource="{Binding SourceDocuments}" Margin="0,8">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Button Command="{Binding DataContext.NavigateToSourceCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource LinkButton}"
                                        HorizontalAlignment="Left"
                                        Margin="0,2">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Text="📄" Margin="0,0,4,0"/>
                                        <TextBlock Text="{Binding Title}"
                                                   Foreground="{DynamicResource Primary}"/>
                                        <TextBlock Text="{Binding MentionCount, StringFormat=' ({0} mentions)'}"
                                                   Foreground="{DynamicResource TextSecondary}"
                                                   FontSize="11"/>
                                    </StackPanel>
                                </Button>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </Expander>
            </StackPanel>
        </ScrollViewer>

        <!-- Action Buttons -->
        <Border Grid.Row="2" Padding="12" Background="{DynamicResource Surface2}">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="Edit"
                        Command="{Binding EditEntityCommand}"
                        IsEnabled="{Binding CanEdit}"
                        Margin="0,0,8,0"/>
                <Button Content="Merge"
                        Command="{Binding MergeEntityCommand}"
                        IsEnabled="{Binding CanEdit}"
                        Margin="0,0,8,0"/>
                <Button Content="Delete"
                        Command="{Binding DeleteEntityCommand}"
                        IsEnabled="{Binding CanEdit}"
                        Style="{StaticResource DangerButton}"/>
            </StackPanel>
        </Border>

        <!-- Loading Overlay -->
        <Border Grid.RowSpan="3"
                Background="#80000000"
                Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}">
            <ProgressRing IsActive="True"/>
        </Border>
    </Grid>
</UserControl>
```

---

## 5. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7f")]
public class EntityDetailViewModelTests
{
    [Fact]
    public async Task OnEntityChanged_LoadsAllSections()
    {
        // Arrange
        var entity = CreateTestEntity(properties: 5, relationships: 3, documents: 2);
        var vm = CreateViewModel();

        // Act
        vm.Entity = entity;
        await Task.Delay(100); // Wait for async load

        // Assert
        vm.Properties.Should().HaveCount(5);
        vm.Relationships.Should().HaveCount(3);
        vm.SourceDocuments.Should().HaveCount(2);
    }

    [Fact]
    public void CanEdit_FalseForWriterPro()
    {
        // Arrange
        var vm = CreateViewModelWithLicense(LicenseTier.WriterPro);

        // Assert
        vm.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void CanEdit_TrueForTeams()
    {
        // Arrange
        var vm = CreateViewModelWithLicense(LicenseTier.Teams);

        // Assert
        vm.CanEdit.Should().BeTrue();
    }
}
```

---

## 6. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Entity header shows name, type, icon, and confidence. |
| 2 | Properties section displays all entity properties. |
| 3 | Relationships section shows linked entities. |
| 4 | Clicking relationship navigates to that entity. |
| 5 | Source documents section shows mention counts. |
| 6 | Clicking document opens it in editor. |
| 7 | Edit/Merge/Delete buttons respect license tier. |

---

## 7. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `EntityDetailView.xaml` | [ ] |
| 2 | `EntityDetailViewModel` | [ ] |
| 3 | `PropertyItemViewModel` | [ ] |
| 4 | `RelationshipItemViewModel` | [ ] |
| 5 | `SourceDocumentItemViewModel` | [ ] |
| 6 | Unit tests | [ ] |

---

## 8. Changelog Entry

```markdown
### Added (v0.4.7f)

- `EntityDetailView` for viewing entity details
- Properties, relationships, and source documents sections
- Navigation to related entities and source documents
- License-gated edit actions
```

---
