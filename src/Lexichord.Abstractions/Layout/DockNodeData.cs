namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Serialized data for a single dock node.
/// </summary>
/// <param name="Id">Unique identifier for the dock.</param>
/// <param name="Type">Type of dock (Root, Proportional, Tool, Document).</param>
/// <param name="Properties">Type-specific properties.</param>
/// <param name="Children">Child dock nodes.</param>
/// <remarks>
/// LOGIC: DockNodeData is a recursive structure that mirrors the dock tree.
/// Each node type has different Properties based on Type.
/// </remarks>
public record DockNodeData(
    string Id,
    DockNodeType Type,
    DockNodeProperties Properties,
    IReadOnlyList<DockNodeData>? Children = null
);
