using Lexichord.Abstractions.Contracts.Commands;
using Lexichord.Host.ViewModels.CommandPalette;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Service for controlling the Command Palette.
/// </summary>
/// <remarks>
/// LOGIC: Bridges ICommandPaletteService to CommandPaletteViewModel.
/// Registered as a singleton to enable consistent state.
/// </remarks>
public class CommandPaletteService : ICommandPaletteService
{
    private readonly CommandPaletteViewModel _viewModel;
    private readonly ILogger<CommandPaletteService> _logger;

    /// <inheritdoc />
    public bool IsVisible => _viewModel.IsVisible;

    /// <inheritdoc />
    public PaletteMode CurrentMode => _viewModel.CurrentMode;

    /// <inheritdoc />
    public event EventHandler<PaletteVisibilityChangedEventArgs>? VisibilityChanged;

    /// <summary>
    /// Creates a new CommandPaletteService.
    /// </summary>
    public CommandPaletteService(
        CommandPaletteViewModel viewModel,
        ILogger<CommandPaletteService> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Forward visibility changes to event
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CommandPaletteViewModel.IsVisible))
            {
                VisibilityChanged?.Invoke(this, new PaletteVisibilityChangedEventArgs
                {
                    IsVisible = _viewModel.IsVisible,
                    Mode = _viewModel.CurrentMode
                });
            }
        };
    }

    /// <inheritdoc />
    public async Task ShowAsync(PaletteMode mode = PaletteMode.Commands, string? initialQuery = null)
    {
        _logger.LogInformation("Showing Command Palette in {Mode} mode", mode);
        await _viewModel.ShowAsync(mode, initialQuery);
    }

    /// <inheritdoc />
    public void Hide()
    {
        _logger.LogDebug("Hiding Command Palette");
        _viewModel.Hide();
    }

    /// <inheritdoc />
    public async Task ToggleAsync(PaletteMode mode = PaletteMode.Commands)
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            await ShowAsync(mode);
        }
    }
}
