using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
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

    /// <summary>
    /// Initializes a new instance of the WindowStateService.
    /// </summary>
    /// <param name="screens">Screen configuration for position validation.</param>
    public WindowStateService(Screens? screens = null)
    {
        _screens = screens;

        // LOGIC: Use platform-appropriate AppData location
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var lexichordDir = Path.Combine(appData, "Lexichord");

        // Create directory if it doesn't exist
        Directory.CreateDirectory(lexichordDir);

        _filePath = Path.Combine(lexichordDir, "appstate.json");
    }

    /// <inheritdoc/>
    public async Task<WindowStateRecord?> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                // LOGIC: No saved state, return null to use defaults
                return null;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var state = JsonSerializer.Deserialize<WindowStateRecord>(json, JsonOptions);

            return state;
        }
        catch (JsonException)
        {
            // LOGIC: Corrupted JSON file—delete and return null
            TryDeleteCorruptedFile();
            return null;
        }
        catch (IOException)
        {
            // LOGIC: File access error—return null to use defaults
            return null;
        }
        catch (Exception)
        {
            // LOGIC: Unexpected error—fail safe to defaults
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
        }
        catch (Exception)
        {
            // LOGIC: Failed to save—silently ignore
            // Window state is non-critical, app close must not fail
        }
    }

    /// <inheritdoc/>
    public bool IsPositionValid(WindowStateRecord state)
    {
        if (_screens is null || _screens.All.Count == 0)
        {
            // LOGIC: No screen info available, assume valid
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

        // LOGIC: Window would be mostly off-screen
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
        }
        catch
        {
            // Ignore deletion errors
        }
    }
}
