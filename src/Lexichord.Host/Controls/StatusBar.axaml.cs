// ═══════════════════════════════════════════════════════════════════════════
// LEXICHORD - StatusBar Control
// Bottom status bar displaying document statistics and sync status
// ═══════════════════════════════════════════════════════════════════════════

using Avalonia;
using Avalonia.Controls;

namespace Lexichord.Host.Controls;

/// <summary>
/// Sync status enumeration.
/// </summary>
public enum SyncStatus
{
    Synced,
    Syncing,
    Offline
}

/// <summary>
/// Status bar control showing document statistics, sync status, and cursor position.
/// </summary>
public partial class StatusBar : UserControl
{
    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Number of words in the document.
    /// </summary>
    public static readonly StyledProperty<int> WordCountProperty =
        AvaloniaProperty.Register<StatusBar, int>(nameof(WordCount), 0);

    public int WordCount
    {
        get => GetValue(WordCountProperty);
        set => SetValue(WordCountProperty, value);
    }

    /// <summary>
    /// Number of characters in the document.
    /// </summary>
    public static readonly StyledProperty<int> CharacterCountProperty =
        AvaloniaProperty.Register<StatusBar, int>(nameof(CharacterCount), 0);

    public int CharacterCount
    {
        get => GetValue(CharacterCountProperty);
        set => SetValue(CharacterCountProperty, value);
    }

    /// <summary>
    /// Current line number of cursor.
    /// </summary>
    public static readonly StyledProperty<int> LineNumberProperty =
        AvaloniaProperty.Register<StatusBar, int>(nameof(LineNumber), 1);

    public int LineNumber
    {
        get => GetValue(LineNumberProperty);
        set => SetValue(LineNumberProperty, value);
    }

    /// <summary>
    /// Current column number of cursor.
    /// </summary>
    public static readonly StyledProperty<int> ColumnNumberProperty =
        AvaloniaProperty.Register<StatusBar, int>(nameof(ColumnNumber), 1);

    public int ColumnNumber
    {
        get => GetValue(ColumnNumberProperty);
        set => SetValue(ColumnNumberProperty, value);
    }

    /// <summary>
    /// Current sync status.
    /// </summary>
    public static readonly StyledProperty<SyncStatus> StatusProperty =
        AvaloniaProperty.Register<StatusBar, SyncStatus>(nameof(Status), SyncStatus.Synced);

    public SyncStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Estimated reading time in minutes (assuming 200 wpm average).
    /// </summary>
    public int ReadingTimeMinutes => Math.Max(1, (int)Math.Ceiling(WordCount / 200.0));

    /// <summary>
    /// Human-readable sync status text.
    /// </summary>
    public string SyncStatusText => Status switch
    {
        SyncStatus.Synced => "Saved",
        SyncStatus.Syncing => "Saving...",
        SyncStatus.Offline => "Offline",
        _ => "Unknown"
    };

    // Status boolean properties for style selectors
    public bool IsSynced => Status == SyncStatus.Synced;
    public bool IsSyncing => Status == SyncStatus.Syncing;
    public bool IsOffline => Status == SyncStatus.Offline;

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public StatusBar()
    {
        InitializeComponent();
        DataContext = this;

        // Update computed properties when dependencies change
        this.GetObservable(WordCountProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(ReadingTimeMinutesProperty, default, ReadingTimeMinutes);
        });

        this.GetObservable(StatusProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(SyncStatusTextProperty, default, SyncStatusText);
            RaisePropertyChanged(IsSyncedProperty, default, IsSynced);
            RaisePropertyChanged(IsSyncingProperty, default, IsSyncing);
            RaisePropertyChanged(IsOfflineProperty, default, IsOffline);
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Direct Properties for Binding
    // ─────────────────────────────────────────────────────────────────────────

    public static readonly DirectProperty<StatusBar, int> ReadingTimeMinutesProperty =
        AvaloniaProperty.RegisterDirect<StatusBar, int>(nameof(ReadingTimeMinutes), o => o.ReadingTimeMinutes);

    public static readonly DirectProperty<StatusBar, string> SyncStatusTextProperty =
        AvaloniaProperty.RegisterDirect<StatusBar, string>(nameof(SyncStatusText), o => o.SyncStatusText);

    public static readonly DirectProperty<StatusBar, bool> IsSyncedProperty =
        AvaloniaProperty.RegisterDirect<StatusBar, bool>(nameof(IsSynced), o => o.IsSynced);

    public static readonly DirectProperty<StatusBar, bool> IsSyncingProperty =
        AvaloniaProperty.RegisterDirect<StatusBar, bool>(nameof(IsSyncing), o => o.IsSyncing);

    public static readonly DirectProperty<StatusBar, bool> IsOfflineProperty =
        AvaloniaProperty.RegisterDirect<StatusBar, bool>(nameof(IsOffline), o => o.IsOffline);
}
