// =============================================================================
// File: LinkingReviewPanel.axaml.cs
// Project: Lexichord.Modules.Knowledge
// Description: Code-behind for LinkingReviewPanel.
// =============================================================================
// LOGIC: Minimal code-behind for the Linking Review Panel. ViewModel is injected
//   via DI and bound in the constructor.
//
// v0.5.5c-i: Linking Review UI
// Dependencies: LinkingReviewViewModel (v0.5.5c-i)
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.Knowledge.UI.Views;

/// <summary>
/// Code-behind for the Linking Review Panel.
/// </summary>
/// <remarks>
/// <para>
/// The view binds to <see cref="ViewModels.LinkingReview.LinkingReviewViewModel"/>
/// which is set as the DataContext via DI injection.
/// </para>
/// <para>
/// <b>Keyboard Shortcuts:</b> Defined in XAML via KeyBindings:
/// <list type="bullet">
///   <item><description>Ctrl+A: Accept</description></item>
///   <item><description>Ctrl+R: Reject</description></item>
///   <item><description>Ctrl+S: Skip</description></item>
///   <item><description>Ctrl+N: Create New Entity</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5c-i as part of the Linking Review UI.
/// </para>
/// </remarks>
public partial class LinkingReviewPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinkingReviewPanel"/> class.
    /// </summary>
    public LinkingReviewPanel()
    {
        InitializeComponent();
    }
}
