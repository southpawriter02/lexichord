// -----------------------------------------------------------------------
// <copyright file="ClipboardService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Implementation of IClipboardService using Avalonia's IClipboard (v0.7.6c).
//   Provides platform-agnostic clipboard operations with UI thread marshalling.
//
//   Thread Safety:
//     - All operations are dispatched to the UI thread via Dispatcher
//     - Safe to call from background threads
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.SummaryExport.Services;

/// <summary>
/// Implementation of <see cref="IClipboardService"/> using Avalonia's clipboard API.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service wraps Avalonia's <see cref="IClipboard"/> interface
/// to provide platform-agnostic clipboard operations. All operations are marshalled
/// to the UI thread as required by most UI frameworks.
/// </para>
/// <para>
/// <b>Platform Support:</b>
/// Uses Avalonia's cross-platform clipboard implementation, which handles
/// Windows, macOS, and Linux automatically.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// All operations use <see cref="Dispatcher.UIThread"/> to ensure thread-safe
/// access to the clipboard. Safe to call from background threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
public sealed class ClipboardService : IClipboardService
{
    private readonly ILogger<ClipboardService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardService"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public ClipboardService(ILogger<ClipboardService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SetTextAsync(string text, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        _logger.LogDebug("Setting clipboard text ({CharCount} characters)", text.Length);

        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var clipboard = GetClipboard();
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                    _logger.LogDebug("Clipboard text set successfully");
                }
                else
                {
                    _logger.LogWarning("Clipboard not available - no application instance");
                }
            }, DispatcherPriority.Normal, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("SetTextAsync cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set clipboard text");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetTextAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Getting clipboard text");

        try
        {
            var task = await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var clipboard = GetClipboard();
                if (clipboard != null)
                {
                    var text = await clipboard.GetTextAsync();
                    _logger.LogDebug(
                        "Retrieved clipboard text: {HasContent}",
                        text != null ? $"{text.Length} characters" : "null");
                    return text;
                }

                _logger.LogWarning("Clipboard not available - no application instance");
                return null;
            }, DispatcherPriority.Normal, ct);

            return await task;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("GetTextAsync cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get clipboard text");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Clearing clipboard");

        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var clipboard = GetClipboard();
                if (clipboard != null)
                {
                    await clipboard.ClearAsync();
                    _logger.LogDebug("Clipboard cleared successfully");
                }
                else
                {
                    _logger.LogWarning("Clipboard not available - no application instance");
                }
            }, DispatcherPriority.Normal, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("ClearAsync cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear clipboard");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ContainsTextAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Checking if clipboard contains text");

        try
        {
            var task = await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var clipboard = GetClipboard();
                if (clipboard != null)
                {
                    // Avalonia doesn't have a direct ContainsText method,
                    // so we check if GetTextAsync returns non-null
                    var text = await clipboard.GetTextAsync();
                    var hasText = text != null;
                    _logger.LogDebug("Clipboard contains text: {HasText}", hasText);
                    return hasText;
                }

                _logger.LogWarning("Clipboard not available - no application instance");
                return false;
            }, DispatcherPriority.Normal, ct);

            return await task;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("ContainsTextAsync cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check clipboard contents");
            throw;
        }
    }

    /// <summary>
    /// Gets the clipboard instance from the current application.
    /// </summary>
    /// <returns>The clipboard instance, or <c>null</c> if not available.</returns>
    private static IClipboard? GetClipboard()
    {
        // LOGIC: Access the clipboard via the current application's top-level control.
        // This approach is used in Avalonia 11.x for clipboard access.
        return Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.Clipboard
            : null;
    }
}
