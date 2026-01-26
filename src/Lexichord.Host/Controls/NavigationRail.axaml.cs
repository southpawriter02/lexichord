// ═══════════════════════════════════════════════════════════════════════════
// LEXICHORD - NavigationRail Control
// Vertical navigation sidebar with icon buttons
// ═══════════════════════════════════════════════════════════════════════════

using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Lexichord.Host.Controls;

/// <summary>
/// Navigation rail control providing vertical icon-based navigation.
/// Displays main application sections with active state indication.
/// </summary>
public partial class NavigationRail : UserControl
{
    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Currently selected navigation item.
    /// </summary>
    public static readonly StyledProperty<string> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationRail, string>(nameof(SelectedItem), "Editor");

    public string SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties for Active States
    // ─────────────────────────────────────────────────────────────────────────

    public bool IsEditorActive => SelectedItem == "Editor";
    public bool IsKnowledgeActive => SelectedItem == "Knowledge";
    public bool IsAgentsActive => SelectedItem == "Agents";
    public bool IsAnalyticsActive => SelectedItem == "Analytics";
    public bool IsSettingsActive => SelectedItem == "Settings";
    public bool IsProfileActive => SelectedItem == "Profile";

    // ─────────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when navigation selection changes.
    /// </summary>
    public event EventHandler<string>? NavigationChanged;

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public NavigationRail()
    {
        InitializeComponent();
        DataContext = this;

        // Notify property changes when SelectedItem changes
        this.GetObservable(SelectedItemProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(nameof(IsEditorActive));
            RaisePropertyChanged(nameof(IsKnowledgeActive));
            RaisePropertyChanged(nameof(IsAgentsActive));
            RaisePropertyChanged(nameof(IsAnalyticsActive));
            RaisePropertyChanged(nameof(IsSettingsActive));
            RaisePropertyChanged(nameof(IsProfileActive));
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Navigate(string destination)
    {
        SelectedItem = destination;
        NavigationChanged?.Invoke(this, destination);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property Changed Helper
    // ─────────────────────────────────────────────────────────────────────────

    private void RaisePropertyChanged(string propertyName)
    {
        // Using Avalonia's property system for bindings
        // In a full implementation, implement INotifyPropertyChanged
    }
}
