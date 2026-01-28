using Avalonia;
using System;

namespace Lexichord.Host;

/// <summary>
/// Application entry point for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: This class configures the Avalonia application builder and starts
/// the desktop application lifecycle. The Main method is the CLR entry point.
///
/// The BuildAvaloniaApp method is also called by the XAML previewer in IDEs,
/// so it must be a public static method that can be invoked independently.
/// </remarks>
internal sealed class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <remarks>
    /// LOGIC: STAThread is required on Windows for proper COM apartment threading.
    /// This enables clipboard operations, file dialogs, and drag-drop functionality.
    /// On other platforms, this attribute is ignored.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds and configures the Avalonia application.
    /// </summary>
    /// <returns>A configured <see cref="AppBuilder"/> instance.</returns>
    /// <remarks>
    /// LOGIC: This method chain configures:
    /// - Configure&lt;App&gt;: Specifies our Application class
    /// - UsePlatformDetect: Auto-detects Windows/macOS/Linux
    /// - WithInterFont: Includes Inter font family
    /// - LogToTrace: Enables trace-level logging (Debug builds)
    ///
    /// The method is public and static so the XAML previewer can invoke it.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
