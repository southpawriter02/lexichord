using System.Text.Json.Serialization;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Dock alignment for tool docks.
/// </summary>
/// <remarks>
/// LOGIC: Specifies which edge of the workspace a tool dock aligns to.
/// This determines the dock's position and collapsing direction.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DockAlignment
{
    /// <summary>Aligned to left side.</summary>
    Left,

    /// <summary>Aligned to right side.</summary>
    Right,

    /// <summary>Aligned to top.</summary>
    Top,

    /// <summary>Aligned to bottom.</summary>
    Bottom
}
