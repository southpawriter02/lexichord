using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dock.Model.Controls;
using Dock.Model.Core;
using Lexichord.Abstractions.Layout;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Layout;

/// <summary>
/// JSON-based layout persistence service.
/// </summary>
/// <remarks>
/// LOGIC: JsonLayoutService serializes the dock hierarchy to JSON files.
/// Layouts are stored in {AppData}/Lexichord/Layouts/{ProfileName}.json.
///
/// Key Features:
/// - Atomic writes via temp file + rename
/// - Auto-save with debouncing (500ms)
/// - Schema versioning for forward compatibility
/// - Profile management (multiple layouts)
/// </remarks>
public sealed class JsonLayoutService : ILayoutService, IDisposable
{
    private readonly ILogger<JsonLayoutService> _logger;
    private readonly IDockFactory _dockFactory;
    private readonly string _layoutDirectory;
    private readonly Timer _autoSaveTimer;
    private readonly object _autoSaveLock = new();
    private bool _autoSavePending;
    private bool _disposed;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const int AutoSaveDelayMs = 500;
    private const string DefaultProfile = "Default";

    public string CurrentProfileName { get; private set; } = DefaultProfile;
    public string LayoutDirectory => _layoutDirectory;

    public event EventHandler<LayoutSavedEventArgs>? LayoutSaved;
    public event EventHandler<LayoutLoadedEventArgs>? LayoutLoaded;

    public JsonLayoutService(
        ILogger<JsonLayoutService> logger,
        IDockFactory dockFactory,
        string? layoutDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dockFactory = dockFactory ?? throw new ArgumentNullException(nameof(dockFactory));

        // LOGIC: Store layouts in user's AppData directory (or custom path for testing)
        if (layoutDirectory is not null)
        {
            _layoutDirectory = layoutDirectory;
        }
        else
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _layoutDirectory = Path.Combine(appDataPath, "Lexichord", "Layouts");
        }

        // LOGIC: Timer for debounced auto-save. Disabled by infinite due-time initially.
        _autoSaveTimer = new Timer(
            AutoSaveCallback,
            state: null,
            dueTime: Timeout.Infinite,
            period: Timeout.Infinite);

        _logger.LogDebug("JsonLayoutService initialized. Layout directory: {Directory}", _layoutDirectory);
    }

    /// <inheritdoc/>
    public async Task<bool> SaveLayoutAsync(
        string profileName = DefaultProfile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var root = _dockFactory.RootDock;
            if (root is null)
            {
                _logger.LogWarning("Cannot save layout: RootDock is null");
                return false;
            }

            var sanitizedName = SanitizeProfileName(profileName);
            var layoutData = BuildLayoutData(root, sanitizedName);

            // Ensure directory exists
            Directory.CreateDirectory(_layoutDirectory);

            var filePath = GetProfilePath(sanitizedName);
            var tempPath = filePath + ".tmp";

            // LOGIC: Atomic write via temp file + rename
            var json = JsonSerializer.Serialize(layoutData, SerializerOptions);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, filePath, overwrite: true);

            CurrentProfileName = sanitizedName;

            _logger.LogInformation("Layout saved to profile: {Profile}", sanitizedName);

            LayoutSaved?.Invoke(this, new LayoutSavedEventArgs(
                sanitizedName,
                filePath,
                WasAutoSave: false));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save layout to profile: {Profile}", profileName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> LoadLayoutAsync(
        string profileName = DefaultProfile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedName = SanitizeProfileName(profileName);
            var filePath = GetProfilePath(sanitizedName);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Layout profile not found: {Profile}", sanitizedName);
                return false;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var layoutData = JsonSerializer.Deserialize<LayoutData>(json, SerializerOptions);

            if (layoutData is null)
            {
                _logger.LogWarning("Failed to deserialize layout: {Profile}", sanitizedName);
                return false;
            }

            // LOGIC: Check for schema migration
            var wasMigrated = false;
            if (layoutData.Metadata.SchemaVersion < LayoutMetadata.CurrentSchemaVersion)
            {
                _logger.LogInformation(
                    "Migrating layout from schema v{Old} to v{New}",
                    layoutData.Metadata.SchemaVersion,
                    LayoutMetadata.CurrentSchemaVersion);

                layoutData = MigrateLayout(layoutData);
                wasMigrated = true;
            }

            ApplyLayoutData(layoutData);
            CurrentProfileName = sanitizedName;

            _logger.LogInformation("Layout loaded from profile: {Profile}", sanitizedName);

            LayoutLoaded?.Invoke(this, new LayoutLoadedEventArgs(
                sanitizedName,
                layoutData.Metadata.SchemaVersion,
                wasMigrated));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load layout from profile: {Profile}", profileName);
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteLayoutAsync(
        string profileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedName = SanitizeProfileName(profileName);
            var filePath = GetProfilePath(sanitizedName);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Cannot delete: profile not found: {Profile}", sanitizedName);
                return Task.FromResult(false);
            }

            File.Delete(filePath);
            _logger.LogInformation("Layout profile deleted: {Profile}", sanitizedName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete layout profile: {Profile}", profileName);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetProfileNamesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_layoutDirectory))
            {
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }

            var profiles = Directory.GetFiles(_layoutDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .ToArray();

            return Task.FromResult<IEnumerable<string>>(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate layout profiles");
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }
    }

    /// <inheritdoc/>
    public Task<bool> ProfileExistsAsync(
        string profileName,
        CancellationToken cancellationToken = default)
    {
        var sanitizedName = SanitizeProfileName(profileName);
        var filePath = GetProfilePath(sanitizedName);
        return Task.FromResult(File.Exists(filePath));
    }

    /// <inheritdoc/>
    public async Task<bool> ResetToDefaultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // LOGIC: Create fresh default layout from factory
            _ = _dockFactory.CreateDefaultLayout();

            // Save it as current profile
            return await SaveLayoutAsync(CurrentProfileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset to default layout");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExportLayoutAsync(
        string profileName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedName = SanitizeProfileName(profileName);
            var sourcePath = GetProfilePath(sanitizedName);

            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Cannot export: profile not found: {Profile}", sanitizedName);
                return false;
            }

            // LOGIC: Read and write asynchronously for export
            var content = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);
            _logger.LogInformation("Layout exported from {Profile} to {Path}", sanitizedName, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export layout");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ImportLayoutAsync(
        string filePath,
        string profileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Cannot import: file not found: {Path}", filePath);
                return false;
            }

            // LOGIC: Validate the file is valid JSON layout
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var layoutData = JsonSerializer.Deserialize<LayoutData>(json, SerializerOptions);

            if (layoutData is null)
            {
                _logger.LogWarning("Cannot import: invalid layout file: {Path}", filePath);
                return false;
            }

            // Ensure directory exists
            Directory.CreateDirectory(_layoutDirectory);

            var sanitizedName = SanitizeProfileName(profileName);
            var destPath = GetProfilePath(sanitizedName);
            File.Copy(filePath, destPath, overwrite: true);

            _logger.LogInformation("Layout imported to profile: {Profile}", sanitizedName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import layout");
            return false;
        }
    }

    /// <inheritdoc/>
    public void TriggerAutoSave()
    {
        if (_disposed) return;

        lock (_autoSaveLock)
        {
            _autoSavePending = true;
            // LOGIC: Reset the timer to debounce rapid changes
            _autoSaveTimer.Change(AutoSaveDelayMs, Timeout.Infinite);
        }
    }

    private void AutoSaveCallback(object? state)
    {
        if (_disposed) return;

        lock (_autoSaveLock)
        {
            if (!_autoSavePending) return;
            _autoSavePending = false;
        }

        try
        {
            var root = _dockFactory.RootDock;
            if (root is null) return;

            var layoutData = BuildLayoutData(root, CurrentProfileName);

            // Ensure directory exists
            Directory.CreateDirectory(_layoutDirectory);

            var filePath = GetProfilePath(CurrentProfileName);
            var tempPath = filePath + ".tmp";

            // LOGIC: Atomic write via temp file + rename
            var json = JsonSerializer.Serialize(layoutData, SerializerOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Auto-saved layout to profile: {Profile}", CurrentProfileName);

            LayoutSaved?.Invoke(this, new LayoutSavedEventArgs(
                CurrentProfileName,
                filePath,
                WasAutoSave: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-save failed");
        }
    }

    private LayoutData BuildLayoutData(IRootDock root, string profileName)
    {
        var metadata = new LayoutMetadata(
            LayoutMetadata.CurrentSchemaVersion,
            profileName,
            DateTime.UtcNow,
            GetAppVersion());

        var rootNode = SerializeDockNode(root);

        return new LayoutData(metadata, rootNode);
    }

    private DockNodeData SerializeDockNode(IDockable dockable)
    {
        var type = GetNodeType(dockable);
        var properties = BuildNodeProperties(dockable, type);
        var children = dockable switch
        {
            IDock dock when dock.VisibleDockables?.Count > 0 =>
                dock.VisibleDockables.Select(SerializeDockNode).ToList(),
            _ => null
        };

        return new DockNodeData(
            dockable.Id ?? Guid.NewGuid().ToString(),
            type,
            properties,
            children);
    }

    private static DockNodeType GetNodeType(IDockable dockable)
    {
        return dockable switch
        {
            IRootDock => DockNodeType.Root,
            IProportionalDock => DockNodeType.Proportional,
            IToolDock => DockNodeType.Tool,
            IDocumentDock => DockNodeType.Document,
            IProportionalDockSplitter => DockNodeType.Splitter,
            _ => DockNodeType.Dockable
        };
    }

    private static DockNodeProperties BuildNodeProperties(IDockable dockable, DockNodeType type)
    {
        var title = dockable.Title;
        double? proportion = dockable switch
        {
            IDock dock => dock.Proportion,
            _ => null
        };

        DockOrientation? orientation = dockable switch
        {
            IProportionalDock pd => pd.Orientation == Dock.Model.Core.Orientation.Horizontal
                ? DockOrientation.Horizontal
                : DockOrientation.Vertical,
            _ => null
        };

        DockAlignment? alignment = dockable switch
        {
            IToolDock td => td.Alignment switch
            {
                Dock.Model.Core.Alignment.Left => DockAlignment.Left,
                Dock.Model.Core.Alignment.Right => DockAlignment.Right,
                Dock.Model.Core.Alignment.Top => DockAlignment.Top,
                Dock.Model.Core.Alignment.Bottom => DockAlignment.Bottom,
                _ => null
            },
            _ => null
        };

        bool? isCollapsed = dockable switch
        {
            IToolDock td => td.IsCollapsable ? (bool?)td.AutoHide : null,
            _ => null
        };

        bool? isActive = dockable switch
        {
            IDock dock => dock.ActiveDockable == dockable,
            _ => null
        };

        string? activeChildId = dockable switch
        {
            IDock dock => dock.ActiveDockable?.Id,
            _ => null
        };

        var canClose = dockable.CanClose;
        var canFloat = dockable.CanFloat;

        return new DockNodeProperties(
            title,
            proportion,
            orientation,
            alignment,
            isCollapsed,
            isActive,
            activeChildId,
            canClose,
            canFloat);
    }

    private void ApplyLayoutData(LayoutData layoutData)
    {
        // LOGIC: For now, we just use this to validate the layout was loaded.
        // Full dock tree reconstruction requires integration with Dock.Avalonia
        // which will be completed in v0.1.1d.

        _logger.LogDebug(
            "Layout data loaded: Profile={Profile}, Version={Version}, RootId={RootId}",
            layoutData.Metadata.ProfileName,
            layoutData.Metadata.SchemaVersion,
            layoutData.Root.Id);
    }

    private LayoutData MigrateLayout(LayoutData oldLayout)
    {
        // LOGIC: No migrations needed for schema v1.
        // When schema version is bumped, add migration code here.
        return oldLayout;
    }

    private string GetProfilePath(string profileName)
    {
        return Path.Combine(_layoutDirectory, $"{profileName}.json");
    }

    private static string SanitizeProfileName(string profileName)
    {
        // LOGIC: Prevent path traversal attacks
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(profileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? DefaultProfile : sanitized;
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetName()
            .Version?.ToString() ?? "0.0.0";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _autoSaveTimer.Dispose();
    }
}
