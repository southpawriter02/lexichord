using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Manages application updates and version information.
/// </summary>
/// <remarks>
/// LOGIC: Provides channel switching between Stable and Insider,
/// version information display, and update checking (stub in v0.1.6d).
/// Channel preference is persisted to AppData.
///
/// Version: v0.1.6d
/// </remarks>
public sealed class UpdateService : IUpdateService
{
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateService> _logger;
    private readonly VersionInfo _versionInfo;
    private readonly string _settingsPath;

    private UpdateChannel _currentChannel = UpdateChannel.Stable;
    private DateTime? _lastCheckTime;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateService"/>.
    /// </summary>
    /// <param name="mediator">MediatR instance for publishing events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public UpdateService(
        IMediator mediator,
        ILogger<UpdateService> logger)
        : this(mediator, logger, GetDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Internal constructor for testing with custom settings path.
    /// </summary>
    internal UpdateService(
        IMediator mediator,
        ILogger<UpdateService> logger,
        string settingsPath)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsPath = settingsPath;

        _versionInfo = BuildVersionInfo();

        // LOGIC: Load settings synchronously to avoid race conditions
        LoadSettings();

        _logger.LogInformation(
            "UpdateService initialized: Version={Version}, Channel={Channel}",
            _versionInfo.Version, _currentChannel);
    }

    /// <inheritdoc/>
    public UpdateChannel CurrentChannel => _currentChannel;

    /// <inheritdoc/>
    public string CurrentVersion => _versionInfo.Version;

    /// <inheritdoc/>
    public DateTime? LastCheckTime => _lastCheckTime;

    /// <inheritdoc/>
#pragma warning disable CS0067 // Event is never used (stub implementation for v0.1.6d)
    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public event EventHandler<UpdateChannelChangedEventArgs>? ChannelChanged;

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

        await SaveSettingsAsync();

        ChannelChanged?.Invoke(this, new UpdateChannelChangedEventArgs
        {
            OldChannel = oldChannel,
            NewChannel = channel
        });

        await _mediator.Publish(new UpdateChannelChangedEvent(oldChannel, channel));
    }

    /// <inheritdoc/>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Checking for updates on {Channel} channel",
            _currentChannel);

        // LOGIC: Simulate network delay (stub implementation)
        await Task.Delay(500, cancellationToken);

        _lastCheckTime = DateTime.UtcNow;
        await SaveSettingsAsync();

        await _mediator.Publish(
            new UpdateCheckCompletedEvent(false, _versionInfo.Version),
            cancellationToken);

        _logger.LogInformation(
            "Update check complete: Up to date (v{Version})",
            _versionInfo.Version);

        // LOGIC: Stub always returns null (no update available)
        return null;
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
        var gitCommit = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?.Split('+')
            .Skip(1)
            .FirstOrDefault()
            ?.Substring(0, Math.Min(7, assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion?.Split('+').Skip(1).FirstOrDefault()?.Length ?? 0));

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
