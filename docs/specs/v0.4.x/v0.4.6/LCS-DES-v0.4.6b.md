# LCS-DES-046b: Search Result Item

## Document Control

| Field            | Value                   |
| :--------------- | :---------------------- |
| **Document ID**  | LCS-DES-046b            |
| **Version**      | v0.4.6b                 |
| **Title**        | Search Result Item      |
| **Status**       | Draft                   |
| **Last Updated** | 2026-01-27              |
| **Owner**        | Lead Architect          |
| **Module**       | `Lexichord.Modules.RAG` |
| **License Tier** | WriterPro               |

---

## 1. Overview

### 1.1 Purpose

This specification defines `SearchResultItemView` and `SearchResultItemViewModel`, which display individual search results with document metadata, relevance score, content preview, and query term highlighting.

### 1.2 Goals

- Define `SearchResultItemView.axaml` UserControl layout
- Implement `SearchResultItemViewModel` for result data binding
- Display document name with navigation affordance
- Show relevance score as visual badge
- Render content preview with highlighted query terms
- Support double-click navigation to source

### 1.3 Non-Goals

- Multi-line preview expansion (future)
- Result comparison view (future)
- Context menu actions (future)

---

## 2. Design

### 2.1 SearchResultItemViewModel

```csharp
namespace Lexichord.Modules.RAG.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

/// <summary>
/// ViewModel for an individual search result item.
/// </summary>
public partial class SearchResultItemViewModel : ViewModelBase
{
    private readonly SearchHit _hit;
    private readonly string _query;

    [ObservableProperty]
    private bool _isSelected;

    public SearchResultItemViewModel(SearchHit hit, string query)
    {
        _hit = hit ?? throw new ArgumentNullException(nameof(hit));
        _query = query ?? string.Empty;
    }

    /// <summary>
    /// Gets the underlying search hit data.
    /// </summary>
    public SearchHit Hit => _hit;

    /// <summary>
    /// Gets the document display name.
    /// </summary>
    public string DocumentName => _hit.Document?.Name ?? "Unknown Document";

    /// <summary>
    /// Gets the document file path.
    /// </summary>
    public string DocumentPath => _hit.Document?.Path ?? string.Empty;

    /// <summary>
    /// Gets the similarity score (0.0 - 1.0).
    /// </summary>
    public float Score => _hit.Score;

    /// <summary>
    /// Gets the score as a percentage (0 - 100).
    /// </summary>
    public int ScorePercent => _hit.ScorePercent;

    /// <summary>
    /// Gets the score display text.
    /// </summary>
    public string ScoreDisplay => $"{ScorePercent}%";

    /// <summary>
    /// Gets the score badge color based on relevance.
    /// </summary>
    public string ScoreColor => Score switch
    {
        >= 0.9f => "HighRelevance",    // Green
        >= 0.8f => "MediumRelevance",  // Yellow
        >= 0.7f => "LowRelevance",     // Orange
        _ => "MinimalRelevance"         // Gray
    };

    /// <summary>
    /// Gets the content preview text (truncated).
    /// </summary>
    public string Preview => GetPreview();

    /// <summary>
    /// Gets the section/heading where this result appears.
    /// </summary>
    public string? Section => _hit.Chunk?.Metadata?.SectionHeading;

    /// <summary>
    /// Gets whether this result has section information.
    /// </summary>
    public bool HasSection => !string.IsNullOrEmpty(Section);

    /// <summary>
    /// Gets the character offset in the source document.
    /// </summary>
    public int SourceOffset => _hit.Chunk?.Metadata?.StartOffset ?? 0;

    /// <summary>
    /// Gets the length of the matched content.
    /// </summary>
    public int SourceLength => _hit.Chunk?.Metadata?.EndOffset - SourceOffset ?? 0;

    /// <summary>
    /// Gets the query terms for highlighting.
    /// </summary>
    public IReadOnlyList<string> QueryTerms => ParseQueryTerms(_query);

    private string GetPreview()
    {
        var content = _hit.Chunk?.Content ?? string.Empty;

        // Normalize whitespace
        content = Regex.Replace(content, @"\s+", " ").Trim();

        // Truncate to reasonable length
        const int maxLength = 200;
        if (content.Length > maxLength)
        {
            // Try to break at word boundary
            var truncated = content[..maxLength];
            var lastSpace = truncated.LastIndexOf(' ');
            if (lastSpace > maxLength - 30)
            {
                truncated = truncated[..lastSpace];
            }
            content = truncated + "...";
        }

        return content;
    }

    private static IReadOnlyList<string> ParseQueryTerms(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();

        // Split on whitespace, filter short terms
        return query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
```

### 2.2 SearchResultItemView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Lexichord.Modules.RAG.ViewModels"
             xmlns:controls="using:Lexichord.Modules.RAG.Controls"
             x:Class="Lexichord.Modules.RAG.Views.SearchResultItemView"
             x:DataType="vm:SearchResultItemViewModel">

    <UserControl.Styles>
        <!-- Score Badge Colors -->
        <Style Selector="Border.score-badge.HighRelevance">
            <Setter Property="Background" Value="{DynamicResource SuccessBackground}"/>
        </Style>
        <Style Selector="Border.score-badge.MediumRelevance">
            <Setter Property="Background" Value="{DynamicResource WarningBackground}"/>
        </Style>
        <Style Selector="Border.score-badge.LowRelevance">
            <Setter Property="Background" Value="{DynamicResource CautionBackground}"/>
        </Style>
        <Style Selector="Border.score-badge.MinimalRelevance">
            <Setter Property="Background" Value="{DynamicResource SecondaryBackground}"/>
        </Style>

        <!-- Hover state -->
        <Style Selector="Border.result-item:pointerover">
            <Setter Property="Background" Value="{DynamicResource HoverBackground}"/>
        </Style>

        <!-- Selected state -->
        <Style Selector="Border.result-item.selected">
            <Setter Property="Background" Value="{DynamicResource SelectionBackground}"/>
        </Style>
    </UserControl.Styles>

    <Border Classes="result-item"
            Classes.selected="{Binding IsSelected}"
            Padding="12"
            Margin="0,0,0,1"
            BorderBrush="{DynamicResource BorderBrush}"
            BorderThickness="0,0,0,1"
            Cursor="Hand"
            DoubleTapped="OnDoubleTapped">

        <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="*,Auto">
            <!-- Document Name and Score -->
            <Grid Grid.Row="0" Grid.Column="0" ColumnDefinitions="Auto,*">
                <!-- Document Icon -->
                <PathIcon Grid.Column="0"
                          Data="{StaticResource DocumentIcon}"
                          Width="14" Height="14"
                          Margin="0,0,6,0"
                          Foreground="{DynamicResource PrimaryBrush}"/>

                <!-- Document Name -->
                <TextBlock Grid.Column="1"
                           Text="{Binding DocumentName}"
                           FontWeight="SemiBold"
                           TextTrimming="CharacterEllipsis"
                           ToolTip.Tip="{Binding DocumentPath}"/>
            </Grid>

            <!-- Score Badge -->
            <Border Grid.Row="0" Grid.Column="1"
                    Classes="score-badge"
                    Classes.HighRelevance="{Binding ScoreColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=HighRelevance}"
                    Classes.MediumRelevance="{Binding ScoreColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=MediumRelevance}"
                    Classes.LowRelevance="{Binding ScoreColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=LowRelevance}"
                    Classes.MinimalRelevance="{Binding ScoreColor, Converter={StaticResource StringEqualsConverter}, ConverterParameter=MinimalRelevance}"
                    CornerRadius="4"
                    Padding="6,2"
                    Margin="8,0,0,0">
                <TextBlock Text="{Binding ScoreDisplay}"
                           FontSize="11"
                           FontWeight="Medium"/>
            </Border>

            <!-- Section Heading (if available) -->
            <StackPanel Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
                        Orientation="Horizontal"
                        Margin="0,4,0,0"
                        IsVisible="{Binding HasSection}">
                <PathIcon Data="{StaticResource SectionIcon}"
                          Width="12" Height="12"
                          Margin="0,0,4,0"
                          Foreground="{DynamicResource SecondaryTextBrush}"/>
                <TextBlock Text="{Binding Section}"
                           FontSize="12"
                           Foreground="{DynamicResource SecondaryTextBrush}"
                           TextTrimming="CharacterEllipsis"/>
            </StackPanel>

            <!-- Content Preview with Highlighting -->
            <controls:HighlightedTextBlock
                Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2"
                Text="{Binding Preview}"
                HighlightTerms="{Binding QueryTerms}"
                Margin="0,6,0,0"
                FontSize="13"
                Foreground="{DynamicResource SecondaryTextBrush}"
                TextWrapping="Wrap"
                MaxLines="3"/>
        </Grid>
    </Border>
</UserControl>
```

### 2.3 SearchResultItemView.axaml.cs

```csharp
namespace Lexichord.Modules.RAG.Views;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

public partial class SearchResultItemView : UserControl
{
    public SearchResultItemView()
    {
        InitializeComponent();
    }

    private async void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SearchResultItemViewModel vm)
        {
            // Navigate to source document
            var navigationService = App.Current?.Services?.GetService<IReferenceNavigationService>();
            if (navigationService != null)
            {
                await navigationService.NavigateToHitAsync(vm.Hit);
            }
        }
    }
}
```

### 2.4 HighlightedTextBlock Control

```csharp
namespace Lexichord.Modules.RAG.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

/// <summary>
/// TextBlock that highlights specified terms within the text.
/// </summary>
public class HighlightedTextBlock : TextBlock
{
    public static readonly StyledProperty<IReadOnlyList<string>> HighlightTermsProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IReadOnlyList<string>>(
            nameof(HighlightTerms),
            defaultValue: Array.Empty<string>());

    public static readonly StyledProperty<IBrush> HighlightBrushProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IBrush>(
            nameof(HighlightBrush),
            defaultValue: Brushes.Yellow);

    public IReadOnlyList<string> HighlightTerms
    {
        get => GetValue(HighlightTermsProperty);
        set => SetValue(HighlightTermsProperty, value);
    }

    public IBrush HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    static HighlightedTextBlock()
    {
        TextProperty.Changed.AddClassHandler<HighlightedTextBlock>((x, _) => x.UpdateInlines());
        HighlightTermsProperty.Changed.AddClassHandler<HighlightedTextBlock>((x, _) => x.UpdateInlines());
    }

    private void UpdateInlines()
    {
        Inlines?.Clear();

        var text = Text;
        if (string.IsNullOrEmpty(text))
            return;

        var terms = HighlightTerms;
        if (terms == null || terms.Count == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        // Build regex pattern for all terms
        var pattern = string.Join("|", terms.Select(Regex.Escape));
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        var lastIndex = 0;
        foreach (Match match in regex.Matches(text))
        {
            // Add non-highlighted text before match
            if (match.Index > lastIndex)
            {
                Inlines?.Add(new Run(text[lastIndex..match.Index]));
            }

            // Add highlighted match
            Inlines?.Add(new Run(match.Value)
            {
                Background = HighlightBrush,
                FontWeight = FontWeight.SemiBold
            });

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            Inlines?.Add(new Run(text[lastIndex..]));
        }
    }
}
```

---

## 3. Visual Design

### 3.1 Layout Specification

```
┌─────────────────────────────────────────────────────┐
│ 📄 Document Name.md                          [95%] │
│ § Section Heading (if available)                    │
│ Preview text with highlighted terms that wraps      │
│ to multiple lines if needed...                      │
└─────────────────────────────────────────────────────┘
```

### 3.2 Score Badge Colors

| Score Range | Color            | Meaning           |
| :---------- | :--------------- | :---------------- |
| ≥ 90%       | Green (#22c55e)  | High relevance    |
| 80-89%      | Yellow (#eab308) | Medium relevance  |
| 70-79%      | Orange (#f97316) | Low relevance     |
| < 70%       | Gray (#6b7280)   | Minimal relevance |

### 3.3 Typography

| Element          | Size | Weight   | Color             |
| :--------------- | :--- | :------- | :---------------- |
| Document name    | 14px | SemiBold | Primary           |
| Score badge      | 11px | Medium   | On badge          |
| Section          | 12px | Normal   | Secondary         |
| Preview          | 13px | Normal   | Secondary         |
| Highlighted term | 13px | SemiBold | Yellow background |

---

## 4. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6b")]
public class SearchResultItemViewModelTests
{
    [Fact]
    public void Constructor_NullHit_Throws()
    {
        FluentActions.Invoking(() => new SearchResultItemViewModel(null!, "query"))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DocumentName_ReturnsHitDocumentName()
    {
        var hit = CreateHit(name: "TestDoc.md");
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.DocumentName.Should().Be("TestDoc.md");
    }

    [Fact]
    public void DocumentName_NullDocument_ReturnsUnknown()
    {
        var hit = new SearchHit { Score = 0.9f, Chunk = CreateChunk(), Document = null };
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.DocumentName.Should().Be("Unknown Document");
    }

    [Theory]
    [InlineData(0.95f, 95)]
    [InlineData(0.85f, 85)]
    [InlineData(0.756f, 76)]  // Rounds
    [InlineData(0.0f, 0)]
    public void ScorePercent_CalculatesCorrectly(float score, int expected)
    {
        var hit = CreateHit(score: score);
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.ScorePercent.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.95f, "HighRelevance")]
    [InlineData(0.90f, "HighRelevance")]
    [InlineData(0.85f, "MediumRelevance")]
    [InlineData(0.80f, "MediumRelevance")]
    [InlineData(0.75f, "LowRelevance")]
    [InlineData(0.70f, "LowRelevance")]
    [InlineData(0.65f, "MinimalRelevance")]
    public void ScoreColor_ReturnsCorrectCategory(float score, string expected)
    {
        var hit = CreateHit(score: score);
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.ScoreColor.Should().Be(expected);
    }

    [Fact]
    public void Preview_TruncatesLongContent()
    {
        var longContent = new string('a', 500);
        var hit = CreateHit(content: longContent);
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.Preview.Should().HaveLength(LessThanOrEqual(203)); // 200 + "..."
        sut.Preview.Should().EndWith("...");
    }

    [Fact]
    public void Preview_NormalizesWhitespace()
    {
        var content = "word1   word2\n\nword3\tword4";
        var hit = CreateHit(content: content);
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.Preview.Should().Be("word1 word2 word3 word4");
    }

    [Theory]
    [InlineData("hello world", new[] { "hello", "world" })]
    [InlineData("   spaced   terms   ", new[] { "spaced", "terms" })]
    [InlineData("a b c", new string[0])]  // Too short
    [InlineData("", new string[0])]
    public void QueryTerms_ParsesCorrectly(string query, string[] expected)
    {
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, query);

        sut.QueryTerms.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Section_ReturnsSectionHeading()
    {
        var chunk = CreateChunk(sectionHeading: "Chapter 1: Introduction");
        var hit = new SearchHit { Score = 0.9f, Chunk = chunk, Document = CreateDocument() };
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.Section.Should().Be("Chapter 1: Introduction");
        sut.HasSection.Should().BeTrue();
    }

    [Fact]
    public void Section_NoHeading_ReturnsFalse()
    {
        var chunk = CreateChunk(sectionHeading: null);
        var hit = new SearchHit { Score = 0.9f, Chunk = chunk, Document = CreateDocument() };
        var sut = new SearchResultItemViewModel(hit, "query");

        sut.HasSection.Should().BeFalse();
    }

    private static SearchHit CreateHit(
        float score = 0.9f,
        string name = "Doc.md",
        string content = "Sample content")
    {
        return new SearchHit
        {
            Score = score,
            Chunk = CreateChunk(content: content),
            Document = CreateDocument(name: name)
        };
    }

    private static TextChunk CreateChunk(
        string content = "Sample content",
        string? sectionHeading = null)
    {
        return new TextChunk
        {
            Content = content,
            Metadata = new ChunkMetadata
            {
                Index = 0,
                StartOffset = 0,
                EndOffset = content.Length,
                SectionHeading = sectionHeading
            }
        };
    }

    private static Document CreateDocument(string name = "Doc.md")
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = $"/docs/{name}"
        };
    }
}
```

---

## 5. File Locations

| File                 | Path                                                                             |
| :------------------- | :------------------------------------------------------------------------------- |
| View                 | `src/Lexichord.Modules.RAG/Views/SearchResultItemView.axaml`                     |
| View code-behind     | `src/Lexichord.Modules.RAG/Views/SearchResultItemView.axaml.cs`                  |
| ViewModel            | `src/Lexichord.Modules.RAG/ViewModels/SearchResultItemViewModel.cs`              |
| HighlightedTextBlock | `src/Lexichord.Modules.RAG/Controls/HighlightedTextBlock.cs`                     |
| Unit tests           | `tests/Lexichord.Modules.RAG.Tests/ViewModels/SearchResultItemViewModelTests.cs` |

---

## 6. Acceptance Criteria

| #   | Criterion                                       | Status |
| :-- | :---------------------------------------------- | :----- |
| 1   | Document name displays with icon                | [ ]    |
| 2   | Score badge shows percentage with correct color | [ ]    |
| 3   | Section heading displays when available         | [ ]    |
| 4   | Content preview truncates long text             | [ ]    |
| 5   | Query terms are highlighted in preview          | [ ]    |
| 6   | Double-click triggers navigation                | [ ]    |
| 7   | Hover state provides visual feedback            | [ ]    |
| 8   | Tooltip shows full document path                | [ ]    |
| 9   | All unit tests pass                             | [ ]    |

---

## 7. Revision History

| Version | Date       | Author         | Changes       |
| :------ | :--------- | :------------- | :------------ |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft |

---
