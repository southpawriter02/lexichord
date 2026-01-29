namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Options for opening the Settings window with specific initial state.
/// </summary>
/// <remarks>
/// LOGIC: Enables deep linking into specific settings pages or pre-populating
/// the search query. Used when opening settings from context menus, error
/// dialogs, or other locations that want to navigate to a specific page.
///
/// Version: v0.1.6a
/// </remarks>
/// <example>
/// <code>
/// // Open settings to a specific page:
/// var options = new SettingsWindowOptions(InitialCategoryId: "editor.fonts");
/// await settingsService.ShowSettingsAsync(options);
///
/// // Open settings with search pre-filled:
/// var options = new SettingsWindowOptions(SearchQuery: "theme");
/// await settingsService.ShowSettingsAsync(options);
/// </code>
/// </example>
/// <param name="InitialCategoryId">
/// The category ID to navigate to when the window opens.
/// If null, the first category is selected.
/// </param>
/// <param name="SearchQuery">
/// A search query to pre-populate in the search box.
/// If provided, the settings window shows search results instead of the tree.
/// </param>
public record SettingsWindowOptions(
    string? InitialCategoryId = null,
    string? SearchQuery = null
);
