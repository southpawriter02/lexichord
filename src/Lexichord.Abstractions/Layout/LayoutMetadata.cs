namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Metadata for a serialized layout.
/// </summary>
/// <param name="SchemaVersion">Version of the layout schema.</param>
/// <param name="ProfileName">Name of this layout profile.</param>
/// <param name="SavedAt">UTC timestamp when saved.</param>
/// <param name="AppVersion">Lexichord version that saved this layout.</param>
/// <remarks>
/// LOGIC: Metadata enables:
/// - Schema migration when format changes
/// - Profile identification
/// - Debugging (when was this saved, by what version)
/// </remarks>
public record LayoutMetadata(
    int SchemaVersion,
    string ProfileName,
    DateTime SavedAt,
    string AppVersion
)
{
    /// <summary>
    /// Current schema version for new layouts.
    /// </summary>
    /// <remarks>
    /// LOGIC: Increment this when making breaking changes to the layout format.
    /// Always add migration code for old versions.
    /// </remarks>
    public const int CurrentSchemaVersion = 1;
}
