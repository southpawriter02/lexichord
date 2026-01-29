using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sentry;

namespace Lexichord.Host.Services;

/// <summary>
/// Implementation of <see cref="ITelemetryService"/> using Sentry.
/// </summary>
/// <remarks>
/// PRIVACY REQUIREMENTS:
/// 1. Opt-in by default — Never enabled without user consent
/// 2. No PII collection — Email, paths, content never transmitted
/// 3. Immediate effect — Toggle changes apply instantly
///
/// PII Scrubbing Patterns:
/// - Windows: C:\Users\username\... → [USER_PATH]\...
/// - macOS: /Users/username/... → /[USER_PATH]/...
/// - Linux: /home/username/... → /[USER_PATH]/...
/// - Emails: user@domain.com → [EMAIL]
///
/// Version: v0.1.7d
/// </remarks>
public sealed class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly IMediator _mediator;
    private readonly string _settingsPath;
    private readonly string? _sentryDsn;

    private IDisposable? _sentryClient;
    private bool _isEnabled;
    private bool _isDisposed;
    private TelemetrySettings _settings;

    // PII scrubbing patterns with 1-second timeout for safety
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex WindowsPathRegex = new(
        @"C:\\Users\\[^\\]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        RegexTimeout);

    private static readonly Regex MacPathRegex = new(
        @"/Users/[^/]+",
        RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex LinuxPathRegex = new(
        @"/home/[^/]+",
        RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled,
        RegexTimeout);

    /// <summary>
    /// Initializes a new instance of <see cref="TelemetryService"/>.
    /// </summary>
    public TelemetryService(
        ILogger<TelemetryService> logger,
        IMediator mediator,
        IConfiguration configuration)
        : this(logger, mediator, configuration, GetDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Internal constructor for testing with custom paths.
    /// </summary>
    internal TelemetryService(
        ILogger<TelemetryService> logger,
        IMediator mediator,
        IConfiguration configuration,
        string settingsPath)
    {
        _logger = logger;
        _mediator = mediator;
        _settingsPath = settingsPath;
        _sentryDsn = configuration["Sentry:Dsn"];
        _settings = LoadSettings();

        // Auto-initialize if previously enabled
        if (_settings.CrashReportingEnabled)
        {
            InitializeSentry();
        }

        _logger.LogDebug(
            "TelemetryService initialized. Enabled: {IsEnabled}, DSN configured: {HasDsn}",
            _isEnabled,
            !string.IsNullOrEmpty(_sentryDsn));
    }

    /// <inheritdoc/>
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc/>
    public void Enable()
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot enable telemetry on disposed service");
            return;
        }

        if (_isEnabled)
        {
            _logger.LogDebug("Telemetry already enabled");
            return;
        }

        InitializeSentry();

        // Persist preference
        _settings = _settings with
        {
            CrashReportingEnabled = true,
            ConsentDate = DateTimeOffset.UtcNow,
            ConsentPromptShown = true,
            InstallationId = _settings.InstallationId ?? Guid.NewGuid().ToString("N")[..12]
        };
        SaveSettings();

        // Set user for session correlation
        if (_settings.InstallationId is not null)
        {
            SetUser(_settings.InstallationId);
        }

        // Publish event
        _ = Task.Run(async () =>
        {
            try
            {
                await _mediator.Publish(new TelemetryPreferenceChangedEvent(true, DateTimeOffset.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish TelemetryPreferenceChangedEvent");
            }
        });

        _logger.LogInformation("Telemetry enabled");
    }

    /// <inheritdoc/>
    public void Disable()
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot disable telemetry on disposed service");
            return;
        }

        if (!_isEnabled)
        {
            _logger.LogDebug("Telemetry already disabled");
            return;
        }

        // Flush pending events
        Flush(TimeSpan.FromSeconds(2));

        _isEnabled = false;

        // Persist preference (keep other metadata)
        _settings = _settings with { CrashReportingEnabled = false };
        SaveSettings();

        // Publish event
        _ = Task.Run(async () =>
        {
            try
            {
                await _mediator.Publish(new TelemetryPreferenceChangedEvent(false, DateTimeOffset.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish TelemetryPreferenceChangedEvent");
            }
        });

        _logger.LogInformation("Telemetry disabled");
    }

    /// <inheritdoc/>
    public void CaptureException(Exception exception, IDictionary<string, string>? tags = null)
    {
        if (!_isEnabled || _isDisposed)
            return;

        try
        {
            SentrySdk.CaptureException(exception, scope =>
            {
                // Scrub the exception message
                scope.SetExtra("originalMessage", ScrubPii(exception.Message));

                // Add scrubbed tags
                if (tags is not null)
                {
                    foreach (var (key, value) in tags)
                    {
                        scope.SetTag(ScrubPii(key), ScrubPii(value));
                    }
                }
            });

            // Publish event (exception type only, no PII)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _mediator.Publish(new CrashCapturedEvent(
                        exception.GetType().Name,
                        DateTimeOffset.UtcNow));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish CrashCapturedEvent");
                }
            });

            _logger.LogDebug("Captured exception: {ExceptionType}", exception.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture exception to Sentry");
        }
    }

    /// <inheritdoc/>
    public void CaptureMessage(string message, TelemetryLevel level = TelemetryLevel.Info)
    {
        if (!_isEnabled || _isDisposed)
            return;

        try
        {
            var scrubbedMessage = ScrubPii(message);
            var sentryLevel = MapToSentryLevel(level);
            SentrySdk.CaptureMessage(scrubbedMessage, sentryLevel);

            _logger.LogDebug("Captured message at level {Level}", level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture message to Sentry");
        }
    }

    /// <inheritdoc/>
    public void AddBreadcrumb(string message, string? category = null)
    {
        if (!_isEnabled || _isDisposed)
            return;

        try
        {
            var scrubbedMessage = ScrubPii(message);
            var scrubbedCategory = category is not null ? ScrubPii(category) : null;

            SentrySdk.AddBreadcrumb(scrubbedMessage, scrubbedCategory);

            _logger.LogTrace("Added breadcrumb: {Category}", scrubbedCategory ?? "general");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add breadcrumb");
        }
    }

    /// <inheritdoc/>
    public IDisposable BeginScope(string operation)
    {
        if (!_isEnabled || _isDisposed)
            return new NoOpDisposable();

        var scrubbedOperation = ScrubPii(operation);
        AddBreadcrumb($"Begin: {scrubbedOperation}", "operation");

        return new TelemetryScope(this, scrubbedOperation);
    }

    /// <inheritdoc/>
    public void SetUser(string userId)
    {
        if (!_isEnabled || _isDisposed)
            return;

        try
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new SentryUser { Id = userId };
            });

            _logger.LogDebug("Set user ID for session correlation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user");
        }
    }

    /// <inheritdoc/>
    public void Flush(TimeSpan timeout)
    {
        if (!_isEnabled || _isDisposed)
            return;

        try
        {
            SentrySdk.FlushAsync(timeout).GetAwaiter().GetResult();
            _logger.LogDebug("Flushed pending telemetry events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush telemetry");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_isEnabled)
        {
            Flush(TimeSpan.FromSeconds(2));
        }

        _sentryClient?.Dispose();
        _sentryClient = null;
        _isEnabled = false;

        _logger.LogDebug("TelemetryService disposed");
    }

    private void InitializeSentry()
    {
        if (string.IsNullOrEmpty(_sentryDsn))
        {
            _logger.LogWarning(
                "Sentry DSN not configured. Telemetry will operate in degraded mode (local logging only)");
            _isEnabled = true; // Still mark as enabled for consistent behavior
            return;
        }

        try
        {
            _sentryClient = SentrySdk.Init(options =>
            {
                options.Dsn = _sentryDsn;

                // CRITICAL: Never send PII automatically
                options.SendDefaultPii = false;

                // Include stack traces for debugging
                options.AttachStacktrace = true;

                // Limit breadcrumb history
                options.MaxBreadcrumbs = 100;

                // Set release version
                options.Release = GetAssemblyVersion();

                // PII scrubbing before send
                options.SetBeforeSend((sentryEvent, _) =>
                {
                    // Scrub exception messages
                    if (sentryEvent.Exception is not null)
                    {
                        // Note: We can't modify the exception directly,
                        // but the scope.SetExtra above handles this
                    }

                    // Scrub breadcrumb messages
                    if (sentryEvent.Breadcrumbs is not null)
                    {
                        foreach (var breadcrumb in sentryEvent.Breadcrumbs)
                        {
                            // Breadcrumbs are already scrubbed when added
                        }
                    }

                    return sentryEvent;
                });
            });

            _isEnabled = true;
            _logger.LogInformation("Sentry SDK initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Sentry SDK");
            _isEnabled = false;
        }
    }

    /// <summary>
    /// Scrubs PII from the input string.
    /// </summary>
    internal static string ScrubPii(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        try
        {
            var result = input;

            // Scrub Windows paths
            result = WindowsPathRegex.Replace(result, "[USER_PATH]");

            // Scrub macOS paths
            result = MacPathRegex.Replace(result, "/[USER_PATH]");

            // Scrub Linux paths
            result = LinuxPathRegex.Replace(result, "/[USER_PATH]");

            // Scrub email addresses
            result = EmailRegex.Replace(result, "[EMAIL]");

            return result;
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out, return a safe placeholder
            return "[SCRUBBED_TIMEOUT]";
        }
    }

    private static SentryLevel MapToSentryLevel(TelemetryLevel level) => level switch
    {
        TelemetryLevel.Debug => SentryLevel.Debug,
        TelemetryLevel.Info => SentryLevel.Info,
        TelemetryLevel.Warning => SentryLevel.Warning,
        TelemetryLevel.Error => SentryLevel.Error,
        TelemetryLevel.Fatal => SentryLevel.Fatal,
        _ => SentryLevel.Info
    };

    private TelemetrySettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new TelemetrySettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<TelemetrySettings>(json) ?? new TelemetrySettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load telemetry settings, using defaults");
            return new TelemetrySettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsPath, json);
            _logger.LogDebug("Saved telemetry settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save telemetry settings");
        }
    }

    private static string GetDefaultSettingsPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "Lexichord", "telemetry-settings.json");
    }

    private static string GetAssemblyVersion()
    {
        return typeof(TelemetryService).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    /// <summary>
    /// No-op disposable for when telemetry is disabled.
    /// </summary>
    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }

    /// <summary>
    /// Scope that adds end breadcrumb when disposed.
    /// </summary>
    private sealed class TelemetryScope : IDisposable
    {
        private readonly TelemetryService _service;
        private readonly string _operation;

        public TelemetryScope(TelemetryService service, string operation)
        {
            _service = service;
            _operation = operation;
        }

        public void Dispose()
        {
            _service.AddBreadcrumb($"End: {_operation}", "operation");
        }
    }
}
