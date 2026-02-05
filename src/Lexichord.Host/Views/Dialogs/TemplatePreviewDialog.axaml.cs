// ═══════════════════════════════════════════════════════════════════════════════
// LEXICHORD - Template Preview Dialog (v0.6.3d Specification)
// Modal dialog for previewing and validating prompt templates
// ═══════════════════════════════════════════════════════════════════════════════

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Lexichord.Host.Views.Dialogs;

/// <summary>
/// Template Preview Dialog for viewing rendered prompt templates.
/// </summary>
/// <remarks>
/// SPEC: LCS-DES-v0.6.3d, Section 15.1 - Template Preview UI
/// This dialog was specified but deferred to v0.6.4. Now implemented.
///
/// Features:
/// - Template selector dropdown
/// - Rendered prompt preview with syntax highlighting
/// - Variable list with types and required status
/// - Validation status badge
/// - Token count indicator
/// </remarks>
public partial class TemplatePreviewDialog : Window
{
    public TemplatePreviewDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
