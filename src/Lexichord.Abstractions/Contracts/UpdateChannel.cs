namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents the update channel for the application.
/// </summary>
/// <remarks>
/// LOGIC: Users can switch between channels to receive different types of updates.
/// Stable receives production-ready releases; Insider receives early access builds.
///
/// Version: v0.1.6d
/// </remarks>
public enum UpdateChannel
{
    /// <summary>
    /// Production-ready releases with thorough testing.
    /// </summary>
    Stable,

    /// <summary>
    /// Early access builds with new features; may contain bugs.
    /// </summary>
    Insider
}
