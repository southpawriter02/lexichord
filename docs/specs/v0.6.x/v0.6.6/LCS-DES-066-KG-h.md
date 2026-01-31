# LCS-DES-066-KG-h: Entity Citation Renderer

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-066-KG-h |
| **System Breakdown** | LCS-SBD-066-KG |
| **Version** | v0.6.6 |
| **Codename** | Entity Citation Renderer (CKVS Phase 3b) |
| **Estimated Hours** | 3 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Entity Citation Renderer** displays which Knowledge Graph entities informed the Co-pilot's response. It provides transparency into AI-generated content by showing the canonical sources backing each claim.

### 1.2 Key Responsibilities

- Render entity citations in Co-pilot responses
- Link citations to Knowledge Graph entities
- Show validation status alongside citations
- Provide hover details for cited entities
- Support different citation display formats

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Copilot/
      UI/
        EntityCitationRenderer.cs
        CitationPanel.xaml
        CitationPanel.xaml.cs
        CitationViewModel.cs
```

---

## 2. Interface Definitions

### 2.1 Entity Citation Renderer Interface

```csharp
namespace Lexichord.KnowledgeGraph.Copilot.UI;

/// <summary>
/// Renders entity citations for Co-pilot responses.
/// </summary>
public interface IEntityCitationRenderer
{
    /// <summary>
    /// Generates citation markup for a response.
    /// </summary>
    CitationMarkup GenerateCitations(
        ValidatedGenerationResult result,
        CitationOptions options);

    /// <summary>
    /// Gets citation details for an entity.
    /// </summary>
    EntityCitationDetail GetCitationDetail(
        KnowledgeEntity entity,
        ValidatedGenerationResult result);
}
```

---

## 3. Data Types

### 3.1 Citation Markup

```csharp
/// <summary>
/// Citation markup for rendering.
/// </summary>
public record CitationMarkup
{
    /// <summary>Cited entities.</summary>
    public required IReadOnlyList<EntityCitation> Citations { get; init; }

    /// <summary>Validation status text.</summary>
    public required string ValidationStatus { get; init; }

    /// <summary>Validation icon.</summary>
    public ValidationIcon Icon { get; init; }

    /// <summary>Formatted HTML/XAML for display.</summary>
    public string? FormattedMarkup { get; init; }
}

/// <summary>
/// A single entity citation.
/// </summary>
public record EntityCitation
{
    /// <summary>Entity ID.</summary>
    public Guid EntityId { get; init; }

    /// <summary>Entity type.</summary>
    public required string EntityType { get; init; }

    /// <summary>Entity name.</summary>
    public required string EntityName { get; init; }

    /// <summary>Display label.</summary>
    public required string DisplayLabel { get; init; }

    /// <summary>Citation confidence.</summary>
    public float Confidence { get; init; }

    /// <summary>Whether entity was verified.</summary>
    public bool IsVerified { get; init; }

    /// <summary>Icon for entity type.</summary>
    public string? TypeIcon { get; init; }
}

public enum ValidationIcon
{
    CheckMark,   // ✓
    Warning,     // ⚠
    Error,       // ✗
    Question     // ?
}
```

### 3.2 Citation Options

```csharp
/// <summary>
/// Options for citation rendering.
/// </summary>
public record CitationOptions
{
    /// <summary>Display format.</summary>
    public CitationFormat Format { get; init; } = CitationFormat.Compact;

    /// <summary>Maximum citations to show.</summary>
    public int MaxCitations { get; init; } = 10;

    /// <summary>Whether to show validation status.</summary>
    public bool ShowValidationStatus { get; init; } = true;

    /// <summary>Whether to show confidence scores.</summary>
    public bool ShowConfidence { get; init; } = false;

    /// <summary>Whether to group by entity type.</summary>
    public bool GroupByType { get; init; } = true;
}

public enum CitationFormat
{
    Compact,     // Icon + name inline
    Detailed,    // Full entity details
    TreeView,    // Hierarchical view
    Inline       // Within text
}
```

### 3.3 Entity Citation Detail

```csharp
/// <summary>
/// Detailed information for a citation.
/// </summary>
public record EntityCitationDetail
{
    /// <summary>The entity.</summary>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>Properties used in generation.</summary>
    public IReadOnlyDictionary<string, object?> UsedProperties { get; init; } =
        new Dictionary<string, object?>();

    /// <summary>Relationships cited.</summary>
    public IReadOnlyList<KnowledgeRelationship> CitedRelationships { get; init; } = [];

    /// <summary>Claims derived from this entity.</summary>
    public IReadOnlyList<Claim> DerivedClaims { get; init; } = [];

    /// <summary>Link to entity in graph browser.</summary>
    public string? BrowserLink { get; init; }
}
```

---

## 4. Implementation

### 4.1 Entity Citation Renderer

```csharp
public class EntityCitationRenderer : IEntityCitationRenderer
{
    private static readonly Dictionary<string, string> TypeIcons = new()
    {
        ["Endpoint"] = "🔗",
        ["Parameter"] = "📝",
        ["Response"] = "📤",
        ["Schema"] = "📋",
        ["Entity"] = "📦",
        ["Error"] = "⚠️"
    };

    public CitationMarkup GenerateCitations(
        ValidatedGenerationResult result,
        CitationOptions options)
    {
        var citations = new List<EntityCitation>();

        foreach (var entity in result.SourceEntities.Take(options.MaxCitations))
        {
            var isVerified = result.PostValidation.Findings
                .All(f => f.RelatedEntity?.Id != entity.Id ||
                          f.Severity != ValidationSeverity.Error);

            citations.Add(new EntityCitation
            {
                EntityId = entity.Id,
                EntityType = entity.Type,
                EntityName = entity.Name,
                DisplayLabel = FormatDisplayLabel(entity),
                Confidence = 1.0f, // From verified context
                IsVerified = isVerified,
                TypeIcon = TypeIcons.GetValueOrDefault(entity.Type, "📄")
            });
        }

        // Group by type if requested
        if (options.GroupByType)
        {
            citations = citations.OrderBy(c => c.EntityType).ThenBy(c => c.EntityName).ToList();
        }

        var validationStatus = FormatValidationStatus(result.PostValidation);
        var icon = GetValidationIcon(result.PostValidation.Status);

        return new CitationMarkup
        {
            Citations = citations,
            ValidationStatus = validationStatus,
            Icon = icon,
            FormattedMarkup = options.Format switch
            {
                CitationFormat.Compact => FormatCompact(citations, validationStatus, icon),
                CitationFormat.Detailed => FormatDetailed(citations, validationStatus, icon),
                CitationFormat.TreeView => FormatTreeView(citations, validationStatus, icon),
                _ => FormatCompact(citations, validationStatus, icon)
            }
        };
    }

    public EntityCitationDetail GetCitationDetail(
        KnowledgeEntity entity,
        ValidatedGenerationResult result)
    {
        // Find properties that were likely used
        var usedProperties = new Dictionary<string, object?>();
        foreach (var prop in entity.Properties)
        {
            if (result.Content.Contains(prop.Value?.ToString() ?? "", StringComparison.OrdinalIgnoreCase))
            {
                usedProperties[prop.Key] = prop.Value;
            }
        }

        // Find derived claims
        var derivedClaims = result.PostValidation is PostValidationResult pvr
            ? pvr.ExtractedClaims.Where(c => c.Subject.EntityId == entity.Id).ToList()
            : new List<Claim>();

        return new EntityCitationDetail
        {
            Entity = entity,
            UsedProperties = usedProperties,
            DerivedClaims = derivedClaims,
            BrowserLink = $"lexichord://graph/entity/{entity.Id}"
        };
    }

    private string FormatDisplayLabel(KnowledgeEntity entity)
    {
        return entity.Type switch
        {
            "Endpoint" => $"{entity.Properties.GetValueOrDefault("method")} {entity.Name}",
            "Parameter" => $"{entity.Name} ({entity.Properties.GetValueOrDefault("location")})",
            _ => entity.Name
        };
    }

    private string FormatValidationStatus(ValidationResult result)
    {
        return result.Status switch
        {
            ValidationStatus.Valid => "Validation passed",
            ValidationStatus.ValidWithWarnings => $"{result.WarningCount} warning(s)",
            ValidationStatus.Invalid => $"{result.ErrorCount} error(s)",
            _ => "Validation incomplete"
        };
    }

    private ValidationIcon GetValidationIcon(object status)
    {
        if (status is ValidationStatus vs)
        {
            return vs switch
            {
                ValidationStatus.Valid => ValidationIcon.CheckMark,
                ValidationStatus.ValidWithWarnings => ValidationIcon.Warning,
                ValidationStatus.Invalid => ValidationIcon.Error,
                _ => ValidationIcon.Question
            };
        }

        if (status is PostValidationStatus pvs)
        {
            return pvs switch
            {
                PostValidationStatus.Valid => ValidationIcon.CheckMark,
                PostValidationStatus.ValidWithWarnings => ValidationIcon.Warning,
                PostValidationStatus.Invalid => ValidationIcon.Error,
                _ => ValidationIcon.Question
            };
        }

        return ValidationIcon.Question;
    }

    private string FormatCompact(
        IReadOnlyList<EntityCitation> citations,
        string status,
        ValidationIcon icon)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📚 Based on:");

        foreach (var citation in citations)
        {
            var verifiedMark = citation.IsVerified ? "✓" : "?";
            sb.AppendLine($"├── {citation.TypeIcon} {citation.DisplayLabel} {verifiedMark}");
        }

        var iconChar = icon switch
        {
            ValidationIcon.CheckMark => "✓",
            ValidationIcon.Warning => "⚠",
            ValidationIcon.Error => "✗",
            _ => "?"
        };

        sb.AppendLine($"{iconChar} {status}");
        return sb.ToString();
    }

    private string FormatDetailed(
        IReadOnlyList<EntityCitation> citations,
        string status,
        ValidationIcon icon)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Knowledge Sources\n");

        var byType = citations.GroupBy(c => c.EntityType);
        foreach (var group in byType)
        {
            sb.AppendLine($"### {group.Key}");
            foreach (var citation in group)
            {
                sb.AppendLine($"- **{citation.EntityName}**");
                sb.AppendLine($"  - Verified: {(citation.IsVerified ? "Yes" : "No")}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatTreeView(
        IReadOnlyList<EntityCitation> citations,
        string status,
        ValidationIcon icon)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Knowledge Sources");

        var byType = citations.GroupBy(c => c.EntityType);
        var typeList = byType.ToList();

        for (int i = 0; i < typeList.Count; i++)
        {
            var group = typeList[i];
            var isLastGroup = i == typeList.Count - 1;
            var groupPrefix = isLastGroup ? "└── " : "├── ";

            sb.AppendLine($"{groupPrefix}{group.Key}");

            var items = group.ToList();
            for (int j = 0; j < items.Count; j++)
            {
                var item = items[j];
                var isLastItem = j == items.Count - 1;
                var itemPrefix = isLastGroup ? "    " : "│   ";
                itemPrefix += isLastItem ? "└── " : "├── ";

                sb.AppendLine($"{itemPrefix}{item.DisplayLabel}");
            }
        }

        return sb.ToString();
    }
}
```

### 4.2 Citation Panel XAML

```xml
<!-- CitationPanel.xaml -->
<UserControl x:Class="Lexichord.KnowledgeGraph.Copilot.UI.CitationPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Border BorderBrush="{DynamicResource BorderBrush}"
            BorderThickness="1"
            CornerRadius="4"
            Padding="8">
        <StackPanel>
            <!-- Header -->
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                <TextBlock Text="📚" FontSize="14" Margin="0,0,4,0"/>
                <TextBlock Text="Based on:" FontWeight="SemiBold"/>
            </StackPanel>

            <!-- Citations List -->
            <ItemsControl ItemsSource="{Binding Citations}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Margin="0,2">
                            <TextBlock Text="├── " Foreground="Gray"/>
                            <TextBlock Text="{Binding TypeIcon}" Margin="0,0,4,0"/>
                            <TextBlock Text="{Binding DisplayLabel}"/>
                            <TextBlock Text="{Binding VerifiedMark}"
                                       Foreground="{Binding VerifiedColor}"
                                       Margin="4,0,0,0"/>
                        </StackPanel>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>

            <!-- Validation Status -->
            <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <TextBlock Text="{Binding ValidationIcon}"
                           Foreground="{Binding ValidationColor}"/>
                <TextBlock Text="{Binding ValidationStatus}"
                           Margin="4,0,0,0"/>
            </StackPanel>
        </StackPanel>
    </Border>
</UserControl>
```

### 4.3 Citation View Model

```csharp
public partial class CitationViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<CitationItemViewModel> _citations = [];

    [ObservableProperty]
    private string _validationStatus = "";

    [ObservableProperty]
    private string _validationIcon = "?";

    [ObservableProperty]
    private Brush _validationColor = Brushes.Gray;

    public void Update(CitationMarkup markup)
    {
        Citations = markup.Citations.Select(c => new CitationItemViewModel
        {
            TypeIcon = c.TypeIcon ?? "📄",
            DisplayLabel = c.DisplayLabel,
            IsVerified = c.IsVerified,
            VerifiedMark = c.IsVerified ? "✓" : "?",
            VerifiedColor = c.IsVerified ? Brushes.Green : Brushes.Orange
        }).ToList();

        ValidationStatus = markup.ValidationStatus;
        ValidationIcon = markup.Icon switch
        {
            ValidationIcon.CheckMark => "✓",
            ValidationIcon.Warning => "⚠",
            ValidationIcon.Error => "✗",
            _ => "?"
        };
        ValidationColor = markup.Icon switch
        {
            ValidationIcon.CheckMark => Brushes.Green,
            ValidationIcon.Warning => Brushes.Orange,
            ValidationIcon.Error => Brushes.Red,
            _ => Brushes.Gray
        };
    }
}

public class CitationItemViewModel
{
    public string TypeIcon { get; init; } = "";
    public string DisplayLabel { get; init; } = "";
    public bool IsVerified { get; init; }
    public string VerifiedMark { get; init; } = "";
    public Brush VerifiedColor { get; init; } = Brushes.Gray;
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| No source entities | Show "No citations" message |
| Entity detail unavailable | Show basic info only |
| Invalid entity ID | Skip citation |

---

## 6. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `GenerateCitations_FormatsCompact` | Compact format works |
| `GenerateCitations_GroupsByType` | Type grouping works |
| `GetCitationDetail_FindsUsedProperties` | Property detection |
| `ValidationIcon_MatchesStatus` | Icons correct |

---

## 7. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Basic citations |
| Teams | Full citations + details |
| Enterprise | Full + custom formats |

---

## 8. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
