namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for displaying and persisting crash reports.
/// </summary>
/// <remarks>
/// LOGIC: Crash reports are saved to disk first, then displayed to the user.
/// This ensures data capture even if the UI fails to display.
/// </remarks>
public interface ICrashReportService
{
    /// <summary>
    /// Displays the crash report dialog to the user.
    /// </summary>
    /// <param name="exception">The exception that caused the crash.</param>
    /// <remarks>
    /// This method should be called from the UI thread if possible.
    /// If the dialog cannot be shown, the report is still saved to disk.
    /// </remarks>
    void ShowCrashReport(Exception exception);

    /// <summary>
    /// Saves a crash report to disk without displaying a dialog.
    /// </summary>
    /// <param name="exception">The exception to save.</param>
    /// <returns>The file path where the report was saved.</returns>
    Task<string> SaveCrashReportAsync(Exception exception);

    /// <summary>
    /// Gets the directory where crash reports are stored.
    /// </summary>
    string CrashReportDirectory { get; }
}
