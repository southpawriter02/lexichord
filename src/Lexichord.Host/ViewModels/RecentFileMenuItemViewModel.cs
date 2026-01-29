using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for a single item in the Recent Files menu.
/// </summary>
/// <remarks>
/// LOGIC: Wraps a RecentFileEntry and provides:
/// - Display text (file name or full path based on settings)
/// - IsEnabled bound to entry.Exists
/// - OpenCommand that invokes parent's execute action with this entry
/// </remarks>
public partial class RecentFileMenuItemViewModel : ObservableObject
{
    private readonly RecentFileEntry _entry;

    public RecentFileMenuItemViewModel(RecentFileEntry entry, Action<RecentFileEntry> openAction)
    {
        _entry = entry;
        // LOGIC: Wrap the parent's action into a command that auto-passes the entry
        OpenCommand = new RelayCommand(() => openAction(_entry), () => _entry.Exists);
    }

    /// <summary>
    /// The underlying entry data.
    /// </summary>
    public RecentFileEntry Entry => _entry;

    /// <summary>
    /// Display text for the menu item.
    /// </summary>
    public string DisplayText => _entry.FileName;

    /// <summary>
    /// Tooltip showing the full path.
    /// </summary>
    public string ToolTip => _entry.FilePath;

    /// <summary>
    /// Whether the menu item is enabled (file exists).
    /// </summary>
    public bool IsEnabled => _entry.Exists;

    /// <summary>
    /// Command to open the file (invokes automatically with this entry).
    /// </summary>
    public ICommand OpenCommand { get; }
}
