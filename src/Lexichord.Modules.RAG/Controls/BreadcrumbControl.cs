// =============================================================================
// File: BreadcrumbControl.cs
// Project: Lexichord.Modules.RAG
// Description: Custom control for displaying heading breadcrumb trails.
// =============================================================================
// LOGIC: Displays a formatted breadcrumb path with bookmark icon and muted text.
//   Shows the hierarchical location of a chunk within its parent document
//   (e.g., "Chapter 1 > Section 2 > Subsection A").
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using Avalonia;
using Avalonia.Controls.Primitives;

namespace Lexichord.Modules.RAG.Controls;

/// <summary>
/// Control for displaying heading breadcrumb trails in search results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BreadcrumbControl"/> displays a formatted breadcrumb trail showing
/// the hierarchical location of a chunk within its parent document. The breadcrumb
/// is rendered with a bookmark icon prefix and muted text styling for visual
/// distinction from primary content.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// &lt;controls:BreadcrumbControl Breadcrumb="{Binding ContextPreview.Breadcrumb}" /&gt;
/// </code>
/// </para>
/// <para>
/// <b>Visual Style:</b> The control displays:
/// <list type="bullet">
///   <item><description>A bookmark icon on the left</description></item>
///   <item><description>The breadcrumb text with truncation on overflow</description></item>
///   <item><description>Muted foreground color for visual hierarchy</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d as part of The Context Window feature.
/// </para>
/// </remarks>
public class BreadcrumbControl : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="Breadcrumb"/> styled property.
    /// </summary>
    /// <remarks>
    /// LOGIC: Property for the breadcrumb text to display. When null or empty,
    /// the control should be hidden via IsVisible binding.
    /// </remarks>
    public static readonly StyledProperty<string?> BreadcrumbProperty =
        AvaloniaProperty.Register<BreadcrumbControl, string?>(
            nameof(Breadcrumb),
            defaultValue: null);

    /// <summary>
    /// Gets or sets the breadcrumb text to display.
    /// </summary>
    /// <value>
    /// The formatted breadcrumb trail (e.g., "Chapter 1 > Section 2").
    /// Null or empty if no breadcrumb is available.
    /// </value>
    public string? Breadcrumb
    {
        get => GetValue(BreadcrumbProperty);
        set => SetValue(BreadcrumbProperty, value);
    }

    /// <summary>
    /// Gets whether the breadcrumb has content to display.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Breadcrumb"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasBreadcrumb => !string.IsNullOrEmpty(Breadcrumb);
}
