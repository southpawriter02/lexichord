namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Contains metadata about a module.
/// </summary>
/// <remarks>
/// LOGIC: ModuleInfo is a record for immutability and value equality.
/// This metadata is used throughout the application for:
/// - Logging: "Loading module: {Name} v{Version} by {Author}"
/// - UI: Displaying loaded modules in settings/about screen
/// - Diagnostics: Troubleshooting which module versions are active
/// - Future: Dependency graph resolution based on Dependencies list
///
/// Design Decisions:
/// - Record type for immutability (metadata shouldn't change at runtime)
/// - Id uses lowercase, no spaces (machine-readable identifier)
/// - Name is human-readable (displayed in UI)
/// - Version uses System.Version for semantic versioning
/// - Dependencies is optional (most modules are independent)
/// </remarks>
/// <param name="Id">Unique identifier for the module (lowercase, no spaces). Example: "tuning", "memory", "agents"</param>
/// <param name="Name">Human-readable display name. Example: "Tuning Engine", "Vector Memory"</param>
/// <param name="Version">Semantic version of the module.</param>
/// <param name="Author">Module author or team name.</param>
/// <param name="Description">Brief description of module functionality.</param>
/// <param name="Dependencies">List of module IDs this module depends on. Used for load ordering.</param>
/// <example>
/// <code>
/// // Simple module without dependencies
/// var info = new ModuleInfo(
///     Id: "sandbox",
///     Name: "Sandbox Module",
///     Version: new Version(0, 0, 1),
///     Author: "Lexichord Team",
///     Description: "Test module for architecture validation"
/// );
///
/// // Module with dependencies
/// var info = new ModuleInfo(
///     Id: "agents",
///     Name: "AI Agents Ensemble",
///     Version: new Version(1, 0, 0),
///     Author: "Lexichord Team",
///     Description: "AI-powered writing agents",
///     Dependencies: ["memory", "llm-providers"]
/// );
/// </code>
/// </example>
public record ModuleInfo(
    string Id,
    string Name,
    Version Version,
    string Author,
    string Description,
    IReadOnlyList<string>? Dependencies = null
)
{
    /// <summary>
    /// Gets the module dependencies, or empty list if none.
    /// </summary>
    /// <remarks>
    /// LOGIC: Ensures Dependencies is never null for easier consumption.
    /// Callers can always iterate without null checks.
    /// </remarks>
    public IReadOnlyList<string> Dependencies { get; init; } = Dependencies ?? [];

    /// <summary>
    /// Returns a formatted string representation of the module info.
    /// </summary>
    /// <returns>A string in the format "Name vVersion by Author".</returns>
    public override string ToString() => $"{Name} v{Version} by {Author}";
}
