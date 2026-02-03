// =============================================================================
// File: ContextChunkControl.cs
// Project: Lexichord.Modules.RAG
// Description: Custom control for displaying context chunk text with muting.
// =============================================================================
// LOGIC: Displays chunk content text with optional muting (reduced opacity).
//   Used to show before/after context chunks with visual distinction from
//   the core search result chunk.
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using Avalonia;
using Avalonia.Controls.Primitives;

namespace Lexichord.Modules.RAG.Controls;

/// <summary>
/// Control for displaying context chunk text with optional muting.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ContextChunkControl"/> displays chunk content text with configurable
/// muting (reduced opacity) for visual distinction. Context chunks (before/after)
/// are typically muted to emphasize the core search result chunk.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// &lt;controls:ContextChunkControl
///     Content="{Binding Content}"
///     IsMuted="True" /&gt;
/// </code>
/// </para>
/// <para>
/// <b>Visual Style:</b>
/// <list type="bullet">
///   <item><description>Text wrapping enabled by default</description></item>
///   <item><description>Max 4 lines for context chunks</description></item>
///   <item><description>Reduced opacity (0.7) when muted</description></item>
///   <item><description>Normal opacity (1.0) for core chunks</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3d as part of The Context Window feature.
/// </para>
/// </remarks>
public class ContextChunkControl : TemplatedControl
{
    /// <summary>
    /// Default opacity for muted context chunks.
    /// </summary>
    public const double MutedOpacity = 0.7;

    /// <summary>
    /// Default opacity for non-muted (core) chunks.
    /// </summary>
    public const double NormalOpacity = 1.0;

    /// <summary>
    /// Defines the <see cref="ChunkContent"/> styled property.
    /// </summary>
    /// <remarks>
    /// LOGIC: Property for the chunk text content to display.
    /// Named "ChunkContent" to avoid conflict with ContentControl.Content.
    /// </remarks>
    public static readonly StyledProperty<string?> ChunkContentProperty =
        AvaloniaProperty.Register<ContextChunkControl, string?>(
            nameof(ChunkContent),
            defaultValue: null);

    /// <summary>
    /// Defines the <see cref="IsMuted"/> styled property.
    /// </summary>
    /// <remarks>
    /// LOGIC: Property indicating whether the chunk should be displayed with
    /// reduced opacity. Context chunks (before/after) are muted; core chunks are not.
    /// </remarks>
    public static readonly StyledProperty<bool> IsMutedProperty =
        AvaloniaProperty.Register<ContextChunkControl, bool>(
            nameof(IsMuted),
            defaultValue: false);

    /// <summary>
    /// Defines the <see cref="MaxLines"/> styled property.
    /// </summary>
    /// <remarks>
    /// LOGIC: Property for the maximum number of lines to display before truncation.
    /// Defaults to 4 for context chunks to balance visibility and space.
    /// </remarks>
    public static readonly StyledProperty<int> MaxLinesProperty =
        AvaloniaProperty.Register<ContextChunkControl, int>(
            nameof(MaxLines),
            defaultValue: 4);

    /// <summary>
    /// Gets or sets the chunk text content to display.
    /// </summary>
    /// <value>
    /// The text content of the chunk. Null or empty results in no display.
    /// </value>
    public string? ChunkContent
    {
        get => GetValue(ChunkContentProperty);
        set => SetValue(ChunkContentProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the chunk should be displayed with reduced opacity.
    /// </summary>
    /// <value>
    /// <c>true</c> to display with muted styling; <c>false</c> for normal styling.
    /// Default is <c>false</c>.
    /// </value>
    public bool IsMuted
    {
        get => GetValue(IsMutedProperty);
        set => SetValue(IsMutedProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of lines to display.
    /// </summary>
    /// <value>
    /// The maximum number of lines before truncation. Default is 4.
    /// </value>
    public int MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    /// <summary>
    /// Gets the current opacity based on muted state.
    /// </summary>
    /// <value>
    /// <see cref="MutedOpacity"/> if <see cref="IsMuted"/> is true;
    /// <see cref="NormalOpacity"/> otherwise.
    /// </value>
    public double ContentOpacity => IsMuted ? MutedOpacity : NormalOpacity;

    /// <summary>
    /// Gets whether there is content to display.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ChunkContent"/> is not null or empty; otherwise, <c>false</c>.
    /// </value>
    public bool HasContent => !string.IsNullOrEmpty(ChunkContent);
}
