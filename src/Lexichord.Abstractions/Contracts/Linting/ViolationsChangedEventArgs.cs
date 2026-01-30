namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Types of changes to the violation collection.
/// </summary>
/// <remarks>
/// LOGIC: Used to indicate what type of change triggered the ViolationsChanged event.
/// This enables renderers to optimize their update strategy.
///
/// Version: v0.2.4a
/// </remarks>
public enum ViolationChangeType
{
    /// <summary>The entire violation collection was replaced.</summary>
    Replaced = 0,

    /// <summary>One or more violations were added.</summary>
    Added = 1,

    /// <summary>One or more violations were removed.</summary>
    Removed = 2,

    /// <summary>All violations were cleared.</summary>
    Cleared = 3
}

/// <summary>
/// Event arguments for the ViolationsChanged event.
/// </summary>
/// <remarks>
/// LOGIC: Provides metadata about what changed in the violation collection
/// to enable efficient UI updates. The renderer can use this information
/// to decide whether to do a full redraw or incremental update.
///
/// Version: v0.2.4a
/// </remarks>
public sealed class ViolationsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or initializes the type of change that occurred.
    /// </summary>
    public required ViolationChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets or initializes the total number of violations after the change.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets or initializes the count of Error-severity violations.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets or initializes the count of Warning-severity violations.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Gets or initializes the count of Info-severity violations.
    /// </summary>
    public int InfoCount { get; init; }

    /// <summary>
    /// Gets or initializes the count of Hint-severity violations.
    /// </summary>
    public int HintCount { get; init; }

    /// <summary>
    /// Creates event args for a replace operation.
    /// </summary>
    /// <param name="violations">The new violation collection.</param>
    /// <returns>Event args populated with counts from the collection.</returns>
    public static ViolationsChangedEventArgs ForReplaced(
        IReadOnlyList<AggregatedStyleViolation> violations)
    {
        return new ViolationsChangedEventArgs
        {
            ChangeType = ViolationChangeType.Replaced,
            TotalCount = violations.Count,
            ErrorCount = violations.Count(v => v.Severity == ViolationSeverity.Error),
            WarningCount = violations.Count(v => v.Severity == ViolationSeverity.Warning),
            InfoCount = violations.Count(v => v.Severity == ViolationSeverity.Info),
            HintCount = violations.Count(v => v.Severity == ViolationSeverity.Hint)
        };
    }

    /// <summary>
    /// Creates event args for a clear operation.
    /// </summary>
    /// <returns>Event args indicating all violations were cleared.</returns>
    public static ViolationsChangedEventArgs ForCleared()
    {
        return new ViolationsChangedEventArgs
        {
            ChangeType = ViolationChangeType.Cleared,
            TotalCount = 0,
            ErrorCount = 0,
            WarningCount = 0,
            InfoCount = 0,
            HintCount = 0
        };
    }
}
