namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Contains information about a module that failed to load.
/// </summary>
/// <remarks>
/// LOGIC: This record captures diagnostic information for module failures.
/// Used for:
/// - Logging failed modules during startup
/// - Displaying error information in diagnostics UI
/// - Troubleshooting module loading issues
///
/// FailureReason is human-readable; Exception provides technical details.
/// </remarks>
/// <param name="AssemblyPath">The file path of the assembly that failed.</param>
/// <param name="ModuleName">The name of the module type, if discovered before failure.</param>
/// <param name="FailureReason">Human-readable description of why loading failed.</param>
/// <param name="Exception">The exception that caused the failure, if any.</param>
public record ModuleLoadFailure(
    string AssemblyPath,
    string? ModuleName,
    string FailureReason,
    Exception? Exception
)
{
    /// <summary>
    /// Returns a formatted string representation of the failure.
    /// </summary>
    public override string ToString() =>
        ModuleName is not null
            ? $"{ModuleName} ({AssemblyPath}): {FailureReason}"
            : $"{AssemblyPath}: {FailureReason}";
}
