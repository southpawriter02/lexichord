using Lexichord.Abstractions.Contracts;

using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a module is successfully loaded.
/// </summary>
/// <remarks>
/// LOGIC: This event is published after a module's InitializeAsync() completes successfully.
/// Subscribers can use this for:
/// - Logging module load events centrally
/// - Updating UI to show module status
/// - Triggering post-load actions (e.g., module-specific migrations)
///
/// Note: This event will be published via MediatR in v0.0.7.
/// For now, the event record is defined for future use.
/// </remarks>
/// <param name="ModuleInfo">The loaded module's metadata.</param>
/// <param name="LoadDuration">Time taken to load and initialize the module.</param>
public record ModuleLoadedEvent(
    ModuleInfo ModuleInfo,
    TimeSpan LoadDuration
) : INotification;

/// <summary>
/// Published when a module fails to load.
/// </summary>
/// <remarks>
/// LOGIC: This event is published when module loading fails for any reason:
/// - Assembly not found
/// - IModule type not found
/// - License check failed
/// - RegisterServices threw exception
/// - InitializeAsync threw exception
///
/// Subscribers can use this for:
/// - Alerting users about missing features
/// - Logging failures for diagnostics
/// - Triggering fallback behavior
/// </remarks>
/// <param name="AssemblyPath">Path to the failed assembly.</param>
/// <param name="ModuleName">Name of the module, if known.</param>
/// <param name="FailureReason">Human-readable reason for failure.</param>
/// <param name="Exception">The exception that caused the failure, if any.</param>
public record ModuleLoadFailedEvent(
    string AssemblyPath,
    string? ModuleName,
    string FailureReason,
    Exception? Exception
) : INotification;

/// <summary>
/// Published when a module is unloaded (future use).
/// </summary>
/// <remarks>
/// LOGIC: Reserved for future hot-reload functionality.
/// In v0.0.4, modules are loaded once at startup and never unloaded.
/// This event is defined for API completeness and future compatibility.
/// </remarks>
/// <param name="ModuleInfo">The unloaded module's metadata.</param>
public record ModuleUnloadedEvent(
    ModuleInfo ModuleInfo
) : INotification;
