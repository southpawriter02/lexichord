using System.Reflection;
using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Manages first run detection and release notes display.
/// </summary>
/// <remarks>
/// LOGIC: Detects whether the application has been updated since last run
/// by comparing the stored version with the current assembly version.
/// Coordinates with IEditorService to display CHANGELOG.md.
///
/// Version: v0.1.7c
/// </remarks>
public sealed class FirstRunService : IFirstRunService
{
    private readonly IMediator _mediator;
    private readonly ILogger<FirstRunService> _logger;
    private readonly string _settingsPath;
    private readonly string _changelogPath;

    private bool _initialized;
    private bool _isFirstRunEver;
    private bool _isFirstRunAfterUpdate;
    private string? _previousVersion;
    private string _currentVersion;
    private string? _installationId;

    /// <summary>
    /// Initializes a new instance of <see cref="FirstRunService"/>.
    /// </summary>
    /// <param name="mediator">MediatR instance for publishing events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public FirstRunService(
        IMediator mediator,
        ILogger<FirstRunService> logger)
        : this(mediator, logger, GetDefaultSettingsPath(), GetDefaultChangelogPath())
    {
    }

    /// <summary>
    /// Internal constructor for testing with custom paths.
    /// </summary>
    internal FirstRunService(
        IMediator mediator,
        ILogger<FirstRunService> logger,
        string settingsPath,
        string changelogPath)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsPath = settingsPath;
        _changelogPath = changelogPath;
        _currentVersion = GetAssemblyVersion();
    }

    /// <inheritdoc/>
    public bool IsFirstRunAfterUpdate
    {
        get
        {
            EnsureInitialized();
            return _isFirstRunAfterUpdate;
        }
    }

    /// <inheritdoc/>
    public bool IsFirstRunEver
    {
        get
        {
            EnsureInitialized();
            return _isFirstRunEver;
        }
    }

    /// <inheritdoc/>
    public string? PreviousVersion
    {
        get
        {
            EnsureInitialized();
            return _previousVersion;
        }
    }

    /// <inheritdoc/>
    public string CurrentVersion => _currentVersion;

    /// <inheritdoc/>
    public string ChangelogPath => _changelogPath;

    /// <inheritdoc/>
    public async Task MarkRunCompletedAsync()
    {
        EnsureInitialized();

        _logger.LogInformation(
            "Marking run completed: Version={Version}, InstallationId={InstallationId}",
            _currentVersion, _installationId);

        var settings = new FirstRunSettings
        {
            LastRunVersion = _currentVersion,
            ShowReleaseNotesOnUpdate = true,
            ShowWelcomeOnFirstRun = true,
            FirstRunDate = _isFirstRunEver ? DateTimeOffset.UtcNow : LoadSettings()?.FirstRunDate,
            InstallationId = _installationId ?? Guid.NewGuid().ToString("N")
        };

        await SaveSettingsAsync(settings);

        // Reset flags after marking complete
        _isFirstRunAfterUpdate = false;
        _isFirstRunEver = false;
        _previousVersion = _currentVersion;
    }

    /// <inheritdoc/>
    public async Task<string> GetReleaseNotesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_changelogPath))
            {
                _logger.LogDebug("Reading release notes from {Path}", _changelogPath);
                return await File.ReadAllTextAsync(_changelogPath, cancellationToken);
            }

            _logger.LogWarning("CHANGELOG.md not found at {Path}", _changelogPath);
            return GetFallbackReleaseNotes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read release notes");
            return GetFallbackReleaseNotes();
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetReleaseNotesForRangeAsync(
        string fromVersion,
        string toVersion,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: For now, return full changelog. Version range parsing can be added later.
        var fullNotes = await GetReleaseNotesAsync(cancellationToken);

        _logger.LogDebug(
            "GetReleaseNotesForRangeAsync called: {From} -> {To} (returning full notes)",
            fromVersion, toVersion);

        return fullNotes;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var settings = LoadSettings();

        _previousVersion = settings?.LastRunVersion;
        _installationId = settings?.InstallationId;

        // Check for Velopack environment variables
        var velopackFirstRun = Environment.GetEnvironmentVariable("LEXICHORD_FIRST_RUN");
        var velopackUpdated = Environment.GetEnvironmentVariable("LEXICHORD_UPDATED");

        // Determine first-run state
        if (_previousVersion is null)
        {
            _isFirstRunEver = true;
            _isFirstRunAfterUpdate = false;

            _logger.LogInformation(
                "First run ever detected: Version={Version}",
                _currentVersion);
        }
        else if (!VersionsMatch(_previousVersion, _currentVersion))
        {
            _isFirstRunEver = false;
            _isFirstRunAfterUpdate = true;

            _logger.LogInformation(
                "First run after update detected: {Previous} -> {Current}",
                _previousVersion, _currentVersion);
        }
        else
        {
            _isFirstRunEver = false;
            _isFirstRunAfterUpdate = false;

            _logger.LogDebug(
                "Normal run: Version={Version}",
                _currentVersion);
        }

        // Override with Velopack signals if present
        if (!string.IsNullOrEmpty(velopackFirstRun))
        {
            _isFirstRunEver = true;
            _logger.LogDebug("Velopack LEXICHORD_FIRST_RUN signal detected");
        }

        if (!string.IsNullOrEmpty(velopackUpdated))
        {
            _isFirstRunAfterUpdate = true;
            _logger.LogDebug("Velopack LEXICHORD_UPDATED signal detected");
        }

        // Publish event if first run detected
        if (_isFirstRunEver || _isFirstRunAfterUpdate)
        {
            // Fire and forget - we don't want to block initialization
            _ = PublishFirstRunEventAsync();
        }
    }

    private async Task PublishFirstRunEventAsync()
    {
        try
        {
            await _mediator.Publish(new FirstRunDetectedEvent(
                _isFirstRunEver,
                _isFirstRunAfterUpdate,
                _previousVersion,
                _currentVersion));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish FirstRunDetectedEvent");
        }
    }

    private FirstRunSettings? LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogDebug("No first-run settings file found at {Path}", _settingsPath);
                return null;
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<FirstRunSettings>(json);

            _logger.LogDebug(
                "Loaded first-run settings: LastVersion={Version}",
                settings?.LastRunVersion);

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load first-run settings");
            return null;
        }
    }

    private async Task SaveSettingsAsync(FirstRunSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_settingsPath, json);
            _logger.LogDebug("Saved first-run settings");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save first-run settings");
        }
    }

    /// <summary>
    /// Compares two version strings with normalization.
    /// </summary>
    /// <remarks>
    /// LOGIC: Normalizes versions by:
    /// - Removing leading 'v'
    /// - Trimming trailing '.0' segments
    /// - Case-insensitive comparison
    /// </remarks>
    private static bool VersionsMatch(string version1, string version2)
    {
        var normalized1 = NormalizeVersion(version1);
        var normalized2 = NormalizeVersion(version2);

        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        // Remove leading 'v'
        var normalized = version.TrimStart('v', 'V');

        // Split into parts and remove trailing zeros
        var parts = normalized.Split('.');
        var significantParts = new List<string>();
        var foundNonZero = false;

        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (parts[i] != "0" || foundNonZero || i < 2)
            {
                significantParts.Insert(0, parts[i]);
                if (parts[i] != "0")
                {
                    foundNonZero = true;
                }
            }
        }

        // Ensure at least Major.Minor
        while (significantParts.Count < 2)
        {
            significantParts.Add("0");
        }

        return string.Join(".", significantParts);
    }

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version ?? new Version(0, 0, 0);
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");
        return Path.Combine(lexichordDir, "first-run-settings.json");
    }

    private static string GetDefaultChangelogPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "CHANGELOG.md");
    }

    private string GetFallbackReleaseNotes()
    {
        return $"""
            # Lexichord {_currentVersion}

            Thank you for using Lexichord!

            The release notes could not be loaded. Please visit our website for the latest changes.

            ---
            *Release notes file was not found at: {_changelogPath}*
            """;
    }
}
