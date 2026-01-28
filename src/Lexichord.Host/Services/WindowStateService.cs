using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Host.Services;

/// <summary>
/// Persists window state to a JSON file in the user's AppData directory.
/// </summary>
/// <remarks>
/// LOGIC: Window state is saved on close and loaded on startup.
/// The file location follows platform conventions via Environment.SpecialFolder.
///
/// The service is designed to be fault-tolerant:
/// - LoadAsync returns null if file doesn't exist or is corrupted
/// - SaveAsync swallows exceptions to avoid disrupting app close
/// - IsPositionValid checks against current screen configuration
/// </remarks>
public sealed class WindowStateService : IWindowStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly Screens? _screens;
    private readonly ILogger<WindowStateService> _logger;

    /// <summary>
    /// Initializes a new instance of the WindowStateService.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostics.</param>
    /// <param name="screens">Screen configuration for position validation.</param>
    public WindowStateService(ILogger<WindowStateService> logger, Screens? screens = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _screens = screens;

        // LOGIC: Use platform-appropriate AppData location
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");

        // Create directory if it doesn't exist
        Directory.CreateDirectory(lexichordDir);

        _filePath = Path.Combine(lexichordDir, "appstate.json");
        
        _logger.LogDebug("WindowStateService initialized. State file: {FilePath}", _filePath);
    }

    /// <inheritdoc/>
    public async Task<WindowStateRecord?> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogDebug("No saved window state found at {FilePath}", _filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var state = JsonSerializer.Deserialize<WindowStateRecord>(json, JsonOptions);

            _logger.LogDebug(
                "Loaded window state: Position=({X},{Y}), Size=({Width}x{Height})",
                state?.X, state?.Y, state?.Width, state?.Height);

            return state;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Corrupted window state file at {FilePath}, deleting", _filePath);
            TryDeleteCorruptedFile();
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read window state from {FilePath}", _filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error loading window state from {FilePath}", _filePath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(WindowStateRecord state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
            
            _logger.LogDebug("Saved window state to {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            // LOGIC: Failed to save—log but continue
            // Window state is non-critical, app close must not fail
            _logger.LogWarning(ex, "Failed to save window state to {FilePath}", _filePath);
        }
    }

    /// <inheritdoc/>
    public bool IsPositionValid(WindowStateRecord state)
    {
        if (_screens is null || _screens.All.Count == 0)
        {
            _logger.LogDebug("No screen info available, assuming position valid");
            return true;
        }

        // LOGIC: Check if at least 100x100 of the window is visible on any screen
        const int minVisiblePixels = 100;

        var windowRect = new PixelRect(
            (int)state.X,
            (int)state.Y,
            (int)state.Width,
            (int)state.Height
        );

        foreach (var screen in _screens.All)
        {
            var intersection = screen.Bounds.Intersect(windowRect);

            if (intersection.Width >= minVisiblePixels &&
                intersection.Height >= minVisiblePixels)
            {
                return true;
            }
        }

        _logger.LogDebug(
            "Window position ({X},{Y}) is mostly off-screen, will use defaults",
            state.X, state.Y);

        return false;
    }

    /// <summary>
    /// Attempts to delete a corrupted state file.
    /// </summary>
    private void TryDeleteCorruptedFile()
    {
        try
        {
            File.Delete(_filePath);
            _logger.LogDebug("Deleted corrupted state file at {FilePath}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete corrupted state file at {FilePath}", _filePath);
        }
    }
}
