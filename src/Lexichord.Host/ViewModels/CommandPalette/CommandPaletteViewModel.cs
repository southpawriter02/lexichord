using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FuzzySharp;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Commands;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.ViewModels.CommandPalette;

/// <summary>
/// ViewModel for the Command Palette.
/// </summary>
/// <remarks>
/// LOGIC: CommandPaletteViewModel manages:
///
/// Search Logic:
/// - Empty query shows all items (commands by category)
/// - Non-empty query triggers fuzzy search via FuzzySharp
/// - Results are ranked by score, filtered above threshold
/// - Match positions calculated for highlighting
///
/// Navigation:
/// - Selected item tracks current highlight
/// - Up/Down wrap around list ends
/// - Enter executes selected, Escape closes
///
/// Modes:
/// - Commands: Shows commands from ICommandRegistry
/// - Files: Shows files (stub for v0.1.5c)
/// </remarks>
public partial class CommandPaletteViewModel : ObservableObject
{
    private const int MinScoreThreshold = 40;
    private const int MaxResults = 50;

    private readonly ICommandRegistry _commandRegistry;
    private readonly IMediator _mediator;
    private readonly ILogger<CommandPaletteViewModel> _logger;

    private List<CommandDefinition> _allCommands = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Placeholder))]
    private PaletteMode _currentMode = PaletteMode.Commands;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private object? _selectedItem;

    /// <summary>
    /// Gets the filtered items to display.
    /// </summary>
    public ObservableCollection<object> FilteredItems { get; } = [];

    /// <summary>
    /// Creates a new CommandPaletteViewModel.
    /// </summary>
    public CommandPaletteViewModel(
        ICommandRegistry commandRegistry,
        IMediator mediator,
        ILogger<CommandPaletteViewModel> logger)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the placeholder text based on current mode.
    /// </summary>
    public string Placeholder => CurrentMode switch
    {
        PaletteMode.Commands => "Type a command name...",
        PaletteMode.Files => "Type a filename...",
        PaletteMode.Symbols => "Type a symbol name...",
        PaletteMode.GoToLine => "Enter line number...",
        _ => "Search..."
    };

    /// <summary>
    /// Shows the palette in the specified mode.
    /// </summary>
    public async Task ShowAsync(PaletteMode mode, string? initialQuery = null)
    {
        _logger.LogInformation("Opening Command Palette in {Mode} mode", mode);

        CurrentMode = mode;
        SearchQuery = initialQuery ?? string.Empty;
        IsVisible = true;

        // LOGIC: Load data based on mode
        if (mode == PaletteMode.Commands)
        {
            _allCommands = _commandRegistry.GetAllCommands()
                .Where(c => c.ShowInPalette)
                .ToList();
        }

        UpdateFilteredItems();

        await _mediator.Publish(new CommandPaletteOpenedEvent(mode));
    }

    /// <summary>
    /// Hides the palette.
    /// </summary>
    [RelayCommand]
    public void Hide()
    {
        _logger.LogDebug("Closing Command Palette");
        IsVisible = false;
        SearchQuery = string.Empty;
        FilteredItems.Clear();
    }

    /// <summary>
    /// Executes the currently selected item.
    /// </summary>
    [RelayCommand]
    public async Task ExecuteSelectedAsync()
    {
        if (SelectedItem is null)
            return;

        if (SelectedItem is CommandSearchResult commandResult)
        {
            _logger.LogInformation(
                "Executing command from palette: {CommandId}",
                commandResult.Command.Id);

            var query = SearchQuery;
            Hide();
            _commandRegistry.TryExecute(commandResult.Command.Id);

            await _mediator.Publish(new CommandPaletteExecutedEvent(
                commandResult.Command.Id,
                commandResult.Command.Title,
                query));
        }
        else if (SelectedItem is FileSearchResult fileResult)
        {
            _logger.LogInformation(
                "Opening file from palette: {FilePath}",
                fileResult.FullPath);

            Hide();
            // TODO: v0.1.5c - Integrate with IEditorService.OpenDocumentAsync
        }
    }

    /// <summary>
    /// Moves selection to the next item.
    /// </summary>
    [RelayCommand]
    public void MoveSelectionDown()
    {
        if (FilteredItems.Count == 0)
            return;

        var currentIndex = FilteredItems.IndexOf(SelectedItem!);
        var nextIndex = (currentIndex + 1) % FilteredItems.Count;
        SelectedItem = FilteredItems[nextIndex];
    }

    /// <summary>
    /// Moves selection to the previous item.
    /// </summary>
    [RelayCommand]
    public void MoveSelectionUp()
    {
        if (FilteredItems.Count == 0)
            return;

        var currentIndex = FilteredItems.IndexOf(SelectedItem!);
        var prevIndex = currentIndex <= 0 ? FilteredItems.Count - 1 : currentIndex - 1;
        SelectedItem = FilteredItems[prevIndex];
    }

    /// <summary>
    /// Moves selection down by a page.
    /// </summary>
    [RelayCommand]
    public void MoveSelectionPageDown()
    {
        if (FilteredItems.Count == 0)
            return;

        var currentIndex = FilteredItems.IndexOf(SelectedItem!);
        var nextIndex = Math.Min(currentIndex + 10, FilteredItems.Count - 1);
        SelectedItem = FilteredItems[nextIndex];
    }

    /// <summary>
    /// Moves selection up by a page.
    /// </summary>
    [RelayCommand]
    public void MoveSelectionPageUp()
    {
        if (FilteredItems.Count == 0)
            return;

        var currentIndex = FilteredItems.IndexOf(SelectedItem!);
        var prevIndex = Math.Max(currentIndex - 10, 0);
        SelectedItem = FilteredItems[prevIndex];
    }

    /// <summary>
    /// Selects the first item.
    /// </summary>
    [RelayCommand]
    public void SelectFirst()
    {
        if (FilteredItems.Count > 0)
            SelectedItem = FilteredItems[0];
    }

    /// <summary>
    /// Selects the last item.
    /// </summary>
    [RelayCommand]
    public void SelectLast()
    {
        if (FilteredItems.Count > 0)
            SelectedItem = FilteredItems[^1];
    }

    partial void OnSearchQueryChanged(string value)
    {
        _logger.LogDebug("Search query changed: \"{Query}\"", value);

        // LOGIC: Check for mode switch prefix
        if (CurrentMode == PaletteMode.Files && value.StartsWith('>'))
        {
            CurrentMode = PaletteMode.Commands;
            SearchQuery = value[1..].TrimStart();
            _allCommands = _commandRegistry.GetAllCommands()
                .Where(c => c.ShowInPalette)
                .ToList();
            return;
        }

        UpdateFilteredItems();
    }

    private void UpdateFilteredItems()
    {
        FilteredItems.Clear();

        var items = CurrentMode switch
        {
            PaletteMode.Commands => SearchCommands(SearchQuery),
            // PaletteMode.Files => SearchFiles(SearchQuery), // v0.1.5c
            _ => Enumerable.Empty<object>()
        };

        foreach (var item in items.Take(MaxResults))
        {
            FilteredItems.Add(item);
        }

        // LOGIC: Auto-select first result
        SelectedItem = FilteredItems.FirstOrDefault();

        _logger.LogDebug(
            "Filtered items: {Count} results for query \"{Query}\"",
            FilteredItems.Count, SearchQuery);
    }

    private IEnumerable<CommandSearchResult> SearchCommands(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // LOGIC: Empty query shows all commands grouped by category
            return _allCommands
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Title)
                .Select(c => CreateCommandResult(c, 100, []));
        }

        // LOGIC: Fuzzy search using FuzzySharp
        var results = new List<CommandSearchResult>();

        foreach (var command in _allCommands)
        {
            // Search against title, category, and tags
            var titleScore = Fuzz.PartialRatio(query, command.Title);
            var categoryScore = Fuzz.PartialRatio(query, command.Category);
            var tagScore = command.Tags?.Max(t => Fuzz.PartialRatio(query, t)) ?? 0;

            var maxScore = Math.Max(titleScore, Math.Max(categoryScore, tagScore));

            if (maxScore >= MinScoreThreshold)
            {
                var matches = CalculateMatchPositions(command.Title, query);
                results.Add(CreateCommandResult(command, maxScore, matches));
            }
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Title);
    }

    private CommandSearchResult CreateCommandResult(
        CommandDefinition command,
        int score,
        IReadOnlyList<MatchPosition> matches)
    {
        // LOGIC: Get shortcut from command's default (v0.1.5d will use IKeyBindingService)
        var shortcutDisplay = command.DefaultShortcut;

        return new CommandSearchResult
        {
            Command = command,
            Score = score,
            TitleMatches = matches,
            ShortcutDisplay = shortcutDisplay
        };
    }

    private static IReadOnlyList<MatchPosition> CalculateMatchPositions(string text, string query)
    {
        // LOGIC: Simple substring matching for highlighting
        // Future: Use more sophisticated algorithm for fuzzy match positions

        var positions = new List<MatchPosition>();
        var textLower = text.ToLowerInvariant();
        var queryLower = query.ToLowerInvariant();

        var index = textLower.IndexOf(queryLower, StringComparison.Ordinal);
        if (index >= 0)
        {
            positions.Add(new MatchPosition(index, query.Length));
        }

        return positions;
    }
}
