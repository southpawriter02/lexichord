using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Lexichord.Host.Views;

/// <summary>
/// Window that displays crash report details to the user.
/// </summary>
/// <remarks>
/// LOGIC: This window is shown when an unhandled exception occurs.
/// It provides:
/// - Exception type, message, and stack trace
/// - Copy to clipboard functionality for support tickets
/// - Link to open the crash reports folder
/// - Clean application exit
/// </remarks>
public partial class CrashReportWindow : Window
{
    private readonly string _reportPath;
    private readonly string _reportContent;

    /// <summary>
    /// Parameterless constructor for XAML design-time support.
    /// </summary>
    public CrashReportWindow()
    {
        InitializeComponent();
        _reportPath = string.Empty;
        _reportContent = string.Empty;
    }

    /// <summary>
    /// Initializes a new CrashReportWindow with exception details.
    /// </summary>
    /// <param name="exception">The exception that caused the crash.</param>
    /// <param name="reportPath">The file path where the crash report was saved.</param>
    public CrashReportWindow(Exception exception, string reportPath)
    {
        InitializeComponent();

        _reportPath = reportPath;
        _reportContent = FormatExceptionReport(exception);

        ReportPathText.Text = $"Crash report saved to: {reportPath}";
        ExceptionDetails.Text = _reportContent;
    }

    /// <summary>
    /// Formats the exception into a user-readable report.
    /// </summary>
    private static string FormatExceptionReport(Exception exception)
    {
        return $"""
            ══════════════════════════════════════════════════════════════
            EXCEPTION TYPE
            ══════════════════════════════════════════════════════════════
            {exception.GetType().FullName}

            ══════════════════════════════════════════════════════════════
            MESSAGE
            ══════════════════════════════════════════════════════════════
            {exception.Message}

            ══════════════════════════════════════════════════════════════
            STACK TRACE
            ══════════════════════════════════════════════════════════════
            {exception.StackTrace ?? "(no stack trace available)"}

            {FormatInnerExceptions(exception)}
            """;
    }

    /// <summary>
    /// Formats inner exceptions recursively.
    /// </summary>
    private static string FormatInnerExceptions(Exception exception)
    {
        if (exception.InnerException is null)
            return string.Empty;

        var result = new StringBuilder();
        var inner = exception.InnerException;
        var depth = 1;

        while (inner is not null)
        {
            result.AppendLine($"""

                ══════════════════════════════════════════════════════════════
                INNER EXCEPTION #{depth}
                ══════════════════════════════════════════════════════════════
                Type: {inner.GetType().FullName}
                Message: {inner.Message}
                Stack Trace:
                {inner.StackTrace ?? "(no stack trace available)"}
                """);

            inner = inner.InnerException;
            depth++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Handles the Copy to Clipboard button click.
    /// </summary>
    private async void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(_reportContent);

            // LOGIC: Provide visual feedback that copy succeeded
            var button = (Button)sender;
            var originalContent = button.Content;
            button.Content = "Copied!";
            button.IsEnabled = false;

            await System.Threading.Tasks.Task.Delay(1500);

            button.Content = originalContent;
            button.IsEnabled = true;
        }
    }

    /// <summary>
    /// Handles the Open Reports Folder button click.
    /// </summary>
    private void OnOpenFolderClicked(object sender, RoutedEventArgs e)
    {
        var folderPath = Path.GetDirectoryName(_reportPath);
        if (folderPath is null) return;

        // LOGIC: Open folder in platform-appropriate file manager
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", folderPath);
            }
        }
        catch
        {
            // LOGIC: Silently fail if we can't open the folder
            // The path is displayed in the dialog anyway
        }
    }

    /// <summary>
    /// Handles the Close Application button click.
    /// </summary>
    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        // LOGIC: Close the dialog, which will allow the exception
        // handler to proceed with application termination
        Close();
    }
}
