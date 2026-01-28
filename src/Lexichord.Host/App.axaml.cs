using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Lexichord.Host.Views;

namespace Lexichord.Host;

/// <summary>
/// The main Avalonia application class for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: This class manages the application lifecycle. The key responsibilities are:
/// 1. Load XAML resources in Initialize()
/// 2. Create the MainWindow in OnFrameworkInitializationCompleted()
///
/// Future versions will add service initialization here (DI, logging, etc.).
/// </remarks>
public partial class App : Application
{
    /// <summary>
    /// Initializes the application and loads XAML resources.
    /// </summary>
    /// <remarks>
    /// LOGIC: This is called early in startup, before the UI is shown.
    /// AvaloniaXamlLoader.Load(this) processes App.axaml and merges
    /// all ResourceDictionaries (themes, styles, etc.) into the application.
    /// </remarks>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Called when the Avalonia framework has completed initialization.
    /// </summary>
    /// <remarks>
    /// LOGIC: At this point, all resources are loaded and the platform
    /// subsystems are ready. We create and assign the MainWindow here.
    ///
    /// For desktop applications, ApplicationLifetime is always
    /// IClassicDesktopStyleApplicationLifetime, which manages the main window
    /// and application exit behavior.
    /// </remarks>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // LOGIC: Create the main window and assign it to the desktop lifetime.
            // The application will exit when this window is closed.
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
