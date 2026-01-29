using System.Text.Json.Serialization;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Dock orientation for proportional docks.
/// </summary>
/// <remarks>
/// LOGIC: Determines whether children in a proportional dock are
/// arranged horizontally (left-to-right) or vertically (top-to-bottom).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DockOrientation
{
    /// <summary>Children arranged horizontally.</summary>
    Horizontal,

    /// <summary>Children arranged vertically.</summary>
    Vertical
}
