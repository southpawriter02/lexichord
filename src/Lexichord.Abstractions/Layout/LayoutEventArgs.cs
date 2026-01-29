namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Event arguments for layout saved events.
/// </summary>
/// <param name="ProfileName">Name of the saved profile.</param>
/// <param name="FilePath">Path where layout was saved.</param>
/// <param name="WasAutoSave">Whether this was an auto-save.</param>
public record LayoutSavedEventArgs(
    string ProfileName,
    string FilePath,
    bool WasAutoSave
);

/// <summary>
/// Event arguments for layout loaded events.
/// </summary>
/// <param name="ProfileName">Name of the loaded profile.</param>
/// <param name="SchemaVersion">Schema version of the loaded layout.</param>
/// <param name="WasMigrated">Whether schema migration was performed.</param>
public record LayoutLoadedEventArgs(
    string ProfileName,
    int SchemaVersion,
    bool WasMigrated
);
