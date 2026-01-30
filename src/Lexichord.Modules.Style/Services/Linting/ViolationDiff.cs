using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Calculates the difference between two violation sets.
/// </summary>
/// <remarks>
/// LOGIC: Used for incremental UI updates - only repaint
/// violations that changed. This reduces render overhead
/// when most violations remain stable between scans.
///
/// Version: v0.2.3d
/// </remarks>
internal static class ViolationDiff
{
    /// <summary>
    /// Calculates added, removed, and modified violations.
    /// </summary>
    /// <param name="previous">Previous violation set.</param>
    /// <param name="current">Current violation set.</param>
    /// <returns>Changes between the sets.</returns>
    /// <remarks>
    /// LOGIC: Comparison uses violation ID for identity:
    /// - Added: In current but not in previous
    /// - Removed: In previous but not in current
    /// - Modified: In both but with different content
    /// </remarks>
    public static ViolationChanges Calculate(
        IReadOnlyList<AggregatedStyleViolation> previous,
        IReadOnlyList<AggregatedStyleViolation> current)
    {
        if (previous.Count == 0 && current.Count == 0)
        {
            return ViolationChanges.Empty;
        }

        var previousSet = previous.ToDictionary(v => v.Id);
        var currentSet = current.ToDictionary(v => v.Id);

        var added = current
            .Where(v => !previousSet.ContainsKey(v.Id))
            .ToList();

        var removed = previous
            .Where(v => !currentSet.ContainsKey(v.Id))
            .ToList();

        var modified = current
            .Where(v => previousSet.TryGetValue(v.Id, out var old) && !v.Equals(old))
            .ToList();

        return new ViolationChanges(added, removed, modified);
    }
}

/// <summary>
/// Represents changes between violation sets.
/// </summary>
/// <param name="Added">Violations new in current set.</param>
/// <param name="Removed">Violations no longer present.</param>
/// <param name="Modified">Violations present in both but changed.</param>
public readonly record struct ViolationChanges(
    IReadOnlyList<AggregatedStyleViolation> Added,
    IReadOnlyList<AggregatedStyleViolation> Removed,
    IReadOnlyList<AggregatedStyleViolation> Modified)
{
    /// <summary>
    /// Gets whether there are any changes.
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Modified.Count > 0;

    /// <summary>
    /// Gets the total number of changes.
    /// </summary>
    public int TotalChanges => Added.Count + Removed.Count + Modified.Count;

    /// <summary>
    /// Empty changes result.
    /// </summary>
    public static ViolationChanges Empty { get; } = new(
        Array.Empty<AggregatedStyleViolation>(),
        Array.Empty<AggregatedStyleViolation>(),
        Array.Empty<AggregatedStyleViolation>());
}
