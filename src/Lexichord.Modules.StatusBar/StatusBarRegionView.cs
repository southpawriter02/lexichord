using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.StatusBar.ViewModels;
using Lexichord.Modules.StatusBar.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.StatusBar;

/// <summary>
/// Wraps StatusBarView for shell region registration.
/// </summary>
/// <remarks>
/// LOGIC: The IShellRegionView interface allows modules to declare
/// which region their view should be placed in. The ShellRegionManager
/// in Host collects all registered views and places them accordingly.
///
/// This wrapper pattern allows:
/// - Lazy view creation (view is created on first access)
/// - Dependency injection for the view
/// - Ordering multiple views in the same region
/// </remarks>
public sealed class StatusBarRegionView : IShellRegionView
{
    private readonly IServiceProvider _provider;
    private StatusBarView? _view;

    public StatusBarRegionView(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: The StatusBar belongs in the Bottom region of the shell.
    /// This region is typically a narrow strip at the bottom of MainWindow.
    /// </remarks>
    public ShellRegion TargetRegion => ShellRegion.Bottom;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Order determines position when multiple views target the same region.
    /// StatusBar uses 100 (middle priority) to allow other modules to position
    /// before (0-99) or after (101+) it.
    /// </remarks>
    public int Order => 100;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: The ViewContent is lazily created on first access.
    /// This ensures the view is created with a valid DI scope.
    /// </remarks>
    public object ViewContent
    {
        get
        {
            if (_view is null)
            {
                _view = _provider.GetRequiredService<StatusBarView>();
                _view.DataContext = _provider.GetRequiredService<StatusBarViewModel>();
            }
            return _view;
        }
    }
}
