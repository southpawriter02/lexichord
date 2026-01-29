using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
/// <remarks>
/// LOGIC: Manages the settings window state including:
/// - Building and displaying the category navigation tree
/// - Handling search queries and filtering
/// - Loading selected page content with error handling
/// - Publishing close events via MediatR
///
/// The ViewModel gets its data from ISettingsPageRegistry and filters
/// pages based on the user's license tier from ILicenseContext.
///
/// Version: v0.1.6a
/// </remarks>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsPageRegistry _registry;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<SettingsViewModel> _logger;

    /// <summary>
    /// Gets or sets the search query for filtering settings pages.
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets the collection of root-level category nodes.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingsCategoryNode> _categories = [];

    /// <summary>
    /// Gets or sets the currently selected category node.
    /// </summary>
    [ObservableProperty]
    private SettingsCategoryNode? _selectedCategory;

    /// <summary>
    /// Gets or sets the currently selected settings page.
    /// </summary>
    [ObservableProperty]
    private ISettingsPage? _selectedPage;

    /// <summary>
    /// Gets or sets the content control for the currently selected page.
    /// </summary>
    [ObservableProperty]
    private Control? _currentPageContent;

    /// <summary>
    /// Gets or sets the action to close the window.
    /// </summary>
    public Action? CloseWindowAction { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        ISettingsPageRegistry registry,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<SettingsViewModel> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the ViewModel with optional settings.
    /// </summary>
    /// <param name="options">Optional settings for initial state.</param>
    public void Initialize(SettingsWindowOptions? options = null)
    {
        _logger.LogDebug("Initializing SettingsViewModel with options: {@Options}", options);

        // Build the category tree
        BuildCategoryTree();

        // Apply options if provided
        if (options?.SearchQuery is not null)
        {
            SearchQuery = options.SearchQuery;
        }

        if (options?.InitialCategoryId is not null)
        {
            NavigateTo(options.InitialCategoryId);
        }
        else if (Categories.Count > 0)
        {
            SelectedCategory = Categories[0];
        }
    }

    /// <summary>
    /// Called when the search query changes.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            BuildCategoryTree();
        }
        else
        {
            BuildSearchResults(value);
        }
    }

    /// <summary>
    /// Called when the selected category changes.
    /// </summary>
    partial void OnSelectedCategoryChanged(SettingsCategoryNode? value)
    {
        SelectedPage = value?.Page;
    }

    /// <summary>
    /// Called when the selected page changes.
    /// </summary>
    partial void OnSelectedPageChanged(ISettingsPage? value)
    {
        if (value is null)
        {
            CurrentPageContent = null;
            return;
        }

        try
        {
            _logger.LogDebug("Loading settings page view: {CategoryId}", value.CategoryId);
            // LOGIC: Cast to Control - ISettingsPage.CreateView() returns object to avoid Avalonia dependency in Abstractions
            CurrentPageContent = value.CreateView() as Control;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create view for settings page: {CategoryId}", value.CategoryId);
            CurrentPageContent = CreateErrorView(value.CategoryId, ex.Message);
        }
    }

    /// <summary>
    /// Navigates to a specific category by ID.
    /// </summary>
    /// <param name="categoryId">The category ID to navigate to.</param>
    [RelayCommand]
    public void NavigateTo(string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return;
        }

        var node = FindNodeByCategoryId(Categories, categoryId);
        if (node is not null)
        {
            SelectedCategory = node;
            _logger.LogDebug("Navigated to settings category: {CategoryId}", categoryId);
        }
        else
        {
            _logger.LogWarning("Settings category not found: {CategoryId}", categoryId);
        }
    }

    /// <summary>
    /// Closes the settings window and publishes the close event.
    /// </summary>
    [RelayCommand]
    public async Task CloseAsync()
    {
        _logger.LogDebug("Closing settings window");

        // Invoke the close action (set by the view)
        CloseWindowAction?.Invoke();

        // Publish the close event for any listeners
        await _mediator.Publish(new SettingsClosedEvent());
    }

    /// <summary>
    /// Builds the category tree from registered pages.
    /// </summary>
    private void BuildCategoryTree()
    {
        var tier = _licenseContext.GetCurrentTier();
        var pages = _registry.GetPages(tier);

        var nodes = BuildNodeTree(pages, null);
        Categories = new ObservableCollection<SettingsCategoryNode>(nodes);

        _logger.LogDebug(
            "Built category tree with {Count} root nodes",
            Categories.Count);
    }

    /// <summary>
    /// Builds the search results as a flat list.
    /// </summary>
    private void BuildSearchResults(string query)
    {
        var tier = _licenseContext.GetCurrentTier();
        var matchingPages = _registry.SearchPages(query, tier);

        // For search results, show as flat list (no children)
        var nodes = matchingPages
            .Select(p => new SettingsCategoryNode(p, Array.Empty<SettingsCategoryNode>()))
            .ToList();

        Categories = new ObservableCollection<SettingsCategoryNode>(nodes);

        // Auto-select first result
        if (Categories.Count > 0)
        {
            SelectedCategory = Categories[0];
        }

        _logger.LogDebug(
            "Search for '{Query}' found {Count} results",
            query,
            Categories.Count);
    }

    /// <summary>
    /// Recursively builds the node tree from a flat list of pages.
    /// </summary>
    private static List<SettingsCategoryNode> BuildNodeTree(
        IReadOnlyList<ISettingsPage> allPages,
        string? parentId)
    {
        var childPages = allPages
            .Where(p => string.Equals(p.ParentCategoryId, parentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase);

        var nodes = new List<SettingsCategoryNode>();

        foreach (var page in childPages)
        {
            var children = BuildNodeTree(allPages, page.CategoryId);
            nodes.Add(new SettingsCategoryNode(page, children));
        }

        return nodes;
    }

    /// <summary>
    /// Recursively finds a node by category ID.
    /// </summary>
    private static SettingsCategoryNode? FindNodeByCategoryId(
        IEnumerable<SettingsCategoryNode> nodes,
        string categoryId)
    {
        foreach (var node in nodes)
        {
            if (node.CategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var found = FindNodeByCategoryId(node.Children, categoryId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates an error view to display when page creation fails.
    /// </summary>
    private static Control CreateErrorView(string categoryId, string errorMessage)
    {
        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = $"Failed to load settings page: {categoryId}",
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brushes.Red
                },
                new TextBlock
                {
                    Text = errorMessage,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Foreground = Avalonia.Media.Brushes.Gray
                }
            }
        };
    }
}
