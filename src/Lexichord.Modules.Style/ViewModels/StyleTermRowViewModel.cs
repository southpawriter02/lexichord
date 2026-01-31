using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Entities;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// Row ViewModel for displaying a StyleTerm in the Lexicon DataGrid.
/// </summary>
/// <remarks>
/// LOGIC: Wraps a StyleTerm entity with computed display properties:
/// - Severity badge helpers (IsError, IsWarning, etc.)
/// - SeveritySortOrder for custom DataGrid sorting
/// - ActiveIcon and ActiveColor for status indicator
///
/// This ViewModel is immutable after construction - to update,
/// create a new instance from the modified StyleTerm.
///
/// Version: v0.2.5a
/// </remarks>
public partial class StyleTermRowViewModel : ObservableObject
{
    private readonly StyleTerm _term;

    /// <summary>
    /// Initializes a new instance wrapping the specified StyleTerm.
    /// </summary>
    /// <param name="term">The underlying StyleTerm entity.</param>
    public StyleTermRowViewModel(StyleTerm term)
    {
        _term = term ?? throw new ArgumentNullException(nameof(term));
    }

    #region StyleTerm Properties (Delegated)

    /// <summary>Gets the term's unique identifier.</summary>
    public Guid Id => _term.Id;

    /// <summary>Gets the text pattern to match.</summary>
    public string Term => _term.Term;

    /// <summary>Gets the suggested replacement text.</summary>
    public string? Replacement => _term.Replacement;

    /// <summary>Gets the grouping category.</summary>
    public string Category => _term.Category;

    /// <summary>Gets the severity level as string.</summary>
    public string Severity => _term.Severity;

    /// <summary>Gets whether the term is active.</summary>
    public bool IsActive => _term.IsActive;

    /// <summary>Gets the optional notes.</summary>
    public string? Notes => _term.Notes;

    /// <summary>Gets the underlying StyleTerm entity.</summary>
    public StyleTerm Entity => _term;

    /// <summary>Gets whether fuzzy matching is enabled for this term.</summary>
    /// <remarks>v0.3.1d - Writer Pro feature</remarks>
    public bool FuzzyEnabled => _term.FuzzyEnabled;

    /// <summary>Gets the fuzzy match threshold (0.0-1.0).</summary>
    /// <remarks>v0.3.1d - Writer Pro feature</remarks>
    public double FuzzyThreshold => _term.FuzzyThreshold;

    /// <summary>Gets a display string for the fuzzy status.</summary>
    /// <remarks>v0.3.1d - Shows "80%" or "Off" in the grid</remarks>
    public string FuzzyStatusText => FuzzyEnabled ? $"{FuzzyThreshold * 100:0}%" : "Off";


    #endregion


    #region Severity Badge Helpers

    /// <summary>Gets whether this term has Error severity.</summary>
    public bool IsError => string.Equals(Severity, "Error", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether this term has Warning severity.</summary>
    public bool IsWarning => string.Equals(Severity, "Warning", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether this term has Suggestion severity.</summary>
    public bool IsSuggestion => string.Equals(Severity, "Suggestion", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether this term has Info severity.</summary>
    public bool IsInfo => string.Equals(Severity, "Info", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the sort order for severity (lower = more severe).
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for default DataGrid sorting - Errors first, then Warning, etc.
    /// </remarks>
    public int SeveritySortOrder => Severity.ToUpperInvariant() switch
    {
        "ERROR" => 0,
        "WARNING" => 1,
        "SUGGESTION" => 2,
        "INFO" => 3,
        _ => 4
    };

    /// <summary>
    /// Gets the severity badge color.
    /// </summary>
    public IBrush SeverityColor => Severity.ToUpperInvariant() switch
    {
        "ERROR" => Brushes.Crimson,
        "WARNING" => Brushes.Orange,
        "SUGGESTION" => Brushes.DodgerBlue,
        "INFO" => Brushes.Gray,
        _ => Brushes.Gray
    };

    #endregion

    #region Active Status Display

    /// <summary>
    /// Gets the icon path data for the active status indicator.
    /// </summary>
    /// <remarks>
    /// LOGIC: Checkmark for active, X for inactive (Material Design icons).
    /// </remarks>
    public string ActiveIcon => IsActive
        ? "M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z"  // Checkmark
        : "M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z";  // X

    /// <summary>
    /// Gets the color for the active status icon.
    /// </summary>
    public IBrush ActiveColor => IsActive ? Brushes.Green : Brushes.Gray;

    #endregion
}
