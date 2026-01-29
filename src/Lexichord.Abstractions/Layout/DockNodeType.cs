using System.Text.Json.Serialization;

namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Type of dock node.
/// </summary>
/// <remarks>
/// LOGIC: Represents the different types of dock containers in the hierarchy.
/// Each type has specific behavior and properties.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DockNodeType
{
    /// <summary>Root dock container.</summary>
    Root,

    /// <summary>Proportional dock with horizontal or vertical orientation.</summary>
    Proportional,

    /// <summary>Tool dock for auxiliary panels.</summary>
    Tool,

    /// <summary>Document dock for tabbed content.</summary>
    Document,

    /// <summary>Splitter between docks.</summary>
    Splitter,

    /// <summary>Individual dockable item.</summary>
    Dockable
}
