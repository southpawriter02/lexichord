// =============================================================================
// File: CitationViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model for the citation panel displaying entity citations.
// =============================================================================
// LOGIC: Bridges the CitationMarkup data from IEntityCitationRenderer to
//   the CitationPanel AXAML view. Converts CitationMarkup into observable
//   properties: citations list, validation status text, icon character,
//   and icon colour. Uses CommunityToolkit.Mvvm for [ObservableProperty].
//
// v0.6.6h: Entity Citation Renderer (CKVS Phase 3b)
// Dependencies: CitationMarkup (v0.6.6h), ValidationIcon (v0.6.6h),
//               CommunityToolkit.Mvvm, Avalonia.Media
// =============================================================================

using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;

namespace Lexichord.Modules.Knowledge.Copilot.UI;

/// <summary>
/// View model for the citation panel displaying entity citations.
/// </summary>
/// <remarks>
/// <para>
/// Provides observable properties bound by the <see cref="CitationPanel"/>
/// AXAML view. Call <see cref="Update"/> with a <see cref="CitationMarkup"/>
/// to refresh the displayed citations and validation status.
/// </para>
/// <para>
/// <b>Binding Properties:</b>
/// <list type="bullet">
///   <item><see cref="Citations"/> — list of <see cref="CitationItemViewModel"/>.</item>
///   <item><see cref="ValidationStatus"/> — status text (e.g., "Validation passed").</item>
///   <item><see cref="ValidationIconText"/> — icon character (✓, ⚠, ✗, ?).</item>
///   <item><see cref="ValidationColor"/> — icon brush (Green, Orange, Red, Gray).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6h as part of the Entity Citation Renderer.
/// </para>
/// </remarks>
public partial class CitationViewModel : ObservableObject
{
    /// <summary>
    /// List of citation item view models for display.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<CitationItemViewModel> _citations = [];

    /// <summary>
    /// Validation status text (e.g., "Validation passed", "2 warning(s)").
    /// </summary>
    [ObservableProperty]
    private string _validationStatus = "";

    /// <summary>
    /// Validation icon character (✓, ⚠, ✗, or ?).
    /// </summary>
    [ObservableProperty]
    private string _validationIconText = "?";

    /// <summary>
    /// Colour brush for the validation icon.
    /// </summary>
    [ObservableProperty]
    private IBrush _validationColor = Brushes.Gray;

    /// <summary>
    /// Updates the view model from a <see cref="CitationMarkup"/>.
    /// </summary>
    /// <param name="markup">
    /// The citation markup produced by <see cref="IEntityCitationRenderer.GenerateCitations"/>.
    /// </param>
    /// <remarks>
    /// LOGIC: Converts each <see cref="EntityCitation"/> to a
    /// <see cref="CitationItemViewModel"/> with appropriate verification
    /// marks and colours. Maps the <see cref="ValidationIcon"/> to a
    /// Unicode character and colour brush.
    /// </remarks>
    public void Update(CitationMarkup markup)
    {
        // Convert citations to item view models
        Citations = markup.Citations.Select(c => new CitationItemViewModel
        {
            TypeIcon = c.TypeIcon ?? "📄",
            DisplayLabel = c.DisplayLabel,
            IsVerified = c.IsVerified,
            VerifiedMark = c.IsVerified ? "✓" : "?",
            VerifiedColor = c.IsVerified ? Brushes.Green : Brushes.Orange
        }).ToList();

        // Set validation status text
        ValidationStatus = markup.ValidationStatus;

        // Map icon enum to character
        ValidationIconText = markup.Icon switch
        {
            ValidationIcon.CheckMark => "✓",
            ValidationIcon.Warning => "⚠",
            ValidationIcon.Error => "✗",
            _ => "?"
        };

        // Map icon enum to colour brush
        ValidationColor = markup.Icon switch
        {
            ValidationIcon.CheckMark => Brushes.Green,
            ValidationIcon.Warning => Brushes.Orange,
            ValidationIcon.Error => Brushes.Red,
            _ => Brushes.Gray
        };
    }
}
