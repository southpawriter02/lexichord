namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Root data transfer object for serialized layouts.
/// </summary>
/// <param name="Metadata">Layout metadata including version.</param>
/// <param name="Root">The root dock node.</param>
/// <remarks>
/// LOGIC: LayoutData is the top-level container for serialized layouts.
/// It contains metadata for versioning and the complete dock tree.
/// </remarks>
public record LayoutData(
    LayoutMetadata Metadata,
    DockNodeData Root
);
