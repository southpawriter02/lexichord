using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Views;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lexichord.Host.Services;

/// <summary>
/// Service for displaying and persisting crash reports.
/// </summary>
/// <remarks>
/// LOGIC: When an unhandled exception occurs:
/// 1. Save the crash report to disk immediately (ensure data capture)
/// 2. Attempt to show the crash dialog (best effort)
/// 3. Log all details to Serilog before any UI interaction
///
/// File location follows platform conventions:
/// - Windows: %APPDATA%/Lexichord/CrashReports/
/// - macOS: ~/Library/Application Support/Lexichord/CrashReports/
/// - Linux: ~/.config/Lexichord/CrashReports/
/// </remarks>
public sealed class CrashReportService : ICrashReportService
{
    private readonly ILogger<CrashReportService> _logger;
    private readonly string _crashReportDir;

    /// <summary>
    /// Initializes a new instance of CrashReportService.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CrashReportService(ILogger<CrashReportService> logger)
        : this(logger, null)
    {
    }

    /// <summary>
    /// Initializes a new instance with a custom crash report directory (for testing).
    /// </summary>
    internal CrashReportService(ILogger<CrashReportService> logger, string? customPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _crashReportDir = customPath ?? Path.Combine(appData, "Lexichord", "CrashReports");
        Directory.CreateDirectory(_crashReportDir);

        _logger.LogDebug("CrashReportService initialized. Reports directory: {CrashReportDir}", _crashReportDir);
    }

    /// <inheritdoc/>
    public string CrashReportDirectory => _crashReportDir;

    /// <inheritdoc/>
    public void ShowCrashReport(Exception exception)
    {
        _logger.LogCritical(exception, "Crash report requested for exception: {ExceptionType}",
            exception.GetType().Name);

        try
        {
            // LOGIC: Save to disk first, before attempting UI
            // This ensures we capture the report even if dialog fails
            var reportPath = SaveCrashReportAsync(exception).GetAwaiter().GetResult();
            _logger.LogInformation("Crash report saved to {Path}", reportPath);

            // Attempt to show dialog (may fail if UI thread is dead)
            var dialog = new CrashReportWindow(exception, reportPath);
            dialog.ShowDialog(null!);
        }
        catch (Exception dialogEx)
        {
            // LOGIC: Dialog failed, but we've already saved the report
            _logger.LogError(dialogEx, "Failed to show crash dialog, report saved to disk");
        }
    }

    /// <inheritdoc/>
    public async Task<string> SaveCrashReportAsync(Exception exception)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        var fileName = $"crash-{timestamp}.log";
        var filePath = Path.Combine(_crashReportDir, fileName);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

        var report = $"""
            ════════════════════════════════════════════════════════════════
            LEXICHORD CRASH REPORT
            ════════════════════════════════════════════════════════════════

            Timestamp (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}
            Application Version: {version}
            OS: {Environment.OSVersion}
            .NET Version: {Environment.Version}
            Machine: {Environment.MachineName}
            64-bit OS: {Environment.Is64BitOperatingSystem}
            64-bit Process: {Environment.Is64BitProcess}
            Working Set: {Environment.WorkingSet / 1024 / 1024} MB
            Processor Count: {Environment.ProcessorCount}

            ════════════════════════════════════════════════════════════════
            EXCEPTION DETAILS
            ════════════════════════════════════════════════════════════════

            Type: {exception.GetType().FullName}
            Message: {exception.Message}
            Source: {exception.Source}
            HResult: 0x{exception.HResult:X8}

            ════════════════════════════════════════════════════════════════
            STACK TRACE
            ════════════════════════════════════════════════════════════════

            {exception.StackTrace ?? "(no stack trace available)"}

            ════════════════════════════════════════════════════════════════
            INNER EXCEPTION(S)
            ════════════════════════════════════════════════════════════════

            {GetInnerExceptionDetails(exception)}

            ════════════════════════════════════════════════════════════════
            ADDITIONAL DATA
            ════════════════════════════════════════════════════════════════

            {GetExceptionData(exception)}

            ════════════════════════════════════════════════════════════════
            END OF REPORT
            ════════════════════════════════════════════════════════════════
            """;

        await File.WriteAllTextAsync(filePath, report);
        _logger.LogDebug("Crash report written to {FilePath}", filePath);

        return filePath;
    }

    /// <summary>
    /// Formats inner exceptions recursively.
    /// </summary>
    private static string GetInnerExceptionDetails(Exception exception)
    {
        var inner = exception.InnerException;
        if (inner is null)
            return "(none)";

        var details = new StringBuilder();
        var depth = 1;

        while (inner is not null)
        {
            details.AppendLine($"""
                [Inner Exception {depth}]
                Type: {inner.GetType().FullName}
                Message: {inner.Message}
                Source: {inner.Source}
                Stack Trace:
                {inner.StackTrace ?? "(no stack trace available)"}

                """);

            inner = inner.InnerException;
            depth++;
        }

        return details.ToString();
    }

    /// <summary>
    /// Formats exception Data dictionary.
    /// </summary>
    private static string GetExceptionData(Exception exception)
    {
        if (exception.Data.Count == 0)
            return "(none)";

        var data = new StringBuilder();
        foreach (var key in exception.Data.Keys)
        {
            data.AppendLine($"  {key}: {exception.Data[key]}");
        }

        return data.ToString();
    }
}
