using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;

// Type aliases to avoid ambiguity with Velopack types
using LexichordUpdateInfo = Lexichord.Abstractions.Contracts.UpdateInfo;
using LexichordUpdateOptions = Lexichord.Abstractions.Contracts.UpdateOptions;
using VelopackUpdateInfo = Velopack.UpdateInfo;

namespace Lexichord.Host.Services;

/// <summary>
/// Manages application updates and version information using Velopack.
/// </summary>
/// <remarks>
/// LOGIC: Provides channel switching between Stable and Insider,
/// version information display, and real update functionality via Velopack.
/// Channel preference is persisted to AppData.
///
/// Version: v0.1.7a
/// </remarks>
public sealed class UpdateService : IUpdateService
{
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateService> _logger;
    private readonly VersionInfo _versionInfo;
    private readonly string _settingsPath;
    private readonly LexichordUpdateOptions _options;

    private UpdateManager? _updateManager;
    private VelopackUpdateInfo? _cachedVelopackUpdate;
    private LexichordUpdateInfo? _cachedUpdateInfo;
    private UpdateChannel _currentChannel = UpdateChannel.Stable;
    private DateTime? _lastCheckTime;
    private bool _isUpdateReady;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateService"/>.
    /// </summary>
    /// <param name="mediator">MediatR instance for publishing events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="options">Update configuration options.</param>
    public UpdateService(
        IMediator mediator,
        ILogger<UpdateService> logger,
        IOptions<LexichordUpdateOptions> options)
        : this(mediator, logger, options.Value, GetDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Internal constructor for testing with custom settings path.
    /// </summary>
    internal UpdateService(
        IMediator mediator,
        ILogger<UpdateService> logger,
        LexichordUpdateOptions options,
        string settingsPath)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _settingsPath = settingsPath;

        _versionInfo = BuildVersionInfo();

        // LOGIC: Load settings synchronously to avoid race conditions
        LoadSettings();

        // LOGIC: Initialize UpdateManager only if we have a valid URL
        InitializeUpdateManager();

        _logger.LogInformation(
            "UpdateService initialized: Version={Version}, Channel={Channel}, IsInstalled={IsInstalled}",
            _versionInfo.Version, _currentChannel, _updateManager?.IsInstalled ?? false);
    }

    /// <inheritdoc/>
    public UpdateChannel CurrentChannel => _currentChannel;

    /// <inheritdoc/>
    public string CurrentVersion => _versionInfo.Version;

    /// <inheritdoc/>
    public DateTime? LastCheckTime => _lastCheckTime;

    /// <inheritdoc/>
    public bool IsUpdateReady => _isUpdateReady;

    /// <inheritdoc/>
    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    /// <inheritdoc/>
    public event EventHandler<UpdateChannelChangedEventArgs>? ChannelChanged;

    /// <inheritdoc/>
    public event EventHandler<DownloadProgressEventArgs>? UpdateProgress;

    /// <inheritdoc/>
    public VersionInfo GetVersionInfo() => _versionInfo;

    /// <inheritdoc/>
    public async Task SetChannelAsync(UpdateChannel channel)
    {
        if (_currentChannel == channel)
        {
            _logger.LogDebug("Channel already set to {Channel}", channel);
            return;
        }

        var oldChannel = _currentChannel;
        _currentChannel = channel;

        _logger.LogInformation(
            "Update channel changed: {OldChannel} -> {NewChannel}",
            oldChannel, channel);

        // LOGIC: Reinitialize UpdateManager with new channel URL
        InitializeUpdateManager();
        _isUpdateReady = false;
        _cachedVelopackUpdate = null;
        _cachedUpdateInfo = null;

        await SaveSettingsAsync();

        ChannelChanged?.Invoke(this, new UpdateChannelChangedEventArgs
        {
            OldChannel = oldChannel,
            NewChannel = channel
        });

        await _mediator.Publish(new UpdateChannelChangedEvent(oldChannel, channel));
    }

    /// <inheritdoc/>
    public async Task<LexichordUpdateInfo?> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Checking for updates on {Channel} channel",
            _currentChannel);

        if (_updateManager is null)
        {
            _logger.LogDebug("Update check skipped: UpdateManager not initialized");
            await RecordCheckComplete(false, cancellationToken);
            return null;
        }

        if (!_updateManager.IsInstalled)
        {
            _logger.LogDebug("Update check skipped: Not installed via Velopack");
            await RecordCheckComplete(false, cancellationToken);
            return null;
        }

        try
        {
            _cachedVelopackUpdate = await _updateManager.CheckForUpdatesAsync();

            if (_cachedVelopackUpdate is null)
            {
                _logger.LogInformation(
                    "Update check complete: Up to date (v{Version})",
                    _versionInfo.Version);
                await RecordCheckComplete(false, cancellationToken);
                return null;
            }

            // Convert Velopack UpdateInfo to our UpdateInfo type
            var targetRelease = _cachedVelopackUpdate.TargetFullRelease;
            _cachedUpdateInfo = new LexichordUpdateInfo(
                Version: targetRelease.Version.ToString(),
                ReleaseNotes: targetRelease.NotesMarkdown ?? string.Empty,
                DownloadUrl: string.Empty, // Velopack handles download internally
                ReleaseDate: DateTime.UtcNow, // Release date not exposed by Velopack
                IsCritical: false,
                DownloadSize: targetRelease.Size);

            _logger.LogInformation(
                "Update available: {CurrentVersion} -> {NewVersion} (Size={Size})",
                _versionInfo.Version,
                _cachedUpdateInfo.Version,
                _cachedUpdateInfo.FormattedDownloadSize);

            UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
            {
                Update = _cachedUpdateInfo
            });

            await _mediator.Publish(new UpdateAvailableEvent(_cachedUpdateInfo), cancellationToken);
            await RecordCheckComplete(true, cancellationToken);

            return _cachedUpdateInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            await RecordCheckComplete(false, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DownloadUpdatesAsync(
        LexichordUpdateInfo update,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_updateManager is null)
        {
            _logger.LogWarning("Download skipped: UpdateManager not initialized");
            return;
        }

        if (!_updateManager.IsInstalled)
        {
            _logger.LogWarning("Download skipped: Not installed via Velopack");
            return;
        }

        if (_cachedVelopackUpdate is null)
        {
            _logger.LogWarning("Download skipped: No update has been checked");
            return;
        }

        _logger.LogInformation("Downloading update to v{Version}", update.Version);

        try
        {
            // Velopack uses Action<int> for progress, not IProgress<int>
            Action<int>? progressAction = null;
            if (progress is not null || UpdateProgress is not null)
            {
                progressAction = percent =>
                {
                    var normalized = percent / 100f;
                    progress?.Report(normalized);
                    UpdateProgress?.Invoke(this, new DownloadProgressEventArgs(
                        normalized,
                        (long)(percent * (update.DownloadSize ?? 0) / 100),
                        update.DownloadSize ?? 0));
                };
            }

            await _updateManager.DownloadUpdatesAsync(_cachedVelopackUpdate, progressAction);

            _isUpdateReady = true;
            _logger.LogInformation("Update download complete: v{Version}", update.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download update");
            throw;
        }
    }

    /// <inheritdoc/>
    public void ApplyUpdatesAndRestart()
    {
        if (!_isUpdateReady || _cachedVelopackUpdate is null)
        {
            throw new InvalidOperationException("No update is ready to apply. Call DownloadUpdatesAsync first.");
        }

        if (_updateManager is null)
        {
            throw new InvalidOperationException("UpdateManager is not initialized.");
        }

        _logger.LogInformation("Applying update and restarting application");

        // LOGIC: This method does not return - the application will exit and restart
        _updateManager.ApplyUpdatesAndRestart(_cachedVelopackUpdate);
    }

    private void InitializeUpdateManager()
    {
        var url = _options.GetUrlForChannel(_currentChannel);

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogDebug("UpdateManager not initialized: No URL configured for {Channel} channel", _currentChannel);
            _updateManager = null;
            return;
        }

        try
        {
            _updateManager = new UpdateManager(url);
            _logger.LogDebug("UpdateManager initialized with URL: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize UpdateManager");
            _updateManager = null;
        }
    }

    private async Task RecordCheckComplete(bool updateFound, CancellationToken cancellationToken)
    {
        _lastCheckTime = DateTime.UtcNow;
        await SaveSettingsAsync();

        await _mediator.Publish(
            new UpdateCheckCompletedEvent(updateFound, _versionInfo.Version),
            cancellationToken);
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");
        Directory.CreateDirectory(lexichordDir);
        return Path.Combine(lexichordDir, "update-settings.json");
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogDebug("No update settings file found, using defaults");
                return;
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<Settings.UpdateSettings>(json);

            if (settings is not null)
            {
                _currentChannel = settings.GetUpdateChannel();
                _lastCheckTime = settings.LastCheckTime;
                _logger.LogDebug("Loaded update settings: Channel={Channel}", _currentChannel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load update settings, using defaults");
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new Settings.UpdateSettings
            {
                Channel = _currentChannel.ToString(),
                LastCheckTime = _lastCheckTime
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_settingsPath, json);
            _logger.LogDebug("Saved update settings");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save update settings");
        }
    }

    private static VersionInfo BuildVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Get version from assembly
        var version = assembly.GetName().Version ?? new Version(0, 0, 0);
        var versionString = $"{version.Major}.{version.Minor}.{version.Build}";
        var fullVersion = version.ToString();

        // Get build date from assembly metadata
        var buildDate = GetBuildDate(assembly);

        // Try to get git info from assembly attributes
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var gitCommit = infoVersion?.Split('+')
            .Skip(1)
            .FirstOrDefault();
        if (gitCommit is not null && gitCommit.Length > 7)
        {
            gitCommit = gitCommit.Substring(0, 7);
        }

        // Determine if debug build
#if DEBUG
        var isDebug = true;
#else
        var isDebug = false;
#endif

        // Build runtime info
        var runtime = $".NET {RuntimeInformation.FrameworkDescription.Replace(".NET ", "")}";

        return new VersionInfo(
            versionString,
            fullVersion,
            buildDate,
            gitCommit,
            null, // GitBranch not easily available at runtime
            isDebug,
            runtime
        );
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        // LOGIC: Try to get build date from AssemblyMetadata attribute
        var buildDateAttr = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate");

        if (buildDateAttr is not null && DateTime.TryParse(buildDateAttr.Value, out var date))
        {
            return date;
        }

        // Fallback: use file modification time
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
        {
            return File.GetLastWriteTimeUtc(location);
        }

        return DateTime.UtcNow;
    }
}
