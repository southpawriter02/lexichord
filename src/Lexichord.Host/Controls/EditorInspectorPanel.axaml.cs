// ═══════════════════════════════════════════════════════════════════════════════
// LEXICHORD - Editor Inspector Panel (P0 - Critical Missing Component)
// Right-side inspector panel for manuscript editor
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.Controls;

/// <summary>
/// Right-side inspector panel for the manuscript editor.
/// Shows style score, suggestions, and document metadata.
/// </summary>
/// <remarks>
/// SPEC: SCREEN_WIREFRAMES.md Section 2.1 - Inspector Panel
/// This was identified as a P0 critical missing component.
///
/// Features:
/// - Style Score gauge with status badge
/// - Issue counts (errors, warnings, info)
/// - Writing suggestions with action buttons
/// - Document metadata (word count, reading time, etc.)
/// - Voice profile sliders (directness, formality, complexity)
/// </remarks>
public partial class EditorInspectorPanel : UserControl
{
    // ─────────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the close button is clicked.
    /// </summary>
    public event EventHandler? CloseRequested;

    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    public static readonly StyledProperty<int> StyleScoreProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(StyleScore), 85);

    public int StyleScore
    {
        get => GetValue(StyleScoreProperty);
        set => SetValue(StyleScoreProperty, value);
    }

    public static readonly StyledProperty<int> ErrorCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(ErrorCount), 0);

    public int ErrorCount
    {
        get => GetValue(ErrorCountProperty);
        set => SetValue(ErrorCountProperty, value);
    }

    public static readonly StyledProperty<int> WarningCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(WarningCount), 2);

    public int WarningCount
    {
        get => GetValue(WarningCountProperty);
        set => SetValue(WarningCountProperty, value);
    }

    public static readonly StyledProperty<int> InfoCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(InfoCount), 1);

    public int InfoCount
    {
        get => GetValue(InfoCountProperty);
        set => SetValue(InfoCountProperty, value);
    }

    public static readonly StyledProperty<int> WordCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(WordCount), 1234);

    public int WordCount
    {
        get => GetValue(WordCountProperty);
        set => SetValue(WordCountProperty, value);
    }

    public static readonly StyledProperty<int> CharacterCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(CharacterCount), 7890);

    public int CharacterCount
    {
        get => GetValue(CharacterCountProperty);
        set => SetValue(CharacterCountProperty, value);
    }

    public static readonly StyledProperty<int> ParagraphCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(ParagraphCount), 12);

    public int ParagraphCount
    {
        get => GetValue(ParagraphCountProperty);
        set => SetValue(ParagraphCountProperty, value);
    }

    public static readonly StyledProperty<int> SentenceCountProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, int>(nameof(SentenceCount), 45);

    public int SentenceCount
    {
        get => GetValue(SentenceCountProperty);
        set => SetValue(SentenceCountProperty, value);
    }

    public static readonly StyledProperty<double> DirectnessProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, double>(nameof(Directness), 65);

    public double Directness
    {
        get => GetValue(DirectnessProperty);
        set => SetValue(DirectnessProperty, value);
    }

    public static readonly StyledProperty<double> FormalityProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, double>(nameof(Formality), 70);

    public double Formality
    {
        get => GetValue(FormalityProperty);
        set => SetValue(FormalityProperty, value);
    }

    public static readonly StyledProperty<double> ComplexityProperty =
        AvaloniaProperty.Register<EditorInspectorPanel, double>(nameof(Complexity), 45);

    public double Complexity
    {
        get => GetValue(ComplexityProperty);
        set => SetValue(ComplexityProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    public string StatusText => StyleScore switch
    {
        >= 90 => "In Harmony",
        >= 70 => "Minor Dissonance",
        >= 50 => "Needs Tuning",
        _ => "Out of Tune"
    };

    public IBrush StatusColor => StyleScore switch
    {
        >= 90 => new SolidColorBrush(Color.Parse("#FF22C55E")),
        >= 70 => new SolidColorBrush(Color.Parse("#FFF59E0B")),
        _ => new SolidColorBrush(Color.Parse("#FFEF4444"))
    };

    public string ReadingTime
    {
        get
        {
            var minutes = WordCount / 200; // Average reading speed
            return minutes <= 1 ? "< 1 min" : $"~{minutes} min";
        }
    }

    public string DirectnessLabel => Directness switch
    {
        < 33 => "Soft",
        < 66 => "Balanced",
        _ => "Assertive"
    };

    public string FormalityLabel => Formality switch
    {
        < 33 => "Casual",
        < 66 => "Balanced",
        _ => "Formal"
    };

    public string ComplexityLabel => Complexity switch
    {
        < 33 => "Simple",
        < 66 => "Balanced",
        _ => "Technical"
    };

    public bool HasSuggestions => Suggestions.Count > 0;

    // ─────────────────────────────────────────────────────────────────────────
    // Collections
    // ─────────────────────────────────────────────────────────────────────────

    public ObservableCollection<SuggestionItem> Suggestions { get; } = [];

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public EditorInspectorPanel()
    {
        InitializeComponent();
        DataContext = this;

        // Load sample suggestions for demo
        LoadSampleSuggestions();

        // Update computed properties when StyleScore changes
        this.GetObservable(StyleScoreProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(StatusTextProperty, default, StatusText);
            RaisePropertyChanged(StatusColorProperty, default, StatusColor);
        });

        // Update ReadingTime when WordCount changes
        this.GetObservable(WordCountProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(ReadingTimeProperty, default, ReadingTime);
        });

        // Update labels when sliders change
        this.GetObservable(DirectnessProperty).Subscribe(_ =>
            RaisePropertyChanged(DirectnessLabelProperty, default, DirectnessLabel));
        this.GetObservable(FormalityProperty).Subscribe(_ =>
            RaisePropertyChanged(FormalityLabelProperty, default, FormalityLabel));
        this.GetObservable(ComplexityProperty).Subscribe(_ =>
            RaisePropertyChanged(ComplexityLabelProperty, default, ComplexityLabel));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Direct Properties for Binding
    // ─────────────────────────────────────────────────────────────────────────

    public static readonly DirectProperty<EditorInspectorPanel, string> StatusTextProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, string>(
            nameof(StatusText), o => o.StatusText);

    public static readonly DirectProperty<EditorInspectorPanel, IBrush> StatusColorProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, IBrush>(
            nameof(StatusColor), o => o.StatusColor);

    public static readonly DirectProperty<EditorInspectorPanel, string> ReadingTimeProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, string>(
            nameof(ReadingTime), o => o.ReadingTime);

    public static readonly DirectProperty<EditorInspectorPanel, string> DirectnessLabelProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, string>(
            nameof(DirectnessLabel), o => o.DirectnessLabel);

    public static readonly DirectProperty<EditorInspectorPanel, string> FormalityLabelProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, string>(
            nameof(FormalityLabel), o => o.FormalityLabel);

    public static readonly DirectProperty<EditorInspectorPanel, string> ComplexityLabelProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, string>(
            nameof(ComplexityLabel), o => o.ComplexityLabel);

    public static readonly DirectProperty<EditorInspectorPanel, bool> HasSuggestionsProperty =
        AvaloniaProperty.RegisterDirect<EditorInspectorPanel, bool>(
            nameof(HasSuggestions), o => o.HasSuggestions);

    // ─────────────────────────────────────────────────────────────────────────
    // Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void FixAll()
    {
        // TODO: Implement fix all suggestions
        Suggestions.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private void LoadSampleSuggestions()
    {
        Suggestions.Add(new SuggestionItem
        {
            Icon = "🟡",
            Location = "Line 12",
            Description = "Consider using 'allowlist' instead of 'whitelist'.",
            Original = "whitelist",
            Replacement = "allowlist",
            SeverityColor = new SolidColorBrush(Color.Parse("#FFF59E0B"))
        });

        Suggestions.Add(new SuggestionItem
        {
            Icon = "🟡",
            Location = "Line 45",
            Description = "This sentence is quite long. Consider breaking it up.",
            HasReplacement = false,
            SeverityColor = new SolidColorBrush(Color.Parse("#FFF59E0B"))
        });

        Suggestions.Add(new SuggestionItem
        {
            Icon = "💡",
            Location = "Line 78",
            Description = "Passive voice detected. Consider using active voice.",
            Original = "The document was created",
            Replacement = "You created the document",
            SeverityColor = new SolidColorBrush(Color.Parse("#FF60A5FA"))
        });
    }
}

/// <summary>
/// Represents a writing suggestion item.
/// </summary>
public partial class SuggestionItem : ObservableObject
{
    public string Icon { get; set; } = "⚠️";
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
    public bool HasReplacement { get; set; } = true;
    public IBrush SeverityColor { get; set; } = Brushes.Yellow;

    [RelayCommand]
    private void Apply()
    {
        // TODO: Apply the suggestion to the document
    }

    [RelayCommand]
    private void Ignore()
    {
        // TODO: Ignore this suggestion
    }
}
